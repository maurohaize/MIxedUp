using NUnit.Framework;

namespace MixedUp.Tests
{
    public class InventoryTests
    {
        TestFactory f;

        [SetUp] public void SetUp() => f = new TestFactory();
        [TearDown] public void TearDown() => f.Cleanup();

        [Test]
        public void HoldsTwoBoxesAndRejectsAThird()
        {
            var inv = f.Player().Inventory;
            Assert.AreEqual(2, inv.Capacity);

            Assert.IsTrue(inv.TryAdd(f.Box("a"), out _));
            Assert.IsTrue(inv.TryAdd(f.Box("b"), out _));
            Assert.IsTrue(inv.IsFull);
            Assert.IsFalse(inv.TryAdd(f.Box("c"), out _));
            Assert.AreEqual(2, inv.Count);
        }

        [Test]
        public void FirstBoxBecomesSelected()
        {
            var inv = f.Player().Inventory;
            var a = f.Box("a");
            inv.TryAdd(a, out int index);
            Assert.AreEqual(0, index);
            Assert.AreSame(a, inv.Selected.box);
        }

        [Test]
        public void RemovingSelectedBoxMovesSelectionToNextOccupiedSlot()
        {
            var inv = f.Player().Inventory;
            inv.TryAdd(f.Box("a"), out _);
            var b = f.Box("b");
            inv.TryAdd(b, out _);

            inv.RemoveAt(0);

            Assert.AreEqual(1, inv.Count);
            Assert.AreSame(b, inv.Selected.box);
        }

        [Test]
        public void CycleSelectionSkipsEmptySlots()
        {
            var inv = f.Player().Inventory;
            var a = f.Box("a");
            var b = f.Box("b");
            inv.TryAdd(a, out _);
            inv.TryAdd(b, out _);

            inv.CycleSelection(1);
            Assert.AreSame(b, inv.Selected.box);
            inv.CycleSelection(1);
            Assert.AreSame(a, inv.Selected.box);

            inv.RemoveAt(0);
            inv.CycleSelection(1);
            Assert.AreSame(b, inv.Selected.box);
        }

        [Test]
        public void TransferMovesTheBoxAndRestartsItsTimer()
        {
            var from = f.Player("from");
            var to = f.Player("to");
            var box = f.Box("a");
            from.Inventory.TryAdd(box, out _);
            from.Inventory.Tick(5f);
            Assert.AreEqual(5f, from.Inventory.Slots[0].heldTime, 0.001f);

            Assert.IsTrue(from.Inventory.TryTransfer(0, to.Inventory));

            Assert.AreEqual(0, from.Inventory.Count);
            Assert.AreEqual(1, to.Inventory.Count);
            Assert.AreEqual(0f, to.Inventory.Slots[0].heldTime, 0.001f);
        }

        [Test]
        public void TransferFailsWhenReceiverIsFullAndKeepsTheBox()
        {
            var from = f.Player("from");
            var to = f.Player("to");
            to.Inventory.TryAdd(f.Box("x"), out _);
            to.Inventory.TryAdd(f.Box("y"), out _);
            var box = f.Box("a");
            from.Inventory.TryAdd(box, out _);

            Assert.IsFalse(from.Inventory.TryTransfer(0, to.Inventory));
            Assert.AreEqual(1, from.Inventory.Count);
            Assert.AreEqual(2, to.Inventory.Count);
        }

        [Test]
        public void HeldTimeOnlyGrowsForOccupiedSlots()
        {
            var inv = f.Player().Inventory;
            inv.TryAdd(f.Box("a"), out _);
            inv.Tick(3f);
            Assert.AreEqual(3f, inv.Slots[0].heldTime, 0.001f);
            Assert.AreEqual(0f, inv.Slots[1].heldTime, 0.001f);
        }
    }
}
