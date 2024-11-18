using BveTypes.ClassWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGMT_CBTC.OBCU {
    public class OBCU {
        public Train Train { get; private set; }
        private LineLimitTargets LineLimitTargetsYellow { get; set; }
        private LineLimitTargets LineLimitTargetsRed { get; set; }
        internal StationTargets StationTargets { get; set; }
        private CATSTargets CATSTargetsYellow { get; set; }
        private CATSTargets CATSTargetsRed { get; set; }

        public PidAto ATO { get; set; }

        public DriveMode DriveMode { get; set; } = DriveMode.SM;

        public double YellowSpeed { get; private set; }
        public double RedSpeed { get; private set; }
        public double? TargetSpeed { get; private set; }
        public double? TargetLocation { get; private set; }
        public double YellowSpeedAccel { get; private set; }
        public bool AtpExceedRcmd { get; private set; }
        public bool AtpEmergency { get; private set; }

        public OBCU(Scenario scenario) {
            Train = new Train(scenario);
            LineLimitTargetsYellow = new LineLimitTargets(scenario, Train, 0);
            LineLimitTargetsRed = new LineLimitTargets(scenario, Train, 5);
            StationTargets = new StationTargets(scenario);
            CATSTargetsYellow = new CATSTargets(scenario, 0);
            CATSTargetsRed = new CATSTargets(scenario, 5);
            ATO = new PidAto(0, 0, 0);
        }

        public void Tick(TimeSpan elapsed, Scenario scenario) {
            Train.Tick(scenario);
            StationTargets.Tick(Train);
            CATSTargetsYellow.Tick(Train);
            CATSTargetsRed.Tick(Train);
            ComputeTargets();
            ComputeAtp();
            ComputeAto(scenario.Vehicle);
            ComputeHoldBrake(scenario.Vehicle);
        }

        private void ComputeTargets() {
            SpeedTarget.TargetTuple redTarget = SpeedTarget.Reduce(Train.Location, 1.2, new List<IEnumerable<SpeedTarget>>() {
                new List<SpeedTarget>() { SpeedTarget.MaxSpeed(Train) },
                LineLimitTargetsRed.Targets,
                CATSTargetsRed.Targets
            });
            SpeedTarget.TargetTuple yellowTarget = SpeedTarget.Reduce(Train.Location, 0.8, new List<IEnumerable<SpeedTarget>>() {
                new List<SpeedTarget>() { SpeedTarget.MaxSpeed(Train, -5) },
                LineLimitTargetsYellow.Targets,
                CATSTargetsYellow.Targets,
                StationTargets.Targets
            });
            RedSpeed = redTarget.Effective.GetTargetAt(Train.Location, 1.2);
            YellowSpeed = yellowTarget.Effective.GetTargetAt(Train.Location, 0.8);
            YellowSpeedAccel = yellowTarget.Effective.GetAccelAt(Train.Location, 0.8);
            TargetSpeed = yellowTarget.Upcoming?.Speed;
            TargetLocation = yellowTarget.Upcoming?.Location;
        }

        private void ComputeAtp() {
            if (!Train.DoorClosed) {
                RedSpeed = 0;
                YellowSpeed = 0;
                YellowSpeedAccel = 0;
                TargetSpeed = null;
                TargetLocation = null;
            }
            AtpExceedRcmd = Train.Speed > YellowSpeed;
            // AtpEmergency |= Train.Speed > RedSpeed;
            if (Train.Speed == 0) AtpEmergency = false;
        }

        private void ComputeAto(Vehicle vehicle) {
            if (DriveMode > DriveMode.SM && !(
                vehicle.Instruments.Cab.Handles.PowerNotch == 0
                && vehicle.Instruments.Cab.Handles.BrakeNotch == 0
                && vehicle.Instruments.Cab.Handles.ReverserPosition == ReverserPosition.F
                )) {
                DriveMode = DriveMode.SM;
            }
            if (DriveMode < DriveMode.AM) return;
            ATO.Kp = 4;
            ATO.Ki = 30;
            ATO.Kd = 3;
            ATO.Tick(this);
            if (Train.DoorClosed) {
                int cmdNotch = ATO.CommandNotch;
                if (cmdNotch >= 0) {
                    vehicle.Instruments.AtsPlugin.AtsHandles.PowerNotch = cmdNotch;
                    vehicle.Instruments.AtsPlugin.AtsHandles.BrakeNotch = 0;
                } else {
                    vehicle.Instruments.AtsPlugin.AtsHandles.PowerNotch = 0;
                    vehicle.Instruments.AtsPlugin.AtsHandles.BrakeNotch = -cmdNotch;
                }
            } else {
                vehicle.Instruments.AtsPlugin.AtsHandles.PowerNotch = 0;
                vehicle.Instruments.AtsPlugin.AtsHandles.BrakeNotch = 3;
                DriveMode = DriveMode.AMInterrupted;
            }
        }

        private void ComputeHoldBrake(Vehicle vehicle) {
            if (Train.Speed < 0.1 && !(
                vehicle.Instruments.Cab.Handles.PowerNotch > 0
                || vehicle.Instruments.AtsPlugin.AtsHandles.PowerNotch > 0)) {
                int holdNotch = 3;
                if (vehicle.Instruments.Cab.Handles.BrakeNotch < holdNotch
                    && vehicle.Instruments.AtsPlugin.AtsHandles.BrakeNotch < holdNotch) {
                    vehicle.Instruments.AtsPlugin.AtsHandles.BrakeNotch = holdNotch;
                }
            }
        }

        internal void OnAtoStartPressed(Scenario scenario) {
            if (ATO.ShouldProvideATOStart(this, scenario.Vehicle)) {
                DriveMode = DriveMode.AM;
            }
        }
    }
}
