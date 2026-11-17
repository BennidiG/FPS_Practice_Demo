using System;
using System.Collections.Generic;
using UnityEngine;

namespace FpsPractice.Models
{
    public enum MeshAxis
    {
        X = 0,
        Y = 1,
        Z = 2
    }

    /// <summary>
    /// Small runtime mesh builder. Every face owns its vertices so recalculated
    /// normals remain hard-edged and every submesh has independent UVs.
    /// </summary>
    public sealed class MeshBuilder
    {
        private readonly List<Vector3> vertices;
        private readonly List<Vector2> uvs;
        private readonly List<int>[] submeshTriangles;

        public MeshBuilder(int materialSlotCount)
        {
            if (materialSlotCount < 1)
            {
                throw new ArgumentOutOfRangeException("materialSlotCount");
            }

            vertices = new List<Vector3>();
            uvs = new List<Vector2>();
            submeshTriangles = new List<int>[materialSlotCount];
            for (int i = 0; i < materialSlotCount; i++)
            {
                submeshTriangles[i] = new List<int>();
            }
        }

        public int VertexCount
        {
            get { return vertices.Count; }
        }

        public int MaterialSlotCount
        {
            get { return submeshTriangles.Length; }
        }

        public int AddVertex(Vector3 position, Vector2 uv)
        {
            int index = vertices.Count;
            vertices.Add(position);
            uvs.Add(uv);
            return index;
        }

        // Indices use Unity's clockwise front-face convention.
        public void AddTriangle(int materialIndex, int a, int b, int c)
        {
            ValidateMaterial(materialIndex);
            ValidateVertex(a);
            ValidateVertex(b);
            ValidateVertex(c);
            submeshTriangles[materialIndex].Add(a);
            submeshTriangles[materialIndex].Add(b);
            submeshTriangles[materialIndex].Add(c);
        }

        public void AddTriangle(int materialIndex, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 outward = Vector3.Cross(c - a, b - a);
            Vector3 u;
            Vector3 v;
            GetProjectionBasis(outward, out u, out v);

            int start = vertices.Count;
            AddVertex(a, ProjectUv(a, a, u, v));
            AddVertex(b, ProjectUv(b, a, u, v));
            AddVertex(c, ProjectUv(c, a, u, v));
            AddTriangle(materialIndex, start, start + 1, start + 2);
        }

        public void AddQuad(int materialIndex, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 outward = Vector3.Cross(c - a, b - a);
            Vector3 u;
            Vector3 v;
            GetProjectionBasis(outward, out u, out v);

            int start = vertices.Count;
            AddVertex(a, ProjectUv(a, a, u, v));
            AddVertex(b, ProjectUv(b, a, u, v));
            AddVertex(c, ProjectUv(c, a, u, v));
            AddVertex(d, ProjectUv(d, a, u, v));
            AddTriangle(materialIndex, start, start + 1, start + 2);
            AddTriangle(materialIndex, start, start + 2, start + 3);
        }

        // The expected outward normal removes handedness ambiguity for generated parts.
        public void AddOrientedTriangle(
            int materialIndex,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 outwardNormal)
        {
            Vector3 inward = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(inward, outwardNormal) > 0.0f)
            {
                AddTriangle(materialIndex, a, c, b);
            }
            else
            {
                AddTriangle(materialIndex, a, b, c);
            }
        }

        public void AddOrientedQuad(
            int materialIndex,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            Vector3 outwardNormal)
        {
            Vector3 inward = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(inward, outwardNormal) > 0.0f)
            {
                AddQuad(materialIndex, a, d, c, b);
            }
            else
            {
                AddQuad(materialIndex, a, b, c, d);
            }
        }

        public void AddChamferBox(
            Vector3 center,
            Vector3 size,
            float bevel,
            int materialIndex)
        {
            AddChamferBox(center, size, bevel, materialIndex, Quaternion.identity);
        }

        public void AddChamferBox(
            Vector3 center,
            Vector3 size,
            float bevel,
            int materialIndex,
            Quaternion rotation)
        {
            ValidateMaterial(materialIndex);
            if (size.x <= 0.0f || size.y <= 0.0f || size.z <= 0.0f)
            {
                throw new ArgumentException("Chamfer box dimensions must be positive.", "size");
            }

            Vector3 half = size * 0.5f;
            float minimumHalf = Mathf.Min(half.x, Mathf.Min(half.y, half.z));
            float edge = Mathf.Clamp(bevel, 0.0f, minimumHalf * 0.98f);

            // Six inset primary faces.
            for (int axis = 0; axis < 3; axis++)
            {
                int uAxis = (axis + 1) % 3;
                int vAxis = (axis + 2) % 3;
                for (int signIndex = 0; signIndex < 2; signIndex++)
                {
                    float sign = signIndex == 0 ? -1.0f : 1.0f;
                    Vector3 p00 = Vector3.zero;
                    Vector3 p10 = Vector3.zero;
                    Vector3 p11 = Vector3.zero;
                    Vector3 p01 = Vector3.zero;

                    p00[axis] = sign * half[axis];
                    p10[axis] = sign * half[axis];
                    p11[axis] = sign * half[axis];
                    p01[axis] = sign * half[axis];

                    p00[uAxis] = -half[uAxis] + edge;
                    p10[uAxis] = half[uAxis] - edge;
                    p11[uAxis] = half[uAxis] - edge;
                    p01[uAxis] = -half[uAxis] + edge;

                    p00[vAxis] = -half[vAxis] + edge;
                    p10[vAxis] = -half[vAxis] + edge;
                    p11[vAxis] = half[vAxis] - edge;
                    p01[vAxis] = half[vAxis] - edge;

                    Vector3 normal = Vector3.zero;
                    normal[axis] = sign;
                    AddOrientedQuad(
                        materialIndex,
                        Transform(center, rotation, p00),
                        Transform(center, rotation, p10),
                        Transform(center, rotation, p11),
                        Transform(center, rotation, p01),
                        rotation * normal);
                }
            }

            if (edge <= 0.000001f)
            {
                return;
            }

            // Twelve flat bevel strips along the original box edges.
            for (int aAxis = 0; aAxis < 3; aAxis++)
            {
                for (int bAxis = aAxis + 1; bAxis < 3; bAxis++)
                {
                    int runAxis = 3 - aAxis - bAxis;
                    for (int aSignIndex = 0; aSignIndex < 2; aSignIndex++)
                    {
                        for (int bSignIndex = 0; bSignIndex < 2; bSignIndex++)
                        {
                            float aSign = aSignIndex == 0 ? -1.0f : 1.0f;
                            float bSign = bSignIndex == 0 ? -1.0f : 1.0f;

                            Vector3 a0 = Vector3.zero;
                            a0[aAxis] = aSign * half[aAxis];
                            a0[bAxis] = bSign * (half[bAxis] - edge);
                            a0[runAxis] = -half[runAxis] + edge;
                            Vector3 a1 = a0;
                            a1[runAxis] = half[runAxis] - edge;

                            Vector3 b0 = Vector3.zero;
                            b0[aAxis] = aSign * (half[aAxis] - edge);
                            b0[bAxis] = bSign * half[bAxis];
                            b0[runAxis] = -half[runAxis] + edge;
                            Vector3 b1 = b0;
                            b1[runAxis] = half[runAxis] - edge;

                            Vector3 normal = Vector3.zero;
                            normal[aAxis] = aSign;
                            normal[bAxis] = bSign;
                            AddOrientedQuad(
                                materialIndex,
                                Transform(center, rotation, a0),
                                Transform(center, rotation, a1),
                                Transform(center, rotation, b1),
                                Transform(center, rotation, b0),
                                rotation * normal);
                        }
                    }
                }
            }

            // Eight triangular corner facets close the beveled shell.
            for (int xIndex = 0; xIndex < 2; xIndex++)
            {
                for (int yIndex = 0; yIndex < 2; yIndex++)
                {
                    for (int zIndex = 0; zIndex < 2; zIndex++)
                    {
                        float xSign = xIndex == 0 ? -1.0f : 1.0f;
                        float ySign = yIndex == 0 ? -1.0f : 1.0f;
                        float zSign = zIndex == 0 ? -1.0f : 1.0f;

                        Vector3 xPoint = new Vector3(
                            xSign * half.x,
                            ySign * (half.y - edge),
                            zSign * (half.z - edge));
                        Vector3 yPoint = new Vector3(
                            xSign * (half.x - edge),
                            ySign * half.y,
                            zSign * (half.z - edge));
                        Vector3 zPoint = new Vector3(
                            xSign * (half.x - edge),
                            ySign * (half.y - edge),
                            zSign * half.z);

                        AddOrientedTriangle(
                            materialIndex,
                            Transform(center, rotation, xPoint),
                            Transform(center, rotation, yPoint),
                            Transform(center, rotation, zPoint),
                            rotation * new Vector3(xSign, ySign, zSign));
                    }
                }
            }
        }

        public void AddCylinder(
            Vector3 center,
            float radius,
            float length,
            int sides,
            MeshAxis axis,
            int materialIndex)
        {
            AddFrustum(
                center,
                radius,
                radius,
                length,
                sides,
                axis,
                materialIndex,
                true,
                true);
        }

        public void AddFrustum(
            Vector3 center,
            float negativeRadius,
            float positiveRadius,
            float length,
            int sides,
            MeshAxis axis,
            int materialIndex)
        {
            AddFrustum(
                center,
                negativeRadius,
                positiveRadius,
                length,
                sides,
                axis,
                materialIndex,
                true,
                true);
        }

        public void AddFrustum(
            Vector3 center,
            float negativeRadius,
            float positiveRadius,
            float length,
            int sides,
            MeshAxis axis,
            int materialIndex,
            bool capNegative,
            bool capPositive)
        {
            ValidateMaterial(materialIndex);
            if (negativeRadius <= 0.0f || positiveRadius <= 0.0f)
            {
                throw new ArgumentException("Frustum radii must be positive.");
            }
            if (length <= 0.0f)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            if (sides < 3)
            {
                throw new ArgumentOutOfRangeException("sides");
            }

            Vector3 direction;
            Vector3 basisU;
            Vector3 basisV;
            GetAxisBasis(axis, out direction, out basisU, out basisV);

            Vector3 negativeCenter = center - direction * (length * 0.5f);
            Vector3 positiveCenter = center + direction * (length * 0.5f);
            float angleStep = Mathf.PI * 2.0f / sides;

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                float angle = i * angleStep;
                float nextAngle = next * angleStep;
                Vector3 radial = basisU * Mathf.Cos(angle) + basisV * Mathf.Sin(angle);
                Vector3 nextRadial = basisU * Mathf.Cos(nextAngle) + basisV * Mathf.Sin(nextAngle);

                Vector3 negative = negativeCenter + radial * negativeRadius;
                Vector3 negativeNext = negativeCenter + nextRadial * negativeRadius;
                Vector3 positive = positiveCenter + radial * positiveRadius;
                Vector3 positiveNext = positiveCenter + nextRadial * positiveRadius;

                Vector3 sideNormal = (radial + nextRadial).normalized;
                sideNormal += direction * ((negativeRadius - positiveRadius) / length);
                AddOrientedQuad(
                    materialIndex,
                    negative,
                    negativeNext,
                    positiveNext,
                    positive,
                    sideNormal);

                if (capNegative)
                {
                    AddOrientedTriangle(
                        materialIndex,
                        negativeCenter,
                        negative,
                        negativeNext,
                        -direction);
                }
                if (capPositive)
                {
                    AddOrientedTriangle(
                        materialIndex,
                        positiveCenter,
                        positive,
                        positiveNext,
                        direction);
                }
            }
        }

        public Mesh Build()
        {
            return Build("ProceduralMesh");
        }

        public Mesh Build(string meshName)
        {
            Mesh mesh = new Mesh();
            mesh.name = string.IsNullOrEmpty(meshName) ? "ProceduralMesh" : meshName;
            if (vertices.Count > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = submeshTriangles.Length;
            for (int i = 0; i < submeshTriangles.Length; i++)
            {
                mesh.SetTriangles(submeshTriangles[i], i, false);
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static Vector3 Transform(Vector3 center, Quaternion rotation, Vector3 point)
        {
            return center + rotation * point;
        }

        private static void GetAxisBasis(
            MeshAxis axis,
            out Vector3 direction,
            out Vector3 basisU,
            out Vector3 basisV)
        {
            switch (axis)
            {
                case MeshAxis.X:
                    direction = Vector3.right;
                    basisU = Vector3.up;
                    basisV = Vector3.forward;
                    break;
                case MeshAxis.Y:
                    direction = Vector3.up;
                    basisU = Vector3.forward;
                    basisV = Vector3.right;
                    break;
                case MeshAxis.Z:
                    direction = Vector3.forward;
                    basisU = Vector3.right;
                    basisV = Vector3.up;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("axis");
            }
        }

        private static void GetProjectionBasis(
            Vector3 normal,
            out Vector3 basisU,
            out Vector3 basisV)
        {
            Vector3 normalized = normal.sqrMagnitude > 0.000000001f
                ? normal.normalized
                : Vector3.up;
            Vector3 reference = Mathf.Abs(normalized.y) < 0.9f
                ? Vector3.up
                : Vector3.right;
            basisU = Vector3.Cross(reference, normalized).normalized;
            basisV = Vector3.Cross(normalized, basisU).normalized;
        }

        private static Vector2 ProjectUv(
            Vector3 point,
            Vector3 origin,
            Vector3 basisU,
            Vector3 basisV)
        {
            Vector3 offset = point - origin;
            return new Vector2(Vector3.Dot(offset, basisU), Vector3.Dot(offset, basisV));
        }

        private void ValidateMaterial(int materialIndex)
        {
            if (materialIndex < 0 || materialIndex >= submeshTriangles.Length)
            {
                throw new ArgumentOutOfRangeException("materialIndex");
            }
        }

        private void ValidateVertex(int index)
        {
            if (index < 0 || index >= vertices.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
        }
    }
}
