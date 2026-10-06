using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>What lies beyond the edge of the map: the river's continuation, the lake, the cave and the road.</summary>
    public class OutskirtsTests : SceneTestBase
    {
        static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
            bool neg = d1 < 0f || d2 < 0f || d3 < 0f, pos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(neg && pos);
        }

        static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

        static bool InWater(Vector2 p, IEnumerable<MeshFilter> waters)
        {
            foreach (var water in waters)
            {
                var mesh = water.sharedMesh;
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    Vector3 a = water.transform.TransformPoint(vertices[triangles[i]]), b = water.transform.TransformPoint(vertices[triangles[i + 1]]), c = water.transform.TransformPoint(vertices[triangles[i + 2]]);
                    if (InTriangle(p, new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(c.x, c.z))) return true;
                }
            }
            return false;
        }

        [UnityTest]
        public IEnumerator TheRiverRunsOnBeyondBothEdgesOfTheMap()
        {
            var beyond = GameObject.Find("RiverBeyond");
            Assert.IsNotNull(beyond);
            var west = beyond.transform.Find("WaterWest").GetComponent<MeshRenderer>().bounds;
            var east = beyond.transform.Find("WaterEast").GetComponent<MeshRenderer>().bounds;
            var lake = beyond.transform.Find("Lake").GetComponent<MeshRenderer>().bounds;

            Assert.Less(west.min.x, -108f, "the west river reaches the cave");
            Assert.Greater(west.max.x, -46f, "and joins the map's river");
            Assert.Less(east.min.x, 46f);
            Assert.Greater(lake.max.x, 115f, "the lake is big");
            Assert.Less(Mathf.Abs(lake.center.z - 8f), 3f);
            Assert.IsNotNull(beyond.transform.Find("Cave"), "the river springs from a cave");
            Assert.IsNotNull(beyond.transform.Find("Pier"), "with a pier on the lake");
            yield break;
        }

        [UnityTest]
        public IEnumerator NoTreesOrRocksStandInTheWater()
        {
            var beyond = GameObject.Find("RiverBeyond").transform;
            var waters = new[] { "WaterWest", "WaterEast", "Lake" }.Select(n => beyond.Find(n).GetComponent<MeshFilter>()).ToArray();
            var outside = GameObject.Find("Outside").transform;

            var drowned = new List<string>();
            foreach (Transform prop in outside)
            {
                if (prop.name.StartsWith("Road") || prop.name == "Road") continue;
                if (InWater(new Vector2(prop.position.x, prop.position.z), waters)) drowned.Add(prop.name + " at " + prop.position.ToString("0.0"));
            }
            Assert.IsEmpty(drowned, string.Join("\n", drowned));
            yield break;
        }

        [UnityTest]
        public IEnumerator ARoadLeadsFromTheHorizonToTheTruck()
        {
            var road = GameObject.Find("Road");
            Assert.IsNotNull(road);
            var bounds = road.transform.Find("RoadSurface").GetComponent<MeshRenderer>().bounds;
            Assert.Less(bounds.min.z, -160f, "it comes from far away");
            Assert.Greater(bounds.max.z, -32f, "and ends at the truck's dock");
            yield break;
        }

        [UnityTest]
        public IEnumerator ThereIsNoFog()
        {
            Assert.IsFalse(RenderSettings.fog);
            yield break;
        }
    }
}
