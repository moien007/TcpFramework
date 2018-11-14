using System;
using System.Collections.Generic;
using System.Text;

namespace TcpFramework
{
    internal static class ThrowHelper
    {
        public static void Arg<T>(T value, string name) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(name);
        }
    }
}
