using BveEx.Extensions.MapStatements;
using BveEx.PluginHost;
using BveEx.PluginHost.Plugins;
using BveEx.PluginHost.Plugins.Extensions;
using BveTypes.ClassWrappers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using TGMT_CBTC.HMI;
using TGMT_CBTC.OBCU;

namespace TGMT_CBTC {

    [Plugin(PluginType.Extension)]
    public class TGMT : AssemblyPluginBase, IExtension {

        private Scenario Scenario { get; set; }

        public OBCU.OBCU OBCU { get; private set; }

        private HMIWindow hmiWindow;
        private KeyHandler keyHandler = new KeyHandler();

        public TGMT(PluginBuilder builder) : base(builder) {
            BveHacker.ScenarioCreated += OnScenarioCreated;
            BveHacker.ScenarioClosed += OnScenarioClosed;
            TGMTPainter.Initialize();
            keyHandler.Init(BveHacker);
            keyHandler.AtoStartPressed += OnAtoStartPressed;
        }

        private void OnScenarioCreated(ScenarioCreatedEventArgs e) {
            Scenario = e.Scenario;
            OBCU = new OBCU.OBCU(Scenario);
            hmiWindow = new HMIWindow();
            hmiWindow.Show();
        }
        private void OnScenarioClosed(EventArgs e) {
            Scenario = null;
            OBCU = null;
            if (hmiWindow != null) hmiWindow.Close();
            hmiWindow = null;
        }

        TimeSpan paintTimeDelay = TimeSpan.FromMilliseconds(1000);

        public override void Tick(TimeSpan elapsed) {
            if (OBCU == null) return;
            OBCU.Tick(elapsed, Scenario);
            paintTimeDelay += elapsed;
            if (paintTimeDelay >= TimeSpan.FromMilliseconds(1000 / 8)) {
                Bitmap hmiBitmap = TGMTPainter.PaintHMI(OBCU).Bitmap;
                hmiWindow.DrawBitmap(hmiBitmap);
                hmiWindow.HandleHMIInfo(OBCU);
                paintTimeDelay = TimeSpan.Zero;
            }
        }

        private void OnAtoStartPressed(object sender, EventArgs e) {
            OBCU.OnAtoStartPressed(Scenario);
        }

        public override void Dispose() {
            TGMTPainter.Dispose();
            if (hmiWindow != null) hmiWindow.Dispose();
        }
    }
}
