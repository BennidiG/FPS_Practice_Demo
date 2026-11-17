using System;
using System.Reflection;
using NUnit.Framework;
using FpsPractice.Tests.EditMode;

public static class OfflineTestRunner
{
    public static int Main()
    {
        int passed = 0;
        int failed = 0;
        CoreLogicTests fixture = new CoreLogicTests();
        MethodInfo[] methods = typeof(CoreLogicTests).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        foreach (MethodInfo method in methods)
        {
            if (method.GetCustomAttributes(typeof(TestAttribute), false).Length == 0)
            {
                continue;
            }

            try
            {
                method.Invoke(fixture, null);
                Console.WriteLine("PASS " + method.Name);
                passed++;
            }
            catch (TargetInvocationException exception)
            {
                Console.Error.WriteLine("FAIL " + method.Name + ": " + exception.InnerException.Message);
                failed++;
            }
        }

        Console.WriteLine("Core logic: " + passed + " passed, " + failed + " failed.");
        return failed == 0 && passed == 19 ? 0 : 1;
    }
}
