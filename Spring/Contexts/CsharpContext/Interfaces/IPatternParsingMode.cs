namespace Spring;

/// <summary>
/// Parsing assembly's scan path
/// </summary>
public interface IPatternParsingMode
{
    /// <summary>
    /// Parse pattern.
    /// </summary>
    /// <param name="pattern">String containing scan path</param>
    /// <returns>Return scan path infos,it could be {assemblyName,namespace,className} or {assemblyName},{classFullName}</returns>
    string[] Parse(string pattern);
}