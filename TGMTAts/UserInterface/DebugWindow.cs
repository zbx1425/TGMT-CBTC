using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;

using System.Windows.Forms;

namespace TGMTAts {
    public partial class DebugWindow : Form {

        public DebugWindow() {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void timer1_Tick(object sender, EventArgs e) {
            if (!TGMTAts.pluginReady) return;
            var sb = new StringBuilder();
            sb.AppendLine("TGMTAts Beta for BVE5/6 1.2 by zbx1425 2021-7-12 https://www.zbx1425.cn");
            sb.AppendFormat("车位     : {0}\n", D(TGMTAts.location));
            if (TGMTAts.nextLimit == null) {
                sb.AppendLine("下一限速 : 无");
            } else {
                sb.AppendFormat("下一限速 : {0,6} {1,-4}\n", D(TGMTAts.nextLimit.Location), D(TGMTAts.nextLimit.Limit));
            }
            sb.AppendLine();
            sb.AppendFormat("授权终点 : {0,6}\n", D(TGMTAts.movementEndpoint.Location));
            var pretrain = PreTrainManager.GetEndpoint();
            sb.AppendFormat("前车信息 : {0,6} {1,-4}\n", D(pretrain.Location), D(pretrain.Limit));
            sb.AppendFormat("车站     : {0,6} {1} {2}\n",
                D(StationManager.NextStation.StopPosition), 
                StationManager.Arrived ? "站内停止" : "",
                StationManager.Arrived ? "已到达" : ""
            );
            sb.AppendFormat("线路限速 : {0,6} {1,-4} -> [{2,6} {3,-4}] -> {4,6} {5,-4}\n",
                D(TGMTAts.trackLimit.last.Location), D(TGMTAts.trackLimit.last.Limit),
                D(TGMTAts.trackLimit.current.Location), D(TGMTAts.trackLimit.current.Limit),
                D(TGMTAts.trackLimit.next.Location), D(TGMTAts.trackLimit.next.Limit)
            );
            foreach (var limit in TGMTAts.trackLimit.trackLimits) {
                sb.AppendFormat("{0,6}: {1,-2} ;", D(limit.Location), D(limit.Limit));
            }
            sb.AppendLine();
            sb.AppendFormat("ATO 级位 : {0}\n", Ato.outputNotch);
            sb.AppendLine();

            foreach (var msg in TGMTAts.debugMessages.Skip(Math.Max(0, TGMTAts.debugMessages.Count() - 8))) {
                sb.AppendLine(msg);
            }
            //sb.AppendFormat("Signal  : {0,6} {1,-4}\n", D(TGMTAts.signalLimit.Location), D(TGMTAts.signalLimit.Limit));
            //sb.AppendFormat(" -Raw5  : {0,3}", TGMTAts.signalLimit.updateCount);
            /*foreach (var sd in TGMTAts.signalLimit.sgdata.Take(5)) {
                sb.AppendFormat("{0,6}: {1} ", D(sd.Distance), D(sd.Aspect));
            }*/
            sb.AppendLine();
            label1.Text = sb.ToString();
        }

        private string D(double d) {
            if (d <= -Config.LessInf) return "-Inf";
            if (d >= Config.LessInf) return "Inf";
            return ((int)d).ToString();
        }

        private string D(int d) {
            if (d <= -Config.LessInf) return "-Inf";
            if (d >= Config.LessInf) return "Inf";
            return d.ToString();
        }
    }
}
