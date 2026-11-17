using UnityEngine;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class MoveCamera : MonoBehaviour
    {
        private const float StandingHeight = 1.8f;
        private const float CrouchHeight = 1.15f;
        private const float StandingEyeHeight = 1.6f;
        private const float CrouchEyeHeight = 0.95f;

        [Header("Fallback values used without Initialization")]
        public float mMoveSpeed = GameSettings.DefaultMovementSpeed;
        public float mMouseCmPer360 = AimSensitivity.DefaultCmPer360;

        [Header("Look")]
        public float mMinimumX = -89f;
        public float mMaximumX = 89f;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float recoilRecovery = 12f;

        [Header("Movement")]
        [SerializeField] private float acceleration = 30f;
        [SerializeField] private float deceleration = 25f;
        [SerializeField] private float airAcceleration = 8f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float crouchMultiplier = 0.5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private LayerMask environmentLayers = 1 << 10;

        private CharacterController characterController;
        private WeaponController weapon;
        private TargetSpawner targetSpawner;
        private PracticeSession trainingSession;
        private Initialization settings;
        private LookAngles lookAngles;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float recoilPitch;
        private float recoilYaw;
        private bool isCrouching;

        public float Pitch { get { return lookAngles.Pitch; } }
        public float Yaw { get { return lookAngles.Yaw; } }
        public bool IsGrounded { get { return characterController != null && characterController.isGrounded; } }
        public bool IsCrouching { get { return isCrouching; } }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            weapon = GetComponentInChildren<WeaponController>();
            targetSpawner = GetComponentInChildren<TargetSpawner>();
            trainingSession = GetComponentInChildren<PracticeSession>();
            settings = GetComponent<Initialization>();
            if (cameraPivot == null)
            {
                Camera view = GetComponentInChildren<Camera>();
                if (view != null)
                {
                    cameraPivot = view.transform;
                }
            }

            float pitch = cameraPivot != null ? cameraPivot.localEulerAngles.x : 0f;
            lookAngles = new LookAngles(new Vector3(pitch, transform.localEulerAngles.y, 0f));
            characterController.height = StandingHeight;
            characterController.center = Vector3.up * (StandingHeight * 0.5f);
            ApplyViewRotation();
        }

        private void Update()
        {
            if (settings != null ? !settings.CanUseGameplayInput : Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            UpdateLook();
            UpdateMovement();
        }

        public void AddRecoil(float pitch, float yaw)
        {
            recoilPitch = Mathf.Clamp(recoilPitch + pitch, -20f, 5f);
            recoilYaw = Mathf.Clamp(recoilYaw + yaw, -5f, 5f);
        }

        private void UpdateLook()
        {
            float sensitivity = settings != null ? settings.DegreesPerCount : AimSensitivity.DegreesPerCount(mMouseCmPer360);
            if (weapon != null)
            {
                sensitivity *= weapon.AimSensitivityMultiplier;
            }
            Vector2 lookInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            float errorBefore = targetSpawner != null ? targetSpawner.GetMinimumAimError(transform.position, transform.forward) : 0f;
            lookAngles.Apply(lookInput, sensitivity, new Vector2(mMinimumX, mMaximumX));

            float recovery = 1f - Mathf.Exp(-recoilRecovery * Time.deltaTime);
            recoilPitch = Mathf.Lerp(recoilPitch, 0f, recovery);
            recoilYaw = Mathf.Lerp(recoilYaw, 0f, recovery);
            ApplyViewRotation();
            if (trainingSession != null && targetSpawner != null)
            {
                float errorAfter = targetSpawner.GetMinimumAimError(transform.position, transform.forward);
                float absoluteDelta = (Mathf.Abs(lookInput.x) + Mathf.Abs(lookInput.y)) * sensitivity;
                trainingSession.RecordAimSample(absoluteDelta, Mathf.Max(0f, errorBefore - errorAfter));
            }
        }

        private void ApplyViewRotation()
        {
            transform.localRotation = Quaternion.Euler(0f, lookAngles.Yaw, 0f);
            if (cameraPivot != null)
            {
                float viewPitch = Mathf.Clamp(lookAngles.Pitch + recoilPitch, mMinimumX, mMaximumX);
                cameraPivot.localRotation = Quaternion.Euler(viewPitch, recoilYaw, 0f);
            }
        }

        private void UpdateMovement()
        {
            bool grounded = characterController.isGrounded;
            UpdateCrouchState();

            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            float speed = settings != null ? settings.MovementSpeed : mMoveSpeed;
            speed = GameSettings.SanitizeMovementSpeed(speed);
            if (isCrouching)
            {
                speed *= crouchMultiplier;
            }
            else if (Input.GetKey(KeyCode.LeftShift) && input.y > 0.1f)
            {
                speed *= sprintMultiplier;
            }

            Vector3 targetVelocity = transform.rotation * new Vector3(input.x, 0f, input.y) * speed;
            float rate = grounded
                ? (input.sqrMagnitude > 0.001f ? acceleration : deceleration)
                : airAcceleration;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * Time.deltaTime);

            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (grounded && !isCrouching && Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 motion = (horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime;
            CollisionFlags flags = characterController.Move(motion);

            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }
        }

        private void UpdateCrouchState()
        {
            bool wantsCrouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
            isCrouching = wantsCrouch || !CanStand();

            float height = isCrouching ? CrouchHeight : StandingHeight;
            float eyeHeight = isCrouching ? CrouchEyeHeight : StandingEyeHeight;
            characterController.height = height;
            characterController.center = Vector3.up * (height * 0.5f);

            if (cameraPivot != null)
            {
                float blend = 1f - Mathf.Exp(-12f * Time.deltaTime);
                Vector3 targetPosition = new Vector3(0f, eyeHeight, 0f);
                cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, targetPosition, blend);
            }
        }

        private bool CanStand()
        {
            float radius = characterController.radius * 0.95f;
            float bottomOffset = Mathf.Max(radius, CrouchHeight - radius);
            Vector3 bottom = transform.position + Vector3.up * bottomOffset;
            Vector3 top = transform.position + Vector3.up * (StandingHeight - radius);
            return !Physics.CheckCapsule(bottom, top, radius, environmentLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
