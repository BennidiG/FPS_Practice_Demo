using UnityEngine;

namespace FpsPractice
{
    public static class RecoilPattern
    {
        private static readonly float[] PitchCurve = { 0.55f, 0.75f, 1f, 1.15f, 1.3f, 1.2f, 1.1f, 0.95f, 0.85f, 0.75f };
        private static readonly float[] YawCurve = { 0f, -0.35f, 0.45f, -0.3f, 0.25f, -0.15f, 0.1f, -0.05f, 0.05f, 0f };

        public static Vector2 Evaluate(int shotIndex, float pitch, float yaw, WeaponModelKind kind)
        {
            int index = Mathf.Clamp(shotIndex, 0, PitchCurve.Length - 1);
            float sidearmMultiplier = kind == WeaponModelKind.Sidearm ? 1.25f : 1f;
            return new Vector2(PitchCurve[index] * pitch * sidearmMultiplier, YawCurve[index] * yaw);
        }
    }
}
