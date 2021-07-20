using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TGMTAts {
    public class TrackLimit : SpeedLimit {
        public List<SpeedLimit> trackLimits = new List<SpeedLimit>();
        public SpeedLimit last = SpeedLimit.inf;
        public SpeedLimit current = SpeedLimit.inf;
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
            } else if (data.Type == 96810) {
                var speed = data.Optional % 1000;
                var location = data.Optional / 1000;
                if (trackLimits.Any(s => s.Location == location)) return;
                this.trackLimits.Add(new SpeedLimit(speed, location));
            }
            this.trackLimits.Sort((x, y) => x.Location.CompareTo(y.Location));
        }

        public void Update(double location) {
            if (trackLimits.Count == 0) {
                current = SpeedLimit.inf;
                next = SpeedLimit.inf;
                return;
            }
            int pointer = 0;
            while (pointer < trackLimits.Count && trackLimits[pointer].Location < location) pointer++;
            if (pointer == trackLimits.Count) {
                if (pointer > 1) {
                    last = trackLimits[pointer - 2];
                } else {
                    last = SpeedLimit.inf;
                }
                current = trackLimits[pointer - 1];
                next = SpeedLimit.inf;
            } else {
                if (pointer > 1) {
                    last = trackLimits[pointer - 2];
                } else {
                    last = SpeedLimit.inf;
                }
                if (pointer > 0) {
                    current = trackLimits[pointer - 1];
                } else {
                    current = SpeedLimit.inf;
                }
                next = trackLimits[pointer];
            }
            Copy(next);
        }

        // Only considering last, current and next!
        public override double AtLocation(double location, double idealDecel, double vOffset = 0) {
            if (location >= next.Location) {
                return next.Limit + vOffset;
            } else if (current.Limit > last.Limit && location < current.Location + Config.TrainLength) {
                return last.Limit + vOffset;
            } else if (location >= current.Location) {
                return Math.Min(current.Limit + vOffset, next.AtLocation(location, idealDecel, vOffset));
            } else {
                return current.AtLocation(location, idealDecel, vOffset);
            }
        }
    }
}
