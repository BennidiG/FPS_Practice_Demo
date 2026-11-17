using UnityEngine;

namespace FpsPractice
{
    public static class GameSettings
    {
        public const float DefaultMouseSensitivity = 2f;
        public const float DefaultMovementSpeed = 8f;
        public const float MinimumMouseSensitivity = 0.1f;
        public const float MaximumMouseSensitivity = 10f;
        public const float MinimumMovementSpeed = 1f;
        public const float MaximumMovementSpeed = 20f;

        public static float SanitizeMouseSensitivity(float value)
        {
            return Sanitize(value, DefaultMouseSensitivity, MinimumMouseSensitivity, MaximumMouseSensitivity);
        }

        public static float SanitizeMovementSpeed(float value)
        {
            return Sanitize(value, DefaultMovementSpeed, MinimumMovementSpeed, MaximumMovementSpeed);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float Sanitize(float value, float fallback, float minimum, float maximum)
        {
            return IsFinite(value) ? Mathf.Clamp(value, minimum, maximum) : fallback;
        }
    }
}
