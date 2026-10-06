using UnityEngine;

namespace MixedUp.EditorTools
{
    /// <summary>
    /// The ice hill: a 5 m rocky plateau under a thick cap of snow, reached by an icy ramp (slippery) or by a safe staircase on
    /// its east side. Dressed with pines, snowdrifts, ice crystals, a snowman, handrails, lanterns, flags and falling snow.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        static Mesh[] iceCrystals, snowPines, snowDrifts, flags;
        static Mesh icePlateau, snowman, lampMesh;

        static GameObject iceSlabPrefab;

        /// <summary>One slab of the ice a frozen box leaves in the river: solid, with a trigger above it that counts as dry land.</summary>
        static GameObject BuildIceSlab(ArtAssets art)
        {
            var root = new GameObject("IceSlab");
            var solid = root.AddComponent<BoxCollider>();
            solid.center = new Vector3(0f, -0.19f, 0f);
            solid.size = new Vector3(1.8f, 0.42f, 1.8f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = SaveInkedMesh(LowPolyProps.IceSlabMesh());
            visual.AddComponent<MeshRenderer>().sharedMaterials = new[] { art.palette, propInk };

            var sheetObject = new GameObject("Sheet");
            sheetObject.transform.SetParent(root.transform, false);
            var sheet = sheetObject.AddComponent<BoxCollider>();
            sheet.isTrigger = true;
            // Starts above the water surface, so the one who wades under the ice is not "on" it.
            sheet.center = new Vector3(0f, 0.4f, 0f);
            sheet.size = new Vector3(2.0f, 1.0f, 2.0f);
            sheetObject.AddComponent<HazardZone>().type = HazardType.IceSheet;

            var slab = root.AddComponent<IceSlab>();
            slab.visual = visual.transform;
            slab.solid = solid;
            slab.sheet = sheet;
            return root;
        }

        static void CreateIceMeshes()
        {
            lampMesh = SaveInkedMesh(LowPolyProps.LampPost());
            icePlateau = SaveInkedMesh(LowPolyProps.IcePlateau());
            snowman = SaveInkedMesh(LowPolyProps.Snowman());
            snowPines = new[] { SaveInkedMesh(LowPolyProps.SnowPine(1)), SaveInkedMesh(LowPolyProps.SnowPine(2)) };
            iceCrystals = new[] { SaveInkedMesh(LowPolyProps.IceCrystals(3)), SaveInkedMesh(LowPolyProps.IceCrystals(7)), SaveInkedMesh(LowPolyProps.IceCrystals(11)) };
            snowDrifts = new[] { SaveInkedMesh(LowPolyProps.SnowDrift(1)), SaveInkedMesh(LowPolyProps.SnowDrift(5)) };
            flags = new[] { SaveInkedMesh(LowPolyProps.Flag(22)), SaveInkedMesh(LowPolyProps.Flag(16)) };
        }

        /// <summary>A rail or beam between two points, as a thin cube.</summary>
        static GameObject Beam(Transform parent, string name, Vector3 from, Vector3 to, float thickness, Material material, bool ink = true)
        {
            Vector3 along = to - from;
            return Prim(PrimitiveType.Cube, name, parent, (from + to) * 0.5f, new Vector3(thickness, thickness, along.magnitude), material,
                false, Quaternion.LookRotation(along.normalized), ink);
        }

        static void BuildIceHill(Transform env, Mats m, ArtAssets art)
        {
            CreateIceMeshes();
            var hill = new GameObject("IceHill").transform;
            hill.SetParent(env, false);

            const float plateauTop = 5f;
            const float width = 8f;
            var mat = art.palette;

            // --- the plateau: a box collider (the flat top is what you walk on) and the rocky, snow-capped model
            var plateau = new GameObject("Plateau");
            plateau.transform.SetParent(hill, false);
            plateau.transform.position = new Vector3(-32f, 0f, -6f);
            var plateauCollider = plateau.AddComponent<BoxCollider>();
            plateauCollider.center = new Vector3(0f, plateauTop * 0.5f, 0f);
            plateauCollider.size = new Vector3(10f, plateauTop, 10f);
            plateau.isStatic = true;
            var plateauVisual = MeshObject("PlateauVisual", plateau.transform, icePlateau, mat);
            plateauVisual.transform.localPosition = Vector3.zero;

            // --- the icy ramp on the south side
            var start = new Vector3(-32f, 0f, -25f);
            var end = new Vector3(-32f, plateauTop, -11f);
            var rotation = Quaternion.LookRotation((end - start).normalized, Vector3.up);
            float length = (end - start).magnitude;
            const float thickness = 0.5f;

            var center = (start + end) * 0.5f - rotation * Vector3.up * (thickness * 0.5f);
            Prim(PrimitiveType.Cube, "IceRamp", hill, center, new Vector3(width, thickness, length), m.ice, true, rotation, true);

            var iceCenter = (start + end) * 0.5f + rotation * Vector3.up * 0.3f;
            HazardVolume("IceVolume", hill, iceCenter, new Vector3(width, 1f, length), rotation, HazardType.Slippery);

            var rng = new System.Random(77);

            // snowbanks and crystals along both edges of the ramp, and a frosty arch of drifts at its foot
            for (int i = 0; i < 9; i++)
            {
                float t = (i + 0.5f) / 9f;
                foreach (var side in new[] { -1f, 1f })
                {
                    var onRamp = Vector3.Lerp(start, end, t) + new Vector3(side * (width * 0.5f + 0.5f), 0f, 0f);
                    var drift = MeshObject("SnowBank", hill, snowDrifts[i % 2], mat);
                    drift.transform.position = onRamp + Vector3.down * 0.1f;
                    drift.transform.rotation = Quaternion.Euler(0f, 90f + (float)rng.NextDouble() * 40f, 0f);
                    drift.transform.localScale = Vector3.one * (0.9f + (float)rng.NextDouble() * 0.5f);
                }
            }
            foreach (var (x, y, z) in new[] { (-36.6f, 1.3f, -22f), (-27.4f, 2.4f, -18.5f), (-36.8f, 3.6f, -15f), (-27.6f, 4.5f, -12.2f) })
            {
                var crystal = MeshObject("RampCrystals", hill, iceCrystals[rng.Next(iceCrystals.Length)], mat);
                crystal.transform.position = new Vector3(x, y, z);
                crystal.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            }
            foreach (var x in new[] { -35.6f, -28.4f })
            {
                PlaceProp(hill, art.cartel, new Vector3(x, 0f, -26.5f), 180f, 1f);
            }

            // --- the staircase on the east side: wooden steps with a dusting of snow, handrails, lanterns and flags
            const int steps = 12;
            float rise = plateauTop / steps;
            var stepTops = new Vector3[steps];
            for (int i = 0; i < steps; i++)
            {
                float top = rise * (i + 1);
                float x = -27f + 1.2f * (steps - i) - 0.6f;
                Prim(PrimitiveType.Cube, "Step" + (i + 1), hill, new Vector3(x, top * 0.5f, -6f), new Vector3(1.2f, top, 4f), m.wood, true, null, true);
                stepTops[i] = new Vector3(x, top, -6f);
                // snow lying on the step
                var snow = Prim(PrimitiveType.Cube, "StepSnow", hill, new Vector3(x - 0.05f, top + 0.03f, -6f), new Vector3(0.95f, 0.07f, 3.7f), m.snow, false);
                snow.isStatic = true;
            }

            foreach (var side in new[] { -1f, 1f })
            {
                float z = -6f + side * 2.2f;
                Vector3 prevHigh = default, prevLow = default;
                for (int i = 0; i < steps; i += 3)
                {
                    var foot = new Vector3(stepTops[i].x, stepTops[i].y, z);
                    Prim(PrimitiveType.Cube, "RailPost", hill, foot + Vector3.up * 0.55f, new Vector3(0.14f, 1.1f, 0.14f), m.woodDark, true, null, true);
                    var high = foot + Vector3.up * 1.05f;
                    var low = foot + Vector3.up * 0.6f;
                    if (i > 0)
                    {
                        Beam(hill, "HandRail", prevHigh, high, 0.1f, m.wood);
                        Beam(hill, "LowRail", prevLow, low, 0.07f, m.wood);
                    }
                    prevHigh = high;
                    prevLow = low;
                }
                // last post at the very top, and the rail running to it
                var topFoot = new Vector3(stepTops[steps - 1].x, plateauTop, z);
                Prim(PrimitiveType.Cube, "RailPost", hill, topFoot + Vector3.up * 0.55f, new Vector3(0.14f, 1.1f, 0.14f), m.woodDark, true, null, true);
                Beam(hill, "HandRail", prevHigh, topFoot + Vector3.up * 1.05f, 0.1f, m.wood);
                Beam(hill, "LowRail", prevLow, topFoot + Vector3.up * 0.6f, 0.07f, m.wood);
            }

            // lanterns at the top of the stairs, flags at the bottom
            foreach (var z in new[] { -8.7f, -3.3f })
            {
                var lamp = Prop(hill, "Lamp", lampMesh, mat, -27.8f, z, 0f, 1f, Solid.Capsule, new Vector3(0f, 1.5f, 0f), new Vector3(0.12f, 3f, 0f), plateauTop);
                AddLampLight(lamp.transform, new Vector3(0f, 2.75f, 0f), 11f, 2.8f);
            }
            for (int i = 0; i < 2; i++)
            {
                float z = i == 0 ? -8.4f : -3.6f;
                Prop(hill, "Flag", flags[i], mat, -11.9f, z, i * 180f, 1f, Solid.Capsule, new Vector3(0f, 1.3f, 0f), new Vector3(0.1f, 2.6f, 0f));
            }

            // --- on top: pines, a snowman, crystals around the frozen box
            foreach (var (x, z, s) in new[] { (-35.7f, -9.6f, 0.8f), (-35.9f, -2.3f, 0.9f), (-28.9f, -1.7f, 0.7f) })
                Prop(hill, "SnowPine", snowPines[rng.Next(2)], mat, x, z, (float)rng.NextDouble() * 360f, s, Solid.Capsule, new Vector3(0f, 1.5f, 0f), new Vector3(0.3f, 3f, 0f), plateauTop);
            Prop(hill, "Snowman", snowman, mat, -35.1f, -6.2f, 90f, 1f, Solid.Capsule, new Vector3(0f, 1f, 0f), new Vector3(0.6f, 2.2f, 0f), plateauTop);
            foreach (var (x, z) in new[] { (-33.6f, -9.9f), (-29.8f, -2.4f), (-35.4f, -3.8f) })
            {
                var crystal = MeshObject("TopCrystals", hill, iceCrystals[rng.Next(iceCrystals.Length)], mat);
                crystal.transform.position = new Vector3(x, plateauTop, z);
                crystal.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            }

            // --- around the base: pines, drifts and boulders so it reads as a natural outcrop
            foreach (var (x, z, s) in new[] { (-39.6f, -12.5f, 1.2f), (-40.2f, -5f, 1.1f), (-39.4f, 1.8f, 1.15f), (-34.5f, 3.2f, 1.0f), (-24.5f, -12.8f, 1.1f), (-21.8f, -3.2f, 0.95f), (-39f, -20f, 1.1f), (-25.2f, -23f, 0.9f) })
                Prop(hill, "SnowPine", snowPines[rng.Next(2)], mat, x, z, (float)rng.NextDouble() * 360f, s, Solid.Capsule, new Vector3(0f, 1.5f, 0f), new Vector3(0.35f, 3f, 0f));
            for (int i = 0; i < 16; i++)
            {
                float angle = i / 16f * 360f;
                var spot = new Vector3(-32f, 0f, -6f) + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (6.6f + (float)rng.NextDouble() * 0.8f);
                if (spot.x > -29.5f && spot.z > -9.5f && spot.z < -2.5f) continue;                  // keep the stairs clear
                if (spot.x > -37.5f && spot.x < -26.5f && spot.z < -10.5f && spot.z > -26f) continue;  // and the ramp
                var drift = MeshObject("Drift", hill, snowDrifts[i % 2], mat);
                drift.transform.position = new Vector3(spot.x, GroundHeight(spot.x, spot.z) - 0.05f, spot.z);
                drift.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                drift.transform.localScale = Vector3.one * (1f + (float)rng.NextDouble() * 0.7f);
            }

            // boulders at the foot of the plateau (kept from before)
            for (int i = 0; i < 9; i++)
            {
                float angle = (float)rng.NextDouble() * 360f;
                var spot = new Vector3(-32f, 0f, -6f) + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * (6.8f + (float)rng.NextDouble() * 1.5f);
                if (spot.x > -29f && spot.z > -9f && spot.z < -3f) continue;
                if (spot.x > -37f && spot.x < -27f && spot.z < -11f && spot.z > -26f) continue;
                PlaceProp(hill, art.rocks[rng.Next(art.rocks.Length)], spot, angle, 0.7f + (float)rng.NextDouble() * 0.6f);
            }

            AddSnowfall(hill, new Vector3(-30f, 13f, -12f), new Vector3(26f, 1f, 32f));
        }
    }
}
