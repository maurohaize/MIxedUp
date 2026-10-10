using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MixedUp.Tests
{
    /// <summary>
    /// A real online match between TWO Unity processes (a host and a client on the same machine), checking that the world is
    /// really shared: boxes taken on one machine disappear on the other, the first player to reach a box wins, deliveries land
    /// in both trucks, and the truck puzzle is the same on both screens and ends the same way.
    /// It is skipped in normal runs. Start two Unity instances with the same MIXEDUP_E2E_DIR (an empty folder), one with
    /// MIXEDUP_E2E_ROLE=host and one with MIXEDUP_E2E_ROLE=client, each running only this test.
    /// </summary>
    public class OnlineWorldE2ETests
    {
        const float Patience = 120f;
        const ushort Port = 7795;
        string dir;
        string role;

        void Put(string name, string text) => File.WriteAllText(Path.Combine(dir, name), text);
        string Get(string name)
        {
            string path = Path.Combine(dir, name);
            try { return File.Exists(path) ? File.ReadAllText(path) : null; }
            catch (IOException) { return null; }
        }

        IEnumerator WaitFor(Func<bool> condition, string what, float seconds = Patience)
        {
            float t = 0f;
            while (!condition())
            {
                t += Time.unscaledDeltaTime;
                if (t > seconds) Assert.Fail("[" + role + "] timed out waiting for: " + what);
                yield return null;
            }
        }

        static PlayerInteractor Hands => PlayerRegistry.Local.GetComponent<PlayerInteractor>();

        /// <summary>Rearranges the cargo (through the normal swap requests) so that no neighbours react.</summary>
        static void ArrangeSafely(TruckPuzzleState state, CombinationRules rules)
        {
            int n = state.Count;
            var boxes = new BoxData[n];
            for (int i = 0; i < n; i++) boxes[i] = state[i];

            var order = new int[n];
            var used = new bool[n];
            bool Search(int depth)
            {
                if (depth == n) return true;
                for (int i = 0; i < n; i++)
                {
                    if (used[i]) continue;
                    if (depth > 0 && rules.OutcomeOf(boxes[order[depth - 1]], boxes[i]) != CombinationOutcome.Safe) continue;
                    used[i] = true;
                    order[depth] = i;
                    if (Search(depth + 1)) return true;
                    used[i] = false;
                }
                return false;
            }
            Assert.IsTrue(Search(0), "a safe arrangement exists for this order");

            // Selection sort into the wanted order using swaps.
            var wanted = new BoxData[n];
            for (int i = 0; i < n; i++) wanted[i] = boxes[order[i]];
            for (int i = 0; i < n; i++)
            {
                if (state[i] == wanted[i]) continue;
                for (int j = i + 1; j < n; j++)
                {
                    if (state[j] != wanted[i]) continue;
                    state.Swap(i, j);
                    break;
                }
            }
        }

        static void DeliverEverythingHeld()
        {
            var truck = LevelDirector.Instance.truck;
            var hands = Hands;
            for (int guard = 0; guard < 4 && hands.Inventory.Count > 0; guard++)
            {
                hands.Inventory.Select(hands.Inventory.FirstOccupiedIndex());
                truck.Interact(hands);
            }
        }

        [UnityTest]
        public IEnumerator TwoMachinesShareTheWorld()
        {
            role = Environment.GetEnvironmentVariable("MIXEDUP_E2E_ROLE");
            dir = Environment.GetEnvironmentVariable("MIXEDUP_E2E_DIR");
            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(dir)) Assert.Ignore("needs two processes (see the class comment)");
            bool host = role == "host";

            // ------------------------------------------------------------ connect and start
            if (host)
            {
                OnlineSession.Host(ConnectionKind.Direct, Port);
                yield return WaitFor(() => OnlineSession.IsHost, "hosting");
                yield return WaitFor(() => NetAvatar.Sorted().Count >= 2, "the client to arrive");
                yield return WaitFor(() => NetAvatar.Host != null, "the host avatar");
                yield return new WaitForSecondsRealtime(1f);
                NetAvatar.Host.SetMap(LevelCatalog.Prototype.id);
                NetAvatar.Host.SetMode("classic");
                NetAvatar.Host.BeginMatch(424242);
                OnlineSession.LoadForAll(LevelCatalog.Prototype.scene);
            }
            else
            {
                // Retry until the host is listening.
                float t = 0f;
                while (!OnlineSession.IsOnline && t < Patience)
                {
                    if (!OnlineSession.IsBusy) OnlineSession.Join("127.0.0.1:" + Port);
                    t += 1f;
                    yield return new WaitForSecondsRealtime(1f);
                }
                Assert.IsTrue(OnlineSession.IsOnline, "[client] joined the host");
            }

            yield return WaitFor(() => SceneManager.GetActiveScene().name == LevelCatalog.Prototype.scene, "the level to load");
            yield return WaitFor(() => LevelDirector.Instance != null && PlayerRegistry.Local != null, "the level director");
            yield return WaitFor(() => !NetWorld.Waiting, "the go signal");
            Assert.IsTrue(NetWorld.Active, "[" + role + "] the online world is active");
            Assert.AreEqual(2, NetAvatar.Sorted().Count, "[" + role + "] both avatars exist");

            var director = LevelDirector.Instance;
            var start = director.Pickups.Where(p => p != null).OrderBy(p => p.NetId).ToList();
            Assert.GreaterOrEqual(start.Count, 4, "[" + role + "] the level has boxes");
            Put(role + "_layout.txt", director.LayoutSignature());
            Put(role + "_loaded", "1");
            yield return WaitFor(() => Get("host_loaded") != null && Get("client_loaded") != null, "the other machine to load");
            Assert.AreEqual(Get("host_layout.txt"), Get("client_layout.txt"), "both machines built the same boxes");

            int idA = start[0].NetId, idB = start[1].NetId, idC = start[2].NetId;

            // ------------------------------------------------------------ a box taken on one machine disappears on the other
            if (host)
            {
                start[0].Interact(Hands);
                Put("a_taken", idA.ToString());
                yield return WaitFor(() => Get("b_taken") != null, "the client to take its box");
                yield return WaitFor(() => !director.FindPickup(idB).IsAvailable, "the client's box to vanish here");
            }
            else
            {
                yield return WaitFor(() => Get("a_taken") != null, "the host to take a box");
                yield return WaitFor(() => !director.FindPickup(idA).IsAvailable, "the host's box to vanish here");
                director.FindPickup(idB).Interact(Hands);
                Put("b_taken", idB.ToString());
            }

            // ------------------------------------------------------------ both reach for the same box
            if (host) Put("contend_go", "1");
            yield return WaitFor(() => Get("contend_go") != null, "the signal");
            var contested = director.FindPickup(idC);
            contested.Interact(Hands);
            yield return new WaitForSecondsRealtime(3f);
            Put(role + "_count", Hands.Inventory.Count.ToString());
            yield return WaitFor(() => Get("host_count") != null && Get("client_count") != null, "both counts");
            int total = int.Parse(Get("host_count")) + int.Parse(Get("client_count"));
            Assert.AreEqual(3, total, "[" + role + "] the contested box ended up with exactly one player");
            Assert.IsFalse(contested.IsAvailable, "[" + role + "] the contested box is gone for both");

            // ------------------------------------------------------------ the snowman falls and the ice appears for everybody
            Assert.GreaterOrEqual(Snowman.All.Count, 1, "[" + role + "] the level has a snowman");
            Vector3? iceSeen = null;
            IceTrailEmitter.Placed += position => iceSeen = position;
            if (host)
            {
                Snowman.All[0].Collapse(Hands.transform.position);
                var emitter = PlayerRegistry.Local.GetComponent<IceTrailEmitter>();
                Assert.IsNotNull(emitter.slabPrefab, "[host] the player can leave ice");
                emitter.Place(new Vector3(7f, 0f, 9f));
                Put("snowman_ice", "1");
            }
            else
            {
                yield return WaitFor(() => Get("snowman_ice") != null, "the host's snowman and ice");
                yield return WaitFor(() => Snowman.All[0].IsCollapsed, "the snowman to fall here too");
                yield return WaitFor(() => iceSeen.HasValue, "the host's ice to appear here");
                Assert.AreEqual(7f, iceSeen.Value.x, 0.01f, "[client] the slab is where the host put it");
            }

            // ------------------------------------------------------------ deliveries reach both trucks
            var truck = director.truck;
            DeliverEverythingHeld();
            yield return new WaitForSecondsRealtime(1.5f);
            Put(role + "_delivered", "1");
            yield return WaitFor(() => Get("host_delivered") != null && Get("client_delivered") != null, "both deliveries");
            yield return WaitFor(() => truck.TotalDelivered >= 3, "three boxes in the truck on this machine");
            Assert.AreEqual(3, truck.TotalDelivered, "[" + role + "] exactly three boxes are in the truck on this machine");

            if (host)
            {
                // The host brings the rest of the order.
                float t = 0f;
                while (!truck.IsComplete && t < Patience)
                {
                    foreach (var pickup in director.Pickups.Where(p => p != null && p.IsAvailable && p.NetId < NetWorld.DropIdBase).ToList())
                    {
                        if (Hands.Inventory.IsFull) DeliverEverythingHeld();
                        pickup.Interact(Hands);
                    }
                    DeliverEverythingHeld();
                    t += 0.5f;
                    yield return new WaitForSecondsRealtime(0.5f);
                }
                Assert.IsTrue(truck.IsComplete, "[host] the order is complete");
            }
            yield return WaitFor(() => truck.IsComplete, "the order to be complete here");

            // ------------------------------------------------------------ the puzzle is the same on both screens
            yield return WaitFor(() => GameManager.Instance.State == GameState.TruckPuzzle, "the puzzle phase");
            var controller = UnityEngine.Object.FindAnyObjectByType<TruckPuzzleController>();
            yield return WaitFor(() => controller.State != null, "the puzzle state");
            var state = controller.State;
            Assert.IsTrue(state.Networked, "[" + role + "] the puzzle is networked");
            Assert.AreEqual(host, state.Authoritative, "[" + role + "] only the host is authoritative");

            string before = string.Join(",", Enumerable.Range(0, state.Count).Select(i => state[i].id));
            if (host)
            {
                Put("arrangement_before", before);
                ArrangeSafely(state, controller.rules);
                yield return new WaitForSecondsRealtime(1f);
                Put("arrangement_after", string.Join(",", Enumerable.Range(0, state.Count).Select(i => state[i].id)));
            }
            yield return WaitFor(() => Get("arrangement_after") != null, "the host's arrangement");
            string expected = Get("arrangement_after");
            yield return WaitFor(() => string.Join(",", Enumerable.Range(0, state.Count).Select(i => state[i].id)) == expected,
                "the same arrangement on this screen");

            // The client rearranges too: the host (the referee) applies it for both.
            if (!host)
            {
                // Two boxes of the same kind trade places: the request travels to the host and back, and changes nothing.
                int a = 0, b = 1;
                for (int i = 0; i < state.Count; i++)
                    for (int j = i + 1; j < state.Count; j++)
                        if (state[i] == state[j]) { a = i; b = j; }
                state.Swap(a, b);
                yield return new WaitForSecondsRealtime(1f);
            }
            yield return new WaitForSecondsRealtime(1.5f);
            Put(role + "_final_arrangement", string.Join(",", Enumerable.Range(0, state.Count).Select(i => state[i].id)));
            yield return WaitFor(() => Get("host_final_arrangement") != null && Get("client_final_arrangement") != null, "both final arrangements");
            Assert.AreEqual(Get("host_final_arrangement"), Get("client_final_arrangement"), "the cargo hold is identical on both machines");

            // ------------------------------------------------------------ the trip ends the same way for everybody
            if (host) controller.StartTravel();
            yield return WaitFor(() => state.Phase != PuzzlePhase.Arranging, "the trip to start here");
            yield return WaitFor(() => state.Phase == PuzzlePhase.Resolved, "the trip to end", 90f);
            string outcome = state.Resolution.Outcome + "/" + state.Resolution.Pair + "/" + state.Resolution.FuseExpired;
            Put(role + "_outcome", outcome);
            yield return WaitFor(() => Get("host_outcome") != null && Get("client_outcome") != null, "both outcomes");
            Assert.AreEqual(Get("host_outcome"), Get("client_outcome"), "the trip ended the same way for both players");

            // The whole team is paid the same amount.
            yield return WaitFor(() => controller.LastResult != null, "the delivery result");
            Put(role + "_pay", controller.LastResult.Reward + "/" + controller.LastResult.TimeBonus);
            yield return WaitFor(() => Get("host_pay") != null && Get("client_pay") != null, "both payments");
            Assert.AreEqual(Get("host_pay"), Get("client_pay"), "both players were paid the same");
            Assert.AreEqual(CombinationOutcome.Safe, state.Resolution.Outcome, "[" + role + "] the safe arrangement was delivered safely");
            Assert.Greater(controller.LastResult.Reward, 0, "[" + role + "] a safe delivery pays something");

            Put(role + "_result", "PASS");
            yield return new WaitForSecondsRealtime(2f);
            OnlineSession.Leave();
        }
    }
}

