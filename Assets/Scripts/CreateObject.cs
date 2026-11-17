using System.Collections.Generic;
using UnityEngine;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class CreateObject : MonoBehaviour
    {
        [Header("Legacy configuration mapped to the new components")]
        public GameObject ShootObj;
        [SerializeField] private int targetCount = 6;
        [SerializeField] private Vector2 spawnX = new Vector2(-8f, 8f);
        [SerializeField] private Vector2 spawnY = new Vector2(2f, 6f);
        [SerializeField] private float spawnZ = -1f;
        [SerializeField] private Vector2 spawnDepth = new Vector2(-1f, -1f);
        [SerializeField] private float minimumSpacing = 1.5f;
        [SerializeField] private float minimumViewAngle = 4f;
        [SerializeField] private float minimumRelocationDistance = 1.5f;
        [SerializeField] private float respawnDelay;
        [SerializeField] private float maxShotDistance = 100f;
        [SerializeField] private LayerMask hitLayers = (1 << 9) | (1 << 10);
        [SerializeField] private Initialization settings;

        private TargetSpawner targetSpawner;
        private WeaponController weapon;
        private PracticeSession session;

        public int ShotsFired { get { return session != null ? session.ShotsFired : 0; } }
        public int TargetsHit { get { return session != null ? session.TargetsHit : 0; } }
        public float Accuracy { get { return session != null ? session.Accuracy : 0f; } }
        public int ActiveTargetCount { get { return targetSpawner != null ? targetSpawner.ActiveTargetCount : 0; } }
        public IReadOnlyList<ShootTarget> Targets { get { return targetSpawner != null ? targetSpawner.Targets : null; } }

        private void Awake()
        {
            targetSpawner = GetComponent<TargetSpawner>();
            weapon = GetComponent<WeaponController>();
            session = GetComponent<PracticeSession>();
            if (settings == null)
            {
                settings = GetComponentInParent<Initialization>();
            }

            if (targetSpawner == null || weapon == null || session == null)
            {
                Debug.LogError("CreateObject compatibility facade requires TargetSpawner, WeaponController and PracticeSession.", this);
                enabled = false;
                return;
            }

            targetSpawner.ConfigureAdvanced(ShootObj, targetCount, spawnX, spawnY, spawnDepth,
                minimumSpacing, transform, minimumViewAngle, minimumRelocationDistance, respawnDelay);
            weapon.Configure(maxShotDistance, hitLayers, settings, targetSpawner, session);
        }

        public bool TryShoot(Ray ray)
        {
            return weapon != null && weapon.TryShoot(ray) == ShotResult.FiredHit;
        }

        public bool SwitchWeapon(int index)
        {
            return weapon != null && weapon.SwitchWeapon(index);
        }

        public void ResetSession()
        {
            if (session != null)
            {
                session.ResetSession();
            }
        }
    }
}
