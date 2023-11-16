namespace Spring;

public interface ICutpointScanningInfo : IMethodScanningInfo
{
    /// <summary>
    /// Get cutpoint name if it has.
    /// </summary>
    string GetName();

    string GetValue();
}