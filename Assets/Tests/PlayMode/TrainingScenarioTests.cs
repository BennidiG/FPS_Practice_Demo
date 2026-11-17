using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace FpsPractice.Tests.PlayMode
{
    public sealed class TrainingScenarioTests
    {
        private GameObject player;
        private GameObject template;
        private CreateObject spawner;
        private PracticeSession session;
        private Initialization settings;
        private WeaponController weapon;

        [SetUp]
        public void SetUp()
        {
            template = new GameObject("Scenario Target Template");
            template.layer = 9;
            template.AddComponent<BoxCollider>();
            template.AddComponent<ShootTarget>();
            template.SetActive(false);

            player = new GameObject("Scenario Test Player");
            player.SetActive(false);
            settings = player.AddComponent<Initialization>();
            player.AddComponent<TargetSpawner>();
            weapon = player.AddComponent<WeaponController>();
            weapon.ConfigureLoadout(new[] { WeaponDefinition.CreateDefault(), WeaponDefinition.CreateSidearmDefault() });
            session = player.AddComponent<PracticeSession>();
            session.ConfigureScenarios(new[] { TrainingScenario.CreateTrackingDefault() });
            spawner = player.AddComponent<CreateObject>();
            spawner.ShootObj = template;
            player.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            ShootTarget[] targets = Object.FindObjectsOfType<ShootTarget>();
            foreach (ShootTarget target in targets) Object.DestroyImmediate(target.gameObject);
            if (player != null) Object.DestroyImmediate(player);
            if (template != null) Object.DestroyImmediate(template);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        [UnityTest]
        public IEnumerator ReloadAndTrainingTime_StopWhilePaused()
        {
            yield return new WaitForSeconds(0.1f);
            Assert.IsFalse(spawner.TryShoot(new Ray(Vector3.zero, Vector3.forward)));
            weapon.BeginReload();
            Assert.IsTrue(weapon.IsReloading);
            settings.SetPaused(true);
            float pausedTime = session.TimeSeconds;

            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(pausedTime, session.TimeSeconds, 0.001f);
            Assert.IsTrue(weapon.IsReloading);
            int pausedShots = spawner.ShotsFired;
            Assert.IsFalse(spawner.TryShoot(new Ray(Vector3.zero, Vector3.forward)));
            Assert.AreEqual(pausedShots, spawner.ShotsFired);

            settings.SetPaused(false);
            yield return new WaitForSeconds(0.15f);
            Assert.Greater(session.TimeSeconds, pausedTime);
        }

        [UnityTest]
        public IEnumerator DynamicTargets_FreezeWithTrainingClock()
        {
            yield return new WaitForSeconds(0.1f);
            Vector3 initial = spawner.Targets[0].transform.position;
            yield return new WaitForSeconds(0.1f);
            Assert.AreNotEqual(initial, spawner.Targets[0].transform.position);

            settings.SetPaused(true);
            Vector3 paused = spawner.Targets[0].transform.position;
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(paused, spawner.Targets[0].transform.position);
        }
    }
}
