using TMPro;
using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>Small extras scattered over the map: rubber ducks, a ferry raft, gusts of wind and the sign of the spinning log.</summary>
    public static partial class PrototypeBuilder
    {
        static Mesh raftMesh, duckMesh;

        static void BuildSecrets(Transform world, WorldMeshes wm, Material mat, Mats m, ArtAssets art)
        {
            var secrets = new GameObject("Secrets").transform;
            secrets.SetParent(world, false);
            raftMesh = SaveInkedMesh(LowPolyProps.Raft());
            duckMesh = SaveInkedMesh(LowPolyProps.Duck());

            // --- rubber ducks in the river
            foreach (var (x, z, yaw) in new[] { (4.5f, 6.4f, 30f), (-14f, 11.7f, 200f), (33f, 7f, 110f) })
            {
                var duck = new GameObject("RubberDuck");
                duck.transform.SetParent(secrets, false);
                duck.transform.position = new Vector3(x, -0.1f, z);
                duck.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                var visual = MeshObject("Visual", duck.transform, duckMesh, mat);
                visual.isStatic = false;
                var trigger = duck.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.6f;
                trigger.center = Vector3.up * 0.3f;
                var script = duck.AddComponent<RubberDuck>();
                script.visual = visual.transform;
            }

            // --- the ferry: a raft that shuttles across the river
            var raft = new GameObject("Raft");
            raft.transform.SetParent(secrets, false);
            raft.transform.position = new Vector3(-8f, 0f, 6.4f);
            var deck = MeshObject("Deck", raft.transform, raftMesh, mat);
            deck.isStatic = false;
            var collider = raft.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, -0.05f, 0f);
            collider.size = new Vector3(3f, 0.2f, 3f);
            var body = raft.AddComponent<Rigidbody>();
            body.isKinematic = true;
            var ferry = raft.AddComponent<MovingRaft>();
            ferry.pointA = new Vector3(-8f, 0f, 6.4f);
            ferry.pointB = new Vector3(-8f, 0f, 11.6f);
            // a short lamp post on a corner of the raft
            var raftLamp = MeshObject("RaftLamp", raft.transform, lampMesh, art.palette);
            raftLamp.isStatic = false;
            raftLamp.transform.localPosition = new Vector3(-1.3f, 0.05f, -1.3f);
            raftLamp.transform.localScale = Vector3.one * 0.7f;
            AddLampLight(raftLamp.transform, new Vector3(0f, 2.78f, 0f), 9f, 1.8f);

            // --- gusts of wind on the trail between the stepping stones and the windmill
            var gust = new GameObject("GustZone");
            gust.transform.SetParent(secrets, false);
            gust.transform.position = new Vector3(18f, 1.5f, 17f);
            var zone = gust.AddComponent<GustZone>();
            zone.direction = new Vector3(1f, 0f, 0.15f);
            zone.halfExtents = new Vector3(5.5f, 2.5f, 3.5f);

            var wind = NewParticles("Wind", gust.transform, fxSoft);
            wind.transform.localPosition = new Vector3(-5.5f, 0f, 0f);
            var main = wind.main;
            main.loop = true;
            main.startLifetime = 0.9f;
            main.startSpeed = 12f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new Color(1f, 1f, 1f, 0.55f);
            main.maxParticles = 120;
            var emission = wind.emission;
            emission.enabled = false;
            var shape = wind.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.2f, 2.4f, 6.5f);
            shape.rotation = new Vector3(0f, 90f, 0f);
            var windRenderer = wind.GetComponent<ParticleSystemRenderer>();
            windRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            windRenderer.lengthScale = 9f;
            zone.wind = wind;
        }

        /// <summary>The sign beside the spinning log: best streaks of jumps over it.</summary>
        static void BuildSweeperSign(Transform parent, Sweeper sweeper, Mats m, Vector3 position, float yaw)
        {
            var root = new GameObject("SweeperSign");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            Prim(PrimitiveType.Cube, "Post", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.16f, 1.8f, 0.16f), m.woodDark, true, null, true);
            Prim(PrimitiveType.Cube, "Board", root.transform, new Vector3(0f, 1.75f, 0f), new Vector3(1.6f, 1.05f, 0.1f), m.wood, false, null, true);

            var faces = new TMP_Text[2];
            for (int i = 0; i < 2; i++)
            {
                float z = i == 0 ? 0.07f : -0.07f;
                Prim(PrimitiveType.Cube, "Paper", root.transform, new Vector3(0f, 1.75f, z), new Vector3(1.46f, 0.92f, 0.03f), m.marker, false);

                var textObject = new GameObject("Text" + i);
                textObject.transform.SetParent(root.transform, false);
                textObject.transform.localPosition = new Vector3(0f, 1.75f, z * 1.5f);
                // TextMeshPro text is read from its -Z side, so the text on the front (+Z) face is turned round.
                textObject.transform.localRotation = Quaternion.Euler(0f, i == 0 ? 180f : 0f, 0f);
                var text = textObject.AddComponent<TextMeshPro>();
                if (UiFactory.Font != null) text.font = UiFactory.Font;
                text.alignment = TextAlignmentOptions.Center;
                text.color = UiFactory.Ink;
                text.enableAutoSizing = true;
                text.fontSizeMin = 0.4f;
                text.fontSizeMax = 1.6f;
                text.rectTransform.sizeDelta = new Vector2(1.34f, 0.84f);
                text.text = string.Empty;
                faces[i] = text;
            }

            var sign = root.AddComponent<SweeperSign>();
            sign.sweeper = sweeper;
            sign.faces = faces;
        }
    }
}
