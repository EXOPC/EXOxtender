using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EXOxtender
{
    class WindowWrapper : System.Windows.Forms.IWin32Window
    {
        private IntPtr _hwnd;

        public WindowWrapper(IntPtr handle)
        {
            _hwnd = handle;
        }

        IntPtr System.Windows.Forms.IWin32Window.Handle
        {
            get { return _hwnd; }
        }
    }
}
