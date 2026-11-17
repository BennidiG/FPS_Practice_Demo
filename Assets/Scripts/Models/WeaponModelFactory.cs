using UnityEngine;

namespace FpsPractice.Models
{
    /// <summary>
    /// Procedural first-person weapon meshes. Both weapons point down local +Z.
    /// Material order: Gunmetal, Polymer, Accent, Grip, Sight.
    /// </summary>
    public static class WeaponModelFactory
    {
        public const int Gunmetal = 0;
        public const int Polymer = 1;
        public const int Accent = 2;
        public const int Grip = 3;
        public const int Sight = 4;

        private const int BarrelSides = 24;
        private const int SmallCylinderSides = 16;

        public static Mesh BuildRifle()
        {
            MeshBuilder builder = new MeshBuilder(5);
            AddRifleReceiver(builder);
            AddRifleHandguardAndBarrel(builder);
            AddRifleStock(builder);
            AddRifleGripMagazineAndTrigger(builder);
            AddRifleRailAndSights(builder);
            return builder.Build("Rifle_Procedural");
        }

        public static Mesh BuildSidearm()
        {
            MeshBuilder builder = new MeshBuilder(5);
            AddSidearmSlideAndBarrel(builder);
            AddSidearmFrame(builder);
            AddSidearmGripAndMagazine(builder);
            AddSidearmTriggerGuard(builder);
            AddSidearmSights(builder);
            return builder.Build("Sidearm_Procedural");
        }

        private static void AddRifleReceiver(MeshBuilder builder)
        {
            Box(builder, new Vector3(0.0f, 0.0f, -0.055f),
                new Vector3(0.074f, 0.082f, 0.320f), 0.008f, Gunmetal);
            Box(builder, new Vector3(0.0f, 0.046f, -0.045f),
                new Vector3(0.064f, 0.030f, 0.300f), 0.005f, Gunmetal);
            Box(builder, new Vector3(0.0f, -0.047f, -0.065f),
                new Vector3(0.064f, 0.025f, 0.190f), 0.005f, Gunmetal);

            // Ejection port, left receiver plate, charging handle and selector.
            Box(builder, new Vector3(0.038f, 0.015f, 0.005f),
                new Vector3(0.003f, 0.026f, 0.092f), 0.001f, Accent);
            Box(builder, new Vector3(-0.038f, 0.006f, -0.035f),
                new Vector3(0.003f, 0.034f, 0.140f), 0.001f, Gunmetal);
            Box(builder, new Vector3(-0.047f, 0.047f, -0.130f),
                new Vector3(0.025f, 0.012f, 0.036f), 0.003f, Grip);
            Box(builder, new Vector3(0.041f, -0.025f, -0.115f),
                new Vector3(0.008f, 0.012f, 0.030f), 0.002f, Accent);
        }

        private static void AddRifleHandguardAndBarrel(MeshBuilder builder)
        {
            Box(builder, new Vector3(0.0f, 0.005f, 0.218f),
                new Vector3(0.072f, 0.074f, 0.250f), 0.009f, Polymer);
            Box(builder, new Vector3(0.0f, 0.047f, 0.215f),
                new Vector3(0.046f, 0.016f, 0.235f), 0.004f, Gunmetal);
            Box(builder, new Vector3(0.0f, -0.038f, 0.219f),
                new Vector3(0.052f, 0.015f, 0.190f), 0.004f, Grip);

            // Recessed-looking dark vent strips on both handguard sides.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    Box(builder,
                        new Vector3(side * 0.037f, 0.004f, 0.135f + i * 0.050f),
                        new Vector3(0.003f, 0.020f, 0.028f), 0.001f, Grip);
                }

                // Flush fasteners give the long handguard a manufactured scale.
                for (int i = 0; i < 2; i++)
                {
                    builder.AddCylinder(
                        new Vector3(side * 0.038f, 0.019f, 0.120f + i * 0.190f),
                        0.0055f,
                        0.004f,
                        SmallCylinderSides,
                        MeshAxis.X,
                        Gunmetal);
                }
            }

            builder.AddCylinder(new Vector3(0.0f, 0.005f, 0.323f),
                0.012f, 0.300f, BarrelSides, MeshAxis.Z, Gunmetal);
            Box(builder, new Vector3(0.0f, 0.006f, 0.343f),
                new Vector3(0.035f, 0.040f, 0.035f), 0.004f, Gunmetal);

            builder.AddFrustum(new Vector3(0.0f, 0.005f, 0.475f),
                0.018f, 0.016f, 0.055f, BarrelSides, MeshAxis.Z, Gunmetal);
            builder.AddCylinder(new Vector3(0.0f, 0.005f, 0.443f),
                0.019f, 0.008f, BarrelSides, MeshAxis.Z, Accent);
            builder.AddCylinder(new Vector3(0.0f, 0.005f, 0.503f),
                0.009f, 0.003f, BarrelSides, MeshAxis.Z, Grip);

            for (int side = -1; side <= 1; side += 2)
            {
                Box(builder, new Vector3(side * 0.0185f, 0.005f, 0.475f),
                    new Vector3(0.004f, 0.010f, 0.024f), 0.001f, Grip);
            }
        }

        private static void AddRifleStock(MeshBuilder builder)
        {
            builder.AddCylinder(new Vector3(0.0f, 0.005f, -0.232f),
                0.017f, 0.140f, BarrelSides, MeshAxis.Z, Gunmetal);
            Box(builder, new Vector3(0.0f, -0.008f, -0.366f),
                new Vector3(0.066f, 0.082f, 0.220f), 0.012f, Polymer);
            Box(builder, new Vector3(0.0f, -0.008f, -0.489f),
                new Vector3(0.072f, 0.096f, 0.026f), 0.008f, Grip);
            Box(builder, new Vector3(0.0f, 0.048f, -0.355f),
                new Vector3(0.060f, 0.028f, 0.190f), 0.007f, Polymer);
            Box(builder, new Vector3(0.0f, -0.062f, -0.335f),
                new Vector3(0.040f, 0.022f, 0.160f), 0.005f, Grip,
                Quaternion.Euler(-10.0f, 0.0f, 0.0f));

            for (int side = -1; side <= 1; side += 2)
            {
                Box(builder, new Vector3(side * 0.034f, -0.005f, -0.365f),
                    new Vector3(0.003f, 0.022f, 0.090f), 0.001f, Accent);
            }
        }

        private static void AddRifleGripMagazineAndTrigger(MeshBuilder builder)
        {
            Quaternion gripRotation = Quaternion.Euler(18.0f, 0.0f, 0.0f);
            Vector3 gripCenter = new Vector3(0.0f, -0.116f, -0.145f);
            Box(builder, gripCenter, new Vector3(0.052f, 0.135f, 0.064f),
                0.009f, Grip, gripRotation);

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 panelCenter = gripCenter + gripRotation * new Vector3(side * 0.027f, 0.0f, 0.0f);
                Box(builder, panelCenter, new Vector3(0.003f, 0.074f, 0.038f),
                    0.001f, Polymer, gripRotation);
            }

            Quaternion magazineRotation = Quaternion.Euler(-8.0f, 0.0f, 0.0f);
            Vector3 magazineCenter = new Vector3(0.0f, -0.135f, 0.025f);
            Box(builder, magazineCenter, new Vector3(0.052f, 0.180f, 0.082f),
                0.006f, Polymer, magazineRotation);
            Box(builder,
                magazineCenter + magazineRotation * new Vector3(0.0f, -0.096f, 0.0f),
                new Vector3(0.059f, 0.016f, 0.092f), 0.004f, Grip, magazineRotation);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 ribCenter = magazineCenter + magazineRotation *
                        new Vector3(side * 0.027f, -0.025f - i * 0.032f, 0.0f);
                    Box(builder, ribCenter, new Vector3(0.003f, 0.009f, 0.062f),
                        0.001f, Gunmetal, magazineRotation);
                }
            }

            Box(builder, new Vector3(0.0f, -0.082f, -0.075f),
                new Vector3(0.046f, 0.010f, 0.084f), 0.003f, Gunmetal);
            Box(builder, new Vector3(0.0f, -0.062f, -0.034f),
                new Vector3(0.046f, 0.045f, 0.009f), 0.002f, Gunmetal,
                Quaternion.Euler(-15.0f, 0.0f, 0.0f));
            Box(builder, new Vector3(0.0f, -0.060f, -0.075f),
                new Vector3(0.010f, 0.037f, 0.009f), 0.002f, Accent,
                Quaternion.Euler(-20.0f, 0.0f, 0.0f));
        }

        private static void AddRifleRailAndSights(MeshBuilder builder)
        {
            Box(builder, new Vector3(0.0f, 0.071f, 0.075f),
                new Vector3(0.034f, 0.012f, 0.510f), 0.003f, Gunmetal);
            for (int i = 0; i < 14; i++)
            {
                Box(builder, new Vector3(0.0f, 0.081f, -0.155f + i * 0.035f),
                    new Vector3(0.046f, 0.008f, 0.014f), 0.002f, Gunmetal);
            }

            // Rear aperture: two ears and a bridge leave a real open sight picture.
            Box(builder, new Vector3(0.0f, 0.094f, -0.145f),
                new Vector3(0.046f, 0.014f, 0.035f), 0.003f, Sight);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(builder, new Vector3(side * 0.017f, 0.116f, -0.145f),
                    new Vector3(0.009f, 0.032f, 0.012f), 0.002f, Sight);
            }
            Box(builder, new Vector3(0.0f, 0.132f, -0.145f),
                new Vector3(0.043f, 0.008f, 0.012f), 0.002f, Sight);

            Box(builder, new Vector3(0.0f, 0.094f, 0.302f),
                new Vector3(0.046f, 0.014f, 0.035f), 0.003f, Sight);
            Box(builder, new Vector3(0.0f, 0.117f, 0.302f),
                new Vector3(0.008f, 0.032f, 0.010f), 0.002f, Sight);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(builder, new Vector3(side * 0.018f, 0.113f, 0.302f),
                    new Vector3(0.007f, 0.025f, 0.012f), 0.002f, Sight);
            }
        }

        private static void AddSidearmSlideAndBarrel(MeshBuilder builder)
        {
            Box(builder, new Vector3(0.0f, 0.035f, -0.005f),
                new Vector3(0.058f, 0.062f, 0.400f), 0.007f, Gunmetal);
            Box(builder, new Vector3(0.0f, 0.069f, -0.005f),
                new Vector3(0.038f, 0.012f, 0.360f), 0.003f, Gunmetal);
            Box(builder, new Vector3(0.0f, 0.035f, -0.211f),
                new Vector3(0.057f, 0.058f, 0.012f), 0.003f, Gunmetal);

            Quaternion serrationRotation = Quaternion.Euler(-12.0f, 0.0f, 0.0f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    Box(builder,
                        new Vector3(side * 0.030f, 0.035f, -0.150f + i * 0.018f),
                        new Vector3(0.003f, 0.038f, 0.007f), 0.001f, Grip,
                        serrationRotation);
                }
            }

            Box(builder, new Vector3(0.030f, 0.047f, 0.025f),
                new Vector3(0.003f, 0.023f, 0.065f), 0.001f, Accent);
            builder.AddCylinder(new Vector3(0.0f, 0.035f, 0.185f),
                0.010f, 0.090f, BarrelSides, MeshAxis.Z, Gunmetal);
            builder.AddCylinder(new Vector3(0.0f, 0.035f, 0.202f),
                0.015f, 0.018f, BarrelSides, MeshAxis.Z, Accent);
            builder.AddCylinder(new Vector3(0.0f, 0.035f, 0.2315f),
                0.006f, 0.003f, BarrelSides, MeshAxis.Z, Grip);
        }

        private static void AddSidearmFrame(MeshBuilder builder)
        {
            Box(builder, new Vector3(0.0f, -0.009f, -0.015f),
                new Vector3(0.056f, 0.040f, 0.335f), 0.006f, Polymer);
            Box(builder, new Vector3(0.0f, -0.033f, 0.092f),
                new Vector3(0.052f, 0.026f, 0.135f), 0.005f, Polymer);

            for (int side = -1; side <= 1; side += 2)
            {
                Box(builder, new Vector3(side * 0.029f, -0.010f, -0.035f),
                    new Vector3(0.003f, 0.022f, 0.105f), 0.001f, Grip);
            }

            Box(builder, new Vector3(0.0f, -0.052f, 0.093f),
                new Vector3(0.032f, 0.010f, 0.095f), 0.002f, Gunmetal);
            for (int i = 0; i < 3; i++)
            {
                Box(builder, new Vector3(0.0f, -0.058f, 0.060f + i * 0.030f),
                    new Vector3(0.038f, 0.007f, 0.009f), 0.002f, Gunmetal);
            }
        }

        private static void AddSidearmGripAndMagazine(MeshBuilder builder)
        {
            Quaternion gripRotation = Quaternion.Euler(15.0f, 0.0f, 0.0f);
            Vector3 gripCenter = new Vector3(0.0f, -0.117f, -0.105f);
            Box(builder, gripCenter, new Vector3(0.054f, 0.166f, 0.074f),
                0.009f, Grip, gripRotation);

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 panelCenter = gripCenter + gripRotation * new Vector3(side * 0.028f, 0.0f, 0.0f);
                Box(builder, panelCenter, new Vector3(0.003f, 0.105f, 0.052f),
                    0.001f, Polymer, gripRotation);
            }

            // The detachable magazine is mostly concealed; its baseplate remains visible.
            Box(builder,
                gripCenter + gripRotation * new Vector3(0.0f, -0.088f, 0.0f),
                new Vector3(0.061f, 0.016f, 0.084f), 0.004f, Polymer, gripRotation);
        }

        private static void AddSidearmTriggerGuard(MeshBuilder builder)
        {
            Box(builder, new Vector3(0.0f, -0.081f, 0.005f),
                new Vector3(0.045f, 0.011f, 0.120f), 0.003f, Polymer);
            Box(builder, new Vector3(0.0f, -0.059f, 0.064f),
                new Vector3(0.045f, 0.052f, 0.011f), 0.003f, Polymer,
                Quaternion.Euler(-18.0f, 0.0f, 0.0f));
            Box(builder, new Vector3(0.0f, -0.057f, -0.053f),
                new Vector3(0.045f, 0.041f, 0.010f), 0.003f, Polymer,
                Quaternion.Euler(12.0f, 0.0f, 0.0f));
            Box(builder, new Vector3(0.0f, -0.052f, -0.008f),
                new Vector3(0.010f, 0.039f, 0.009f), 0.002f, Accent,
                Quaternion.Euler(-20.0f, 0.0f, 0.0f));
        }

        private static void AddSidearmSights(MeshBuilder builder)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Box(builder, new Vector3(side * 0.016f, 0.085f, -0.168f),
                    new Vector3(0.012f, 0.022f, 0.018f), 0.003f, Sight);
            }
            Box(builder, new Vector3(0.0f, 0.083f, 0.158f),
                new Vector3(0.010f, 0.020f, 0.018f), 0.003f, Sight);
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

        private static void Box(
            MeshBuilder builder,
            Vector3 center,
            Vector3 size,
            float bevel,
            int material,
            Quaternion rotation)
        {
            builder.AddChamferBox(center, size, bevel, material, rotation);
        }
    }
}
