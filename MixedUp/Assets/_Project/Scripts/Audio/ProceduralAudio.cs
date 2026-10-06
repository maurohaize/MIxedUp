using System;
using System.Collections.Generic;
using UnityEngine;

namespace MixedUp
{
    /// <summary>
    /// Builds every sound and the music out of maths: noise through filters, sine sweeps and plucked notes. Nothing is
    /// recorded or downloaded, so there is no licence to worry about and the files cannot go missing.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 24000;
        const int Variants = 3;

        static readonly Dictionary<SfxId, AudioClip[]> sfx = new Dictionary<SfxId, AudioClip[]>();
        static readonly Dictionary<MusicId, AudioClip> music = new Dictionary<MusicId, AudioClip>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            sfx.Clear();
            music.Clear();
        }

        /// <summary>One of a few slightly different takes of a sound, so repeated steps do not sound like a machine gun.</summary>
        public static AudioClip Get(SfxId id, int variant)
        {
            if (!sfx.TryGetValue(id, out var clips))
            {
                clips = new AudioClip[Variants];
                sfx[id] = clips;
            }
            int index = Mathf.Abs(variant) % Variants;
            if (clips[index] == null) clips[index] = Build(id, index);
            return clips[index];
        }

        public static AudioClip Music(MusicId id)
        {
            if (!music.TryGetValue(id, out var clip) || clip == null)
            {
                clip = BuildMusic(id);
                music[id] = clip;
            }
            return clip;
        }

        // ------------------------------------------------------------ building blocks

        sealed class Buffer
        {
            public readonly float[] data;
            public Buffer(float seconds) { data = new float[Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate))]; }
            public int Length => data.Length;
        }

        /// <summary>A resonant band-pass filter that can be swept while it runs (kept inside its stable range).</summary>
        struct Filter
        {
            float low, band, lp, hpOut, hpIn;

            public float Band(float input, float frequency, float q)
            {
                float damp = 1f / Mathf.Max(0.1f, q);
                float f = 2f * Mathf.Sin(Mathf.PI * Mathf.Clamp(frequency, 20f, SampleRate * 0.45f) / SampleRate);
                f = Mathf.Min(f, Mathf.Max(0.05f, 1.8f - damp));
                low += f * band;
                float high = input - low - damp * band;
                band += f * high;
                return band;
            }

            public float LowPass(float input, float frequency)
            {
                float a = 1f - Mathf.Exp(-6.2832f * Mathf.Clamp(frequency, 20f, SampleRate * 0.45f) / SampleRate);
                lp += a * (input - lp);
                return lp;
            }

            public float HighPass(float input, float frequency)
            {
                float rc = 1f / (6.2832f * Mathf.Clamp(frequency, 20f, SampleRate * 0.45f));
                float dt = 1f / SampleRate;
                float a = rc / (rc + dt);
                hpOut = a * (hpOut + input - hpIn);
                hpIn = input;
                return hpOut;
            }
        }

        static float Env(float t, float attack, float decay) => (attack <= 0f ? 1f : Mathf.Clamp01(t / attack)) * Mathf.Exp(-t / Mathf.Max(0.0001f, decay));

        /// <summary>Adds filtered noise: `freqStart` sweeps to `freqEnd` over the clip.</summary>
        static void AddNoise(Buffer b, System.Random rng, float start, float length, float freqStart, float freqEnd, float q,
            float gain, float attack, float decay, int mode = 0)
        {
            var filter = new Filter();
            int from = Mathf.RoundToInt(start * SampleRate), count = Mathf.RoundToInt(length * SampleRate);
            for (int i = 0; i < count && from + i < b.Length; i++)
            {
                float t = i / (float)SampleRate, k = count > 1 ? i / (float)(count - 1) : 0f;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                float freq = Mathf.Lerp(freqStart, freqEnd, k);
                float v = mode == 0 ? filter.Band(n, freq, q) : mode == 1 ? filter.LowPass(n, freq) : filter.HighPass(n, freq);
                b.data[from + i] += v * gain * Env(t, attack, decay);
            }
        }

        /// <summary>Adds a tone whose pitch glides from `freqStart` to `freqEnd`. `shape`: 0 sine, 1 triangle, 2 soft square.</summary>
        static void AddTone(Buffer b, float start, float length, float freqStart, float freqEnd, float gain, float attack, float decay,
            int shape = 0, float vibrato = 0f, float vibratoRate = 6f)
        {
            int from = Mathf.RoundToInt(start * SampleRate), count = Mathf.RoundToInt(length * SampleRate);
            float phase = 0f;
            for (int i = 0; i < count && from + i < b.Length; i++)
            {
                float t = i / (float)SampleRate, k = count > 1 ? i / (float)(count - 1) : 0f;
                float freq = Mathf.Lerp(freqStart, freqEnd, k) * (1f + vibrato * Mathf.Sin(t * vibratoRate * 6.2832f));
                phase += freq / SampleRate;
                float p = phase - Mathf.Floor(phase);
                float wave = shape == 0 ? Mathf.Sin(p * 6.2832f)
                           : shape == 1 ? (p < 0.5f ? 4f * p - 1f : 3f - 4f * p)
                           : Mathf.Clamp(Mathf.Sin(p * 6.2832f) * 2.2f, -1f, 1f);
                b.data[from + i] += wave * gain * Env(t, attack, decay);
            }
        }

        /// <summary>A plucked note: a few decaying harmonics, bright at first and mellow after.</summary>
        static void AddPluck(Buffer b, float start, float length, float freq, float gain, float decay = 0.25f)
        {
            int from = Mathf.RoundToInt(start * SampleRate), count = Mathf.RoundToInt(length * SampleRate);
            for (int i = 0; i < count && from + i < b.Length; i++)
            {
                float t = i / (float)SampleRate;
                float v = Mathf.Sin(6.2832f * freq * t) + 0.45f * Mathf.Sin(6.2832f * freq * 2f * t) * Mathf.Exp(-t * 9f)
                          + 0.2f * Mathf.Sin(6.2832f * freq * 3f * t) * Mathf.Exp(-t * 14f);
                b.data[from + i] += v * gain * Env(t, 0.004f, decay);
            }
        }

        static AudioClip ToClip(string name, Buffer b, float peak = 0.9f)
        {
            float max = 0.0001f;
            for (int i = 0; i < b.Length; i++) max = Mathf.Max(max, Mathf.Abs(b.data[i]));
            float scale = peak / max;   // every sound is brought to the same peak, quiet or loud
            int fade = Mathf.Min(b.Length / 4, Mathf.RoundToInt(0.01f * SampleRate));
            for (int i = 0; i < b.Length; i++)
            {
                float v = b.data[i] * scale;
                if (i > b.Length - fade) v *= (b.Length - i) / (float)fade;
                b.data[i] = Mathf.Clamp(v, -1f, 1f);
            }
            var clip = AudioClip.Create(name, b.Length, 1, SampleRate, false);
            clip.SetData(b.data, 0);
            return clip;
        }

        // ------------------------------------------------------------------ effects

        static AudioClip Build(SfxId id, int variant)
        {
            var rng = new System.Random((int)id * 101 + variant * 17 + 3);
            float v = 1f + (variant - 1) * 0.12f;      // pitch / brightness variation
            Buffer b;

            switch (id)
            {
                case SfxId.FootGrass:
                    b = new Buffer(0.14f);
                    AddNoise(b, rng, 0f, 0.12f, 2200f * v, 1400f * v, 0.9f, 1.0f, 0.004f, 0.035f);
                    AddTone(b, 0f, 0.08f, 110f * v, 70f, 0.25f, 0.002f, 0.03f);
                    break;
                case SfxId.FootDirt:
                    b = new Buffer(0.16f);
                    AddNoise(b, rng, 0f, 0.14f, 1000f * v, 600f, 0.8f, 1.1f, 0.004f, 0.045f);
                    AddTone(b, 0f, 0.1f, 95f * v, 60f, 0.4f, 0.002f, 0.04f);
                    break;
                case SfxId.FootWood:
                    b = new Buffer(0.2f);
                    AddTone(b, 0f, 0.15f, 190f * v, 150f, 0.6f, 0.001f, 0.05f, 1);
                    AddTone(b, 0f, 0.1f, 330f * v, 300f, 0.25f, 0.001f, 0.03f);
                    AddNoise(b, rng, 0f, 0.03f, 3000f, 3000f, 0.7f, 0.4f, 0.001f, 0.01f, 2);
                    break;
                case SfxId.FootStone:
                    b = new Buffer(0.14f);
                    AddNoise(b, rng, 0f, 0.05f, 3200f * v, 3200f, 0.7f, 0.7f, 0.001f, 0.015f, 2);
                    AddTone(b, 0f, 0.09f, 140f * v, 100f, 0.45f, 0.001f, 0.03f);
                    break;
                case SfxId.FootSnow:
                    b = new Buffer(0.2f);
                    AddNoise(b, rng, 0f, 0.09f, 1500f * v, 900f, 1.2f, 1.1f, 0.006f, 0.04f, 1);
                    AddNoise(b, rng, 0.05f, 0.1f, 2600f * v, 1800f, 1.5f, 0.6f, 0.004f, 0.03f);
                    break;
                case SfxId.FootWater:
                    b = new Buffer(0.28f);
                    AddNoise(b, rng, 0f, 0.22f, 900f * v, 500f, 1.2f, 0.8f, 0.01f, 0.09f);
                    AddTone(b, 0.02f, 0.06f, 300f * v, 700f, 0.3f, 0.004f, 0.03f);
                    AddTone(b, 0.1f, 0.05f, 400f * v, 800f, 0.2f, 0.004f, 0.025f);
                    break;
                case SfxId.FootMud:
                    b = new Buffer(0.3f);
                    AddNoise(b, rng, 0f, 0.26f, 450f * v, 220f, 1.4f, 1.3f, 0.02f, 0.1f);
                    AddTone(b, 0f, 0.2f, 160f * v, 70f, 0.4f, 0.01f, 0.08f, 0, 0.1f, 14f);
                    break;
                case SfxId.Jump:
                    b = new Buffer(0.2f);
                    AddTone(b, 0f, 0.16f, 260f * v, 520f * v, 0.5f, 0.005f, 0.07f, 1);
                    AddNoise(b, rng, 0f, 0.15f, 700f, 1400f, 0.7f, 0.35f, 0.01f, 0.06f);
                    break;
                case SfxId.Land:
                    b = new Buffer(0.25f);
                    AddTone(b, 0f, 0.2f, 120f * v, 52f, 0.9f, 0.002f, 0.08f);
                    AddNoise(b, rng, 0f, 0.12f, 900f, 400f, 0.7f, 0.6f, 0.002f, 0.04f, 1);
                    break;
                case SfxId.Crouch:
                    b = new Buffer(0.14f);
                    AddNoise(b, rng, 0f, 0.12f, 1800f * v, 900f, 0.8f, 0.5f, 0.01f, 0.05f);
                    break;
                case SfxId.Splash:
                    b = new Buffer(0.7f);
                    AddNoise(b, rng, 0f, 0.55f, 1800f * v, 450f, 0.7f, 1.0f, 0.01f, 0.2f);
                    for (int i = 0; i < 5; i++) AddTone(b, 0.05f + i * 0.07f, 0.06f, (350f + i * 80f) * v, (700f + i * 120f) * v, 0.18f, 0.004f, 0.03f);
                    break;
                case SfxId.Pickup:
                    b = new Buffer(0.4f);
                    AddTone(b, 0f, 0.18f, 660f * v, 660f * v, 0.5f, 0.003f, 0.09f, 1);
                    AddTone(b, 0.07f, 0.26f, 990f * v, 990f * v, 0.5f, 0.003f, 0.12f, 1);
                    AddTone(b, 0.07f, 0.2f, 1980f * v, 1980f * v, 0.12f, 0.003f, 0.07f);
                    break;
                case SfxId.Pass:
                    b = new Buffer(0.3f);
                    AddNoise(b, rng, 0f, 0.2f, 500f, 1500f * v, 0.8f, 0.5f, 0.02f, 0.08f);
                    AddTone(b, 0.12f, 0.15f, 520f * v, 780f * v, 0.4f, 0.003f, 0.06f, 1);
                    break;
                case SfxId.Deliver:
                    b = new Buffer(0.9f);
                    AddPluck(b, 0f, 0.5f, 523.25f * v, 0.5f);
                    AddPluck(b, 0.11f, 0.5f, 659.25f * v, 0.5f);
                    AddPluck(b, 0.22f, 0.7f, 783.99f * v, 0.55f, 0.4f);
                    AddNoise(b, rng, 0.02f, 0.1f, 3500f, 3500f, 0.7f, 0.15f, 0.001f, 0.03f, 2);
                    break;
                case SfxId.Hurt:
                    b = new Buffer(0.3f);
                    AddTone(b, 0f, 0.25f, 330f * v, 150f, 0.6f, 0.004f, 0.1f, 2, 0.04f, 18f);
                    AddNoise(b, rng, 0f, 0.1f, 1200f, 600f, 0.8f, 0.5f, 0.002f, 0.04f);
                    break;
                case SfxId.Death:
                    b = new Buffer(1.0f);
                    AddTone(b, 0f, 0.9f, 520f * v, 90f, 0.6f, 0.01f, 0.45f, 2, 0.05f, 9f);
                    AddNoise(b, rng, 0.1f, 0.6f, 900f, 200f, 0.7f, 0.3f, 0.02f, 0.3f, 1);
                    break;
                case SfxId.Zap:
                    b = new Buffer(0.45f);
                    AddNoise(b, rng, 0f, 0.4f, 4200f * v, 1800f, 0.6f, 0.9f, 0.002f, 0.18f, 2);
                    AddTone(b, 0f, 0.4f, 55f, 55f, 0.5f, 0.002f, 0.2f, 2);
                    AddTone(b, 0f, 0.25f, 1800f, 300f, 0.35f, 0.002f, 0.1f, 2);
                    break;
                case SfxId.Bounce:
                    b = new Buffer(0.5f);
                    AddTone(b, 0f, 0.45f, 180f * v, 520f * v, 0.7f, 0.004f, 0.22f, 0, 0.08f, 11f);
                    AddTone(b, 0f, 0.3f, 360f * v, 1040f * v, 0.2f, 0.004f, 0.12f, 0, 0.08f, 11f);
                    break;
                case SfxId.Whoosh:
                    b = new Buffer(0.45f);
                    AddNoise(b, rng, 0f, 0.4f, 400f, 1700f * v, 0.7f, 1f, 0.12f, 0.12f);
                    break;
                case SfxId.Gust:
                    b = new Buffer(1.2f);
                    AddNoise(b, rng, 0f, 1.1f, 500f * v, 1100f, 0.6f, 1f, 0.5f, 0.5f);
                    break;
                case SfxId.UiClick:
                    b = new Buffer(0.12f);
                    AddTone(b, 0f, 0.08f, 420f * v, 300f, 0.6f, 0.001f, 0.03f, 1);
                    AddNoise(b, rng, 0f, 0.02f, 2500f, 2500f, 0.8f, 0.3f, 0.001f, 0.008f, 2);
                    break;
                case SfxId.UiHover:
                    b = new Buffer(0.08f);
                    AddTone(b, 0f, 0.05f, 880f * v, 820f, 0.3f, 0.001f, 0.02f);
                    break;
                case SfxId.Heal:
                    b = new Buffer(0.9f);
                    for (int i = 0; i < 4; i++) AddPluck(b, i * 0.1f, 0.5f, 659.25f * Mathf.Pow(1.25f, i) * v * 0.5f, 0.35f, 0.35f);
                    break;
                case SfxId.Hug:
                    b = new Buffer(0.8f);
                    AddTone(b, 0f, 0.7f, 392f * v, 392f * v, 0.35f, 0.08f, 0.35f);
                    AddTone(b, 0f, 0.7f, 494f * v, 494f * v, 0.3f, 0.08f, 0.35f);
                    AddTone(b, 0f, 0.7f, 587f * v, 587f * v, 0.3f, 0.08f, 0.35f);
                    break;
                case SfxId.Push:
                    b = new Buffer(0.3f);
                    AddNoise(b, rng, 0f, 0.2f, 600f, 1800f * v, 0.7f, 0.6f, 0.01f, 0.07f);
                    AddTone(b, 0.05f, 0.18f, 110f * v, 60f, 0.7f, 0.002f, 0.07f);
                    break;
                case SfxId.Squeak:
                    b = new Buffer(0.4f);
                    AddTone(b, 0f, 0.12f, 700f * v, 1250f * v, 0.5f, 0.004f, 0.06f, 2, 0.03f, 25f);
                    AddTone(b, 0.15f, 0.14f, 650f * v, 1150f * v, 0.5f, 0.004f, 0.07f, 2, 0.03f, 25f);
                    break;
                case SfxId.IceCrack:
                    b = new Buffer(0.45f);
                    AddNoise(b, rng, 0f, 0.05f, 4500f, 4500f, 0.7f, 1f, 0.001f, 0.012f, 2);
                    AddNoise(b, rng, 0.07f, 0.04f, 3800f * v, 3800f, 0.7f, 0.7f, 0.001f, 0.01f, 2);
                    AddTone(b, 0.01f, 0.4f, 2200f * v, 2100f, 0.25f, 0.002f, 0.18f);
                    AddTone(b, 0.01f, 0.4f, 3300f * v, 3250f, 0.12f, 0.002f, 0.15f);
                    break;
                case SfxId.Collapse:
                    b = new Buffer(0.7f);
                    AddNoise(b, rng, 0f, 0.6f, 1100f * v, 350f, 0.8f, 1f, 0.01f, 0.25f, 1);
                    AddTone(b, 0f, 0.3f, 130f, 60f, 0.5f, 0.003f, 0.12f);
                    break;
                case SfxId.Bell:
                    b = new Buffer(1.2f);
                    AddTone(b, 0f, 1.1f, 880f * v, 880f * v, 0.5f, 0.002f, 0.5f);
                    AddTone(b, 0f, 1.1f, 1320f * v, 1320f * v, 0.2f, 0.002f, 0.35f);
                    AddTone(b, 0f, 0.8f, 2210f * v, 2210f * v, 0.1f, 0.002f, 0.2f);
                    break;
                case SfxId.Win:
                    b = new Buffer(1.6f);
                    foreach (var (t, f) in new[] { (0f, 523.25f), (0.14f, 659.25f), (0.28f, 783.99f), (0.42f, 1046.5f) }) AddPluck(b, t, 0.9f, f * v, 0.5f, 0.45f);
                    AddTone(b, 0.45f, 1.0f, 523.25f, 523.25f, 0.2f, 0.05f, 0.5f);
                    AddTone(b, 0.45f, 1.0f, 659.25f, 659.25f, 0.2f, 0.05f, 0.5f);
                    AddTone(b, 0.45f, 1.0f, 783.99f, 783.99f, 0.2f, 0.05f, 0.5f);
                    break;
                case SfxId.Lose:
                    b = new Buffer(1.4f);
                    foreach (var (t, f) in new[] { (0f, 392f), (0.25f, 349.23f), (0.5f, 329.63f), (0.8f, 261.63f) }) AddPluck(b, t, 0.8f, f * v, 0.5f, 0.4f);
                    break;
                default:
                    b = new Buffer(0.1f);
                    break;
            }
            return ToClip(id + "_" + variant, b);
        }

        // -------------------------------------------------------------------- music

        static float Note(int semitonesFromA3) => 220f * Mathf.Pow(2f, semitonesFromA3 / 12f);

        static AudioClip BuildMusic(MusicId id)
        {
            bool menu = id == MusicId.Menu;
            float bpm = menu ? 84f : 112f;
            float beat = 60f / bpm;
            const int bars = 8;
            var b = new Buffer(bars * 4 * beat);
            var rng = new System.Random(menu ? 11 : 29);

            // A minor, F, C, G (roots in semitones from A3), twice round.
            int[] roots = { 0, -4, 3, -2 };
            int[][] triads = { new[] { 0, 3, 7 }, new[] { 0, 4, 7 }, new[] { 0, 4, 7 }, new[] { 0, 4, 7 } };
            int[] pentatonic = { 0, 3, 5, 7, 10, 12, 15, 17, 19 };

            for (int bar = 0; bar < bars; bar++)
            {
                int chord = bar % 4;
                float t0 = bar * 4 * beat;

                // soft pad
                foreach (int interval in triads[chord])
                    AddTone(b, t0, 4 * beat * 1.02f, Note(roots[chord] + interval + 12), Note(roots[chord] + interval + 12), 0.05f, 0.35f, 4f);

                // bass
                if (menu)
                {
                    AddTone(b, t0, 2 * beat, Note(roots[chord] - 12), Note(roots[chord] - 12), 0.16f, 0.02f, beat * 1.2f, 1);
                    AddTone(b, t0 + 2 * beat, 2 * beat, Note(roots[chord] - 5), Note(roots[chord] - 5), 0.1f, 0.02f, beat * 1.2f, 1);
                }
                else
                {
                    for (int step = 0; step < 8; step++)
                    {
                        int note = roots[chord] - 12 + (step % 4 == 2 ? 7 : 0);
                        AddTone(b, t0 + step * beat * 0.5f, beat * 0.45f, Note(note), Note(note), 0.14f, 0.005f, beat * 0.25f, 1);
                    }
                    for (int k = 0; k < 4; k += 2) AddTone(b, t0 + k * beat, 0.22f, 130f, 48f, 0.35f, 0.002f, 0.08f);       // kick
                    for (int k = 0; k < 8; k++) AddNoise(b, rng, t0 + k * beat * 0.5f + (k % 2 == 1 ? 0f : 0.003f), 0.04f, 7000f, 7000f, 0.7f, k % 2 == 1 ? 0.07f : 0.03f, 0.001f, 0.012f, 2);
                }

                // melody: plucks on a pentatonic scale, a little different each bar but the same every loop
                for (int step = 0; step < 8; step++)
                {
                    if (rng.NextDouble() > (menu ? 0.38 : 0.55)) continue;
                    int degree = pentatonic[rng.Next(pentatonic.Length)];
                    AddPluck(b, t0 + step * beat * 0.5f, beat * 1.4f, Note(roots[chord] + degree), menu ? 0.13f : 0.11f, menu ? 0.5f : 0.3f);
                }
            }
            return ToClip(menu ? "MenuMusic" : "GameMusic", b, 0.55f);
        }

        /// <summary>A gentle wind, used as the ambience bed. 6 s of looping noise.</summary>
        public static AudioClip Wind()
        {
            var rng = new System.Random(5);
            var b = new Buffer(6f);
            var filter = new Filter();
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SampleRate;
                float sweep = 420f + 260f * Mathf.Sin(t * 1.05f) + 130f * Mathf.Sin(t * 2.7f);
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                float low = filter.LowPass(n, sweep);
                b.data[i] = low * 2.5f * (0.6f + 0.4f * Mathf.Sin(t * 0.9f));
            }
            return ToClip("Wind", b, 0.5f);
        }

        /// <summary>The river: a steady babble. 4 s of looping noise.</summary>
        public static AudioClip River()
        {
            var rng = new System.Random(8);
            var b = new Buffer(4f);
            var filter = new Filter();
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SampleRate;
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                float band = filter.Band(n, 900f + 400f * Mathf.Sin(t * 5.1f) + 250f * Mathf.Sin(t * 11.7f), 1.6f);
                b.data[i] = band * (0.7f + 0.3f * Mathf.Sin(t * 3.3f));
            }
            return ToClip("River", b, 0.5f);
        }

        /// <summary>A little two-note bird call.</summary>
        public static AudioClip Bird(int variant)
        {
            var b = new Buffer(0.5f);
            float f = 2400f + variant * 300f;
            AddTone(b, 0f, 0.12f, f, f * 1.4f, 0.5f, 0.01f, 0.06f, 0, 0.02f, 40f);
            AddTone(b, 0.16f, 0.16f, f * 1.3f, f * 0.9f, 0.5f, 0.01f, 0.08f, 0, 0.02f, 40f);
            if (variant % 2 == 0) AddTone(b, 0.34f, 0.1f, f * 1.2f, f * 1.5f, 0.4f, 0.01f, 0.05f, 0, 0.02f, 40f);
            return ToClip("Bird" + variant, b, 0.5f);
        }
    }
}
