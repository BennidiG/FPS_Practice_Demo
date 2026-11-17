using System.Collections.Generic;
using UnityEngine;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    public sealed class TargetSpawner : MonoBehaviour
    {
        private const int SpawnAttempts = 32;

        private struct RespawnRequest
        {
            public ShootTarget Target;
            public float ReadyTime;

            public RespawnRequest(ShootTarget target, float readyTime)
            {
                Target = target;
                ReadyTime = readyTime;
            }
        }

        private struct MotionState
        {
            public Vector3 Origin;
            public float Phase;
        }

        [SerializeField] private GameObject targetPrefab;
        [SerializeField] private int targetCount = 6;
        [SerializeField] private Vector2 spawnX = new Vector2(-8f, 8f);
        [SerializeField] private Vector2 spawnY = new Vector2(2f, 6f);
        [SerializeField] private Vector2 spawnDepth = new Vector2(-1f, -1f);
        [SerializeField] private float minimumSpacing = 1.5f;
        [SerializeField] private float minimumViewAngle = 4f;
        [SerializeField] private float minimumRelocationDistance = 1.5f;
        [SerializeField] private float respawnDelay;
        [SerializeField] private Transform referenceView;

        private readonly List<ShootTarget> targets = new List<ShootTarget>();
        private readonly List<MotionState> motionStates = new List<MotionState>();
        private readonly HashSet<ShootTarget> targetLookup = new HashSet<ShootTarget>();
        private readonly HashSet<ShootTarget> placementWarnings = new HashSet<ShootTarget>();
        private readonly List<RespawnRequest> pendingRespawns = new List<RespawnRequest>();
        private readonly SeededRandom fallbackRandom = new SeededRandom(12345);
        private Transform poolRoot;
        private PracticeSession session;
        private Initialization settings;
        private TrainingScenario scenario;
        private bool initialized;

        public IReadOnlyList<ShootTarget> Targets { get { return targets; } }
        public float LastRespawnExecutionMs { get; private set; }
        public float WorstRespawnExecutionMs { get; private set; }
        public int ActiveTargetCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != null && targets[i].gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private float CurrentTime { get { return session != null ? session.TimeSeconds : Time.time; } }
        private SeededRandom Random { get { return session != null ? session.Random : fallbackRandom; } }

        public void Configure(GameObject prefab, int count, Vector2 horizontal, Vector2 vertical, float z, float spacing)
        {
            ConfigureAdvanced(prefab, count, horizontal, vertical, new Vector2(z, z), spacing, null, 4f, 1.5f, 0f);
        }

        public void ConfigureAdvanced(GameObject prefab, int count, Vector2 horizontal, Vector2 vertical,
            Vector2 depth, float spacing, Transform view, float viewAngle, float relocationDistance, float delay)
        {
            targetPrefab = prefab;
            targetCount = Mathf.Clamp(count, 1, 32);
            spawnX = new Vector2(Mathf.Min(horizontal.x, horizontal.y), Mathf.Max(horizontal.x, horizontal.y));
            spawnY = new Vector2(Mathf.Min(vertical.x, vertical.y), Mathf.Max(vertical.x, vertical.y));
            spawnDepth = new Vector2(Mathf.Min(depth.x, depth.y), Mathf.Max(depth.x, depth.y));
            minimumSpacing = Mathf.Max(1.1f, spacing);
            referenceView = view;
            minimumViewAngle = Mathf.Clamp(viewAngle, 0f, 30f);
            minimumRelocationDistance = Mathf.Max(0f, relocationDistance);
            respawnDelay = Mathf.Max(0f, delay);
            session = GetComponent<PracticeSession>();
        }

        public void ConfigureScenario(TrainingScenario newScenario, PracticeSession trainingSession)
        {
            scenario = newScenario;
            session = trainingSession;
            if (scenario != null)
            {
                targetCount = scenario.TargetCount;
            }

            if (initialized)
            {
                ResetTargets();
            }
        }

        private void Awake()
        {
            session = GetComponent<PracticeSession>();
            settings = GetComponentInParent<Initialization>();
            targetCount = Mathf.Clamp(targetCount, 1, 32);
            minimumSpacing = Mathf.Max(1.1f, minimumSpacing);
            spawnDepth = new Vector2(Mathf.Min(spawnDepth.x, spawnDepth.y), Mathf.Max(spawnDepth.x, spawnDepth.y));
            if (targetPrefab == null || targetPrefab.GetComponent<ShootTarget>() == null)
            {
                Debug.LogError("TargetSpawner requires a prefab with a ShootTarget component.", this);
                enabled = false;
                return;
            }

            poolRoot = new GameObject("Target Pool").transform;
            EnsurePoolSize(targetCount);
            initialized = true;
            ResetTargets();
        }

        private void EnsurePoolSize(int count)
        {
            while (targets.Count < count)
            {
                int index = targets.Count;
                GameObject instance = Instantiate(targetPrefab, Vector3.zero, targetPrefab.transform.rotation, poolRoot);
                instance.name = "Target_" + index.ToString("00");
                ShootTarget target = instance.GetComponent<ShootTarget>();
                targets.Add(target);
                motionStates.Add(new MotionState());
                targetLookup.Add(target);
            }
        }

        private void Update()
        {
            if ((settings != null && settings.IsPaused) || (session != null && !session.IsRunning))
            {
                return;
            }

            float time = CurrentTime;
            for (int i = pendingRespawns.Count - 1; i >= 0; i--)
            {
                if (time >= pendingRespawns[i].ReadyTime)
                {
                    ShootTarget target = pendingRespawns[i].Target;
                    pendingRespawns.RemoveAt(i);
                    if (target != null)
                    {
                        PlaceTarget(target);
                    }
                }
            }

            UpdateMotion(time);
        }

        public bool TryGetTarget(Collider collider, out ShootTarget target)
        {
            target = collider != null ? collider.GetComponentInParent<ShootTarget>() : null;
            if (target == null || !targetLookup.Contains(target) || !target.gameObject.activeSelf)
            {
                target = null;
                return false;
            }

            return true;
        }

        public void RespawnTarget(ShootTarget target)
        {
            if ((settings != null && settings.IsPaused) || (session != null && !session.IsRunning))
            {
                return;
            }

            if (target == null || !targetLookup.Contains(target))
            {
                return;
            }

            float delay = respawnDelay;
            if (scenario != null)
            {
                delay = Random.Range(scenario.MinimumRespawnDelay, scenario.MaximumRespawnDelay);
            }

            if (delay > 0f)
            {
                target.gameObject.SetActive(false);
                for (int i = 0; i < pendingRespawns.Count; i++)
                {
                    if (pendingRespawns[i].Target == target)
                    {
                        return;
                    }
                }

                pendingRespawns.Add(new RespawnRequest(target, CurrentTime + delay));
            }
            else
            {
                PlaceTarget(target);
            }
        }

        public void ResetTargets()
        {
            pendingRespawns.Clear();
            EnsurePoolSize(targetCount);
            for (int i = 0; i < targets.Count; i++)
            {
                if (i < targetCount)
                {
                    PlaceTarget(targets[i]);
                }
                else
                {
                    targets[i].gameObject.SetActive(false);
                }
            }
        }

        private void PlaceTarget(ShootTarget target)
        {
            float respawnStarted = Time.realtimeSinceStartup;
            Vector3 position;
            if (!TryFindSpawnPosition(target, out position))
            {
                target.gameObject.SetActive(false);
                if (placementWarnings.Add(target))
                {
                    Debug.LogWarning("Not enough space for a target. The target was disabled instead of overlapping another target.", target);
                }

                RecordRespawnExecution(respawnStarted);
                return;
            }

            int index = targets.IndexOf(target);
            placementWarnings.Remove(target);
            target.Place(position, targetPrefab.transform.rotation);
            target.ConfigureForSpawn(scenario != null ? scenario.TargetHealth : 100f, CurrentTime, session);
            if (index >= 0)
            {
                motionStates[index] = new MotionState { Origin = position, Phase = Random.Range(0f, 10f) };
            }

            Physics.SyncTransforms();
            RecordRespawnExecution(respawnStarted);
        }

        private void UpdateMotion(float time)
        {
            if (scenario == null || scenario.Motion == TargetMotionKind.Static)
            {
                return;
            }

            bool moved = false;
            for (int i = 0; i < targetCount; i++)
            {
                ShootTarget target = targets[i];
                if (!target.gameObject.activeSelf)
                {
                    continue;
                }

                Vector3 desired = TargetTrajectory.Evaluate(
                    scenario.Motion, motionStates[i].Origin, scenario.AmplitudeX, scenario.AmplitudeY,
                    scenario.MoveSpeed, motionStates[i].Phase, time);
                if (!IsMotionPositionValid(target, desired))
                {
                    continue;
                }

                target.transform.position = desired;
                moved = true;
            }

            if (moved)
            {
                Physics.SyncTransforms();
            }
        }

        private bool IsMotionPositionValid(ShootTarget movingTarget, Vector3 desired)
        {
            if (Physics.CheckSphere(desired, 0.65f, 1 << 10, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            float requiredDistance = minimumSpacing * minimumSpacing;
            for (int i = 0; i < targets.Count; i++)
            {
                ShootTarget other = targets[i];
                if (other == movingTarget || other == null || !other.gameObject.activeSelf)
                {
                    continue;
                }

                if ((other.transform.position - desired).sqrMagnitude < requiredDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryFindSpawnPosition(ShootTarget excluded, out Vector3 position)
        {
            position = new Vector3(0f, (spawnY.x + spawnY.y) * 0.5f, (spawnDepth.x + spawnDepth.y) * 0.5f);
            float bestDistance = -1f;
            float requiredDistance = minimumSpacing * minimumSpacing;

            for (int attempt = 0; attempt < SpawnAttempts; attempt++)
            {
                Vector3 candidate = RandomCandidate();
                float nearestDistance = NearestDistanceSquared(candidate, excluded);
                if (IsCandidateValid(candidate, excluded, nearestDistance, requiredDistance))
                {
                    position = candidate;
                    return true;
                }

                if (nearestDistance > bestDistance)
                {
                    bestDistance = nearestDistance;
                    position = candidate;
                }
            }

            for (int x = 0; x <= 16; x++)
            {
                for (int y = 0; y <= 4; y++)
                {
                    for (int z = 0; z <= 3; z++)
                    {
                        Vector3 candidate = new Vector3(
                            Mathf.Lerp(spawnX.x, spawnX.y, x / 16f),
                            Mathf.Lerp(spawnY.x, spawnY.y, y / 4f),
                            Mathf.Lerp(spawnDepth.x, spawnDepth.y, z / 3f));
                        float nearestDistance = NearestDistanceSquared(candidate, excluded);
                        if (IsCandidateValid(candidate, excluded, nearestDistance, requiredDistance))
                        {
                            position = candidate;
                            return true;
                        }

                        if (nearestDistance > bestDistance)
                        {
                            bestDistance = nearestDistance;
                            position = candidate;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsCandidateValid(Vector3 candidate, ShootTarget excluded, float nearestDistance, float requiredDistance)
        {
            if (nearestDistance < requiredDistance || !HasAngularClearance(candidate, excluded))
            {
                return false;
            }

            if (excluded != null && excluded.gameObject.activeSelf && minimumRelocationDistance > 0f)
            {
                return (excluded.transform.position - candidate).sqrMagnitude
                    >= minimumRelocationDistance * minimumRelocationDistance;
            }

            return true;
        }

        private bool HasAngularClearance(Vector3 candidate, ShootTarget excluded)
        {
            if (referenceView == null || minimumViewAngle <= 0f)
            {
                return true;
            }

            Vector3 candidateDirection = (candidate - referenceView.position).normalized;
            for (int i = 0; i < targets.Count; i++)
            {
                ShootTarget other = targets[i];
                if (other == excluded || other == null || !other.gameObject.activeSelf)
                {
                    continue;
                }

                Vector3 otherDirection = (other.transform.position - referenceView.position).normalized;
                if (Vector3.Angle(candidateDirection, otherDirection) < minimumViewAngle)
                {
                    return false;
                }
            }

            return true;
        }

        private Vector3 RandomCandidate()
        {
            return new Vector3(
                Random.Range(spawnX.x, spawnX.y),
                Random.Range(spawnY.x, spawnY.y),
                Random.Range(spawnDepth.x, spawnDepth.y));
        }

        private float NearestDistanceSquared(Vector3 candidate, ShootTarget excluded)
        {
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < targets.Count; i++)
            {
                ShootTarget other = targets[i];
                if (other == excluded || other == null || !other.gameObject.activeSelf)
                {
                    continue;
                }

                float distance = (other.transform.position - candidate).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }

            return nearestDistance;
        }

        public float GetMinimumAimError(Vector3 origin, Vector3 forward)
        {
            float minimum = 180f;
            for (int i = 0; i < targetCount; i++)
            {
                ShootTarget target = targets[i];
                if (target == null || !target.gameObject.activeSelf) continue;
                float angle = Vector3.Angle(forward, (target.transform.position - origin).normalized);
                if (angle < minimum) minimum = angle;
            }

            return minimum;
        }

        public bool IsAimingAtTarget(Vector3 origin, Vector3 forward, float maximumAngle)
        {
            return GetMinimumAimError(origin, forward) <= maximumAngle;
        }

        private void RecordRespawnExecution(float started)
        {
            LastRespawnExecutionMs = (Time.realtimeSinceStartup - started) * 1000f;
            WorstRespawnExecutionMs = Mathf.Max(WorstRespawnExecutionMs, LastRespawnExecutionMs);
        }

        public void ResetPerformanceMetrics()
        {
            LastRespawnExecutionMs = 0f;
            WorstRespawnExecutionMs = 0f;
        }

        private void OnDestroy()
        {
            if (poolRoot != null)
            {
                Destroy(poolRoot.gameObject);
            }
        }
    }
}
