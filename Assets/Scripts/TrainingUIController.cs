using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FpsPractice
{
    public enum TrainingUIPage
    {
        Live,
        History,
        Help,
        Summary
    }

    [DisallowMultipleComponent]
    public sealed class TrainingUIController : MonoBehaviour
    {
        private const float TextRefreshInterval = 0.2f;

        private static readonly Color PanelColor = new Color(0.025f, 0.04f, 0.065f, 0.88f);
        private static readonly Color ViewportColor = new Color(0.015f, 0.025f, 0.045f, 0.72f);
        private static readonly Color AccentColor = new Color(0.2f, 0.85f, 0.9f, 1f);
        private static readonly Color WarmAccentColor = new Color(1f, 0.68f, 0.28f, 1f);
        private static readonly Color PrimaryTextColor = new Color(0.93f, 0.97f, 1f, 1f);
        private static readonly Color SecondaryTextColor = new Color(0.61f, 0.72f, 0.79f, 1f);
        private static readonly Color ButtonColor = new Color(0.07f, 0.13f, 0.19f, 0.96f);

        public int LiveTextCount;
        public int PanelCount;
        public int ButtonCount;

        private readonly List<Text> textElements = new List<Text>();

        private PracticeSession session;
        private Font uiFont;
        private GameObject canvasRoot;
        private GameObject livePage;
        private GameObject summaryPage;
        private GameObject historyPage;
        private GameObject helpPage;
        private EventSystem ownedEventSystem;

        private Text liveScenarioText;
        private Text liveScenarioDetailText;
        private Text liveStatsText;
        private Text summaryBodyText;
        private Text historyBodyText;
        private Text historyPathText;

        private TrainingUIPage currentPage;
        private float refreshCountdown;
        private bool initialized;
        private bool lastSessionSummary;
        private string lastExportResult = string.Empty;
        private string exportMessage = string.Empty;

        // These members are cached once so the controller also works with PracticeSession
        // versions that predate the advanced summary, history, and export API.
        private PropertyInfo advancedSummaryProperty;
        private PropertyInfo historyTextProperty;
        private PropertyInfo lastExportPathProperty;
        private MethodInfo exportTrainingDataMethod;
        private MethodInfo getScenarioAtMethod;

        private void Awake()
        {
            if (initialized)
            {
                return;
            }

            session = GetComponent<PracticeSession>();
            CacheOptionalSessionApi();
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            BuildCanvas();
            BuildLivePage();
            BuildSummaryPage();
            BuildHistoryPage();
            BuildHelpPage();
            BuildToolbar();
            EnsureEventSystem();

            initialized = true;
            lastSessionSummary = session != null && session.ShowSummary;
            ShowPage(lastSessionSummary ? TrainingUIPage.Summary : TrainingUIPage.Live);
            RefreshVisiblePage();
            refreshCountdown = TextRefreshInterval;
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            SynchronizeSummaryPage();

            bool sessionSummaryVisible = session != null && session.ShowSummary;
            if (!sessionSummaryVisible && Input.GetKeyDown(KeyCode.F7))
            {
                CycleStandardPage();
            }

            refreshCountdown -= Time.unscaledDeltaTime;
            if (refreshCountdown <= 0f)
            {
                RefreshVisiblePage();
                refreshCountdown = TextRefreshInterval;
            }
        }

        public void ConfigureFonts(Font font)
        {
            uiFont = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            for (int i = 0; i < textElements.Count; i++)
            {
                if (textElements[i] != null)
                {
                    textElements[i].font = uiFont;
                }
            }
        }

        public void ShowPage(TrainingUIPage page)
        {
            currentPage = page;
            if (!initialized)
            {
                return;
            }

            livePage.SetActive(page == TrainingUIPage.Live);
            historyPage.SetActive(page == TrainingUIPage.History);
            helpPage.SetActive(page == TrainingUIPage.Help);
            summaryPage.SetActive(page == TrainingUIPage.Summary);

            // A page becomes current immediately; its text is refreshed by the
            // regular throttled refresh on the next Update.
            refreshCountdown = 0f;
        }

        private void OnDestroy()
        {
            if (canvasRoot != null)
            {
                canvasRoot.SetActive(false);
                Destroy(canvasRoot);
                canvasRoot = null;
            }

            if (ownedEventSystem != null)
            {
                Destroy(ownedEventSystem.gameObject);
                ownedEventSystem = null;
            }

            textElements.Clear();
            session = null;
            initialized = false;
        }

        private void CacheOptionalSessionApi()
        {
            if (session == null)
            {
                return;
            }

            Type sessionType = session.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            advancedSummaryProperty = sessionType.GetProperty("AdvancedSummaryText", flags);
            historyTextProperty = sessionType.GetProperty("HistoryText", flags);
            lastExportPathProperty = sessionType.GetProperty("LastExportPath", flags);
            exportTrainingDataMethod = sessionType.GetMethod(
                "ExportTrainingData", flags, null, Type.EmptyTypes, null);
            getScenarioAtMethod = sessionType.GetMethod(
                "GetScenarioAt", flags, null, new[] { typeof(int) }, null);
        }

        private void BuildCanvas()
        {
            canvasRoot = CreateRectObject("TrainingUI_Canvas", null);
            RectTransform canvasRect = canvasRoot.GetComponent<RectTransform>();
            Stretch(canvasRect);

            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            canvasRoot.AddComponent<GraphicRaycaster>();
        }

        private void BuildLivePage()
        {
            livePage = CreatePanel(
                "LivePage", canvasRoot.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-32f, -32f), new Vector2(580f, 260f), PanelColor);
            livePage.SetActive(false);

            Image accent = CreateImage("LiveAccent", livePage.transform, AccentColor, false);
            SetRect(accent.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -16f), new Vector2(4f, 54f));

            CreateText("LiveTitle", livePage.transform, "LIVE  /  TRAINING", 16,
                FontStyle.Bold, AccentColor, TextAnchor.UpperLeft,
                new Vector2(24f, -17f), new Vector2(530f, 26f), true);
            liveScenarioText = CreateText("LiveScenario", livePage.transform, "Training", 29,
                FontStyle.Bold, PrimaryTextColor, TextAnchor.UpperLeft,
                new Vector2(24f, -50f), new Vector2(530f, 40f), true);
            liveScenarioDetailText = CreateText("LiveScenarioDetail", livePage.transform,
                "REAL-TIME SESSION DATA", 15, FontStyle.Normal, SecondaryTextColor,
                TextAnchor.UpperLeft, new Vector2(24f, -94f), new Vector2(530f, 26f), true);
            liveStatsText = CreateText("LiveStats", livePage.transform, string.Empty, 18,
                FontStyle.Normal, PrimaryTextColor, TextAnchor.UpperLeft,
                new Vector2(24f, -132f), new Vector2(530f, 108f), true);
            liveStatsText.lineSpacing = 1.12f;
        }

        private void BuildSummaryPage()
        {
            summaryPage = CreatePanel(
                "SummaryPage", canvasRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(980f, 680f), PanelColor);
            summaryPage.SetActive(false);

            CreateText("SummaryTitle", summaryPage.transform, "SESSION SUMMARY", 30,
                FontStyle.Bold, PrimaryTextColor, TextAnchor.UpperLeft,
                new Vector2(30f, -23f), new Vector2(920f, 44f), false);
            CreateText("SummarySubtitle", summaryPage.transform,
                "F6 OR SUMMARY TOGGLES THE SESSION  /  CLOSE RETURNS TO LIVE", 14,
                FontStyle.Normal, AccentColor, TextAnchor.UpperLeft,
                new Vector2(32f, -72f), new Vector2(916f, 26f), false);
            summaryBodyText = CreateScrollableText("SummaryScroll", summaryPage.transform,
                new Vector2(28f, -112f), new Vector2(924f, 540f), 21);
            summaryBodyText.lineSpacing = 1.15f;
        }

        private void BuildHistoryPage()
        {
            historyPage = CreatePanel(
                "HistoryPage", canvasRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1120f, 720f), PanelColor);
            historyPage.SetActive(false);

            CreateText("HistoryTitle", historyPage.transform, "TRAINING HISTORY", 30,
                FontStyle.Bold, PrimaryTextColor, TextAnchor.UpperLeft,
                new Vector2(30f, -22f), new Vector2(1060f, 44f), false);
            historyPathText = CreateText("HistoryExportPath", historyPage.transform,
                string.Empty, 16, FontStyle.Normal, WarmAccentColor, TextAnchor.UpperLeft,
                new Vector2(32f, -75f), new Vector2(1056f, 56f), false);
            historyBodyText = CreateScrollableText("HistoryScroll", historyPage.transform,
                new Vector2(28f, -142f), new Vector2(1064f, 550f), 18);
            historyBodyText.lineSpacing = 1.15f;
        }

        private void BuildHelpPage()
        {
            helpPage = CreatePanel(
                "HelpPage", canvasRoot.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(850f, 660f), PanelColor);
            helpPage.SetActive(false);

            CreateText("HelpTitle", helpPage.transform, "TRAINING CONTROLS", 30,
                FontStyle.Bold, PrimaryTextColor, TextAnchor.UpperLeft,
                new Vector2(30f, -23f), new Vector2(790f, 44f), false);
            CreateText("HelpSubtitle", helpPage.transform, "QUICK REFERENCE", 14,
                FontStyle.Bold, AccentColor, TextAnchor.UpperLeft,
                new Vector2(32f, -72f), new Vector2(786f, 26f), false);

            const string help =
                "F7   Cycle Live / History / Help\n" +
                "F6   Toggle summary (handled by PracticeSession)\n\n" +
                "START      Start the first training scenario\n" +
                "NEXT       Advance to the next scenario\n" +
                "RESET      Restart the current scenario\n" +
                "SUMMARY    Toggle the current session summary\n" +
                "EXPORT     Save training data through PracticeSession\n" +
                "CLOSE      Close this page and return to Live\n\n" +
                "Live metrics refresh five times per second and stay\n" +
                "clear of the center crosshair. History shows prior\n" +
                "training records and the latest export path.";
            Text body = CreateText("HelpBody", helpPage.transform, help, 20,
                FontStyle.Normal, PrimaryTextColor, TextAnchor.UpperLeft,
                new Vector2(32f, -122f), new Vector2(786f, 496f), false);
            body.lineSpacing = 1.12f;
        }

        private void BuildToolbar()
        {
            GameObject toolbar = CreatePanel(
                "Toolbar", canvasRoot.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 26f), new Vector2(962f, 72f),
                new Color(0.025f, 0.04f, 0.065f, 0.94f));

            float x = 18f;
            CreateButton("StartButton", toolbar.transform, "START", x, ButtonColor, OnStartClicked);
            x += 156f;
            CreateButton("NextButton", toolbar.transform, "NEXT", x, ButtonColor, OnNextClicked);
            x += 156f;
            CreateButton("ResetButton", toolbar.transform, "RESET", x, ButtonColor, OnResetClicked);
            x += 156f;
            CreateButton("SummaryButton", toolbar.transform, "SUMMARY", x,
                new Color(0.04f, 0.29f, 0.33f, 0.98f), OnSummaryClicked);
            x += 156f;
            CreateButton("ExportButton", toolbar.transform, "EXPORT", x,
                new Color(0.36f, 0.23f, 0.09f, 0.98f), OnExportClicked);
            x += 156f;
            CreateButton("CloseButton", toolbar.transform, "CLOSE", x, ButtonColor, OnCloseClicked);
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventObject = new GameObject(
                "TrainingUI_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventObject.hideFlags = HideFlags.DontSave;
            ownedEventSystem = eventObject.GetComponent<EventSystem>();
        }

        private void SynchronizeSummaryPage()
        {
            bool showSummary = session != null && session.ShowSummary;
            if (showSummary)
            {
                if (currentPage != TrainingUIPage.Summary)
                {
                    ShowPage(TrainingUIPage.Summary);
                }
            }
            else if (lastSessionSummary && currentPage == TrainingUIPage.Summary)
            {
                ShowPage(TrainingUIPage.Live);
            }

            lastSessionSummary = showSummary;
        }

        private void CycleStandardPage()
        {
            if (currentPage == TrainingUIPage.Live)
            {
                ShowPage(TrainingUIPage.History);
            }
            else if (currentPage == TrainingUIPage.History)
            {
                ShowPage(TrainingUIPage.Help);
            }
            else
            {
                ShowPage(TrainingUIPage.Live);
            }
        }

        private void RefreshVisiblePage()
        {
            switch (currentPage)
            {
                case TrainingUIPage.Live:
                    RefreshLive();
                    break;
                case TrainingUIPage.History:
                    RefreshHistory();
                    break;
                case TrainingUIPage.Summary:
                    RefreshSummary();
                    break;
            }
        }

        private void RefreshLive()
        {
            if (session == null)
            {
                liveScenarioText.text = "SESSION OFFLINE";
                liveScenarioDetailText.text = "PracticeSession component is required";
                liveStatsText.text = "No training data available.";
                return;
            }

            liveScenarioText.text = string.IsNullOrEmpty(session.CurrentScenarioName)
                ? "Training" : session.CurrentScenarioName;

            TrainingScenario scenario = GetScenarioAt(session.CurrentScenarioIndex);
            if (scenario == null)
            {
                scenario = session.CurrentScenario;
            }

            liveScenarioDetailText.text = scenario != null
                ? string.Format("MODE  {0}    /    SEED  {1}", scenario.Mode, scenario.Seed)
                : "REAL-TIME SESSION DATA";
            liveStatsText.text = string.Format(
                "SCORE  {0}     ACCURACY  {1:0.0}%\n" +
                "HITS  {2} / {3}     KILLS  {4}     STREAK  {5}\n" +
                "REACTION MED/P95  {6:0}/{7:0} ms   TTK MED/P95  {8:0}/{9:0} ms\n" +
                "MOUSE PATH  {10:0.0}%     ON-TARGET  {11:0.0}%",
                session.Score, session.Accuracy * 100f,
                session.TargetsHit, session.ShotsFired, session.Kills, session.BestStreak,
                session.MedianReactionMs, session.P95ReactionMs,
                session.MedianTtkMs, session.P95TtkMs,
                session.MousePathEfficiency * 100f, session.TrackingTimeRatio * 100f);
        }

        private void RefreshSummary()
        {
            if (session == null)
            {
                summaryBodyText.text = "No PracticeSession is available.";
                return;
            }

            string summary = session.SummaryText;
            string advancedSummary = ReadStringProperty(advancedSummaryProperty);
            if (!string.IsNullOrEmpty(advancedSummary) &&
                (string.IsNullOrEmpty(summary) || summary.IndexOf(advancedSummary, StringComparison.Ordinal) < 0))
            {
                summary = string.IsNullOrEmpty(summary)
                    ? advancedSummary
                    : summary + "\n" + advancedSummary;
            }

            summaryBodyText.text = string.IsNullOrEmpty(summary)
                ? "No summary is available yet.\nFire at a target, then press F6."
                : summary;
        }

        private void RefreshHistory()
        {
            string history = ReadStringProperty(historyTextProperty);
            historyBodyText.text = string.IsNullOrEmpty(history)
                ? "No training history is available yet.\nComplete a scenario to create a record."
                : history;

            string path = ReadStringProperty(lastExportPathProperty);
            if (string.IsNullOrEmpty(path))
            {
                path = lastExportResult;
            }

            if (!string.IsNullOrEmpty(path))
            {
                historyPathText.text = "LAST EXPORT\n" + path;
            }
            else if (!string.IsNullOrEmpty(exportMessage))
            {
                historyPathText.text = "EXPORT STATUS\n" + exportMessage;
            }
            else
            {
                historyPathText.text = "LAST EXPORT\nNo export created yet.";
            }
        }

        private string ReadStringProperty(PropertyInfo property)
        {
            if (session == null || property == null)
            {
                return string.Empty;
            }

            try
            {
                return property.GetValue(session, null) as string ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private TrainingScenario GetScenarioAt(int index)
        {
            if (session == null || getScenarioAtMethod == null)
            {
                return null;
            }

            try
            {
                return getScenarioAtMethod.Invoke(session, new object[] { index }) as TrainingScenario;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void OnStartClicked()
        {
            if (session == null)
            {
                return;
            }

            session.StartScenario(0);
            AfterSessionRestart();
        }

        private void OnNextClicked()
        {
            if (session == null)
            {
                return;
            }

            session.NextScenario();
            AfterSessionRestart();
        }

        private void OnResetClicked()
        {
            if (session == null)
            {
                return;
            }

            session.ResetSession();
            AfterSessionRestart();
        }

        private void AfterSessionRestart()
        {
            lastSessionSummary = session.ShowSummary;
            ShowPage(TrainingUIPage.Live);
        }

        private void OnSummaryClicked()
        {
            if (session == null)
            {
                ShowPage(TrainingUIPage.Summary);
                return;
            }

            session.ToggleSummary();
            lastSessionSummary = session.ShowSummary;
            ShowPage(session.ShowSummary ? TrainingUIPage.Summary : TrainingUIPage.Live);
        }

        private void OnExportClicked()
        {
            if (session == null || exportTrainingDataMethod == null)
            {
                exportMessage = "Export is unavailable in this PracticeSession version.";
            }
            else
            {
                try
                {
                    string path = exportTrainingDataMethod.Invoke(session, null) as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        lastExportResult = path;
                        exportMessage = "Export completed.";
                    }
                    else
                    {
                        exportMessage = "Export failed or no valid output path.";
                    }
                }
                catch (TargetInvocationException exception)
                {
                    exportMessage = "Export failed: " +
                        (exception.InnerException != null ? exception.InnerException.Message : exception.Message);
                }
                catch (Exception exception)
                {
                    exportMessage = "Export failed: " + exception.Message;
                }
            }

            if (currentPage == TrainingUIPage.History)
            {
                refreshCountdown = 0f;
            }
        }

        private void OnCloseClicked()
        {
            if (session != null && session.ShowSummary)
            {
                session.ToggleSummary();
            }

            lastSessionSummary = false;
            ShowPage(TrainingUIPage.Live);
        }

        private GameObject CreatePanel(string objectName, Transform parent, Vector2 anchor,
            Vector2 pivot, Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = CreateRectObject(objectName, parent);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            SetRect(image.rectTransform, anchor, pivot, position, size);

            Shadow shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.48f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;

            PanelCount++;
            return panel;
        }

        private Image CreateImage(string objectName, Transform parent, Color color, bool raycastTarget)
        {
            GameObject imageObject = CreateRectObject(objectName, parent);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private Text CreateText(string objectName, Transform parent, string content, int fontSize,
            FontStyle fontStyle, Color color, TextAnchor alignment, Vector2 position,
            Vector2 size, bool isLiveText)
        {
            GameObject textObject = CreateRectObject(objectName, parent);
            textObject.AddComponent<CanvasRenderer>();
            Text text = textObject.AddComponent<Text>();
            text.font = uiFont;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.alignByGeometry = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.raycastTarget = false;
            SetRect(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);

            textElements.Add(text);
            if (isLiveText)
            {
                LiveTextCount++;
            }

            return text;
        }

        private Text CreateScrollableText(string objectName, Transform parent,
            Vector2 position, Vector2 size, int fontSize)
        {
            GameObject viewportObject = CreateRectObject(objectName + "_Viewport", parent);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            SetRect(viewport, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);

            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = ViewportColor;
            viewportImage.raycastTarget = true;
            Mask mask = viewportObject.AddComponent<Mask>();
            mask.showGraphic = true;

            ScrollRect scrollRect = viewportObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.1f;

            GameObject contentObject = CreateRectObject(objectName + "_Content", viewportObject.transform);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 16);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 0f;

            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text text = CreateText(objectName + "_Text", contentObject.transform, string.Empty,
                fontSize, FontStyle.Normal, PrimaryTextColor, TextAnchor.UpperLeft,
                Vector2.zero, new Vector2(100f, 100f), false);
            scrollRect.content = content;
            return text;
        }

        private Button CreateButton(string objectName, Transform parent, string label, float x,
            Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = CreateRectObject(objectName, parent);
            buttonObject.AddComponent<CanvasRenderer>();
            Image image = buttonObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            SetRect(image.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x, -14f), new Vector2(146f, 44f));

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.72f, 0.9f, 0.94f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.onClick.AddListener(action);

            CreateText(objectName + "_Label", buttonObject.transform, label, 16,
                FontStyle.Bold, PrimaryTextColor, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(146f, 44f), false);
            ButtonCount++;
            return button;
        }

        private static GameObject CreateRectObject(string objectName, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.hideFlags = HideFlags.DontSave;
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
