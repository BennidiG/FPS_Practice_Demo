using UnityEngine;
using FpsPractice.Models;

namespace FpsPractice
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public sealed class TargetVisual : MonoBehaviour
    {
        private static Mesh sharedVisualMesh;
        private static Mesh sharedCollisionMesh;
        private bool initialized;
        private MeshRenderer targetRenderer;
        private ShootTarget target;
        private MaterialPropertyBlock hitProperties;
        private bool flashing;

        private void OnEnable()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            if (sharedVisualMesh == null)
            {
                sharedVisualMesh = TargetModelFactory.BuildTarget();
            }

            if (sharedCollisionMesh == null)
            {
                sharedCollisionMesh = TargetModelFactory.BuildCollisionTarget();
            }

            MeshFilter filter = GetComponent<MeshFilter>();
            filter.sharedMesh = sharedVisualMesh;
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            meshCollider.convex = true;
            meshCollider.sharedMesh = sharedCollisionMesh;
            target = GetComponent<ShootTarget>();
            targetRenderer = GetComponent<MeshRenderer>();
            hitProperties = new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            if (flashing && targetRenderer != null)
            {
                targetRenderer.SetPropertyBlock(null);
                flashing = false;
            }
        }

        private void Update()
        {
            if (target == null || targetRenderer == null || !target.HasBeenHit)
            {
                return;
            }

            float currentTime = target.Session != null ? target.Session.TimeSeconds : Time.time;
            float age = currentTime - target.LastHitTime;
            if (age >= 0f && age < 0.22f)
            {
                float intensity = 1f - age / 0.22f;
                hitProperties.SetColor("_EmissionColor", new Color(intensity, intensity * 0.35f, intensity * 0.1f, 1f));
                targetRenderer.SetPropertyBlock(hitProperties);
                flashing = true;
            }
            else if (flashing)
            {
                targetRenderer.SetPropertyBlock(null);
                flashing = false;
            }
        }
    }
}
