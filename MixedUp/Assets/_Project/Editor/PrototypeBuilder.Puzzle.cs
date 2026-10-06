using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp.EditorTools
{
    public static partial class PrototypeBuilder
    {
        const string TruckSidePath = Root + "/Art/UI/Puzzle/truck_side.png";
        const int MaxPuzzleSlots = 8;

        // ----------------------------------------------------------------- data

        /// <summary>The full 5x5 table. HOT+ELECTRIC explodes, TOXIC+ELECTRIC is lethal, HOT+TOXIC and HOT+HOT are dangerous.</summary>
        static CombinationRules CreateRules(GameAssets a)
        {
            return LoadOrCreate<CombinationRules>(DataDir + "/CombinationRules.asset", r =>
            {
                var n = a.normal; var h = a.hot; var e = a.electric; var f = a.frozen; var t = a.toxic;
                r.rules = new[]
                {
                    Rule(n, n, CombinationOutcome.Safe, null, true),
                    Rule(h, h, CombinationOutcome.Danger, "hint.hot_hot", false),
                    Rule(e, e, CombinationOutcome.Safe, null, true),
                    Rule(f, f, CombinationOutcome.Safe, null, true),
                    Rule(t, t, CombinationOutcome.Safe, null, true),
                    Rule(h, e, CombinationOutcome.Explosion, "hint.hot_electric", false),
                    Rule(h, f, CombinationOutcome.Safe, "hint.hot_frozen", false),
                    Rule(h, n, CombinationOutcome.Safe, "hint.hot_normal", true),
                    Rule(t, e, CombinationOutcome.GameOver, "hint.toxic_electric", false),
                    Rule(f, e, CombinationOutcome.Safe, "hint.frozen_electric", false),
                    Rule(h, t, CombinationOutcome.Danger, "hint.hot_toxic", false),
                    Rule(n, e, CombinationOutcome.Safe, "hint.normal_electric", true),
                    Rule(n, f, CombinationOutcome.Safe, "hint.normal_frozen", true),
                    Rule(n, t, CombinationOutcome.Safe, "hint.normal_toxic", true),
                    Rule(f, t, CombinationOutcome.Safe, "hint.frozen_toxic", false)
                };
            });
        }

        static CombinationRule Rule(BoxData x, BoxData y, CombinationOutcome outcome, string hintKey, bool known) =>
            new CombinationRule { a = x, b = y, outcome = outcome, hintKey = hintKey, knownByDefault = known };

        // ---------------------------------------------------------- level notes

        /// <summary>A signpost with a notice (the hand-made sign model), with a "!" floating above it.</summary>
        static GameObject BuildLoreNote(ArtAssets art)
        {
            var root = new GameObject("LoreNote");
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.size = new Vector3(1.1f, 2f, 1.1f);

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            PrefabUtility.InstantiatePrefab(art.cartel, visual);

            var mark = new GameObject("Mark", typeof(Billboard));
            mark.transform.SetParent(root.transform, false);
            mark.transform.localPosition = new Vector3(0f, 3.1f, 0f);
            var text = mark.AddComponent<TextMeshPro>();
            text.text = "!";
            text.fontSize = 9f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.75f, 0.25f, 0.15f);
            text.rectTransform.sizeDelta = new Vector2(2f, 2f);

            var note = root.AddComponent<LoreNote>();
            note.visual = visual;
            note.bobHeight = 0f;
            return root;
        }

        static void PlaceNote(Transform parent, GameObject prefab, CombinationRules rules, BoxData a, BoxData b, Vector3 position, string name)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.position = position;
            var note = instance.GetComponent<LoreNote>();
            note.rules = rules;
            note.a = a;
            note.b = b;
            EditorUtility.SetDirty(note);
            PrefabUtility.RecordPrefabInstancePropertyModifications(note);
        }

        static Sprite LoadUiSprite(string path)
        {
            if (!System.IO.File.Exists(path)) return null;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 512;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------------ UI

        static void BuildPuzzleUi(Transform canvas, UIManager ui, GameAssets a, TruckPuzzleController controller)
        {
            var screen = BuildPuzzleScreen(canvas, controller);
            ui.puzzlePanel = screen.gameObject;
            BuildResultsPanel(canvas, ui);
            var manual = BuildManualPanel(canvas, a);
            ui.manualPanel = manual;
            screen.manualPanel = manual;
            ui.wipe = BuildWipe(canvas);
        }

        static TruckPuzzleScreen BuildPuzzleScreen(Transform canvas, TruckPuzzleController controller)
        {
            var bg = FullScreenDim("PuzzleScreen", canvas, new Color(0.99f, 0.82f, 0.56f, 1f));
            var screen = bg.gameObject.AddComponent<TruckPuzzleScreen>();
            screen.controller = controller;

            screen.titleLabel = UiFactory.NewText("Title", bg, "", 84f, Ink, TextAlignmentOptions.Center, "puzzle.title_arrange");
            UiFactory.Place(screen.titleLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1700f, 120f));

            screen.alertLabel = UiFactory.NewText("Alert", bg, "", 54f, new Color(0.75f, 0.2f, 0.1f));
            UiFactory.Place(screen.alertLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(1700f, 80f));
            screen.alertLabel.gameObject.SetActive(false);

            var cargo = UiFactory.Panel("CargoHold", bg, new Vector2(1840f, 400f), out var cargoRoot);
            UiFactory.Place(cargoRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(1840f, 400f));
            var row = cargo.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 0f;
            row.padding = new RectOffset(20, 20, 20, 20);
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            screen.slots = new PuzzleSlotView[MaxPuzzleSlots];
            screen.junctions = new PuzzleJunctionView[MaxPuzzleSlots - 1];
            for (int i = 0; i < MaxPuzzleSlots; i++)
            {
                screen.slots[i] = BuildPuzzleSlot(cargo, i);
                if (i < MaxPuzzleSlots - 1) screen.junctions[i] = BuildJunction(cargo, i);
            }

            screen.hintLabel = UiFactory.NewText("Hint", bg, "", 42f, Ink);
            UiFactory.Place(screen.hintLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(1700f, 70f));

            BuildTravelBar(bg, screen);

            screen.manualButton = UiFactory.NewButton("ManualButton", bg, "", "ui.manual", new Vector2(400f, 100f));
            UiFactory.Place((RectTransform)screen.manualButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-280f, 60f), new Vector2(400f, 100f));
            screen.startButton = UiFactory.NewButton("StartButton", bg, "", "puzzle.start", new Vector2(480f, 110f));
            UiFactory.Place((RectTransform)screen.startButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(280f, 60f), new Vector2(480f, 110f));
            return screen;
        }

        static PuzzleSlotView BuildPuzzleSlot(RectTransform parent, int index)
        {
            var inner = UiFactory.Panel("Slot" + (index + 1), parent, new Vector2(168f, 240f), out var root);

            var selection = UiFactory.NewImage("Selection", root, UiFactory.Highlight);
            UiFactory.Stretch(selection.rectTransform, -10f);
            selection.transform.SetAsFirstSibling();
            selection.enabled = false;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = inner.GetComponent<Image>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.93f, 0.7f);
            colors.pressedColor = new Color(0.85f, 0.78f, 0.6f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            var icon = UiFactory.NewImage("Icon", inner, Color.white);
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(140f, 130f));
            icon.preserveAspect = true;

            var label = UiFactory.NewText("Name", inner, "", 28f, Ink);
            UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(150f, 60f));
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 30f;

            var view = root.gameObject.AddComponent<PuzzleSlotView>();
            view.button = button;
            view.icon = icon;
            view.label = label;
            view.selection = selection;
            return view;
        }

        static PuzzleJunctionView BuildJunction(RectTransform parent, int index)
        {
            var root = UiFactory.NewRect("Junction" + (index + 1), parent);
            root.sizeDelta = new Vector2(58f, 240f);

            var ring = UiFactory.NewImage("Ring", root, Ink);
            UiFactory.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(54f, 54f));
            var disc = UiFactory.NewImage("Disc", ring.transform, Color.white);
            UiFactory.Place(disc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
            var symbol = UiFactory.NewText("Symbol", disc.transform, "?", 26f, Ink);
            UiFactory.Stretch(symbol.rectTransform);

            var fuseRoot = UiFactory.NewImage("Fuse", root, new Color(Ink.r, Ink.g, Ink.b, 0.3f));
            UiFactory.Place(fuseRoot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(52f, 12f));
            var fill = UiFactory.NewImage("Fill", fuseRoot.transform, new Color(0.85f, 0.3f, 0.15f));
            UiFactory.Stretch(fill.rectTransform);
            fuseRoot.gameObject.SetActive(false);

            var marker = root.gameObject.AddComponent<OutcomeMarker>();
            marker.ring = ring;
            marker.disc = disc;
            marker.symbol = symbol;

            var view = root.gameObject.AddComponent<PuzzleJunctionView>();
            view.marker = marker;
            view.fuseRoot = fuseRoot.gameObject;
            view.fuseFill = fill.rectTransform;
            return view;
        }

        static void BuildTravelBar(RectTransform parent, TruckPuzzleScreen screen)
        {
            var travel = UiFactory.NewRect("Travel", parent);
            UiFactory.Place(travel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(1300f, 190f));

            screen.timeLabel = UiFactory.NewText("Time", travel, "", 56f, Ink, TextAlignmentOptions.MidlineLeft);
            UiFactory.Place(screen.timeLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(610f, 6f), new Vector2(150f, 64f));

            var track = UiFactory.NewImage("Track", travel, new Color(Ink.r, Ink.g, Ink.b, 0.25f));
            UiFactory.Place(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1200f, 26f));
            var fill = UiFactory.NewImage("Fill", track.transform, new Color(0.75f, 0.35f, 0.25f));
            UiFactory.Stretch(fill.rectTransform);

            var truck = UiFactory.NewImage("Truck", track.transform, Color.white);
            truck.sprite = LoadUiSprite(TruckSidePath);
            truck.preserveAspect = true;
            UiFactory.Place(truck.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(190f, 110f));

            screen.travelRoot = travel.gameObject;
            screen.travelTrack = track.rectTransform;
            screen.travelFill = fill.rectTransform;
            screen.truckIcon = truck.rectTransform;
            travel.gameObject.SetActive(false);
        }

        static void BuildResultsPanel(Transform canvas, UIManager ui)
        {
            var panel = FullScreenDim("ResultsPanel", canvas, new Color(0.99f, 0.82f, 0.56f, 0.97f));
            ui.resultsPanel = panel.gameObject;
            var screen = panel.gameObject.AddComponent<ResultsScreen>();
            var card = CenteredCard(panel, new Vector2(1000f, 920f));

            screen.titleLabel = CardText(card, "", null, 62f, 120f, Ink);
            screen.boxesLabel = CardText(card, "", null, 46f, 64f, Ink);
            screen.timeLabel = CardText(card, "", null, 46f, 64f, Ink);
            screen.penaltyLabel = CardText(card, "", null, 42f, 60f, new Color(0.75f, 0.35f, 0.1f));
            CardText(card, "", "result.reward_label", 44f, 56f, Ink);

            var rewardRow = UiFactory.NewRect("Reward", card);
            rewardRow.sizeDelta = new Vector2(0f, 120f);
            var layout = rewardRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 24f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var coinRing = UiFactory.NewImage("CoinRing", rewardRow, Ink);
            coinRing.rectTransform.sizeDelta = new Vector2(100f, 100f);
            coinRing.sprite = null;
            var coin = UiFactory.NewImage("Coin", coinRing.transform, new Color(0.95f, 0.78f, 0.25f));
            UiFactory.Place(coin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f));
            coinRing.gameObject.AddComponent<DiscSprite>();
            coin.gameObject.AddComponent<DiscSprite>();

            var number = UiFactory.NewText("Amount", rewardRow, "0", 110f, Ink, TextAlignmentOptions.Left);
            number.rectTransform.sizeDelta = new Vector2(420f, 110f);
            screen.rewardLabel = number;

            screen.walletLabel = CardText(card, "", null, 38f, 56f, new Color(Ink.r, Ink.g, Ink.b, 0.7f));
            ui.resultsRetryButton = CardButton(card, "", "ui.retry", 90f);
            ui.resultsQuitButton = CardButton(card, "", "ui.quit", 90f);
            ui.resultsScreen = screen;
        }

        static ManualPanel BuildManualPanel(Transform canvas, GameAssets a)
        {
            var dim = FullScreenDim("ManualPanel", canvas, new Color(0f, 0f, 0f, 0.6f));
            var panel = dim.gameObject.AddComponent<ManualPanel>();
            panel.rules = a.rules;

            var inner = UiFactory.Panel("Card", dim, new Vector2(1500f, 960f), out var root);
            UiFactory.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 960f));

            var title = UiFactory.NewText("Title", inner, "", 64f, Ink, TextAlignmentOptions.Center, "ui.manual_title");
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(1400f, 90f));

            var rows = UiFactory.NewRect("Rows", inner);
            UiFactory.Place(rows, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1400f, 700f));
            var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var template = UiFactory.NewRect("RowTemplate", rows);
            template.sizeDelta = new Vector2(0f, 60f);
            var iconA = UiFactory.NewImage("IconA", template, Color.white);
            UiFactory.Place(iconA.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(52f, 52f));
            iconA.preserveAspect = true;
            var plus = UiFactory.NewText("Plus", template, "+", 40f, Ink);
            UiFactory.Place(plus.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(76f, 0f), new Vector2(40f, 52f));
            var iconB = UiFactory.NewImage("IconB", template, Color.white);
            UiFactory.Place(iconB.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, 0f), new Vector2(52f, 52f));
            iconB.preserveAspect = true;

            var markerRoot = UiFactory.NewRect("Marker", template);
            UiFactory.Place(markerRoot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(210f, 0f), new Vector2(54f, 54f));
            var ring = UiFactory.NewImage("Ring", markerRoot, Ink);
            UiFactory.Stretch(ring.rectTransform);
            var disc = UiFactory.NewImage("Disc", ring.transform, Color.white);
            UiFactory.Place(disc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
            var symbol = UiFactory.NewText("Symbol", disc.transform, "?", 24f, Ink);
            UiFactory.Stretch(symbol.rectTransform);
            var marker = markerRoot.gameObject.AddComponent<OutcomeMarker>();
            marker.ring = ring;
            marker.disc = disc;
            marker.symbol = symbol;

            var hint = UiFactory.NewText("Hint", template, "", 30f, Ink, TextAlignmentOptions.MidlineLeft);
            hint.rectTransform.anchorMin = new Vector2(0f, 0f);
            hint.rectTransform.anchorMax = new Vector2(1f, 1f);
            hint.rectTransform.offsetMin = new Vector2(280f, 0f);
            hint.rectTransform.offsetMax = new Vector2(-10f, 0f);
            hint.enableAutoSizing = true;
            hint.fontSizeMin = 18f;
            hint.fontSizeMax = 30f;

            var rowView = template.gameObject.AddComponent<ManualRowView>();
            rowView.iconA = iconA;
            rowView.iconB = iconB;
            rowView.marker = marker;
            rowView.hintLabel = hint;

            panel.rowContainer = rows;
            panel.rowTemplate = rowView;
            panel.closeButton = UiFactory.NewButton("Close", inner, "", "ui.close", new Vector2(360f, 90f));
            UiFactory.Place((RectTransform)panel.closeButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(360f, 90f));
            return panel;
        }

        static PaperWipe BuildWipe(Transform canvas)
        {
            var root = UiFactory.NewRect("WipeRoot", canvas);
            UiFactory.Stretch(root);

            var sheet = UiFactory.NewImage("Sheet", root, UiFactory.Paper);
            sheet.raycastTarget = true;
            UiFactory.Place(sheet.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-120f, 0f), new Vector2(2400f, 1500f));
            sheet.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);

            var wipe = root.gameObject.AddComponent<PaperWipe>();
            wipe.sheet = sheet.rectTransform;
            wipe.sheetImage = sheet;
            return wipe;
        }
    }
}
