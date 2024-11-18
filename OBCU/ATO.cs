using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TGMT_CBTC;

namespace TGMT_CBTC.OBCU {

    public class ATO {

        private OBCU OBCU { get; set; }

        public double lastRecSpeed, lastRecTime, recAccel;

        public int outputNotch, lastOutputTime;

        public ATO(OBCU obcu) {
            OBCU = obcu;
        }

        public void ResetCache() {
            lastRecSpeed = lastRecTime = recAccel = outputNotch = lastOutputTime = 0;
        }

        public void UpdateAccel(double speed, double recSpeed) {
            if (OBCU.Train.Time - lastRecTime > 100) {
                recAccel = (recSpeed - lastRecSpeed) / (OBCU.Train.Time / 1000d - lastRecTime);
                lastRecSpeed = recSpeed;
                lastRecTime = OBCU.Train.Time / 1000d;
            }
        }

        public double GetCmdDecel(double speed, double recSpeed, double ebSpeed) {
            var recAcc = recAccel;
            if (recAcc > 0) recAcc = 0;

            // 从 bve-autopilot 借来的算法
            // 由于使用时总出现超过紧停速度的情况，做了些改动，使它更激进地将速度保持在推荐速度以下
            var staDist = OBCU.StationTargets.Targets[0].Location - OBCU.Train.Location;
            var decelA = -recAcc * (speed / recSpeed) - (recSpeed - speed) / 0.5;
            var decelB = -recAcc * (speed / recSpeed) - (recSpeed - speed) / 1;

            // 防止紧停, 尤其是大下坡上, 但似乎难以完全避免
            // 也许应该把坡度考虑进去？
            if (speed > ebSpeed - 3) {
                decelA += 5;
                decelB += 5;
            }

            if (staDist < 0) {
                return 5; // 好像要冲标了，赶紧停车
            } else if (staDist < 0.6) {
                return Math.Max(decelA, 2); // 这大概停得住吧?
            } else if (staDist < 20) {
                return decelA;
            } else if (staDist > 20 && recSpeed < 5) {
                return 2; // 到移动授权终点了?
            } else {
                return decelB;
            }
        }

        public int GetCmdNotch(double speed, double recSpeed, double ebSpeed) {
            if (OBCU.Train.Time - lastOutputTime < 0) {
                lastRecSpeed = lastRecTime = recAccel = outputNotch = lastOutputTime = 0;
            }
            // var accelDueToPitch = TGMTAts.trackPitch * 10; // 角度极小，sin约等于tan
            var accelDueToPitch = 0;
            var cmdDecel = GetCmdDecel(speed, recSpeed, ebSpeed) - accelDueToPitch;
            if (cmdDecel > 0) {
                // 限制更改制动指令的时间，以免过于频繁地反复横跳
                if (OBCU.Train.Time - lastOutputTime > 250) {
                    outputNotch = -(int)Math.Round(Math.Min(cmdDecel 
                        / (OBCU.Train.MaxServiceDeceleration * Units.MPS2_TO_KMHS), 1)
                        * OBCU.Train.BrakeNotches);
                    if (outputNotch > 0) outputNotch = 0;
                    lastOutputTime = OBCU.Train.Time;
                }
            } else {
                const double targetTime = 4;
                var targetAccel = (recSpeed - speed) / targetTime + recAccel - accelDueToPitch;
                if (OBCU.Train.Time - lastOutputTime > 250) {
                    outputNotch = (int)Math.Round(Math.Max(0, Math.Min(1, targetAccel / 3.6))
                        * OBCU.Train.PowerNotches);
                    lastOutputTime = OBCU.Train.Time;
                }
            }
            return outputNotch;
        }

        //public double GetMaxAccelAt(double speed) {
        //    int pointer = 0;
        //    while (pointer < Config.Acceleration.Count && Config.Acceleration[pointer].Key < speed) pointer++;
        //    if (pointer == 0) {
        //        return Config.Acceleration[0].Value;
        //    } if (pointer >= Config.Acceleration.Count) {
        //        return Config.Acceleration.Last().Value;
        //    } else {
        //        return Config.Acceleration[pointer].Value
        //            - (Config.Acceleration[pointer - 1].Value - Config.Acceleration[pointer].Value)
        //            * ((speed - Config.Acceleration[pointer - 1].Key)
        //                / (Config.Acceleration[pointer].Key - Config.Acceleration[pointer - 1].Key));
        //    }
        //}

        //public bool IsAvailable() {
        //    return TGMTAts.pPower == 0 && TGMTAts.pBrake == 0 && TGMTAts.pReverser == 1
        //            && !TGMTAts.doorOpen && TGMTAts.driveMode == 1 && TGMTAts.selectedMode > 2
        //            && TGMTAts.ebState == 0
        //            // 车站范围内不能接通ATO
        //            && (StationManager.NextStation.StopPosition - TGMTAts.location > Config.StationStartDistance 
        //                || StationManager.Arrived)
        //            // 离移动授权终点太近不能接通ATO (这是现实情况吗？)
        //            && (TGMTAts.motionEndpoint.Location - TGMTAts.location > 50 || TGMTAts.releaseSpeed)
        //            // CTC下离前车太近不能接通ATO (这是现实情况吗？)
        //            && (TGMTAts.signalMode == 1 || PreTrainManager.GetEndpoint().Location - TGMTAts.location > 50);
        //}
    }
}
