using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace EXOxtender
{
    public partial class TransparentLayer : Form
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private bool isDragListening = true;
        private string[] draggedFiles = null;
        private IntPtr _exoUI;
        private string _tempPath;
        private DragDropEffects dragFeedback = DragDropEffects.Copy;

        public TransparentLayer(IntPtr exoUI, string tempPath, int x, int y, int w, int h)
        {
            InitializeComponent();
            _exoUI = exoUI;
            _tempPath = tempPath;
            Load += new EventHandler(formLoad);
            TopMost = true;
            Opacity = 0.01f;
            Visible = true;
            FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            Show();
            this.Size = new Size(w, h);
            this.Left = x;
            this.Top = y;
            BringToFront();
        }

        private void formLoad(object sender, System.EventArgs e)
        {
            SetForegroundWindow(this.Handle);

            try
            {
                AllowDrop = true;
                DragEnter += new DragEventHandler(dragEnter);
                DragOver += new DragEventHandler(dragOver);
            }
            catch (Exception ex)
            {
                /*
                using (StreamWriter writer = new StreamWriter(@"\patrice\patrice2.txt", true))
                {
                    writer.WriteLine("Exception in TransparentLayer: " + ex.Message);
                    writer.WriteLine("Exception in TransparentLayer: " + ex.StackTrace);
                }
                */
            }
        }

        private void dragEnter(object sender, DragEventArgs e)
        {
            e.Effect = dragFeedback;

            // Check if the Dataformat of the data can be accepted
            // (we only accept file drops from Explorer, etc.)
            if (!isDragListening)
                return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // take note of the file name
                // Extract the data from the DataObject-Container into a string list
                draggedFiles = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                saveFilenamesToXml();
                isDragListening = false;
            }
        }

        private void saveFilenamesToXml()
        {
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Encoding = Encoding.ASCII;
            XmlWriter outXml = XmlWriter.Create(_tempPath + "draggedFiles.xml", xmlSettings);
            outXml.WriteStartElement("draggedFiles");

            if (draggedFiles != null)
            {
                foreach (string filename in draggedFiles)
                {
                    outXml.WriteStartElement("filename");
                    outXml.WriteCData(filename);
                    outXml.WriteEndElement();
                }
            }

            outXml.WriteEndElement();
            outXml.Close();
            MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_DRAGGED_FILES_READY, 0), 0);
        }

        private void dragOver(object sender, DragEventArgs e)
        {
            e.Effect = dragFeedback;
            BringToFront();
        }

        public void setFeedback(int feedbackCode)
        {
            dragFeedback =
                feedbackCode == 1 ? DragDropEffects.Copy :
                feedbackCode == 2 ? DragDropEffects.Move :
                feedbackCode == 4 ? DragDropEffects.Link :
                feedbackCode == 8 ? DragDropEffects.Scroll :
                feedbackCode == 11 ? DragDropEffects.All :
                DragDropEffects.None;
        }
    }

}
