using System;
using System.Collections.Generic;
using UnityEngine;

namespace RollerCoasterHeroes
{
    public enum PickupKind { Coin, Boost, Brake }

    // Arc-length lookup is shared by geometry, pickups, simulation and camera.
    public sealed class RideTrack
    {
        public const int Samples = 1200;
        public readonly Vector3[] Points = new Vector3[Samples + 1];
        readonly float[] starts = new float[Samples + 1];
        public float Length => starts[Samples];

        public RideTrack()
        {
            for (int i = 0; i <= Samples; i++)
            {
                float t = i / (float)Samples;
                Points[i] = new Vector3(1250 * t - 625,
                    19 + 11 * Mathf.Sin(t * Mathf.PI * 5) + 6 * Mathf.Sin(t * Mathf.PI * 12),
                    65 * Mathf.Sin(t * Mathf.PI * 3.2f) + 24 * Mathf.Sin(t * Mathf.PI * 8));
                if (i > 0) starts[i] = starts[i - 1] + Vector3.Distance(Points[i - 1], Points[i]);
            }
        }

        public Vector3 Position(float distance)
        {
            distance = Mathf.Clamp(distance, 0, Length);
            int index = Array.BinarySearch(starts, distance);
            if (index >= 0) return Points[index];
            index = Mathf.Clamp(~index - 1, 0, Samples - 1);
            return Vector3.Lerp(Points[index], Points[index + 1],
                (distance - starts[index]) / (starts[index + 1] - starts[index]));
        }

        public Quaternion Rotation(float distance)
        {
            Vector3 tangent = (Position(distance + 0.7f) - Position(distance - 0.7f)).normalized;
            Vector3 before = (Position(distance) - Position(distance - 4)).normalized;
            Vector3 after = (Position(distance + 4) - Position(distance)).normalized;
            float bank = Mathf.Clamp(Vector3.SignedAngle(before, after, Vector3.up) * -1.6f, -18, 18);
            return Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0, 0, bank);
        }
    }

    public sealed class RidePickup
    {
        public float Distance;
        public PickupKind Kind;
        public bool Collected;
    }

    // No scene dependencies: deterministic fixed-step racing, including replay/reset.
    public sealed class RideModel
    {
        public const float MinSpeed = 7, MaxSpeed = 37;
        public readonly RideTrack Track;
        public readonly List<RidePickup> Pickups = new List<RidePickup>();
        public float Distance { get; private set; }
        public float Speed { get; private set; }
        public float Seconds { get; private set; }
        public int Coins { get; private set; }
        public bool Finished { get; private set; }
        public float EffectSeconds { get; private set; }
        public PickupKind Effect { get; private set; }
        public float Score => Mathf.Max(0, 10000 - 50 * Seconds) + 100 * Coins;
        public event Action<int, PickupKind> Collected;
        public event Action Completed;

        public RideModel(RideTrack track)
        {
            Track = track;
            var random = new System.Random(20261013);
            for (int i = 0; i < 70; i++)
                Pickups.Add(new RidePickup {
                    Distance = track.Length * (0.06f + 0.88f * (i + 0.5f) / 70) + (float)random.NextDouble() * 0.08f,
                    Kind = i % 17 == 0 ? PickupKind.Boost : i % 23 == 0 ? PickupKind.Brake : PickupKind.Coin
                });
            Reset();
        }

        public void Reset()
        {
            Distance = Seconds = EffectSeconds = 0;
            Speed = 13;
            Coins = 0;
            Finished = false;
            Effect = PickupKind.Coin;
            foreach (var p in Pickups) p.Collected = false;
        }

        public void Step(float dt)
        {
            if (Finished || dt <= 0) return;
            EffectSeconds = Mathf.Max(0, EffectSeconds - dt);
            Vector3 delta = Track.Position(Distance + 0.5f) - Track.Position(Distance);
            float slope = delta.y / Mathf.Max(0.0001f, delta.magnitude);
            float motor = EffectSeconds > 0 ? (Effect == PickupKind.Boost ? 8 : -5) : 4;
            Speed = Mathf.Clamp(Speed + (motor - slope * 12 - Speed * 0.07f) * dt, MinSpeed, MaxSpeed);
            float travel = Speed * dt;
            float fraction = Mathf.Min(1, (Track.Length - Distance) / travel);
            Distance = Mathf.Min(Track.Length, Distance + travel);
            Seconds += dt * fraction;
            for (int i = 0; i < Pickups.Count; i++)
            {
                var p = Pickups[i];
                if (p.Collected || Distance < p.Distance) continue;
                p.Collected = true;
                if (p.Kind == PickupKind.Coin) Coins++;
                else
                {
                    Effect = p.Kind;
                    EffectSeconds = p.Kind == PickupKind.Boost ? 5 : 4;
                    Speed = Mathf.Clamp(Speed * (p.Kind == PickupKind.Boost ? 1.4f : 0.7f), MinSpeed, MaxSpeed);
                }
                Collected?.Invoke(i, p.Kind);
            }
            if (Distance >= Track.Length)
            {
                Finished = true;
                EffectSeconds = 0;
                Completed?.Invoke();
            }
        }
    }
}
