using UnityEngine;
using FpsPractice.Models;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public sealed class WeaponView : MonoBehaviour
    {
        [SerializeField] private Material[] materials = new Material[5];
        [SerializeField] private float switchDuration = 0.25f;
        [SerializeField] private float bobDistance = 0.008f;
        [SerializeField] private float swayAmount = 0.35f;

        private GameObject rifle;
        private GameObject sidearm;
        private GameObject activeModel;
        private WeaponModelKind currentKind;
        private float switchStarted;
        private float recoilKick;
        private float bobPhase;
        private float swayX;
        private float swayY;
        private float aimBlend;
        private float reloadProgress;
        private CharacterController characterController;
        private Initialization settings;
        private PracticeSession session;

        private float CurrentTime { get { return session != null ? session.TimeSeconds : Time.time; } }

        private void Awake()
        {
            characterController = GetComponentInParent<CharacterController>();
            settings = GetComponentInParent<Initialization>();
            session = GetComponentInParent<PracticeSession>();
            if (materials == null || materials.Length < 5)
            {
                Debug.LogError("WeaponView requires five materials: Gunmetal, Polymer, Accent, Grip and Sight.", this);
                enabled = false;
                return;
            }

            rifle = CreateModel("Vanguard Rifle View", WeaponModelFactory.BuildRifle());
            sidearm = CreateModel("Precision Sidearm View", WeaponModelFactory.BuildSidearm());
            rifle.SetActive(false);
            sidearm.SetActive(false);
        }

        public void ConfigureMaterials(Material[] slots)
        {
            materials = slots;
        }

        public void SwitchTo(WeaponModelKind kind)
        {
            if (!enabled) return;
            currentKind = kind;
            if (rifle != null) rifle.SetActive(kind == WeaponModelKind.Rifle);
            if (sidearm != null) sidearm.SetActive(kind == WeaponModelKind.Sidearm);
            activeModel = kind == WeaponModelKind.Rifle ? rifle : sidearm;
            switchStarted = CurrentTime;
            recoilKick = 0f;
            reloadProgress = 0f;
            aimBlend = 0f;
        }

        public void SetAimBlend(float value)
        {
            aimBlend = Mathf.Clamp01(value);
        }

        public void SetReloadProgress(float value)
        {
            reloadProgress = Mathf.Clamp01(value);
        }

        public void OnFire(float recoilScale)
        {
            recoilKick = Mathf.Min(0.12f, recoilKick + 0.025f * Mathf.Max(0.2f, recoilScale));
        }

        public Vector3 GetMuzzlePosition()
        {
            if (activeModel == null) return transform.position;
            Vector3 localMuzzle = currentKind == WeaponModelKind.Rifle
                ? new Vector3(0f, 0.005f, 0.52f)
                : new Vector3(0f, 0.035f, 0.235f);
            return activeModel.transform.TransformPoint(localMuzzle);
        }

        private void LateUpdate()
        {
            if (activeModel == null || (settings != null && settings.IsPaused))
            {
                return;
            }

            float duration = Mathf.Max(0.01f, switchDuration);
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((CurrentTime - switchStarted) / duration));
            float blend = 1f - Mathf.Exp(-14f * Time.deltaTime);
            recoilKick = Mathf.Lerp(recoilKick, 0f, blend);

            bool canSway = settings == null || settings.CanUseGameplayInput;
            float mouseX = canSway ? Input.GetAxisRaw("Mouse X") : 0f;
            float mouseY = canSway ? Input.GetAxisRaw("Mouse Y") : 0f;
            float swayReduction = 1f - aimBlend * 0.75f;
            swayX = Mathf.Lerp(swayX, -mouseX * swayAmount * swayReduction, blend);
            swayY = Mathf.Lerp(swayY, -mouseY * swayAmount * swayReduction, blend);

            float speed = 0f;
            bool grounded = false;
            if (characterController != null)
            {
                Vector3 velocity = characterController.velocity;
                speed = new Vector2(velocity.x, velocity.z).magnitude;
                grounded = characterController.isGrounded;
            }

            float bobWeight = canSway && grounded ? Mathf.Clamp01(speed / 6f) * swayReduction : 0f;
            bobPhase += Time.deltaTime * (4f + speed * 1.5f) * bobWeight;
            Vector3 bob = new Vector3(
                Mathf.Sin(bobPhase) * bobDistance * bobWeight,
                Mathf.Cos(bobPhase * 2f) * bobDistance * bobWeight,
                0f);

            Vector3 hipPosition = currentKind == WeaponModelKind.Rifle
                ? new Vector3(0.24f, -0.23f, 0.56f)
                : new Vector3(0.25f, -0.22f, 0.45f);
            Vector3 adsPosition = currentKind == WeaponModelKind.Rifle
                ? new Vector3(0f, -0.105f, 0.52f)
                : new Vector3(0f, -0.12f, 0.42f);
            Vector3 hipRotation = currentKind == WeaponModelKind.Rifle
                ? new Vector3(0f, -3f, 0f)
                : new Vector3(0f, -2f, 0f);

            float reloadDip = Mathf.Sin(reloadProgress * Mathf.PI) * 0.18f;
            Vector3 basePosition = Vector3.Lerp(hipPosition, adsPosition, aimBlend);
            Vector3 baseRotation = Vector3.Lerp(hipRotation, Vector3.zero, aimBlend);
            Vector3 switchOffset = Vector3.down * ((1f - progress) * 0.28f);
            Vector3 reloadOffset = Vector3.down * reloadDip;
            Vector3 recoilOffset = Vector3.back * recoilKick;
            activeModel.transform.localPosition = basePosition + switchOffset + reloadOffset + recoilOffset + bob;
            activeModel.transform.localRotation = Quaternion.Euler(
                baseRotation.x + swayY + reloadDip * 65f,
                baseRotation.y + swayX,
                baseRotation.z - swayX * 0.5f);
        }

        private GameObject CreateModel(string modelName, Mesh mesh)
        {
            GameObject model = new GameObject(modelName);
            model.layer = 8;
            model.transform.SetParent(transform, false);
            MeshFilter filter = model.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = model.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return model;
        }
    }
}
