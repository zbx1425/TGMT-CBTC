using BveTypes.ClassWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGMT_CBTC.OBCU {
    public class CATSTargets {
        // This is not present in an actual TGMT system!

        private SectionManager SectionManager { get; set; }
        private double SpeedOffset { get; set; }
        public List<SpeedTarget> Targets { get; set; } = new List<SpeedTarget>();

        public CATSTargets(Scenario scenario, double speedOffset) {
            SectionManager = scenario.SectionManager;
            SpeedOffset = speedOffset;
            Targets.Add(SpeedTarget.Empty());
            Targets.Add(SpeedTarget.Empty());
        }

        public void Tick(Train train) {
            if (SectionManager.Sections.CurrentIndex >= 0 && SectionManager.Sections.CurrentIndex < SectionManager.Sections.Count) {
                Targets[0] = SpeedTarget.LineSpeed(
                    SectionManager.Sections[SectionManager.Sections.CurrentIndex].Location,
                    SectionManager.CurrentSectionSpeedLimit * Units.MPS_TO_KMH + SpeedOffset);
            } else {
                Targets[0] = SpeedTarget.Empty();
            }
            if (SectionManager.Sections.CurrentIndex >= -1 && SectionManager.Sections.CurrentIndex < SectionManager.Sections.Count - 1) {
                Targets[1] = SpeedTarget.LineSpeed(
                    SectionManager.Sections[SectionManager.Sections.CurrentIndex + 1].Location,
                    SectionManager.ForwardSectionSpeedLimit * Units.MPS_TO_KMH + SpeedOffset);
            } else {
                Targets[1] = SpeedTarget.Empty();
            }
        }
    }
}
