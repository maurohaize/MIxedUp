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
        public void CombinationTableFollowsTheDesignSpec()
        {
            var rules = Load<CombinationRules>(Data + "CombinationRules.asset");
            var db = Load<BoxDatabase>(Data + "BoxDatabase.asset");
            BoxData Box(string id) => db.GetById(id);

            Assert.AreEqual(CombinationOutcome.Explosion, rules.OutcomeOf(Box("hot"), Box("electric")), "HOT + ELECTRIC");
            Assert.AreEqual(CombinationOutcome.Safe, rules.OutcomeOf(Box("hot"), Box("frozen")), "HOT + FROZEN");
            Assert.AreEqual(CombinationOutcome.Safe, rules.OutcomeOf(Box("hot"), Box("normal")), "HOT + NORMAL");
            Assert.AreEqual(CombinationOutcome.GameOver, rules.OutcomeOf(Box("toxic"), Box("electric")), "TOXIC + ELECTRIC");
            Assert.AreEqual(CombinationOutcome.Safe, rules.OutcomeOf(Box("frozen"), Box("electric")), "FROZEN + ELECTRIC");
        }

        [Test]
        public void EveryBoxPairHasExactlyOneRuleAndAllFourOutcomesExist()
        {
            var rules = Load<CombinationRules>(Data + "CombinationRules.asset");
            var db = Load<BoxDatabase>(Data + "BoxDatabase.asset");

            var keys = new System.Collections.Generic.HashSet<string>();
            foreach (var rule in rules.rules) Assert.IsTrue(keys.Add(rule.Key), "duplicate rule " + rule.Key);
            Assert.AreEqual(db.boxes.Length * (db.boxes.Length + 1) / 2, keys.Count, "one rule per unordered pair");

            var outcomes = new System.Collections.Generic.HashSet<CombinationOutcome>();
            foreach (var rule in rules.rules) outcomes.Add(rule.outcome);
            Assert.AreEqual(4, outcomes.Count, "safe, danger, explosion and game over must all be reachable");
        }

        [Test]
        public void EveryNonTrivialRuleHasATranslatedHint()
        {
            var rules = Load<CombinationRules>(Data + "CombinationRules.asset");
            foreach (var rule in rules.rules)
            {
                if (rule.IsTrivial) continue;
                foreach (var language in new[] { Language.Basque, Language.Spanish, Language.English })
                    Assert.IsTrue(Localization.Has(rule.hintKey, language), rule.Key + " hint " + language);
            }
        }

        [Test]
        public void ThePrototypeOrderIsSolvableButNotAlreadySolved()
        {
            var rules = Load<CombinationRules>(Data + "CombinationRules.asset");
            var order = Load<OrderData>(Data + "Order_Prototype.asset");

            var boxes = new System.Collections.Generic.List<BoxData>();
            foreach (var line in order.lines)
                for (int i = 0; i < line.count; i++) boxes.Add(line.box);

            Assert.IsFalse(IsAllSafe(rules, boxes), "the delivery order must need rearranging, or there is no puzzle");
            Assert.IsTrue(AnyPermutationIsSafe(rules, boxes, 0), "there must be at least one safe arrangement");
        }

        static bool IsAllSafe(CombinationRules rules, System.Collections.Generic.List<BoxData> boxes)
        {
            for (int i = 0; i + 1 < boxes.Count; i++)
                if (rules.OutcomeOf(boxes[i], boxes[i + 1]) != CombinationOutcome.Safe) return false;
            return true;
        }

        static bool AnyPermutationIsSafe(CombinationRules rules, System.Collections.Generic.List<BoxData> boxes, int from)
        {
            if (from == boxes.Count - 1) return IsAllSafe(rules, boxes);
            for (int i = from; i < boxes.Count; i++)
            {
                (boxes[from], boxes[i]) = (boxes[i], boxes[from]);
                bool found = AnyPermutationIsSafe(rules, boxes, from + 1);
                (boxes[from], boxes[i]) = (boxes[i], boxes[from]);
                if (found) return true;
            }
            return false;
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
