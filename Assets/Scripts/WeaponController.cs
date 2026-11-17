using UnityEngine;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    public sealed class WeaponController : MonoBehaviour
    {
        [Header("Loadout")]
        [SerializeField] private WeaponDefinition[] weapons;
        [SerializeField] private float switchDuration = 0.25f;

        [Header("Hitscan")]
        [SerializeField] private float maxShotDistance = 100f;
        [SerializeField] private LayerMask hitLayers = (1 << 9) | (1 << 10);

        [Header("ADS")]
        [SerializeField] private float adsFieldOfView = 45f;
        [SerializeField] private float adsTransitionSpeed = 10f;
        [SerializeField] private float adsSensitivity = 0.65f;

        private Initialization settings;
        private TargetSpawner targetSpawner;
        private PracticeSession session;
        private WeaponView weaponView;
        private FeedbackController feedback;
        private Camera viewCamera;
        private WeaponState[] weaponStates;
        private int[] shotSequences;
        private int currentIndex;
        private float switchReadyTime;
        private float defaultFieldOfView = 60f;
        private float aimBlend;
        private readonly SeededRandom fallbackRandom = new SeededRandom(12345);

        public int CurrentIndex { get { return currentIndex; } }
        public int WeaponCount { get { return weapons != null ? weapons.Length : 0; } }
        public string CurrentWeaponName { get { return CurrentDefinition != null ? CurrentDefinition.DisplayName : "Unarmed"; } }
        public bool IsSwitching { get { return TimeNow < switchReadyTime; } }
        public int CurrentAmmo { get { return CurrentState != null ? CurrentState.CurrentAmmo : 0; } }
        public int MagazineSize { get { return CurrentState != null ? CurrentState.MagazineSize : 0; } }
        public bool IsReloading { get { return CurrentState != null && CurrentState.IsReloading; } }
        public float LastShotExecutionMs { get; private set; }
        public float WorstShotExecutionMs { get; private set; }
        public float AimBlend { get { return aimBlend; } }
        public bool IsAimingDownSights { get { return aimBlend > 0.5f; } }
        public float AimSensitivityMultiplier { get { return Mathf.Lerp(1f, adsSensitivity, aimBlend); } }

        private float TimeNow { get { return session != null ? session.TimeSeconds : Time.time; } }
        private SeededRandom Random { get { return session != null ? session.Random : fallbackRandom; } }
        private WeaponDefinition CurrentDefinition
        {
            get { return weapons != null && currentIndex >= 0 && currentIndex < weapons.Length ? weapons[currentIndex] : null; }
        }
        private WeaponState CurrentState
        {
            get { return weaponStates != null && currentIndex >= 0 && currentIndex < weaponStates.Length ? weaponStates[currentIndex] : null; }
        }

        public void Configure(float shotDistance, LayerMask layers, Initialization inputSettings, TargetSpawner spawner, PracticeSession practiceSession)
        {
            maxShotDistance = Mathf.Max(0.1f, shotDistance);
            hitLayers = layers;
            settings = inputSettings;
            targetSpawner = spawner;
            session = practiceSession;
        }

        public void ConfigureLoadout(WeaponDefinition[] loadout)
        {
            weapons = loadout;
        }

        private void Awake()
        {
            if (settings == null) settings = GetComponentInParent<Initialization>();
            if (targetSpawner == null) targetSpawner = GetComponent<TargetSpawner>();
            if (session == null) session = GetComponent<PracticeSession>();
            weaponView = GetComponent<WeaponView>();
            feedback = GetComponent<FeedbackController>();
            viewCamera = GetComponent<Camera>();
            if (viewCamera != null)
            {
                defaultFieldOfView = viewCamera.fieldOfView;
            }

            maxShotDistance = Mathf.Max(0.1f, maxShotDistance);
            if (weapons == null || weapons.Length == 0)
            {
                weapons = new[] { WeaponDefinition.CreateDefault() };
            }

            weaponStates = new WeaponState[weapons.Length];
            shotSequences = new int[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == null) weapons[i] = WeaponDefinition.CreateDefault();
                weaponStates[i] = new WeaponState(weapons[i].MagazineSize, weapons[i].RoundsPerMinute, weapons[i].ReloadDuration);
            }

            currentIndex = 0;
            switchReadyTime = 0f;
            if (weaponView != null) weaponView.SwitchTo(CurrentDefinition.Model);
            if (targetSpawner == null || session == null)
            {
                Debug.LogError("WeaponController requires TargetSpawner and PracticeSession components.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (CurrentState != null)
            {
                CurrentState.Update(TimeNow);
            }

            bool paused = settings != null && settings.IsPaused;
            bool canUseInput = settings != null ? settings.CanUseGameplayInput : Cursor.lockState == CursorLockMode.Locked;
            if (!paused)
            {
                UpdateAim(canUseInput);
            }

            if (!canUseInput)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchWeapon(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon(1);
            else if (Input.GetKeyDown(KeyCode.Q)) SwitchWeapon((currentIndex + 1) % WeaponCount);

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && WeaponCount > 0)
            {
                int direction = scroll > 0f ? 1 : -1;
                SwitchWeapon((currentIndex + direction + WeaponCount) % WeaponCount);
            }

            if (IsSwitching || CurrentDefinition == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                bool wasReloading = CurrentState.IsReloading;
                CurrentState.BeginReload(TimeNow);
                if (!wasReloading && CurrentState.IsReloading && feedback != null)
                {
                    feedback.PlayReload();
                }
            }

            bool fireRequested = CurrentDefinition.Automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
            if (fireRequested)
            {
                TryShoot(new Ray(transform.position, transform.forward));
            }
        }

        private void UpdateAim(bool canUseInput)
        {
            bool wantsAim = canUseInput && Input.GetMouseButton(1) && !IsSwitching && !IsReloading;
            aimBlend = Mathf.MoveTowards(aimBlend, wantsAim ? 1f : 0f, adsTransitionSpeed * Time.deltaTime);
            if (viewCamera != null)
            {
                viewCamera.fieldOfView = Mathf.Lerp(defaultFieldOfView, adsFieldOfView, aimBlend);
            }

            if (weaponView != null)
            {
                weaponView.SetAimBlend(aimBlend);
                if (CurrentState != null)
                {
                    weaponView.SetReloadProgress(CurrentState.GetReloadProgress(TimeNow));
                }
            }
        }

        public void BeginReload()
        {
            if ((settings != null && settings.IsPaused) || (session != null && !session.IsRunning))
            {
                return;
            }

            if (CurrentState != null)
            {
                CurrentState.BeginReload(TimeNow);
            }
        }

        public bool SwitchWeapon(int index)
        {
            if ((settings != null && settings.IsPaused) || (session != null && !session.IsRunning))
            {
                return false;
            }

            if (weaponStates == null || index < 0 || index >= weapons.Length || index == currentIndex || IsSwitching)
            {
                return false;
            }

            if (CurrentState != null) CurrentState.CancelReload();
            currentIndex = index;
            switchReadyTime = TimeNow + Mathf.Max(0.05f, switchDuration);
            aimBlend = 0f;
            if (weaponView != null) weaponView.SwitchTo(CurrentDefinition.Model);
            if (feedback != null) feedback.PlaySwitch();
            return true;
        }

        public ShotResult TryShoot(Ray ray)
        {
            if ((settings != null && settings.IsPaused) || (session != null && !session.IsRunning))
            {
                return ShotResult.NotReady;
            }

            WeaponDefinition definition = CurrentDefinition;
            WeaponState state = CurrentState;
            if (definition == null || state == null || IsSwitching || !state.TryFire(TimeNow))
            {
                return ShotResult.NotReady;
            }

            float shotStarted = Time.realtimeSinceStartup;
            Vector2 spread = Random.InsideUnitCircle() * (definition.SpreadAngle * Mathf.Lerp(1f, 0.5f, aimBlend));
            Vector3 direction = Quaternion.AngleAxis(spread.x, transform.up)
                * Quaternion.AngleAxis(-spread.y, transform.right) * ray.direction;
            Ray shotRay = new Ray(ray.origin, direction);

            bool hit = false;
            RaycastHit hitInfo;
            float range = Mathf.Min(maxShotDistance, definition.Range);
            if (Physics.Raycast(shotRay, out hitInfo, range, hitLayers, QueryTriggerInteraction.Ignore))
            {
                ShootTarget target;
                if (targetSpawner.TryGetTarget(hitInfo.collider, out target))
                {
                    TargetHitResult result = target.ApplyDamage(definition.Damage, hitInfo.point);
                    hit = true;
                    if (result.Killed)
                    {
                        targetSpawner.RespawnTarget(target);
                    }

                    session.RegisterShot(result, TimeNow - target.SpawnTime);
                }
                else
                {
                    session.RegisterShot(false);
                }

                if (feedback != null)
                {
                    feedback.PlayImpact(hitInfo.point, hitInfo.normal, hit);
                }
            }
            else
            {
                session.RegisterShot(false);
            }

            MoveCamera player = GetComponentInParent<MoveCamera>();
            if (player != null)
            {
                Vector2 recoil = RecoilPattern.Evaluate(shotSequences[currentIndex], definition.RecoilPitch, definition.RecoilYaw, definition.Model);
                shotSequences[currentIndex] = Mathf.Min(shotSequences[currentIndex] + 1, 1000000);
                float adsRecoil = Mathf.Lerp(1f, 0.75f, aimBlend);
                player.AddRecoil(-recoil.x * adsRecoil, recoil.y * adsRecoil);
            }

            if (weaponView != null) weaponView.OnFire(definition.RecoilPitch * Mathf.Lerp(1f, 0.75f, aimBlend));
            if (feedback != null)
            {
                feedback.PlayShot(definition.Model);
                if (state.IsReloading) feedback.PlayReload();
            }

            LastShotExecutionMs = (Time.realtimeSinceStartup - shotStarted) * 1000f;
            WorstShotExecutionMs = Mathf.Max(WorstShotExecutionMs, LastShotExecutionMs);
            return hit ? ShotResult.FiredHit : ShotResult.FiredMiss;
        }

        public void ResetPerformanceMetrics()
        {
            LastShotExecutionMs = 0f;
            WorstShotExecutionMs = 0f;
        }

        public void ResetWeapon()
        {
            if (weaponStates != null)
            {
                for (int i = 0; i < weaponStates.Length; i++) weaponStates[i].Reset();
            }

            if (shotSequences != null)
            {
                for (int i = 0; i < shotSequences.Length; i++) shotSequences[i] = 0;
            }

            currentIndex = 0;
            switchReadyTime = 0f;
            aimBlend = 0f;
            if (viewCamera != null) viewCamera.fieldOfView = defaultFieldOfView;
            if (weaponView != null && CurrentDefinition != null) weaponView.SwitchTo(CurrentDefinition.Model);
        }
    }
}
