using System.Threading.Tasks;

namespace TcpFramework.Common
{
#if NET45
    internal static class CompletedTask
    {
        public static readonly Task Task = Task.FromResult(true);
    }
#endif
}
