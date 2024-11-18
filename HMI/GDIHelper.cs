using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TGMT_CBTC.HMI {

    public class GDIHelper : IDisposable {

        [DllImport("gdi32")]
        internal static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);
        [DllImport("gdi32")]
        internal static extern int SelectObject([In] IntPtr hdc, [In] IntPtr hObject);
        [DllImport("gdi32")]
        internal static extern int DeleteDC([In] IntPtr hdc);
        [DllImport("gdi32")]
        internal static extern int DeleteObject([In] IntPtr hObject);
        [DllImport("gdi32")]
        internal static extern int BitBlt([In] IntPtr hDestDC, int x, int y, int nWidth, int nHeight,
            [In] IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
        internal const int SRCCOPY = 0xCC0020;

        [DllImport("gdi32")]
        internal static extern int Rectangle([In] IntPtr hdc, int X1, int Y1, int X2, int Y2);
        [DllImport("gdi32")]
        internal static extern IntPtr CreateSolidBrush(int crColor);

        public struct HBitmap {
            public Bitmap img;
            public IntPtr ptr;

            public HBitmap(Bitmap rimg) {
                img = rimg;
                ptr = img.GetHbitmap();
            }
        }

        private Dictionary<Bitmap, IntPtr> Images = new Dictionary<Bitmap, IntPtr>();
        private Dictionary<Color, IntPtr> Brushes = new Dictionary<Color, IntPtr>();
        public Graphics Graphics;

        public Bitmap Bitmap;

        public GDIHelper(Graphics g) {
            this.Graphics = g;
        }

        public GDIHelper(int width, int height) {
            this.Bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            this.Graphics = Graphics.FromImage(this.Bitmap);
            this.Graphics.Clear(Color.Transparent);
        }

        private IntPtr hDC;
        private IntPtr hMemDC;

        public void BeginGDI() {
            if (hDC != IntPtr.Zero) return;
            hDC = Graphics.GetHdc();
            hMemDC = CreateCompatibleDC(hDC);
        }

        public void EndGDI() {
            if (hDC == IntPtr.Zero) return;
            DeleteDC(hMemDC);
            Graphics.ReleaseHdc();
            hDC = IntPtr.Zero;
        }

        public bool HasAcquiredHDC() {
            return hDC != IntPtr.Zero;
        }

        public void DrawImage(Bitmap img, int x, int y) {
            if (hDC == IntPtr.Zero) throw new InvalidOperationException("You must call BeginGDI() first!");
            SelectObject(hMemDC, GetHBitmap(img));
            BitBlt(hDC, x, y, img.Width, img.Height, hMemDC, 0, 0, SRCCOPY);
        }

        public void DrawImage(Bitmap img, int x, int y, int sy, int h) {
            if (hDC == IntPtr.Zero) throw new InvalidOperationException("You must call BeginGDI() first!");
            SelectObject(hMemDC, GetHBitmap(img));
            BitBlt(hDC, x, y, img.Width, h, hMemDC, 0, sy, SRCCOPY);
        }

        public void FillRect12(Color color, int x1, int y1, int x2, int y2) {
            if (hDC == IntPtr.Zero) throw new InvalidOperationException("You must call BeginGDI() first!");
            SelectObject(hDC, GetSolidBrush(color));
            Rectangle(hDC, x1, y1, x2, y2);
        }

        public void FillRectWH(Color color, int x1, int y1, int w, int h) {
            if (hDC == IntPtr.Zero) throw new InvalidOperationException("You must call BeginGDI() first!");
            SelectObject(hDC, GetSolidBrush(color));
            Rectangle(hDC, x1, y1, x1 + w, y1 + h);
        }

        private IntPtr GetHBitmap(Bitmap image) {
            new DateTime(152).ToShortTimeString();
            if (!Images.ContainsKey(image)) Images.Add(image, image.GetHbitmap());
            return Images[image];
        }

        private IntPtr GetSolidBrush(Color color) {
            if (!Brushes.ContainsKey(color)) {
                Brushes.Add(color, CreateSolidBrush(color.R | color.G << 8 | color.B << 16));
            }
            return Brushes[color];
        }

        public void Dispose() {
            EndGDI();
            if (this.Bitmap != null) this.Bitmap.Dispose();
            foreach (var pair in Images) DeleteObject(pair.Value);
            Images.Clear();
            foreach (var pair in Brushes) DeleteObject(pair.Value);
            Brushes.Clear();
        }
    }
}