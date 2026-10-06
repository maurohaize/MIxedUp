using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp.EditorTools
{
    /// <summary>The multiplayer lobby card of the main menu: entry view (name, create, join) and room view (members, mode, ready, start).</summary>
    public static partial class PrototypeBuilder
    {
        /// <summary>A text box on a paper strip. The placeholder is a localized hint.</summary>
        static TMP_InputField NewInput(string name, Transform parent, Vector2 size, string placeholderKey, TMP_InputField.CharacterValidation validation = TMP_InputField.CharacterValidation.None)
        {
            var root = UiFactory.NewRect(name, parent);
            root.sizeDelta = size;
            var background = UiFactory.NewSprite("Background", root, "paper", sliced: true, slicedScale: 3.2f);
            UiFactory.Stretch(background.rectTransform);
            background.raycastTarget = true;

            var viewport = UiFactory.NewRect("Viewport", root);
            viewport.gameObject.AddComponent<RectMask2D>();
            UiFactory.Stretch(viewport, 22f);

            var text = UiFactory.NewText("Text", viewport, "", size.y * 0.5f, UiFactory.Ink, TextAlignmentOptions.MidlineLeft, null, false);
            UiFactory.Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var placeholder = UiFactory.NewText("Placeholder", viewport, "", size.y * 0.42f, new Color(UiFactory.Ink.r, UiFactory.Ink.g, UiFactory.Ink.b, 0.4f), TextAlignmentOptions.MidlineLeft, placeholderKey, false);
            UiFactory.Stretch(placeholder.rectTransform);
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;

            var input = root.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterValidation = validation;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.caretColor = UiFactory.Ink;
            input.selectionColor = new Color(0.95f, 0.75f, 0.3f, 0.5f);
            input.pointSize = text.fontSize;
            return input;
        }

        static LobbyPanel BuildLobbyPanel(Transform canvas, GameAssets a)
        {
            var dim = FullScreenDim("LobbyPanel", canvas, new Color(0f, 0f, 0f, 0.62f));
            var panel = dim.gameObject.AddComponent<LobbyPanel>();
            panel.palette = a.palette;

            const float w = 1500f, h = 920f;
            var card = UiFactory.Panel("Card", dim, new Vector2(w, h), out var cardRoot, false, true);
            UiFactory.Place(cardRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h));

            var title = UiFactory.NewText("Title", card, "", 96f, UiFactory.Ink, TextAlignmentOptions.Center, "ui.multiplayer");
            UiFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1000f, 120f));
            var underline = UiFactory.NewSprite("Underline", card, "underline");
            UiFactory.Place(underline.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(560f, 32f));

            // ------------------------------------------------------------ entry view
            var entry = UiFactory.NewRect("Entry", card);
            UiFactory.Stretch(entry);
            panel.entryView = entry.gameObject;

            var nameLabel = UiFactory.NewText("NameLabel", entry, "", 40f, Brick, TextAlignmentOptions.MidlineLeft, "lobby.name");
            At(nameLabel.rectTransform, 120f, 190f, 600f, 54f);
            panel.nameInput = NewInput("NameInput", entry, new Vector2(620f, 84f), "lobby.name_hint");
            At((RectTransform)panel.nameInput.transform, 120f, 250f, 620f, 84f);

            panel.createButton = UiFactory.NewButton("Create", entry, "", "lobby.create", new Vector2(620f, 112f));
            At((RectTransform)panel.createButton.transform, 100f, 420f, 620f, 112f);

            var codeLabel = UiFactory.NewText("CodeLabel", entry, "", 40f, Brick, TextAlignmentOptions.MidlineLeft, "lobby.code");
            At(codeLabel.rectTransform, 820f, 190f, 560f, 54f);
            panel.codeInput = NewInput("CodeInput", entry, new Vector2(560f, 84f), "lobby.code_hint", TMP_InputField.CharacterValidation.Alphanumeric);
            At((RectTransform)panel.codeInput.transform, 820f, 250f, 560f, 84f);
            panel.joinButton = UiFactory.NewButton("Join", entry, "", "lobby.join", new Vector2(560f, 112f));
            At((RectTransform)panel.joinButton.transform, 800f, 420f, 560f, 112f);

            var hint = UiFactory.NewText("DemoHint", entry, "", 32f, new Color(UiFactory.Ink.r, UiFactory.Ink.g, UiFactory.Ink.b, 0.7f), TextAlignmentOptions.Top, "lobby.hint_demo");
            At(hint.rectTransform, 120f, 585f, 1260f, 90f);
            panel.messageLabel = UiFactory.NewText("Message", entry, "", 40f, UiFactory.Danger, TextAlignmentOptions.Center);
            At(panel.messageLabel.rectTransform, 120f, 690f, 1260f, 60f);

            panel.backButton = UiFactory.NewButton("Back", entry, "", "ui.back", new Vector2(340f, 92f), UiFactory.ButtonStyle.SignLeft);
            At((RectTransform)panel.backButton.transform, 70f, 790f, 340f, 92f);

            // -------------------------------------------------------------- room view
            var room = UiFactory.NewRect("Room", card);
            UiFactory.Stretch(room);
            panel.roomView = room.gameObject;

            panel.codeLabel = UiFactory.NewText("RoomCode", room, "", 78f, UiFactory.Danger, TextAlignmentOptions.Center);
            At(panel.codeLabel.rectTransform, 200f, 168f, 1100f, 100f);

            panel.memberNames = new TMP_Text[RoomInfo.MaxPlayersLimit];
            panel.memberStates = new TMP_Text[RoomInfo.MaxPlayersLimit];
            panel.memberSkin = new Image[RoomInfo.MaxPlayersLimit];
            panel.memberClothes = new Image[RoomInfo.MaxPlayersLimit];
            for (int i = 0; i < RoomInfo.MaxPlayersLimit; i++)
            {
                var row = UiFactory.NewRect("Member" + (i + 1), room);
                At(row, 90f, 300f + i * 96f, 720f, 84f);
                var strip = UiFactory.NewSprite("Strip", row, "paper", sliced: true, slicedScale: 4f);
                strip.color = new Color(1f, 1f, 1f, 0.55f);
                UiFactory.Stretch(strip.rectTransform);

                var skin = UiFactory.NewSprite("Skin", row, "swatch");
                UiFactory.Place(skin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(56f, 56f));
                var clothes = UiFactory.NewSprite("Clothes", row, "swatch");
                UiFactory.Place(clothes.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(86f, 0f), new Vector2(38f, 38f));

                var name = UiFactory.NewText("Name", row, "", 42f, UiFactory.Ink, TextAlignmentOptions.MidlineLeft);
                UiFactory.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(142f, 0f), new Vector2(340f, 60f));
                var state = UiFactory.NewText("State", row, "", 34f, Brick, TextAlignmentOptions.MidlineRight);
                UiFactory.Place(state.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(230f, 60f));

                panel.memberNames[i] = name;
                panel.memberStates[i] = state;
                panel.memberSkin[i] = skin;
                panel.memberClothes[i] = clothes;
            }

            var modeTitle = UiFactory.NewText("ModeTitle", room, "", 38f, Brick, TextAlignmentOptions.Center, "ui.mode");
            At(modeTitle.rectTransform, 900f, 300f, 500f, 50f);
            panel.modeLabel = UiFactory.NewText("ModeName", room, "", 52f, UiFactory.Ink, TextAlignmentOptions.Center);
            At(panel.modeLabel.rectTransform, 960f, 352f, 380f, 110f);
            panel.modeDescription = UiFactory.NewText("ModeDescription", room, "", 30f, new Color(UiFactory.Ink.r, UiFactory.Ink.g, UiFactory.Ink.b, 0.8f), TextAlignmentOptions.Top);
            At(panel.modeDescription.rectTransform, 880f, 470f, 540f, 120f);
            panel.modePrevious = UiFactory.NewButton("ModePrevious", room, "<", null, new Vector2(80f, 80f), UiFactory.ButtonStyle.Plank);
            At((RectTransform)panel.modePrevious.transform, 880f, 366f, 80f, 80f);
            panel.modeNext = UiFactory.NewButton("ModeNext", room, ">", null, new Vector2(80f, 80f), UiFactory.ButtonStyle.Plank);
            At((RectTransform)panel.modeNext.transform, 1340f, 366f, 80f, 80f);

            panel.waitingLabel = UiFactory.NewText("Waiting", room, "", 34f, new Color(UiFactory.Ink.r, UiFactory.Ink.g, UiFactory.Ink.b, 0.75f), TextAlignmentOptions.Center);
            At(panel.waitingLabel.rectTransform, 840f, 620f, 620f, 80f);

            panel.readyButton = UiFactory.NewButton("Ready", room, "", "lobby.ready_action", new Vector2(520f, 110f));
            At((RectTransform)panel.readyButton.transform, 880f, 730f, 520f, 110f);
            panel.readyButtonLabel = panel.readyButton.GetComponentInChildren<TMP_Text>();
            panel.startButton = UiFactory.NewButton("Start", room, "", "lobby.start", new Vector2(520f, 110f));
            At((RectTransform)panel.startButton.transform, 880f, 730f, 520f, 110f);
            panel.leaveButton = UiFactory.NewButton("Leave", room, "", "lobby.leave", new Vector2(340f, 92f), UiFactory.ButtonStyle.SignLeft);
            At((RectTransform)panel.leaveButton.transform, 70f, 810f, 340f, 92f);

            room.gameObject.SetActive(false);
            dim.gameObject.SetActive(false);
            return panel;
        }
    }
}
