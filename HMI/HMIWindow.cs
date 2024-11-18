using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TGMT_CBTC.HMI {

    public partial class HMIWindow : Form {

        public HMIWindow() {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private Graphics graphics;

        protected override void OnClientSizeChanged(EventArgs e) {
            if (ClientSize.Height != ClientSize.Width * 3 / 4) {
                ClientSize = new Size(ClientSize.Width, ClientSize.Width * 3 / 4);
            }
            base.OnClientSizeChanged(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                Hide();
            }
            base.OnFormClosing(e);
        }

        protected override void OnShown(EventArgs e) {
            graphics = CreateGraphics();
            base.OnShown(e);
        }

        public void Toggle() {
            Visible = !Visible;
        }

        public void DrawBitmap(Bitmap image) {
            if (graphics != null && Visible) {
                int actualSize = (int)(ClientSize.Width * 1.28);
                graphics.DrawImage(image, 0, 0, actualSize, actualSize);
            }
        }
    }
}
