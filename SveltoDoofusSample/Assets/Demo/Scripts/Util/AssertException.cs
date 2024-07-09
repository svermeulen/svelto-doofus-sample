using System;

namespace SveltoDoofusSample
{
    public class AssertException : Exception
    {
        public AssertException(string message)
            : base(message) { }

        public AssertException(string format, params object[] formatArgs)
            : base(string.Format(format, formatArgs)) { }
    }
}
