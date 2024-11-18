using BveTypes.ClassWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGMT_CBTC.OBCU {

    public class PidAto {

        public double Kp, Ki, Kd;
        private double ek_1, ek_2, uk_1;
        private int lastTime;

        public double CommandAccel { get; set; }
        public int CommandNotch { get; set; }

        public PidAto(double kp, double ki, double kd) {
            Kp = kp;
            Ki = ki;
            Kd = kd;
        }

        public void Reset() {
            ek_1 = ek_2 = uk_1 = 0;
            lastTime = 0;
        }

        public void Tick(OBCU obcu) {
            double targetSpeed = obcu.YellowSpeed;
            double currentSpeed = obcu.Train.Speed;
            double error = targetSpeed - currentSpeed;

            if (lastTime == 0) {
                lastTime = obcu.Train.Time;
                ek_1 = ek_2 = error;
                return;
            }

            double dt = (obcu.Train.Time - lastTime) / 1000;
            if (dt < 0 || dt > 10) {
                Reset();
                return;
            }
            if (dt < 0.1) return;

            double a0 = Kp + Ki * dt + Kd / dt;
            double a1 = -Kp - 2 * Kd / dt;
            double a2 = Kd / dt;

            double u = a0 * error + a1 * ek_1 + a2 * ek_2 + uk_1;
            ek_2 = ek_1;
            ek_1 = error;

            CommandAccel = u;
            if (obcu.YellowSpeed <= 2) {
                CommandAccel = Math.Min(CommandAccel, -2.5);
            }
            if (CommandAccel < 0) {
                CommandNotch = -(int)Math.Round(Math.Min(-CommandAccel
                        / (obcu.Train.MaxServiceDeceleration * Units.MPS2_TO_KMHS), 1)
                        * obcu.Train.BrakeNotches);
                if (CommandNotch > 0) CommandNotch = 0;
            } else if (CommandAccel > 0) {
                CommandNotch = (int)Math.Round(Math.Min(CommandAccel
                        / (1.0 * Units.MPS2_TO_KMHS), 1)
                        * obcu.Train.PowerNotches);
                if (CommandNotch < 0) CommandNotch = 0;
            } else {
                CommandNotch = 0;
            }
        }

        public bool ShouldProvideATOStart(OBCU obcu, Vehicle vehicle) {
            return vehicle.Instruments.Cab.Handles.PowerNotch == 0
                && vehicle.Instruments.Cab.Handles.BrakeNotch == 0
                && vehicle.Instruments.Cab.Handles.ReverserPosition == ReverserPosition.F
                && obcu.YellowSpeed > 5
                && !obcu.AtpEmergency;
        }
    }
}
