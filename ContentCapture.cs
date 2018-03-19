using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

namespace ScreenContentExporter
{
    public class ContentCapture
    {

        /// <summary>
        /// Creates an Image object containing a screen shot of the entire desktop
        /// </summary>
        /// <returns></returns>
        public Image CaptureScreen()
        {
            return CaptureWindow(User32.GetDesktopWindow());
        }

        /// <summary>
        /// Creates an Image object containing a screen shot of a specific window
        /// </summary>
        /// <param name="handle">The handle to the window. (In windows forms, this is obtained by the Handle property)</param>
        /// <returns></returns>
        static public Image CaptureWindow(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            // get the size
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // bitblt over

            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
            // restore selection
            GDI32.SelectObject(hdcDest, hOld);
            // clean up 
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);

            // get a .NET image object for it

            Image img = Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            GDI32.DeleteObject(hBitmap);

            return img;
        }

        public IntPtr WindowType(IntPtr handle)
        {
            return User32.GetWindowLongPtr(handle, -16);
        }

        static public Bitmap CaptureWindowBitmap(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            // get the size
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;

            if (width < 1 || height < 1) //TODO ablakok összefűzése egy ablakká ha egy process gyermekei
            {
                return null;
            }
            // create a device context we can copy to
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // bitblt over

            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
            // restore selection
            GDI32.SelectObject(hdcDest, hOld);
            // clean up 
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);

            // get a .NET image object for it

            Bitmap bmp = Bitmap.FromHbitmap(hBitmap);
            // free up the Bitmap object
            GDI32.DeleteObject(hBitmap);

            return bmp;
        }

        /// <summary>
        /// Captures a screen shot of a specific window, and saves it to a file
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        public void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format)
        {
            Image img = CaptureWindow(handle);
            img.Save(filename, format);
        }
        public void CaptureWindowToStream(IntPtr handle, Stream st, ImageFormat format)
        {
            Image img = CaptureWindow(handle);
            img.Save(st, format);
        }

        static private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }


        static public byte[] CaptureWindowToByteArray(IntPtr handle,long compressionValue)
        {
            ImageCodecInfo jpgEncoder;
            System.Drawing.Imaging.Encoder myEncoder;
            EncoderParameters myEncoderParameters;

            jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            myEncoder = System.Drawing.Imaging.Encoder.Quality;
            myEncoderParameters = new EncoderParameters(1);


            Bitmap bmp = CaptureWindowBitmap(handle);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, compressionValue);
            myEncoderParameters.Param[0] = myEncoderParameter;
            using (var st = new MemoryStream())
            {
                bmp.Save(st, jpgEncoder,myEncoderParameters); // ImageFormat.jpeg compression value 70);
                return st.ToArray();
            }
        }
        static public MemoryStream CaptureWindowToStream(IntPtr handle, long compressionValue)
        {
            ImageCodecInfo jpgEncoder;
            System.Drawing.Imaging.Encoder myEncoder;
            EncoderParameters myEncoderParameters;

            jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            myEncoder = System.Drawing.Imaging.Encoder.Quality;
            myEncoderParameters = new EncoderParameters(1);


            Bitmap bmp = CaptureWindowBitmap(handle);
            if (bmp == null)
            {
                throw new Exception("Window not reached!");
            }
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, compressionValue);
            myEncoderParameters.Param[0] = myEncoderParameter;
            var st = new MemoryStream();
            bmp.Save(st, jpgEncoder, myEncoderParameters); // ImageFormat.jpeg compression value 70);
            return st;            
        }

        /// <summary>
        /// Captures a screen shot of the entire desktop, and saves it to a file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        public void CaptureScreenToFile(string filename, ImageFormat format)
        {
            Image img = CaptureScreen();
            img.Save(filename, format);
        }

        /// <summary>
        /// Helper class containing Gdi32 API functions
        /// </summary>
        private class GDI32
        {

            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter

            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                int nHeight);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        }

        /// <summary>
        /// Helper class containing User32 API functions
        /// </summary>
        public class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);


            [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
            private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);
            [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
            private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

            // This static method is required because Win32 does not support
            // GetWindowLongPtr directly
            public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
            {
                IntPtr a;
                if (IntPtr.Size == 8)
                    a = GetWindowLongPtr64(hWnd, nIndex);
                else
                    a = GetWindowLongPtr32(hWnd, nIndex);
                return a;
            }


            [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
            public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);
            [DllImport("User32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
            [DllImport("User32.dll")]
            public static extern bool IsIconic(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();
            [DllImport("kernel32.dll")]
            public static extern uint GetCurrentThreadId();
            [DllImport("user32.dll")]
            public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
            [DllImport("user32.dll")]
            public static extern bool BringWindowToTop(IntPtr hWnd);
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SetFocus(IntPtr hWnd);

            [DllImport("USER32.DLL")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            public static void bringToFront(string title)
            {
                // Get a handle to the Calculator application.
                IntPtr handle = FindWindow(null, title);

                // Verify that Calculator is a running process.
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                // Make Calculator the foreground application
                SetForegroundWindow(handle);
                ForceWindowToForeground(handle);
            }

            public static void AttachedThreadInputAction(Action action)
            {
                var foreThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
                var appThread = GetCurrentThreadId();
                bool threadsAttached = false;
                try
                {
                    threadsAttached =
                        foreThread == appThread ||
                        AttachThreadInput(foreThread, appThread, true);
                    if (threadsAttached) action();
                    else throw new ThreadStateException("AttachThreadInput failed.");
                }
                finally
                {
                    if (threadsAttached)
                        AttachThreadInput(foreThread, appThread, false);
                }
            }
            public const uint SW_SHOW = 5;

            ///<summary>
            /// Forces the window to foreground.
            ///</summary>
            ///hwnd">The HWND.</param>
            public static void ForceWindowToForeground(IntPtr hwnd)
            {
                AttachedThreadInputAction(
                    () =>
                    {
                        BringWindowToTop(hwnd);
                        ShowWindow(hwnd, SW_SHOW);
                    });
            }

            public static IntPtr SetFocusAttached(IntPtr hWnd)
            {
                var result = new IntPtr();
                AttachedThreadInputAction(
                    () =>
                    {
                        result = SetFocus(hWnd);
                    });
                return result;
            }





            [System.Runtime.InteropServices.DllImport("user32.dll")]
            static extern bool ClipCursor(ref RECT lpRect);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetCursorPos(int x, int y);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetCursorPos(out POINT lpPoint);

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;

                public static implicit operator Point(POINT point)
                {
                    return new Point(point.X, point.Y);
                }
            }

        }

    }
}
