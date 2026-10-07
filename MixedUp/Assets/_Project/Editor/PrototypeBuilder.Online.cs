using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// The online pieces: the network avatar (the ghost other players see, a Netcode player prefab that lives in
    /// Resources/Net) and the Lobby scene, a plaza by the truck where players gather before the host starts the game.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        const string NetResourcesDir = Root + "/Resources/Net";

        /// <summary>Centre of the plaza, on the clear stage by the truck (the same spot the main menu uses for its diorama).</summary>
        static readonly Vector3 PlazaCentre = new Vector3(11f, 0f, -27f);

        // ------------------------------------------------------------ the avatar

        static GameObject BuildNetAvatarPrefab(Mats m, PlayerPalette palette)
        {
            EnsureFolder(NetResourcesDir);

            var root = new GameObject("NetAvatar");
            root.AddComponent<NetworkObject>();
            var avatar = root.AddComponent<NetAvatar>();

            // What other players see: the character model with colours, a face, hands and boots, but no body of its own.
            var ghost = new GameObject("Ghost");
            ghost.transform.SetParent(root.transform, false);
            var parts = BuildCharacterModel("Visual", ghost.transform, m);

            ghost.AddComponent<PlayerInventory>();
            var status = ghost.AddComponent<PlayerStatus>();
            var appearance = AddAppearance(ghost, parts, palette, false);

            var carry = ghost.AddComponent<CarriedBoxesView>();
            carry.status = status;
            carry.anchor = parts.carryAnchor;

            var expression = ghost.AddComponent<CharacterFace>();
            expression.faceRenderer = parts.face;

            var animator = ghost.AddComponent<PlayerAnimator>();
            animator.status = status;
            animator.carry = carry;
            animator.body = parts.body;
            animator.handLeft = parts.handLeft;
            animator.handRight = parts.handRight;
            animator.bootLeft = parts.bootLeft;
            animator.bootRight = parts.bootRight;

            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(ghost.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            var text = labelGo.AddComponent<TextMeshPro>();
            text.fontSize = 1.7f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = UiFactory.Ink;
            text.fontStyle = FontStyles.Bold;
            text.rectTransform.sizeDelta = new Vector2(6f, 3f);
            text.text = string.Empty;

            avatar.ghost = ghost;
            avatar.appearance = appearance;
            avatar.animator = animator;
            avatar.status = status;
            avatar.label = text;
            avatar.palette = palette;

            string path = NetResourcesDir + "/NetAvatar.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            EnsureNetworkObjectHash(asset);
            return asset;
        }

        /// <summary>
        /// A NetworkObject needs a non-zero GlobalObjectIdHash to be spawned. Netcode fills it in when the component is
        /// validated in the Editor; a prefab saved from code may not have been validated yet, so make sure it is.
        /// </summary>
        static void EnsureNetworkObjectHash(GameObject prefab)
        {
            var networkObject = prefab != null ? prefab.GetComponent<NetworkObject>() : null;
            if (networkObject == null) return;

            var serialized = new SerializedObject(networkObject);
            var property = serialized.FindProperty("GlobalObjectIdHash");
            if (property != null && property.propertyType == SerializedPropertyType.Integer && property.longValue == 0)
            {
                property.longValue = 0x4D58A17E;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
            }
        }

        // ------------------------------------------------------------ the lobby

        static void BuildLobbyScene(GameAssets a, Mats m, ArtAssets art, Prefabs p)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureLighting(art);

            var env = new GameObject("Environment").transform;
            ColliderBox("Ground", env, new Vector3(0f, -0.5f, -20f), new Vector3(90f, 1f, 50f));
            MeshObject("GroundVisual", env, art.groundSouth, art.palette);
            MeshObject("Hills", env, art.hills, art.palette);
            BuildSkyDecor(env, art);

            // The truck and a pile of boxes: the same view as the main menu, so the lobby feels like "the yard before the job".
            var stage = new GameObject("Stage").transform;
            var truck = (GameObject)PrefabUtility.InstantiatePrefab(art.truck, stage);
            truck.name = "Truck";
            truck.transform.position = new Vector3(0f, 0f, -26f);
            var boxes = new GameObject("Boxes").transform;
            boxes.SetParent(stage, false);
            PlaceMenuBox(boxes, art, "normal", new Vector3(-0.8f, 0f, -31.4f), 8f);
            PlaceMenuBox(boxes, art, "toxic", new Vector3(0.7f, 0f, -31.3f), -10f);
            PlaceMenuBox(boxes, art, "hot", new Vector3(-0.1f, 1.05f, -31.4f), 14f);
            PlaceMenuBox(boxes, art, "frozen", new Vector3(3.4f, 0f, -23.4f), 25f);
            PlaceMenuBox(boxes, art, "electric", new Vector3(2.4f, 0f, -29.6f), -18f);
            BuildMenuScenery(env, art);

            BuildPlaza(env, m);

            // Invisible walls keep everybody in the yard.
            Boundary(env, "WallEast", new Vector3(32f, 5f, -26f), new Vector3(1f, 12f, 30f));
            Boundary(env, "WallWest", new Vector3(-12f, 5f, -26f), new Vector3(1f, 12f, 30f));
            Boundary(env, "WallSouth", new Vector3(10f, 5f, -41f), new Vector3(46f, 12f, 1f));
            Boundary(env, "WallNorth", new Vector3(10f, 5f, -12f), new Vector3(46f, 12f, 1f));

            // The local player stands on the plaza; the lobby moves it to its own slot once the network has assigned one.
            var players = new GameObject("Players").transform;
            SpawnCharacter(players, p.player, PlazaCentre + new Vector3(0f, 0.05f, -3f), a.palette, CharacterCustomization.DefaultSkin, CharacterCustomization.DefaultClothes);

            var slots = new Transform[RoomInfo.MaxPlayersLimit];
            var slotsRoot = new GameObject("Slots").transform;
            for (int i = 0; i < slots.Length; i++)
            {
                float angle = (i / (float)slots.Length) * Mathf.PI * 2f + Mathf.PI * 0.25f;
                var slot = new GameObject("Slot" + (i + 1)).transform;
                slot.SetParent(slotsRoot, false);
                slot.position = PlazaCentre + new Vector3(Mathf.Cos(angle), 0.05f, Mathf.Sin(angle)) * 3.4f;
                slot.rotation = Quaternion.LookRotation(PlazaCentre - slot.position + Vector3.up * (slot.position.y - PlazaCentre.y), Vector3.up);
                slots[i] = slot;
            }

            ConfigureCamera();
            var lobby = BuildLobbyHud(a);
            lobby.slots = slots;
            lobby.palette = a.palette;
            BuildEventSystem();

            EditorSceneManager.SaveScene(scene, LobbyScenePath);
        }

        /// <summary>A round stone floor with a campfire in the middle, ringed by torches.</summary>
        static void BuildPlaza(Transform env, Mats m)
        {
            var plaza = new GameObject("Plaza").transform;
            plaza.SetParent(env, false);
            plaza.position = PlazaCentre;

            Prim(PrimitiveType.Cylinder, "Floor", plaza, new Vector3(0f, 0.02f, 0f), new Vector3(11f, 0.02f, 11f), m.stone, false);
            Prim(PrimitiveType.Cylinder, "FloorInner", plaza, new Vector3(0f, 0.035f, 0f), new Vector3(7.4f, 0.02f, 7.4f), m.sandy, false);

            for (int i = 0; i < 9; i++)
            {
                float angle = i / 9f * Mathf.PI * 2f;
                Prim(PrimitiveType.Sphere, "FireStone" + i, plaza, new Vector3(Mathf.Cos(angle) * 0.85f, 0.14f, Mathf.Sin(angle) * 0.85f),
                    new Vector3(0.42f, 0.3f, 0.42f), m.stone, false);
            }
            AddFireFx(plaza, new Vector3(0f, 0.3f, 0f), 1.3f);

            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * Mathf.PI * 2f + Mathf.PI / 6f;
                AddTorchPost(plaza, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 5.2f, 1.6f);
            }
        }

        static OnlineLobby BuildLobbyHud(GameAssets a)
        {
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var lobby = canvasGo.AddComponent<OnlineLobby>();

            // --- room card (top left): code or address, and what to tell the friends
            var roomCard = UiFactory.Panel("RoomCard", canvasGo.transform, new Vector2(800f, 190f), out var roomRoot, false, true);
            UiFactory.Place(roomRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(800f, 190f));
            lobby.codeLabel = UiFactory.NewText("Code", roomCard, "", 62f, UiFactory.Danger, TextAlignmentOptions.Center);
            At(lobby.codeLabel.rectTransform, 40f, 36f, 720f, 80f);
            lobby.hintLabel = UiFactory.NewText("Hint", roomCard, "", 28f, new Color(UiFactory.Ink.r, UiFactory.Ink.g, UiFactory.Ink.b, 0.8f), TextAlignmentOptions.Top);
            At(lobby.hintLabel.rectTransform, 50f, 118f, 700f, 60f);

            // --- players (left)
            var membersCard = UiFactory.Panel("Members", canvasGo.transform, new Vector2(800f, 470f), out var membersRoot, false, false);
            UiFactory.Place(membersRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -250f), new Vector2(800f, 470f));
            lobby.memberNames = new TMP_Text[RoomInfo.MaxPlayersLimit];
            lobby.memberStates = new TMP_Text[RoomInfo.MaxPlayersLimit];
            lobby.memberSkin = new Image[RoomInfo.MaxPlayersLimit];
            lobby.memberClothes = new Image[RoomInfo.MaxPlayersLimit];
            for (int i = 0; i < RoomInfo.MaxPlayersLimit; i++)
            {
                var row = UiFactory.NewRect("Member" + (i + 1), membersCard);
                At(row, 40f, 40f + i * 100f, 720f, 84f);
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

                lobby.memberNames[i] = name;
                lobby.memberStates[i] = state;
                lobby.memberSkin[i] = skin;
                lobby.memberClothes[i] = clothes;
            }

            // --- map and mode (top right)
            var choiceCard = UiFactory.Panel("Choices", canvasGo.transform, new Vector2(800f, 620f), out var choiceRoot, false, true);
            UiFactory.Place(choiceRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(800f, 620f));

            var mapHeader = UiFactory.NewText("MapHeader", choiceCard, "", 38f, Brick, TextAlignmentOptions.Center, "lobby.map");
            At(mapHeader.rectTransform, 150f, 34f, 500f, 46f);
            lobby.mapName = UiFactory.NewText("MapName", choiceCard, "", 50f, UiFactory.Ink, TextAlignmentOptions.Center);
            At(lobby.mapName.rectTransform, 130f, 84f, 540f, 70f);
            lobby.mapDescription = UiFactory.NewText("MapDescription", choiceCard, "", 28f, new Color(UiFactory.Ink.r, UiFactory.Ink.g, UiFactory.Ink.b, 0.85f), TextAlignmentOptions.Top);
            At(lobby.mapDescription.rectTransform, 70f, 158f, 660f, 80f);
            lobby.mapPrevious = UiFactory.NewButton("MapPrevious", choiceCard, "<", null, new Vector2(80f, 80f), UiFactory.ButtonStyle.Plank);
            At((RectTransform)lobby.mapPrevious.transform, 36f, 90f, 80f, 80f);
            lobby.mapNext = UiFactory.NewButton("MapNext", choiceCard, ">", null, new Vector2(80f, 80f), UiFactory.ButtonStyle.Plank);
            At((RectTransform)lobby.mapNext.transform, 684f, 90f, 80f, 80f);

            var modeHeader = UiFactory.NewText("ModeHeader", choiceCard, "", 38f, Brick, TextAlignmentOptions.Center, "ui.mode");
            At(modeHeader.rectTransform, 150f, 300f, 500f, 46f);
            lobby.modeName = UiFactory.NewText("ModeName", choiceCard, "", 50f, UiFactory.Ink, TextAlignmentOptions.Center);
            At(lobby.modeName.rectTransform, 130f, 350f, 540f, 70f);
            lobby.modeDescription = UiFactory.NewText("ModeDescription", choiceCard, "", 28f, new Color(UiFactory.Ink.r, UiFactory.Ink.g, UiFactory.Ink.b, 0.85f), TextAlignmentOptions.Top);
            At(lobby.modeDescription.rectTransform, 70f, 424f, 660f, 100f);
            lobby.modePrevious = UiFactory.NewButton("ModePrevious", choiceCard, "<", null, new Vector2(80f, 80f), UiFactory.ButtonStyle.Plank);
            At((RectTransform)lobby.modePrevious.transform, 36f, 356f, 80f, 80f);
            lobby.modeNext = UiFactory.NewButton("ModeNext", choiceCard, ">", null, new Vector2(80f, 80f), UiFactory.ButtonStyle.Plank);
            At((RectTransform)lobby.modeNext.transform, 684f, 356f, 80f, 80f);

            // --- buttons (bottom)
            lobby.waitingLabel = UiFactory.NewText("Waiting", canvasGo.transform, "", 36f, new Color(UiFactory.Ink.r, UiFactory.Ink.g, UiFactory.Ink.b, 0.9f), TextAlignmentOptions.Center);
            UiFactory.Place(lobby.waitingLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(900f, 60f));
            lobby.readyButton = UiFactory.NewButton("Ready", canvasGo.transform, "", "lobby.ready_action", new Vector2(520f, 110f));
            UiFactory.Place((RectTransform)lobby.readyButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(520f, 110f));
            lobby.readyLabel = lobby.readyButton.GetComponentInChildren<TMP_Text>();
            lobby.startButton = UiFactory.NewButton("Start", canvasGo.transform, "", "lobby.start", new Vector2(520f, 110f));
            UiFactory.Place((RectTransform)lobby.startButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(520f, 110f));
            lobby.leaveButton = UiFactory.NewButton("Leave", canvasGo.transform, "", "lobby.leave", new Vector2(340f, 92f), UiFactory.ButtonStyle.SignLeft);
            UiFactory.Place((RectTransform)lobby.leaveButton.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(340f, 92f));

            var always = UiFactory.NewText("TabHint", canvasGo.transform, "", 32f, Color.white, TextAlignmentOptions.BottomRight, "lobby.tab_hint");
            UiFactory.Place(always.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(760f, 50f));
            always.outlineWidth = 0.25f;
            always.outlineColor = new Color32(40, 28, 16, 255);

            var mouse = UiFactory.NewText("MouseMode", canvasGo.transform, "", 40f, Color.white, TextAlignmentOptions.Top, "lobby.mouse_mode");
            UiFactory.Place(mouse.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(700f, 56f));
            mouse.outlineWidth = 0.25f;
            mouse.outlineColor = new Color32(40, 28, 16, 255);
            lobby.mousePanel = mouse.gameObject;
            lobby.mousePanel.SetActive(false);

            return lobby;
        }
    }
}
