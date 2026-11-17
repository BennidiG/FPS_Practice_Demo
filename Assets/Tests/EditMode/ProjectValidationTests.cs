using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FpsPractice.Tests.EditMode
{
    public sealed class ProjectValidationTests
    {
        [Test]
        public void BuildSettings_ContainBothRequiredScenes()
        {
            AssertSceneEnabled("Assets/Scenes/StartScene.unity");
            AssertSceneEnabled("Assets/Scenes/SampleScene.unity");
        }

        [Test]
        public void TargetPrefab_HasOneAccurateColliderAndInstancedMaterial()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Object/ShootObject.prefab");
            Assert.NotNull(prefab);
            Assert.NotNull(prefab.GetComponent<ShootTarget>());
            Assert.NotNull(prefab.GetComponent<TargetVisual>());
            Assert.AreEqual(9, prefab.layer);

            Collider[] colliders = prefab.GetComponents<Collider>();
            Assert.AreEqual(1, colliders.Length);
            Assert.IsInstanceOf<MeshCollider>(colliders[0]);
            Assert.IsTrue(((MeshCollider)colliders[0]).convex);
            Assert.IsTrue(colliders[0].enabled);
            Assert.IsFalse(colliders[0].isTrigger);

            MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
            Assert.NotNull(meshFilter.sharedMesh);
            Assert.LessOrEqual(meshFilter.sharedMesh.vertexCount, 200);
            Assert.AreEqual(meshFilter.sharedMesh, ((MeshCollider)colliders[0]).sharedMesh);

            MeshRenderer meshRenderer = prefab.GetComponent<MeshRenderer>();
            Assert.IsFalse(meshRenderer.receiveShadows);
            Assert.AreEqual(6, meshRenderer.sharedMaterials.Length);
            Assert.IsTrue(meshRenderer.sharedMaterial.enableInstancing);

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                Mesh generatedMesh = instance.GetComponent<MeshFilter>().sharedMesh;
                Assert.NotNull(generatedMesh);
                Assert.Greater(generatedMesh.vertexCount, 200);
                Assert.AreEqual(6, generatedMesh.subMeshCount);
                Mesh collisionMesh = instance.GetComponent<MeshCollider>().sharedMesh;
                Assert.NotNull(collisionMesh);
                Assert.AreNotSame(generatedMesh, collisionMesh);
                Assert.Less(collisionMesh.vertexCount, generatedMesh.vertexCount);

                instance.SetActive(false);
                instance.SetActive(true);
                Assert.AreSame(collisionMesh, instance.GetComponent<MeshCollider>().sharedMesh);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PracticeScene_HasSplitControllerAndClosedBoundaries()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            MoveCamera player = Object.FindObjectOfType<MoveCamera>();
            Assert.NotNull(player);
            Assert.NotNull(player.GetComponent<CharacterController>());
            Assert.NotNull(player.GetComponent<Initialization>());
            Assert.IsNull(player.GetComponent<Rigidbody>());
            Assert.AreEqual(8, player.gameObject.layer);
            Assert.AreEqual(8, LayerMask.NameToLayer("Player"));
            Assert.AreEqual(9, LayerMask.NameToLayer("Target"));
            Assert.AreEqual(10, LayerMask.NameToLayer("Environment"));

            Transform camera = player.transform.Find("Main Camera");
            Assert.NotNull(camera);
            Assert.NotNull(camera.GetComponent<CreateObject>().ShootObj);
            Assert.NotNull(camera.GetComponent<TargetSpawner>());
            Assert.NotNull(camera.GetComponent<WeaponController>());
            Assert.NotNull(camera.GetComponent<PracticeSession>());
            Assert.NotNull(camera.GetComponent<PracticeHUD>());
            Assert.NotNull(camera.GetComponent<WeaponView>());
            Assert.NotNull(camera.GetComponent<FeedbackController>());
            Assert.AreEqual(2, camera.GetComponent<WeaponController>().WeaponCount);
            Assert.AreEqual(4, camera.GetComponent<PracticeSession>().ScenarioCount);
            RangeEnvironment range = Object.FindObjectOfType<RangeEnvironment>();
            Assert.NotNull(range);
            Assert.AreEqual(9, range.MaterialCount);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("Wall_") || root.name == "Plane")
                {
                    Collider boundary = root.GetComponent<Collider>();
                    Assert.NotNull(boundary, root.name + " has no collider");
                    Assert.IsTrue(boundary.enabled, root.name + " collider is disabled");
                    Assert.AreEqual(10, root.layer, root.name + " has an incorrect layer");
                }
            }

            Assert.NotNull(GameObject.Find("Wall_Back"));
        }

        [Test]
        public void StartScene_ButtonsAreSeparated()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity", OpenSceneMode.Single);
            RectTransform practice = GameObject.Find("ChooseModeButton").GetComponent<RectTransform>();
            RectTransform start = GameObject.Find("StartButton").GetComponent<RectTransform>();
            RectTransform settings = GameObject.Find("SetButton").GetComponent<RectTransform>();

            Assert.GreaterOrEqual(Vector2.Distance(practice.anchoredPosition, start.anchoredPosition), 48f);
            Assert.GreaterOrEqual(Vector2.Distance(start.anchoredPosition, settings.anchoredPosition), 48f);
            Assert.NotNull(Object.FindObjectOfType<ChangeScene>());
            Assert.NotNull(Object.FindObjectOfType<Initialization>());
        }

        [TestCase("Assets/Scenes/StartScene.unity")]
        [TestCase("Assets/Scenes/SampleScene.unity")]
        public void Scenes_HaveNoMissingScripts(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                AssertNoMissingScripts(root);
            }
        }

        private static void AssertSceneEnabled(string scenePath)
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == scenePath && scene.enabled)
                {
                    Assert.Pass();
                }
            }

            Assert.Fail("Scene is missing or disabled: " + scenePath);
        }

        private static void AssertNoMissingScripts(GameObject gameObject)
        {
            Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject), gameObject.name);
            foreach (Transform child in gameObject.transform)
            {
                AssertNoMissingScripts(child.gameObject);
            }
        }
    }
}
