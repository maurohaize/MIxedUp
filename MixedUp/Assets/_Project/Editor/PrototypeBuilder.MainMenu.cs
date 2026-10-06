using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// The main menu scene: a living low-poly diorama (the truck, the character and a few boxes on the meadow) seen by a
    /// slowly swaying camera, with the game's logo and a wooden signpost whose planks are the buttons.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

        static void BuildMainMenuScene(GameAssets a, Mats m, ArtAssets art, Prefabs p)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureLighting(art);

            var env = new GameObject("Environment").transform;
            MeshObject("Ground", env, art.groundSouth, art.palette);
            MeshObject("Hills", env, art.hills, art.palette);
            BuildSkyDecor(env, art);

            var stage = new GameObject("Stage").transform;
            var truck = (GameObject)PrefabUtility.InstantiatePrefab(art.truck, stage);
            truck.name = "Truck";
            truck.transform.position = new Vector3(0f, 0f, -26f);

            // A pile of boxes on the dock behind the truck and a few lying around the character.
            var boxes = new GameObject("Boxes").transform;
            boxes.SetParent(stage, false);
            PlaceMenuBox(boxes, art, "normal", new Vector3(-0.8f, 0f, -31.4f), 8f);
            PlaceMenuBox(boxes, art, "toxic", new Vector3(0.7f, 0f, -31.3f), -10f);
            PlaceMenuBox(boxes, art, "hot", new Vector3(-0.1f, 1.05f, -31.4f), 14f);
            PlaceMenuBox(boxes, art, "frozen", new Vector3(3.4f, 0f, -23.4f), 25f);
            PlaceMenuBox(boxes, art, "electric", new Vector3(2.4f, 0f, -29.6f), -18f);
            PlaceMenuBox(boxes, art, "normal", new Vector3(3.9f, 0f, -29.9f), 40f);

            var hero = (GameObject)PrefabUtility.InstantiatePrefab(p.preview, stage);
            hero.name = "Character";
            hero.transform.position = new Vector3(6.2f, 0f, -26.4f);
            hero.transform.rotation = Quaternion.Euler(0f, 75f, 0f);

            BuildMenuScenery(env, art);

            var camera = Camera.main;
            camera.transform.position = new Vector3(15f, 2.4f, -26f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 700f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 50f;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;

            var orbit = camera.gameObject.AddComponent<MenuCameraOrbit>();
            orbit.focus = hero.transform;
            orbit.yaw = 250f;
            orbit.distance = 6.4f;
            orbit.height = 1.6f;
            orbit.swayDegrees = 8f;
            orbit.screenShift = 2.2f;

            BuildMainMenuUi(a, p);
            BuildEventSystem();

            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        static void PlaceMenuBox(Transform parent, ArtAssets art, string id, Vector3 position, float yaw)
        {
            var box = (GameObject)PrefabUtility.InstantiatePrefab(art.boxWorld[id], parent);
            box.transform.position = position;
            box.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            SetStaticRecursively(box);
        }

        /// <summary>Trees on the far side and around the clearing, plus the small things that make the meadow feel alive.</summary>
        static void BuildMenuScenery(Transform env, ArtAssets art)
        {
            var scenery = new GameObject("Scenery").transform;
            scenery.SetParent(env, false);
            var rng = new System.Random(2024);
            var tints = new[] { art.palette, art.palette, art.tintAutumn, art.tintGold, art.tintTeal };

            bool Clear(float x, float z) =>
                !(x > -6f && x < 6f && z > -34f && z < -18f)       // the truck
                && !(x > 1f && x < 20f && z > -36f && z < -18f);    // the stage and the camera

            int placed = 0;
            for (int attempt = 0; attempt < 400 && placed < 34; attempt++)
            {
                float x = Mathf.Lerp(-42f, 40f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-33f, 3f, (float)rng.NextDouble());
                if (!Clear(x, z)) continue;
                PlaceProp(scenery, art.trees[rng.Next(art.trees.Length)], new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f,
                    Mathf.Lerp(0.85f, 1.4f, (float)rng.NextDouble()), tints[rng.Next(tints.Length)]);
                placed++;
            }

            for (int i = 0; i < 16; i++)
            {
                float x = Mathf.Lerp(-42f, 40f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-33f, 3f, (float)rng.NextDouble());
                if (!Clear(x, z) || (x > -9f && x < 9f)) continue;
                PlaceProp(scenery, art.rocks[rng.Next(art.rocks.Length)], new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f,
                    0.6f + (float)rng.NextDouble() * 0.9f);
            }

            for (int i = 0; i < 26; i++)
            {
                float x = Mathf.Lerp(-42f, 40f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-34f, 4f, (float)rng.NextDouble());
                if (!Clear(x, z)) continue;
                var bush = MeshObject("Bush", scenery, art.bushes[rng.Next(art.bushes.Length)], art.palette);
                bush.transform.position = new Vector3(x, 0f, z);
                bush.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                bush.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.5f, (float)rng.NextDouble());
            }

            for (int i = 0; i < 300; i++)
            {
                float x = Mathf.Lerp(-43f, 43f, (float)rng.NextDouble());
                float z = Mathf.Lerp(-34f, 4f, (float)rng.NextDouble());
                bool flower = i % 5 == 0;
                var mesh = flower ? art.flowers[rng.Next(art.flowers.Length)] : art.tufts[rng.Next(art.tufts.Length)];
                var go = MeshObject(flower ? "Flower" : "Tuft", scenery, mesh, art.palette, false);
                go.transform.position = new Vector3(x, 0f, z);
                go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.8f, (float)rng.NextDouble());
            }
        }

        static void BuildMainMenuUi(GameAssets a, Prefabs p)
        {
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var menu = canvasGo.AddComponent<MainMenu>();

            // Pencil hatching over the whole 3D view: it ties the render to the hand-drawn interface.
            var sketch = UiFactory.NewSprite("SketchFrame", canvasGo.transform, "sketch_frame");
            sketch.preserveAspect = false;
            sketch.color = new Color(1f, 1f, 1f, 0.85f);
            UiFactory.Stretch(sketch.rectTransform);

            var signpost = UiFactory.NewRect("Signpost", canvasGo.transform);
            UiFactory.Stretch(signpost);
            menu.signpost = signpost.gameObject;

            var logo = UiFactory.NewSprite("Logo", signpost, "logo");
            At(logo.rectTransform, 70f, 36f, 540f, 442f);
            logo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -3f);

            var post = UiFactory.NewSprite("Post", signpost, "post");
            post.preserveAspect = false;
            At(post.rectTransform, 280f, 470f, 100f, 700f);

            menu.playButton = UiFactory.NewButton("Play", signpost, "", "ui.play", new Vector2(560f, 128f));
            At((RectTransform)menu.playButton.transform, 50f, 520f, 560f, 128f);
            menu.settingsButton = UiFactory.NewButton("Settings", signpost, "", "ui.settings", new Vector2(500f, 112f));
            At((RectTransform)menu.settingsButton.transform, 76f, 680f, 500f, 112f);
            menu.quitButton = UiFactory.NewButton("Quit", signpost, "", "ui.quit", new Vector2(400f, 100f), UiFactory.ButtonStyle.SignLeft);
            At((RectTransform)menu.quitButton.transform, 150f, 826f, 400f, 100f);

            var footer = UiFactory.NewText("Footer", canvasGo.transform, "MIXED UP", 30f, new Color(UiFactory.Ink.r, UiFactory.Ink.g, UiFactory.Ink.b, 0.55f), TextAlignmentOptions.BottomRight);
            UiFactory.Place(footer.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 28f), new Vector2(500f, 50f));

            menu.settings = BuildSettingsPanel(canvasGo.transform, a, p.preview);
            menu.settings.gameObject.SetActive(false);
        }
    }
}
