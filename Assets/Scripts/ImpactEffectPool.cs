using UnityEngine;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    public sealed class ImpactEffectPool : MonoBehaviour
    {
        private sealed class Effect
        {
            public GameObject Object;
            public Vector3 Velocity;
            public float Remaining;
            public float Duration;
            public bool Shell;
        }

        [SerializeField] private Material decalMaterial;
        [SerializeField] private Material shellMaterial;
        [SerializeField] private int decalCount = 24;
        [SerializeField] private int shellCount = 12;

        private Effect[] decals;
        private Effect[] shells;
        private int decalIndex;
        private int shellIndex;
        private Initialization settings;

        public int DecalCount { get { return decals != null ? decals.Length : 0; } }
        public int ShellCount { get { return shells != null ? shells.Length : 0; } }

        public void Configure(Material decal, Material shell)
        {
            decalMaterial = decal;
            shellMaterial = shell;
        }

        private void Awake()
        {
            settings = GetComponentInParent<Initialization>();
            decalCount = Mathf.Clamp(decalCount, 4, 64);
            shellCount = Mathf.Clamp(shellCount, 2, 32);
            decals = CreateEffects("Bullet Hole", decalCount, PrimitiveType.Quad, decalMaterial);
            shells = CreateEffects("Shell Casing", shellCount, PrimitiveType.Cube, shellMaterial);
        }

        public void SpawnDecal(Vector3 position, Vector3 normal)
        {
            if (decals == null) return;
            Effect effect = decals[decalIndex];
            decalIndex = (decalIndex + 1) % decals.Length;
            effect.Object.transform.SetPositionAndRotation(position + normal * 0.002f, Quaternion.LookRotation(normal));
            effect.Object.transform.localScale = Vector3.one * 0.09f;
            effect.Remaining = effect.Duration = 10f;
            effect.Object.SetActive(true);
        }

        public void SpawnShell(Vector3 position, Vector3 direction)
        {
            if (shells == null) return;
            Effect effect = shells[shellIndex];
            shellIndex = (shellIndex + 1) % shells.Length;
            effect.Object.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, 20f));
            effect.Object.transform.localScale = new Vector3(0.012f, 0.012f, 0.035f);
            effect.Velocity = direction * 1.4f + Vector3.up * 1.1f;
            effect.Remaining = effect.Duration = 1.4f;
            effect.Object.SetActive(true);
        }

        private void Update()
        {
            if (settings != null && settings.IsPaused) return;
            Tick(decals, false);
            Tick(shells, true);
        }

        private void Tick(Effect[] effects, bool shell)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                Effect effect = effects[i];
                if (effect.Remaining <= 0f) continue;
                effect.Remaining -= Time.deltaTime;
                if (effect.Remaining <= 0f)
                {
                    effect.Object.SetActive(false);
                    continue;
                }

                if (shell)
                {
                    effect.Velocity += Vector3.down * 5.5f * Time.deltaTime;
                    effect.Object.transform.position += effect.Velocity * Time.deltaTime;
                    effect.Object.transform.Rotate(effect.Velocity * 90f * Time.deltaTime, Space.Self);
                }
            }
        }

        private Effect[] CreateEffects(string prefix, int count, PrimitiveType type, Material material)
        {
            Effect[] effects = new Effect[count];
            for (int i = 0; i < count; i++)
            {
                GameObject instance = GameObject.CreatePrimitive(type);
                instance.name = prefix + " " + i;
                instance.layer = 8;
                Collider collider = instance.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                instance.transform.SetParent(transform, false);
                MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                instance.SetActive(false);
                effects[i] = new Effect { Object = instance, Shell = type == PrimitiveType.Cube };
            }

            return effects;
        }
    }
}
