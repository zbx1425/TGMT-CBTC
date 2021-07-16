using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TGMTAts {

    static class Ato {

        public static double lastRecSpeed, lastRecTime;

        public static int outputNotch, lastBrakeOutputTime;

        public static double GetCmdDecel(double speed, double recSpeed) {
            var recAcc = (recSpeed - lastRecSpeed) / (TGMTAts.time / 1000d - lastRecTime);
            if (recAcc > 0) recAcc = 0;

            // 从 bve-autopilot 借来的算法
            var staDist = StationManager.NextStation.StopPosition - TGMTAts.location;
            var decelA = speed * speed / 2 / staDist;
            var decelB = -recAcc * (speed / recSpeed) - (recSpeed - speed) / 2;

            lastRecSpeed = recSpeed;
            lastRecTime = TGMTAts.time / 1000d;
            if (staDist < 0.5 && !StationManager.Arrived) {
                return 2; //这大概停得住吧?
            } else if (staDist < 20 && !StationManager.Arrived) {
                return Math.Min(decelA, decelB);
            } else {
                return decelB;
            }
        }

        public static int GetCmdNotch(double speed, double recSpeed) {
            var decel = GetCmdDecel(speed, recSpeed);
            if (decel > 0) {
                // 限制更改制动指令的时间，以免反复横跳
                if (TGMTAts.time - lastBrakeOutputTime > 250) {
                    outputNotch = -(int)Math.Round(Math.Min(decel / -Config.MaxServiceDeceleration, 1)
                        * TGMTAts.vehicleSpec.BrakeNotches - 1);
                    if (outputNotch > 0) outputNotch = 0;
                    lastBrakeOutputTime = TGMTAts.time;
                }
            } else {
                if (speed > recSpeed - 1) {
                    outputNotch = 0;
                } else if (speed < recSpeed - 5) {
                    outputNotch = TGMTAts.vehicleSpec.PowerNotches;
                } else if (speed < recSpeed - 3 && outputNotch <= 0) {
                    outputNotch = TGMTAts.vehicleSpec.PowerNotches / 2;
                } else if (speed > recSpeed - 3 && outputNotch > 0) {
                    outputNotch = TGMTAts.vehicleSpec.PowerNotches / 2;
                }
            }
            return outputNotch;
        }

        public static bool IsAvailable() {
            return TGMTAts.pPower == 0 && TGMTAts.pBrake == 0 && TGMTAts.pReverser == 1
                    && !TGMTAts.doorOpen && TGMTAts.driveMode == 1 && TGMTAts.selectedMode > 2
                    // 车站范围内不能接通ATO
                    && (StationManager.NextStation.StopPosition - TGMTAts.location > Config.StationStartDistance 
                        || StationManager.Arrived)
                    // 离移动授权终点太近不能接通ATO (这是现实情况吗？)
                    && (TGMTAts.movementEndpoint.Location - TGMTAts.location > 50 || TGMTAts.releaseSpeed);
        }
    }
}
