using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace FpsPractice.Tests.PlayMode
{
    public sealed class TrainingUIControllerTests
    {
        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
        }

        [UnityTest]
        public IEnumerator UI_BuildsOnceAndSwitchesPages()
        {
            host = new GameObject("Training UI Test");
            host.AddComponent<PracticeSession>();
            TrainingUIController controller = host.AddComponent<TrainingUIController>();
            yield return null;

            Assert.AreEqual(5, controller.PanelCount);
            Assert.AreEqual(6, controller.ButtonCount);
            Assert.AreEqual(4, controller.LiveTextCount);
            controller.ShowPage(TrainingUIPage.History);
            yield return null;
            controller.ShowPage(TrainingUIPage.Help);
            yield return null;
            Assert.AreEqual(5, controller.PanelCount);
            Assert.AreEqual(6, controller.ButtonCount);
        }
    }
}
