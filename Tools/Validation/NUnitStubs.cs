using System;
using System.Collections.Generic;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute { }

    public static class Assert
    {
        public static void Greater(int actual, int expected)
        {
            if (actual <= expected) throw new Exception("Expected " + actual + " to be greater than " + expected);
        }

        public static void IsTrue(bool condition)
        {
            if (!condition) throw new Exception("Expected true but got false");
        }

        public static void IsFalse(bool condition)
        {
            if (condition) throw new Exception("Expected false but got true");
        }

        public static void AreEqual<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception("Values are not equal");
        }

        public static void LessOrEqual(float actual, float expected)
        {
            if (actual > expected) throw new Exception("Expected " + actual + " <= " + expected);
        }

        public static void AreEqual(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + " but got " + actual);
        }

        public static void AreEqual(float expected, float actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + " but got " + actual);
        }

        public static void AreEqual(float expected, float actual, float tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + " but got " + actual);
        }
    }
}
