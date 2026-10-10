using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace MixedUp
{
    /// <summary>
    /// Keeps the URP "GPU Resident Drawer" switched off. On some laptops (an integrated Intel GPU next to an NVIDIA one) it makes the
    /// editor crash natively as soon as a level with many objects starts. The game does not need it, so whatever quality level or
    /// pipeline asset is active, it is turned off when the game starts. Done by reflection so no render pipeline assembly is needed.
    /// </summary>
    public static class RenderSafety
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void DisableGpuResidentDrawer()
        {
            Disable(GraphicsSettings.currentRenderPipeline);
            Disable(QualitySettings.renderPipeline);
        }

        static void Disable(RenderPipelineAsset asset)
        {
            if (asset == null) return;
            try
            {
                var property = asset.GetType().GetProperty("gpuResidentDrawerMode", BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanWrite) return;
                var current = property.GetValue(asset);
                if (current == null || Convert.ToInt32(current) == 0) return;
                property.SetValue(asset, Enum.ToObject(property.PropertyType, 0));
            }
            catch (Exception)
            {
                // A different render pipeline version: nothing to turn off.
            }
        }
    }
}
