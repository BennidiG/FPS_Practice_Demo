using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace FpsPractice.Tests.PlayMode
{
    public sealed class PlayerControllerTests
    {
        private GameObject player;
        private GameObject floor;

        [TearDown]
        public void TearDown()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (player != null)
            {
                Object.DestroyImmediate(player);
            }

            if (floor != null)
            {
                Object.DestroyImmediate(floor);
            }
        }

        [UnityTest]
        public IEnumerator Gravity_LandsPlayerOnFloor()
        {
            CreatePlayer(new Vector3(0f, 2f, 0f));
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(10f, 1f, 10f);
            Physics.SyncTransforms();

            yield return new WaitForSeconds(1.5f);

            CharacterController controller = player.GetComponent<CharacterController>();
            Assert.IsTrue(controller.isGrounded);
            Assert.GreaterOrEqual(player.transform.position.y, -0.05f);
            Assert.LessOrEqual(player.transform.position.y, 0.2f);
        }

        [UnityTest]
        public IEnumerator Recoil_RotatesCameraWithoutRotatingBody()
        {
            CreatePlayer(Vector3.zero);
            MoveCamera controller = player.GetComponent<MoveCamera>();
            Transform camera = player.transform.Find("View");
            controller.AddRecoil(-2f, 1f);

            yield return null;

            Assert.AreEqual(0f, player.transform.localEulerAngles.y, 0.001f);
            Assert.AreNotEqual(0f, camera.localEulerAngles.x);
            Assert.AreNotEqual(0f, camera.localEulerAngles.y);
        }

        private void CreatePlayer(Vector3 position)
        {
            player = new GameObject("Controller Test Player");
            player.transform.position = position;
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.skinWidth = 0.04f;

            GameObject view = new GameObject("View");
            view.transform.SetParent(player.transform, false);
            view.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            view.AddComponent<Camera>();
            player.AddComponent<MoveCamera>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
