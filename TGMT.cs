using AtsEx.Extensions.MapStatements;
using AtsEx.PluginHost;
using AtsEx.PluginHost.Plugins;
using AtsEx.PluginHost.Plugins.Extensions;
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

        private HMIWindow hmiWindow = new HMIWindow();
        private KeyHandler keyHandler = new KeyHandler();

        public TGMT(PluginBuilder builder) : base(builder) {
            BveHacker.ScenarioCreated += OnScenarioCreated;
            TGMTPainter.Initialize();
            hmiWindow.Show();
            keyHandler.Init(BveHacker);
            keyHandler.AtoStartPressed += OnAtoStartPressed;
        }

        private void OnScenarioCreated(ScenarioCreatedEventArgs e) {
            Scenario = e.Scenario;
            OBCU = new OBCU.OBCU(Scenario);
        }

        TimeSpan paintTimeDelay = TimeSpan.FromMilliseconds(1000);

        public override TickResult Tick(TimeSpan elapsed) {
            OBCU.Tick(elapsed, Scenario);
            paintTimeDelay += elapsed;
            if (paintTimeDelay >= TimeSpan.FromMilliseconds(1000 / 8)) {
                Bitmap hmiBitmap = TGMTPainter.PaintHMI(OBCU).Bitmap;
                hmiWindow.DrawBitmap(hmiBitmap);
                paintTimeDelay = TimeSpan.Zero;
            }
            return new ExtensionTickResult();
        }

        private void OnAtoStartPressed(object sender, EventArgs e) {
            OBCU.OnAtoStartPressed(Scenario);
        }

        public override void Dispose() {
            TGMTPainter.Dispose();
            hmiWindow.Dispose();
        }
    }
}
