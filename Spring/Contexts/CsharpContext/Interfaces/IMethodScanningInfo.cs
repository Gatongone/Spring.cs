using System.Reflection;

namespace Spring
{
    public interface IMethodScanningInfo : ITypeScanningInfo
    {
        /// <summary>
        /// Get assembly pattern
        /// </summary>
        string GetMethodPattern();

        bool MatchMethod(MethodInfo methodInfo);
    }
}