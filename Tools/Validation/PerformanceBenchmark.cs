using System;
using System.Diagnostics;
using FpsPractice;
using FpsPractice.Models;
using UnityEngine;

public static class PerformanceBenchmark
{
    private delegate Mesh MeshFactory();

    public static int Main()
    {
        Console.WriteLine("OFFLINE MANAGED-CODE MICROBENCHMARK");
        Console.WriteLine("Unity physics cooking, rendering and frame timing are not included.");
        RunMeshBenchmark("Rifle", WeaponModelFactory.BuildRifle);
        RunMeshBenchmark("Sidearm", WeaponModelFactory.BuildSidearm);
        RunMeshBenchmark("Target visual", TargetModelFactory.BuildTarget);
        RunMeshBenchmark("Target collision", TargetModelFactory.BuildCollisionTarget);
        RunMeshBenchmark("Range environment", RangeModelFactory.Build);
        RunWeaponStateBenchmark();
        return 0;
    }

    private static void RunMeshBenchmark(string name, MeshFactory factory)
    {
        for (int i = 0; i < 3; i++)
        {
            factory();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memoryBefore = GC.GetAllocatedBytesForCurrentThread();
        Stopwatch stopwatch = Stopwatch.StartNew();
        const int iterations = 100;
        for (int i = 0; i < iterations; i++)
        {
            factory();
        }

        stopwatch.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - memoryBefore;
        Console.WriteLine(name + ": " + (stopwatch.Elapsed.TotalMilliseconds / iterations).ToString("0.000")
            + " ms/build, " + (allocated / (double)iterations / 1024.0).ToString("0.0") + " KiB/build (100 builds)");
    }

    private static void RunWeaponStateBenchmark()
    {
        WeaponState state = new WeaponState(12, 600f, 1.1f);
        const int iterations = 1000000;
        int fired = 0;
        long memoryBefore = GC.GetAllocatedBytesForCurrentThread();
        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            if (state.TryFire(i * 0.11f))
            {
                fired++;
            }

            if (i % 12 == 11)
            {
                state.Reset();
            }
        }

        stopwatch.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - memoryBefore;
        Console.WriteLine("WeaponState: " + (stopwatch.Elapsed.TotalMilliseconds / iterations * 1000000.0).ToString("0.0")
            + " ns/call, " + allocated + " bytes total, " + fired + " fired (1,000,000 calls)");
    }
}
