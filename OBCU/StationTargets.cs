using BveTypes.ClassWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGMT_CBTC.OBCU {
    public class StationTargets {

        private StationList Stations { get; set; }

        private int NextStationIndex { get; set; } = 0;

        public List<SpeedTarget> Targets { get; private set; } = new List<SpeedTarget>();

        public StationTargets(Scenario scenario) {
            Stations = scenario.Route.Stations;
            Targets.Add(SpeedTarget.Empty());
        }

        public void Tick(Train train) {
            if (Stations.CurrentIndex >= -1 && Stations.CurrentIndex + 1 < NextStationIndex) {
               NextStationIndex = Stations.CurrentIndex + 1;
            }
            while (NextStationIndex < Stations.Count 
                && (Stations[NextStationIndex].Location + 100 < train.Location
                || ((Station)Stations[NextStationIndex]).Pass)) {
                NextStationIndex++;
            }
            if (Math.Abs(train.Location - Stations[NextStationIndex].Location) < 10
                && !train.DoorClosed) {
                NextStationIndex++;
            }
            if (NextStationIndex >= Stations.Count) {
                Targets[0] = SpeedTarget.Empty();
            } else {
                Targets[0] = SpeedTarget.StopPoint(Stations[NextStationIndex].Location);
            }
        }
    }
}
