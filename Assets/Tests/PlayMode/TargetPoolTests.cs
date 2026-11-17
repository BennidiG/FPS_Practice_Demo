using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace FpsPractice.Tests.PlayMode
{
    public sealed class TargetPoolTests
    {
        private GameObject player;
        private GameObject template;
        private CreateObject spawner;

        [SetUp]
        public void SetUp()
        {
            template = new GameObject("Test Target Template");
            template.layer = 9;
            template.AddComponent<BoxCollider>();
            template.AddComponent<ShootTarget>();
            template.SetActive(false);

            player = new GameObject("Test Player");
            player.SetActive(false);
            player.AddComponent<TargetSpawner>();
            player.AddComponent<WeaponController>();
            player.AddComponent<PracticeSession>();
            spawner = player.AddComponent<CreateObject>();
            spawner.ShootObj = template;
            player.GetComponent<WeaponController>().ConfigureLoadout(new[]
            {
                WeaponDefinition.CreateDefault(),
                WeaponDefinition.CreateSidearmDefault()
            });
            player.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            ShootTarget[] remainingTargets = Object.FindObjectsOfType<ShootTarget>();
            foreach (ShootTarget target in remainingTargets)
            {
                Object.DestroyImmediate(target.gameObject);
            }

            if (player != null)
            {
                Object.DestroyImmediate(player);
            }

            if (template != null)
            {
                Object.DestroyImmediate(template);
            }
        }

        [UnityTest]
        public IEnumerator Shooting_IgnoresEnvironmentAndReusesTarget()
        {
            yield return null;
            GameObject environment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            environment.layer = 10;
            environment.transform.position = new Vector3(0f, 0f, 5f);
            Physics.SyncTransforms();

            Assert.IsFalse(spawner.TryShoot(new Ray(Vector3.zero, Vector3.forward)));
            Assert.NotNull(environment);
            Assert.AreEqual(1, spawner.ShotsFired);
            Assert.AreEqual(0, spawner.TargetsHit);
            Object.DestroyImmediate(environment);

            ShootTarget target = spawner.Targets[0];
            int originalInstanceId = target.GetInstanceID();
            target.Place(new Vector3(0f, 0f, 5f), Quaternion.identity);
            target.MaxHealth = 1f;
            target.CurrentHealth = 1f;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.25f);

            Assert.IsTrue(spawner.TryShoot(new Ray(Vector3.zero, Vector3.forward)));
            Assert.AreEqual(2, spawner.ShotsFired);
            Assert.AreEqual(1, spawner.TargetsHit);
            Assert.AreEqual(6, spawner.ActiveTargetCount);
            Assert.AreEqual(originalInstanceId, spawner.Targets[0].GetInstanceID());
            Assert.Less(target.transform.position.z, 0f);
        }

        [UnityTest]
        public IEnumerator Reset_KeepsPoolAndClearsStatistics()
        {
            yield return null;
            spawner.TryShoot(new Ray(Vector3.zero, Vector3.forward));
            int originalInstanceId = spawner.Targets[0].GetInstanceID();
            spawner.ResetSession();

            Assert.AreEqual(0, spawner.ShotsFired);
            Assert.AreEqual(0, spawner.TargetsHit);
            Assert.AreEqual(6, spawner.ActiveTargetCount);
            Assert.AreEqual(originalInstanceId, spawner.Targets[0].GetInstanceID());
        }

        [UnityTest]
        public IEnumerator Switching_BlocksFireAndPreservesAmmo()
        {
            yield return null;
            WeaponController weapon = player.GetComponent<WeaponController>();
            Assert.IsFalse(spawner.TryShoot(new Ray(Vector3.zero, Vector3.forward)));
            Assert.AreEqual(29, weapon.CurrentAmmo);
            Assert.AreEqual(1, spawner.ShotsFired);

            Assert.IsTrue(weapon.SwitchWeapon(1));
            Assert.IsTrue(weapon.IsSwitching);
            Assert.AreEqual(12, weapon.CurrentAmmo);
            Assert.IsFalse(spawner.TryShoot(new Ray(Vector3.zero, Vector3.forward)));
            Assert.AreEqual(1, spawner.ShotsFired);

            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(weapon.IsSwitching);
            Assert.IsTrue(weapon.SwitchWeapon(0));
            Assert.AreEqual(29, weapon.CurrentAmmo);
        }

        [UnityTest]
        public IEnumerator Spawns_StayInsideRangeAndSeparated()
        {
            yield return null;
            for (int iteration = 0; iteration < 25; iteration++)
            {
                spawner.ResetSession();
                for (int i = 0; i < spawner.Targets.Count; i++)
                {
                    Vector3 position = spawner.Targets[i].transform.position;
                    Assert.GreaterOrEqual(position.x, -8f);
                    Assert.LessOrEqual(position.x, 8f);
                    Assert.GreaterOrEqual(position.y, 2f);
                    Assert.LessOrEqual(position.y, 6f);
                    Assert.AreEqual(-1f, position.z);

                    for (int j = i + 1; j < spawner.Targets.Count; j++)
                    {
                        float distance = Vector3.Distance(position, spawner.Targets[j].transform.position);
                        Assert.GreaterOrEqual(distance, 1.49f);
                    }
                }
            }
        }
    }
}
