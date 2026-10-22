using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using R = MixedUp.Trailer.ShotContext.Reaction;

namespace MixedUp.Trailer
{
    /// <summary>
    /// The shot list of the trailer, in story order. Each shot is filmed in a real map with the four friends; the edit
    /// (Remotion, folder Trailer/) trims them, adds the subtitles, the music and the titles.
    /// </summary>
    public static class TrailerShots
    {
        const string Lobby = "Lobby", Meadow = "Level_Prototype", Summit = "Level_Summit", Harbour = "Level_Harbour";

        static float YawTo(Vector3 from, Vector3 to) => Quaternion.LookRotation(new Vector3(to.x - from.x, 0f, to.z - from.z)).eulerAngles.y;
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        public static readonly List<Shot> All = new List<Shot>
        {
            // ------------------------------------------------------------------ cold open
            new Shot
            {
                id = "S01_ColdOpen", scene = Meadow, duration = 3.2f, vertical = true,
                setup = c =>
                {
                    var iker = c.Actor(Friends.Iker, V(-32f, 4.4f, -12.6f), 180f);
                    iker.Hold(c.Box("frozen"));
                    c.Camera.shake = 0.08f;
                    c.Camera.move = TrailerCamera.Dolly(V(-28.5f, 2.6f, -23f), V(-28.8f, 2.8f, -21.5f), V(-32f, 3.6f, -14f), V(-32f, 2.6f, -15.5f), 3.2f, c.vertical ? 68f : 48f);
                },
                action = c => ColdOpen(c)
            },

            // ------------------------------------------------------------------ studio doors
            new Shot
            {
                id = "S02_Doors", scene = Lobby, duration = 7.5f, vertical = true, preroll = 0.8f,
                setup = c =>
                {
                    var doors = TrailerSets.BuildDoors(V(11f, 0f, -34.2f), Vector3.forward, "MUELLE 1");
                    c.Actor(Friends.Mauro, V(9.6f, 0.05f, -27.4f), 200f);
                    c.Actor(Friends.Ane, V(12.6f, 0.05f, -26.5f), 160f);
                    c.Actor(Friends.Iker, V(10.4f, 0.05f, -24.9f), 170f);
                    c.Actor(Friends.Maite, V(13.4f, 0.05f, -29.0f), 230f);
                    var inside = V(11f, 1.65f, -39.8f);
                    var through = V(11f, 2.1f, -31.2f);
                    c.Camera.smoothFollow = false;
                    c.Camera.shake = 0.015f;
                    float fov = c.vertical ? 72f : 50f;
                    c.Camera.move = t =>
                    {
                        float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 3.4f) / 3.6f));
                        return new TrailerCamera.Pose
                        {
                            position = Vector3.Lerp(inside, through, k),
                            lookAt = Vector3.Lerp(V(11f, 1.75f, -34f), V(11f, 1.0f, -26f), k),
                            fov = fov
                        };
                    };
                    c.director.StartCoroutine(OpenDoorsLater(c, doors));
                },
                action = c => LobbyIdle(c)
            },

            // ------------------------------------------------------------------ lobby
            new Shot
            {
                id = "S03_LobbyGather", scene = Lobby, duration = 7.5f, vertical = true,
                setup = c =>
                {
                    c.Actor(Friends.Mauro, V(9.9f, 0.05f, -27.6f), 60f);
                    c.Actor(Friends.Ane, V(2.5f, 0.05f, -31f), 60f);
                    c.Actor(Friends.Iker, V(17f, 0.05f, -21f), 220f);
                    c.Actor(Friends.Maite, V(19f, 0.05f, -32f), 300f);
                    c.Camera.move = TrailerCamera.Orbit(() => V(11f, 0f, -27f), c.vertical ? 11f : 9.5f, 3.6f, 200f, -6f, 0.8f, c.vertical ? 70f : 50f);
                },
                action = c => LobbyGather(c)
            },
            new Shot
            {
                id = "S04_LobbyHug", scene = Lobby, duration = 4.5f,
                setup = c =>
                {
                    var mauro = c.Actor(Friends.Mauro, V(9.6f, 0.05f, -28.2f), 90f);
                    var ane = c.Actor(Friends.Ane, V(10.7f, 0.05f, -28.2f), 270f);
                    var iker = c.Actor(Friends.Iker, V(13.2f, 0.05f, -25.6f), 140f);
                    var maite = c.Actor(Friends.Maite, V(14.0f, 0.05f, -26.6f), 300f);
                    c.Camera.move = TrailerCamera.Static(V(11.6f, 1.7f, -33.2f), V(11.8f, 0.9f, -27f), 46f);
                },
                action = c => LobbyHug(c)
            },
            new Shot
            {
                id = "S05_LobbyReady", scene = Lobby, duration = 4.2f, vertical = true,
                setup = c =>
                {
                    float[] xs = { 9.2f, 10.4f, 11.6f, 12.8f };
                    var who = new[] { Friends.Ane, Friends.Mauro, Friends.Iker, Friends.Maite };
                    for (int i = 0; i < 4; i++) c.Actor(who[i], V(xs[i], 0.05f, -26.2f), 180f);
                    c.Camera.move = TrailerCamera.Dolly(V(11f, 0.7f, -31.2f), V(11f, 0.75f, -30.0f), V(11f, 1.25f, -26f), V(11f, 1.3f, -26f), 4.2f, c.vertical ? 74f : 44f);
                },
                action = c => LobbyReady(c)
            },

            // ------------------------------------------------------------------ meadow
            new Shot
            {
                id = "S06_MeadowArrive", scene = Meadow, duration = 5.5f, vertical = true,
                setup = c =>
                {
                    c.Actor(Friends.Mauro, V(2.2f, 0.05f, -20.5f), 0f);
                    c.Actor(Friends.Ane, V(0.4f, 0.05f, -21.4f), 350f);
                    c.Actor(Friends.Iker, V(4.0f, 0.05f, -21.2f), 10f);
                    c.Actor(Friends.Maite, V(-1.4f, 0.05f, -20.8f), 345f);
                    c.Camera.avoidObstacles = true;
                    c.Camera.move = TrailerCamera.Dolly(V(1.4f, 1.6f, -26.5f), V(1.4f, 2.4f, -25.0f), V(1.4f, 1.4f, -16f), V(1.4f, 2.2f, -4f), 5.5f, c.vertical ? 72f : 50f);
                },
                action = c => MeadowArrive(c)
            },
            new Shot
            {
                id = "S07_HotPotato", scene = Meadow, duration = 6.5f, vertical = true,
                setup = c =>
                {
                    var iker = c.Actor(Friends.Iker, V(-2f, 0.05f, -8f), 160f);
                    var mauro = c.Actor(Friends.Mauro, V(1.5f, 0.05f, -15.5f), 340f);
                    iker.Hold(c.Box("hot"));
                    c.Camera.move = TrailerCamera.Follow(() => iker.Position, () => 115f, c.vertical ? 6.2f : 4.6f, 1.8f, 1.1f, 0f, c.vertical ? 70f : 50f);
                },
                action = c => HotPotato(c)
            },
            new Shot
            {
                id = "S08_LogJump", scene = Meadow, duration = 6.5f, vertical = true,
                setup = c =>
                {
                    var sweeper = Object.FindAnyObjectByType<Sweeper>();
                    var centre = sweeper != null ? sweeper.transform.position : V(22f, 0f, 26f);
                    c.Actor(Friends.Ane, centre + V(-1.6f, 0.1f, -4.6f), 0f);
                    c.Actor(Friends.Maite, centre + V(1.6f, 0.1f, -4.8f), 0f);
                    c.Camera.avoidObstacles = true;
                    c.Camera.move = TrailerCamera.Static(centre + V(5.5f, 1.4f, -6.5f), centre + V(-0.6f, 0.8f, 0f), c.vertical ? 74f : 52f);
                },
                action = c => LogJump(c)
            },
            new Shot
            {
                id = "S09_RiverPush", scene = Meadow, duration = 4.6f, vertical = true,
                setup = c =>
                {
                    var iker = c.Actor(Friends.Iker, V(-6f, 0.05f, 3.6f), 0f);
                    var mauro = c.Actor(Friends.Mauro, V(-6.2f, 0.05f, 1.3f), 0f);
                    c.Camera.move = TrailerCamera.Static(V(-1.8f, 1.5f, 0.4f), V(-6.4f, 0.4f, 4.4f), c.vertical ? 72f : 48f);
                },
                action = c => RiverPush(c)
            },
            new Shot
            {
                id = "S10_Duck", scene = Meadow, duration = 3.4f,
                setup = c =>
                {
                    var duck = NearestDuck(V(4.5f, 0f, 6.4f));
                    var at = duck != null ? duck.transform.position : V(4.5f, 0f, 6.4f);
                    c.Actor(Friends.Ane, V(at.x - 0.6f, 0.05f, 4.2f), YawTo(V(at.x, 0f, 4.2f), at));
                    c.Camera.move = TrailerCamera.Static(at + V(2.2f, 0.9f, -3.2f), at + V(-0.6f, 0.4f, -0.8f), 40f);
                },
                action = c => Duck(c)
            },
            new Shot
            {
                id = "S11_Bounce", scene = Meadow, duration = 5f, vertical = true,
                setup = c =>
                {
                    c.Actor(Friends.Maite, V(-18f, 0.1f, 33.5f), 0f);
                    c.Camera.move = TrailerCamera.Static(V(-11.5f, 1.2f, 34.5f), V(-18f, 2.8f, 40f), c.vertical ? 74f : 52f);
                },
                action = c => Bounce(c)
            },
            new Shot
            {
                id = "S12_Tunnel", scene = Meadow, duration = 4.6f,
                setup = c =>
                {
                    var iker = c.Actor(Friends.Iker, V(-37f, 0.1f, 26.5f), 0f);
                    c.Camera.move = TrailerCamera.Follow(() => iker.Position, () => 0f, 3.8f, 1.2f, 0.7f, 0.6f, 52f);
                },
                action = c => Tunnel(c)
            },

            // ------------------------------------------------------------------ frozen summit
            new Shot
            {
                id = "S13_SummitLake", scene = Summit, duration = 6.5f, vertical = true,
                setup = c =>
                {
                    c.Actor(Friends.Mauro, V(-2f, 0.05f, -22f), 0f);
                    c.Actor(Friends.Ane, V(0.5f, 0.05f, -23.5f), 0f);
                    c.Actor(Friends.Iker, V(3f, 0.05f, -22.5f), 0f);
                    c.Actor(Friends.Maite, V(5.5f, 0.05f, -24f), 0f);
                    c.Camera.move = t => new TrailerCamera.Pose
                    {
                        position = c.Centre() + V(c.vertical ? 11f : 9.5f, 3.2f, -3.5f),
                        lookAt = c.Centre() + V(0f, 0.6f, 1.5f),
                        fov = c.vertical ? 72f : 48f
                    };
                },
                action = c => SummitLake(c)
            },
            new Shot
            {
                id = "S14_Snowman", scene = Summit, duration = 3.6f,
                setup = c =>
                {
                    var iker = c.Actor(Friends.Iker, V(-22f, 0.05f, -21.5f), 0f);
                    c.Camera.move = TrailerCamera.Static(V(-18.2f, 1.3f, -16.8f), V(-22f, 1f, -14f), 50f);
                },
                action = c => Snowman(c)
            },
            new Shot
            {
                id = "S15_Glacier", scene = Summit, duration = 5f,
                setup = c =>
                {
                    var maite = c.Actor(Friends.Maite, V(-30f, 0.05f, -14.5f), 0f);
                    maite.Hold(c.Box("frozen"));
                    c.Camera.move = TrailerCamera.Static(V(-27.5f, 1.8f, -16.5f), V(-30f, 1.8f, -8.5f), 50f);
                },
                action = c => Glacier(c)
            },

            // ------------------------------------------------------------------ dusk harbour
            new Shot
            {
                id = "S16_HarbourRaft", scene = Harbour, duration = 7f, vertical = true, preroll = 0.4f,
                mode = GameModes.Night,
                setup = c =>
                {
                    var raft = Object.FindAnyObjectByType<MovingRaft>();
                    c.Actor(Friends.Ane, V(24.4f, 0.12f, 9.6f), 0f);
                    c.Actor(Friends.Mauro, V(25.6f, 0.12f, 10.4f), 20f);
                    c.Camera.move = TrailerCamera.Orbit(() => raft != null ? raft.transform.position : V(25f, 0f, 15f), c.vertical ? 8.5f : 7f, 2.4f, 250f, 9f, 1f, c.vertical ? 70f : 48f);
                },
                action = c => HarbourRaft(c)
            },
            new Shot
            {
                id = "S17_HarbourZap", scene = Harbour, duration = 5.6f, vertical = true,
                mode = GameModes.Night,
                setup = c =>
                {
                    var ane = c.Actor(Friends.Ane, V(9f, 0.3f, 10f), 180f);
                    var iker = c.Actor(Friends.Iker, V(12f, 0.3f, 10.6f), 90f);
                    iker.Hold(c.Box("electric"));
                    c.Camera.move = TrailerCamera.Static(V(10.6f, 1.9f, 4.4f), V(12.6f, 0.4f, 11.5f), c.vertical ? 72f : 50f);
                },
                action = c => HarbourZap(c)
            },

            // ------------------------------------------------------------------ the big finish
            new Shot
            {
                id = "S18_Delivery", scene = Meadow, duration = 5.5f, mode = GameModes.Night,
                setup = c =>
                {
                    float[] xs = { -2.6f, -0.9f, 0.9f, 2.6f };
                    var who = new[] { Friends.Mauro, Friends.Ane, Friends.Iker, Friends.Maite };
                    string[] boxes = { "hot", "electric", "toxic", "frozen" };
                    for (int i = 0; i < 4; i++)
                    {
                        var a = c.Actor(who[i], V(xs[i], 0.05f, -38.5f + i * 0.4f), 0f);
                        a.Hold(c.Box(boxes[i]));
                    }
                    c.Camera.move = TrailerCamera.Static(V(5.8f, 1.6f, -38.2f), V(0f, 1.0f, -31.5f), 48f);
                },
                action = c => Delivery(c)
            },
            new Shot
            {
                id = "S19_Boom", scene = Meadow, duration = 6.5f, vertical = true, mode = GameModes.Night,
                setup = c =>
                {
                    float[] xs = { -2.4f, -0.8f, 0.8f, 2.4f };
                    var who = new[] { Friends.Mauro, Friends.Ane, Friends.Iker, Friends.Maite };
                    for (int i = 0; i < 4; i++) c.Actor(who[i], V(xs[i], 0.05f, -35.4f), 0f);
                    c.Camera.move = TrailerCamera.Static(V(0.2f, 1.3f, -43.5f), V(0f, 1.7f, -30f), c.vertical ? 76f : 50f);
                },
                action = c => Boom(c)
            },
            new Shot
            {
                id = "S20_KeyArt", scene = Harbour, duration = 8f, vertical = true,
                setup = c =>
                {
                    c.ClearBoxes();
                    float top = TrailerSets.BoxHeap(c, V(-8f, 0f, -16f), 0f);
                    c.Actor(Friends.Mauro, V(-8f, top + 0.05f, -16f), 160f);
                    c.Actor(Friends.Ane, V(-9.1f, 2.15f, -16f), 200f);
                    c.Actor(Friends.Iker, V(-6.9f, 2.15f, -16.05f), 150f);
                    c.Actor(Friends.Maite, V(-10.4f, 1.1f, -15.7f), 210f);
                    c.Camera.move = TrailerCamera.Dolly(V(-6.6f, 0.6f, -24.5f), V(-6.9f, 0.9f, -22.8f), V(-8.2f, 2.6f, -16f), V(-8.2f, 2.9f, -16f), 8f, c.vertical ? 78f : 46f);
                },
                action = c => KeyArt(c)
            },
        };

        // ====================================================================== scripts

        static IEnumerator ColdOpen(ShotContext c)
        {
            var iker = c.cast[0];
            yield return c.At(0.1f);
            c.Say(iker, "¡AAAAAAAAAH!", 2.6f);
            c.React(iker, R.Scream, 0.75f);
            c.Run(iker.Walk(Vector3.back, 2.8f, true));
            yield return c.At(1.4f);
            iker.Jump();
        }

        static IEnumerator OpenDoorsLater(ShotContext c, TrailerSets.Doors doors)
        {
            while (c.T < 2.6f) yield return null;
            AudioManager.Play(SfxId.Bell, doors.Centre, 0.8f, 0.8f);
            yield return doors.Open(2.2f);
        }

        static IEnumerator LobbyIdle(ShotContext c)
        {
            var fire = V(11f, 0f, -27f);
            foreach (var a in c.cast) a.FaceNow(fire);
            yield return c.At(4.2f);
            c.cast[0].HugNearest();
        }

        static IEnumerator LobbyGather(ShotContext c)
        {
            var fire = V(11f, 0f, -27f);
            var mauro = c.cast[0];
            var ane = c.cast[1];
            var iker = c.cast[2];
            var maite = c.cast[3];
            c.Run(ane.MoveTo(V(8.4f, 0.05f, -28.4f)));
            c.Run(iker.MoveTo(V(12.4f, 0.05f, -24.6f)));
            c.Run(maite.MoveTo(V(13.8f, 0.05f, -28.9f)));
            yield return c.At(0.7f);
            c.Say(mauro, "¿Habéis traído las cajas?", 1.7f);
            yield return c.At(2.6f);
            c.Say(ane, "¿Qué cajas?", 1.2f);
            yield return c.At(4.0f);
            c.Say(iker, "Las que explotan.", 1.5f);
            c.React(ane, R.Gasp, 1.0f);
            c.React(mauro, R.Hmph, 1.2f);
            yield return c.At(5.0f);
            foreach (var a in c.cast) c.Run(a.Face(fire, 0.5f));
            yield return c.At(5.8f);
            c.Say(maite, "Genial. Qué ilusión.", 1.6f);
        }

        static IEnumerator LobbyHug(ShotContext c)
        {
            var mauro = c.cast[0];
            var iker = c.cast[2];
            var maite = c.cast[3];
            yield return c.At(0.4f);
            mauro.HugNearest();
            c.React(mauro, R.Aww);
            c.React(c.cast[1], R.Aww, 0.15f);
            yield return c.At(1.2f);
            iker.FaceNow(maite.Position);
            iker.Shove();
            c.React(maite, R.Oof, 0.1f);
            yield return c.At(1.6f);
            c.Say(maite, "¡Oye!", 1.2f);
            yield return c.At(2.8f);
            c.Run(maite.MoveTo(iker.Position, true, 0.9f));
            yield return c.At(3.4f);
            maite.FaceNow(iker.Position);
            maite.Shove();
            c.React(iker, R.Oof, 0.1f);
            c.React(c.cast[1], R.Laugh, 0.3f);
        }

        static IEnumerator LobbyReady(ShotContext c)
        {
            var mauro = c.cast[1];
            yield return c.At(0.5f);
            c.Say(mauro, "Vale... ¿listos?", 1.5f);
            yield return c.At(2.2f);
            c.Say(c.cast[0], "¡Sí!", 1.2f);
            for (int i = 1; i < c.cast.Count; i++) c.React(c.cast[i], R.Yay, 0.04f * i);
            foreach (var a in c.cast) a.Jump();
        }

        static IEnumerator MeadowArrive(ShotContext c)
        {
            yield return c.At(0.4f);
            c.Say(c.cast[0], "Seis cajas, un camión. ¿Qué puede salir mal?", 2.0f);
            yield return c.At(2.4f);
            c.React(c.cast[1], R.Yay);
            c.React(c.cast[3], R.Yay, 0.12f);
            c.Run(c.cast[0].MoveTo(V(6f, 0f, -6f), true));
            c.Run(c.cast[1].MoveTo(V(-8f, 0f, -8f), true));
            c.Run(c.cast[2].MoveTo(V(14f, 0f, -10f), true));
            c.Run(c.cast[3].MoveTo(V(-16f, 0f, -2f), true));
            yield return c.At(3.1f);
            c.cast[2].Jump();
            c.React(c.cast[2], R.Hup);
        }

        static IEnumerator HotPotato(ShotContext c)
        {
            var iker = c.cast[0];
            var mauro = c.cast[1];
            c.Run(iker.MoveTo(V(1.0f, 0.05f, -14.4f), true));
            yield return c.At(0.2f);
            c.Say(iker, "Quema... quema... ¡QUEMA!", 2.2f);
            yield return c.At(2.4f);
            iker.GiveTo(mauro);
            c.React(iker, R.Laugh, 0.15f);
            yield return c.At(2.9f);
            c.Say(mauro, "¡¿Y me la das A MÍ?!", 1.8f);
            c.Run(mauro.MoveTo(V(0f, 0.05f, -24f), true));
            c.Run(iker.MoveTo(V(-4f, 0.05f, -6f), false));
        }

        static IEnumerator LogJump(ShotContext c)
        {
            var ane = c.cast[0];
            var maite = c.cast[1];
            var sweeper = Object.FindAnyObjectByType<Sweeper>();
            var centre = sweeper != null ? sweeper.transform.position : V(22f, 0f, 26f);
            c.Run(ane.MoveTo(centre + V(-1.6f, 0f, -2.6f), false, 0.25f));
            c.Run(maite.MoveTo(centre + V(1.6f, 0f, -2.6f), false, 0.25f));
            yield return c.At(1.0f);
            if (sweeper != null && sweeper.arm != null)
            {
                // Ane jumps just before the log reaches her; Maite does not.
                float wait = 0f;
                while (wait < 4f)
                {
                    var arm = sweeper.arm.right;
                    var toAne = ane.Position - centre;
                    float angle = Vector3.SignedAngle(new Vector3(arm.x, 0f, arm.z), new Vector3(toAne.x, 0f, toAne.z), Vector3.up);
                    if (Mathf.Abs(Mathf.Abs(angle) - 0f) < 24f || Mathf.Abs(Mathf.Abs(angle) - 180f) < 24f) break;
                    wait += Time.deltaTime;
                    yield return null;
                }
            }
            ane.Jump();
            c.React(ane, R.Hup);
            c.React(maite, R.Oof, 0.35f);
            yield return c.Wait(0.9f);
            c.Say(ane, "¡Ja!", 0.9f);
            yield return c.Wait(1.4f);
            c.Say(maite, "Vale... eso ha dolido.", 1.6f);
        }

        static IEnumerator RiverPush(ShotContext c)
        {
            var iker = c.cast[0];
            var mauro = c.cast[1];
            yield return c.At(0.2f);
            c.Say(iker, "Ni se te ocurra...", 1.4f);
            c.Run(mauro.MoveTo(iker.Position + Vector3.back * 0.9f, false, 0.2f));
            yield return c.At(1.6f);
            mauro.FaceNow(iker.Position);
            mauro.Shove();
            c.React(iker, R.Scream, 0.1f);
            yield return c.At(2.9f);
            c.Say(mauro, "Ups.", 1.2f);
        }

        static RubberDuck NearestDuck(Vector3 near)
        {
            RubberDuck best = null;
            float bestDistance = float.MaxValue;
            foreach (var duck in Object.FindObjectsByType<RubberDuck>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(duck.transform.position, near);
                if (d < bestDistance) { best = duck; bestDistance = d; }
            }
            return best;
        }

        static IEnumerator Duck(ShotContext c)
        {
            var ane = c.cast[0];
            var duck = NearestDuck(ane.Position);
            yield return c.At(0.6f);
            if (duck != null) duck.Interact(ane.Interactor);
            c.React(ane, R.Gasp, 0.1f);
            yield return c.At(0.8f);
            c.Say(ane, "Hola, patito.", 1.5f);
            yield return c.At(2.2f);
            if (duck != null) duck.Interact(ane.Interactor);
        }

        static IEnumerator Bounce(ShotContext c)
        {
            var maite = c.cast[0];
            yield return c.At(0.3f);
            c.Run(maite.MoveTo(V(-18f, 0.1f, 38.6f), true, 0.2f));
            yield return c.At(1.3f);
            c.Say(maite, "¡Allá voooy!", 1.6f);
            yield return c.At(2.0f);
            yield return maite.Walk(Vector3.forward, 1.0f);
            c.React(maite, R.Yay);
            yield return maite.MoveTo(V(-18f, 2.5f, 43.2f), false, 0.4f, 2f);
            maite.PickUp();
        }

        static IEnumerator Tunnel(ShotContext c)
        {
            var iker = c.cast[0];
            yield return c.At(0.2f);
            c.Say(iker, "Si te agachas, cabes. Es ciencia.", 2.0f);
            yield return c.At(0.6f);
            iker.Crouch(true);
            yield return iker.MoveTo(V(-37f, 0.1f, 35f), false, 0.3f, 6f, 1f);
            iker.PickUp();
        }

        static IEnumerator SummitLake(ShotContext c)
        {
            yield return c.At(0.2f);
            foreach (var a in c.cast) c.Run(a.Walk(Vector3.forward + Vector3.right * Random.Range(-0.15f, 0.15f), 1.9f, true));
            yield return c.At(1.2f);
            c.Say(c.cast[0], "¡No frenes! ¡No frenes!", 1.6f);
            yield return c.At(2.2f);
            c.Run(c.cast[3].Walk(Vector3.right, 0.6f));
            c.Run(c.cast[1].Walk(Vector3.left, 0.5f));
            c.React(c.cast[2], R.Laugh, 0.25f);
            yield return c.At(3.0f);
            c.Say(c.cast[1], "¡No puedo paraaar!", 1.6f);
            yield return c.At(4.6f);
            c.Say(c.cast[3], "Me encanta este sitio.", 1.6f);
        }

        static IEnumerator Snowman(ShotContext c)
        {
            var iker = c.cast[0];
            yield return c.At(0.2f);
            yield return iker.MoveTo(V(-22f, 0.05f, -14.6f), true, 0.2f, 2.4f);
            c.React(iker, R.Oof);
            yield return c.At(2.1f);
            c.Say(iker, "...Perdón.", 1.3f);
        }

        static IEnumerator Glacier(ShotContext c)
        {
            var maite = c.cast[0];
            yield return c.At(0.2f);
            yield return maite.MoveTo(V(-30f, 2.6f, -4.5f), false, 0.3f, 2.8f);
            maite.Stop();
            c.React(maite, R.Gasp);
            yield return c.At(3.2f);
            c.Say(maite, "¡Otra vez no!", 1.4f);
        }

        static IEnumerator HarbourRaft(ShotContext c)
        {
            yield return c.At(0.8f);
            c.Say(c.cast[0], "Qué bonito es esto...", 1.8f);
            yield return c.At(3.0f);
            c.Say(c.cast[1], "Calla y rema.", 1.4f);
            c.React(c.cast[0], R.Hmph, 1.2f);
        }

        static IEnumerator HarbourZap(ShotContext c)
        {
            var ane = c.cast[0];
            var iker = c.cast[1];
            yield return c.At(0.3f);
            c.Say(iker, "Tranquila, sé lo que hago.", 1.6f);
            yield return c.At(1.4f);
            yield return iker.Walk(Vector3.forward, 1.1f);
            c.React(iker, R.Scream, 0.2f);
            yield return c.At(3.0f);
            c.Say(ane, "¡TE DIJE QUE NO TE METIERAS AL AGUA!", 2.2f);
        }

        static IEnumerator Delivery(ShotContext c)
        {
            for (int i = 0; i < c.cast.Count; i++)
            {
                var a = c.cast[i];
                c.Run(DeliverOne(c, a, 0.3f + i * 0.9f));
            }
            yield return c.At(2.9f);
            c.Say(c.cast[0], "¡La última!", 1.2f);
        }

        static IEnumerator DeliverOne(ShotContext c, TrailerActor a, float when)
        {
            yield return c.At(when);
            var to = c.Truck != null ? c.Truck.InteractionTransform.position : V(0f, 0f, -31.5f);
            yield return a.MoveTo(new Vector3(a.Position.x * 0.5f, 0f, to.z - 1.6f), true, 0.4f, 3f);
            a.Deliver(c.Truck);
            c.React(a, R.Hup);
        }

        static IEnumerator Boom(ShotContext c)
        {
            foreach (var a in c.cast) a.FaceNow(V(0f, 0f, -26f));
            yield return c.At(0.6f);
            c.Say(c.cast[0], "¡Buen viaje!", 1.2f);
            yield return c.At(2.0f);
            c.Mark("boom");
            if (c.Truck != null) c.Run(TrailerSets.BlowUp(c.Truck, c));
            for (int i = 0; i < c.cast.Count; i++) c.React(c.cast[i], R.Gasp, 0.08f + 0.05f * i);
            yield return c.At(2.9f);
            c.React(c.cast[2], R.Laugh);
            yield return c.At(3.6f);
            c.Say(c.cast[3], "¿Quién ha puesto la caliente al lado de la eléctrica?", 2.0f);
        }

        static IEnumerator KeyArt(ShotContext c)
        {
            var camera = V(-6.8f, 0.8f, -23.5f);
            foreach (var a in c.cast) a.FaceNow(camera);
            yield return c.At(1.5f);
            c.cast[2].Jump();
            c.React(c.cast[2], R.Yay);
            c.React(c.cast[0], R.Laugh, 0.3f);
            c.React(c.cast[3], R.Laugh, 0.45f);
        }
    }
}
