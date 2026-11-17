using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    public sealed class ChangeScene : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = "SampleScene";
        [SerializeField] private Button startButton;
        [SerializeField] private Button practiceModeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Initialization settings;

        private bool isLoading;

        private void Awake()
        {
            if (settings == null)
            {
                settings = GetComponent<Initialization>();
            }

            if (startButton == null)
            {
                startButton = FindButton("StartButton");
            }

            if (practiceModeButton == null)
            {
                practiceModeButton = FindButton("ChooseModeButton");
            }

            if (settingsButton == null)
            {
                settingsButton = FindButton("SetButton");
            }
        }

        private void OnEnable()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(StartPractice);
            }

            if (practiceModeButton != null)
            {
                practiceModeButton.onClick.AddListener(StartPractice);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(ToggleSettings);
            }
        }

        private void OnDisable()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartPractice);
            }

            if (practiceModeButton != null)
            {
                practiceModeButton.onClick.RemoveListener(StartPractice);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(ToggleSettings);
            }
        }

        public void GoNextScene(GameObject unused)
        {
            StartPractice();
        }

        public void StartPractice()
        {
            if (isLoading)
            {
                return;
            }

            string scenePath = "Assets/Scenes/" + nextSceneName + ".unity";
            if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
            {
                Debug.LogError("Scene is not enabled in Build Settings: " + scenePath, this);
                return;
            }

            isLoading = true;
            if (startButton != null)
            {
                startButton.interactable = false;
            }

            if (settings != null)
            {
                settings.SaveSettings();
            }

            SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
        }

        public void ToggleSettings()
        {
            if (settings != null)
            {
                settings.ToggleSettings();
            }
        }

        private static Button FindButton(string objectName)
        {
            GameObject buttonObject = GameObject.Find(objectName);
            return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
        }
    }
}
