using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace TGMTAts{
	public static partial class TGMTAts {

        private static bool a1Down, b1Down;

        [DllExport(CallingConvention.StdCall)]
        public static void KeyDown(AtsKey key){
            if (key == AtsKey.B2) {
                // End 模式确认
                switch (ackMessage) {
                    case 2:
                        // 释放速度
                        if (!releaseSpeed) Log("释放速度");
                        releaseSpeed = true;
                        break;
                    case 4:
                        // 确认预选模式
                        var lastSigMode = signalMode;
                        selectedMode = selectingMode;
                        selectModeStartTime = 0;
                        FixIncompatibleModes();
                        if (signalMode < lastSigMode) {
                            // CTC->ITC 降级到RM
                            // 有说实际运行中这么操作不会到RM的，不过移动授权终点不知道好没好？
                            signalMode = 0;
                            FixIncompatibleModes();
                        }
                        break;
                    case 6:
                        // 切换到RM
                        ebState = 0;
                        signalMode = 0;
                        FixIncompatibleModes();
                        break;
                }
            } else if (key == AtsKey.A1 || key == AtsKey.B1) {
                // Insert Home ATO启动
                if (key == AtsKey.A1) a1Down = true;
                if (key == AtsKey.B1) b1Down = true;
                if (a1Down && b1Down && Ato.IsAvailable()) {
                    driveMode = 2;
                }
            } else if (key == AtsKey.C1) {
                // PageUp 模式升级
                if (selectingMode == -1) selectingMode = selectedMode;
                // TODO: XAM还没做
                selectingMode = Math.Min(4, Math.Max(0, selectingMode + 1));
                selectModeStartTime = time;
            } else if (key == AtsKey.C2) {
                // PageDown 模式降级
                if (selectingMode == -1) selectingMode = selectedMode;
                // TODO: XAM还没做
                selectingMode = Math.Min(4, Math.Max(0, selectingMode - 1));
                selectModeStartTime = time;
            }
		}

        [DllExport(CallingConvention.StdCall)]
        public static void KeyUp(AtsKey key) {
            if (key == AtsKey.A1) a1Down = false;
            if (key == AtsKey.B1) b1Down = false;
        }

        [DllExport(CallingConvention.StdCall)]
        public static void SetBeaconData(AtsBeaconData data) {
            switch (data.Type) {
                case -16777214:
                    trackLimit.SetBeacon(data);
                    break;
                case 96811:
                    deviceCapability = data.Optional;
                    FixIncompatibleModes();
                    break;
                case 96812:
                    doorMode = data.Optional;
                    break;
                case 96813:
                    signalMode = data.Optional / 10 % 10;
                    selectedMode = data.Optional / 100 % 10;
                    driveMode = 1;
                    FixIncompatibleModes();
                    break;
                case 96810:
                    trackLimit.SetBeacon(data);
                    break;
                case 96820:
                case 96821:
                case 96822:
                case 96823:
                    StationManager.SetBeacon(data);
                    break;
                case 96828:
                case 96829:
                    PreTrainManager.SetBeacon(data);
                    break;
                case 96801:
                case 96802:
                    // TGMT 主
                    // TGMT 填充
                    signalMode = 2;
                    FixIncompatibleModes();

                    if (signalMode == 1) {
                        if (data.Signal > 0) {
                            Log("移动授权延伸到 " + data.Optional);
                            // 延伸移动授权终点
                            movementEndpoint = new SpeedLimit(0, data.Optional);
                            releaseSpeed = false;
                        } else {
                            Log("红灯 移动授权终点是 " + location + data.Distance);
                            movementEndpoint = new SpeedLimit(0, location + data.Distance);
                        }
                    }
                    break;
                case 96803:
                    // TGMT 定位
                    signalMode = 2;
                    FixIncompatibleModes();
                    break;
            }
        }

        [DllExport(CallingConvention.StdCall)]
        public static void Initialize(int initialHandlePosition) {
            driveMode = 1;
            FixIncompatibleModes();

            lastDrawTime = 0;
            movementEndpoint = SpeedLimit.inf;
            nextLimit = null;
            selectingMode = -1;
            selectModeStartTime = 0;
            pluginReady = false;
            reverseStartLocation = Config.LessInf;
            releaseSpeed = false;
            ebState = 0;
            ackMessage = 0;

            Ato.ResetCache();
            PreTrainManager.ResetCache();
        }

        public static int time;
        public static int doorOpenTime, doorCloseTime;
        
        [DllExport(CallingConvention.StdCall)]
        public static void DoorOpen() {
            doorOpen = true;
            doorOpenTime = time;
        }
        
        [DllExport(CallingConvention.StdCall)]
        public static void DoorClose() {
            doorOpen = false;
            doorCloseTime = time;
        }

        [DllExport(CallingConvention.StdCall)]
        public static void HornBlow(int type){

		}
		
	}
}