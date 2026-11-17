using UnityEngine;

namespace FpsPractice.Models
{
    /// <summary>
    /// A compact beveled target disc. Local Y is thickness and the scored face is -Y.
    /// Material order: Outer, White, Red, Gold, Dark, Blue.
    /// </summary>
    public static class TargetModelFactory
    {
        public const int Outer = 0;
        public const int White = 1;
        public const int Red = 2;
        public const int Gold = 3;
        public const int Dark = 4;
        public const int Blue = 5;

        private const int RimSegments = 32;
        private const float OuterRadius = 0.650f;
        private const float FaceRadius = 0.590f;
        private const float HalfThickness = 0.080f;

        public static Mesh BuildTarget()
        {
            MeshBuilder builder = new MeshBuilder(6);

            // The body is one closed, beveled cylinder with no stand, cutouts or recesses.
            // Its convex silhouette remains suitable for a convex MeshCollider.
            builder.AddFrustum(
                Vector3.zero,
                OuterRadius,
                OuterRadius,
                0.120f,
                RimSegments,
                MeshAxis.Y,
                Outer,
                false,
                false);
            builder.AddFrustum(
                new Vector3(0.0f, -0.070f, 0.0f),
                FaceRadius,
                OuterRadius,
                0.020f,
                RimSegments,
                MeshAxis.Y,
                Outer,
                false,
                false);
            builder.AddFrustum(
                new Vector3(0.0f, 0.070f, 0.0f),
                OuterRadius,
                FaceRadius,
                0.020f,
                RimSegments,
                MeshAxis.Y,
                Outer,
                false,
                false);

            AddDisc(builder, FaceRadius, HalfThickness, Vector3.up, Dark);

            // These coplanar annuli replace the front cap, so there is no z-fighting
            // from laying colored cylinders on top of an existing circular face.
            AddAnnulus(builder, 0.520f, FaceRadius, -HalfThickness, White);
            AddAnnulus(builder, 0.380f, 0.520f, -HalfThickness, Blue);
            AddAnnulus(builder, 0.240f, 0.380f, -HalfThickness, White);
            AddAnnulus(builder, 0.100f, 0.240f, -HalfThickness, Red);
            AddDisc(builder, 0.100f, -HalfThickness, Vector3.down, Gold);

            // Shallow hex heads read as bolts without changing the round outer silhouette.
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2.0f / 8.0f + Mathf.PI * 0.125f;
                Vector3 position = RadialPoint(0.555f, -HalfThickness - 0.0015f, angle);
                builder.AddCylinder(
                    position,
                    0.014f,
                    0.003f,
                    6,
                    MeshAxis.Y,
                    Dark);
            }

            return builder.Build("Target_Procedural");
        }

        public static Mesh BuildCollisionTarget()
        {
            MeshBuilder builder = new MeshBuilder(1);
            builder.AddCylinder(Vector3.zero, OuterRadius, HalfThickness * 2.0f, RimSegments, MeshAxis.Y, Outer);
            return builder.Build("Target_Collision");
        }

        private static void AddAnnulus(
            MeshBuilder builder,
            float innerRadius,
            float outerRadius,
            float y,
            int material)
        {
            float angleStep = Mathf.PI * 2.0f / RimSegments;
            for (int i = 0; i < RimSegments; i++)
            {
                float angle = i * angleStep;
                float nextAngle = (i + 1) * angleStep;
                Vector3 inner = RadialPoint(innerRadius, y, angle);
                Vector3 outer = RadialPoint(outerRadius, y, angle);
                Vector3 innerNext = RadialPoint(innerRadius, y, nextAngle);
                Vector3 outerNext = RadialPoint(outerRadius, y, nextAngle);
                builder.AddOrientedQuad(
                    material,
                    inner,
                    outer,
                    outerNext,
                    innerNext,
                    Vector3.down);
            }
        }

        private static void AddDisc(
            MeshBuilder builder,
            float radius,
            float y,
            Vector3 outward,
            int material)
        {
            float angleStep = Mathf.PI * 2.0f / RimSegments;
            Vector3 center = new Vector3(0.0f, y, 0.0f);
            for (int i = 0; i < RimSegments; i++)
            {
                float angle = i * angleStep;
                float nextAngle = (i + 1) * angleStep;
                builder.AddOrientedTriangle(
                    material,
                    center,
                    RadialPoint(radius, y, angle),
                    RadialPoint(radius, y, nextAngle),
                    outward);
            }
        }

        private static Vector3 RadialPoint(float radius, float y, float angle)
        {
            // Matches MeshBuilder's Y-axis basis: Z * cos(angle) + X * sin(angle).
            return new Vector3(radius * Mathf.Sin(angle), y, radius * Mathf.Cos(angle));
        }
    }
}
