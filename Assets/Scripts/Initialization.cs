using System;
using UnityEngine;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class Initialization : MonoBehaviour
    {
        private const string MouseSensitivityKey = "FpsPractice.MouseCmPer360";
        private const string CountsPerCentimeterKey = "FpsPractice.CountsPerCentimeter";
        private const string MovementSpeedKey = "FpsPractice.MovementSpeed";

        [SerializeField] private bool captureCursor = true;
        [SerializeField] private bool showSettings;
        [SerializeField] private int targetFrameRate = 120;

        private bool initialized;

        public float MouseCmPer360 { get; private set; }
        public float CountsPerCentimeter { get; private set; }
        public float DegreesPerCount { get { return AimSensitivity.DegreesPerCount(MouseCmPer360, CountsPerCentimeter); } }
        public float MovementSpeed { get; private set; }
        public bool IsPaused { get; private set; }
        public bool SettingsVisible { get { return showSettings; } }
        public bool CapturedCursorThisFrame { get; private set; }
        public bool CanUseGameplayInput
        {
            get
            {
                return captureCursor && !IsPaused && Cursor.lockState == CursorLockMode.Locked && !CapturedCursorThisFrame;
            }
        }

        public event Action<bool> PauseChanged;

        private void Awake()
        {
            MouseCmPer360 = AimSensitivity.SanitizeCmPer360(
                PlayerPrefs.GetFloat(MouseSensitivityKey, AimSensitivity.DefaultCmPer360));
            MovementSpeed = GameSettings.SanitizeMovementSpeed(
                PlayerPrefs.GetFloat(MovementSpeedKey, GameSettings.DefaultMovementSpeed));
            CountsPerCentimeter = Mathf.Max(1f, PlayerPrefs.GetFloat(CountsPerCentimeterKey, AimSensitivity.CountsPerCentimeter));
            Application.targetFrameRate = Mathf.Max(30, targetFrameRate);
            initialized = true;

            if (captureCursor)
            {
                SetPaused(false);
            }
            else
            {
                IsPaused = false;
                ReleaseCursor();
            }
        }

        private void Update()
        {
            CapturedCursorThisFrame = false;
            if (!captureCursor)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (showSettings)
                {
                    Resume();
                }
                else
                {
                    PauseAndShowSettings();
                }
            }

            if (!IsPaused && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            {
                CaptureCursor();
                CapturedCursorThisFrame = true;
            }
        }

        public void ToggleSettings()
        {
            if (showSettings)
            {
                Resume();
            }
            else
            {
                PauseAndShowSettings();
            }
        }

        public void Resume()
        {
            showSettings = false;
            SetPaused(false);
            SaveSettings();
        }

        public void SaveSettings()
        {
            if (!initialized)
            {
                return;
            }

            PlayerPrefs.SetFloat(MouseSensitivityKey, MouseCmPer360);
            PlayerPrefs.SetFloat(CountsPerCentimeterKey, CountsPerCentimeter);
            PlayerPrefs.SetFloat(MovementSpeedKey, MovementSpeed);
            PlayerPrefs.Save();
        }

        private void PauseAndShowSettings()
        {
            showSettings = true;
            SetPaused(captureCursor);
            SaveSettings();
        }

        public void SetPaused(bool paused)
        {
            if (!captureCursor)
            {
                IsPaused = false;
                ReleaseCursor();
                return;
            }

            bool changed = IsPaused != paused;
            IsPaused = paused;
            if (paused)
            {
                ReleaseCursor();
            }
            else
            {
                CaptureCursor();
            }

            if (changed && PauseChanged != null)
            {
                PauseChanged(paused);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && captureCursor)
            {
                PauseAndShowSettings();
            }
        }

        private void OnDisable()
        {
            SaveSettings();
            if (captureCursor)
            {
                ReleaseCursor();
            }
        }

        private void OnGUI()
        {
            if (!showSettings)
            {
                return;
            }

            const float width = 420f;
            const float height = 240f;
            GUI.BeginGroup(new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height));
            GUI.Box(new Rect(0f, 0f, width, height), "Practice Settings");

            GUI.Label(new Rect(24f, 42f, 240f, 24f), "cm per 360 degrees");
            MouseCmPer360 = GUI.HorizontalSlider(
                new Rect(210f, 48f, 150f, 24f), MouseCmPer360,
                AimSensitivity.MinimumCmPer360, AimSensitivity.MaximumCmPer360);
            GUI.Label(new Rect(24f, 68f, 380f, 24f), MouseCmPer360.ToString("0.0") + " cm | " + CountsPerCentimeter.ToString("0") + " counts/cm");

            GUI.Label(new Rect(24f, 100f, 240f, 24f), "Movement speed");
            MovementSpeed = GUI.HorizontalSlider(
                new Rect(210f, 106f, 150f, 24f), MovementSpeed,
                GameSettings.MinimumMovementSpeed, GameSettings.MaximumMovementSpeed);
            GUI.Label(new Rect(24f, 126f, 360f, 24f), MovementSpeed.ToString("0.0"));

            MouseCmPer360 = AimSensitivity.SanitizeCmPer360(MouseCmPer360);
            MovementSpeed = GameSettings.SanitizeMovementSpeed(MovementSpeed);

            if (GUI.Button(new Rect(24f, 174f, 170f, 40f), "Reset defaults"))
            {
                MouseCmPer360 = AimSensitivity.DefaultCmPer360;
                MovementSpeed = GameSettings.DefaultMovementSpeed;
            }

            if (GUI.Button(new Rect(216f, 174f, 180f, 40f), captureCursor ? "Resume" : "Close"))
            {
                Resume();
            }

            GUI.EndGroup();
        }

        private static void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
