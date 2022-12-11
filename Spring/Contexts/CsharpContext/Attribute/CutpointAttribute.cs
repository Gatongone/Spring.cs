using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Spring
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CutpointAttribute : Attribute, ICutpointScanningInfo
    {
        private readonly string m_Value;
        protected readonly string name;
        protected readonly string assemblyValue;
        protected readonly string namespaceValue;
        protected readonly string typeValue;
        protected readonly string methodValue;
        private string m_MethodName;
        private string[] m_MethodParams;

        /// <summary>
        /// [Assembly][Namespace][ClassName][Method]
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="InvalidPatternException"></exception>
        public CutpointAttribute(string value)
        {
            var results = PatternAnalyser.ParseBrackets(value);
            if (results.Length != 3 && results.Length != 4)
                throw new InvalidPatternException();
            assemblyValue = results[0];
            if (results.Length == 3)
            {
                typeValue = results[1];
                methodValue = results[2];
            }
            else
            {
                namespaceValue = results[1];
                typeValue = results[2];
                methodValue = results[3];
            }

            m_Value = value;
            PatternAnalyser.AnalyseMethod(methodValue, out m_MethodName, out m_MethodParams);
        }

        public CutpointAttribute(string cutpointName, string value) : this(value)
        {
            name = cutpointName;
        }

        public string GetValue() => m_Value;
        public string GetName() => name;
        public string GetAssemblyPattern() => assemblyValue;
        public string GetNamespacePattern() => namespaceValue;
        public string GetTypePattern() => typeValue;
        public string GetMethodPattern() => methodValue;

        public bool MatchMethod(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                return false;
            if (!m_MethodName.Equals("*") && !methodInfo.Name.Equals(m_MethodName))
                return false;
            var parameters = methodInfo.GetParameters().Select(param => param.ParameterType).ToArray();
            if (m_MethodParams.Length == 1 && m_MethodParams[0].Equals("*"))
                return true;
            if (parameters.Length != m_MethodParams.Length)
                return false;
            return !parameters.Where((t, index) => PatternAnalyser.MatchType(m_MethodParams[index], t)).Any();
        }

        public bool MatchType(Type type)
        {
            if (type == null || typeValue == null)
                return false;
            var assemblyName = type.Assembly.GetName().Name;
            var isSucceed = false;
            if (!string.IsNullOrEmpty(assemblyName) && !string.IsNullOrEmpty(assemblyValue)) 
                isSucceed = assemblyValue.Equals("*") || Regex.IsMatch(assemblyName, assemblyValue);
            if (isSucceed && type.Namespace != null && !string.IsNullOrEmpty(namespaceValue)) 
                isSucceed = namespaceValue.Equals("*") || Regex.IsMatch(type.Namespace, namespaceValue);
            if (isSucceed && !string.IsNullOrEmpty(typeValue)) 
                isSucceed = typeValue.Equals("*") || Regex.IsMatch(type.Name, typeValue);
            return isSucceed;
        }
    }
}