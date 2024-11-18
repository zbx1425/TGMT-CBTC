using BveTypes.ClassWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TGMT_CBTC {
    public class LineLimitTargets {

        public List<SpeedTarget> Targets { get; private set; } = new List<SpeedTarget>();

        public LineLimitTargets(Scenario scenario, Train train, double speedOffset) {
            Init(scenario, train, speedOffset);
        }

        private void Init(Scenario scenario, Train train, double speedOffset) {
            List<AreaSpeedTarget> areaTargets = new List<AreaSpeedTarget>();
            List<double> interestedPoints = new List<double>();
            foreach (var speedLimit in scenario.Route.SpeedLimits) {
                if (areaTargets.Count > 0) {
                    areaTargets[areaTargets.Count - 1].LocationEnd 
                        = speedLimit.Location + train.Length;
                    interestedPoints.Add(speedLimit.Location + train.Length);
                }
                areaTargets.Add(new AreaSpeedTarget(speedLimit.Location, 
                    ((ValueNode<double>)speedLimit).Value * Units.MPS_TO_KMH + speedOffset));
                interestedPoints.Add(speedLimit.Location);
            }
            interestedPoints.Sort();
            Targets.Clear();
            double lastLimit = double.MaxValue;
            foreach (var location in interestedPoints) {
                // Find out the minimum limit that applies to this point
                double minLimit = double.MaxValue;
                foreach (var areaTarget in areaTargets) {
                    if (areaTarget.LocationStart <= location && areaTarget.LocationEnd > location) {
                        minLimit = Math.Min(minLimit, areaTarget.Speed);
                    }
                    if (areaTarget.LocationStart > location) {
                        break;
                    }
                }
                if (minLimit != lastLimit) {
                    lastLimit = minLimit;
                    Targets.Add(SpeedTarget.LineSpeed(location, minLimit));
                }
            }
        }

        private class AreaSpeedTarget {

            public double LocationStart { get; set; }
            public double LocationEnd { get; set; }
            public double Speed { get; set; }

            public AreaSpeedTarget(double locationStart, double speed) {
                LocationStart = locationStart;
                LocationEnd = double.MaxValue;
                Speed = speed;
            }
        }
    }
}
