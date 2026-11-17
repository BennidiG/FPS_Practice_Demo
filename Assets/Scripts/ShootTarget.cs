using UnityEngine;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ShootTarget : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public float CurrentHealth = 100f;
        public float SpawnTime { get; private set; }
        public float LastHitTime { get; private set; }
        public PracticeSession Session { get; private set; }
        public bool HasBeenHit { get; private set; }

        public void Place(Vector3 position, Quaternion rotation)
        {
            gameObject.SetActive(false);
            transform.SetPositionAndRotation(position, rotation);
            gameObject.SetActive(true);
        }

        public void ConfigureForSpawn(float health, float spawnTime)
        {
            ConfigureForSpawn(health, spawnTime, null);
        }

        public void ConfigureForSpawn(float health, float spawnTime, PracticeSession trainingSession)
        {
            MaxHealth = Mathf.Max(1f, health);
            CurrentHealth = MaxHealth;
            SpawnTime = spawnTime;
            LastHitTime = -100f;
            Session = trainingSession;
            HasBeenHit = false;
        }

        public TargetHitResult ApplyDamage(float baseDamage, Vector3 worldHitPoint)
        {
            Vector3 localHitPoint = transform.InverseTransformPoint(worldHitPoint);
            TargetHitZone zone = TargetHitZoneEvaluator.Evaluate(localHitPoint);
            bool firstHit = !HasBeenHit;
            TargetHitResult result = TargetHitZoneEvaluator.ApplyDamage(zone, baseDamage, CurrentHealth, firstHit);
            CurrentHealth = Mathf.Max(0f, CurrentHealth - result.Damage);
            HasBeenHit = true;
            LastHitTime = Session != null ? Session.TimeSeconds : Time.time;
            return result;
        }
    }
}
