using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace FpsPractice.Tests.PlayMode
{
    public sealed class WeaponViewTests
    {
        private GameObject viewObject;
        private WeaponView view;
        private Material[] materials;

        [SetUp]
        public void SetUp()
        {
            viewObject = new GameObject("Weapon View Test Camera");
            viewObject.SetActive(false);
            view = viewObject.AddComponent<WeaponView>();
            materials = new Material[5];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = new Material(Shader.Find("Standard"));
            }

            view.ConfigureMaterials(materials);
            viewObject.SetActive(true);
            view.SwitchTo(WeaponModelKind.Rifle);
        }

        [TearDown]
        public void TearDown()
        {
            if (viewObject != null)
            {
                Object.DestroyImmediate(viewObject);
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
        public IEnumerator ViewModel_RemainsParentedWithoutLocalDrift()
        {
            yield return null;
            Transform model = viewObject.transform.Find("Vanguard Rifle View");
            Assert.NotNull(model);
            Assert.AreEqual(viewObject.transform, model.parent);
            Assert.AreEqual(5, model.GetComponent<MeshFilter>().sharedMesh.subMeshCount);

            yield return new WaitForSeconds(0.3f);
            Vector3 localPosition = model.localPosition;
            viewObject.transform.rotation = Quaternion.Euler(10f, 45f, 0f);
            yield return null;
            Assert.AreEqual(localPosition, model.localPosition);
        }

        [UnityTest]
        public IEnumerator Ads_MovesWeaponToCenter()
        {
            yield return null;
            view.SetAimBlend(1f);
            yield return null;
            Transform model = viewObject.transform.Find("Vanguard Rifle View");
            Assert.AreEqual(0f, model.localPosition.x, 0.001f);
        }

        [UnityTest]
        public IEnumerator Switching_ReusesExistingModels()
        {
            yield return null;
            Transform rifle = viewObject.transform.Find("Vanguard Rifle View");
            int originalId = rifle.GetInstanceID();

            view.SwitchTo(WeaponModelKind.Sidearm);
            yield return null;
            Assert.IsFalse(rifle.gameObject.activeSelf);
            Transform sidearm = viewObject.transform.Find("Precision Sidearm View");
            Assert.IsTrue(sidearm.gameObject.activeSelf);
            Assert.AreEqual(5, sidearm.GetComponent<MeshFilter>().sharedMesh.subMeshCount);

            view.SwitchTo(WeaponModelKind.Rifle);
            Assert.AreEqual(originalId, rifle.GetInstanceID());
        }
    }
}
