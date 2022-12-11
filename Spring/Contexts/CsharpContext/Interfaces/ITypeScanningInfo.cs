using System;

namespace Spring
{
    /// <summary>
    /// The type information filtered during scanning assembly
    /// </summary>
    public interface ITypeScanningInfo
    {
        /// <summary>
        /// Get assembly pattern
        /// </summary>
        string GetAssemblyPattern();

        /// <summary>
        /// Get namespace pattern
        /// </summary>
        string GetNamespacePattern();

        /// <summary>
        /// Get type pattern
        /// </summary>
        string GetTypePattern();

        /// <summary>
        /// Is the Pattern of the Name and namespace of the type correct
        /// </summary>
        bool MatchType(Type type);
    }
}