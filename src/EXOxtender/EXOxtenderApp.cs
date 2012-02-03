using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Xml;
using System.Diagnostics;
using Windows7.Multitouch;
using Windows7.Multitouch.Interop;
using Windows7.Multitouch.Manipulation;
using System.Runtime.InteropServices;
using Windows7.Multitouch.WinForms;
using System.Security.Permissions;
using NativeWifi;

using System.IO;

namespace EXOxtender
{
    public partial class EXOxtenderApp : WMTouch.WMTouchForm
    {
        // Functions imported from our unmanaged DLL
        [DllImport("EXOxtender.Multitouch.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool InitializeTouch([MarshalAs(UnmanagedType.LPWStr)]string windowName, IntPtr childWindowHandle);
        [DllImport("EXOxtender.Multitouch.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void UninitializeTouch();
        [DllImport("EXOxtender.Multitouch.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void TouchIgnore(IntPtr childWindowHandle);
        [DllImport("EXOxtender.Multitouch.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void GestureEnable(IntPtr childWindowHandle);
        [DllImport("EXOxtender.Multitouch.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void TouchEnable(IntPtr childWindowHandle);

        // Declare external functions.
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out Point lpPoint);

        public const int WM_TOUCH = 0x0240;

        public const int EX_READY = 2000;
        public const int EX_OK = 1000;

        private const int UDP_PORT = 55445;

        private bool disposed = false;
        public IntPtr _exoUI;
        private string _windowName;
        private string _tempPath;
        StateObjClass _stateObj;
        private bool _showWindow = false;

        private bool hookModeEnabled = false;
        private bool transparentLayerModeEnabled = false;
        TransparentWindow tw;
        ApplicationContext _ctx;

        // WIFI
        private bool isMonitoringWifi = false;
        private WlanClient wlanClient = new WlanClient();


        private WlanClient.WlanInterface currentWlanInterface = null;
        private bool isWifiConnected = false;
        private bool previousIsWifiConnected = false;
        private int wifiSignal = 0;
        private bool ignoreOneConnectionNotification = false;
        private Object wifiMutex = new Object();
        private bool mustExit = false;
        private ThreadStart job;
        private Thread thread;

        private bool isMonitoringBrightness = false;
        private Object brightnessMutex = new Object();
        private byte[] brightnessLevels = null;

        static int _totalEvents = 0;

        private bool isListeningToUDP = false;
        private ThreadStart udpJob;
        private Thread udpThread;
        UdpClient udpClient = null;

        TransparentLayer transparentLayer = null;

        public EXOxtenderApp(ApplicationContext ctx, string windowName, string tempPath, string showWindow)
        {
            _ctx = ctx;
            _windowName = windowName;
            _tempPath = tempPath;
            if (_tempPath.Length > 0 && _tempPath[_tempPath.Length - 1] != '\\')
                _tempPath += '\\';
            InitializeComponent();

            job = new ThreadStart(wifiThread);
            thread = new Thread(job);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!string.IsNullOrEmpty(showWindow) && showWindow == "/ShowWindow")
            {
                _showWindow = true;
            }

            if (!Windows7.Multitouch.TouchHandler.DigitizerCapabilities.IsMultiTouchReady)
            {
                textBox1.Text += "Multitouch is not available";
            }

            _exoUI = MessageHelper.FindWindow_INTPTR(null, _windowName);

            if (_exoUI == IntPtr.Zero)
            {
                textBox1.Text += string.Format("Window \"{0}\" not found.  Please restart EXOxtender...{1}", _windowName, Environment.NewLine);
            }
            else
            {
                textBox1.Text += string.Format("Window \"{0}\" found!{1}", _windowName, Environment.NewLine);
                StartReadySignal();

                MonitorTouchEvents();
            }

            /*
            using (StreamWriter writer = new StreamWriter(@"\patrice\patrice2.txt", true))
            {
                writer.WriteLine("EXOxtender started");
            }
            */

            if (_showWindow)
            {
                this.Show();
            }
            else
            {
                // this is required for the transparent layer to work (?)
                this.Opacity = 0.0f;
                this.Show();
                this.Hide();
            }

            XmlWriter outXml = XmlWriter.Create(_tempPath + "test.xml");
            outXml.WriteStartElement("test");
            outXml.WriteEndElement();
            outXml.Close();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            //if (_showWindow)
            //{
            //    this.Show();
            //}
            //else
            //{
            //    HideWindow();
            //}
        }

        //protected override void SetVisibleCore(bool value)
        //{
        //    if (!_showWindow)
        //    {
        //        base.SetVisibleCore(false);
        //        return;
        //    }

        //    base.SetVisibleCore(value);
        //}


        private void StartReadySignal()
        {
            textBox1.Text += string.Format("Starting to send EX_READY message...{0}", Environment.NewLine);

            _stateObj = new StateObjClass();
            _stateObj.TimerCanceled = false;
            System.Threading.TimerCallback TimerDelegate =
                new System.Threading.TimerCallback(SendReadySignal);

            // Create a timer that calls a procedure every 2 seconds.
            // Note: There is no Start method; the timer starts running as soon as 
            // the instance is created.
            System.Threading.Timer TimerItem =
                new System.Threading.Timer(TimerDelegate, _stateObj, 100, 1000);

            // Save a reference for Dispose.
            _stateObj.TimerReference = TimerItem;

            textBox1.Text += string.Format("Waiting for EX_OK message...{0}", Environment.NewLine);
        }

        private void MonitorTouchEvents()
        {
            backgroundWorker1.RunWorkerAsync();
        }

        private void SendReadySignal(object StateObj)
        {
            StateObjClass State = (StateObjClass)StateObj;
            
            if (State.TimerCanceled)
            // Dispose Requested.
            {
                State.TimerReference.Dispose();
            }
            
            MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EX_READY, 0), 0);
        }

        public class StateObjClass
        {
            // Used to hold parameters for calls to TimerTask.
            //public int SomeValue;
            public System.Threading.Timer TimerReference;
            public bool TimerCanceled;

        }

        private void trace(String s)
        {
            using (StreamWriter writer = new StreamWriter(@"\patrice\patrice2.txt", true))
            {
                writer.WriteLine(s);
                writer.Flush();
            }
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            try
            {
                _exoUI = MessageHelper.FindWindow_INTPTR(null, _windowName);

                if (_exoUI != IntPtr.Zero && m.Msg == EXOMsg.WM_APP + 5)
                {

                    //SET UP VARIABLES
                    EXOxtenderLibrary.VolumeControl _vol;

                    int _hWnd = m.HWnd.ToInt32();
                    int _arg0 = MessageHelper.LoWord(m.WParam.ToInt32());
                    int _arg1 = MessageHelper.HiWord(m.WParam.ToInt32());
                    int _arg2 = MessageHelper.LoWord(m.LParam.ToInt32());
                    int _arg3 = MessageHelper.HiWord(m.LParam.ToInt32());
                    //EXO UI MessageID = 32773;

                    textBox1.Text += string.Format("WM_APP + 5 received: arg0:{0} arg1:{1} arg2:{2} arg3:{3}", _arg0, _arg1, _arg2, _arg3) + Environment.NewLine;

                    switch (_arg0)
                    {
                        case EXOMsg.EX_SHUTDOWN:
                            this.Close();
                            break;

                        case EXOMsg.EX_WIFI_GET:

                            lock (wifiMutex)
                            {
                                try
                                {
                                    bool setMonitoring;

                                    if (_arg1 == 1)
                                        setMonitoring = true;
                                    else if (_arg1 == 2)
                                        setMonitoring = false;
                                    else
                                        setMonitoring = isMonitoringWifi;

                                    setCurrentWlanInterface(setMonitoring);

                                    if (_arg1 == 0 || _arg1 == 1)
                                    {
                                        sendWifiStatus(EXOMsg.EX_WIFI_INFO);
                                    }
                                }
                                catch (Exception e)
                                {
                                    trace("wifi get exception: " + e.Message);
                                    trace(e.StackTrace);
                                }
                            }

                            break;

                        case EXOMsg.EX_WIFI_SET:

                            try
                            {
                                if (_arg1 == 1 || _arg1 == 2)
                                    wifiSet(_arg1);
                            }
                            catch (Exception e)
                            {
                                trace("wifi set exception: " + e.Message);
                                trace(e.StackTrace);
                            }

                            break;

                        case EXOMsg.EX_DISP_GET:

                            lock (brightnessMutex)
                            {
                                getBrightnessLevels();

                                if (_arg1 == 1)
                                    isMonitoringBrightness = true;
                                else if (_arg1 == 2)
                                    isMonitoringBrightness = false;

                                if (_arg1 == 0 || _arg1 == 1)
                                    sendBrightness(EXOMsg.EX_DISP_INFO);
                            }

                            break;

                        case EXOMsg.EX_DISP_SET:

                            lock (brightnessMutex)
                            {
                                if (_arg1 != 0) // 0 means ignore
                                {
                                    getBrightnessLevels();

                                    EXOxtenderLibrary.Brightness _bri0 = new EXOxtenderLibrary.Brightness();

                                    if (_arg1 == 2) // 2 means OFF
                                        _arg2 = 0;  // set it to minimum

                                    if (_arg2 < 0) _arg2 = 0;
                                    if (_arg2 >= brightnessLevels.Length)
                                        _arg2 = brightnessLevels.Length - 1;

                                    _bri0.BrightnessLevel = brightnessLevels[_arg2];
                                    _bri0.setBrightnessLevel();

                                    //int _returnValue0 = Convert.ToInt32(_bri0.getBrightness());

                                    //textBox1.Text += "EX_DISP_SET - New level:" + _arg2.ToString() + "\r\n";
                                    //textBox1.Text += "hWnd:" + m.HWnd.ToString() + " Msg:" + m.Msg.ToString() + " arg0:" + _arg0 + " arg1:" + _arg1 + " arg2:" + _arg2 + " arg3:" + _arg3 + "\r\n";
                                    //textBox1.Text += "ReturnCode:" + _arg2.ToString() + "\r\n";
                                }
                            }
                            break;

                        case EXOMsg.EX_SND_GET:
                            //GET THE VOLUME LEVEL
                            int _volumeLevel = EXOxtenderLibrary.VolumeControl.Instance.GetVolume();

                            EXOxtenderLibrary.VolumeControl.Instance.VolumeChanged -= Instance_VolumeChanged;

                            if (_arg1 == 0)
                            {
                                MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_SND_INFO, _volumeLevel), MessageHelper.MakeLParam(EXOxtenderLibrary.VolumeControl.Instance.isMute, 0));
                            }
                            else if (_arg1 == 1)
                            {
                                MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_SND_INFO, _volumeLevel), MessageHelper.MakeLParam(EXOxtenderLibrary.VolumeControl.Instance.isMute, 0));
                                EXOxtenderLibrary.VolumeControl.Instance.VolumeChanged += new EXOxtenderLibrary.VolumeControl.VolumeEventHandler(Instance_VolumeChanged);
                            }

                            //textBox1.Text += "UI_VOLUME_GET - Current level:" + _volumeLevel.ToString() + ", Mute:" + MessageHelper.HiWord(m.LParam.ToInt32()).ToString() + "\r\n";
                            //textBox1.Text += "hWnd:" + m.HWnd.ToString() + " Msg:" + m.Msg.ToString() + " arg0:" + _arg0 + " arg1:" + _arg1 + " arg2:" + _arg2 + " arg3:" + _arg3 + "\r\n";


                            break;

                        case EXOMsg.EX_SND_SET:
                            //SET THE VOLUME TO A SPECIFIED VALUE
                            int _newVolumeLevel = _arg1;
                            if (_newVolumeLevel > 0 && _newVolumeLevel < 101)
                            { EXOxtenderLibrary.VolumeControl.Instance.SetVolume(_newVolumeLevel); }

                            if (_arg2 == 1)
                            { EXOxtenderLibrary.VolumeControl.Instance.Mute = true; }
                            else if (_arg2 == 2)
                            { EXOxtenderLibrary.VolumeControl.Instance.Mute = false; }

                            //textBox1.Text += "UI_VOLUME_SET - New level:" + _newVolumeLevel.ToString() + ", Mute:" + MessageHelper.HiWord(m.LParam.ToInt32()).ToString() + "\r\n";
                            //textBox1.Text += "hWnd:" + m.HWnd.ToString() + " Msg:" + m.Msg.ToString() + " arg0:" + _arg0 + " arg1:" + _arg1 + " arg2:" + _arg2 + " arg3:" + _arg3 + "\r\n";
                            //try
                            //{
                            //    MessageHelper.PostMessage(_exoUI, WM_APP + 5, MessageHelper.MakeWParam(147, UI_VOLUME_SET), MessageHelper.MakeLParam(_vol.GetVolume(), _vol.isMute));
                            //}
                            //catch (Exception e)
                            //{ textBox1.Text = e.Message; }
                            break;
                        case EX_OK:
                            // Request Dispose of the timer object.
                            textBox1.Text += string.Format("EX_OK received! Stopping to send EX_READY message...{0}", Environment.NewLine);
                            _stateObj.TimerCanceled = true;
                            break;
                        case EXOMsg.EX_TOUCH_SET:
                            /*
                            using (StreamWriter writer = new StreamWriter(@"\patrice\patrice2.txt", true))
                            {
                                writer.WriteLine("EX_TOUCH_SET");
                            }
                            */
                            textBox1.Text += string.Format("EX_TOUCH_SET received!{0}", Environment.NewLine);
                            if (_arg1 == 1)
                            {
                                //Touch manager enabled in hook mode
                                textBox1.Text += string.Format("Initializing touch manager in hook mode...{0}", Environment.NewLine);

                                int keyboardHandle = MessageHelper.MakeWParam(_arg2, _arg3);
                                IntPtr keyboardIntPtr = new IntPtr(keyboardHandle);

                                //MessageBox.Show("keyboardptr: " + keyboardIntPtr.ToString() + "arg2: " + _arg2 + "arg3: " + _arg3);

                                if (InitializeTouch(_windowName, keyboardIntPtr))
                                {
                                    hookModeEnabled = true;
                                    textBox1.Text += string.Format("Touch manager successfully enabled!{0}", Environment.NewLine);
                                    /*
                                    using (StreamWriter writer = new StreamWriter(@"\patrice\patrice2.txt", true))
                                    {
                                        writer.WriteLine("InitTouch ok");
                                    }
                                    */
                                }
                                else
                                {
                                    textBox1.Text += string.Format("ERROR: Unable to initialize touch manager.{0}", Environment.NewLine);
                                    /*
                                    using (StreamWriter writer = new StreamWriter(@"\patrice\patrice2.txt", true))
                                    {
                                        writer.WriteLine("InitTouch failed");
                                    }
                                    */
                                }
                            }
                            else if (_arg1 == 2)
                            {
                                //Touch manager enabled in transparent layer mode
                                WindowWrapper wrapper = new WindowWrapper(_exoUI);

                                tw = new TransparentWindow();
                                tw.Touchdown += new EventHandler<EXOxtender.WMTouchEventArgs>(tw_Touchdown);
                                tw.Touchup += new EventHandler<EXOxtender.WMTouchEventArgs>(tw_Touchup);
                                tw.TouchMove += new EventHandler<EXOxtender.WMTouchEventArgs>(tw_TouchMove);

                                //tw.Show();
                                tw.Show(wrapper);
                                transparentLayerModeEnabled = true;

                                textBox1.Text += string.Format("Touch manager enabled in transparent layer mode.{0}", Environment.NewLine);
                            }
                            else if (_arg1 == 99)
                            {
                                //Touch manager disabled
                                textBox1.Text += string.Format("Disabling touch manager...{0}", Environment.NewLine);

                                if (hookModeEnabled)
                                {
                                    UninitializeTouch();
                                }

                                if (transparentLayerModeEnabled && tw != null)
                                {
                                    tw.Close();
                                }
                            }
                            break;
                        case EXOMsg.EX_TOUCH_AREA_POS:
                            if (tw != null)
                            {
                                string msg = string.Format("EX_TOUCH_AREA_POS received: X={0}, Y={1}{2}", _arg1, _arg2, Environment.NewLine);
                                textBox1.Text += msg;
                                tw.Location = new Point(_arg1, _arg2);
                            }
                            break;
                        case EXOMsg.EX_TOUCH_AREA_SIZE:
                            if (tw != null)
                            {
                                string msg = string.Format("EX_TOUCH_AREA_SIZE received: X={0}, Y={1}{2}", _arg1, _arg2, Environment.NewLine);
                                textBox1.Text += msg;
                                tw.Size = new Size(_arg1, _arg2);
                            }
                            break;
                        case EXOMsg.EX_GESTURE_ENABLE:
                            {
                                int windowHandle = MessageHelper.MakeWParam(_arg2, _arg3);
                                IntPtr windowIntPtr = new IntPtr(windowHandle);
                                GestureEnable(windowIntPtr);
                            }
                            break;
                        case EXOMsg.EX_TOUCH_ENABLE:
                            {
                                int windowHandle = MessageHelper.MakeWParam(_arg2, _arg3);
                                IntPtr windowIntPtr = new IntPtr(windowHandle);
                                TouchEnable(windowIntPtr);
                            }
                            break;
                        case EXOMsg.EX_TOUCH_IGNORE:
                            {
                                int windowHandle = MessageHelper.MakeWParam(_arg2, _arg3);
                                IntPtr windowIntPtr = new IntPtr(windowHandle);
                                TouchIgnore(windowIntPtr);
                            }
                            break;

                        case EXOMsg.EX_UDP_GET:
                            {
                                if (_arg1 == 1)
                                {
                                    if (!isListeningToUDP)
                                    {
                                        isListeningToUDP = true;
                                        udpJob = new ThreadStart(udpThreadProc);
                                        udpThread = new Thread(udpJob);
                                        udpThread.Start();
                                    }
                                }
                                else if (_arg1 == 2)
                                {
                                    if (isListeningToUDP)
                                    {
                                        isListeningToUDP = false;
                                        if (udpClient != null)
                                            udpClient.Close();
                                        udpThread.Abort();
                                    }
                                }
                            }
                            break;

                        case EXOMsg.EX_UDP_SEND:
                            {
                                try
                                {
                                    XmlTextReader reader = new XmlTextReader(_tempPath + "udp_send.xml");
                                    String ip = null;
                                    while (reader.Read())
                                    {
                                        if (reader.NodeType ==  XmlNodeType.Element && reader.Name == "message")
                                        {
                                             while (reader.MoveToNextAttribute())
                                             {
                                                 if (reader.Name == "ip")
                                                 {
                                                     ip = reader.Value;
                                                 }
                                             }
                                        }
                                        else if (reader.NodeType == XmlNodeType.Text && ip != null)
                                        {
                                            byte[] message = Encoding.ASCII.GetBytes(reader.Value);
                                            IPAddress ipAddress = IPAddress.Parse(ip);
                                            UdpClient socket = new UdpClient();
                                            socket.Connect(ipAddress, UDP_PORT);
                                            socket.Send(message, message.Length);
                                            socket.Close();
                                        }
                                    }
                                }
                                catch (IOException e)
                                {
                                    // ignore errors while sending UDP, anyway UDP is not reliable
                                }
                            }
                            break;
                        case EXOMsg.EX_HARDWARE_REPORT_GET:
                            hardwareReportGet();
                            break;
                        case EXOMsg.EX_TRANSP_LAYER_OPEN:
                            exTranspLayerOpen();
                            break;
                        case EXOMsg.EX_TRANSP_LAYER_CLOSE:
                            exTranspLayerClose();
                            break;
                    }
                }
                base.WndProc(ref m);
            }
            catch (Exception e)
            {
                trace("EXOxtender WndProc Exception: " + e.Message);
                trace(e.StackTrace);
            }
        }

        private void wifiSet(int arg1)
        {
            lock (wifiMutex)
            {
                if (arg1 == 1 && isWifiConnected) return;
                if (arg1 == 2 && !isWifiConnected) return;

                if (arg1 == 1)
                {
                    if (wlanClient.Interfaces.Length > 0)
                    {
                        // use the first interface
                        WlanClient.WlanInterface wlanIface = wlanClient.Interfaces[0];

                        Wlan.WlanProfileInfo[] profiles = wlanIface.GetProfiles();
                        if (profiles.Length > 0)
                        {
                            // use the first profile
                            String profileName = profiles[0].profileName;
                            wlanIface.Connect(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, profileName);
                        }
                    }
                }
                else if (arg1 == 2)
                {
                    if (currentWlanInterface != null)
                    {
                        currentWlanInterface.Disconnect();
                    }
                }
            }
        }

        private void wifiThread()
        {
            int count = 0;

            while (!mustExit)
            {
                lock (wifiMutex)
                {
                    try
                    {
                        if (isMonitoringWifi)
                        {
                            setCurrentWlanInterface(true);
                            sendWifiStatus(EXOMsg.EX_WIFI_EVENT_CHANGE);
                        }
                    }
                    catch (Exception e)
                    {
                        trace("wifi thread exception: " + e.Message);
                        trace(e.StackTrace);
                    }
                }

                ++count;
                if (count > 3)
                {
                    count = 0;
                    lock (brightnessMutex)
                    {
                        if (isMonitoringBrightness)
                            sendBrightness(EXOMsg.EX_DISP_EVENT_CHANGE);
                    }
                }

                Thread.Sleep(500);
            }
        }

        private void udpThreadProc()
        {
            try
            {
                int index = 1;
                while (isListeningToUDP)
                {
                    udpClient = new UdpClient(55445);
                    IPEndPoint ipEndPoint = new IPEndPoint(System.Net.IPAddress.Any, 0);
                    for (; ; )
                    {
                        byte[] message = udpClient.Receive(ref ipEndPoint);
                        FileStream udpFile = new FileStream(_tempPath + "udp_received_" + index + ".xml", FileMode.Create);
                        udpFile.Write(message, 0, message.Length);
                        udpFile.Close();
                        MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_UDP_EVENT_RECEIVED, index), MessageHelper.MakeLParam(0, 0));
                        ++index;
                        if (index > 100)
                            index = 1;
                    }
                }
            }
            catch (ThreadAbortException e)
            {
            }
        }

        private void setCurrentWlanInterface(bool setMonitoring)
        {
            /*
            // Remove the callbacks, if any
            if (isMonitoringWifi && currentWlanInterface != null)
            {
                currentWlanInterface.WlanNotification -= wlanIface_WlanNotification;
                currentWlanInterface.WlanConnectionNotification -= wlanIface_WlanConnectionNotification;
            }
            */

            currentWlanInterface = null;

            previousIsWifiConnected = isWifiConnected;
            isWifiConnected = false;

            // find the first connected interface
            foreach (WlanClient.WlanInterface wlanIface in wlanClient.Interfaces)
            {
                if (wlanIface.InterfaceState == Wlan.WlanInterfaceState.Connected)
                {
                    isWifiConnected = true;
                    currentWlanInterface = wlanIface;
                    break;
                }
            }

            /*
            if (isWifiConnected && setMonitoring)
            {
                // set the callbacks
                ignoreOneConnectionNotification = true;
                currentWlanInterface.WlanNotification += wlanIface_WlanNotification;
                currentWlanInterface.WlanConnectionNotification += wlanIface_WlanConnectionNotification;
            }
            else if (setMonitoring)
            {
                // no interface connected, but we want to monitor.
                // let's register a callback on each interface to see when one gets connected
                foreach (WlanClient.WlanInterface wlanIface in wlanClient.Interfaces)
                {
                    wlanIface.WlanConnectionNotification += wlanIface_WlanConnectionNotification;
                }
            }
            */

            isMonitoringWifi = setMonitoring;
        }

        private void sendWifiStatus(int message)
        {
            if (isWifiConnected)
            {
                Wlan.WlanConnectionAttributes attr = currentWlanInterface.CurrentConnection;

                if (attr.isState == Wlan.WlanInterfaceState.Connected)
                {
                    int signalQuality = Convert.ToInt32(attr.wlanAssociationAttributes.wlanSignalQuality);

                    if (signalQuality != wifiSignal || isWifiConnected != previousIsWifiConnected || message == EXOMsg.EX_WIFI_INFO)
                    {
                        MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(message, 1), MessageHelper.MakeLParam(signalQuality, 0));
                    }
                    wifiSignal = signalQuality;
                }
                else
                {
                    isWifiConnected = false;
                }
            }

            if (!isWifiConnected)
            {
                if (isWifiConnected != previousIsWifiConnected || message == EXOMsg.EX_WIFI_INFO)
                {
                    MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(message, 2), MessageHelper.MakeLParam(0, 0));
                }
            }
        }

        void wlanIface_WlanConnectionNotification(Wlan.WlanNotificationData notifyData, Wlan.WlanConnectionNotificationData connNotifyData)
        {
            if (ignoreOneConnectionNotification)
            {
                ignoreOneConnectionNotification = false;
                return;
            }
            setCurrentWlanInterface(true);
            sendWifiStatus(EXOMsg.EX_WIFI_EVENT_CHANGE);
        }

        void wlanIface_WlanNotification(Wlan.WlanNotificationData notifyData)
        {
            if (notifyData.notificationSource == Wlan.WlanNotificationSource.MSM)
            {
                NativeWifi.Wlan.WlanNotificationCodeMsm code = (NativeWifi.Wlan.WlanNotificationCodeMsm)notifyData.notificationCode;

                if (code == Wlan.WlanNotificationCodeMsm.SignalQualityChange)
                {
                    IntPtr val = notifyData.dataPtr;
                    int signalQuality = Convert.ToInt32((uint)Marshal.PtrToStructure(val, typeof(uint)));

                    MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_WIFI_EVENT_CHANGE, 1), MessageHelper.MakeLParam(signalQuality, 0));
                }
            }
        }

        private void getBrightnessLevels()
        {
            if (brightnessLevels == null)
            {
                EXOxtenderLibrary.Brightness _bri0 = new EXOxtenderLibrary.Brightness();
                brightnessLevels = _bri0.GetBrightnessLevels();
            }
        }

        private void sendBrightness(int message)
        {
            EXOxtenderLibrary.Brightness _bri = new EXOxtenderLibrary.Brightness();
            int _returnValue = Convert.ToInt32(_bri.getBrightness());

            //textBox1.Text += "hWnd:" + m.HWnd.ToString() + " Msg:" + m.Msg.ToString() + " arg0:" + _arg0 + " arg1:" + _arg1 + " arg2:" + _arg2 + " arg3:" + _arg3 + "\r\n";
            //textBox1.Text += "ReturnCode:" + _returnValue + "\r\n";

            MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(message, 1), MessageHelper.MakeLParam(_returnValue, brightnessLevels.Length));
        }

        void Instance_VolumeChanged(object sender, EventArgs e)
        {
            MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_SND_EVENT_CHANGE, EXOxtenderLibrary.VolumeControl.Instance.GetVolume()), MessageHelper.MakeLParam(EXOxtenderLibrary.VolumeControl.Instance.isMute, 0));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            HideWindow();
        }

        private void HideWindow()
        {
            //HIDE THE FORM AS WHEN IT GOES LIVE
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false; // This is optional
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            if (_exoUI != IntPtr.Zero && _exoUI != null)
            {
                //const int SC_MINIMIZE= 0xF020;
                const int SC_MAXIMIZE = 0xF030;
                const int WM_SYSCOMMAND = 0x0112;
                MessageHelper.PostMessage(_exoUI, WM_SYSCOMMAND, SC_MAXIMIZE, 0);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageHelper.PostMessage(this.Handle, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EX_OK, 0), 0);
            MessageHelper.PostMessage(this.Handle, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_TOUCH_SET, 1), 0);
        }


        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected override void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    if (components != null)
                    {
                        components.Dispose();
                    }

                    if(backgroundWorker1 != null)
                    {
                        backgroundWorker1.Dispose();
                    }

                    if (tw != null)
                    {
                        tw.Close();
                    }

                    if (_ctx != null)
                    {
                        _ctx.ExitThread();
                    }

                    mustExit = true;
                    thread.Join();

                    if (isListeningToUDP)
                    {
                        isListeningToUDP = false;
                        if (udpClient != null)
                            udpClient.Close();
                        udpThread.Abort();
                    }
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                if (hookModeEnabled)
                {
                    UninitializeTouch();
                }
                _exoUI = IntPtr.Zero;

                // Note disposing has been done.
                disposed = true;
            }
            base.Dispose(disposing);
        }

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~EXOxtenderApp()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _exoUI = MessageHelper.FindWindow_INTPTR(null, _windowName);

            if (_exoUI != IntPtr.Zero)
            {
                MessageHelper.PostMessage(this.Handle, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EX_OK, 0), 0);
                MessageHelper.PostMessage(this.Handle, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_TOUCH_SET, 2), 0);
                MessageHelper.PostMessage(this.Handle, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_TOUCH_AREA_POS, 300), MessageHelper.MakeLParam(300, 0));
                MessageHelper.PostMessage(this.Handle, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_TOUCH_AREA_SIZE, 300), MessageHelper.MakeLParam(300,0));
            }
        }

        void tw_Touchdown(object sender, EXOxtender.WMTouchEventArgs e)
        {
            if (_exoUI != IntPtr.Zero)
            {
                string msg = string.Format("TransparentWindow DOWN id={0}, x={1}, y={2}{3}", e.Id, e.AbsoluteLocationX, e.AbsoluteLocationY,Environment.NewLine);
                textBox1.Text += msg;
                MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_TOUCH_EVENT_START, e.Id), MessageHelper.MakeLParam(e.AbsoluteLocationX, e.AbsoluteLocationY));

                _totalEvents += 1;
            }
        }

        void tw_Touchup(object sender, EXOxtender.WMTouchEventArgs e)
        {
            if (_exoUI != IntPtr.Zero)
            {
                string msg = string.Format("TransparentWindow UP id={0}, x={1}, y={2}{3}", e.Id, e.AbsoluteLocationX, e.AbsoluteLocationY, Environment.NewLine);
                //textBox1.Text += msg;
                MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_TOUCH_EVENT_END, e.Id), MessageHelper.MakeLParam(e.AbsoluteLocationX, e.AbsoluteLocationY));

                _totalEvents += 1;
            }
        }

        void tw_TouchMove(object sender, EXOxtender.WMTouchEventArgs e)
        {
            if (_exoUI != IntPtr.Zero)
            {
                string msg = string.Format("TransparentWindow MOVE id={0}, x={1}, y={2}{3}", e.Id, e.AbsoluteLocationX, e.AbsoluteLocationY, Environment.NewLine);
                //textBox1.Text += msg;
                MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_TOUCH_EVENT_MOVE, e.Id), MessageHelper.MakeLParam(e.AbsoluteLocationX, e.AbsoluteLocationY));

                _totalEvents += 1;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                System.Threading.Thread.Sleep(500);

                backgroundWorker1.ReportProgress(0, _totalEvents * 2);
                _totalEvents = 0;

            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            lblEventsPerSec.Text = e.UserState.ToString();
            //textBox1.Text += e.UserState.ToString() + Environment.NewLine;

            IntPtr window = MessageHelper.FindWindow_INTPTR(null, _windowName);

            if (window == IntPtr.Zero)
            {
                this.Close();
            }
            else if (window != _exoUI)
            {
                _exoUI = window;
            }
            
        }

        private void btnShutdown_Click(object sender, EventArgs e)
        {
            MessageHelper.PostMessage(this.Handle, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_SHUTDOWN, 0), 0);
        }

        private void hardwareReportGet()
        {
            XmlWriter outXml = XmlWriter.Create(_tempPath + "hardware_report.xml");
            outXml.WriteStartElement("EXOxtender");

            // list mac addresses
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            if (nics != null)
            {
                foreach (NetworkInterface adapter in nics)
                {
                    if (adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    {
                        IPInterfaceProperties properties = adapter.GetIPProperties();
                        PhysicalAddress address = adapter.GetPhysicalAddress();
                        byte[] bytes = address.GetAddressBytes();
                        if (bytes != null && bytes.Length > 0)
                        {
                            string addr = "";
                            for (int i = 0; i < bytes.Length; i++)
                            {
                                // Display the physical address in hexadecimal.
                                addr += string.Format("{0}", bytes[i].ToString("X2"));
                            }
                            outXml.WriteElementString("macAddr", addr);
                        }
                    }
                }
            }

            outXml.WriteEndElement();
            outXml.Close();
            MessageHelper.PostMessage(_exoUI, EXOMsg.WM_APP + 5, MessageHelper.MakeWParam(EXOMsg.EX_HARDWARE_REPORT_READY, 0), 0);
        }

        private void exTranspLayerOpen()
        {
            if (transparentLayer != null)
                return;

            try
            {
                //Invoke((MethodInvoker)delegate { transparentLayer = new TransparentLayer(); });
                Point point;
                GetCursorPos(out point);
                transparentLayer = new TransparentLayer(_exoUI, _tempPath, point);
            }
            catch (Exception ex)
            {
                /*
                using (StreamWriter writer = new StreamWriter(@"\patrice\patrice2.txt", true))
                {
                    writer.WriteLine("Exception in exTranspLayer: " + ex.Message);
                    writer.WriteLine("Exception in exTranspLayer: " + ex.StackTrace);
                }
                */
            }
        }

        private void exTranspLayerClose()
        {
            if (transparentLayer == null)
                return;
            transparentLayer.Close();
            transparentLayer = null;
        }

    }

}

