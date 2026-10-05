using NUnit.Framework;
using UnityEngine;

namespace MixedUp.Tests
{
    public class InteractionTests
    {
        TestFactory f;
        readonly System.Collections.Generic.List<GameObject> objects = new System.Collections.Generic.List<GameObject>();

        [SetUp] public void SetUp() => f = new TestFactory();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
            objects.Clear();
            f.Cleanup();
        }

        PlayerInteractor Actor(string name = "Actor")
        {
            var go = new GameObject(name);
            objects.Add(go);
            var interactor = go.AddComponent<PlayerInteractor>();
            go.AddComponent<PlayerPassTarget>();
            return interactor;
        }

        BoxPickup Pickup(BoxData data)
        {
            var go = new GameObject("Pickup_" + data.id);
            objects.Add(go);
            go.AddComponent<BoxCollider>();
            var pickup = go.AddComponent<BoxPickup>();
            pickup.data = data;
            return pickup;
        }

        Truck MakeTruck(OrderData order)
        {
            var go = new GameObject("Truck");
            objects.Add(go);
            var truck = go.AddComponent<Truck>();
            truck.order = order;
            return truck;
        }

        OrderData Order(BoxData a, int countA, BoxData b = null, int countB = 0)
        {
            var order = f.Make<OrderData>();
            order.lines = b == null
                ? new[] { new OrderLine { box = a, count = countA } }
                : new[] { new OrderLine { box = a, count = countA }, new OrderLine { box = b, count = countB } };
            return order;
        }

        // ---------------------------------------------------------------- pickups

        [Test]
        public void PickingUpABoxMakesItDisappearFromTheWorld()
        {
            var actor = Actor();
            var pickup = Pickup(f.Box("hot"));

            Assert.IsTrue(pickup.TryGetPrompt(actor, out var prompt));
            Assert.IsTrue(prompt.Enabled);
            Assert.AreEqual("prompt.pickup", prompt.Key);

            pickup.Interact(actor);

            Assert.AreEqual(1, actor.Inventory.Count);
            Assert.IsFalse(pickup.gameObject.activeSelf);
            Assert.IsFalse(pickup.IsAvailable);
        }

        [Test]
        public void FullInventoryBlocksPickupAndLeavesTheBoxInTheWorld()
        {
            var actor = Actor();
            actor.Inventory.TryAdd(f.Box("a"), out _);
            actor.Inventory.TryAdd(f.Box("b"), out _);
            var pickup = Pickup(f.Box("hot"));

            Assert.IsTrue(pickup.TryGetPrompt(actor, out var prompt));
            Assert.IsFalse(prompt.Enabled);
            Assert.AreEqual("prompt.inventory_full", prompt.Key);

            pickup.Interact(actor);
            Assert.IsTrue(pickup.gameObject.activeSelf);
            Assert.IsTrue(pickup.IsAvailable);
            Assert.AreEqual(2, actor.Inventory.Count);
        }

        [Test]
        public void ACollectedBoxCannotBePickedUpTwice()
        {
            var first = Actor("first");
            var second = Actor("second");
            var pickup = Pickup(f.Box("hot"));

            pickup.Interact(first);
            pickup.Interact(second);

            Assert.AreEqual(1, first.Inventory.Count);
            Assert.AreEqual(0, second.Inventory.Count);
            Assert.IsFalse(pickup.TryGetPrompt(second, out _));
        }

        // ---------------------------------------------------------------- passing

        [Test]
        public void PassingMovesTheSelectedBoxToTheTeammate()
        {
            var giver = Actor("giver");
            var receiver = Actor("receiver");
            var hot = f.Box("hot");
            var toxic = f.Box("toxic");
            giver.Inventory.TryAdd(hot, out _);
            giver.Inventory.TryAdd(toxic, out _);
            giver.Inventory.Select(1);

            var target = receiver.GetComponent<PlayerPassTarget>();
            Assert.IsTrue(target.TryGetPrompt(giver, out var prompt));
            Assert.AreEqual("prompt.pass", prompt.Key);
            Assert.IsTrue(prompt.Enabled);

            target.Interact(giver);

            Assert.AreEqual(1, giver.Inventory.Count);
            Assert.AreSame(hot, giver.Inventory.Selected.box);
            Assert.AreEqual(1, receiver.Inventory.Count);
            Assert.AreSame(toxic, receiver.Inventory.Selected.box);
        }

        [Test]
        public void PassingIsBlockedWhenTheTeammateIsFull()
        {
            var giver = Actor("giver");
            var receiver = Actor("receiver");
            giver.Inventory.TryAdd(f.Box("hot"), out _);
            receiver.Inventory.TryAdd(f.Box("a"), out _);
            receiver.Inventory.TryAdd(f.Box("b"), out _);

            var target = receiver.GetComponent<PlayerPassTarget>();
            Assert.IsTrue(target.TryGetPrompt(giver, out var prompt));
            Assert.IsFalse(prompt.Enabled);

            target.Interact(giver);
            Assert.AreEqual(1, giver.Inventory.Count);
        }

        [Test]
        public void NothingToPassMeansNoPrompt()
        {
            var giver = Actor("giver");
            var receiver = Actor("receiver");
            Assert.IsFalse(receiver.GetComponent<PlayerPassTarget>().TryGetPrompt(giver, out _));
        }

        [Test]
        public void PassingStartsACooldownForBothPlayers()
        {
            var giver = Actor("giver");
            var receiver = Actor("receiver");
            giver.passCooldown = receiver.passCooldown = 30f;
            giver.Inventory.TryAdd(f.Box("hot"), out _);

            receiver.GetComponent<PlayerPassTarget>().Interact(giver);

            Assert.IsFalse(giver.PassReady);
            Assert.IsFalse(receiver.PassReady, "the receiver must not be able to bounce it straight back");
        }

        [Test]
        public void DummyTeammateLetsYouTakeTheBoxBack()
        {
            var player = Actor("player");
            var dummy = Actor("dummy");
            dummy.GetComponent<PlayerPassTarget>().allowTakeBack = true;
            var box = f.Box("hot");
            dummy.Inventory.TryAdd(box, out _);

            var target = dummy.GetComponent<PlayerPassTarget>();
            Assert.IsTrue(target.TryGetPrompt(player, out var prompt));
            Assert.AreEqual("prompt.take_back", prompt.Key);

            target.Interact(player);
            Assert.AreSame(box, player.Inventory.Selected.box);
            Assert.AreEqual(0, dummy.Inventory.Count);
        }

        [Test]
        public void ADeadTeammateCannotReceiveBoxes()
        {
            var giver = Actor("giver");
            var receiver = Actor("receiver");
            giver.Inventory.TryAdd(f.Box("hot"), out _);
            receiver.Status.Kill(DeathCause.Void);

            Assert.IsFalse(receiver.GetComponent<PlayerPassTarget>().TryGetPrompt(giver, out _));
        }

        // ------------------------------------------------------------------ truck

        [Test]
        public void TruckAcceptsOnlyBoxesTheOrderStillNeeds()
        {
            var hot = f.Box("hot");
            var toxic = f.Box("toxic");
            var truck = MakeTruck(Order(hot, 1));

            Assert.IsTrue(truck.Accepts(hot));
            Assert.IsFalse(truck.Accepts(toxic));

            truck.Deliver(hot);
            Assert.IsFalse(truck.Accepts(hot), "order only asked for one");
        }

        [Test]
        public void DeliveringTakesTheSelectedBoxOutOfTheInventory()
        {
            var actor = Actor();
            var hot = f.Box("hot");
            var normal = f.Box("normal");
            var truck = MakeTruck(Order(hot, 1, normal, 1));
            actor.Inventory.TryAdd(hot, out _);
            actor.Inventory.TryAdd(normal, out _);
            actor.Inventory.Select(1);

            Assert.IsTrue(truck.TryGetPrompt(actor, out var prompt));
            Assert.AreEqual("prompt.deliver", prompt.Key);
            truck.Interact(actor);

            Assert.AreEqual(1, truck.DeliveredCount(normal));
            Assert.AreEqual(0, truck.DeliveredCount(hot));
            Assert.AreEqual(1, actor.Inventory.Count);
            Assert.AreSame(hot, actor.Inventory.Selected.box);
        }

        [Test]
        public void TruckRejectsBoxesNotInTheOrderAndKeepsThemWithThePlayer()
        {
            var actor = Actor();
            var truck = MakeTruck(Order(f.Box("hot"), 1));
            actor.Inventory.TryAdd(f.Box("toxic"), out _);

            Assert.IsTrue(truck.TryGetPrompt(actor, out var prompt));
            Assert.IsFalse(prompt.Enabled);
            Assert.AreEqual("prompt.truck_reject", prompt.Key);

            truck.Interact(actor);
            Assert.AreEqual(1, actor.Inventory.Count);
            Assert.AreEqual(0, truck.TotalDelivered);
        }

        [Test]
        public void CompletingTheOrderRaisesEventsExactlyOnce()
        {
            var hot = f.Box("hot");
            var normal = f.Box("normal");
            var truck = MakeTruck(Order(hot, 1, normal, 2));
            int completed = 0, changed = 0;
            truck.OrderCompleted += () => completed++;
            truck.Changed += () => changed++;

            truck.Deliver(hot);
            truck.Deliver(normal);
            Assert.IsFalse(truck.IsComplete);
            Assert.AreEqual(0, completed);

            truck.Deliver(normal);
            truck.Deliver(normal);

            Assert.IsTrue(truck.IsComplete);
            Assert.AreEqual(1, completed);
            Assert.AreEqual(3, changed);
        }

        [Test]
        public void ACompletedTruckStopsOfferingDeliveries()
        {
            var actor = Actor();
            var hot = f.Box("hot");
            var truck = MakeTruck(Order(hot, 1));
            truck.Deliver(hot);
            actor.Inventory.TryAdd(f.Box("x"), out _);

            Assert.IsFalse(truck.TryGetPrompt(actor, out _));
        }

        [Test]
        public void EmptyHandedPlayersGetAHintAtTheTruck()
        {
            var actor = Actor();
            var truck = MakeTruck(Order(f.Box("hot"), 1));

            Assert.IsTrue(truck.TryGetPrompt(actor, out var prompt));
            Assert.IsFalse(prompt.Enabled);
            Assert.AreEqual("prompt.truck_empty", prompt.Key);
        }
    }
}
