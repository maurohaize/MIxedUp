using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace MixedUp.Tests
{
    public class LocalizationTests
    {
        const string CsvPath = "Assets/_Project/Resources/Localization/strings.csv";
        const string ScriptsPath = "Assets/_Project/Scripts";

        const string LanguagePref = "settings.language";
        bool hadPref;
        int oldPref;

        [SetUp]
        public void SetUp()
        {
            hadPref = PlayerPrefs.HasKey(LanguagePref);
            oldPref = PlayerPrefs.GetInt(LanguagePref, 0);
            Localization.LoadFromText(File.ReadAllText(CsvPath));
        }

        [TearDown]
        public void TearDown()
        {
            if (hadPref) PlayerPrefs.SetInt(LanguagePref, oldPref);
            else PlayerPrefs.DeleteKey(LanguagePref);
            Localization.LoadFromText(File.ReadAllText(CsvPath));
        }

        [Test]
        public void ParsesQuotedCommasEscapedQuotesAndFallsBackToEnglish()
        {
            Localization.LoadFromText("key,eu,es,en\nhello,Kaixo,\"Hola, mundo\",\"Say \"\"hi\"\"\"\nonly_en,,,Only English\n# comment,x,y,z\n");

            Localization.SetLanguage(Language.Spanish);
            Assert.AreEqual("Hola, mundo", Localization.Get("hello"));
            Localization.SetLanguage(Language.English);
            Assert.AreEqual("Say \"hi\"", Localization.Get("hello"));
            Localization.SetLanguage(Language.Basque);
            Assert.AreEqual("Kaixo", Localization.Get("hello"));
            Assert.AreEqual("Only English", Localization.Get("only_en"), "falls back to English");
            Assert.AreEqual("[# comment]", Localization.Get("# comment"), "comment rows are skipped");
            Assert.AreEqual("[missing]", Localization.Get("missing"));
        }

        [Test]
        public void FormatsArguments()
        {
            Localization.SetLanguage(Language.English);
            Assert.AreEqual("PICK UP HOT", Localization.Get("prompt.pickup", "HOT"));
        }

        [Test]
        public void ChangingLanguageNotifiesListenersAndSwitchesText()
        {
            Localization.SetLanguage(Language.English);
            int calls = 0;
            System.Action handler = () => calls++;
            Localization.LanguageChanged += handler;
            try
            {
                Localization.SetLanguage(Language.Basque);
                Assert.AreEqual(1, calls);
                Assert.AreEqual("IRTEN", Localization.Get("ui.quit"));

                Localization.SetLanguage(Language.Spanish);
                Assert.AreEqual("SALIR", Localization.Get("ui.quit"));

                Localization.SetLanguage(Language.Spanish);
                Assert.AreEqual(2, calls, "setting the same language is a no-op");
            }
            finally
            {
                Localization.LanguageChanged -= handler;
            }
        }

        [Test]
        public void EveryKeyIsTranslatedIntoAllThreeLanguages()
        {
            var missing = Localization.Keys
                .SelectMany(k => new[] { Language.Basque, Language.Spanish, Language.English }
                    .Where(l => !Localization.Has(k, l)).Select(l => k + " [" + l + "]"))
                .ToList();

            Assert.IsEmpty(missing, "Untranslated: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryKeyUsedInCodeExistsInTheTable()
        {
            var pattern = new Regex("\"((?:ui|prompt|box|effect|death|toast)\\.[a-z_.]+)\"");
            var keysInCode = Directory.GetFiles(ScriptsPath, "*.cs", SearchOption.AllDirectories)
                .SelectMany(file => pattern.Matches(File.ReadAllText(file)).Cast<Match>().Select(m => m.Groups[1].Value))
                .Distinct()
                .ToList();

            Assert.IsNotEmpty(keysInCode);
            var missing = keysInCode.Where(k => !Localization.Has(k, Language.English)).ToList();
            Assert.IsEmpty(missing, "Keys used in code but missing from strings.csv: " + string.Join(", ", missing));
        }

        [Test]
        public void BoxAndEffectKeysFollowTheNamingConvention()
        {
            foreach (var id in new[] { "normal", "hot", "electric", "frozen", "toxic" })
            {
                Assert.IsTrue(Localization.Has("box." + id + ".name", Language.Basque), id + " name");
                Assert.IsTrue(Localization.Has("box." + id + ".desc", Language.Basque), id + " description");
            }
        }
    }
}
