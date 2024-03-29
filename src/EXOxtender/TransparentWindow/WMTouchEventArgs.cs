﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EXOxtender
{
    public class WMTouchEventArgs : System.EventArgs
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

        // Touch event flags ((TOUCHINPUT.dwFlags) [winuser.h]
        private const int TOUCHEVENTF_PRIMARY = 0x0010;

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

        public int AbsoluteLocationX { get; set; }
        public int AbsoluteLocationY { get; set; }

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
}
