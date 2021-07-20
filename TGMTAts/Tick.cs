using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TGMTAts {

    public static partial class TGMTAts {
        
        internal static SpeedLimit nextLimit;

        public static SpeedLimit movementEndpoint = SpeedLimit.inf;

        private const int speedMultiplier = 4;

        static int lastDrawTime = 0;

        [DllExport(CallingConvention.StdCall)]
        public static AtsHandles Elapse(AtsVehicleState state, IntPtr hPanel, IntPtr hSound) {
            var panel = new AtsIoArray(hPanel);
            var sound = new AtsIoArray(hSound);
            pluginReady = true;
            ackMessage = 0;
            location = state.Location;
            time = state.Time;

            var handles = new AtsHandles { Power = pPower, Brake = pBrake,
                Reverser = pReverser, ConstantSpeed = AtsCscInstruction.Continue };

            double ebSpeed = 0, recommendSpeed = 0, targetSpeed = 0, targetDistance = 0;
            trackLimit.Update(location);
            StationManager.Update(state, doorOpen);

            CalculatedLimit maximumCurve = null, targetCurve = null, recommendCurve = null;
            switch (signalMode) {
                case 0:
                    ebSpeed = Config.RMSpeed;
                    recommendSpeed = -10;
                    targetDistance = -10;
                    targetSpeed = -10;
                    driveMode = 0;
                    break;
                case 1:
                    // ITC
                    if (selectedMode > 0 && driveMode == 0) driveMode = 1;
                    maximumCurve = CalculatedLimit.Calculate(location,
                        Config.EbPatternDeceleration, Config.RecommendSpeedOffset, movementEndpoint, trackLimit);
                    targetCurve = CalculatedLimit.Calculate(location,
                        Config.EbPatternDeceleration, 0, movementEndpoint, trackLimit);
                    recommendCurve = CalculatedLimit.Calculate(location,
                        Config.RecommendDeceleration, 0, StationManager.RecommendCurve(), movementEndpoint, trackLimit);
                    // 释放速度
                    if (movementEndpoint.Location - location < Config.ReleaseSpeedDistance 
                        && movementEndpoint.Location > location
                        && state.Speed < Config.ReleaseSpeed && !releaseSpeed) {
                        ackMessage = 2;
                    }
                    break;
                case 2:
                    // CTC
                    releaseSpeed = false;
                    movementEndpoint = StationManager.CTCEndpoint();
                    if (selectedMode > 0 && driveMode == 0) driveMode = 1;
                    maximumCurve = CalculatedLimit.Calculate(location,
                        Config.EbPatternDeceleration, Config.RecommendSpeedOffset, movementEndpoint,
                        PreTrainManager.GetEndpoint(), trackLimit);
                    targetCurve = CalculatedLimit.Calculate(location,
                        Config.EbPatternDeceleration, 0, movementEndpoint, 
                        PreTrainManager.GetEndpoint(), trackLimit);
                    recommendCurve = CalculatedLimit.Calculate(location,
                        Config.RecommendDeceleration, 0, StationManager.RecommendCurve(), 
                        PreTrainManager.GetEndpoint(), movementEndpoint, trackLimit);
                    break;
                default:
                    // fallback
                    ebSpeed = Config.MaxSpeed;
                    recommendSpeed = -10;
                    targetSpeed = 0;
                    targetDistance = -10;
                    break;
            }
            if (maximumCurve != null) {
                // ITC/CTC 有速度曲线
                ebSpeed = Math.Min(Config.MaxSpeed, Math.Max(0, maximumCurve.CurrentTarget));
                recommendSpeed = Math.Min(ebSpeed - Config.RecommendSpeedOffset, 
                    Math.Max(0, recommendCurve.CurrentTarget));
                nextLimit = targetCurve.NextLimit;
                targetDistance = targetCurve.NextLimit.Location - location;
                targetSpeed = targetCurve.NextLimit.Limit;
                if (location > movementEndpoint.Location) {
                    // 如果已冲出移动授权终点，释放速度无效
                    if (releaseSpeed) Log("超出了移动授权终点, 释放速度无效");
                    recommendSpeed = 0;
                    ebSpeed = 0;
                    releaseSpeed = false;
                }
                if (releaseSpeed) {
                    ebSpeed = Math.Max(ebSpeed, Config.ReleaseSpeed);
                    recommendSpeed = Math.Max(recommendSpeed, Config.ReleaseSpeed - Config.RecommendSpeedOffset);
                }
            }

            // 显示速度、预选模式、驾驶模式、控制级别、车门模式
            panel[1] = Convert.ToInt32(Math.Ceiling(Math.Abs(state.Speed) * speedMultiplier));
            panel[22] = selectedMode;
            panel[24] = driveMode;
            panel[25] = signalMode;
            panel[28] = (driveMode > 0) ? (driveMode > 1 ? doorMode : 1) : 0;

            // 显示临时预选模式
            if (state.Speed != 0 || time > selectModeStartTime + Config.ModeSelectTimeout * 1000) {
                selectingMode = -1;
                selectModeStartTime = 0;
            }
            if (selectingMode >= 0) {
                ackMessage = 4;
                panel[22] = time % 500 < 250 ? selectingMode : 6;
            }

            // 显示目标速度、建议速度、干预速度
            if (doorOpen) {
                targetDistance = 0;
                targetSpeed = -10;
            }
            panel[11] = distanceToPixel(targetDistance);
            panel[19] = (int)targetDistance;
            panel[16] = (int)(ebSpeed * speedMultiplier);
            if (driveMode < 2) {
                panel[15] = (int)(recommendSpeed * speedMultiplier);
            } else {
                panel[15] = -1;
            }
            distanceToColor(targetSpeed, targetDistance, panel);
            targetSpeed = Math.Min(targetSpeed, Config.MaxSpeed);
            panel[17] = (int)targetSpeed;
            panel[18] = (targetSpeed < 0) ? 1 : 0;
            panel[29] = panel[31] = 0;

            // 显示出发与屏蔽门信息
            if (signalMode > 1 && state.Speed == 0) {
                if (Math.Abs(StationManager.NextStation.StopPosition - location) < Config.DoorEnableWindow
                    && time > StationManager.NextStation.DepartureTime - Config.DepartRequestTime * 1000) {
                    panel[32] = 2;
                } else if (doorOpen && time - doorOpenTime >= 3000) {
                    panel[32] = 1;
                } else {
                    panel[32] = 0;
                }
            } else {
                panel[32] = 0;
            }
            if (signalMode >= 2 && state.Speed == 0) {
                if (doorOpen) {
                    if (time - doorOpenTime >= 1000) {
                        panel[29] = 3;
                    } else {
                        panel[29] = 0;
                    }
                } else {
                    if (time - doorCloseTime >= 1000) {
                        panel[29] = 0;
                    } else {
                        panel[29] = 3;
                    }
                }
            }

            // 如果没有无线电，显示无线电故障
            panel[23] = state.Speed == 0 ? 0 : 1;
            panel[30] = deviceCapability != 2 ? 1 : 0;

            // ATO
            panel[40] = 0;
            Ato.UpdateAccel(state.Speed, recommendSpeed);
            if (signalMode > 0) {
                if (handles.Power != 0 || handles.Brake != 0 || handles.Reverser != 1) {
                    driveMode = 1;
                }
                if (recommendSpeed == 0 && state.Speed == 0) {
                    driveMode = 1;
                }
                if (driveMode >= 2) {
                    panel[40] = 1;
                    var notch = Ato.GetCmdNotch(state.Speed, recommendSpeed, ebSpeed);
                    if (notch < 0) {
                        handles.Power = 0;
                        handles.Brake = -notch;
                        panel[21] = 3;
                    } else if (notch > 0) {
                        handles.Power = notch;
                        handles.Brake = 0;
                        panel[21] = 1;
                    } else {
                        handles.Power = 0;
                        handles.Brake = 0;
                        panel[21] = 2;
                    }
                } else {
                    panel[21] = 0;
                    if (Ato.IsAvailable()) {
                        // 闪烁
                        panel[40] = time % 500 < 250 ? 1 : 0;
                    }
                }
            }

            // ATP 制动干预部分
            if (ebSpeed > 0) {
                // 有移动授权
                if (state.Speed == 0 && handles.Power == 0) {
                    // 低于制动缓解速度
                    if (ebState > 0) {
                        if (location > movementEndpoint.Location) {
                            // 冲出移动授权终点，要求RM
                            ackMessage = 6;
                        } else {
                            handles.Brake = 0;
                            ebState = 0;
                        }
                    }
                    panel[10] = 0;
                } else if (state.Speed > ebSpeed) {
                    // 超出制动干预速度
                    ebState = 1;
                    if (driveMode > 1) driveMode = 1;
                    sound[0] = 1;
                    panel[10] = 2;
                    panel[29] = 2;
                    handles.Brake = vehicleSpec.BrakeNotches + 1;
                } else {
                    if (ebState > 0) {
                        // 刚刚触发紧急制动，继续制动
                        panel[10] = 2;
                        panel[29] = 2;
                        handles.Brake = vehicleSpec.BrakeNotches + 1;
                    } else if (driveMode == 1 && state.Speed > recommendSpeed) {
                        // 超出建议速度，显示警告
                        if (panel[10] == 0) sound[0] = 1;
                        panel[10] = 1;
                    } else {
                        panel[10] = 0;
                    }
                }
            } else if (signalMode == 1) {
                // ITC下冲出移动授权终点。
                if (state.Speed == 0) {
                    // 停稳后降级到RM模式。等待确认。
                    ackMessage = 6;
                }
                ebState = 1;
                // 显示紧急制动、目标距离0、速度0
                panel[10] = 2;
                panel[29] = 2;
                panel[11] = 0;
                panel[19] = 0;
                panel[17] = 0;
                handles.Brake = vehicleSpec.BrakeNotches + 1;
            }
            
            // 防溜、车门零速保护
            if (state.Speed < 0.5 && handles.Power < 1 && handles.Brake < 1) {
                handles.Brake = 1;
            }
            if (doorOpen) {
                panel[15] = -10 * speedMultiplier;
                panel[16] = 0;
                if (handles.Brake < 4) handles.Brake = 4;
            }

            // 后退监督: 每1m一次紧制 (先这么做着, 有些地区似乎是先1m之后每次0.5m)
            if (handles.Reverser == -1) {
                if (location > reverseStartLocation) reverseStartLocation = location;
                if (location < reverseStartLocation - Config.ReverseStepDistance) {
                    if (state.Speed == 0 && handles.Power == 0) {
                        reverseStartLocation = location;
                    } else {
                        handles.Brake = vehicleSpec.BrakeNotches + 1;
                    }
                }
            } else if (state.Speed >= 0) {
                reverseStartLocation = Config.LessInf;
            }

            // 显示释放速度、确认消息
            if (releaseSpeed) panel[31] = 3;
            if (ackMessage > 0) {
                panel[35] = ackMessage;
                panel[36] = ((state.Time / 1000) % 0.5 < 0.25) ? 1 : 0;
            } else {
                panel[35] = panel[36] = 0;
            }

            // 显示TDT、车门使能，车门零速保护
            if (StationManager.NextStation != null) {
                int sectogo = Convert.ToInt32((state.Time / 1000) - StationManager.NextStation.DepartureTime);
                if (StationManager.Arrived) {
                    // 已停稳，可开始显示TDT
                    if (location - StationManager.NextStation.StopPosition < Config.TDTFreezeDistance) {
                        // 未发车
                        // 这里先要求至少100m的移动授权
                        if (movementEndpoint.Location - location > 100) {
                            // 出站信号绿灯
                            if (sectogo < 0) {
                                // 未到发车时间
                                panel[102] = -1;
                            } else {
                                panel[102] = 1;
                            }
                        } else {
                            // 出站信号红灯
                            panel[102] = -1;
                        }
                        if (sectogo < 0) {
                            // 未到发车时间
                            panel[102] = -1;
                        } else {
                            panel[102] = 1;
                        }
                        panel[101] = Math.Min(Math.Abs(sectogo), 999);
                    } else {
                        // 已发车
                        panel[102] = -1;
                    }
                } else {
                    panel[102] = 0;
                    panel[101] = 0;
                }
                if (StationManager.NextStation.DepartureTime < 0.1) panel[102] = 0;
                if (Math.Abs(StationManager.NextStation.StopPosition - location) < Config.StationStartDistance) {
                    // 在车站范围内
                    if (Math.Abs(StationManager.NextStation.StopPosition - location) < Config.DoorEnableWindow) {
                        // 在停车窗口内
                        if (state.Speed < 1) {
                            panel[26] = 2;
                        } else {
                            panel[26] = 1;
                        }
                        if (state.Speed == 0) {
                            // 停稳, 可以解锁车门, 解锁对应方向车门
                            if (StationManager.NextStation.OpenLeftDoors) {
                                panel[27] = 1;
                            } else if (StationManager.NextStation.OpenRightDoors) {
                                panel[27] = 2;
                            } else {
                                panel[27] = 0;
                            }
                            if (doorOpen) {
                                panel[27] += 2; // 切换成已开门图像
                            }
                        } else {
                            panel[27] = 0;
                        }
                    } else {
                        // 不在停车窗口内
                        panel[26] = 1;
                        panel[27] = 0;
                    }
                } else {
                    // 不在车站范围内
                    panel[26] = 0;
                    panel[27] = 0;
                }
                if (signalMode == 0) {
                    // RM-IXL, 门要是开了就当它按了门允许, 没有车门使能和停车窗口指示
                    panel[26] = 0;
                    panel[27] = doorOpen ? 5 : 0;
                }
            }

            // 信号灯
            if (signalMode >= 2) {
                panel[41] = 2;
            } else {
                if (doorOpen) {
                    if (time - doorOpenTime >= 1000) {
                        panel[41] = 1;
                    } else {
                        panel[41] = 0;
                    }
                } else {
                    if (time - doorCloseTime >= 1000) {
                        panel[41] = 0;
                    } else {
                        panel[41] = 1;
                    }
                }
            }

            // 刷新HMI, TDT, 信号机材质，为了减少对FPS影响把它限制到最多一秒十次
            if (lastDrawTime > state.Time) lastDrawTime = 0;
            if (state.Time - lastDrawTime > 100) {
                lastDrawTime = state.Time;
                TextureManager.UpdateTexture(TextureManager.HmiTexture, TGMTPainter.PaintHMI(panel, state));
                TextureManager.UpdateTexture(TextureManager.TdtTexture, TGMTPainter.PaintTDT(panel, state));
            }
            return handles;
        }

        // 把目标距离折算成距离条上的像素数量。
        private static int distanceToPixel(double targetdistance) {
            int tgpixel = -10;
            if (targetdistance < 1) {
                tgpixel = 0;
            } else if (targetdistance < 2) {
                tgpixel = Convert.ToInt32(0 + (targetdistance - 1) / 1 * 20);
            } else if (targetdistance < 5) {
                tgpixel = Convert.ToInt32(20 + (targetdistance - 2) / 3 * 30);
            } else if (targetdistance < 10) {
                tgpixel = Convert.ToInt32(50 + (targetdistance - 5) / 5 * 20);
            } else if (targetdistance < 20) {
                tgpixel = Convert.ToInt32(70 + (targetdistance - 10) / 10 * 20);
            } else if (targetdistance < 50) {
                tgpixel = Convert.ToInt32(90 + (targetdistance - 20) / 30 * 28);
            } else if (targetdistance < 100) {
                tgpixel = Convert.ToInt32(118 + (targetdistance - 50) / 50 * 20);
            } else if (targetdistance < 200) {
                tgpixel = Convert.ToInt32(138 + (targetdistance - 100) / 100 * 20);
            } else if (targetdistance < 500) {
                tgpixel = Convert.ToInt32(158 + (targetdistance - 200) / 300 * 27);
            } else if (targetdistance < 750) {
                tgpixel = Convert.ToInt32(185 + (targetdistance - 500) / 250 * 15);
            } else {
                tgpixel = 200;
            }
            return tgpixel;
        }

        // 根据把目标距离设定距离条的颜色。
        private static void distanceToColor(double targetspeed, double targetdistance, AtsIoArray panel) {
            if (targetspeed < 0) {
                panel[12] = 0; panel[13] = 0; panel[14] = 0;
            } else if (targetspeed == 0) {
                if (targetdistance < 150) {
                    panel[12] = 1; panel[13] = 0; panel[14] = 0;
                } else if (targetdistance < 300) {
                    panel[12] = 0; panel[13] = 1; panel[14] = 0;
                } else {
                    panel[12] = 0; panel[13] = 0; panel[14] = 1;
                }
            } else if (targetspeed <= 25) {
                if (targetdistance < 300) {
                    panel[12] = 0; panel[13] = 1; panel[14] = 0;
                } else {
                    panel[12] = 0; panel[13] = 0; panel[14] = 1;
                }
            } else if (targetspeed <= 60) {
                if (targetdistance < 150) {
                    panel[12] = 0; panel[13] = 1; panel[14] = 0;
                } else {
                    panel[12] = 0; panel[13] = 0; panel[14] = 1;
                }
            } else {
                panel[12] = 0; panel[13] = 0; panel[14] = 1;
            }
        }
    }
}