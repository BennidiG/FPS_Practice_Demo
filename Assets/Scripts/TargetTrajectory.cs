using UnityEngine;

namespace FpsPractice
{
    public static class TargetTrajectory
    {
        public static Vector3 Evaluate(TargetMotionKind motion, Vector3 origin, float amplitudeX,
            float amplitudeY, float speed, float phase, float time)
        {
            switch (motion)
            {
                case TargetMotionKind.PingPong:
                    float x = Mathf.PingPong(time * speed + phase, amplitudeX * 2f) - amplitudeX;
                    return origin + new Vector3(x, 0f, 0f);
                case TargetMotionKind.Sine:
                    float sineX = Mathf.Sin((time + phase) * speed) * amplitudeX;
                    float sineY = Mathf.Sin((time + phase) * speed * 0.73f + phase) * amplitudeY;
                    return origin + new Vector3(sineX, sineY, 0f);
                default:
                    return origin;
            }
        }
    }
}
