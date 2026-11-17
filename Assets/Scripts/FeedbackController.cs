using UnityEngine;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    public sealed class FeedbackController : MonoBehaviour
    {
        [SerializeField] private AudioClip rifleShot;
        [SerializeField] private AudioClip sidearmShot;
        [SerializeField] private AudioClip reload;
        [SerializeField] private AudioClip weaponSwitch;
        [SerializeField] private AudioClip hit;
        [SerializeField] private AudioClip kill;
        [SerializeField] private Material flashMaterial;
        [SerializeField] private int poolSize = 8;

        private AudioSource audioSource;
        private WeaponView weaponView;
        private ImpactEffectPool impactEffects;
        private Initialization settings;
        private ImpactFlash[] flashes;
        private int nextFlash;

        private void Awake()
        {
            weaponView = GetComponent<WeaponView>();
            impactEffects = GetComponent<ImpactEffectPool>();
            settings = GetComponentInParent<Initialization>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.15f;
            poolSize = Mathf.Clamp(poolSize, 4, 32);
            flashes = new ImpactFlash[poolSize];
            for (int i = 0; i < flashes.Length; i++)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Impact Flash " + i;
                quad.layer = 8;
                Collider quadCollider = quad.GetComponent<Collider>();
                if (quadCollider != null)
                {
                    Destroy(quadCollider);
                }

                quad.transform.SetParent(transform, false);
                ImpactFlash flash = quad.AddComponent<ImpactFlash>();
                flash.Initialize(flashMaterial);
                flashes[i] = flash;
            }
        }

        private void Update()
        {
            if (settings != null && settings.IsPaused)
            {
                return;
            }

            for (int i = 0; i < flashes.Length; i++)
            {
                flashes[i].Tick(Time.deltaTime);
            }
        }

        public void PlayShot(WeaponModelKind kind)
        {
            Play(kind == WeaponModelKind.Rifle ? rifleShot : sidearmShot, 0.75f);
            if (weaponView != null)
            {
                ShowFlash(weaponView.GetMuzzlePosition(), transform.rotation, 0.08f,
                    new Color(1f, 0.72f, 0.2f, 1f), 0.045f);
                if (impactEffects != null)
                {
                    impactEffects.SpawnShell(weaponView.GetMuzzlePosition(), transform.right);
                }
            }
        }

        public void PlayImpact(Vector3 position, Vector3 normal, bool killed)
        {
            Play(killed ? kill : hit, killed ? 0.7f : 0.45f);
            if (impactEffects != null)
            {
                impactEffects.SpawnDecal(position, normal);
            }

            ShowFlash(position, Quaternion.LookRotation(normal), killed ? 0.065f : 0.04f,
                killed ? new Color(0.2f, 1f, 0.65f, 1f) : new Color(1f, 0.75f, 0.15f, 1f), 0.12f);
        }

        public void PlayReload()
        {
            Play(reload, 0.65f);
        }

        public void PlaySwitch()
        {
            Play(weaponSwitch, 0.55f);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        private void ShowFlash(Vector3 position, Quaternion rotation, float scale, Color color, float duration)
        {
            if (flashes == null || flashes.Length == 0 || flashMaterial == null)
            {
                return;
            }

            ImpactFlash flash = flashes[nextFlash];
            nextFlash = (nextFlash + 1) % flashes.Length;
            flash.Show(position, rotation, scale, color, duration);
        }
    }
}
