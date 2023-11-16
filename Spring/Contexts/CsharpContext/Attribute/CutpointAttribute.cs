using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Spring;

[AttributeUsage(AttributeTargets.Method)]
public sealed class CutpointAttribute : Attribute, ICutpointScanningInfo
{
    private readonly string m_Value;
    private readonly string m_Name;
    private readonly string m_AssemblyValue;
    private readonly string m_NamespaceValue;
    private readonly string m_TypeValue;
    private readonly string m_MethodValue;
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
        m_AssemblyValue = results[0];
        if (results.Length == 3)
        {
            m_TypeValue = results[1];
            m_MethodValue = results[2];
        }
        else
        {
            m_NamespaceValue = results[1];
            m_TypeValue = results[2];
            m_MethodValue = results[3];
        }

        m_Value = value;
        PatternAnalyser.AnalyseMethod(m_MethodValue, out m_MethodName, out m_MethodParams);
    }

    public CutpointAttribute(string cutpointName, string value) : this(value)
    {
        m_Name = cutpointName;
    }

    public string GetValue() => m_Value;
    public string GetName() => m_Name;
    public string GetAssemblyPattern() => m_AssemblyValue;
    public string GetNamespacePattern() => m_NamespaceValue;
    public string GetTypePattern() => m_TypeValue;
    public string GetMethodPattern() => m_MethodValue;

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
        if (type == null || m_TypeValue == null)
            return false;
        var assemblyName = type.Assembly.GetName().Name;
        var isSucceed = false;
        if (!string.IsNullOrEmpty(assemblyName) && !string.IsNullOrEmpty(m_AssemblyValue)) 
            isSucceed = m_AssemblyValue.Equals("*") || Regex.IsMatch(assemblyName, m_AssemblyValue);
        if (isSucceed && type.Namespace != null && !string.IsNullOrEmpty(m_NamespaceValue)) 
            isSucceed = m_NamespaceValue.Equals("*") || Regex.IsMatch(type.Namespace, m_NamespaceValue);
        if (isSucceed && !string.IsNullOrEmpty(m_TypeValue)) 
            isSucceed = m_TypeValue.Equals("*") || Regex.IsMatch(type.Name, m_TypeValue);
        return isSucceed;
    }
}