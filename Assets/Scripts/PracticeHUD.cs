using UnityEngine;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    public sealed class PracticeHUD : MonoBehaviour
    {
        private const string ControlsText = "RMB ADS  1/2/Q switch  R reload  F3 mode  F4/T reset  F6 summary  F8 metrics  Esc";
        [SerializeField] private bool performanceOnly;

        private PracticeSession session;
        private WeaponController weapon;
        private TargetSpawner targetSpawner;
        private Initialization settings;
        private string statsText = string.Empty;
        private string weaponText = string.Empty;
        private string trainingText = string.Empty;
        private string performanceText = string.Empty;
        private string lastWeaponName = string.Empty;
        private int lastShots = -1;
        private int lastHits = -1;
        private int lastAmmo = -1;
        private bool lastReloading;
        private bool lastSwitching;
        private bool showPerformance;
        private int frameCount;
        private int framesOver33Ms;
        private float elapsedTime;
        private float worstFrameMs;
        private float performanceTextCountdown;
        private float trainingTextCountdown;

        private void Awake()
        {
            session = GetComponent<PracticeSession>();
            weapon = GetComponent<WeaponController>();
            targetSpawner = GetComponent<TargetSpawner>();
            settings = GetComponentInParent<Initialization>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                showPerformance = !showPerformance;
                frameCount = 0;
                framesOver33Ms = 0;
                elapsedTime = 0f;
                worstFrameMs = 0f;
                performanceTextCountdown = 0f;
                if (showPerformance)
                {
                    if (weapon != null) weapon.ResetPerformanceMetrics();
                    if (targetSpawner != null) targetSpawner.ResetPerformanceMetrics();
                }
            }

            float deltaTime = Time.unscaledDeltaTime;
            frameCount++;
            elapsedTime += deltaTime;
            float frameMs = deltaTime * 1000f;
            worstFrameMs = Mathf.Max(worstFrameMs, frameMs);
            if (frameMs > 33.333f) framesOver33Ms++;

            if (session == null || weapon == null) return;
            if (lastShots != session.ShotsFired || lastHits != session.TargetsHit)
            {
                lastShots = session.ShotsFired;
                lastHits = session.TargetsHit;
                statsText = string.Format("Hits: {0}  Shots: {1}  Accuracy: {2:0.0}%",
                    lastHits, lastShots, session.Accuracy * 100f);
            }

            if (lastWeaponName != weapon.CurrentWeaponName || lastAmmo != weapon.CurrentAmmo
                || lastReloading != weapon.IsReloading || lastSwitching != weapon.IsSwitching)
            {
                lastWeaponName = weapon.CurrentWeaponName;
                lastAmmo = weapon.CurrentAmmo;
                lastReloading = weapon.IsReloading;
                lastSwitching = weapon.IsSwitching;
                weaponText = lastSwitching
                    ? lastWeaponName + "  |  Equipping..."
                    : lastWeaponName + "  |  " + (lastReloading
                        ? "Reloading..."
                        : "Ammo: " + lastAmmo + " / " + weapon.MagazineSize);
            }

            trainingTextCountdown -= deltaTime;
            if (trainingTextCountdown <= 0f)
            {
                trainingTextCountdown = 0.25f;
                trainingText = string.Format(
                    "{0}  Score: {1}  Kills: {2}  Streak: {3}\nReaction: {4:0} ms (avg {5:0})  TTK: {6:0} ms (avg {7:0})",
                    session.CurrentScenarioName, session.Score, session.Kills, session.BestStreak,
                    session.LastReactionMs, session.AverageReactionMs,
                    session.LastTtkMs, session.AverageTtkMs);
            }

            if (showPerformance)
            {
                performanceTextCountdown -= deltaTime;
                if (performanceTextCountdown <= 0f)
                {
                    performanceTextCountdown = 0.25f;
                    float averageMs = frameCount > 0 ? elapsedTime * 1000f / frameCount : 0f;
                    float respawnMs = targetSpawner != null ? targetSpawner.LastRespawnExecutionMs : 0f;
                    float worstRespawnMs = targetSpawner != null ? targetSpawner.WorstRespawnExecutionMs : 0f;
                    performanceText = string.Format(
                        "Frame avg {0:0.00} ms | worst {1:0.00} ms | >33 ms {2}\nShot {3:0.000} ms | worst {4:0.000} ms\nRespawn {5:0.000} ms | worst {6:0.000} ms",
                        averageMs, worstFrameMs, framesOver33Ms,
                        weapon.LastShotExecutionMs, weapon.WorstShotExecutionMs,
                        respawnMs, worstRespawnMs);
                }
            }
        }

        private void OnGUI()
        {
            if (session == null || weapon == null)
            {
                return;
            }

            if (session.ShowSummary)
            {
                const float width = 640f;
                const float height = 360f;
                GUI.BeginGroup(new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height));
                GUI.Box(new Rect(0f, 0f, width, height), string.Empty);
                GUI.Label(new Rect(24f, 20f, width - 48f, height - 40f), session.SummaryText);
                GUI.EndGroup();
                return;
            }

            if (settings != null && settings.IsPaused)
            {
                return;
            }

            if (!performanceOnly)
            {
                GUI.Label(new Rect(16f, 16f, 560f, 24f), statsText);
                GUI.Label(new Rect(16f, 40f, 620f, 24f), weaponText);
                GUI.Label(new Rect(16f, 64f, 1250f, 24f), ControlsText);
                GUI.Label(new Rect(16f, 88f, 760f, 48f), trainingText);
            }

            if (showPerformance)
            {
                GUI.Label(new Rect(16f, 142f, 700f, 72f), performanceText);
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            float x = Screen.width * 0.5f;
            float y = Screen.height * 0.5f;
            float size = Mathf.Lerp(8f, 5f, weapon.AimBlend);
            GUI.DrawTexture(new Rect(x - 1f, y - size, 2f, size * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - size, y - 1f, size * 2f, 2f), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
