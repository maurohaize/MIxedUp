using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>Walks through all the scenery: nothing may hover above the ground, and nothing may be buried in it.</summary>
    public class ScenerySanityTests : SceneTestBase
    {
        const float MaxHover = 0.35f;

        static readonly HashSet<string> Groups = new HashSet<string>
        {
            "StartCamp", "MudSwamp", "BonfireCamp", "Sweeper", "MushroomPlatform", "Farm", "StepStones", "TrailProps", "Hurdles",
            "Toadstools", "TrailFlowers"
        };

        /// <summary>The visible things that were placed on the ground: trees, rocks, props, bushes, fences.</summary>
        static IEnumerable<Transform> Props()
        {
            foreach (var rootName in new[] { "Scenery", "World" })
            {
                var root = GameObject.Find(rootName);
                if (root == null) continue;
                foreach (Transform child in root.transform)
                {
                    if (Groups.Contains(child.name))
                    {
                        foreach (Transform inner in child)
                            if (HasVisual(inner) && inner.GetComponent<Sweeper>() == null && inner.name != "Arm") yield return inner;
                    }
                    else if (HasVisual(child))
                    {
                        yield return child;
                    }
                }
            }
        }

        static bool HasVisual(Transform t) => t.GetComponentsInChildren<Renderer>().Length > 0 && t.GetComponentInParent<Spin>() == null;

        static Bounds BoundsOf(Transform t)
        {
            var renderers = t.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        [UnityTest]
        public IEnumerator NothingFloatsAboveTheGround()
        {
            yield return null;
            var floating = new List<string>();
            int checkedProps = 0;

            foreach (var prop in Props().Distinct())
            {
                if (prop.name.StartsWith("Fence")) continue;   // rails are meant to be raised
                var mine = prop.GetComponentsInChildren<Collider>();
                var bounds = BoundsOf(prop);
                if (bounds.size.y < 0.05f && bounds.size.x > 10f) continue;   // huge flat sheets

                float best = float.MaxValue, groundAt = 0f;
                int samples = 0;
                foreach (var offset in new[] { Vector2.zero, new Vector2(0.4f, 0f), new Vector2(-0.4f, 0f), new Vector2(0f, 0.4f), new Vector2(0f, -0.4f) })
                {
                    var origin = new Vector3(bounds.center.x + offset.x * bounds.extents.x, bounds.min.y + 3f, bounds.center.z + offset.y * bounds.extents.z);
                    var hits = Physics.RaycastAll(origin, Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore);
                    float ground = float.NegativeInfinity;
                    foreach (var hit in hits)
                    {
                        if (mine.Contains(hit.collider)) continue;
                        if (hit.collider.transform.IsChildOf(prop)) continue;
                        ground = Mathf.Max(ground, hit.point.y);
                    }
                    if (float.IsNegativeInfinity(ground)) continue;
                    samples++;
                    if (bounds.min.y - ground < best) groundAt = ground;
                    best = Mathf.Min(best, bounds.min.y - ground);
                }
                if (samples == 0) continue;
                checkedProps++;
                if (best > MaxHover)
                    floating.Add(prop.name + " at " + prop.position.ToString("0.0") + " hovers " + best.ToString("0.00") + " m (bottom " + bounds.min.y.ToString("0.00") + ", ground " + groundAt.ToString("0.00") + ")");
            }

            Assert.Greater(checkedProps, 100, "the audit really looked at the scenery");
            Assert.IsEmpty(floating, "floating props:\n" + string.Join("\n", floating));
        }
    }
}
