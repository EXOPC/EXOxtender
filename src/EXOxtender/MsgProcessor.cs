using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EXOxtender
{
    internal abstract class MsgProcessor : IDisposable
    {
        protected IntPtr Handle { get; set; }
        protected bool IsActive { get; set; }

        internal MsgProcessor(IntPtr handle)
        {
            Handle = handle;
        }

        public void Start()
        {
            if (!IsActive)
            {
                IsActive = true;
                OnStart();
            }
        }

        public void Stop()
        {
            if (IsActive)
            {
                OnStop();
                IsActive = false;
            }
        }

        void IDisposable.Dispose()
        {
            OnStop();
        }

        protected abstract void OnStart();
        protected abstract void OnStop();
        internal void ProcessWindowMessage(ref System.Windows.Forms.Message m)
        {
            if (Handle != IntPtr.Zero && m.Msg == EXOMsg.WM_APP + 5)
            {
                int arg0 = MessageHelper.LoWord(m.WParam.ToInt32());
                int arg1 = MessageHelper.HiWord(m.WParam.ToInt32());
                int arg2 = MessageHelper.LoWord(m.LParam.ToInt32());
                int arg3 = MessageHelper.HiWord(m.LParam.ToInt32());

                ProcessMessage(arg0, arg1, arg2, arg3);
            }
        }

        protected abstract void ProcessMessage(int arg0, int arg1, int arg2, int arg3);
    }
}
