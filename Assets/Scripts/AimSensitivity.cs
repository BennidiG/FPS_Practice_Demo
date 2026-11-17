using UnityEngine;

namespace FpsPractice
{
    public static class AimSensitivity
    {
        public const float CountsPerCentimeter = 100f;
        public const float DefaultCmPer360 = 30f;
        public const float MinimumCmPer360 = 5f;
        public const float MaximumCmPer360 = 100f;

        public static float SanitizeCmPer360(float value)
        {
            if (!GameSettings.IsFinite(value))
            {
                return DefaultCmPer360;
            }

            return Mathf.Clamp(value, MinimumCmPer360, MaximumCmPer360);
        }

        public static float DegreesPerCount(float cmPer360)
        {
            return DegreesPerCount(cmPer360, CountsPerCentimeter);
        }

        public static float DegreesPerCount(float cmPer360, float countsPerCentimeter)
        {
            return 360f / (SanitizeCmPer360(cmPer360) * Mathf.Max(1f, countsPerCentimeter));
        }
    }
}
