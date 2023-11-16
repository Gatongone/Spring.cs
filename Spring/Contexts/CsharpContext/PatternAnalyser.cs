using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Spring;

public static class PatternAnalyser
{
    /// <summary>
    /// Return to the string content in the bracket
    /// </summary>
    public static string[] ParseBrackets(string pattern)
    {
        var results = Regex.Matches(pattern, @"\[([^\[\]]+)\]");
        return results.Select(result => result.Groups[1].Value).ToArray();
    }
        
    public static void AnalyseMethod(string methodValue,out string methodName,out string[] paramsNames)
    {
        var methodNameBuilder = new StringBuilder();
        var paramsPatternBuilder = new StringBuilder();
        var isInRoundBracket = false;
        var isInAngleBracket = false;
        var paramsCollection = new List<string>();
            
        for (var i = 0; i < methodValue.Length; i++)
        {
            var str = methodValue[i];

            switch (str)
            {
                case '<':
                    paramsPatternBuilder.Append(str);
                    isInAngleBracket = true; 
                    break;
                case '>':
                    paramsPatternBuilder.Append(str);
                    isInAngleBracket = false;
                    break;
                case '(':
                    isInRoundBracket = true;
                    break;
                case ')':
                case ',':
                    if (isInAngleBracket)
                    {
                        paramsPatternBuilder.Append(str);
                        break;
                    }
                    paramsCollection.Add(paramsPatternBuilder.ToString());
                    paramsPatternBuilder.Clear();
                    break;
                case ' ':
                    break;
                default:
                    if (isInRoundBracket) paramsPatternBuilder.Append(str);
                    else methodNameBuilder.Append(str);
                    break;
            }
        }
        methodName = methodNameBuilder.ToString();
        paramsNames = paramsCollection.ToArray();
    }
        
    public static bool TryGetValueType(string name,out Type type)
    {
        type = null;
        switch (name)
        {
            case "int": type = typeof(int); break;
            case "long":type = typeof(long);break;
            case "byte": type = typeof(byte);break;
            case "sbyte": type = typeof(sbyte);break;
            case "char": type = typeof(char);break;
            case "uint": type = typeof(uint);break;
            case "ulong": type = typeof(ulong);break;
            case "ushort": type = typeof(ushort);break;
            case "float": type = typeof(float);break;
            case "double": type = typeof(double);break;
            case "decimal": type = typeof(decimal);break;
            case "string": type = typeof(string);break;
            case "int?": type = typeof(int?); break;
            case "long?":type = typeof(long?);break;
            case "byte?": type = typeof(byte?);break;
            case "sbyte?": type = typeof(sbyte?);break;
            case "char?": type = typeof(char?);break;
            case "uint?": type = typeof(uint?);break;
            case "ulong?": type = typeof(ulong?);break;
            case "ushort?": type = typeof(ushort?);break;
            case "float?": type = typeof(float?);break;
            case "double?": type = typeof(double?);break;
            case "decimal?": type = typeof(decimal?);break;
        }
        return type != null;
    }
        
    public static bool MatchType(string name, Type type)
    {
        if (TryGetValueType(name, out var basicType))
        {
            return basicType == type;
        }

        if (name.Contains('?'))
        {
            return Regex.Match(type.ToString(), @"\[(.+)\]")
                .Groups[1]
                .Value
                .Equals(name.Replace("?", ""), StringComparison.Ordinal);
        }

        if (name.Contains('<') || name.Contains('>'))
        {
            var targetNames = Regex.Match(name, @"\<(.+)\>")
                .Groups[1]
                .Value
                .Split(',');
            var originNames = Regex.Match(type.ToString(), @"\[(.+)\]")
                .Groups[1]
                .Value
                .Split(',');
            if (targetNames.Length != originNames.Length)
                return false;
            for (var index = 0; index < originNames.Length; index++)
            {
                var compared = targetNames[index];
                if (TryGetValueType(compared, out var targetType))
                {
                    compared = targetType.ToString();
                }
                if (!compared.Equals(originNames[index]))
                    return false;
            }
            return true;
        }
        return name.Equals(type.ToString());
    }
}