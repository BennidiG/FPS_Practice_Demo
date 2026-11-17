using UnityEngine;

namespace FpsPractice
{
    [DisallowMultipleComponent]
    public sealed class ImpactFlash : MonoBehaviour
    {
        private MeshRenderer flashRenderer;
        private MaterialPropertyBlock propertyBlock;
        private float duration;
        private float remaining;
        private float startScale;
        private Color color;

        public void Initialize(Material material)
        {
            flashRenderer = GetComponent<MeshRenderer>();
            flashRenderer.sharedMaterial = material;
            propertyBlock = new MaterialPropertyBlock();
            gameObject.SetActive(false);
        }

        public void Show(Vector3 position, Quaternion rotation, float scale, Color tint, float lifetime)
        {
            duration = Mathf.Max(0.01f, lifetime);
            remaining = duration;
            startScale = scale;
            color = tint;
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = Vector3.one * scale;
            gameObject.SetActive(true);
            ApplyColor();
        }

        public void Tick(float deltaTime)
        {
            if (remaining <= 0f || deltaTime <= 0f)
            {
                return;
            }

            remaining -= deltaTime;
            if (remaining <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            float progress = 1f - remaining / duration;
            transform.localScale = Vector3.one * (startScale * (1f + progress * 0.5f));
            ApplyColor();
        }

        private void ApplyColor()
        {
            float alpha = duration > 0f ? Mathf.Clamp01(remaining / duration) : 0f;
            Color faded = new Color(color.r, color.g, color.b, color.a * alpha);
            propertyBlock.SetColor("_Color", faded);
            propertyBlock.SetColor("_EmissionColor", faded);
            flashRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
