using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }
        public HideFlags hideFlags { get; set; }
        public int GetInstanceID() { return 0; }
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object { return original; }
        public static void Destroy(Object target) { }
        public static void DestroyImmediate(Object target) { }
        public static T FindObjectOfType<T>() { return default(T); }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; set; }
        public Transform transform { get; set; }
        public T GetComponent<T>() { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
        public T GetComponentInChildren<T>() { return default(T); }
        public T[] GetComponents<T>() { return new T[0]; }
    }

    public class Behaviour : Component { public bool enabled { get; set; } }
    public class MonoBehaviour : Behaviour { }
    public class Camera : Behaviour { public float fieldOfView { get; set; } }
    public class AudioClip : Object { }
    public class AudioSource : Behaviour
    {
        public bool playOnAwake { get; set; }
        public float spatialBlend { get; set; }
        public void PlayOneShot(AudioClip clip, float volume) { }
    }
    public class MaterialPropertyBlock
    {
        public void SetColor(string name, Color value) { }
    }
    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public LightShadows shadows { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public float range { get; set; }
    }
    public enum LightType { Point }
    public enum LightShadows { None }
    public class Font : Object { }
    public class TextMesh : Component
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public float characterSize { get; set; }
        public TextAnchor anchor { get; set; }
        public TextAlignment alignment { get; set; }
        public Color color { get; set; }
        public bool richText { get; set; }
    }
    public enum TextAnchor { MiddleCenter, UpperLeft }
    public enum TextAlignment { Center }
    public enum FontStyle { Normal, Bold }
    public enum HorizontalWrapMode { Wrap }
    public enum VerticalWrapMode { Overflow }
    public enum RenderMode { ScreenSpaceOverlay }
    public static class Resources { public static T GetBuiltinResource<T>(string path) { return default(T); } }
    public enum HideFlags { DontSave }
    public enum PrimitiveType { Quad, Cube }
    public enum Space { Self, World }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() { return new T(); }
    }
    public class Material : Object { }
    public class Mesh : Object
    {
        private Vector3[] vertexData = new Vector3[0];
        private Vector2[] uv = new Vector2[0];
        private List<int>[] triangles = new List<int>[0];
        public int subMeshCount { get; set; }
        public int vertexCount { get { return vertexData.Length; } }
        public Vector3[] vertices { get { return vertexData; } }
        public Rendering.IndexFormat indexFormat { get; set; }
        public void SetVertices(List<Vector3> values) { vertexData = values.ToArray(); }
        public void SetUVs(int channel, List<Vector2> values) { uv = values.ToArray(); }
        public void SetTriangles(List<int> values, int submesh, bool calculateBounds)
        {
            if (triangles.Length != subMeshCount) Array.Resize(ref triangles, subMeshCount);
            triangles[submesh] = new List<int>(values);
        }
        public int[] GetTriangles(int submesh) { return triangles[submesh].ToArray(); }
        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
        public void RecalculateTangents() { }
    }
    public class MeshFilter : Component { public Mesh sharedMesh { get; set; } }
    public class MeshRenderer : Component
    {
        public Material[] sharedMaterials { get; set; }
        public Material sharedMaterial
        {
            get { return sharedMaterials != null && sharedMaterials.Length > 0 ? sharedMaterials[0] : null; }
            set { if (sharedMaterials == null || sharedMaterials.Length == 0) sharedMaterials = new[] { value }; else sharedMaterials[0] = value; }
        }
        public bool receiveShadows { get; set; }
        public void SetPropertyBlock(MaterialPropertyBlock block) { }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public Rendering.LightProbeUsage lightProbeUsage { get; set; }
        public Rendering.ReflectionProbeUsage reflectionProbeUsage { get; set; }
    }
    public class MeshCollider : Collider { public bool convex { get; set; } public Mesh sharedMesh { get; set; } }

    public class GameObject : Object
    {
        public bool activeSelf { get; private set; }
        public int layer { get; set; }
        public bool isStatic { get; set; }
        public Transform transform { get; private set; }
        public GameObject(string name) { this.name = name; activeSelf = true; transform = new Transform(); }
        public GameObject(string name, params Type[] components) : this(name) { }
        public void SetActive(bool active) { activeSelf = active; }
        public T GetComponent<T>() { return default(T); }
        public T AddComponent<T>() where T : Component, new() { return new T(); }
        public static GameObject Find(string name) { return null; }
        public static GameObject CreatePrimitive(PrimitiveType type) { return new GameObject(type.ToString()); }
    }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Quaternion localRotation { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 forward { get { return rotation * Vector3.forward; } }
        public Vector3 up { get { return rotation * Vector3.up; } }
        public Vector3 right { get { return rotation * Vector3.right; } }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public Vector3 InverseTransformPoint(Vector3 point) { return point - position; }
        public Vector3 TransformPoint(Vector3 point) { return position + point; }
        public void SetPositionAndRotation(Vector3 value, Quaternion angle) { position = value; rotation = angle; }
        public void Rotate(Vector3 angles, Space space) { }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
    }

    public class Collider : Component { }
    public class BoxCollider : Collider { public Vector3 size { get; set; } }
    public class CharacterController : Collider
    {
        public float height { get; set; }
        public float radius { get; set; }
        public bool isGrounded { get; set; }
        public Vector3 velocity { get; set; }
        public Vector3 center { get; set; }
        public CollisionFlags Move(Vector3 motion) { return CollisionFlags.None; }
    }
    [Flags] public enum CollisionFlags { None = 0, Below = 1, Above = 2, Sides = 4 }
    public enum QueryTriggerInteraction { Ignore = 0 }

    public struct LayerMask
    {
        private int value;
        public static implicit operator int(LayerMask mask) { return mask.value; }
        public static implicit operator LayerMask(int value) { return new LayerMask { value = value }; }
    }

    public struct Ray
    {
        public Vector3 origin;
        public Vector3 direction;
        public Ray(Vector3 origin, Vector3 direction) { this.origin = origin; this.direction = direction; }
    }

    public struct RaycastHit
    {
        public Collider collider { get; set; }
        public Vector3 point { get; set; }
        public Vector3 normal { get; set; }
    }

    public static class Physics
    {
        public static bool Raycast(Ray ray, out RaycastHit hit, float distance, int layers, QueryTriggerInteraction triggers) { hit = new RaycastHit(); return false; }
        public static bool CheckCapsule(Vector3 bottom, Vector3 top, float radius, int layers, QueryTriggerInteraction triggers) { return false; }
        public static bool CheckSphere(Vector3 center, float radius, int layers, QueryTriggerInteraction triggers) { return false; }
        public static void SyncTransforms() { }
    }

    public struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0f, 0f); } }
        public static Vector2 one { get { return new Vector2(1f, 1f); } }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } }
        public void Normalize() { float length = (float)Math.Sqrt(sqrMagnitude); if (length > 0f) { x /= length; y /= length; } }
        public static Vector2 operator *(Vector2 value, float scale) { return new Vector2(value.x * scale, value.y * scale); }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float this[int index]
        {
            get { return index == 0 ? x : index == 1 ? y : z; }
            set { if (index == 0) x = value; else if (index == 1) y = value; else z = value; }
        }
        public static Vector3 zero { get { return new Vector3(0f, 0f, 0f); } }
        public static Vector3 one { get { return new Vector3(1f, 1f, 1f); } }
        public static Vector3 up { get { return new Vector3(0f, 1f, 0f); } }
        public static Vector3 down { get { return new Vector3(0f, -1f, 0f); } }
        public static Vector3 right { get { return new Vector3(1f, 0f, 0f); } }
        public static Vector3 forward { get { return new Vector3(0f, 0f, 1f); } }
        public static Vector3 back { get { return new Vector3(0f, 0f, -1f); } }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } }
        public Vector3 normalized { get { float length = magnitude; return length > 0.000001f ? this / length : zero; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator -(Vector3 value) { return new Vector3(-value.x, -value.y, -value.z); }
        public static Vector3 operator *(Vector3 value, float scale) { return new Vector3(value.x * scale, value.y * scale, value.z * scale); }
        public static Vector3 operator /(Vector3 value, float scale) { return new Vector3(value.x / scale, value.y / scale, value.z / scale); }
        public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
        public static Vector3 Cross(Vector3 a, Vector3 b) { return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }
        public static float Angle(Vector3 a, Vector3 b) { return (float)(Math.Acos(Math.Max(-1.0, Math.Min(1.0, Dot(a.normalized, b.normalized)))) * 180.0 / Math.PI); }
        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDelta)
        {
            Vector3 delta = target - current;
            float length = delta.magnitude;
            return length <= maxDelta || length == 0f ? target : current + delta * (maxDelta / length);
        }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { return a + (b - a) * Mathf.Clamp(t, 0f, 1f); }
    }

    public struct Quaternion
    {
        public static Quaternion Euler(float x, float y, float z) { return new Quaternion(); }
        public static Quaternion AngleAxis(float angle, Vector3 axis) { return new Quaternion(); }
        public static Quaternion LookRotation(Vector3 forward) { return new Quaternion(); }
        public static Quaternion identity { get { return new Quaternion(); } }
        public static Quaternion operator *(Quaternion a, Quaternion b) { return new Quaternion(); }
        public static Vector3 operator *(Quaternion rotation, Vector3 point) { return point; }
    }

    public static class Mathf
    {
        public static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
        public static int Clamp(int value, int min, int max) { return Math.Max(min, Math.Min(max, value)); }
        public static float Min(float a, float b) { return Math.Min(a, b); }
        public static int Min(int a, int b) { return Math.Min(a, b); }
        public static float Max(float a, float b) { return Math.Max(a, b); }
        public static int Max(int a, int b) { return Math.Max(a, b); }
        public static float Repeat(float value, float length) { return (value % length + length) % length; }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp(t, 0f, 1f); }
        public static float Exp(float value) { return (float)Math.Exp(value); }
        public static float Sqrt(float value) { return (float)Math.Sqrt(value); }
        public static float Abs(float value) { return Math.Abs(value); }
        public static float Sin(float value) { return (float)Math.Sin(value); }
        public static float Cos(float value) { return (float)Math.Cos(value); }
        public static float Clamp01(float value) { return Clamp(value, 0f, 1f); }
        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = t * t * (3f - 2f * t);
            return from + (to - from) * t;
        }
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            float delta = target - current;
            return Math.Abs(delta) <= maxDelta ? target : current + Math.Sign(delta) * maxDelta;
        }
        public static float PingPong(float value, float length) { return length - Math.Abs(Repeat(value, length * 2f) - length); }
        public const float PI = (float)Math.PI;
    }

    public static class Random
    {
        public static float Range(float min, float max) { return min; }
        public static Vector2 insideUnitCircle { get { return Vector2.zero; } }
    }
    public static class Time
    {
        public static float deltaTime { get { return 0.02f; } }
        public static float unscaledDeltaTime { get { return 0.02f; } }
        public static float time { get { return 0f; } }
        public static float realtimeSinceStartup { get { return 0f; } }
    }
    public static class Input
    {
        public static float GetAxisRaw(string name) { return 0f; }
        public static bool GetKeyDown(KeyCode key) { return false; }
        public static bool GetKey(KeyCode key) { return false; }
        public static bool GetMouseButtonDown(int button) { return false; }
        public static bool GetMouseButton(int button) { return false; }
        public static Vector2 mouseScrollDelta { get { return Vector2.zero; } }
    }
    public enum KeyCode { Escape, R, T, LeftShift, LeftControl, C, Space, Alpha1, Alpha2, Q, F3, F4, F6, F7, F8 }

    public static class Cursor { public static CursorLockMode lockState { get; set; } public static bool visible { get; set; } }
    public enum CursorLockMode { None, Locked }
    public static class PlayerPrefs
    {
        public static float GetFloat(string key, float fallback) { return fallback; }
        public static void SetFloat(string key, float value) { }
        public static void Save() { }
    }
    public static class Application { public static int targetFrameRate { get; set; } public static bool isPlaying { get; set; } public static string persistentDataPath { get { return "Temp"; } } }
    public static class JsonUtility
    {
        public static string ToJson(object value, bool prettyPrint) { return "{\"records\":[]}"; }
        public static T FromJson<T>(string json) { return default(T); }
    }
    public static class Screen { public static int width { get { return 1920; } } public static int height { get { return 1080; } } }
    public static class Debug
    {
        public static void LogError(string message, Object context) { }
        public static void LogWarning(string message, Object context) { }
    }

    public struct Rect { public Rect(float x, float y, float width, float height) { } }
    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1f, 1f, 1f, 1f); } }
    }
    public class Texture2D { public static Texture2D whiteTexture { get { return new Texture2D(); } } }
    public static class GUI
    {
        public static Color color { get; set; }
        public static void BeginGroup(Rect rect) { }
        public static void EndGroup() { }
        public static void Box(Rect rect, string text) { }
        public static void Label(Rect rect, string text) { }
        public static float HorizontalSlider(Rect rect, float value, float min, float max) { return value; }
        public static bool Button(Rect rect, string text) { return false; }
        public static void DrawTexture(Rect rect, Texture2D texture) { }
    }

    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class Header : Attribute { public Header(string text) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class ExecuteAlways : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenu : Attribute
    {
        public string fileName { get; set; }
        public string menuName { get; set; }
    }
}

namespace UnityEngine.Rendering
{
    public enum IndexFormat { UInt16, UInt32 }
    public enum ShadowCastingMode { Off, On }
    public enum LightProbeUsage { Off }
    public enum ReflectionProbeUsage { Off }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public class UnityEvent
    {
        public void AddListener(UnityAction action) { }
        public void RemoveListener(UnityAction action) { }
    }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : Behaviour { }
    public class StandaloneInputModule : Behaviour { }
}

namespace UnityEngine.UI
{
    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public bool pixelPerfect { get; set; }
        public int sortingOrder { get; set; }
    }
    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ScaleWithScreenSize }
        public enum ScreenMatchMode { MatchWidthOrHeight }
        public ScaleMode uiScaleMode { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public float matchWidthOrHeight { get; set; }
        public float referencePixelsPerUnit { get; set; }
    }
    public class GraphicRaycaster : Behaviour { }
    public class CanvasRenderer : Behaviour { }
    public class Image : Behaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform { get; set; }
    }
    public class Text : Image
    {
        public Font font { get; set; }
        public string text { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool alignByGeometry { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool supportRichText { get; set; }
        public float lineSpacing { get; set; }
    }
    public class Shadow : Behaviour
    {
        public Color effectColor { get; set; }
        public Vector2 effectDistance { get; set; }
        public bool useGraphicAlpha { get; set; }
    }
    public class Mask : Behaviour { public bool showGraphic { get; set; } }
    public class ScrollRect : Behaviour
    {
        public enum MovementType { Clamped }
        public RectTransform viewport { get; set; }
        public RectTransform content { get; set; }
        public bool horizontal { get; set; }
        public bool vertical { get; set; }
        public MovementType movementType { get; set; }
        public float scrollSensitivity { get; set; }
        public bool inertia { get; set; }
        public float decelerationRate { get; set; }
    }
    public class RectOffset
    {
        public RectOffset(int left, int right, int top, int bottom) { }
    }
    public class VerticalLayoutGroup : Behaviour
    {
        public RectOffset padding { get; set; }
        public TextAnchor childAlignment { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public float spacing { get; set; }
    }
    public class ContentSizeFitter : Behaviour
    {
        public enum FitMode { Unconstrained, PreferredSize }
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
    }
    public struct ColorBlock
    {
        public Color normalColor;
        public Color highlightedColor;
        public Color pressedColor;
        public Color selectedColor;
        public Color disabledColor;
        public float colorMultiplier;
        public float fadeDuration;
    }
    public struct Navigation
    {
        public enum Mode { None }
        public Mode mode;
    }
    public class Button : Behaviour
    {
        private readonly ButtonClickedEvent click = new ButtonClickedEvent();
        public ButtonClickedEvent onClick { get { return click; } }
        public bool interactable { get; set; }
        public Image targetGraphic { get; set; }
        public ColorBlock colors { get; set; }
        public Navigation navigation { get; set; }
        public class ButtonClickedEvent : UnityEvent { }
    }
}

namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single }
    public static class SceneManager { public static void LoadScene(string path, LoadSceneMode mode) { } }
    public static class SceneUtility { public static int GetBuildIndexByScenePath(string path) { return 0; } }
}
