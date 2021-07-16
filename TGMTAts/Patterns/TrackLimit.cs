using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TGMTAts {
    public class TrackLimit : SpeedLimit {
        public List<SpeedLimit> trackLimits = new List<SpeedLimit>();
        public SpeedLimit previous = SpeedLimit.inf;
        public SpeedLimit next = SpeedLimit.inf;

        public void SetBeacon(TGMTAts.AtsBeaconData data) {
            if (data.Type == -16777214) {
                double limit = (double)(data.Optional & 4095);
                int position = (data.Optional >> 12);
                var item = new SpeedLimit(limit, position);
                if (!this.trackLimits.Contains(item)) {
                    this.trackLimits.Add(item);
                }
                //adm(item.Location.ToString()+" LIMIT "+limit.ToString(),OpenBveApi.Colors.MessageColor.White,2000);
            } else if (data.Type == 906688010) {
                if (trackLimits.Any(s => s.Location == data.Optional)) return;
                this.trackLimits.Add(new SpeedLimit(0, data.Optional));
            } else if (data.Type == 906688011) {
                this.trackLimits[trackLimits.Count - 1].Limit = data.Optional;
            }
            this.trackLimits.Sort((x, y) => x.Location.CompareTo(y.Location));
        }

        public void Update(double location) {
            if (trackLimits.Count == 0) {
                previous = SpeedLimit.inf;
                next = SpeedLimit.inf;
                return;
            }
            int pointer = 0;
            while (pointer < trackLimits.Count && trackLimits[pointer].Location < location) pointer++;
            if (pointer == trackLimits.Count) {
                previous = trackLimits.Last();
                next = SpeedLimit.inf;
            } else if (pointer == 0) {
                previous = SpeedLimit.inf;
                next = trackLimits.First();
            } else {
                previous = trackLimits[pointer - 1];
                next = trackLimits[pointer];
            }
            Copy(next);
        }

        // Only considering previous and next!
        public override double AtLocation(double location, double idealDecel, double vOffset = 0) {
            if (location >= next.Location) {
                return next.Limit + vOffset;
            } else if (location >= previous.Location) {
                return Math.Min(previous.Limit + vOffset, next.AtLocation(location, idealDecel, vOffset));
            } else {
                return previous.AtLocation(location, idealDecel, vOffset);
            }
        }
    }
}
