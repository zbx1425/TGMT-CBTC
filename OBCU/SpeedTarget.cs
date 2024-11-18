using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TGMT_CBTC {
    public struct SpeedTarget {

        public double Location { get; set; }
        public double Speed { get; set; }
        public bool ContinueTillNext { get; set; }

        public SpeedTarget(double beginLocation, double speed, bool areaTarget) {
            Location = beginLocation;
            Speed = speed;
            ContinueTillNext = areaTarget;
        }

        public double GetTargetAt(double location, double deceleration) {
            if (location < Location) {
                return Math.Sqrt(2 * (deceleration) * (Location - location)
                    + Math.Pow((Speed * Units.KMH_TO_MPS), 2)) * Units.MPS_TO_KMH;
            } else {
                return Speed;
            }
        }

        public double GetAccelAt(double location, double deceleration) {
            if (location < Location) {
                return -deceleration;
            } else {
                return 0;
            }
        }

        public static SpeedTarget LineSpeed(double location, double speed) {
            return new SpeedTarget(location, speed, true);
        }

        public static SpeedTarget CheckPoint(double location, double speed) {
            return new SpeedTarget(location, speed, false);
        }

        public static SpeedTarget StopPoint(double location) {
            return new SpeedTarget(location, 0, true);
        }

        public static SpeedTarget MaxSpeed(Train train) {
            return new SpeedTarget(double.NegativeInfinity, train.MaxSpeed, true);
        }

        public static SpeedTarget MaxSpeed(Train train, double speedOffset) {
            return new SpeedTarget(double.NegativeInfinity, train.MaxSpeed + speedOffset, true);
        }

        public static SpeedTarget Empty() {
            return new SpeedTarget(double.NegativeInfinity, double.PositiveInfinity, true);
        }

        public struct TargetTuple {
            public SpeedTarget Effective { get; set; }
            public SpeedTarget? Upcoming { get; set; }

            public TargetTuple(SpeedTarget effective, SpeedTarget? upcoming) {
                Effective = effective;
                Upcoming = upcoming;
            }
        }

        public static TargetTuple Reduce(double location, double deceleration, IEnumerable<IEnumerable<SpeedTarget>> suppliers) {
            SpeedTarget effective = SpeedTarget.Empty();
            SpeedTarget? upcoming = null;

            foreach (var targetSet in suppliers) {
                SpeedTarget? thisSetLastTarget = null;
                foreach (var target in targetSet) {
                    if (target.Location < location) {
                        thisSetLastTarget = target;
                        continue;
                    }
                    if (thisSetLastTarget.HasValue && thisSetLastTarget.Value.ContinueTillNext) {
                        if (thisSetLastTarget.Value.Speed < effective.GetTargetAt(location, deceleration)) {
                            effective = thisSetLastTarget.Value;
                        }
                        if (thisSetLastTarget.Value.Speed == 0 && (!upcoming.HasValue || upcoming.Value.Speed > 0)) {
                            upcoming = thisSetLastTarget.Value;
                        }
                        thisSetLastTarget = null;
                    }

                    if (target.GetTargetAt(location, deceleration) < effective.GetTargetAt(location, deceleration)) {
                        effective = target;
                    }
                    if (!upcoming.HasValue) {
                        upcoming = target;
                    } else {
                        if (upcoming.Value.GetTargetAt(location, deceleration) > target.GetTargetAt(location, deceleration)) {
                            upcoming = target;
                        }
                        // double minLocation = Math.Min(upcoming.Value.Location, target.Location);
                        // double minSpeed = Math.Min(upcoming.Value.GetTargetAt(minLocation, deceleration),
                        //     target.GetTargetAt(minLocation, deceleration));
                        // upcoming = new SpeedTarget(minLocation, minSpeed, false);
                    }

                    // if (target.Location > location + 1000) {
                    //     break;
                    // }
                }
                if (thisSetLastTarget.HasValue && thisSetLastTarget.Value.ContinueTillNext) {
                    if (thisSetLastTarget.Value.Speed < effective.GetTargetAt(location, deceleration)) {
                        effective = thisSetLastTarget.Value;
                    }
                    if (thisSetLastTarget.Value.Speed == 0 && (!upcoming.HasValue || upcoming.Value.Speed > 0)) {
                        upcoming = thisSetLastTarget.Value;
                    }
                    thisSetLastTarget = null;
                }
            }
            return new TargetTuple(effective, upcoming);
        }
    }
}
