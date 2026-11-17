using UnityEngine;

namespace FpsPractice
{
    public enum TargetHitZone
    {
        Bullseye,
        Critical,
        Inner,
        Outer,
        Rim,
        Frame
    }

    public struct TargetHitResult
    {
        public TargetHitZone Zone;
        public int Score;
        public float Damage;
        public bool Killed;
        public bool FirstHit;

        public TargetHitResult(TargetHitZone zone, int score, float damage, bool killed, bool firstHit)
        {
            Zone = zone;
            Score = score;
            Damage = damage;
            Killed = killed;
            FirstHit = firstHit;
        }
    }

    public static class TargetHitZoneEvaluator
    {
        public static TargetHitZone Evaluate(Vector3 localHitPoint)
        {
            return EvaluateRadius(Mathf.Sqrt(localHitPoint.x * localHitPoint.x + localHitPoint.z * localHitPoint.z));
        }

        public static TargetHitZone EvaluateRadius(float radius)
        {
            if (radius <= 0.10f) return TargetHitZone.Bullseye;
            if (radius <= 0.24f) return TargetHitZone.Critical;
            if (radius <= 0.38f) return TargetHitZone.Inner;
            if (radius <= 0.52f) return TargetHitZone.Outer;
            if (radius <= 0.59f) return TargetHitZone.Rim;
            return TargetHitZone.Frame;
        }

        public static TargetHitResult ApplyDamage(TargetHitZone zone, float baseDamage, float currentHealth, bool firstHit)
        {
            float multiplier;
            int score;
            switch (zone)
            {
                case TargetHitZone.Bullseye:
                    multiplier = 2f;
                    score = 100;
                    break;
                case TargetHitZone.Critical:
                    multiplier = 1.5f;
                    score = 75;
                    break;
                case TargetHitZone.Inner:
                    multiplier = 1.25f;
                    score = 50;
                    break;
                case TargetHitZone.Outer:
                    multiplier = 1f;
                    score = 25;
                    break;
                case TargetHitZone.Rim:
                    multiplier = 0.75f;
                    score = 10;
                    break;
                default:
                    multiplier = 0.5f;
                    score = 5;
                    break;
            }

            float damage = Mathf.Max(0f, baseDamage) * multiplier;
            return new TargetHitResult(zone, score, damage, currentHealth - damage <= 0f, firstHit);
        }
    }
}
