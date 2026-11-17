using NUnit.Framework;
using UnityEngine;
using FpsPractice.Models;
using System.Collections.Generic;

namespace FpsPractice.Tests.EditMode
{
    public sealed class CoreLogicTests
    {
        [Test]
        public void Settings_ClampOutOfRangeValues()
        {
            Assert.AreEqual(0.1f, GameSettings.SanitizeMouseSensitivity(-5f));
            Assert.AreEqual(10f, GameSettings.SanitizeMouseSensitivity(100f));
            Assert.AreEqual(1f, GameSettings.SanitizeMovementSpeed(0f));
            Assert.AreEqual(20f, GameSettings.SanitizeMovementSpeed(50f));
        }

        [Test]
        public void Settings_ReplaceNonFiniteValuesWithDefaults()
        {
            Assert.AreEqual(GameSettings.DefaultMouseSensitivity, GameSettings.SanitizeMouseSensitivity(float.NaN));
            Assert.AreEqual(GameSettings.DefaultMovementSpeed, GameSettings.SanitizeMovementSpeed(float.PositiveInfinity));
        }

        [Test]
        public void LookAngles_NormalizeInitialRotation()
        {
            LookAngles angles = new LookAngles(new Vector3(370f, -450f, 25f));
            Assert.AreEqual(10f, angles.Pitch, 0.001f);
            Assert.AreEqual(-90f, angles.Yaw, 0.001f);
        }

        [Test]
        public void LookAngles_ClampPitchAndWrapYaw()
        {
            LookAngles angles = new LookAngles(Vector3.zero);
            angles.Apply(new Vector2(100f, 100f), 2f, new Vector2(-89f, 89f));
            Assert.AreEqual(-89f, angles.Pitch, 0.001f);
            Assert.AreEqual(-160f, angles.Yaw, 0.001f);

            angles.Apply(new Vector2(-100f, -100f), 2f, new Vector2(-89f, 89f));
            Assert.AreEqual(89f, angles.Pitch, 0.001f);
            Assert.AreEqual(0f, angles.Yaw, 0.001f);
        }

        [Test]
        public void LookAngles_IgnoreNonFiniteInput()
        {
            LookAngles angles = new LookAngles(new Vector3(5f, 10f, 0f));
            angles.Apply(new Vector2(float.NaN, float.PositiveInfinity), 2f, new Vector2(-89f, 89f));
            Assert.AreEqual(5f, angles.Pitch, 0.001f);
            Assert.AreEqual(10f, angles.Yaw, 0.001f);
        }

        [Test]
        public void Stats_TrackAccuracyAndReset()
        {
            PracticeStats stats = new PracticeStats();
            Assert.AreEqual(0f, stats.Accuracy);
            stats.RegisterShot(true);
            stats.RegisterShot(false);
            stats.RegisterShot(true);
            Assert.AreEqual(3, stats.Shots);
            Assert.AreEqual(2, stats.Hits);
            Assert.AreEqual(2f / 3f, stats.Accuracy, 0.001f);

            stats.Reset();
            Assert.AreEqual(0, stats.Shots);
            Assert.AreEqual(0, stats.Hits);
            Assert.AreEqual(0f, stats.Accuracy);
        }

        [Test]
        public void WeaponState_EnforcesCadenceAmmoAndReload()
        {
            WeaponState state = new WeaponState(2, 600f, 1f);
            Assert.IsTrue(state.TryFire(0f));
            Assert.IsFalse(state.TryFire(0.05f));
            Assert.IsTrue(state.TryFire(0.1f));
            Assert.AreEqual(0, state.CurrentAmmo);
            Assert.IsTrue(state.IsReloading);
            Assert.IsFalse(state.TryFire(0.5f));

            state.Update(1.1f);
            Assert.AreEqual(2, state.CurrentAmmo);
            Assert.IsFalse(state.IsReloading);
            Assert.IsTrue(state.TryFire(1.1f));
        }

        [Test]
        public void WeaponState_ResetRestoresReadyState()
        {
            WeaponState state = new WeaponState(3, 300f, 1f);
            Assert.IsTrue(state.TryFire(0f));
            state.BeginReload(0f);
            Assert.IsTrue(state.IsReloading);
            state.CancelReload();
            Assert.IsFalse(state.IsReloading);
            state.BeginReload(0f);

            state.Reset();
            Assert.AreEqual(3, state.CurrentAmmo);
            Assert.IsFalse(state.IsReloading);
            Assert.IsTrue(state.TryFire(0f));
        }

        [Test]
        public void TargetMesh_HasDetailedGeometryAndSixMaterialSlots()
        {
            Mesh mesh = TargetModelFactory.BuildTarget();
            Assert.Greater(mesh.vertexCount, 200);
            Assert.AreEqual(6, mesh.subMeshCount);
            AssertGeometryIsFiniteAndPopulated(mesh);

            Mesh collision = TargetModelFactory.BuildCollisionTarget();
            Assert.AreEqual(1, collision.subMeshCount);
            Assert.Greater(collision.vertexCount, 0);
            Assert.IsTrue(collision.vertexCount < mesh.vertexCount);
            AssertGeometryIsFiniteAndPopulated(collision);
        }

        [Test]
        public void WeaponMeshes_HaveDetailedGeometryAndFiveMaterialSlots()
        {
            Mesh rifle = WeaponModelFactory.BuildRifle();
            Mesh sidearm = WeaponModelFactory.BuildSidearm();
            Assert.Greater(rifle.vertexCount, 500);
            Assert.Greater(sidearm.vertexCount, 300);
            Assert.AreEqual(5, rifle.subMeshCount);
            Assert.AreEqual(5, sidearm.subMeshCount);
            AssertGeometryIsFiniteAndPopulated(rifle);
            AssertGeometryIsFiniteAndPopulated(sidearm);
        }

        [Test]
        public void AimSensitivity_ConvertsCmPer360()
        {
            Assert.AreEqual(0.12f, AimSensitivity.DegreesPerCount(30f), 0.0001f);
            Assert.AreEqual(0.06f, AimSensitivity.DegreesPerCount(30f, 200f), 0.0001f);
            Assert.AreEqual(5f, AimSensitivity.SanitizeCmPer360(0.01f));
            Assert.AreEqual(100f, AimSensitivity.SanitizeCmPer360(500f));
            Assert.AreEqual(30f, AimSensitivity.SanitizeCmPer360(float.NaN));
        }

        [Test]
        public void SeededRandom_RepeatsSequenceAndStaysInRange()
        {
            SeededRandom first = new SeededRandom(42);
            SeededRandom second = new SeededRandom(42);
            for (int i = 0; i < 100; i++)
            {
                float firstValue = first.NextFloat();
                float secondValue = second.NextFloat();
                Assert.AreEqual(firstValue, secondValue);
                Assert.IsTrue(firstValue >= 0f && firstValue < 1f);
            }

            Assert.LessOrEqual(first.InsideUnitCircle().sqrMagnitude, 1f);
        }

        [Test]
        public void TargetTrajectory_IsDeterministicAndBounded()
        {
            Vector3 origin = new Vector3(1f, 3f, -2f);
            Vector3 first = TargetTrajectory.Evaluate(TargetMotionKind.Sine, origin, 2f, 0.5f, 1.5f, 0.3f, 2f);
            Vector3 second = TargetTrajectory.Evaluate(TargetMotionKind.Sine, origin, 2f, 0.5f, 1.5f, 0.3f, 2f);
            Assert.AreEqual(first, second);
            Assert.LessOrEqual(first.x - origin.x, 2f);
            Assert.LessOrEqual(first.y - origin.y, 0.5f);
            Assert.AreEqual(origin, TargetTrajectory.Evaluate(TargetMotionKind.Static, origin, 2f, 1f, 3f, 0f, 10f));
        }

        [Test]
        public void TargetHitZones_ApplyDamageAndScores()
        {
            Assert.AreEqual(TargetHitZone.Bullseye, TargetHitZoneEvaluator.EvaluateRadius(0.05f));
            Assert.AreEqual(TargetHitZone.Critical, TargetHitZoneEvaluator.EvaluateRadius(0.2f));
            Assert.AreEqual(TargetHitZone.Inner, TargetHitZoneEvaluator.EvaluateRadius(0.3f));
            Assert.AreEqual(TargetHitZone.Outer, TargetHitZoneEvaluator.EvaluateRadius(0.5f));
            Assert.AreEqual(TargetHitZone.Rim, TargetHitZoneEvaluator.EvaluateRadius(0.55f));
            Assert.AreEqual(TargetHitZone.Frame, TargetHitZoneEvaluator.EvaluateRadius(0.64f));

            TargetHitResult result = TargetHitZoneEvaluator.ApplyDamage(TargetHitZone.Bullseye, 34f, 100f, true);
            Assert.AreEqual(68f, result.Damage, 0.001f);
            Assert.IsFalse(result.Killed);
            Assert.AreEqual(100, result.Score);
            result = TargetHitZoneEvaluator.ApplyDamage(TargetHitZone.Bullseye, 50f, 100f, true);
            Assert.IsTrue(result.Killed);
        }

        [Test]
        public void TrainingMetrics_ComputesReactionTtkAndZones()
        {
            TrainingMetrics metrics = new TrainingMetrics();
            metrics.RecordMiss();
            metrics.RecordHit(new TargetHitResult(TargetHitZone.Bullseye, 100, 100f, true, true), 0.4f);
            metrics.RecordHit(new TargetHitResult(TargetHitZone.Inner, 50, 25f, false, true), 0.2f);
            Assert.AreEqual(3, metrics.Shots);
            Assert.AreEqual(2, metrics.Hits);
            Assert.AreEqual(1, metrics.Kills);
            Assert.AreEqual(150, metrics.Score);
            Assert.AreEqual(300f, metrics.AverageReactionMs, 0.001f);
            Assert.AreEqual(400f, metrics.AverageTtkMs, 0.001f);
            Assert.AreEqual(2, metrics.BestStreak);
            Assert.AreEqual(1, metrics.GetZoneHits(TargetHitZone.Bullseye));
            metrics.Reset();
            Assert.AreEqual(0, metrics.Shots);
            Assert.AreEqual(0f, metrics.AverageReactionMs);
        }

        [Test]
        public void TrainingAnalytics_ComputesPercentilesAndAimEfficiency()
        {
            List<float> values = new List<float> { 400f, 100f, 300f, 200f };
            Assert.AreEqual(250f, TrainingAnalytics.Percentile(values, 0.5f), 0.001f);
            Assert.AreEqual(385f, TrainingAnalytics.Percentile(values, 0.95f), 0.001f);
            Assert.AreEqual(400f, values[0]);

            AimPathTracker tracker = new AimPathTracker();
            tracker.AddSample(10f, 5f);
            tracker.AddSample(20f, 5f);
            Assert.AreEqual(1f / 3f, tracker.Efficiency, 0.001f);
            tracker.Reset();
            Assert.AreEqual(0f, tracker.Efficiency);
        }

        [Test]
        public void TrainingHistory_SelectsPersonalBestAndExports()
        {
            TrainingHistoryStore.Clear();
            TrainingRecord first = new TrainingRecord { scenario = "Tracking", weapon = "Rifle", score = 100, accuracy = 0.8f, reactionMedian = 300f };
            TrainingRecord second = new TrainingRecord { scenario = "Tracking", weapon = "Rifle", score = 100, accuracy = 0.9f, reactionMedian = 350f };
            TrainingHistoryStore.Add(first);
            TrainingHistoryStore.Add(second);
            Assert.AreEqual(second, TrainingHistoryStore.GetPersonalBest("Tracking"));
            Assert.IsTrue(TrainingHistoryStore.BuildCsv().Contains("Tracking"));
            Assert.IsTrue(TrainingHistoryStore.BuildJson().Contains("records"));
            TrainingHistoryStore.Clear();
        }

        [Test]
        public void RecoilPattern_IsDeterministicAndClamped()
        {
            Vector2 first = RecoilPattern.Evaluate(0, 0.2f, 0.1f, WeaponModelKind.Rifle);
            Vector2 repeat = RecoilPattern.Evaluate(0, 0.2f, 0.1f, WeaponModelKind.Rifle);
            Assert.AreEqual(first, repeat);
            Vector2 clamped = RecoilPattern.Evaluate(100000, 0.2f, 0.1f, WeaponModelKind.Rifle);
            Assert.AreEqual(RecoilPattern.Evaluate(9, 0.2f, 0.1f, WeaponModelKind.Rifle), clamped);
        }

        [Test]
        public void RangeMesh_HasAllMaterialsAndValidGeometry()
        {
            Mesh mesh = RangeModelFactory.Build();
            Assert.Greater(mesh.vertexCount, 500);
            Assert.AreEqual(RangeModelFactory.MaterialSlotCount, mesh.subMeshCount);
            AssertGeometryIsFiniteAndPopulated(mesh);
        }

        private static void AssertGeometryIsFiniteAndPopulated(Mesh mesh)
        {
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 vertex = mesh.vertices[i];
                Assert.IsTrue(GameSettings.IsFinite(vertex.x) && GameSettings.IsFinite(vertex.y) && GameSettings.IsFinite(vertex.z));
            }

            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                Assert.Greater(mesh.GetTriangles(submesh).Length, 0);
            }
        }
    }
}
