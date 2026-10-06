using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MixedUp.EditorTools
{
    public static partial class PrototypeBuilder
    {
        static readonly Color Ink = UiFactory.Ink;

        static void BuildHud(GameAssets a, Prefabs prefabs, Truck truck, TruckPuzzleController puzzle)
        {
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var ui = canvasGo.AddComponent<UIManager>();

            var hudRoot = UiFactory.NewRect("HudRoot", canvasGo.transform);
            UiFactory.Stretch(hudRoot);
            ui.hudRoot = hudRoot.gameObject;

            var overlays = UiFactory.NewRect("Overlays", hudRoot);
            UiFactory.Stretch(overlays);
            var shockFlash = UiFactory.NewImage("ShockFlash", hudRoot, new Color(1f, 0.95f, 0.5f, 0f));
            UiFactory.Stretch(shockFlash.rectTransform);
            var damageFlash = UiFactory.NewImage("DamageFlash", hudRoot, new Color(0.8f, 0.1f, 0.1f, 0f));
            UiFactory.Stretch(damageFlash.rectTransform);

            ui.healthHud = BuildHealth(hudRoot, damageFlash);
            ui.effectHud = BuildEffects(hudRoot, overlays, shockFlash);
            BuildOrder(hudRoot, a, truck);
            ui.inventoryHud = BuildInventory(hudRoot);
            ui.promptHud = BuildPrompt(hudRoot);
            BuildToast(hudRoot);

            BuildPausePanel(canvasGo.transform, ui);
            BuildGameOverPanel(canvasGo.transform, ui);
            BuildPuzzleUi(canvasGo.transform, ui, a, puzzle);
            ui.settingsPanel = BuildSettingsPanel(canvasGo.transform, a, prefabs.preview);

            // Save the scene tidy: only the HUD is visible, the other screens open when needed.
            ui.pausePanel.SetActive(false);
            ui.gameOverPanel.SetActive(false);
            ui.puzzlePanel.SetActive(false);
            ui.resultsPanel.SetActive(false);
            ui.manualPanel.gameObject.SetActive(false);
            ui.settingsPanel.gameObject.SetActive(false);
            ui.wipe.sheet.gameObject.SetActive(false);
        }

        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();

            // AddComponent assigns an in-memory default asset that is not saved with the scene,
            // so bind the UI actions of the project's real input asset explicitly.
            const string path = "Assets/InputSystem_Actions.inputactions";
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            var references = AssetDatabase.LoadAllAssetsAtPath(path).OfType<InputActionReference>()
                .Where(r => r.action != null && r.action.actionMap != null && r.action.actionMap.name == "UI")
                .GroupBy(r => r.action.name)
                .ToDictionary(g => g.Key, g => g.First());

            InputActionReference Ref(string action) => references.TryGetValue(action, out var r) ? r : null;

            module.actionsAsset = asset;
            module.point = Ref("Point");
            module.leftClick = Ref("Click");
            module.rightClick = Ref("RightClick");
            module.middleClick = Ref("MiddleClick");
            module.scrollWheel = Ref("ScrollWheel");
            module.move = Ref("Navigate");
            module.submit = Ref("Submit");
            module.cancel = Ref("Cancel");
            module.trackedDevicePosition = Ref("TrackedDevicePosition");
            module.trackedDeviceOrientation = Ref("TrackedDeviceOrientation");

            Debug.Log("[MixedUp] UI input module bound to " + (asset != null ? asset.name : "NOTHING") +
                      " with " + references.Count + " UI action references");
        }

        // ---------------------------------------------------------------- health

        static HealthHud BuildHealth(RectTransform parent, Image damageFlash)
        {
            var inner = UiFactory.Panel("HealthPanel", parent, new Vector2(440f, 100f), out var root);
            UiFactory.Place(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(440f, 100f));

            var title = UiFactory.NewText("Title", inner, "", 30f, Ink, TextAlignmentOptions.TopLeft, "ui.hp");
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -8f), new Vector2(200f, 36f));
            var value = UiFactory.NewText("Value", inner, "100", 30f, Ink, TextAlignmentOptions.TopRight);
            UiFactory.Place(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -8f), new Vector2(120f, 36f));

            var barBg = UiFactory.NewSprite("BarBackground", inner, "slider_track", sliced: true, slicedScale: 1.5f);
            barBg.color = new Color(1f, 1f, 1f, 0.55f);
            UiFactory.Place(barBg.rectTransform, Vector2.zero, Vector2.zero, new Vector2(18f, 12f), new Vector2(394f, 30f));
            var fill = UiFactory.NewSprite("Fill", barBg.transform, "bar_brick", sliced: true, slicedScale: 1.6f);
            UiFactory.Stretch(fill.rectTransform);

            var hud = root.gameObject.AddComponent<HealthHud>();
            hud.fill = fill.rectTransform;
            hud.valueLabel = value;
            hud.damageFlash = damageFlash;
            return hud;
        }

        // --------------------------------------------------------------- effects

        static EffectHud BuildEffects(RectTransform parent, RectTransform overlays, Image shockFlash)
        {
            var row = UiFactory.NewRect("EffectBanners", parent);
            UiFactory.Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(620f, 260f));
            var layout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var hud = row.gameObject.AddComponent<EffectHud>();
            hud.overlayContainer = overlays;
            hud.shockFlash = shockFlash;
            hud.banners = new EffectHud.BannerView[4];

            for (int i = 0; i < hud.banners.Length; i++)
            {
                var bannerRect = UiFactory.NewRect("Banner" + (i + 1), row);
                bannerRect.sizeDelta = new Vector2(600f, 58f);
                var bg = UiFactory.NewSprite("Background", bannerRect, "paper", sliced: true, slicedScale: 5f);
                bg.color = new Color(Ink.r, Ink.g, Ink.b, 0.2f);
                UiFactory.Stretch(bg.rectTransform);
                var label = UiFactory.NewText("Label", bannerRect, "", 30f, Ink);
                UiFactory.Stretch(label.rectTransform, 8f);

                var barBg = UiFactory.NewSprite("BarBackground", bannerRect, "slider_track", sliced: true, slicedScale: 3f);
                barBg.color = new Color(1f, 1f, 1f, 0.5f);
                UiFactory.Place(barBg.rectTransform, Vector2.zero, Vector2.zero, new Vector2(10f, 4f), new Vector2(580f, 12f));
                var fill = UiFactory.NewSprite("Fill", barBg.transform, "brush_white", sliced: true, slicedScale: 3f);
                UiFactory.Stretch(fill.rectTransform);

                hud.banners[i] = new EffectHud.BannerView
                {
                    root = bannerRect.gameObject,
                    background = bg,
                    label = label,
                    barFill = fill.rectTransform,
                    barFillImage = fill
                };
                bannerRect.gameObject.SetActive(false);
            }
            return hud;
        }

        // ----------------------------------------------------------------- order

        static void BuildOrder(RectTransform parent, GameAssets a, Truck truck)
        {
            int lines = a.order.lines.Length;
            float height = 70f + lines * 60f;
            var inner = UiFactory.Panel("OrderPanel", parent, new Vector2(400f, height), out var root);
            UiFactory.Place(root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(400f, height));

            var title = UiFactory.NewText("Title", inner, "", 34f, Ink, TextAlignmentOptions.Top, "ui.order");
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(360f, 44f));

            var rows = UiFactory.NewRect("Rows", inner);
            UiFactory.Place(rows, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(380f, lines * 60f));
            var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var template = UiFactory.NewRect("RowTemplate", rows);
            template.sizeDelta = new Vector2(0f, 54f);
            var icon = UiFactory.NewImage("Icon", template, Color.white);
            UiFactory.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(50f, 50f));
            icon.preserveAspect = true;
            var nameLabel = UiFactory.NewText("Name", template, "", 28f, Ink, TextAlignmentOptions.MidlineLeft);
            UiFactory.Place(nameLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(66f, 0f), new Vector2(190f, 40f));
            var countLabel = UiFactory.NewText("Count", template, "", 30f, Ink, TextAlignmentOptions.MidlineRight);
            UiFactory.Place(countLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(90f, 40f));
            var strike = UiFactory.NewImage("Strike", template, Ink);
            strike.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            strike.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            strike.rectTransform.offsetMin = new Vector2(8f, -2f);
            strike.rectTransform.offsetMax = new Vector2(-8f, 2f);
            strike.enabled = false;

            var rowView = template.gameObject.AddComponent<OrderRowView>();
            rowView.icon = icon;
            rowView.nameLabel = nameLabel;
            rowView.countLabel = countLabel;
            rowView.strikeThrough = strike;

            var hud = root.gameObject.AddComponent<TruckOrderHud>();
            hud.truck = truck;
            hud.rowContainer = rows;
            hud.rowTemplate = rowView;
        }

        // ------------------------------------------------------------- inventory

        static InventoryHud BuildInventory(RectTransform parent)
        {
            var row = UiFactory.NewRect("Inventory", parent);
            UiFactory.Place(row, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(900f, 230f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var hud = row.gameObject.AddComponent<InventoryHud>();
            hud.slots = new InventoryHud.SlotView[GameInput.MaxSlots];

            for (int i = 0; i < hud.slots.Length; i++)
            {
                var inner = UiFactory.Panel("Slot" + (i + 1), row, new Vector2(190f, 210f), out var root);

                var selection = UiFactory.NewSprite("Selection", root, "highlight", sliced: true, slicedScale: 2.2f);
                UiFactory.Stretch(selection.rectTransform, -22f);
                selection.transform.SetAsFirstSibling();

                var icon = UiFactory.NewImage("Icon", inner, Color.white);
                UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(150f, 120f));
                icon.preserveAspect = true;

                var hint = UiFactory.NewText("KeyHint", inner, (i + 1).ToString(), 24f, new Color(Ink.r, Ink.g, Ink.b, 0.6f), TextAlignmentOptions.TopLeft);
                UiFactory.Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -6f), new Vector2(30f, 30f));

                var label = UiFactory.NewText("Name", inner, "", 28f, Ink);
                UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(180f, 40f));

                var barBg = UiFactory.NewSprite("SeverityBackground", inner, "slider_track", sliced: true, slicedScale: 2.5f);
                barBg.color = new Color(1f, 1f, 1f, 0.5f);
                UiFactory.Place(barBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(150f, 18f));
                var fill = UiFactory.NewSprite("Fill", barBg.transform, "brush_white", sliced: true, slicedScale: 2.5f);
                fill.color = new Color(0.55f, 0.75f, 0.45f);
                UiFactory.Stretch(fill.rectTransform);

                hud.slots[i] = new InventoryHud.SlotView
                {
                    root = root.gameObject,
                    icon = icon,
                    label = label,
                    selection = selection,
                    severityFill = fill.rectTransform,
                    severityFillImage = fill
                };
            }
            return hud;
        }

        // ---------------------------------------------------------------- prompt

        static PromptHud BuildPrompt(RectTransform parent)
        {
            var inner = UiFactory.Panel("PromptPanel", parent, new Vector2(1000f, 70f), out var root, false);
            UiFactory.Place(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 270f), new Vector2(1000f, 70f));
            var label = UiFactory.NewText("Prompt", inner, "", 38f, Ink);
            UiFactory.Stretch(label.rectTransform, 6f);

            var hud = parent.gameObject.AddComponent<PromptHud>();
            hud.label = label;
            hud.container = root.gameObject;
            root.gameObject.SetActive(false);
            return hud;
        }

        static void BuildToast(RectTransform parent)
        {
            var inner = UiFactory.Panel("Toast", parent, new Vector2(900f, 72f), out var root, false);
            UiFactory.Place(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(900f, 72f));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var label = UiFactory.NewText("Label", inner, "", 38f, Ink);
            UiFactory.Stretch(label.rectTransform, 6f);

            var toast = root.gameObject.AddComponent<ToastHud>();
            toast.label = label;
            toast.group = group;
        }

        // ---------------------------------------------------------------- panels

        static RectTransform FullScreenDim(string name, Transform parent, Color color)
        {
            var image = UiFactory.NewImage(name, parent, color);
            image.raycastTarget = true;
            UiFactory.Stretch(image.rectTransform);
            return image.rectTransform;
        }

        static RectTransform CenteredCard(Transform parent, Vector2 size, bool tape = true)
        {
            var inner = UiFactory.Panel("Card", parent, size, out var root, true, tape);
            UiFactory.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);

            var layout = inner.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(70, 70, 60, 60);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return inner;
        }

        static TextMeshProUGUI CardText(Transform card, string text, string key, float size, float height, Color color)
        {
            var label = UiFactory.NewText("Text", card, text, size, color, TextAlignmentOptions.Center, key);
            label.rectTransform.sizeDelta = new Vector2(0f, height);
            return label;
        }

        static Button CardButton(Transform card, string text, string key, float height,
            UiFactory.ButtonStyle style = UiFactory.ButtonStyle.SignRight)
        {
            var button = UiFactory.NewButton("Button", card, text, key, new Vector2(0f, height), style);
            return button;
        }

        static void BuildPausePanel(Transform canvas, UIManager ui)
        {
            var panel = FullScreenDim("PausePanel", canvas, new Color(0f, 0f, 0f, 0.55f));
            ui.pausePanel = panel.gameObject;
            var card = CenteredCard(panel, new Vector2(760f, 900f));
            card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(80, 80, 70, 60);

            CardText(card, "", "ui.paused", 100f, 112f, Ink);
            var underline = UiFactory.NewSprite("Underline", card, "underline");
            underline.rectTransform.sizeDelta = new Vector2(0f, 26f);

            ui.resumeButton = CardButton(card, "", "ui.resume", 100f);
            ui.settingsButton = CardButton(card, "", "ui.settings", 100f);
            ui.manualButton = CardButton(card, "", "ui.manual", 100f);
            ui.menuButton = CardButton(card, "", "ui.main_menu", 100f);
            ui.pauseQuitButton = CardButton(card, "", "ui.quit", 100f);
        }

        static void BuildGameOverPanel(Transform canvas, UIManager ui)
        {
            var panel = FullScreenDim("GameOverPanel", canvas, new Color(0.35f, 0.05f, 0.05f, 0.8f));
            ui.gameOverPanel = panel.gameObject;

            var card = UiFactory.Panel("Card", panel, new Vector2(1500f, 800f), out var root, true, true);
            UiFactory.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 800f));

            // The author's own drawing of the exploding boxes, on the left.
            var art = UiFactory.NewSprite("Art", card, "art_gameover");
            At(art.rectTransform, 70f, 130f, 560f, 560f);
            art.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);

            var title = UiFactory.NewText("Title", card, "", 130f, UiFactory.Danger, TextAlignmentOptions.Center, "ui.game_over");
            At(title.rectTransform, 660f, 70f, 760f, 150f);
            var cause = UiFactory.NewText("CauseTitle", card, "", 44f, new Color(Ink.r, Ink.g, Ink.b, 0.75f), TextAlignmentOptions.Center, "ui.death_cause");
            At(cause.rectTransform, 660f, 235f, 760f, 56f);
            ui.deathCauseLabel = UiFactory.NewText("Cause", card, "", 52f, Ink, TextAlignmentOptions.Center);
            At(ui.deathCauseLabel.rectTransform, 680f, 292f, 720f, 170f);
            ui.quipLabel = UiFactory.NewText("Quip", card, "", 36f, new Color(Ink.r, Ink.g, Ink.b, 0.7f), TextAlignmentOptions.Center);
            At(ui.quipLabel.rectTransform, 680f, 460f, 720f, 70f);

            ui.retryButton = UiFactory.NewButton("Retry", card, "", "ui.retry", new Vector2(520f, 100f));
            At((RectTransform)ui.retryButton.transform, 690f, 545f, 520f, 100f);
            ui.gameOverMenuButton = UiFactory.NewButton("Menu", card, "", "ui.main_menu", new Vector2(520f, 100f));
            At((RectTransform)ui.gameOverMenuButton.transform, 690f, 650f, 520f, 96f);
        }
    }
}
