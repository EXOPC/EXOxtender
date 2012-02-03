using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EXOxtender
{
    internal static class EXOMsg
    {
        //Hexadecimal constants
        internal const int WM_APP = 0x8000;

        //Decimal constants
        internal const int EX_TOUCH_SET = 1100;
        internal const int EX_TOUCH_AREA_POS = 1102;
        internal const int EX_TOUCH_AREA_SIZE = 1103;
        internal const int EX_GESTURE_ENABLE = 1104;
        internal const int EX_TOUCH_ENABLE = 1105;
        internal const int EX_TOUCH_IGNORE = 1106;
        internal const int EX_HARDWARE_REPORT_GET = 1140;
        internal const int EX_TRANSP_LAYER_OPEN = 1150;
        internal const int EX_TRANSP_LAYER_CLOSE = 1151;
        internal const int EX_DRAG_LISTENING_STOP = 1152;
        internal const int EX_DRAGGED_FILES_GET = 1153;
        internal const int EX_SHUTDOWN = 1999;
        internal const int EX_TOUCH_EVENT_START = 2075;
        internal const int EX_TOUCH_EVENT_END = 2076;
        internal const int EX_TOUCH_EVENT_MOVE = 2077;
        internal const int EX_HARDWARE_REPORT_READY = 2140;
        internal const int EX_DRAGGED_FILES_READY = 2150;

        //Sound
        internal const int EX_SND_SET = 1001;
        internal const int EX_SND_GET = 1002;
        internal const int EX_SND_INFO = 2010;
        internal const int EX_SND_EVENT_CHANGE = 2015;

        //Brightness
        internal const int EX_DISP_SET = 1022;
        internal const int EX_DISP_GET = 1023;
        internal const int EX_DISP_INFO = 2040;
        internal const int EX_DISP_EVENT_CHANGE = 2045;

        //WiFi
        internal const int EX_WIFI_GET = 1041;
        internal const int EX_WIFI_SET = 1040;
        internal const int EX_WIFI_INFO = 2020;
        internal const int EX_WIFI_EVENT_CHANGE = 2025;

        // UDP
        internal const int EX_UDP_GET = 1145;
        internal const int EX_UDP_SEND = 1146;
        internal const int EX_UDP_EVENT_RECEIVED = 2100;

        internal const int EX_DESTROYWINDOW = 1202;

    }
}
