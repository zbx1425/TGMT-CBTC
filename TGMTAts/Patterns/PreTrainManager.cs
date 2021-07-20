using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TGMTAts {
    
    static class PreTrainManager {

        public class PassCommand {

            public double Location;
            public int Time;
        }

        public static List<PassCommand> Commands = new List<PassCommand>();

        public static double PreTrainSpeed;
        public static double lastTime;

        public static void ResetCache() {
            PreTrainSpeed = -10000;
            lastTime = 0;
        }

        public static void SetBeacon(TGMTAts.AtsBeaconData data) {
            switch (data.Type) {
                case 96828:
                    var findIndex = Commands.FindIndex(t => t.Location == data.Optional);
                    if (findIndex >= 0) Commands.RemoveAt(findIndex);
                    Commands.Add(new PassCommand() { Location = data.Optional, Time = 0 });
                    break;
                case 96829:
                    if (Commands.Count <= 0) return;
                    Commands[Commands.Count - 1].Time = TGMTAts.ConvertTime(data.Optional) * 1000;
                    Commands.Sort((a, b) => a.Time.CompareTo(b.Time));
                    break;
            }
        }

        public static SpeedLimit GetEndpoint() {
            if (Commands.Count == 0) return SpeedLimit.inf;
            int pointer = 0;
            var time = TGMTAts.time;
            while (pointer < Commands.Count && Commands[pointer].Time < time) pointer++;
            if (pointer == 0 || pointer >= Commands.Count) {
                return SpeedLimit.inf;
            } else {
                var trainLocation = Commands[pointer - 1].Location
                    + ((Commands[pointer].Location - Commands[pointer - 1].Location)
                    * ((double)(time - Commands[pointer - 1].Time)
                        / (Commands[pointer].Time - Commands[pointer - 1].Time)));
                var targetSpeed = (Commands[pointer].Location - Commands[pointer - 1].Location)
                    / (Commands[pointer].Time - Commands[pointer - 1].Time) * 1000 * 3.6;
                var nextTargetSpeed = Config.LessInf;
                if (pointer < Commands.Count - 1) {
                    nextTargetSpeed = (Commands[pointer + 1].Location - Commands[pointer].Location)
                    / (Commands[pointer + 1].Time - Commands[pointer].Time) * 1000 * 3.6;
                }
                targetSpeed = Math.Floor(targetSpeed / 5) * 5;
                nextTargetSpeed = Math.Floor(nextTargetSpeed / 5) * 5;
                targetSpeed = Math.Min(targetSpeed,
                    new SpeedLimit(nextTargetSpeed, Commands[pointer].Location)
                    .AtLocation(trainLocation, Config.MaxServiceDeceleration * 0.8));

                if (PreTrainSpeed < 0) {
                    PreTrainSpeed = targetSpeed;
                } else if (lastTime > 0) {
                    if (PreTrainSpeed < targetSpeed - 2) {
                        PreTrainSpeed += Ato.GetMaxAccelAt(PreTrainSpeed) * (TGMTAts.time - lastTime) / 1000;
                    } else if (PreTrainSpeed > targetSpeed + 2) {
                        PreTrainSpeed += Config.MaxServiceDeceleration * 0.8 * (TGMTAts.time - lastTime) / 1000;
                    } else {
                        PreTrainSpeed = targetSpeed;
                    }
                }
                lastTime = TGMTAts.time;
                return new SpeedLimit(Math.Round(PreTrainSpeed), trainLocation - Config.CTCSafetyDistance);
            }
        }
    }
}
