using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace TGMTAts {

    static class GDI32 {
        // Rectangle
        [DllImport("gdi32")]
        private static extern int Rectangle([In] IntPtr hdc, int X1, int Y1, int X2, int Y2);
        [DllImport("gdi32")]
        private static extern int CreateSolidBrush(int crColor);

        // 
        [DllImport("gdi32")]
        private static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);
        [DllImport("gdi32")]
        private static extern int SelectObject([In] IntPtr hdc, [In] IntPtr hObject);
        [DllImport("gdi32")]
        private static extern int DeleteDC([In] IntPtr hdc);
        [DllImport("gdi32")]
        private static extern int DeleteObject([In] IntPtr hObject);
        [DllImport("gdi32")]
        public static extern int BitBlt([In] IntPtr hDestDC, int x, int y, int nWidth, int nHeight,
            [In] IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
        private const int SRCCOPY = 0xCC0020; // (DWORD) dest = source
        private const int SRCAND = 0x8800C6;  // (DWORD) dest = source AND dest
        private const int SRCINVERT = 0x660046;       // (DWORD) dest = source XOR dest

        // Text
        [DllImport("gdi32")]
        private static extern int SetTextColor([In] IntPtr hdc, int crColor);
        [DllImport("gdi32")]
        private static extern long SetTextAlign([In] IntPtr hdc, int wFlags);
        [DllImport("gdi32")]
        private static extern int SetBkColor([In] IntPtr hdc, int crColor);
        [DllImport("gdi32")]
        private static extern int SetBkMode([In] IntPtr hdc, int nBkMode);
        [DllImport("gdi32")]
        private static extern int TextOut([In] IntPtr hdc, int x, int y, string lpString, int nCount);

        private const int DEFAULT_CHARSET = 1;
        private const int OUT_CHARACTER_PRECIS = 2;
        private const int CLIP_CHARACTER_PRECIS = 1;
        private const int DEFAULT_QUALITY = 0;
        private const int FF_DONTCARE = 0;    // Don't care or don't know.
        private const int TRANSPARENT = 1;
        private const int TA_LEFT = 0;
        private const int TA_RIGHT = 2;

        // Clip
        [DllImport("gdi32")]
        private static extern int SelectClipRgn([In] IntPtr hdc, int hRgn);
        [DllImport("gdi32")]
        private static extern int CreateRectRgn(int X1, int Y1, int X2, int Y2);

        public struct PreStoragedHBitmap {
            public Bitmap img;
            public IntPtr ptr;

            public PreStoragedHBitmap(Bitmap rimg) {
                img = rimg;
                ptr = img.GetHbitmap();
            }
        }

        public static List<PreStoragedHBitmap> Images = new List<PreStoragedHBitmap>();
        public static List<int> Fonts = new List<int>();

        public static Graphics g;

        public static void BindGraphics(Graphics graphics) {
            g = graphics;
        }

        public static void FreeGraphics(Graphics g) {

        }

        public static int LoadImage(string path) {
            var img = new Bitmap(path);
            Images.Add(new PreStoragedHBitmap(img));
            return Images.Count - 1;
        }

        public static int LoadImage(Bitmap Image) {
            Images.Add(new PreStoragedHBitmap(Image));
            return Images.Count - 1;
        }

        public static int LoadImage(Color col, int maskid) {
            Bitmap img = new Bitmap(Images[maskid].img.Width, Images[maskid].img.Height);
            Graphics grap = Graphics.FromImage(img);
            grap.FillRectangle(new SolidBrush(col), new Rectangle(0, 0, img.Width, img.Height));
            Images.Add(new PreStoragedHBitmap(img));
            return Images.Count - 1;
        }

        public static void DrawImage(int imgid, int x, int y) {
            IntPtr hdcPtr = g.GetHdc();
            PreStoragedHBitmap buf = Images[imgid];
            IntPtr bmpptr = buf.ptr;
            IntPtr memdcPtr = CreateCompatibleDC(hdcPtr);
            SelectObject(memdcPtr, bmpptr);
            BitBlt(hdcPtr, x, y, buf.img.Width, buf.img.Height, memdcPtr, 0, 0, SRCCOPY);
            DeleteDC(memdcPtr);
            g.ReleaseHdc();
        }

        public static void DrawImage(int imgid, int x, int y, int sy, int h) {
            IntPtr hdcPtr = g.GetHdc();
            PreStoragedHBitmap buf = Images[imgid];
            IntPtr bmpptr = buf.ptr;
            IntPtr memdcPtr = CreateCompatibleDC(hdcPtr);
            SelectObject(memdcPtr, bmpptr);
            BitBlt(hdcPtr, x, y, buf.img.Width, h, memdcPtr, 0, sy, SRCCOPY);
            DeleteDC(memdcPtr);
            g.ReleaseHdc();
        }

        /*public static void DrawMaskImage(int imgidtgt, int imgidmsk, int x, int y) {
            PreStoragedHBitmap TargetBuf = Images[imgidtgt];
            PreStoragedHBitmap MaskBuf = Images[imgidmsk];
            IntPtr tgtmemdcPtr = CreateCompatibleDC(hdcPtr);
            IntPtr mskmemdcPtr = CreateCompatibleDC(hdcPtr);
            SelectObject(tgtmemdcPtr, TargetBuf.ptr);
            SelectObject(mskmemdcPtr, MaskBuf.ptr);
            (hdcPtr, x, y, TargetBuf.img.Width, TargetBuf.img.Height, tgtmemdcPtr, 0, 0, SRCINVERT);
            (hdcPtr, x, y, MaskBuf.img.Width, MaskBuf.img.Height, mskmemdcPtr, 0, 0, SRCAND);
            (hdcPtr, x, y, TargetBuf.img.Width, TargetBuf.img.Height, tgtmemdcPtr, 0, 0, SRCINVERT);
            DeleteDC(tgtmemdcPtr);
            DeleteDC(mskmemdcPtr);
        }*/

        public static void FreeImage() {
            foreach (PreStoragedHBitmap Current in Images)
                DeleteObject(Current.ptr);
        }
    }

}