using System.Collections.Generic;
using UnityEngine;

namespace RollerCoasterHeroes
{
    // Original synthesized score and one-shot cues; no downloaded or licensed recordings.
    public sealed class RideAudio
    {
        const int Hz = 22050;
        readonly AudioSource music, effects, wheels;
        readonly List<AudioClip> clips = new List<AudioClip>();
        readonly AudioClip coin, boost, brake, finish;
        public bool Muted => music.mute;
        public RideAudio(GameObject root)
        {
            music = root.AddComponent<AudioSource>(); music.loop = true; music.volume = .22f;
            music.clip = MakeMusic(); music.Play();
            effects = root.AddComponent<AudioSource>(); effects.volume = .42f;
            wheels = root.AddComponent<AudioSource>(); wheels.loop = true; wheels.volume = .025f;
            wheels.clip = Clip("Soft wheel rattle", 1, t => Mathf.Sin(t * Mathf.PI * 2 * 65) * Mathf.Pow(1 - t * 12 % 1, 6) * .25f);
            wheels.Play();
            coin = Cue("Coin sparkle", new[] { 84, 91, 96 }, .075f);
            boost = Clip("Turbo whoosh", .7f, t => Mathf.Sin(2 * Mathf.PI * (180 * t + 800 * t * t)) * Mathf.Sin(t / .7f * Mathf.PI) * .35f);
            brake = Clip("Comic brake", .65f, t => Mathf.Sin(2 * Mathf.PI * (650 * t - 350 * t * t) + Mathf.Sin(t * 95) * 2) * (1 - t / .65f) * .32f);
            finish = Cue("Finish fanfare", new[] { 72, 76, 79, 84, 79, 84 }, .16f);
        }
        public void ToggleMusic() { music.mute = !music.mute; }
        public void Play(PickupKind kind) { effects.PlayOneShot(kind == PickupKind.Coin ? coin : kind == PickupKind.Boost ? boost : brake); }
        public void Finish() { effects.PlayOneShot(finish); }
        public void Reset() { effects.Stop(); music.time = 0; }
        public void Tick(float speed, bool finished) { wheels.volume = finished ? 0 : .018f + speed * .0005f; wheels.pitch = .7f + speed / 35; }

        AudioClip Clip(string name, float duration, System.Func<float, float> sample)
        {
            var data = new float[Mathf.RoundToInt(Hz * duration)];
            for (int i = 0; i < data.Length; i++)
            {
                float edge = Mathf.Min(1, Mathf.Min(i, data.Length - 1 - i) / 100f);
                data[i] = Mathf.Clamp(sample(i / (float)Hz) * edge, -.9f, .9f);
            }
            var clip = AudioClip.Create(name, data.Length, 1, Hz, false); clip.SetData(data, 0); clips.Add(clip); return clip;
        }
        static float Tone(int midi, float t)
        {
            float phase = 2 * Mathf.PI * 440 * Mathf.Pow(2, (midi - 69) / 12f) * t;
            return Mathf.Sin(phase) + .18f * Mathf.Sin(phase * 2);
        }
        AudioClip Cue(string title, int[] notes, float step)
        {
            return Clip(title, notes.Length * step, t => {
                int i = Mathf.Min(notes.Length - 1, (int)(t / step)); float local = t % step;
                return Tone(notes[i], local) * Mathf.Exp(-local * 16) * .4f;
            });
        }
        AudioClip MakeMusic()
        {
            const float beat = 60f / 132;
            int[] melody = { 72, 76, 79, 76, 81, 79, 76, 74, 72, 76, 79, 84, 83, 79, 76, 74,
                69, 72, 76, 79, 77, 76, 74, 72, 71, 74, 79, 83, 81, 79, 74, 71 };
            int[] bass = { 48, 45, 53, 55 };
            return Clip("Green Hills - original 132 BPM arcade loop", beat * 64, t => {
                int eighth = (int)(t / (beat * .5f));
                float local = t % (beat * .5f), pulse = t % beat;
                int chord = bass[((int)(t / (beat * 4))) % bass.Length];
                float lead = Tone(melody[eighth % melody.Length], local) * Mathf.Exp(-local * 13) * .20f;
                float low = Tone(chord, pulse) * Mathf.Exp(-pulse * 9) * .19f;
                float kick = Mathf.Sin(2 * Mathf.PI * (65 * pulse + 2 * (1 - Mathf.Exp(-pulse * 40)))) * Mathf.Exp(-pulse * 28) * .22f;
                float hatTime = t % (beat * .5f);
                float hat = Mathf.Sin(t * 17431) * Mathf.Sin(t * 11597) * Mathf.Exp(-hatTime * 90) * .07f;
                return lead + low + kick + hat;
            });
        }
        public void Dispose() { foreach (var clip in clips) Object.Destroy(clip); }
    }
}
