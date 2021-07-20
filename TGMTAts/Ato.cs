using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TGMTAts {

    static class Ato {

        public static double lastRecSpeed, lastRecTime, recAccel;

        public static int outputNotch, lastOutputTime;

        public static void ResetCache() {
            lastRecSpeed = lastRecTime = recAccel = outputNotch = lastOutputTime = 0;
        }

        public static void UpdateAccel(double speed, double recSpeed) {
            if (TGMTAts.time - lastRecTime > 100) {
                recAccel = (recSpeed - lastRecSpeed) / (TGMTAts.time / 1000d - lastRecTime);
                lastRecSpeed = recSpeed;
                lastRecTime = TGMTAts.time / 1000d;
            }
        }

        public static double GetCmdDecel(double speed, double recSpeed, double ebSpeed) {
            var recAcc = recAccel;
            if (recAcc > 0) recAcc = 0;

            // 从 bve-autopilot 借来的算法
            var staDist = StationManager.NextStation.StopPosition - TGMTAts.location;
            var decelA = speed * speed / 2 / staDist;
            var decelB = -recAcc * (speed / recSpeed) - (recSpeed - speed) / 2;

            // 防止紧停, 尤其是大下坡上, 似乎难以完全避免
            if (speed > ebSpeed - 3) {
                decelA += 2;
                decelB += 2;
            }

            if (staDist < 0.6 && !StationManager.Arrived) {
                return 2; // 这大概停得住吧?
            } else if (staDist < -0.3 && !StationManager.Arrived) {
                return 5; // 好像要冲标了，赶紧停车
            //} else if (staDist < 20 && !StationManager.Arrived) {
            //    return Math.Min(decelA, decelB);
            } else if (staDist > 20 && recSpeed < 3) {
                return 2; // 到移动授权终点了?
            } else {
                return decelB;
            }
        }

        public static int GetCmdNotch(double speed, double recSpeed, double ebSpeed) {
            var decel = GetCmdDecel(speed, recSpeed, ebSpeed);
            if (decel > 0) {
                // 限制更改制动指令的时间，以免过于频繁地反复横跳
                if (TGMTAts.time - lastOutputTime > 250) {
                    outputNotch = -(int)Math.Round(Math.Min(decel / -Config.MaxServiceDeceleration, 1)
                        * TGMTAts.vehicleSpec.BrakeNotches);
                    if (outputNotch > 0) outputNotch = 0;
                    lastOutputTime = TGMTAts.time;
                }
            } else {
                const double targetTime = 4;
                var targetAccel = (recSpeed - speed) / targetTime + recAccel;
                if (TGMTAts.time - lastOutputTime > 250) {
                    outputNotch = (int)Math.Round(Math.Max(0, Math.Min(1, targetAccel / GetMaxAccelAt(speed)))
                        * TGMTAts.vehicleSpec.PowerNotches);
                    lastOutputTime = TGMTAts.time;
                }
            }
            return outputNotch;
        }

        public static double GetMaxAccelAt(double speed) {
            int pointer = 0;
            while (pointer < Config.Acceleration.Count && Config.Acceleration[pointer].Key < speed) pointer++;
            if (pointer == 0) {
                return Config.Acceleration[0].Value;
            } if (pointer >= Config.Acceleration.Count) {
                return Config.Acceleration.Last().Value;
            } else {
                return Config.Acceleration[pointer].Value
                    - (Config.Acceleration[pointer - 1].Value - Config.Acceleration[pointer].Value)
                    * ((speed - Config.Acceleration[pointer - 1].Key)
                        / (Config.Acceleration[pointer].Key - Config.Acceleration[pointer - 1].Key));
            }
        }

        public static bool IsAvailable() {
            return TGMTAts.pPower == 0 && TGMTAts.pBrake == 0 && TGMTAts.pReverser == 1
                    && !TGMTAts.doorOpen && TGMTAts.driveMode == 1 && TGMTAts.selectedMode > 2
                    && TGMTAts.ebState == 0
                    // 车站范围内不能接通ATO
                    && (StationManager.NextStation.StopPosition - TGMTAts.location > Config.StationStartDistance 
                        || StationManager.Arrived)
                    // 离移动授权终点太近不能接通ATO (这是现实情况吗？)
                    && (TGMTAts.movementEndpoint.Location - TGMTAts.location > 50 || TGMTAts.releaseSpeed)
                    // CTC下离前车太近不能接通ATO (这是现实情况吗？)
                    && (TGMTAts.signalMode == 1 || PreTrainManager.GetEndpoint().Location - TGMTAts.location > 50);
        }
    }
}
