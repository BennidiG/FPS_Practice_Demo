using System.Collections.Generic;
using UnityEngine;
using FpsPractice.Models;

namespace FpsPractice
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class RangeEnvironment : MonoBehaviour
    {
        [SerializeField] private Material[] materials = new Material[RangeModelFactory.MaterialSlotCount];
        private readonly List<GameObject> generatedObjects = new List<GameObject>();
        private static Mesh sharedRangeMesh;
        private bool initialized;

        public GameObject GeneratedEnvironment { get; private set; }
        public int GeneratedColliderCount { get; private set; }
        public int GeneratedLightCount { get; private set; }
        public int MaterialCount { get { return materials != null ? materials.Length : 0; } }

        public void ConfigureMaterials(Material[] slots)
        {
            materials = slots;
        }

        private void OnEnable()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            if (materials == null || materials.Length < RangeModelFactory.MaterialSlotCount)
            {
                Debug.LogError("RangeEnvironment requires nine materials: Floor, Wall, Metal, Accent, SafetyYellow, Rubber, Light, White and WarningRed.", this);
                enabled = false;
                return;
            }

            if (sharedRangeMesh == null)
            {
                sharedRangeMesh = RangeModelFactory.Build();
            }

            CreateEnvironmentMesh();
            CreateFacilityColliders();
            CreateLights();
            CreateSigns();
        }

        private void CreateEnvironmentMesh()
        {
            GeneratedEnvironment = CreateChild("Environment Geometry");
            GeneratedEnvironment.isStatic = true;
            MeshFilter filter = GeneratedEnvironment.AddComponent<MeshFilter>();
            filter.sharedMesh = sharedRangeMesh;
            MeshRenderer renderer = GeneratedEnvironment.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private void CreateFacilityColliders()
        {
            for (int i = -1; i <= 1; i++)
            {
                CreateBoxCollider("Lane Divider " + (i + 2), new Vector3(i * 4f, 0.65f, -3f), new Vector3(0.12f, 1.3f, 6f));
            }

            CreateBoxCollider("Left Stool", new Vector3(-4.25f, 0.32f, -19.1f), new Vector3(0.7f, 0.64f, 0.7f));
            CreateBoxCollider("Right Stool", new Vector3(4.25f, 0.32f, -19.1f), new Vector3(0.7f, 0.64f, 0.7f));
            CreateBoxCollider("Control Desk", new Vector3(0f, 0.82f, -19.2f), new Vector3(6f, 0.14f, 1.2f));
        }

        private void CreateLights()
        {
            CreateLight("Range Light Front", new Vector3(0f, 5.8f, -2f));
            CreateLight("Range Light Middle", new Vector3(0f, 5.8f, -10f));
            CreateLight("Range Light Rear", new Vector3(0f, 5.8f, -18f));
        }

        private void CreateSigns()
        {
            CreateSign("Lane 01", "01", new Vector3(-6f, 4.3f, 0.72f), 72, new Color(0.1f, 0.8f, 0.9f, 1f));
            CreateSign("Lane 02", "02", new Vector3(-2f, 4.3f, 0.72f), 72, new Color(0.1f, 0.8f, 0.9f, 1f));
            CreateSign("Lane 03", "03", new Vector3(2f, 4.3f, 0.72f), 72, new Color(0.1f, 0.8f, 0.9f, 1f));
            CreateSign("Lane 04", "04", new Vector3(6f, 4.3f, 0.72f), 72, new Color(0.1f, 0.8f, 0.9f, 1f));
            CreateSign("Range Title", "SHOOTING RANGE", new Vector3(0f, 5.4f, 0.72f), 82, new Color(1f, 0.72f, 0.1f, 1f));
        }

        private void CreateLight(string objectName, Vector3 position)
        {
            GameObject lightObject = CreateChild(objectName);
            lightObject.transform.localPosition = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.72f, 0.86f, 1f, 1f);
            light.intensity = 1.15f;
            light.range = 12f;
            light.shadows = LightShadows.None;
            GeneratedLightCount++;
        }

        private void CreateSign(string objectName, string value, Vector3 position, int fontSize, Color color)
        {
            GameObject sign = CreateChild(objectName);
            sign.transform.localPosition = position;
            sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh text = sign.AddComponent<TextMesh>();
            text.text = value;
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.characterSize = 0.08f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            text.richText = false;
        }

        private void CreateBoxCollider(string objectName, Vector3 position, Vector3 size)
        {
            GameObject colliderObject = CreateChild(objectName);
            colliderObject.layer = 10;
            colliderObject.isStatic = true;
            colliderObject.transform.localPosition = position;
            BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
            collider.size = size;
            GeneratedColliderCount++;
        }

        private GameObject CreateChild(string objectName)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            child.hideFlags = HideFlags.DontSave;
            generatedObjects.Add(child);
            return child;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < generatedObjects.Count; i++)
            {
                if (generatedObjects[i] == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(generatedObjects[i]);
                }
                else
                {
                    DestroyImmediate(generatedObjects[i]);
                }
            }

            generatedObjects.Clear();
        }
    }
}
