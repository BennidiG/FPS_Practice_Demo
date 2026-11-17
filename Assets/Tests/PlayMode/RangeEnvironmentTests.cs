using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace FpsPractice.Tests.PlayMode
{
    public sealed class RangeEnvironmentTests
    {
        private GameObject root;
        private RangeEnvironment range;
        private Material[] materials;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Range Environment Test");
            root.SetActive(false);
            range = root.AddComponent<RangeEnvironment>();
            materials = new Material[9];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = new Material(Shader.Find("Standard"));
            }

            range.ConfigureMaterials(materials);
            root.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                {
                    Object.DestroyImmediate(materials[i]);
                }
            }
        }

        [UnityTest]
        public IEnumerator Environment_BuildsOnceWithGeometryLightsAndColliders()
        {
            yield return null;
            Assert.NotNull(range.GeneratedEnvironment);
            Mesh mesh = range.GeneratedEnvironment.GetComponent<MeshFilter>().sharedMesh;
            Assert.NotNull(mesh);
            Assert.AreEqual(9, mesh.subMeshCount);
            Assert.Greater(mesh.vertexCount, 500);
            Assert.AreEqual(6, range.GeneratedColliderCount);
            Assert.AreEqual(3, range.GeneratedLightCount);
            Assert.AreEqual(new Vector3(0f, 0.82f, -19.2f), GameObject.Find("Control Desk").transform.localPosition);
            Assert.AreEqual(new Vector3(-4.25f, 0.32f, -19.1f), GameObject.Find("Left Stool").transform.localPosition);

            int childCount = root.transform.childCount;
            root.SetActive(false);
            root.SetActive(true);
            yield return null;
            Assert.AreEqual(childCount, root.transform.childCount);
        }
    }
}
