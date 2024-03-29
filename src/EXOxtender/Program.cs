﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows;
using System.Threading;

using System.IO;

namespace EXOxtender
{
    static class Program
    {
        static EXOxtenderApp window;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //args = new string[] { "toto" };
            if (args.Length > 0)
            {
                using (StreamWriter writer = new StreamWriter(@"\patrice\patrice2.txt", true))
                {
                    writer.WriteLine("EXOxtender started");
                    for (int i = 0; i < args.Length; ++i)
                        writer.WriteLine("arg " + i + " = " + args[i]);
                }
                string arg0 = args[0];
                string arg1 = string.Empty;
                string arg2 = string.Empty;

                if (args.Length > 1)
                {
                    arg1 = args[1];
                    if (args.Length > 2)
                        arg2 = args[2];
                }
                //switch (Convert.ToInt32(args[0]))
                //{
                //    case EXOxtender.EXOxtenderApp.UI_BRIGHT_SET:
                //        System.Environment.Exit(SetBrightness(Convert.ToInt32(args[1])));
                //        break;
                //    case EXOxtender.EXOxtenderApp.UI_BRIGHT_GET:
                //        System.Environment.Exit(GetBrightness());
                //        break;
                //}
                
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                //ApplicationContext ctx = new ApplicationContext();
                //EXOxtenderApp app = new EXOxtenderApp(ctx, arg0, arg1);

                //Application.Run(ctx);

                var thread = new Thread(() =>
                {
                    ApplicationContext ctx = new ApplicationContext();
                    window = new EXOxtenderApp(ctx, arg0, arg1, arg2);
                    var handle = window.Handle;
                    Application.Run(ctx);
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                //Application.Run(new EXOxtenderApp(null, arg0, arg1));
            }
            else
            {
                Application.Exit();
            }
        }

        public static int GetBrightness()
        {
            EXOxtenderLibrary.Brightness _bri = new EXOxtenderLibrary.Brightness();
            return Convert.ToInt32(_bri.getBrightness());
        }

        public static int SetBrightness(int _brightnessLevel)
        {
            EXOxtenderLibrary.Brightness _bri = new EXOxtenderLibrary.Brightness();
            _bri.BrightnessLevel = _brightnessLevel;
            _bri.setBrightnessLevel();
            return Convert.ToInt32(_bri.getBrightness());
        }

    }
}
