using System;
using System.Collections.Generic;
using System.IO;

namespace TGMTAts {

    public static class Config {

        public const double LessInf = 1000000;

        public static bool Debug = false;
        public static int Variant = 0;

        public static double MaxSpeed = 100;
        public static double RecommendSpeedOffset = -5;
        public static double RecommendDeceleration = -2;
        public static double MaxServiceDeceleration = -4;

        public static double MaxReverseDistance = 5;
        public static double DoorEnableWindow = 1;

        public static int[] SignalAspectSpeed = { 0, 25, 45, 65 };
        public static double ReleaseSpeedDistance = 100;
        public static double StationStartDistance = 100;
        public static double StationEndDistance = 5;
        public static double StationMotionEndpoint = 3;
        public static double StationDepartRequestTime = 10;

        public static double TDTFreezeDistance = 10;
        public static double TDTMaximumShowingTime = LessInf;
        public static int[] TDTOverflowBehavior = { 0, 0 };

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
            var dict = new Dictionary<string, string>();
            StreamReader configFile = File.OpenText(path);
            string line;
            while ((line = configFile.ReadLine()) != null) {
                line = line.Trim();
                if (line.Length > 0 && line[0] != '#') {
                    string[] tokens = line.Split('=');
                    dict.Add(tokens[0].Trim().ToLowerInvariant(), tokens[1].Trim());
                }
            }
            configFile.Close();

            dict.Cfg("debug", ref Debug);
            dict.Cfg("maxspeed", ref MaxSpeed);
            dict.Cfg("recommendspeedoffset", ref RecommendSpeedOffset);
            dict.Cfg("recommenddeceleration", ref RecommendDeceleration);
            dict.Cfg("maxreversedistance", ref MaxReverseDistance);
            dict.Cfg("doorenablewindow", ref DoorEnableWindow);
            dict.Cfg("signalaspectspeed", ref SignalAspectSpeed);
            dict.Cfg("releasespeeddistance", ref ReleaseSpeedDistance);
            dict.Cfg("stationstartdistance", ref StationStartDistance);
            dict.Cfg("stationenddistance", ref StationEndDistance);
            dict.Cfg("stationmotionendpoint", ref StationMotionEndpoint);
            dict.Cfg("stationdepartrequesttime", ref StationDepartRequestTime);
            dict.Cfg("freezedistance", ref TDTFreezeDistance);
            dict.Cfg("maximumshowingtime", ref TDTMaximumShowingTime);
            dict.Cfg("overflowbehavior", ref TDTOverflowBehavior);
            if (dict.ContainsKey("variant")) {
                switch (dict["variant"].ToLowerInvariant()) {
                    case "siemens":
                        Variant = 0;
                        break;
                    case "casco":
                        Variant = 1;
                        break;
                }
            }
        }

    }
}
