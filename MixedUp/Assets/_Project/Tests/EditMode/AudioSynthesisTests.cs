using System;
using NUnit.Framework;
using UnityEngine;

namespace MixedUp.Tests
{
    public class AudioSynthesisTests
    {
        [Test]
        public void EverySoundIsAudibleShortAndNotClipped()
        {
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                for (int variant = 0; variant < 3; variant++)
                {
                    var clip = ProceduralAudio.Get(id, variant);
                    Assert.IsNotNull(clip, id + " " + variant);
                    Assert.Less(clip.length, 2.5f, id + " is a short effect");
                    Assert.Greater(clip.length, 0.05f, id.ToString());

                    var data = new float[clip.samples];
                    clip.GetData(data, 0);
                    float peak = 0f, energy = 0f;
                    foreach (var s in data)
                    {
                        peak = Mathf.Max(peak, Mathf.Abs(s));
                        energy += s * s;
                    }
                    Assert.Greater(peak, 0.2f, id + " is not silent");
                    Assert.LessOrEqual(peak, 1f, id + " does not clip");
                    Assert.IsFalse(float.IsNaN(energy), id + " has no NaN samples");
                }
            }
        }

        [Test]
        public void TakesOfTheSameSoundDifferSoStepsDoNotRepeatExactly()
        {
            var a = new float[ProceduralAudio.Get(SfxId.FootGrass, 0).samples];
            var b = new float[ProceduralAudio.Get(SfxId.FootGrass, 1).samples];
            ProceduralAudio.Get(SfxId.FootGrass, 0).GetData(a, 0);
            ProceduralAudio.Get(SfxId.FootGrass, 1).GetData(b, 0);
            bool different = a.Length != b.Length;
            for (int i = 0; i < Mathf.Min(a.Length, b.Length) && !different; i++) different = !Mathf.Approximately(a[i], b[i]);
            Assert.IsTrue(different);
        }

        [Test]
        public void BothMusicTracksLoopOverEightBars()
        {
            foreach (MusicId id in Enum.GetValues(typeof(MusicId)))
            {
                var clip = ProceduralAudio.Music(id);
                Assert.Greater(clip.length, 15f, id.ToString());
                Assert.Less(clip.length, 40f, id.ToString());
            }
            Assert.Greater(Mathf.Abs(ProceduralAudio.Music(MusicId.Menu).length - ProceduralAudio.Music(MusicId.Game).length), 0.01f, "different tempo");
        }

        [Test]
        public void AmbienceLoopsExist()
        {
            Assert.Greater(ProceduralAudio.Wind().length, 3f);
            Assert.Greater(ProceduralAudio.River().length, 2f);
            Assert.Greater(ProceduralAudio.Bird(1).length, 0.3f);
        }
    }
}
