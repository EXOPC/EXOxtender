﻿// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Permissions;

namespace EXOxtender.WMTouch
{
    // Base class for multi-touch aware form.
    // Receives touch notifications through Windows messages and converts them
    // to touch events Touchdown, Touchup and Touchmove.
    public class WMTouchForm : Form
    {
        ///////////////////////////////////////////////////////////////////////
        // Public interface

        // Constructor
        [SecurityPermission(SecurityAction.Demand)]
        public WMTouchForm()
        {
            // Setup handlers
            try
            {
                Load += new System.EventHandler(this.OnLoadHandler);
            }
            catch (Exception exception)
            {
                Debug.Print("ERROR: Could not add form load handler");
                Debug.Print(exception.ToString());
            }

            // GetTouchInputInfo need to be
            // passed the size of the structure it will be filling
            // we get the sizes upfront so they can be used later.
            touchInputSize = Marshal.SizeOf(new TOUCHINPUT());
        }

        ///////////////////////////////////////////////////////////////////////
        // Protected members, for derived classes.

        // Touch event handlers
        protected event EventHandler<WMTouchEventArgs> Touchdown;   // touch down event handler
        protected event EventHandler<WMTouchEventArgs> Touchup;     // touch up event handler
        protected event EventHandler<WMTouchEventArgs> TouchMove;   // touch move event handler

        // EventArgs passed to Touch handlers
        protected class WMTouchEventArgs : System.EventArgs
        {
            // Private data members
            private int x;                  // touch x client coordinate in pixels
            private int y;                  // touch y client coordinate in pixels
            private int id;                 // contact ID
            private int mask;               // mask which fields in the structure are valid
            private int flags;              // flags
            private int time;               // touch event time
            private int contactX;           // x size of the contact area in pixels
            private int contactY;           // y size of the contact area in pixels

            // Access to data members
            public int LocationX
            {
                get { return x; }
                set { x = value; }
            }
            public int LocationY
            {
                get { return y; }
                set { y = value; }
            }
            public int Id
            {
                get { return id; }
                set { id = value; }
            }
            public int Flags
            {
                get { return flags; }
                set { flags = value; }
            }
            public int Mask
            {
                get { return mask; }
                set { mask = value; }
            }
            public int Time
            {
                get { return time; }
                set { time = value; }
            }
            public int ContactX
            {
                get { return contactX; }
                set { contactX = value; }
            }
            public int ContactY
            {
                get { return contactY; }
                set { contactY = value; }
            }
            public bool IsPrimaryContact
            {
                get { return (flags & TOUCHEVENTF_PRIMARY) != 0; }
            }

            // Constructor
            public WMTouchEventArgs()
            {
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // Private class definitions, structures, attributes and native fn's
        //Exercise1-Task2-Step2 

        // Touch event window message constants [winuser.h]
        public const int WM_TOUCHMOVE = 0x0240;
        public const int WM_TOUCHDOWN = 0x0241;
        public const int WM_TOUCHUP = 0x0242;

        // Touch event flags ((TOUCHINPUT.dwFlags) [winuser.h]
        private const int TOUCHEVENTF_MOVE = 0x0001;
        private const int TOUCHEVENTF_DOWN = 0x0002;
        private const int TOUCHEVENTF_UP = 0x0004;
        private const int TOUCHEVENTF_INRANGE = 0x0008;
        private const int TOUCHEVENTF_PRIMARY = 0x0010;
        private const int TOUCHEVENTF_NOCOALESCE = 0x0020;
        private const int TOUCHEVENTF_PEN = 0x0040;

        // Touch input mask values (TOUCHINPUT.dwMask) [winuser.h]
        private const int TOUCHINPUTMASKF_TIMEFROMSYSTEM = 0x0001; // the dwTime field contains a system generated value
        private const int TOUCHINPUTMASKF_EXTRAINFO = 0x0002; // the dwExtraInfo field is valid
        private const int TOUCHINPUTMASKF_CONTACTAREA = 0x0004; // the cxContact and cyContact fields are valid

        // Touch API defined structures [winuser.h]
        //Exercise1-Task2-Step4 
        [StructLayout(LayoutKind.Sequential)]
        private struct TOUCHINPUT
        {
            public int x;
            public int y;
            public System.IntPtr hSource;
            public int dwID;
            public int dwFlags;
            public int dwMask;
            public int dwTime;
            public System.IntPtr dwExtraInfo;
            public int cxContact;
            public int cyContact;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTS
        {
            public short x;
            public short y;
        }

        // Currently touch/multitouch access is done through unmanaged code
        // We must p/invoke into user32 [winuser.h]
        //Exercise1-Task2-Step3 
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterTouchWindow(System.IntPtr hWnd, int ulFlags);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterTouchWindow_INT(int hWnd, int ulFlags);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTouchInputInfo(System.IntPtr hTouchInput, int cInputs, [In, Out] TOUCHINPUT[] pInputs, int cbSize);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern void CloseTouchInputHandle(System.IntPtr lParam);

        // Attributes
        private int touchInputSize;        // size of TOUCHINPUT structure

        ///////////////////////////////////////////////////////////////////////
        // Private methods

        // OnLoad window event handler: Registers the form for multi-touch input.
        // in:
        //      sender      object that has sent the event
        //      e           event arguments
        private void OnLoadHandler(Object sender, EventArgs e)
        {
            //ulong ulFlags = 0;
            int ulFlags = 0;
            try
            {
                if (!RegisterTouchWindow(this.Handle, ulFlags))
                {
                    Debug.Print("ERROR: Could not register window for touch");
                }
                //IntPtr _testApp = MessageHelper.FindWindow_INTPTR(null, "TestApp");
                //if (!RegisterTouchWindow(_testApp, ulFlags))
                //{
                //    Debug.Print("ERROR: Could not register TestApplicaiton for touch");
                //}
            }
            catch (Exception exception)
            {
                Debug.Print("ERROR: RegisterTouchWindow API not available");
                Debug.Print(exception.ToString());
            }
        }

        // Window procedure. Receives WM_ messages.
        // Translates WM_TOUCH window messages to touch events.
        // Normally, touch events are sufficient for a derived class,
        // but the window procedure can be overriden, if needed.
        // in:
        //      m       message
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            // Decode and handle WM_TOUCH* message.
            bool handled;
            switch (m.Msg)
            {
                case WM_TOUCHDOWN:
                case WM_TOUCHMOVE:
                case WM_TOUCHUP:
                    handled = DecodeTouch(ref m);
                    break;
                default:
                    handled = false;
                    break;
            }

            // Call parent WndProc for default message processing.
            base.WndProc(ref m);

            if (handled)
            {
                // Acknowledge event if handled.
                try
                {
                    m.Result = new System.IntPtr(1);
                }
                catch (Exception exception)
                {
                    Debug.Print("ERROR: Could not allocate result ptr");
                    Debug.Print(exception.ToString());
                }
            }
        }

        // Extracts lower 16-bit word from an 32-bit int.
        // in:
        //      number      int
        // returns:
        //      lower word
        private static int LoWord(int number)
        {
            return number & 0xffff;
        }

        // Decodes and handles WM_TOUCH* messages.
        // Unpacks message arguments and invokes appropriate touch events.
        // in:
        //      m           window message
        // returns:
        //      flag whether the message has been handled
        public bool DecodeTouch(ref Message m)
        {
            // More than one touchinput may be associated with a touch message,
            // so an array is needed to get all event information.
            int inputCount = LoWord(m.WParam.ToInt32()); // Number of touch inputs, actual per-contact messages

            TOUCHINPUT[] inputs; // Array of TOUCHINPUT structures
            try
            {
                inputs = new TOUCHINPUT[inputCount]; // Allocate the storage for the parameters of the per-contact messages
            }
            catch (Exception exception)
            {
                Debug.Print("ERROR: Could not allocate inputs array");
                Debug.Print(exception.ToString());
                return false;
            }

            Debug.Print("Inputs: " + inputs.Length.ToString());

            // Unpack message parameters into the array of TOUCHINPUT structures, each
            // representing a message for one single contact.
            //Exercise2-Task1-Step3 
            if (!GetTouchInputInfo(m.LParam, inputCount, inputs, touchInputSize))
            {
                // Get touch info failed.
                return false;
            }

            // For each contact, dispatch the message to the appropriate message
            // handler.
            // Note that for WM_TOUCHDOWN you can get down & move notifications
            // and for WM_TOUCHUP you can get up & move notifications
            // WM_TOUCHMOVE will only contain move notifications
            // and up & down notifications will never come in the same message
            bool handled = false; // // Flag, is message handled
            //Exercise2-Task1-Step4
            for (int i = 0; i < inputCount; i++)
            {
                TOUCHINPUT ti = inputs[i];

                // Assign a handler to this message.
                EventHandler<WMTouchEventArgs> handler = null;     // Touch event handler
                if ((ti.dwFlags & TOUCHEVENTF_DOWN) != 0)
                {
                    handler = Touchdown;
                }
                else if ((ti.dwFlags & TOUCHEVENTF_UP) != 0)
                {
                    handler = Touchup;
                }
                else if ((ti.dwFlags & TOUCHEVENTF_MOVE) != 0)
                {
                    handler = TouchMove;
                }

                Debug.Print("Inputs: " + inputs.Length.ToString() + "\r\nCoordinates: " + ti.x.ToString() + "," + ti.y.ToString());

                // Convert message parameters into touch event arguments and handle the event.
                if (handler != null)
                {
                    // Convert the raw touchinput message into a touchevent.
                    WMTouchEventArgs te; // Touch event arguments

                    try
                    {
                        te = new WMTouchEventArgs();
                    }
                    catch (Exception excep)
                    {
                        Debug.Print("Could not allocate WMTouchEventArgs");
                        Debug.Print(excep.ToString());
                        continue;
                    }

                    // TOUCHINFO point coordinates and contact size is in 1/100 of a pixel; convert it to pixels.
                    // Also convert screen to client coordinates.
                    te.ContactY = ti.cyContact / 100;
                    te.ContactX = ti.cxContact / 100;
                    te.Id = ti.dwID;
                    {
                        Point pt = PointToClient(new Point(ti.x / 100, ti.y / 100));
                        te.LocationX = pt.X;
                        te.LocationY = pt.Y;
                    }
                    te.Time = ti.dwTime;
                    te.Mask = ti.dwMask;
                    te.Flags = ti.dwFlags;

                    // Invoke the event handler.
                    handler(this, te);

                    // Mark this event as handled.
                    handled = true;
                }
            }

            CloseTouchInputHandle(m.LParam);

            return handled;
        }
    }
}
