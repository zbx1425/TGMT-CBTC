using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGMT_CBTC.OBCU {
    public enum DriveMode {
        RM,
        SM,
        AMInterrupted,
        AM,
        XAM
    }

    static class DriveModeID {

        private static int[] mapping = new int[] { 0, 1, 2, 2, 3 };

        public static int DisplayIndex(this DriveMode driveMode) {
            return mapping[(int)driveMode];
        }
    }
}
