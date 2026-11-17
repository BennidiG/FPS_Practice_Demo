using UnityEngine;

namespace FpsPractice
{
    public enum TrainingMode
    {
        StaticFlick,
        DynamicFlick,
        Tracking,
        Reaction
    }

    public enum TargetMotionKind
    {
        Static,
        PingPong,
        Sine
    }

    [CreateAssetMenu(fileName = "TrainingScenario", menuName = "FPS Practice/Training Scenario")]
    public sealed class TrainingScenario : ScriptableObject
    {
        [SerializeField] private string displayName = "Static Flick";
        [SerializeField] private TrainingMode mode = TrainingMode.StaticFlick;
        [SerializeField] private int targetCount = 6;
        [SerializeField] private int targetHealth = 100;
        [SerializeField] private TargetMotionKind motion = TargetMotionKind.Static;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float amplitudeX;
        [SerializeField] private float amplitudeY;
        [SerializeField] private float minimumRespawnDelay = 0.2f;
        [SerializeField] private float maximumRespawnDelay = 0.2f;
        [SerializeField] private float duration = 60f;
        [SerializeField] private uint seed = 12345;

        public string DisplayName { get { return displayName; } }
        public TrainingMode Mode { get { return mode; } }
        public int TargetCount { get { return Mathf.Clamp(targetCount, 1, 32); } }
        public int TargetHealth { get { return Mathf.Max(1, targetHealth); } }
        public TargetMotionKind Motion { get { return motion; } }
        public float MoveSpeed { get { return Mathf.Max(0f, moveSpeed); } }
        public float AmplitudeX { get { return Mathf.Max(0f, amplitudeX); } }
        public float AmplitudeY { get { return Mathf.Max(0f, amplitudeY); } }
        public float MinimumRespawnDelay { get { return Mathf.Max(0f, minimumRespawnDelay); } }
        public float MaximumRespawnDelay { get { return Mathf.Max(minimumRespawnDelay, maximumRespawnDelay); } }
        public float Duration { get { return Mathf.Max(0f, duration); } }
        public uint Seed { get { return seed; } }

        public static TrainingScenario CreateTrackingDefault()
        {
            TrainingScenario scenario = CreateInstance<TrainingScenario>();
            scenario.displayName = "Tracking";
            scenario.mode = TrainingMode.Tracking;
            scenario.targetCount = 3;
            scenario.targetHealth = 120;
            scenario.motion = TargetMotionKind.Sine;
            scenario.moveSpeed = 1.5f;
            scenario.amplitudeX = 2.5f;
            scenario.amplitudeY = 0.4f;
            scenario.seed = 34567;
            return scenario;
        }

        public static TrainingScenario CreateDefault()
        {
            TrainingScenario scenario = CreateInstance<TrainingScenario>();
            scenario.minimumRespawnDelay = 0f;
            scenario.maximumRespawnDelay = 0f;
            return scenario;
        }
    }
}
