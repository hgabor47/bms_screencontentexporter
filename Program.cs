/*
 
    DATASHEET
    input:  handle to window (int)
    output: variable length byte[]


 * */




using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ScreenContentExporter

{
    class Program
    {
        public static long JPEGQuality = 20L;
        static string shipUUID = "18c549f7-5254-44e7-8842-7ff7c3ba839f";  //This Pod is a SHIP
        static BabylonMS.BabylonMS bms;

        static IntPtr hwnd;
        static bool datavalid;
        static uint mintime;
        static BabylonMS.BMSEventSessionParameter screensession;        
        static Stopwatch stopWatch;

        static void Main(string[] args)
        {
            hwnd = IntPtr.Zero;
            datavalid = false;
            bms = BabylonMS.BabylonMS.ShipDocking(shipUUID,args);
            bms.Connected += ClientConnected;
            bms.Disconnected += Disconnected;
            bms.NewInputFrame += NewInputFrame;
            bms.OpenGate(false);//client 
            

            stopWatch = Stopwatch.StartNew();
            while (true)
            {
                if (datavalid)
                {
                    if (stopWatch.ElapsedMilliseconds > mintime)
                    {
                        stopWatch.Restart();
                        try
                        {
                            ContentCapture.User32.RECT r = new ContentCapture.User32.RECT();
                            ContentCapture.User32.GetWindowRect(hwnd, ref r);
                            MemoryStream zmem = new BabylonMS.zipper(ContentCapture.CaptureWindowToStream(hwnd, JPEGQuality)).GetZip(false);

                            BabylonMS.BMSField data = screensession.outputPack.GetFieldByName("DATA");
                            BabylonMS.BMSField recta = screensession.outputPack.GetFieldByName("RECT");
                            if (data != null)
                            {
                                data.Value(zmem.ToArray());
                            }
                            if (recta != null)
                            {
                                recta.Value((Int16)r.left);
                                recta.Value((Int16)r.top);
                                recta.Value((Int16)r.right);
                                recta.Value((Int16)r.bottom);
                            }
                            if (!screensession.TransferPacket(true)) {
                                Disconnected(null);
                            }
                            if (data!=null)
                                data.clearValue();
                            if (recta != null) {
                                recta.clearValue();
                            }
                        }
                        catch (Exception ) {
                            Disconnected(null);
                        };
                    } else
                    {
                        Thread.Sleep(10);
                    }
                }
                else
                {
                    Thread.Sleep(300);                    
                }
            }
        }
        static void Disconnected(BabylonMS.BMSEventSessionParameter session)
        {
            Environment.Exit(0);   
        }
        static void ClientConnected(BabylonMS.BMSEventSessionParameter session)
        {
            //session.inputPack.AddField("HWND", BabylonMS.BabylonMS.CONST_FT_INT64);  //hwnd = new IntPtr(Int64.Parse(chd.Attributes["handle"].Value));
            session.outputPack.AddField("DATA", BabylonMS.BabylonMS.CONST_FT_BYTE);
            session.outputPack.AddField("RECT", BabylonMS.BabylonMS.CONST_FT_INT16);
        }

        static void NewInputFrame(BabylonMS.BMSEventSessionParameter session)
        {
            screensession = session;            
            BabylonMS.BMSField cmdfield = session.inputPack.GetField(0);
            byte cmd = (byte)cmdfield.getValue(0);//CMD
            switch (cmd) {
                case VRMainContentExporter.VRCEShared.CONST_CAPTURE_START:
                    {
                        Int64 inp = session.inputPack.GetFieldByName("HWND").getValue(0);
                        mintime = (uint)session.inputPack.GetFieldByName("MINTIME").getValue(0);                                                
                        if (inp == 0)
                        {
                            hwnd = ContentCapture.User32.GetDesktopWindow(); //desktop
                        } else {
                            hwnd = new IntPtr(inp); //Window
                        }
                        datavalid = true;
                        break;
                    }
                case VRMainContentExporter.VRCEShared.CONST_CAPTURE_FOCUS_WINDOW:
                    {
                        //mert a következő Continous miatt ennek kell lennie
                        //cmdfield.clearValue().Value(VRMainContentExporter.VRCEShared.CONST_CAPTURE_START);
                        if (hwnd != IntPtr.Zero)
                        {
                            ContentCapture.User32.ForceWindowToForeground(hwnd);
                            ContentCapture.User32.SetForegroundWindow(hwnd);
                            restrictArea(hwnd, true, false);
                        }
                        break; }


            }
        }



        public static void restrictArea(IntPtr hwnd, bool center, bool restrict)
        {
            if (center || restrict)
            {
                ContentCapture.User32.RECT r = new ContentCapture.User32.RECT();
                ContentCapture.User32.GetWindowRect(hwnd, ref r);
                if (center)
                {
                    setMousePos(r.left + ((r.right - r.left) / 2), r.top + ((r.bottom - r.top) / 2));
                }
                if (restrict)
                {
                    setMouseArea(r.left, r.top, (r.right - r.left), (r.bottom - r.top));
                }
            }
        }

        public static void setMousePos(int x, int y)
        {
            System.Windows.Forms.Cursor.Position = new Point(x, y);
        }

        public static int left;
        public static int top;
        public static void setMouseArea(int x, int y, int w, int h)
        {
            System.Windows.Forms.Cursor.Clip = new Rectangle(x, y, w, h);
            left = x;
            top = y;
        }


    }
}
