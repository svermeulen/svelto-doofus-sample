using System;
using System.Collections.Generic;

namespace SveltoDoofusSample
{
    public static class Assert
    {
        public static void That(bool condition)
        {
            if (!condition)
            {
                throw CreateException("Assert hit!");
            }
        }

        public static void That(bool condition, string message)
        {
            if (!condition)
            {
                throw CreateException(message);
            }
        }

        // Prefer generics to object to avoid boxing and causing allocs
        public static void That<T>(bool condition, string message, T arg1)
        {
            if (!condition)
            {
                throw CreateException(message, arg1);
            }
        }

        public static void That<T1, T2>(bool condition, string message, T1 arg1, T2 arg2)
        {
            if (!condition)
            {
                throw CreateException(message, arg1, arg2);
            }
        }

        public static void That<T1, T2, T3>(
            bool condition,
            string message,
            T1 arg1,
            T2 arg2,
            T3 arg3
        )
        {
            if (!condition)
            {
                throw CreateException(message, arg1, arg2, arg3);
            }
        }

        public static void That<T1, T2, T3, T4>(
            bool condition,
            string message,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4
        )
        {
            if (!condition)
            {
                throw CreateException(message, arg1, arg2, arg3, arg4);
            }
        }

        public static void That<T1, T2, T3, T4, T5>(
            bool condition,
            string message,
            T1 arg1,
            T2 arg2,
            T3 arg3,
            T4 arg4,
            T5 arg5
        )
        {
            if (!condition)
            {
                throw CreateException(message, arg1, arg2, arg3, arg4, arg5);
            }
        }

        // Prefer generics to object to avoid boxing and causing allocs
        public static void IsEqual<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected!, actual!))
            {
                throw CreateException(
                    "Expected (left): {0}, Actual (right): {1}",
                    expected!,
                    actual!
                );
            }
        }

        public static void IsEqual<T>(T expected, T actual, string message)
            where T : notnull
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw CreateException(
                    "{0}\nExpected (left): {1}, Actual (right): {2}",
                    message,
                    expected,
                    actual
                );
            }
        }

        public static void IsEqual<T1, T2, T3>(
            T1 expected,
            T1 actual,
            string message,
            T2 arg1,
            T3 arg2
        )
            where T1 : notnull
        {
            if (!EqualityComparer<T1>.Default.Equals(expected, actual))
            {
                throw CreateException(
                    "{0}\nExpected (left): {1}, Actual (right): {2}",
                    string.Format(message, arg1, arg2),
                    expected,
                    actual
                );
            }
        }

        // Prefer generics to object to avoid boxing and causing allocs
        public static void IsNotEqual<T>(T expected, T actual)
            where T : notnull
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw CreateException(
                    "Expected (left): {0}, Actual (right): {1}",
                    expected,
                    actual
                );
            }
        }

        public static void IsNotEqual<T>(T expected, T actual, string message)
            where T : notnull
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw CreateException(
                    "{0}\nExpected (left): {1}, Actual (right): {2}",
                    message,
                    expected,
                    actual
                );
            }
        }

        // Prefer generics to object to avoid boxing and causing allocs
        public static void IsNull<T>(T value)
        {
            if (value != null)
            {
                throw CreateException("Expected given value to be null");
            }
        }

        public static void IsNull<T>(T value, string message)
            where T : class
        {
            if (value != null)
            {
                throw CreateException(message);
            }
        }

        public static void IsNull<T1, T2>(T1 value, string message, T2 arg1)
            where T1 : class
        {
            if (value != null)
            {
                throw CreateException(message, arg1);
            }
        }

        // Prefer generics to object to avoid boxing and causing allocs
        public static void IsNotNull<T>(T value)
            where T : class
        {
            if (value == null)
            {
                throw CreateException("Expected given value to be non-null");
            }
        }

        public static void IsNotNull<T>(T value, string message)
            where T : class
        {
            if (value == null)
            {
                throw CreateException(message);
            }
        }

        public static void IsNotNull<T1, T2>(T1 value, string message, T2 arg1)
            where T1 : class
        {
            if (value == null)
            {
                throw CreateException(message, arg1);
            }
        }

        public static void IsNotNull<T1, T2, T3>(T1 value, string message, T2 arg1, T3 arg2)
            where T1 : class
        {
            if (value == null)
            {
                throw CreateException(message, arg1, arg2);
            }
        }

        public static void IsNotNull<T1, T2, T3, T4>(
            T1 value,
            string message,
            T2 arg1,
            T3 arg2,
            T4 arg3
        )
            where T1 : class
        {
            if (value == null)
            {
                throw CreateException(message, arg1, arg2, arg3);
            }
        }

        public static void Throws(Action action)
        {
            Throws<Exception>(action);
        }

        public static void Throws<T>(Action action)
            where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw CreateException(
                "Expected to receive exception of type '{0}' but nothing was thrown",
                typeof(T).Name
            );
        }

        public static AssertException CreateException()
        {
            return new AssertException("Assert hit!");
        }

        public static AssertException CreateException(string message, params object[] args)
        {
            return new AssertException("Assert hit!  Details: {0}", string.Format(message, args));
        }
    }
}
