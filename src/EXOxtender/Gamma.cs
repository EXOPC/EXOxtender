//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Runtime.InteropServices;
//using System.Drawing;

//namespace EXOxtenderLibrary
//{
//    public class Gamma
//    {
//        [DllImport("gdi32.dll")]
//        private unsafe static extern bool SetDeviceGammaRamp(Int32 hdc, void* ramp);

//        [DllImport("gdi32.dll")]
//        private unsafe static extern short GetDeviceGammaRamp(Int32 hdc, void* ramp);

//        //[DllImport("user32.dll")]
//        //private static IntPtr GetDC(IntPtr hWnd);

//        private static bool initialized = false;
//        private static Int32 hdc;

//        private static void InitializeClass()
//        {
//            if (initialized)
//                return;

//            //Get the hardware device context of the screen, we can do
//            //this by getting the graphics object of null (IntPtr.Zero)
//            //then getting the HDC and converting that to an Int32.
//            hdc = Graphics.FromHwnd(IntPtr.Zero).GetHdc().ToInt32();

//            initialized = true;
//        }

//        public bool setGamma(short gamma)
//        {
//            return doGamma(gamma);
//        }

//        public static unsafe bool doGamma(short gamma)
//        {
//            InitializeClass();
            
//            if (gamma > 255)
//                gamma = 255;

//            if (gamma < 0)
//                gamma = 0;

//            short* gArray = stackalloc short[3 * 256];
//            short* idx = gArray;

//            for (int j = 0; j < 3; j++)
//            {
//                for (int i = 0; i < 256; i++)
//                {
//                    int arrayVal = i * (gamma + 128);

//                    if (arrayVal > 65535)
//                        arrayVal = 65535;

//                    *idx = (short)arrayVal;
//                    idx++;
//                }
//            }

//            //For some reason, this always returns false?
//            bool retVal = SetDeviceGammaRamp(hdc, gArray);

//            return retVal;
//        }

//    }
//}
