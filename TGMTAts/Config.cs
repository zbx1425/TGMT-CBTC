using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TGMTAts {

    public static class Config {

        public const double LessInf = 100000000;
        public static string PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static bool Debug = false;

        public static double MaxSpeed = 80;
        public static double RecommendSpeedOffset = -5;
        public static double RecommendDeceleration = -2;
        public static double EbPatternDeceleration = -3;
        public static double MaxServiceDeceleration = -4;
        public static List<KeyValuePair<int, double>> Acceleration = new List<KeyValuePair<int, double>>();

        public static double ReverseStepDistance = 5;
        public static double DoorEnableWindow = 1;

        public static double RMSpeed = 25;
        public static double ReleaseSpeed = 25;
        public static double ReleaseSpeedDistance = 200;
        public static double StationStartDistance = 100;
        public static double StationEndDistance = 5;
        public static double StationMotionEndpoint = 3;
        public static double CTCSafetyDistance = 30;
        public static double CloseRequestShowTime = 1000;
        public static double TrainHoldShowTime = 20;

        public static double TrainLength = 0;

        public static double DepartRequestTime = 10;
        public static double ModeSelectTimeout = 5;

        public static double TDTFreezeDistance = 10;

        public static string HarmonyPath = "0Harmony.dll";
        public static string ImageAssetPath = "TGMT";

        public static string HMIImageSuffix = "zbx1425_tgmt_hmi.png";
        public static string TDTImageSuffix = "zbx1425_tgmt_tdt.png";
        public static string SignalImageSuffix = "zbx1425_tgmt_signal.png";

        private static void Cfg(this Dictionary<string, string> configDict, string key, ref double param) {
            if (configDict.ContainsKey(key)) {
                var value = configDict[key].ToLowerInvariant();
                if (value == "inf") {
                    param = LessInf;
                } else if (value == "-inf") {
                    param = -LessInf;
                } else {
                    double result;
                    if (!double.TryParse(configDict[key], out result)) return;
                    param = result;
                }
            }
        }

        private static void Cfg(this Dictionary<string, string> configDict, string key, ref bool param) {
            if (configDict.ContainsKey(key)) {
                var str = configDict[key].ToLowerInvariant();
                param = (str == "true" || str == "1");
            }
        }

        private static void Cfg(this Dictionary<string, string> configDict, string key, ref string param) {
            if (configDict.ContainsKey(key)) {
                param = configDict[key];
            }
        }

        private static void Cfg(this Dictionary<string, string> configDict, string key, ref int[] param) {
            if (configDict.ContainsKey(key)) {
                var outputList = new List<int>();
                foreach (var value in configDict[key].Split(',')) {
                    int result;
                    if (!int.TryParse(value.Trim(), out result)) return;
                    outputList.Add(result);
                }
                param = outputList.ToArray();
            }
        }

        public static void Load(string path) {
            if (!File.Exists(path)) return;

            var dict = new Dictionary<string, string>();
            StreamReader configFile = File.OpenText(path);
            string line;
            while ((line = configFile.ReadLine()) != null) {
                line = line.Trim();
                if (line.Length > 0 && line[0] != '#') {
                    string[] commentTokens = line.Split('#');
                    string[] tokens = commentTokens[0].Trim().Split('=');
                    dict.Add(tokens[0].Trim().ToLowerInvariant(), tokens[1].Trim());
                }
            }
            configFile.Close();

            dict.Cfg("debug", ref Debug);

            dict.Cfg("maxspeed", ref MaxSpeed);
            dict.Cfg("recommendspeedoffset", ref RecommendSpeedOffset);
            dict.Cfg("recommenddeceleration", ref RecommendDeceleration);
            dict.Cfg("maxservicedeceleration", ref MaxServiceDeceleration);
            dict.Cfg("ebpatterndeceleration", ref EbPatternDeceleration);

            dict.Cfg("ctcsafetydistance", ref CTCSafetyDistance);
            dict.Cfg("reversestepdistance", ref ReverseStepDistance);
            dict.Cfg("doorenablewindow", ref DoorEnableWindow);
            dict.Cfg("releasespeed", ref ReleaseSpeed);
            dict.Cfg("rmspeed", ref RMSpeed);
            dict.Cfg("releasespeeddistance", ref ReleaseSpeedDistance);
            dict.Cfg("stationstartdistance", ref StationStartDistance);
            dict.Cfg("stationenddistance", ref StationEndDistance);
            dict.Cfg("stationmotionendpoint", ref StationMotionEndpoint);
            dict.Cfg("departrequesttime", ref DepartRequestTime);
            dict.Cfg("closerequestshowtime", ref CloseRequestShowTime);
            dict.Cfg("trainholdshowtime", ref TrainHoldShowTime);

            dict.Cfg("trainlength", ref TrainLength);

            dict.Cfg("tdtfreezedistance", ref TDTFreezeDistance);
            dict.Cfg("modeselecttimeout", ref ModeSelectTimeout);

            dict.Cfg("harmonypath", ref HarmonyPath);
            dict.Cfg("imageassetpath", ref ImageAssetPath);
            HarmonyPath = Path.GetFullPath(Path.Combine(PluginDir, HarmonyPath));
            ImageAssetPath = Path.GetFullPath(Path.Combine(PluginDir, ImageAssetPath));

            dict.Cfg("hmiimagesuffix", ref HMIImageSuffix);
            dict.Cfg("tdtimagesuffix", ref TDTImageSuffix);
            dict.Cfg("signalimagesuffix", ref SignalImageSuffix);

            var accelText = "0:3.3";
            dict.Cfg("acceleration", ref accelText);
            foreach (var tokens in accelText.Split(',').Select(t => t.Split(':'))) {
                var speed = int.Parse(tokens[0].Trim());
                var accel = double.Parse(tokens[1].Trim());
                Acceleration.Add(new KeyValuePair<int, double>(speed, accel));
            }
            Acceleration.Sort((a, b) => a.Key.CompareTo(b.Key));
        }

    }
}
