using NUnit.Framework;

namespace MixedUp.Tests
{
    /// <summary>What the lobby accepts as a way into a room, the map list and the avatar pose that travels the network.</summary>
    public class OnlineTests
    {
        [Test]
        public void AnAddressWithADotGoesDirectWithTheDefaultPort()
        {
            Assert.IsTrue(OnlineSession.TryParse("192.168.1.5", out var kind, out var address, out var port));
            Assert.AreEqual(ConnectionKind.Direct, kind);
            Assert.AreEqual("192.168.1.5", address);
            Assert.AreEqual(OnlineSession.DefaultPort, port);
        }

        [Test]
        public void AnAddressCanCarryItsPort()
        {
            Assert.IsTrue(OnlineSession.TryParse(" 10.0.0.2:9000 ", out var kind, out var address, out var port));
            Assert.AreEqual(ConnectionKind.Direct, kind);
            Assert.AreEqual("10.0.0.2", address);
            Assert.AreEqual(9000, port);
        }

        [Test]
        public void SixLettersAndDigitsIsARelayCodeInUpperCase()
        {
            Assert.IsTrue(OnlineSession.TryParse("ab12cd", out var kind, out var address, out _));
            Assert.AreEqual(ConnectionKind.Relay, kind);
            Assert.AreEqual("AB12CD", address);
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("ab12cd7")]
        [TestCase("1.2.3.4:abc")]
        [TestCase("ab 2cd")]
        public void NonsenseIsRejected(string text)
        {
            Assert.IsFalse(OnlineSession.TryParse(text, out _, out _, out _));
        }

        [Test]
        public void TheMapListWrapsAndEveryMapHasAScene()
        {
            Assert.GreaterOrEqual(LevelCatalog.All.Length, 1);
            foreach (var level in LevelCatalog.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(level.scene), level.id);
                Assert.AreSame(level, LevelCatalog.Find(level.id));
            }
            var first = LevelCatalog.All[0];
            Assert.AreSame(first, LevelCatalog.Step(LevelCatalog.All[LevelCatalog.All.Length - 1], 1));
            Assert.AreSame(LevelCatalog.All[LevelCatalog.All.Length - 1], LevelCatalog.Step(first, -1));
        }

        [Test]
        public void PoseFlagsAndEquality()
        {
            var a = new AvatarPose { position = new UnityEngine.Vector3(1f, 2f, 3f), yaw = 90f, speed = 4f, flags = AvatarPose.Grounded | AvatarPose.Crouching };
            var b = a;
            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a.Has(AvatarPose.Grounded));
            Assert.IsFalse(a.Has(AvatarPose.Dead));
            b.flags |= AvatarPose.Dead;
            Assert.IsFalse(a.Equals(b));
        }
    }
}
