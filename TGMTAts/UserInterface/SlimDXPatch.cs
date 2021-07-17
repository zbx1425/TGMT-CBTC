using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using SlimDX.Direct3D9;
using HarmonyLib;

namespace TGMTAts {

    [HarmonyPatch(typeof(Texture))]
    [HarmonyPatch("FromFile")]
    [HarmonyPatch(new Type[] { typeof(Device), typeof(string), typeof(int), typeof(int), typeof(int), typeof(Usage),
        typeof(Format), typeof(Pool), typeof(Filter), typeof(Filter), typeof(int) })]
    class SlimDXTexturePatch {

        static void Postfix(ref Texture __result, Device device, string fileName) {
            // zbx1425.tgmttdt.png
            if (fileName.EndsWith("stop6.png", StringComparison.OrdinalIgnoreCase)) {
                __result = TextureManager.CreateTdtTexture(device);
            } else if (fileName.EndsWith("zbx1425.tgmthmi.png", StringComparison.OrdinalIgnoreCase)) {
                __result = TextureManager.CreateHmiTexture(device);
            }
        }
    }

    static class TextureManager {
        
        public static Texture HmiTexture, TdtTexture, SignalTexture;

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
