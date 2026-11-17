using UnityEngine;

namespace FpsPractice
{
    public enum WeaponModelKind
    {
        Rifle,
        Sidearm
    }

    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "FPS Practice/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Vanguard Rifle";
        [SerializeField] private WeaponModelKind model = WeaponModelKind.Rifle;
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private float roundsPerMinute = 600f;
        [SerializeField] private float reloadDuration = 1.6f;
        [SerializeField] private bool automatic = true;
        [SerializeField] private float spreadAngle = 0.12f;
        [SerializeField] private float recoilPitch = 0.18f;
        [SerializeField] private float recoilYaw = 0.08f;
        [SerializeField] private float range = 100f;
        [SerializeField] private float damage = 34f;

        public string DisplayName { get { return displayName; } }
        public WeaponModelKind Model { get { return model; } }
        public int MagazineSize { get { return magazineSize; } }
        public float RoundsPerMinute { get { return roundsPerMinute; } }
        public float ReloadDuration { get { return reloadDuration; } }
        public bool Automatic { get { return automatic; } }
        public float SpreadAngle { get { return spreadAngle; } }
        public float RecoilPitch { get { return recoilPitch; } }
        public float RecoilYaw { get { return recoilYaw; } }
        public float Range { get { return range; } }
        public float Damage { get { return damage; } }

        public static WeaponDefinition CreateDefault()
        {
            return CreateInstance<WeaponDefinition>();
        }

        public static WeaponDefinition CreateSidearmDefault()
        {
            WeaponDefinition definition = CreateInstance<WeaponDefinition>();
            definition.displayName = "Precision Sidearm";
            definition.model = WeaponModelKind.Sidearm;
            definition.magazineSize = 12;
            definition.roundsPerMinute = 260f;
            definition.reloadDuration = 1f;
            definition.automatic = false;
            definition.spreadAngle = 0f;
            definition.recoilPitch = 0.4f;
            definition.recoilYaw = 0.04f;
            definition.range = 100f;
            definition.damage = 50f;
            return definition;
        }
    }
}
