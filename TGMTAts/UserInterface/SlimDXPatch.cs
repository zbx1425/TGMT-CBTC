using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using HarmonyLib;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using SlimDX.Direct3D9;

namespace TGMTAts {

    // SlimDX methods are called with reflection, to avoid direct reference to SlimDX.dll

    class TextureManager {

        public static Texture HmiTexture, TdtTexture, SignalTexture;

        public static void ApplyPatch() {
            TGMTAts.harmony.Patch(typeof(Texture).GetMethods()
                .Where(m => m.Name == "FromFile" && m.GetParameters().Length == 11)
                .FirstOrDefault(), 
                null, new HarmonyMethod(typeof(TextureManager), "FromFilePostfix")
            );
        }

        private static void FromFilePostfix(ref Texture __result, Device device, string fileName) {
            if (fileName.EndsWith(Config.TDTImageSuffix, StringComparison.OrdinalIgnoreCase)) {
                __result = TextureManager.CreateTdtTexture(device);
            } else if (fileName.EndsWith(Config.HMIImageSuffix, StringComparison.OrdinalIgnoreCase)) {
                __result = TextureManager.CreateHmiTexture(device);
            }
        }

        public static void Dispose() {
            HmiTexture?.Dispose();
            TdtTexture?.Dispose();
            SignalTexture?.Dispose();
            HmiTexture = null;
            TdtTexture = null;
            SignalTexture = null;
        }

        public static Texture CreateHmiTexture(Device device) {
            if (HmiTexture != null) return HmiTexture;
            HmiTexture = new Texture(device, 1024, 1024, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
            return HmiTexture;
        }

        public static Texture CreateTdtTexture(Device device) {
            if (TdtTexture != null) return TdtTexture;
            TdtTexture = new Texture(device, 32, 32, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);
            return TdtTexture;
        }

        public static void UpdateTexture(Texture texture, Bitmap bmp) {
            if (texture == null) return;
            var desc = texture.GetLevelDescription(0);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);

            var rect = texture.LockRectangle(0, LockFlags.Discard);
            if (rect.Data.CanWrite) rect.Data.WriteRange(bmpData.Scan0, rect.Pitch * desc.Height);
            bmp.UnlockBits(bmpData);
            texture.UnlockRectangle(0);
        }
    }
}
