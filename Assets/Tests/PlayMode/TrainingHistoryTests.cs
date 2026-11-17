using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace FpsPractice.Tests.PlayMode
{
    public sealed class TrainingHistoryTests
    {
        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            TrainingHistoryStore.Clear();
        }

        [UnityTest]
        public IEnumerator RepeatedSummary_UpdatesRecordWithoutDuplicating()
        {
            TrainingHistoryStore.Clear();
            host = new GameObject("Training History Test");
            PracticeSession session = host.AddComponent<PracticeSession>();
            session.PersistenceEnabled = false;
            yield return null;

            session.RegisterShot(true);
            session.ToggleSummary();
            session.ToggleSummary();
            session.RegisterShot(true);
            session.ToggleSummary();

            Assert.AreEqual(1, TrainingHistoryStore.Records.Count);
            Assert.AreEqual(2, TrainingHistoryStore.Records[0].shots);
            Assert.AreEqual(2, TrainingHistoryStore.Records[0].hits);
        }
    }
}
