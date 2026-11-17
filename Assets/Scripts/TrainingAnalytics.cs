using System;
using System.Collections.Generic;

namespace FpsPractice
{
    public static class TrainingAnalytics
    {
        public static float Percentile(List<float> sortedCopySource, float percentile01)
        {
            if (sortedCopySource == null || sortedCopySource.Count == 0 || float.IsNaN(percentile01))
            {
                return 0f;
            }

            List<float> sorted = new List<float>(sortedCopySource.Count);
            for (int i = 0; i < sortedCopySource.Count; i++)
            {
                float value = sortedCopySource[i];
                if (!float.IsNaN(value) && !float.IsInfinity(value))
                {
                    sorted.Add(value);
                }
            }

            if (sorted.Count == 0)
            {
                return 0f;
            }

            sorted.Sort();
            if (percentile01 <= 0f) return sorted[0];
            if (percentile01 >= 1f) return sorted[sorted.Count - 1];

            float position = percentile01 * (sorted.Count - 1);
            int lower = (int)position;
            int upper = lower + 1;
            float fraction = position - lower;
            return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
        }
    }

    public sealed class AimPathTracker
    {
        private float totalAbsoluteDelta;
        private float totalIdealDelta;

        public float TotalAbsoluteDelta { get { return totalAbsoluteDelta; } }
        public float TotalIdealDelta { get { return totalIdealDelta; } }
        public float Efficiency
        {
            get
            {
                if (totalAbsoluteDelta <= 0f || totalIdealDelta <= 0f) return 0f;
                float efficiency = totalIdealDelta / totalAbsoluteDelta;
                if (float.IsNaN(efficiency) || efficiency <= 0f) return 0f;
                return efficiency < 1f ? efficiency : 1f;
            }
        }

        public void Reset()
        {
            totalAbsoluteDelta = 0f;
            totalIdealDelta = 0f;
        }

        public void AddSample(float absoluteYawDelta, float idealYawDelta)
        {
            if (!IsFinite(absoluteYawDelta) || !IsFinite(idealYawDelta)) return;
            totalAbsoluteDelta += Math.Abs(absoluteYawDelta);
            totalIdealDelta += Math.Abs(idealYawDelta);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
