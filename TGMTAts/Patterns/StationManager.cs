using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TGMTAts {
    public static class StationManager {

        public class Station {
            public int StopPosition = (int)Config.LessInf;
            public int RouteOpenTime = 0;
            public int DepartureTime = 0;
            public int DoorOpenType = 0;
            public bool OpenLeftDoors { get { return DoorOpenType == 1 || DoorOpenType == 3; } }
            public bool OpenRightDoors { get { return DoorOpenType == 2 || DoorOpenType == 3; } }
        }

        public static Station NextStation = new Station();

        public static bool Stopped;

        public static bool Arrived;

        public static void SetBeacon(TGMTAts.AtsBeaconData data) {
            switch (data.Type) {
                case 96820:
                    NextStation.StopPosition = data.Optional;
                    TGMTAts.Log("车站停车位置 " + NextStation.StopPosition.ToString());
                    break;
                case 96821:
                    NextStation.DoorOpenType = data.Optional;
                    break;
                case 96822:
                    NextStation.RouteOpenTime = TGMTAts.ConvertTime(data.Optional) * 1000;
                    break;
                case 96823:
                    NextStation.DepartureTime = TGMTAts.ConvertTime(data.Optional) * 1000;
                    break;
            }
        }

        public static void Update(TGMTAts.AtsVehicleState state, bool doorState) {
            if (state.Speed == 0 && state.Location > NextStation.StopPosition - Config.StationStartDistance) {
                if (!Stopped) TGMTAts.Log("已在站内停稳");
                Stopped = true;
            }
            if (doorState) {
                if (!Arrived) TGMTAts.Log("已开门");
                Arrived = true;
            }
            if (state.Location > NextStation.StopPosition + Config.StationEndDistance) {
                NextStation = new Station();
                Stopped = false;
                Arrived = false;
                TGMTAts.Log("已出站");
            }
        }

        public static SpeedLimit RecommendCurve() {
            if (NextStation.StopPosition >= (int)Config.LessInf) {
                return SpeedLimit.inf;
            } else if (Arrived) {
                return SpeedLimit.inf;
            } else if (Stopped) {
                return new SpeedLimit(0, 0);
            } else {
                return new SpeedLimit(0, NextStation.StopPosition);
            }
        }

        public static SpeedLimit CTCEndpoint() {
            if (TGMTAts.time > NextStation.RouteOpenTime) {
                return SpeedLimit.inf;
            } else {
                return new SpeedLimit(0, NextStation.StopPosition + Config.StationMotionEndpoint);
            }
        }
    }
}
