using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>Banners naming each active box effect, plus the full-screen overlays that sell them.</summary>
    public class EffectHud : MonoBehaviour
    {
        [Serializable]
        public class BannerView
        {
            public GameObject root;
            public Image background;
            public TMP_Text label;
            public RectTransform barFill;
            public Image barFillImage;
        }

        public BannerView[] banners;
        public RectTransform overlayContainer;
        public Image shockFlash;

        /// <summary>Share of the artwork's height cropped from the bottom (where the hand-drawn box is painted).</summary>
        const float OverlayBottomCrop = 0.3f;

        readonly Dictionary<BoxEffect, Graphic> overlays = new Dictionary<BoxEffect, Graphic>();
        readonly Dictionary<BoxEffect, float> active = new Dictionary<BoxEffect, float>();
        readonly List<BoxEffect> order = new List<BoxEffect>();
        readonly List<BoxEffect> stale = new List<BoxEffect>();

        PlayerStatus status;
        float shock;

        public void Bind(PlayerStatus playerStatus)
        {
            if (status != null) status.Shocked -= OnShocked;
            status = playerStatus;
            if (status != null) status.Shocked += OnShocked;
        }

        void OnDestroy()
        {
            if (status != null) status.Shocked -= OnShocked;
        }

        void OnShocked() => shock = 1f;

        void Update()
        {
            if (status == null) return;

            CollectActiveEffects();
            UpdateBanners();
            UpdateOverlays();
            UpdateShockFlash();
        }

        void CollectActiveEffects()
        {
            active.Clear();
            order.Clear();

            var slots = status.Inventory.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.box.effects == null) continue;

                var ctx = new EffectContext(status, slot.heldTime, 0f, status.Hazards);
                foreach (var effect in slot.box.effects)
                {
                    if (effect == null) continue;
                    float severity = effect.Severity01(ctx);
                    if (active.TryGetValue(effect, out float existing))
                    {
                        active[effect] = Mathf.Max(existing, severity);
                    }
                    else
                    {
                        active[effect] = severity;
                        order.Add(effect);
                    }
                }
            }
        }

        void UpdateBanners()
        {
            for (int i = 0; i < banners.Length; i++)
            {
                var view = banners[i];
                bool show = i < order.Count;
                if (view.root.activeSelf != show) view.root.SetActive(show);
                if (!show) continue;

                var effect = order[i];
                float severity = active[effect];
                UiUtil.SetText(view.label, Localization.Get(effect.displayNameKey));
                view.barFill.anchorMax = new Vector2(severity, 1f);
                view.barFillImage.color = effect.hudColor;
                var bg = effect.hudColor;
                bg.a = 0.25f;
                view.background.color = bg;
            }
        }

        void UpdateOverlays()
        {
            float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f);

            foreach (var effect in order)
            {
                if (effect.overlay == EffectOverlay.None) continue;
                var image = GetOverlay(effect);
                float alpha = effect.overlay == EffectOverlay.Fog
                    ? status.VisionObstruction
                    : Mathf.Lerp(0.3f, 1f, active[effect]) * Mathf.Min(1f, pulse);
                SetOverlay(image, OverlayColor(effect), alpha);
            }

            stale.Clear();
            foreach (var pair in overlays)
            {
                if (active.ContainsKey(pair.Key)) continue;

                float alpha = pair.Key.overlay == EffectOverlay.Fog
                    ? status.VisionObstruction
                    : Mathf.MoveTowards(pair.Value.color.a, 0f, Time.unscaledDeltaTime * 2f);
                SetOverlay(pair.Value, pair.Value.color, alpha);
                if (alpha <= 0.001f) stale.Add(pair.Key);
            }
            foreach (var effect in stale) overlays[effect].gameObject.SetActive(false);
        }

        Graphic GetOverlay(BoxEffect effect)
        {
            if (!overlays.TryGetValue(effect, out var graphic))
            {
                bool art = effect.screenOverlay != null && effect.overlay != EffectOverlay.Fog;
                var go = new GameObject("Overlay_" + effect.name, typeof(RectTransform), art ? typeof(RawImage) : typeof(Image));
                go.transform.SetParent(overlayContainer, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;

                if (art)
                {
                    var raw = go.GetComponent<RawImage>();
                    raw.texture = effect.screenOverlay;
                    raw.uvRect = new Rect(0f, OverlayBottomCrop, 1f, 1f - OverlayBottomCrop);
                    graphic = raw;
                }
                else
                {
                    var image = go.GetComponent<Image>();
                    image.sprite = effect.overlay == EffectOverlay.Fog ? ProceduralSprites.Fog : ProceduralSprites.Vignette;
                    graphic = image;
                }
                graphic.raycastTarget = false;
                overlays[effect] = graphic;
            }
            if (!graphic.gameObject.activeSelf) graphic.gameObject.SetActive(true);
            return graphic;
        }

        /// <summary>Hand-drawn artwork keeps its own colours; generated overlays are tinted by the effect.</summary>
        static Color OverlayColor(BoxEffect effect) =>
            effect.screenOverlay != null && effect.overlay != EffectOverlay.Fog ? Color.white : effect.hudColor;

        static void SetOverlay(Graphic image, Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        void UpdateShockFlash()
        {
            if (shockFlash == null) return;
            shock = Mathf.MoveTowards(shock, 0f, Time.unscaledDeltaTime * 3f);
            var c = shockFlash.color;
            c.a = shock * 0.6f;
            shockFlash.color = c;
        }
    }
}
