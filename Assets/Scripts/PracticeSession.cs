using System;
using System.IO;
using UnityEngine;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-80)]
    public sealed class PracticeSession : MonoBehaviour
    {
        [SerializeField] private TrainingScenario[] scenarios;

        private readonly TrainingMetrics metrics = new TrainingMetrics();
        private TargetSpawner targetSpawner;
        private WeaponController weapon;
        private Initialization settings;
        private SeededRandom random = new SeededRandom(12345);
        private readonly AimPathTracker aimPath = new AimPathTracker();
        private float trackingTime;
        private float onTargetTime;
        private TrainingRecord activeRecord;
        private bool persistenceEnabled = true;
        private int currentScenarioIndex;

        public float TimeSeconds { get; private set; }
        public bool IsRunning { get; private set; }
        public bool ShowSummary { get; private set; }
        private string summaryText = string.Empty;
        public string SummaryText { get { return summaryText; } private set { summaryText = value; } }
        public int CurrentScenarioIndex { get { return currentScenarioIndex; } }
        public string CurrentScenarioName { get { return CurrentScenario != null ? CurrentScenario.DisplayName : "Training"; } }
        public int ScenarioCount { get { return scenarios != null ? scenarios.Length : 0; } }
        public SeededRandom Random { get { return random; } }
        public int ShotsFired { get { return metrics.Shots; } }
        public int TargetsHit { get { return metrics.Hits; } }
        public int Kills { get { return metrics.Kills; } }
        public int Score { get { return metrics.Score; } }
        public int BestStreak { get { return metrics.BestStreak; } }
        public float Accuracy { get { return metrics.Accuracy; } }
        public float LastReactionMs { get { return metrics.LastReactionMs; } }
        public float LastTtkMs { get { return metrics.LastTtkMs; } }
        public float AverageReactionMs { get { return metrics.AverageReactionMs; } }
        public float AverageTtkMs { get { return metrics.AverageTtkMs; } }
        public float MedianReactionMs { get { return metrics.MedianReactionMs; } }
        public float P95ReactionMs { get { return metrics.P95ReactionMs; } }
        public float MedianTtkMs { get { return metrics.MedianTtkMs; } }
        public float P95TtkMs { get { return metrics.P95TtkMs; } }
        public float MousePathEfficiency { get { return metrics.MousePathEfficiency; } }
        public float TrackingTimeRatio { get { return trackingTime > 0f ? onTargetTime / trackingTime : 0f; } }
        private string lastExportPath = string.Empty;
        public string LastExportPath { get { return lastExportPath; } private set { lastExportPath = value; } }
        public bool PersistenceEnabled { get { return persistenceEnabled; } set { persistenceEnabled = value; } }
        public string AdvancedSummaryText
        {
            get
            {
                return metrics.BuildSummaryText()
                    + "Reaction median/P95: " + MedianReactionMs.ToString("0") + " / " + P95ReactionMs.ToString("0") + " ms\n"
                    + "TTK median/P95: " + MedianTtkMs.ToString("0") + " / " + P95TtkMs.ToString("0") + " ms\n"
                    + "Mouse path efficiency: " + (MousePathEfficiency * 100f).ToString("0.0") + "%\n"
                    + "Tracking on-target: " + (TrackingTimeRatio * 100f).ToString("0.0") + "%";
            }
        }
        public string HistoryText
        {
            get
            {
                string text = TrainingHistoryStore.BuildHistoryText(5);
                TrainingRecord best = TrainingHistoryStore.GetPersonalBest(CurrentScenarioName);
                return best == null ? text : "PERSONAL BEST  Score " + best.score + "  Accuracy " + (best.accuracy * 100f).ToString("0.0") + "%\n\n" + text;
            }
        }
        public TrainingScenario CurrentScenario
        {
            get { return scenarios != null && currentScenarioIndex >= 0 && currentScenarioIndex < scenarios.Length ? scenarios[currentScenarioIndex] : null; }
        }

        private void Awake()
        {
            targetSpawner = GetComponent<TargetSpawner>();
            weapon = GetComponent<WeaponController>();
            settings = GetComponentInParent<Initialization>();
            if (scenarios == null || scenarios.Length == 0)
            {
                scenarios = new[] { TrainingScenario.CreateDefault() };
            }

            LoadHistory();
        }

        private void Start()
        {
            StartScenario(0);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                ToggleSummary();
            }

            if (ShowSummary)
            {
                if (Input.GetKeyDown(KeyCode.F3))
                {
                    NextScenario();
                }
                else if (Input.GetKeyDown(KeyCode.F4))
                {
                    ResetSession();
                }

                return;
            }

            bool paused = settings != null && settings.IsPaused;
            if (!paused)
            {
                if (IsRunning)
                {
                    TimeSeconds += Time.deltaTime;
                    if (CurrentScenario != null && CurrentScenario.Mode == TrainingMode.Tracking && targetSpawner != null)
                    {
                        trackingTime += Time.deltaTime;
                        if (targetSpawner.IsAimingAtTarget(transform.position, transform.forward, 3f))
                        {
                            onTargetTime += Time.deltaTime;
                        }
                    }

                    if (CurrentScenario != null && CurrentScenario.Duration > 0f && TimeSeconds >= CurrentScenario.Duration)
                    {
                        EndTraining();
                    }
                }

                if (Input.GetKeyDown(KeyCode.F3))
                {
                    NextScenario();
                }

                if (Input.GetKeyDown(KeyCode.F4) || Input.GetKeyDown(KeyCode.T))
                {
                    ResetSession();
                }
            }
        }

        public void ConfigureScenarios(TrainingScenario[] loadout)
        {
            scenarios = loadout;
        }

        public void RecordAimSample(float absoluteDelta, float idealDelta)
        {
            aimPath.AddSample(absoluteDelta, idealDelta);
            metrics.SetMousePathEfficiency(aimPath.Efficiency);
        }

        public TrainingScenario GetScenarioAt(int index)
        {
            return scenarios != null && index >= 0 && index < scenarios.Length ? scenarios[index] : null;
        }

        public void NextScenario()
        {
            if (scenarios != null && scenarios.Length > 0)
            {
                StartScenario((currentScenarioIndex + 1) % scenarios.Length);
            }
        }

        public void StartScenario(int index)
        {
            if (scenarios == null || scenarios.Length == 0)
            {
                return;
            }

            currentScenarioIndex = Mathf.Clamp(index, 0, scenarios.Length - 1);
            if (CurrentScenario == null)
            {
                scenarios[currentScenarioIndex] = TrainingScenario.CreateDefault();
            }

            random = new SeededRandom(CurrentScenario.Seed);
            aimPath.Reset();
            trackingTime = 0f;
            onTargetTime = 0f;
            activeRecord = null;
            metrics.Reset();
            TimeSeconds = 0f;
            SummaryText = string.Empty;
            ShowSummary = false;
            IsRunning = true;
            if (settings != null)
            {
                settings.SetPaused(false);
            }

            if (weapon != null)
            {
                weapon.ResetWeapon();
            }

            if (targetSpawner != null)
            {
                targetSpawner.ConfigureScenario(CurrentScenario, this);
            }
        }

        public void ResetSession()
        {
            StartScenario(currentScenarioIndex);
        }

        public void RegisterShot(bool hit)
        {
            if (hit)
            {
                metrics.RecordHit(new TargetHitResult(TargetHitZone.Outer, 0, 0f, false, false), 0f);
            }
            else
            {
                metrics.RecordMiss();
            }
        }

        public void RegisterShot(TargetHitResult result, float secondsSinceSpawn)
        {
            metrics.RecordHit(result, secondsSinceSpawn);
        }

        public void EndTraining()
        {
            IsRunning = false;
            RecordTraining();
            SummaryText = CurrentScenarioName + "\n" + AdvancedSummaryText;
            ShowSummary = true;
            if (settings != null)
            {
                settings.SetPaused(true);
            }
        }

        public void ToggleSummary()
        {
            if (ShowSummary)
            {
                ShowSummary = false;
                if (settings != null)
                {
                    settings.SetPaused(false);
                }

                return;
            }

            if (metrics.Shots == 0)
            {
                return;
            }

            SummaryText = CurrentScenarioName + "\n" + AdvancedSummaryText;
            ShowSummary = true;
            RecordTraining();
            if (settings != null)
            {
                settings.SetPaused(true);
            }
        }
        private void RecordTraining()
        {
            if (metrics.Shots == 0)
            {
                return;
            }

            TrainingRecord latest = metrics.BuildRecord(CurrentScenarioName,
                weapon != null ? weapon.CurrentWeaponName : "Unknown", CurrentScenario.Seed);
            latest.utcTimestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            latest.trackingTimeRatio = TrackingTimeRatio;
            if (activeRecord == null)
            {
                activeRecord = latest;
                TrainingHistoryStore.Add(activeRecord);
            }
            else
            {
                UpdateRecord(activeRecord, latest);
            }

            SaveHistoryFiles();
        }

        private static void UpdateRecord(TrainingRecord target, TrainingRecord source)
        {
            target.scenario = source.scenario;
            target.weapon = source.weapon;
            target.utcTimestamp = source.utcTimestamp;
            target.seed = source.seed;
            target.shots = source.shots;
            target.hits = source.hits;
            target.kills = source.kills;
            target.score = source.score;
            target.bestStreak = source.bestStreak;
            target.accuracy = source.accuracy;
            target.reactionMedian = source.reactionMedian;
            target.reactionP95 = source.reactionP95;
            target.ttkMedian = source.ttkMedian;
            target.ttkP95 = source.ttkP95;
            target.mousePathEfficiency = source.mousePathEfficiency;
            target.trackingTimeRatio = source.trackingTimeRatio;
        }

        public string ExportTrainingData()
        {
            RecordTraining();
            SaveHistoryFiles();
            return LastExportPath;
        }

        private string ExportDirectory
        {
            get { return Path.Combine(Application.persistentDataPath, "FpsPractice"); }
        }

        private void SaveHistoryFiles()
        {
            if (!persistenceEnabled)
            {
                LastExportPath = string.Empty;
                return;
            }

            try
            {
                Directory.CreateDirectory(ExportDirectory);
                File.WriteAllText(Path.Combine(ExportDirectory, "training-history.json"), TrainingHistoryStore.BuildJson());
                File.WriteAllText(Path.Combine(ExportDirectory, "training-history.csv"), TrainingHistoryStore.BuildCsv());
                LastExportPath = ExportDirectory;
            }
            catch (Exception exception)
            {
                LastExportPath = string.Empty;
                Debug.LogWarning("Could not save training history: " + exception.Message, this);
            }
        }

        private void LoadHistory()
        {
            try
            {
                string path = Path.Combine(ExportDirectory, "training-history.json");
                if (File.Exists(path))
                {
                    TrainingHistoryStore.LoadJson(File.ReadAllText(path));
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not load training history: " + exception.Message, this);
            }
        }
    }
}
