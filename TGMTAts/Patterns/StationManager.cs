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
                case 906688020:
                    NextStation.StopPosition = data.Optional;
                    TGMTAts.Log("车站停车位置 " + NextStation.StopPosition.ToString());
                    break;
                case 906688021:
                    NextStation.DoorOpenType = data.Optional;
                    break;
                case 906688022:
                    NextStation.RouteOpenTime = data.Optional;
                    break;
                case 906688023:
                    NextStation.DepartureTime = data.Optional;
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
            /* if (Stations.Count > 0) {
                int stapointer = 0;
                while ((Stations[stapointer].StopPosition + Config.StationEndDistance - 1 < location)
                    && stapointer < Stations.Count) stapointer++;
                if (stapointer >= Stations.Count) {
                    NextStation = null;
                    Copy(inf);
                    EndPoint.Copy(inf);
                } else {
                    NextStation = Stations[stapointer];
                    if (ArrivePtr == stapointer && IsRouteOpen(location, state, stapointer)) {
                        Copy(inf);
                        EndPoint.Copy(inf);
                    } else {
                        this.Limit = 0;
                        this.Location = Station.StopPosition;
                        this.EndPoint.Limit = 0;
                        this.EndPoint.Location = Station.StopPosition + Config.StationMotionEndpoint;
                    }
                }
                if (ArrivePtr < 0 && Math.Abs(Stations[stapointer].StopPosition - location) < Config.DoorEnableWindow &&
                    state.Speed == 0.0 && doorState) {
                    ArrivePtr = stapointer;
                    ArriveTime = state.Time / 1000;
                }
                if (Stations[stapointer].StopPosition - location > 200) ArrivePtr = -1;
            } else {
                NextStation = null;
                Copy(inf);
                EndPoint.Copy(inf);
            } */
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
            if (TGMTAts.time / 1000 > NextStation.RouteOpenTime) {
                return SpeedLimit.inf;
            } else {
                return new SpeedLimit(0, NextStation.StopPosition + Config.StationMotionEndpoint);
            }
        }
    }
}
