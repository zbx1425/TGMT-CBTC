using BveTypes.ClassWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TGMT_CBTC {
    public class Train {

        public double Length { get; private set; }
        public double Speed { get; private set; }
        public double Location { get; private set; }
        public bool DoorClosed { get; private set; }
        public double MaxServiceDeceleration { get; set; }
        public double MaxSpeed { get; set; }
        public int Time { get; private set; }

        public int PowerNotches { get; private set; }
        public int BrakeNotches { get; private set; }

        public Train(Scenario scenario) {
            Length = (scenario.Vehicle.Dynamics.MotorCar.Count + scenario.Vehicle.Dynamics.TrailerCar.Count)
                * scenario.Vehicle.Dynamics.CarLength;
            MaxServiceDeceleration = 4.0 * Units.KMHS_TO_MPS2;
            MaxSpeed = 100;

            PowerNotches = scenario.Vehicle.Instruments.Cab.Handles.NotchInfo.PowerNotchCount;
            BrakeNotches = scenario.Vehicle.Instruments.Cab.Handles.NotchInfo.BrakeNotchCount;
        }

        public void Tick(Scenario scenario) {
            Speed = scenario.LocationManager.SpeedMeterPerSecond * Units.MPS_TO_KMH;
            Location = scenario.LocationManager.Location;
            Time = scenario.TimeManager.TimeMilliseconds;
            DoorClosed = scenario.Vehicle.Doors.AreAllClosed;
        }
    }
}
