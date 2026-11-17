using System;

namespace FpsPractice
{
    public enum ShotResult
    {
        NotReady,
        FiredMiss,
        FiredHit
    }

    public sealed class WeaponState
    {
        private float nextFireTime;
        private float reloadEndTime;

        public int MagazineSize { get; private set; }
        public int CurrentAmmo { get; private set; }
        public float RoundsPerMinute { get; private set; }
        public float ReloadDuration { get; private set; }
        public bool IsReloading { get; private set; }

        public WeaponState(int magazineSize, float roundsPerMinute, float reloadDuration)
        {
            MagazineSize = Math.Max(1, magazineSize);
            RoundsPerMinute = Math.Max(1f, roundsPerMinute);
            ReloadDuration = Math.Max(0.05f, reloadDuration);
            Reset();
        }

        public bool TryFire(float time)
        {
            Update(time);
            if (IsReloading || time < nextFireTime)
            {
                return false;
            }

            if (CurrentAmmo <= 0)
            {
                BeginReload(time);
                return false;
            }

            CurrentAmmo--;
            nextFireTime = time + 60f / RoundsPerMinute;
            if (CurrentAmmo == 0)
            {
                BeginReload(time);
            }

            return true;
        }

        public void BeginReload(float time)
        {
            if (IsReloading || CurrentAmmo >= MagazineSize)
            {
                return;
            }

            IsReloading = true;
            reloadEndTime = time + ReloadDuration;
        }

        public void Update(float time)
        {
            if (!IsReloading || time < reloadEndTime)
            {
                return;
            }

            CurrentAmmo = MagazineSize;
            IsReloading = false;
        }

        public float GetReloadProgress(float time)
        {
            if (!IsReloading)
            {
                return 0f;
            }

            float remaining = reloadEndTime - time;
            return Clamp01(1f - remaining / ReloadDuration);
        }

        private static float Clamp01(float value)
        {
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }

        public void CancelReload()
        {
            IsReloading = false;
            reloadEndTime = 0f;
        }

        public void Reset()
        {
            CurrentAmmo = MagazineSize;
            IsReloading = false;
            nextFireTime = 0f;
            reloadEndTime = 0f;
        }
    }
}
