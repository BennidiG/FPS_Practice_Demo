using UnityEngine;

namespace FpsPractice.Models
{
    /// <summary>
    /// Procedural indoor shooting-range shell and fixtures, in meters.
    /// Material order: Floor, Wall, Metal, Accent, SafetyYellow, Rubber,
    /// Light, White, WarningRed. The mesh contains no scene objects.
    /// </summary>
    public static class RangeModelFactory
    {
        public const int MaterialSlotCount = 9;

        private const int Floor = 0;
        private const int Wall = 1;
        private const int Metal = 2;
        private const int Accent = 3;
        private const int SafetyYellow = 4;
        private const int Rubber = 5;
        private const int Light = 6;
        private const int White = 7;
        private const int WarningRed = 8;

        private static readonly float[] LaneCenters = { -6.0f, -2.0f, 2.0f, 6.0f };
        private static readonly float[] LaneDividers = { -4.0f, 0.0f, 4.0f };
        private static readonly float[] LightRows = { -2.0f, -6.0f, -10.0f, -14.0f, -18.0f };
        private static readonly float[] ColumnRows = { -0.75f, -5.35f, -10.75f, -15.75f, -20.75f };
        private static readonly float[] PanelRows = { -2.8f, -8.2f, -12.4f, -18.2f };

        public static Mesh Build()
        {
            MeshBuilder builder = new MeshBuilder(MaterialSlotCount);

            AddShell(builder);
            AddStructure(builder);
            AddFloorMarkings(builder);
            AddLaneDividers(builder);
            AddBulletTraps(builder);
            AddCeilingLights(builder);
            AddVentilation(builder);
            AddWallPanels(builder);
            AddRearConsole(builder);
            AddSideStools(builder);

            return builder.Build("Range_Procedural");
        }

        private static void AddShell(MeshBuilder builder)
        {
            // The six architectural surfaces use the requested centers and sizes exactly.
            Box(builder, new Vector3(0.0f, -0.1f, -10.0f),
                new Vector3(20.0f, 0.2f, 22.0f), 0.0f, Floor);
            Box(builder, new Vector3(0.0f, 7.05f, -10.0f),
                new Vector3(20.0f, 0.2f, 22.0f), 0.0f, Wall);

            for (int side = -1; side <= 1; side += 2)
            {
                Box(builder, new Vector3(side * 10.1f, 3.5f, -10.0f),
                    new Vector3(0.2f, 7.0f, 22.0f), 0.0f, Wall);
            }

            Box(builder, new Vector3(0.0f, 3.5f, 1.1f),
                new Vector3(20.0f, 7.0f, 0.2f), 0.0f, Wall);
            Box(builder, new Vector3(0.0f, 3.5f, -21.1f),
                new Vector3(20.0f, 7.0f, 0.2f), 0.0f, Wall);

            // Dark lower wall protection stops at the inside faces of the shell.
            for (int side = -1; side <= 1; side += 2)
            {
                Box(builder, new Vector3(side * 9.96f, 0.60f, -10.0f),
                    new Vector3(0.08f, 1.20f, 21.84f), 0.015f, Metal);
                Box(builder, new Vector3(side * 9.915f, 1.14f, -10.0f),
                    new Vector3(0.01f, 0.045f, 21.50f), 0.0f, Accent);
            }

            Box(builder, new Vector3(0.0f, 0.60f, 0.96f),
                new Vector3(19.84f, 1.20f, 0.08f), 0.015f, Metal);
            Box(builder, new Vector3(0.0f, 0.60f, -20.96f),
                new Vector3(19.84f, 1.20f, 0.08f), 0.015f, Metal);
        }

        private static void AddStructure(MeshBuilder builder)
        {
            // Five-meter nominal column rhythm, shifted slightly to clear the lights.
            for (int i = 0; i < ColumnRows.Length; i++)
            {
                float z = ColumnRows[i];
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(builder, new Vector3(side * 9.78f, 3.5f, z),
                        new Vector3(0.44f, 7.0f, 0.36f), 0.025f, Metal);
                }

                Box(builder, new Vector3(0.0f, 6.73f, z),
                    new Vector3(20.0f, 0.34f, 0.36f), 0.025f, Metal);
            }
        }

        private static void AddFloorMarkings(MeshBuilder builder)
        {
            for (int i = 0; i < LaneDividers.Length; i++)
            {
                Box(builder, new Vector3(LaneDividers[i], 0.006f, -10.0f),
                    new Vector3(0.065f, 0.012f, 21.40f), 0.0f, White);
            }

            Box(builder, new Vector3(0.0f, 0.009f, -8.0f),
                new Vector3(18.40f, 0.018f, 0.16f), 0.0f, SafetyYellow);
        }

        private static void AddLaneDividers(MeshBuilder builder)
        {
            for (int i = 0; i < LaneDividers.Length; i++)
            {
                Box(builder, new Vector3(LaneDividers[i], 0.65f, -3.0f),
                    new Vector3(0.12f, 1.30f, 6.0f), 0.018f, Wall);
            }
        }

        private static void AddBulletTraps(MeshBuilder builder)
        {
            for (int i = 0; i < LaneCenters.Length; i++)
            {
                float x = LaneCenters[i];
                // Covers targets centered from y=2 through y=6, including their 0.65 m radius.
                Box(builder, new Vector3(x, 3.35f, 0.85f),
                    new Vector3(2.80f, 6.70f, 0.14f), 0.025f, Rubber);

                for (int side = -1; side <= 1; side += 2)
                {
                    Box(builder, new Vector3(x + side * 1.47f, 3.39f, 0.85f),
                        new Vector3(0.12f, 6.78f, 0.22f), 0.012f, Metal);
                    Box(builder, new Vector3(x + side * 1.47f, 5.90f, 0.731f),
                        new Vector3(0.075f, 0.24f, 0.018f), 0.0f, WarningRed);
                }

                Box(builder, new Vector3(x, 6.74f, 0.85f),
                    new Vector3(3.06f, 0.08f, 0.22f), 0.012f, Metal);
                Box(builder, new Vector3(x, 0.07f, 0.85f),
                    new Vector3(3.06f, 0.14f, 0.22f), 0.012f, Metal);
                Box(builder, new Vector3(x, 6.74f, 0.731f),
                    new Vector3(2.82f, 0.065f, 0.018f), 0.0f, SafetyYellow);
            }
        }

        private static void AddCeilingLights(MeshBuilder builder)
        {
            for (int row = 0; row < LightRows.Length; row++)
            {
                for (int lane = 0; lane < LaneCenters.Length; lane++)
                {
                    Vector3 center = new Vector3(LaneCenters[lane], 0.0f, LightRows[row]);
                    Box(builder, center + new Vector3(0.0f, 6.91f, 0.0f),
                        new Vector3(3.14f, 0.16f, 0.72f), 0.018f, Metal);

                    // The lower face of every diffuser is exactly y = 6.82.
                    Box(builder, center + new Vector3(0.0f, 6.85f, 0.0f),
                        new Vector3(3.0f, 0.06f, 0.60f), 0.0f, Light);
                }
            }
        }

        private static void AddVentilation(MeshBuilder builder)
        {
            float[] collarRows = { -2.25f, -7.25f, -12.25f, -17.25f };
            float[] grilleRows = { -4.5f, -14.5f };

            for (int side = -1; side <= 1; side += 2)
            {
                float ductX = side * 9.28f;
                builder.AddCylinder(new Vector3(ductX, 6.12f, -10.0f),
                    0.28f, 19.60f, 16, MeshAxis.Z, Metal);

                for (int i = 0; i < collarRows.Length; i++)
                {
                    builder.AddFrustum(new Vector3(ductX, 6.12f, collarRows[i]),
                        0.305f, 0.305f, 0.10f, 16, MeshAxis.Z, Metal, false, false);
                }

                for (int i = 0; i < grilleRows.Length; i++)
                {
                    float z = grilleRows[i];
                    builder.AddCylinder(new Vector3(ductX, 5.50f, z),
                        0.20f, 0.80f, 12, MeshAxis.Y, Metal);
                    builder.AddCylinder(new Vector3(side * 9.60f, 5.10f, z),
                        0.20f, 0.56f, 12, MeshAxis.X, Metal);

                    Box(builder, new Vector3(side * 9.94f, 5.10f, z),
                        new Vector3(0.08f, 0.72f, 1.04f), 0.01f, Metal);
                    Box(builder, new Vector3(side * 9.885f, 5.10f, z),
                        new Vector3(0.02f, 0.60f, 0.92f), 0.0f, Rubber);

                    for (int slat = 0; slat < 5; slat++)
                    {
                        Box(builder,
                            new Vector3(side * 9.855f, 4.86f + slat * 0.12f, z),
                            new Vector3(0.03f, 0.035f, 0.88f), 0.0f, Metal);
                    }
                }
            }
        }

        private static void AddWallPanels(MeshBuilder builder)
        {
            // Flat acoustic panels add rhythm without signage or visual clutter.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < PanelRows.Length; i++)
                {
                    float z = PanelRows[i];
                    Box(builder, new Vector3(side * 9.95f, 3.25f, z),
                        new Vector3(0.08f, 1.90f, 2.30f), 0.012f, Metal);
                    Box(builder, new Vector3(side * 9.90f, 3.25f, z),
                        new Vector3(0.025f, 1.72f, 2.12f), 0.0f, White);
                    Box(builder, new Vector3(side * 9.882f, 2.52f, z),
                        new Vector3(0.012f, 0.055f, 1.90f), 0.0f, Accent);
                }
            }
        }

        private static void AddRearConsole(MeshBuilder builder)
        {
            Box(builder, new Vector3(0.0f, 0.82f, -19.20f),
                new Vector3(6.0f, 0.14f, 1.20f), 0.025f, White);

            for (int xSide = -1; xSide <= 1; xSide += 2)
            {
                for (int zSide = -1; zSide <= 1; zSide += 2)
                {
                    Box(builder,
                        new Vector3(xSide * 2.72f, 0.375f, -19.20f + zSide * 0.45f),
                        new Vector3(0.14f, 0.75f, 0.14f), 0.015f, Metal);
                }
            }

            Box(builder, new Vector3(0.0f, 0.39f, -19.38f),
                new Vector3(3.50f, 0.70f, 0.62f), 0.035f, Metal);
            for (int panel = -1; panel <= 1; panel++)
            {
                Box(builder, new Vector3(panel * 1.12f, 0.43f, -19.056f),
                    new Vector3(1.04f, 0.48f, 0.025f), 0.008f, White);
            }
            Box(builder, new Vector3(0.0f, 0.67f, -19.045f),
                new Vector3(3.10f, 0.035f, 0.015f), 0.0f, Accent);

            for (int monitor = -1; monitor <= 1; monitor++)
            {
                float x = monitor * 1.65f;
                Box(builder, new Vector3(x, 0.91f, -19.55f),
                    new Vector3(0.38f, 0.04f, 0.28f), 0.008f, Metal);
                builder.AddCylinder(new Vector3(x, 1.01f, -19.55f),
                    0.045f, 0.24f, 12, MeshAxis.Y, Metal);
                Box(builder, new Vector3(x, 1.43f, -19.55f),
                    new Vector3(1.42f, 0.72f, 0.10f), 0.025f, Metal);
                Box(builder, new Vector3(x, 1.43f, -19.493f),
                    new Vector3(1.28f, 0.58f, 0.014f), 0.0f, Accent);
            }

            Box(builder, new Vector3(0.0f, 0.916f, -18.88f),
                new Vector3(1.15f, 0.052f, 0.32f), 0.008f, Metal);
            builder.AddCylinder(new Vector3(2.55f, 0.915f, -18.95f),
                0.055f, 0.05f, 12, MeshAxis.Y, WarningRed);
        }

        private static void AddSideStools(MeshBuilder builder)
        {
            for (int stool = 0; stool < 2; stool++)
            {
                float x = stool == 0 ? -4.25f : 4.25f;
                const float z = -19.10f;
                builder.AddCylinder(new Vector3(x, 0.57f, z),
                    0.34f, 0.14f, 16, MeshAxis.Y, Rubber);
                builder.AddCylinder(new Vector3(x, 0.29f, z),
                    0.055f, 0.44f, 12, MeshAxis.Y, Metal);
                builder.AddCylinder(new Vector3(x, 0.045f, z),
                    0.27f, 0.09f, 16, MeshAxis.Y, Metal);
                builder.AddCylinder(new Vector3(x, 0.26f, z),
                    0.025f, 0.48f, 12, MeshAxis.X, Metal);
            }
        }

        private static void Box(
            MeshBuilder builder,
            Vector3 center,
            Vector3 size,
            float bevel,
            int material)
        {
            builder.AddChamferBox(center, size, bevel, material);
        }
    }
}
