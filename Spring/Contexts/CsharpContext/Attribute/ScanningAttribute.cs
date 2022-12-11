using System;
using System.Text.RegularExpressions;

namespace Spring
{
    public class ScanningAttribute : Attribute, ITypeScanningInfo
    {
        protected readonly string assemblyName;
        protected readonly string namespaceName;
        protected readonly string typeName;

        /// <summary>
        /// Pattern must be [AssemblyName][NamespaceName][ClassName] or [AssemblyName][ClassFullName]. 
        /// When all files need to be scanned, the name is defined as '*'.
        /// When specific files need to be scanned, Names can be defined by RegExp.
        /// </summary>
        /// <param name="pattern">Scan pattern</param>
        /// <exception cref="InvalidPatternException">Thrown when the pattern cannot be parsed correctly.</exception>
        public ScanningAttribute(string pattern)
        {
            var results = PatternAnalyser.ParseBrackets(pattern);
            if (results.Length != 2 && results.Length != 3)
                throw new InvalidPatternException();
            assemblyName = results[0];
            if (results.Length == 2)
                typeName = results[1];
            else
            {
                namespaceName = results[1];
                typeName = results[2];
            }
        }

        public ScanningAttribute(string assemblyName, string namespaceName, string typeName)
        {
            this.assemblyName = assemblyName;
            this.namespaceName = namespaceName;
            this.typeName = typeName;
        }

        public ScanningAttribute(string assemblyName, string typeFullName)
        {
            this.assemblyName = assemblyName;
            typeName = typeFullName;
        }

        public string GetAssemblyPattern() => assemblyName;

        public string GetNamespacePattern() => assemblyName;

        public string GetTypePattern() => typeName;


        public bool MatchType(Type type) => IsMatch(type, GetNamespacePattern(), GetTypePattern());

        private static bool IsMatch(Type type, string namespaceName, string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;
            if (string.IsNullOrEmpty(namespaceName) && IsMatch(typeName, typeName))
                return true;
            return type.Namespace != null && IsMatch(type.Namespace, namespaceName) && IsMatch(type.Name, typeName);
        }

        private static bool IsMatch(string target, string pattern)
        {
            return pattern.Equals("*", StringComparison.Ordinal) || Regex.IsMatch(target, pattern);
        }
    }
}