using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EXOxtender.Interfaces
{
    internal interface IMsgProcessor
    {
        void ProcessWindowMessage(ref System.Windows.Forms.Message m);
    }
}
