using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TGMTAts {

    public static class TGMTPainter {

        public static Bitmap bmp800, bmp1024, bmptdt;
        public static Graphics g800, g1024, gtdt;

        public static void Initialize() {
            var imgDir = Config.ImageAssetPath;

            bmp800 = new Bitmap(800, 600, PixelFormat.Format32bppRgb);
            g800 = Graphics.FromImage(bmp800);
            bmp1024 = new Bitmap(1024, 1024, PixelFormat.Format32bppRgb);
            g1024 = Graphics.FromImage(bmp1024);
            GDI32.BindGraphics(g800);

            bmptdt = new Bitmap(32, 32, PixelFormat.Format32bppRgb);
            gtdt = Graphics.FromImage(bmptdt);

            hmi = GDI32.LoadImage(Path.Combine(imgDir, "hmi.png"));
            ackcmd = GDI32.LoadImage(Path.Combine(imgDir, "ackcmd.png"));
            atoctrl = GDI32.LoadImage(Path.Combine(imgDir, "atoctrl.png"));
            dormode = GDI32.LoadImage(Path.Combine(imgDir, "dormode.png"));
            dorrel = GDI32.LoadImage(Path.Combine(imgDir, "dorrel.png"));
            drvmode = GDI32.LoadImage(Path.Combine(imgDir, "drvmode.png"));
            emergency = GDI32.LoadImage(Path.Combine(imgDir, "emergency.png"));
            fault = GDI32.LoadImage(Path.Combine(imgDir, "fault.png"));
            selmode = GDI32.LoadImage(Path.Combine(imgDir, "selmode.png"));
            sigmode = GDI32.LoadImage(Path.Combine(imgDir, "sigmode.png"));
            special = GDI32.LoadImage(Path.Combine(imgDir, "special.png"));
            stopsig = GDI32.LoadImage(Path.Combine(imgDir, "stopsig.png"));
            departure = GDI32.LoadImage(Path.Combine(imgDir, "departure.png"));
            menu = GDI32.LoadImage(Path.Combine(imgDir, "menu.png"));

            num0 = GDI32.LoadImage(Path.Combine(imgDir, "num0.png"));
            numn0 = GDI32.LoadImage(Path.Combine(imgDir, "num-0.png"));
            colon = GDI32.LoadImage(Path.Combine(imgDir, "colon.png"));
        }

        public static void Dispose() {
            GDI32.FreeImage();
            GDI32.FreeGraphics(g800);
        }
        
        public static Bitmap PaintHMI(TGMTAts.AtsIoArray panel, TGMTAts.AtsVehicleState state) {
            GDI32.DrawImage(hmi, 0, 0);

            GDI32.DrawImage(menu, 700, 60, panel[23] * 60, 60);
            GDI32.DrawImage(drvmode, 589, 133, panel[24] * 50, 50);
            GDI32.DrawImage(sigmode, 686, 133, panel[25] * 50, 50);
            GDI32.DrawImage(stopsig, 686, 200, panel[26] * 50, 50);
            GDI32.DrawImage(dorrel, 589, 267, panel[27] * 50, 50);
            GDI32.DrawImage(dormode, 589, 337, panel[28] * 50, 50);
            GDI32.DrawImage(departure, 686, 267, panel[32] * 50, 50);
            GDI32.DrawImage(emergency, 686, 337, panel[29] * 50, 50);
            GDI32.DrawImage(fault, 589, 405, panel[30] * 50, 50);
            GDI32.DrawImage(special, 686, 405, panel[31] * 50, 50);
            GDI32.DrawImage(ackcmd, 490, 472, panel[35] * 100, 100);
            GDI32.DrawImage(atoctrl, 32, 405, panel[21] * 50, 50);
            GDI32.DrawImage(selmode, 150, 405, panel[22] * 50, 50);

            if (panel[18] == 0) {
                GDI32.DrawImage(num0, 64, 120, D(panel[17], 0) * 18, 18);
                GDI32.DrawImage(numn0, 50, 120, D(panel[17], 1) * 18, 18);
                GDI32.DrawImage(numn0, 36, 120, D(panel[17], 2) * 18, 18);
            }
            GDI32.DrawImage(num0, 289, 212, D((int)state.Speed, 0) * 18, 18);
            GDI32.DrawImage(numn0, 275, 212, D((int)state.Speed, 1) * 18, 18);

            var sec = state.Time / 1000 % 60;
            var min = state.Time / 1000 / 60 % 60;
            var hrs = state.Time / 1000 / 3600 % 60;
            GDI32.DrawImage(num0, 186, 552, D(hrs, 1) * 18, 18);
            GDI32.DrawImage(num0, 200, 552, D(hrs, 0) * 18, 18);
            GDI32.DrawImage(num0, 228, 552, D(min, 1) * 18, 18);
            GDI32.DrawImage(num0, 242, 552, D(min, 0) * 18, 18);
            GDI32.DrawImage(num0, 270, 552, D(sec, 1) * 18, 18);
            GDI32.DrawImage(num0, 284, 552, D(sec, 0) * 18, 18);
            if (sec % 2 == 0) {
                GDI32.DrawImage(colon, 214, 552);
                GDI32.DrawImage(colon, 256, 552);
            }

            g800.FillRectangle(overspeed[panel[10]], new Rectangle(20, 18, 80, 78));
            g800.FillRectangle(targetColor[panel[13] * 1 + panel[14] * 2], new Rectangle(68, 354 - panel[11], 10, panel[11]));
            if (panel[36] != 0 && TGMTAts.time % 500 < 250) {
                g800.DrawRectangle(ackPen, new Rectangle(488, 470, 280, 100));
            }

            var tSpeed = ((double)panel[1] / 400 * 288 - 144) / 180 * Math.PI;
            g800.DrawEllipse(circlePen, new Rectangle(255, 188, 66, 66));
            g800.DrawLine(needlePen, Poc(288, 221, 33, 0, tSpeed), Poc(288, 221, 125, 0, tSpeed));
            g800.FillPolygon(Brushes.White, new Point[] {
                Poc(288, 221, 163, 0, tSpeed), Poc(288, 221, 123, -5, tSpeed), Poc(288, 221, 123, 5, tSpeed)
            });
            if (panel[15] >= 0) {
                var tRecommend = ((double)panel[15] / 400 * 288 - 144) / 180 * Math.PI;
                g800.FillPolygon(Brushes.Yellow, new Point[] {
                    Poc(288, 221, 165, 0, tRecommend), Poc(288, 221, 185, -11, tRecommend), Poc(288, 221, 185, 11, tRecommend)
                });
            }
            if (panel[16] >= 0) {
                var tLimit = ((double)panel[16] / 400 * 288 - 144) / 180 * Math.PI;
                g800.FillPolygon(Brushes.Red, new Point[] {
                    Poc(288, 221, 165, 0, tLimit), Poc(288, 221, 185, -11, tLimit), Poc(288, 221, 185, 11, tLimit)
                });
            }

            g1024.DrawImageUnscaled(bmp800, 0, 0);
            return bmp1024;
        }

        public static Bitmap PaintTDT(TGMTAts.AtsIoArray panel, TGMTAts.AtsVehicleState state) {
            gtdt.Clear(Color.White);
            double dist = Math.Abs(StationManager.NextStation.StopPosition - state.Location);
            string str;
            if (dist > 100) {
                str = "---";
            } else if (dist > 1) {
                str = Math.Round(dist).ToString() + "m";
            } else {
                str = Math.Round(dist * 100).ToString() + "cm";
            }
            gtdt.DrawString(str, new Font("Arial", 12, GraphicsUnit.Pixel), Brushes.Red, 0, 0);
            return bmptdt;
        }

        static int[] pow10 = new int[] { 1, 10, 100, 1000 };

        static int D(int src, int digit) {
            if (pow10[digit] > src) {
                return 10;
            } else if (digit == 0 && src == 0) {
                return 0;
            } else {
                return src / pow10[digit] % 10;
            }
        }

        static Point Poc(int cx, int cy, int dr, int dt, double theta) {
            return new Point(
                (int)(cx + dr * Math.Sin(theta) + dt * Math.Cos(theta)),
                (int)(cy - dr * Math.Cos(theta) + dt * Math.Sin(theta))
            );
        }

        static Pen needlePen = new Pen(Color.White, 10);
        static Pen circlePen = new Pen(Color.White, 5);
        static Pen ackPen = new Pen(Color.Yellow, 4);
        static Brush[] targetColor = new Brush[] { new SolidBrush(Color.Red), new SolidBrush(Color.Orange), new SolidBrush(Color.Green) };
        static Brush[] overspeed = new Brush[] { new SolidBrush(Color.Black), new SolidBrush(Color.Orange), new SolidBrush(Color.Red) };
        static int hmi, ackcmd, atoctrl, dormode, dorrel, drvmode, emergency, fault, departure, menu,
            selmode, sigmode, special, stopsig, num0, numn0, colon;
    }
}
