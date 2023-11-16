namespace Spring;

public interface ICutpointMaps
{
    bool ContainsCutpoint(string cutPointPattern);
    bool TryGetCutpointAdvice(string cutPointPattern, out CutpointAdvices advices);
    bool TryGetCutpoint(string cutPointPattern, out ICutpointScanningInfo cutpoint);
    (ICutpointScanningInfo cutpoint, CutpointAdvices advices)[] GetCutpointInfos();
    void AddCutpoint(string cutpointPattern, ICutpointScanningInfo scanningInfo);
    void AddCutpointNameMap(string cutpointName, string cutpointPattern);
}