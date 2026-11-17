using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace FpsPractice.Tests.PlayMode
{
    public sealed class ImpactEffectPoolTests
    {
        private GameObject host;
        private ImpactEffectPool pool;
        private Material material;

        [SetUp]
        public void SetUp()
        {
            material = new Material(Shader.Find("Standard"));
            host = new GameObject("Impact Pool Test");
            pool = host.AddComponent<ImpactEffectPool>();
            pool.Configure(material, material);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (material != null) Object.DestroyImmediate(material);
        }

        [UnityTest]
        public IEnumerator Effects_ReuseFixedPool()
        {
            yield return null;
            Assert.AreEqual(24, pool.DecalCount);
            Assert.AreEqual(12, pool.ShellCount);
            int children = host.transform.childCount;
            for (int i = 0; i < 50; i++)
            {
                pool.SpawnDecal(Vector3.zero, Vector3.up);
                pool.SpawnShell(Vector3.zero, Vector3.right);
            }

            Assert.AreEqual(children, host.transform.childCount);
        }
    }
}
