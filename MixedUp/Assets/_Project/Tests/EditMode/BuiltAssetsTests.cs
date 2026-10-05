using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MixedUp.Tests
{
    /// <summary>Sanity checks on what MixedUp > Build Phase 1 Prototype produces.</summary>
    public class BuiltAssetsTests
    {
        const string Data = "Assets/_Project/Data/";

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Assert.Ignore("Missing " + path + ". Run MixedUp > Build Phase 1 Prototype first.");
            return asset;
        }

        [Test]
        public void DatabaseHasTheFiveBoxTypesWithTheRightEffects()
        {
            var db = Load<BoxDatabase>(Data + "BoxDatabase.asset");
            Assert.AreEqual(5, db.boxes.Length);

            Assert.IsFalse(db.GetById("normal").effects.Length > 0);
            Assert.IsTrue(db.GetById("hot").HasEffect<HeatEffect>());
            Assert.IsTrue(db.GetById("electric").HasEffect<ElectricEffect>());
            Assert.IsTrue(db.GetById("frozen").HasEffect<FrozenEffect>());
            Assert.IsTrue(db.GetById("toxic").HasEffect<ToxicEffect>());
        }

        [Test]
        public void EveryBoxHasAnIconAndLocalizedText()
        {
            var db = Load<BoxDatabase>(Data + "BoxDatabase.asset");
            foreach (var box in db.boxes)
            {
                Assert.IsNotNull(box.icon, box.id + " icon");
                Assert.IsTrue(Localization.Has(box.nameKey, Language.Basque), box.id + " name key");
                Assert.IsTrue(Localization.Has(box.descriptionKey, Language.Basque), box.id + " description key");
                foreach (var effect in box.effects)
                    Assert.IsTrue(Localization.Has(effect.displayNameKey, Language.Basque), box.id + " effect key");
            }
        }

        [Test]
        public void BoxIdsAreUnique()
        {
            var db = Load<BoxDatabase>(Data + "BoxDatabase.asset");
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var box in db.boxes) Assert.IsTrue(ids.Add(box.id), "duplicate id " + box.id);
        }

        [Test]
        public void PrototypeOrderRequestsEveryBoxType()
        {
            var order = Load<OrderData>(Data + "Order_Prototype.asset");
            var db = Load<BoxDatabase>(Data + "BoxDatabase.asset");

            Assert.AreEqual(6, order.TotalBoxes);
            foreach (var box in db.boxes) Assert.Greater(order.RequiredCount(box), 0, box.id);
        }

        [Test]
        public void PaletteOffersTheNineRequestedClothesColours()
        {
            var palette = Load<PlayerPalette>(Data + "PlayerPalette.asset");
            Assert.AreEqual(9, palette.clothesColors.Length);
            Assert.GreaterOrEqual(palette.skinTones.Length, 1);
        }
    }
}
