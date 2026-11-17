using UnityEngine;

namespace FpsPractice
{
    public struct LookAngles
    {
        private float pitch;
        private float yaw;

        public float Pitch { get { return pitch; } }
        public float Yaw { get { return yaw; } }

        public LookAngles(Vector3 eulerAngles)
        {
            pitch = NormalizeAngle(eulerAngles.x);
            yaw = NormalizeAngle(eulerAngles.y);
        }

        public void Apply(Vector2 input, float sensitivity, Vector2 pitchLimits)
        {
            if (!GameSettings.IsFinite(input.x) || !GameSettings.IsFinite(input.y))
            {
                input = Vector2.zero;
            }

            if (!GameSettings.IsFinite(sensitivity) || sensitivity <= 0f)
            {
                sensitivity = AimSensitivity.DegreesPerCount(AimSensitivity.DefaultCmPer360);
            }
            float minimumPitch = Mathf.Min(pitchLimits.x, pitchLimits.y);
            float maximumPitch = Mathf.Max(pitchLimits.x, pitchLimits.y);

            pitch = Mathf.Clamp(pitch - input.y * sensitivity, minimumPitch, maximumPitch);
            yaw = NormalizeAngle(yaw + input.x * sensitivity);
        }

        public static float NormalizeAngle(float angle)
        {
            return GameSettings.IsFinite(angle) ? Mathf.Repeat(angle + 180f, 360f) - 180f : 0f;
        }
    }
}
