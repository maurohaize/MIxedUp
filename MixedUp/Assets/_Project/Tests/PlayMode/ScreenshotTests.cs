using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>
    /// Renders a gallery of the game to PNG files so the visuals can be inspected without opening the editor.
    /// Explicit: needs a GPU, so it is skipped by normal test runs.
    /// Output folder: env MIXEDUP_SHOTS, or Temp/Shots.
    /// </summary>
    public class ScreenshotTests : SceneTestBase
    {
        const int Width = 1280, Height = 720;

        string OutDir
        {
            get
            {
                string dir = System.Environment.GetEnvironmentVariable("MIXEDUP_SHOTS");
                if (string.IsNullOrEmpty(dir)) dir = "Temp/Shots";
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        // ----------------------------------------------------------- helpers

        IEnumerator Capture(string name)
        {
            var cam = Camera.main;
            var canvas = Object.FindAnyObjectByType<Canvas>();
            var rt = new RenderTexture(Width, Height, 24);
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;

            cam.targetTexture = rt;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;

            // Render explicitly instead of waiting for the end of frame, which never arrives in batch mode.
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            Object.Destroy(tex);
            rt.Release();
            Object.Destroy(rt);
        }

        void FreeCamera(Vector3 position, Vector3 lookAt)
        {
            var follow = Camera.main.GetComponent<ThirdPersonCamera>();
            follow.enabled = false;
            Camera.main.transform.position = position;
            Camera.main.transform.rotation = Quaternion.LookRotation(lookAt - position);
        }

        void FollowCamera() => Camera.main.GetComponent<ThirdPersonCamera>().enabled = true;

        // --------------------------------------------------------- phase one

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator PhaseOneGallery()
        {
            FreeCamera(new Vector3(20f, 9f, -44f), new Vector3(0f, 1.5f, -26f));
            yield return Capture("01_truck_overview");

            FreeCamera(new Vector3(0f, 55f, -70f), new Vector3(0f, 0f, 12f));
            yield return Capture("02_level_overview");

            FreeCamera(new Vector3(-12f, 7f, -33f), new Vector3(-31f, 2.5f, -14f));
            yield return Capture("03_ice_hill");

            FreeCamera(new Vector3(-12f, 4f, 24f), new Vector3(-27f, 0.5f, 9f));
            yield return Capture("04_bridge_river");

            FollowCamera();
            yield return new WaitForSeconds(0.3f);
            yield return Capture("05_hud_start");

            var inv = status.Inventory;
            inv.TryAdd(BoxOf("hot"), out _);
            status.Tick(14f);
            yield return Capture("06_hot_carried");

            inv.Clear();
            inv.TryAdd(BoxOf("toxic"), out _);
            inv.TryAdd(BoxOf("frozen"), out _);
            for (int i = 0; i < 25; i++) status.Tick(1f);
            yield return Capture("07_toxic_frozen");

            inv.Clear();
            inv.TryAdd(BoxOf("electric"), out _);
            yield return Capture("08_electric_inventory");

            var dummy = Object.FindAnyObjectByType<TeammateDummy>();
            player.Teleport(dummy.transform.position + new Vector3(1.5f, 0.05f, 0f));
            yield return new WaitForSeconds(0.5f);
            yield return Capture("09_near_teammate_prompt");

            foreach (var p in Object.FindObjectsByType<BoxPickup>())
                if (p.data.id == "normal") { player.Teleport(p.transform.position + new Vector3(1f, 0.05f, 0f)); break; }
            yield return new WaitForSeconds(0.6f);
            yield return Capture("10_pickup_prompt");

            ui.pausePanel.SetActive(true);
            yield return Capture("11_pause");
            ui.pausePanel.SetActive(false);
        }

        // ------------------------------------------------------- character

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator CharacterGallery()
        {
            ui.hudRoot.SetActive(false);
            var teammate = Object.FindAnyObjectByType<TeammateDummy>();
            var at = player.transform.position;

            FreeCamera(at + new Vector3(0.9f, 1.25f, 3.3f), at + Vector3.up * 0.85f);
            yield return Capture("30_character_front");

            FreeCamera(at + new Vector3(-3.4f, 1.1f, 0.2f), at + Vector3.up * 0.85f);
            yield return Capture("31_character_side");

            var inv = status.Inventory;
            inv.TryAdd(BoxOf("normal"), out _);
            yield return new WaitForSeconds(0.5f);
            FreeCamera(at + new Vector3(1.4f, 1.3f, 3.0f), at + Vector3.up * 0.9f);
            yield return Capture("32_carry_one_front");

            inv.TryAdd(BoxOf("frozen"), out _);
            yield return new WaitForSeconds(0.5f);
            FreeCamera(at + new Vector3(1.4f, 1.3f, 3.0f), at + Vector3.up * 1.0f);
            yield return Capture("33_carry_two_front");

            FreeCamera(at + new Vector3(3.2f, 1.2f, 0.6f), at + Vector3.up * 1.0f);
            yield return Capture("34_carry_two_side");

            FollowCamera();
            ui.hudRoot.SetActive(true);
            yield return new WaitForSeconds(0.3f);
            yield return Capture("35_carry_two_game_camera");

            ui.hudRoot.SetActive(false);
            var theirs = teammate.transform.position;
            FreeCamera(theirs + new Vector3(0.8f, 1.2f, 3.2f), theirs + Vector3.up * 0.9f);
            yield return Capture("36_teammate");

            // Running while holding something: the hands keep hugging the boxes.
            FollowCamera();
            SetKey(UnityEngine.InputSystem.Key.W, true);
            yield return new WaitForSeconds(0.7f);
            ui.hudRoot.SetActive(true);
            yield return Capture("37_running_with_boxes");
            SetKey(UnityEngine.InputSystem.Key.W, false);
        }

        // --------------------------------------------------------- phase two

        [UnityTest, Explicit("Needs a GPU; writes PNGs")]
        public IEnumerator PhaseTwoGallery()
        {
            var note = GameObject.Find("Note_HotElectric").transform.position;
            FreeCamera(note + new Vector3(3f, 2.2f, 4f), note + Vector3.up * 0.8f);
            yield return Capture("20_lore_note");

            puzzle.explosionFuseSeconds = 30f;
            puzzle.travelSeconds = 30f;
            yield return DeliverEverything();
            yield return new WaitForSecondsRealtime(1.5f);   // let the paper wipe peel away
            yield return Capture("21_puzzle_arranging_unknown");

            CombinationManual.Discover(puzzle.rules.Find(BoxOf("hot"), BoxOf("electric")));
            screen.slots[0].button.onClick.Invoke();
            yield return Capture("22_puzzle_selected_known_explosion");
            screen.slots[0].button.onClick.Invoke();

            screen.startButton.onClick.Invoke();
            puzzle.State.Tick(3f);
            yield return Capture("23_travel_fuse_lit");

            Arrange(SafeOrder);
            puzzle.State.Tick(4f);
            yield return Capture("24_travel_safe");

            ui.manualPanel.Show();
            yield return Capture("25_manual");
            ui.manualPanel.Hide();

            puzzle.State.Tick(100f);
            yield return null;
            yield return Capture("26_results_safe");

            // Explosion that kills: fresh level, hurt player.
            ui.resultsRetryButton.onClick.Invoke();
            yield return null;
            yield return new WaitForSeconds(0.8f);
            Resolve();
            player = PlayerRegistry.Local;
            status = player.GetComponent<PlayerStatus>();
            status.Damage(55f, DeathCause.Fall);
            puzzle.explosionFuseSeconds = 0.2f;
            yield return DeliverEverything();
            yield return new WaitForSecondsRealtime(1.5f);
            screen.startButton.onClick.Invoke();
            puzzle.State.Tick(0.5f);
            yield return null;
            yield return Capture("27_game_over_explosion");
        }
    }
}
