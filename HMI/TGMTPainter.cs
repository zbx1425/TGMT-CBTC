using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using TGMT_CBTC.OBCU;

namespace TGMT_CBTC.HMI {

    public static class TGMTPainter {

        public static GDIHelper hHMI, hTDT;

        public static void Initialize() {
            var imgDir = "C:\\Users\\zbx1425\\Documents\\BveTs\\Scenarios\\zbx1425\\TGMT";
            
            hHMI = new GDIHelper(1024, 1024);
            hTDT = new GDIHelper(256, 256);

            hmi = new Bitmap(Path.Combine(imgDir, "hmi.png"));
            ackcmd = new Bitmap(Path.Combine(imgDir, "ackcmd.png"));
            atoctrl = new Bitmap(Path.Combine(imgDir, "atoctrl.png"));
            dormode = new Bitmap(Path.Combine(imgDir, "dormode.png"));
            dorrel = new Bitmap(Path.Combine(imgDir, "dorrel.png"));
            drvmode = new Bitmap(Path.Combine(imgDir, "drvmode.png"));
            emergency = new Bitmap(Path.Combine(imgDir, "emergency.png"));
            fault = new Bitmap(Path.Combine(imgDir, "fault.png"));
            selmode = new Bitmap(Path.Combine(imgDir, "selmode.png"));
            sigmode = new Bitmap(Path.Combine(imgDir, "sigmode.png"));
            special = new Bitmap(Path.Combine(imgDir, "special.png"));
            stopsig = new Bitmap(Path.Combine(imgDir, "stopsig.png"));
            departure = new Bitmap(Path.Combine(imgDir, "departure.png"));
            menu = new Bitmap(Path.Combine(imgDir, "menu.png"));

            num0 = new Bitmap(Path.Combine(imgDir, "num0.png"));
            numn0 = new Bitmap(Path.Combine(imgDir, "num-0.png"));
            colon = new Bitmap(Path.Combine(imgDir, "colon.png"));

            tdtbackoff = new Bitmap(Path.Combine(imgDir, "tdt_back_off.png"));
            tdtbackred = new Bitmap(Path.Combine(imgDir, "tdt_back_red.png"));
            tdtbackgreen = new Bitmap(Path.Combine(imgDir, "tdt_back_green.png"));
            tdtdigitsred = Image.FromFile(Path.Combine(imgDir, "tdt_digits_red.png"));
            tdtdigitsgreen = Image.FromFile(Path.Combine(imgDir, "tdt_digits_green.png"));
        }

        public static void Dispose() {
            hHMI.Dispose();
            hTDT.Dispose();
        }

        public static GDIHelper PaintHMI(OBCU.OBCU obcu) {
            int[] panel = new int[256];
            hHMI.BeginGDI();
            hHMI.DrawImage(hmi, 0, 0);

            hHMI.DrawImage(menu, 700, 60, (obcu.Train.Speed == 0 ? 0 : 1) * 60, 60);
            hHMI.DrawImage(drvmode, 589, 133, obcu.DriveMode.DisplayIndex() * 50, 50);
            hHMI.DrawImage(sigmode, 686, 133, 2 * 50, 50);
            hHMI.DrawImage(stopsig, 686, 200, panel[26] * 50, 50);
            hHMI.DrawImage(dorrel, 589, 267, panel[27] * 50, 50);
            hHMI.DrawImage(dormode, 589, 337, 1 * 50, 50);
            hHMI.DrawImage(departure, 686, 267, panel[32] * 50, 50);
            hHMI.DrawImage(emergency, 686, 337, panel[29] * 50, 50);
            hHMI.DrawImage(fault, 589, 405, panel[30] * 50, 50);
            hHMI.DrawImage(special, 686, 405, panel[31] * 50, 50);
            hHMI.DrawImage(ackcmd, 490, 472, panel[35] * 100, 100);
            hHMI.DrawImage(atoctrl, 32, 405, panel[21] * 50, 50);
            hHMI.DrawImage(selmode, 150, 405, 4 * 50, 50);

            if (obcu.TargetSpeed.HasValue) {
                hHMI.DrawImage(num0, 64, 120, D((int)Math.Round(obcu.TargetSpeed.Value), 0) * 18, 18);
                hHMI.DrawImage(numn0, 50, 120, D((int)Math.Round(obcu.TargetSpeed.Value), 1) * 18, 18);
                hHMI.DrawImage(numn0, 36, 120, D((int)Math.Round(obcu.TargetSpeed.Value), 2) * 18, 18);
            }
            hHMI.DrawImage(num0, 289, 212, D((int)(obcu.Train.Speed), 0) * 18, 18);
            hHMI.DrawImage(numn0, 275, 212, D((int)(obcu.Train.Speed), 1) * 18, 18);

            var sec = obcu.Train.Time / 1000 % 60;
            var min = obcu.Train.Time / 1000 / 60 % 60;
            var hrs = obcu.Train.Time / 1000 / 3600 % 60;
            hHMI.DrawImage(num0, 186, 552, D(hrs, 1) * 18, 18);
            hHMI.DrawImage(num0, 200, 552, D(hrs, 0) * 18, 18);
            hHMI.DrawImage(num0, 228, 552, D(min, 1) * 18, 18);
            hHMI.DrawImage(num0, 242, 552, D(min, 0) * 18, 18);
            hHMI.DrawImage(num0, 270, 552, D(sec, 1) * 18, 18);
            hHMI.DrawImage(num0, 284, 552, D(sec, 0) * 18, 18);
            if (sec % 2 == 0) {
                hHMI.DrawImage(colon, 214, 552);
                hHMI.DrawImage(colon, 256, 552);
            }

            if ((obcu.AtpExceedRcmd && obcu.DriveMode == DriveMode.SM) || obcu.AtpEmergency) {
                hHMI.FillRectWH(obcu.AtpEmergency ? Color.Red : Color.Orange, 20, 18, 80, 78);
            }
            if (obcu.TargetLocation.HasValue) {
                double distance = obcu.TargetLocation.Value - obcu.Train.Location;
                const double topHeight = 356 - 154;
                const double topDist = 750;
                double height = distance > 1 
                    ? (Math.Min(Math.Log10(distance) / Math.Log10(topDist), 1) * topHeight)
                    : 0;
                Color targetColor = Color.Green;
                if (distance < 150) {
                    if (obcu.TargetSpeed.Value == 0) {
                        targetColor = Color.Red;
                    } else if (obcu.TargetSpeed.Value < 60) {
                        targetColor = Color.Orange;
                    }
                } else if (distance < 300) {
                    if (obcu.TargetSpeed.Value < 25) {
                        targetColor = Color.Orange;
                    }
                }
                hHMI.FillRectWH(targetColor, 68, 356 - (int)height, 10, (int)height);
            }
            hHMI.EndGDI();

            if (panel[36] != 0 && DateTime.Now.Millisecond % 500 < 250) {
                hHMI.Graphics.DrawRectangle(ackPen, new Rectangle(488, 470, 280, 100));
            }

            Font debugFont = new Font("Consolas", 10, FontStyle.Bold);
            hHMI.Graphics.DrawString("Acmd=" + obcu.ATO.CommandAccel.ToString("0.00"), debugFont, Brushes.Orange, 32, 395);
            hHMI.Graphics.DrawString("Ncmd=" + obcu.ATO.CommandNotch.ToString(), debugFont, Brushes.Orange, 32, 415);

            var tSpeed = (obcu.Train.Speed / obcu.Train.MaxSpeed * 288 - 144) / 180 * Math.PI;
            hHMI.Graphics.DrawEllipse(circlePen, new Rectangle(255, 188, 66, 66));
            hHMI.Graphics.DrawLine(needlePen, Poc(288, 221, 33, 0, tSpeed), Poc(288, 221, 125, 0, tSpeed));
            hHMI.Graphics.FillPolygon(Brushes.White, new Point[] {
                Poc(288, 221, 163, 0, tSpeed), Poc(288, 221, 123, -5, tSpeed), Poc(288, 221, 123, 5, tSpeed)
            });
            // hHMI.Graphics.DrawArc(ebSpeedPen, 288 - 175, 221 - 175, 175 * 2, 175 * 2, (float)panel[16] / 480 * 288 - 144 - 90, 288 - (float)panel[16] / 480 * 288);
            if (obcu.DriveMode == DriveMode.SM || true) {
                var tRecommend = (obcu.YellowSpeed / obcu.Train.MaxSpeed * 288 - 144) / 180 * Math.PI;
                hHMI.Graphics.FillPolygon(Brushes.Yellow, new Point[] {
                    Poc(288, 221, 165, 0, tRecommend), Poc(288, 221, 185, -11, tRecommend), Poc(288, 221, 185, 11, tRecommend)
                });
            }
            if (panel[16] >= 0) {
                var tLimit = (obcu.RedSpeed / obcu.Train.MaxSpeed * 288 - 144) / 180 * Math.PI;
                hHMI.Graphics.FillPolygon(Brushes.Red, new Point[] {
                    Poc(288, 221, 165, 0, tLimit), Poc(288, 221, 185, -11, tLimit), Poc(288, 221, 185, 11, tLimit)
                });
            }
            return hHMI;
        }

        static int last101 = 0, last102 = 0;

        public static GDIHelper PaintTDT(int[] panel, Train state) {
            if (panel[101] == last101 && panel[102] == last102) return null;
            hTDT.BeginGDI();
            Image digitImage;
            if (panel[102] == -1) {
                hTDT.DrawImage(tdtbackred, 0, 0);
                digitImage = tdtdigitsred;
            } else if (panel[102] == 1) {
                hTDT.DrawImage(tdtbackgreen, 0, 0);
                digitImage = tdtdigitsgreen;
            } else {
                hTDT.DrawImage(tdtbackoff, 0, 0);
                digitImage = null;
            }
            hTDT.EndGDI();
            if (digitImage != null) {
                for (int i = 0; i <= 2; i++) {
                    var xpos = 152 - 55 * i;
                    hTDT.Graphics.SetClip(new Rectangle(xpos, 67, 60, 120));
                    var di = D(panel[101], i);
                    if (di == 10) di = 0;
                    hTDT.Graphics.DrawImageUnscaled(digitImage, xpos, 67 - 120 * di);
                }
            }

            last101 = panel[101]; last102 = panel[102];
            return hTDT;
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
        static Pen ebSpeedPen = new Pen(Color.Red, 5);
        static Pen ackPen = new Pen(Color.Yellow, 4);
        static Color[] targetColor = new Color[] { Color.Red, Color.Orange, Color.Green };
        static Color[] overspeed = new Color[] { Color.Black, Color.Orange, Color.Red };
        static Bitmap hmi, ackcmd, atoctrl, dormode, dorrel, drvmode, emergency, fault, departure, menu,
            selmode, sigmode, special, stopsig, num0, numn0, colon;
        static Bitmap tdtbackoff, tdtbackred, tdtbackgreen;
        static Image tdtdigitsred, tdtdigitsgreen;
    }
}
