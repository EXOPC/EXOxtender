using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EXOxtender
{
    internal class TouchMsgProcessor : MsgProcessor
    {
        internal TouchMsgProcessor(IntPtr handle)
            : base(handle)
        {

        }

        protected override void ProcessMessage(int arg0, int arg1, int arg2, int arg3)
        {
            if (IsActive)
            {
                switch (arg0)
                {
                    case EXOMsg.EX_TOUCH_SET:
                        //SetTouch();
                        break;
                }
            }
        }

        //private void SetTouch()
        //{
        //    //textBox1.Text += string.Format("EX_TOUCH_SET received!{0}", Environment.NewLine);
        //    if (_arg1 == 1)
        //    {
        //        //Touch manager enabled in hook mode
        //        //textBox1.Text += string.Format("Initializing touch manager in hook mode...{0}", Environment.NewLine);

        //        if (InitializeTouch(_exoUI))
        //        {
        //            //textBox1.Text += string.Format("Touch manager successfully enabled!{0}", Environment.NewLine);
        //        }
        //        else
        //        {
        //            //textBox1.Text += string.Format("ERROR: Unable to initialize touch manager.{0}", Environment.NewLine);
        //        }
        //    }
        //    else if (_arg1 == 2)
        //    {
        //        //Touch manager enabled in transparent layer mode
        //        //textBox1.Text += string.Format("ERROR: Transparent layer mode is not supported.{0}", Environment.NewLine);
        //        //throw new NotSupportedException("Transparent layer mode not yet supported.");
        //    }
        //    else if (_arg1 == 99)
        //    {
        //        //Touch manager disabled
        //        //textBox1.Text += string.Format("Disabling touch manager...{0}", Environment.NewLine);
        //        UninitializeTouch();
        //    }
        //}

        protected override void OnStart()
        {
            //throw new NotImplementedException();
        }

        protected override void OnStop()
        {
            //throw new NotImplementedException();
        }
    }
}
