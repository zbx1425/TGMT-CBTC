using BveEx.PluginHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TGMT_CBTC {
    public class KeyHandler {

        public event EventHandler AtoStartPressed;

        public void Init(IBveHacker bveHacker) {
            bveHacker.MainFormSource.KeyDown += OnMainFormKeyDown;
            bveHacker.MainFormSource.KeyUp += OnMainFormKeyUp;
        }

        private bool KeyKDown = false, KeyLDown = false;

        private void OnMainFormKeyDown(object sender, System.Windows.Forms.KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.K:
                    KeyKDown = true;
                    if (KeyKDown && KeyLDown) AtoStartPressed?.Invoke(this, EventArgs.Empty);
                    break;
                case Keys.L:
                    KeyLDown = true;
                    if (KeyKDown && KeyLDown) AtoStartPressed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        private void OnMainFormKeyUp(object sender, System.Windows.Forms.KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.K:
                    KeyKDown = false;
                    break;
                case Keys.L:
                    KeyLDown = false;
                    break;
            }
        }
    }
}
