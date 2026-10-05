using NUnit.Framework;
using UnityEngine;

namespace MixedUp.Tests
{
    public class EffectTests
    {
        TestFactory f;
        HeatEffect heat;
        ElectricEffect electric;
        FrozenEffect frozen;
        ToxicEffect toxic;

        [SetUp]
        public void SetUp()
        {
            f = new TestFactory();
            heat = f.Make<HeatEffect>();
            electric = f.Make<ElectricEffect>();
            frozen = f.Make<FrozenEffect>();
            toxic = f.Make<ToxicEffect>();
        }

        [TearDown] public void TearDown() => f.Cleanup();

        PlayerStatus PlayerWith(params BoxData[] boxes) => PlayerWithHealth(100f, boxes);

        PlayerStatus PlayerWithHealth(float maxHealth, params BoxData[] boxes)
        {
            var p = f.Player("Player", maxHealth);
            foreach (var box in boxes) p.Inventory.TryAdd(box, out _);
            return p;
        }

        [Test]
        public void NormalBoxHasNoEffect()
        {
            var p = PlayerWith(f.Box("normal"));
            for (int i = 0; i < 20; i++) p.Tick(1f);

            Assert.AreEqual(p.maxHealth, p.Health, 0.001f);
            Assert.AreEqual(1f, p.Modifiers.AccelerationMul, 0.001f);
            Assert.AreEqual(0f, p.VisionObstruction, 0.001f);
        }

        [Test]
        public void HeatHurtsMoreTheLongerTheBoxIsCarried()
        {
            var p = PlayerWithHealth(100000f, f.Box("hot", heat));
            float start = p.Health;

            p.Tick(0.1f);
            float early = start - p.Health;

            for (int i = 0; i < 40; i++) p.Tick(1f);
            float before = p.Health;
            p.Tick(0.1f);
            float late = before - p.Health;

            Assert.Greater(early, 0f);
            Assert.Greater(late, early * 3f);
        }

        [Test]
        public void HeatEventuallyKillsAndReportsItsCause()
        {
            var p = PlayerWith(f.Box("hot", heat));
            DeathCause? cause = null;
            p.Died += c => cause = c;

            for (int i = 0; i < 200 && !p.IsDead; i++) p.Tick(0.5f);

            Assert.IsTrue(p.IsDead);
            Assert.AreEqual("death.heat", cause.Value.Key);
        }

        [Test]
        public void PassingAHotBoxRestartsItsRamp()
        {
            var a = PlayerWithHealth(100000f, f.Box("hot", heat));
            var b = f.Player("b", 100000f);

            for (int i = 0; i < 30; i++) a.Tick(1f);
            float beforeA = a.Health;
            a.Tick(0.1f);
            float rateBeforePass = (beforeA - a.Health) / 0.1f;

            a.Inventory.TryTransfer(0, b.Inventory);
            float beforeB = b.Health;
            b.Tick(0.1f);
            float rateAfterPass = (beforeB - b.Health) / 0.1f;

            Assert.Greater(rateBeforePass, 8f);
            Assert.AreEqual(heat.startDamagePerSecond, rateAfterPass, 0.3f);
        }

        [Test]
        public void ElectricBoxIsHarmlessOutsideWater()
        {
            var p = PlayerWith(f.Box("electric", electric));
            for (int i = 0; i < 20; i++) p.Tick(0.5f);
            Assert.AreEqual(p.maxHealth, p.Health, 0.001f);
        }

        [Test]
        public void ElectricBoxShocksInWaterThenKillsOnTheSecondShock()
        {
            var p = PlayerWith(f.Box("electric", electric));
            DeathCause? cause = null;
            p.Died += c => cause = c;
            int shocks = 0;
            p.Shocked += () => shocks++;
            p.Hazards = new HazardState { InWater = true };

            p.Tick(0.1f);
            Assert.AreEqual(p.maxHealth - electric.shockDamage, p.Health, 0.001f);
            Assert.AreEqual(1, shocks);

            p.Tick(0.5f);
            Assert.AreEqual(1, shocks, "second shock must wait for the interval");
            Assert.IsFalse(p.IsDead);

            p.Tick(0.3f);
            Assert.IsTrue(p.IsDead);
            Assert.AreEqual("death.electric_water", cause.Value.Key);
        }

        [Test]
        public void LeavingTheWaterStopsTheShocks()
        {
            var p = PlayerWith(f.Box("electric", electric));
            p.Hazards = new HazardState { InWater = true };
            p.Tick(0.1f);
            float afterFirst = p.Health;

            p.Hazards = new HazardState { InWater = false };
            for (int i = 0; i < 10; i++) p.Tick(0.5f);

            Assert.GreaterOrEqual(p.Health, afterFirst);
            Assert.IsFalse(p.IsDead);
        }

        [Test]
        public void FrozenBoxReducesControlAndEnablesSlopeSliding()
        {
            var p = PlayerWith(f.Box("frozen", frozen));
            p.Tick(0.1f);

            Assert.Less(p.Modifiers.AccelerationMul, 0.2f);
            Assert.Less(p.Modifiers.BrakingMul, 0.2f);
            Assert.Greater(p.Modifiers.SlopeSlide, 0f);
        }

        [Test]
        public void FrozenIsWorseOnWetOrSlipperyGround()
        {
            var dry = PlayerWith(f.Box("frozen", frozen));
            dry.Tick(0.1f);

            var wet = PlayerWith(f.Box("frozen2", frozen));
            wet.Hazards = new HazardState { OnSlippery = true };
            wet.Tick(0.1f);

            Assert.Less(wet.Modifiers.AccelerationMul, dry.Modifiers.AccelerationMul);
        }

        [Test]
        public void ControlReturnsOnceTheFrozenBoxIsGone()
        {
            var a = PlayerWith(f.Box("frozen", frozen));
            var b = f.Player("b");
            a.Tick(0.1f);
            Assert.Less(a.Modifiers.AccelerationMul, 1f);

            a.Inventory.TryTransfer(0, b.Inventory);
            a.Tick(0.1f);
            b.Tick(0.1f);

            Assert.AreEqual(1f, a.Modifiers.AccelerationMul, 0.001f);
            Assert.Less(b.Modifiers.AccelerationMul, 1f);
        }

        [Test]
        public void ToxicFogGrowsWhileCarryingAndFadesAfterwards()
        {
            var a = PlayerWith(f.Box("toxic", toxic));
            var b = f.Player("b");

            for (int i = 0; i < 12; i++) a.Tick(1f);
            float thick = a.VisionObstruction;
            Assert.Greater(thick, 0.2f);

            a.Inventory.TryTransfer(0, b.Inventory);
            for (int i = 0; i < 3; i++) a.Tick(1f);
            for (int i = 0; i < 5; i++) b.Tick(1f);

            Assert.Less(a.VisionObstruction, thick, "the previous carrier recovers");
            Assert.Greater(b.VisionObstruction, 0f, "the new carrier starts getting foggy");
            Assert.Less(b.VisionObstruction, thick, "but starts from zero, not from where the old carrier was");
        }

        [Test]
        public void EffectsFromBothBoxesStack()
        {
            var p = PlayerWith(f.Box("hot", heat), f.Box("frozen", frozen));
            p.Tick(1f);

            Assert.Less(p.Health, p.maxHealth);
            Assert.Less(p.Modifiers.AccelerationMul, 0.2f);
        }

        [Test]
        public void HealthRegeneratesOnlyAfterTheDelay()
        {
            var p = f.Player();
            p.Damage(30f, DeathCause.Fall);

            p.Tick(3f);
            Assert.AreEqual(70f, p.Health, 0.001f);

            p.Tick(2f);
            Assert.Greater(p.Health, 70f);
        }

        [Test]
        public void DeadPlayersNoLongerTick()
        {
            var p = PlayerWith(f.Box("hot", heat));
            p.Kill(DeathCause.Void);
            float heldBefore = p.Inventory.Slots[0].heldTime;

            p.Tick(5f);

            Assert.AreEqual(heldBefore, p.Inventory.Slots[0].heldTime, 0.001f);
            Assert.AreEqual(0f, p.Health);
        }

        [Test]
        public void DamageIgnoresNegativeAmountsAndDeadPlayers()
        {
            var p = f.Player();
            p.Damage(-10f, DeathCause.Fall);
            Assert.AreEqual(p.maxHealth, p.Health, 0.001f);

            int deaths = 0;
            p.Died += _ => deaths++;
            p.Kill(DeathCause.Void);
            p.Kill(DeathCause.Fall);
            p.Damage(10f, DeathCause.Fall);
            Assert.AreEqual(1, deaths);
        }
    }
}
