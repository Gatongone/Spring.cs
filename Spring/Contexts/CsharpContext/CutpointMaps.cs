using System;
using System.Collections.Generic;
using System.Linq;

namespace Spring;

internal class CutpointMaps : ICutpointMaps
{
    private readonly Dictionary<string, (ICutpointScanningInfo cutpoint, CutpointAdvices advices)> m_CutpointMaps = new();
    private readonly Dictionary<string, string> m_CutpointNameMaps = new();

    public bool ContainsCutpoint(string cutPointPattern)
    {
        if (m_CutpointNameMaps.TryGetValue(cutPointPattern, out var cutpointValue))
        {
            return true;
        }

        cutpointValue = cutPointPattern;
        return m_CutpointMaps.ContainsKey(cutpointValue);
    }

    public bool TryGetCutpointAdvice(string cutPointPattern, out CutpointAdvices advices)
    {
        advices = null;
        if (m_CutpointNameMaps.TryGetValue(cutPointPattern, out var cutpointValue))
        {
            if (m_CutpointMaps.TryGetValue(cutpointValue, out var cutpointInfoPairs))
            {
                advices = cutpointInfoPairs.advices;
                return true;
            }
        }
        else
        {
            if (m_CutpointMaps.TryGetValue(cutPointPattern, out var cutpointInfoPairs))
            {
                advices = cutpointInfoPairs.advices;
                return true;
            }
        }
        return false;
    }

    public bool TryGetCutpoint(string cutPointPattern, out ICutpointScanningInfo cutpoint)
    {
        cutpoint = null;
        if (m_CutpointNameMaps.TryGetValue(cutPointPattern, out var cutpointValue))
        {
            if (m_CutpointMaps.TryGetValue(cutpointValue, out var cutpointInfoPairs))
            {
                cutpoint = cutpointInfoPairs.cutpoint;
                return true;
            }
        }
        else
        {
            if (m_CutpointMaps.TryGetValue(cutPointPattern, out var cutpointInfoPairs))
            {
                cutpoint = cutpointInfoPairs.cutpoint;
                return true;
            }
        }

        return false;
    }

    public (ICutpointScanningInfo cutpoint, CutpointAdvices advices)[] GetCutpointInfos()
    {
        return m_CutpointMaps.Values.ToArray();
    }

    public void AddCutpoint(string cutpointPattern, ICutpointScanningInfo scanningInfo)
    {
        if (m_CutpointNameMaps.ContainsKey(cutpointPattern) || m_CutpointMaps.ContainsKey(cutpointPattern))
        {
            throw new ArgumentException($"A cutpoint with the same pattern has already been added. Pattern: {cutpointPattern}");
        }

        m_CutpointMaps.Add(cutpointPattern, (scanningInfo, new CutpointAdvices()));
    }

    public void AddCutpointNameMap(string cutpointName, string cutpointPattern)
    {
        if (m_CutpointNameMaps.ContainsKey(cutpointPattern))
        {
            throw new ArgumentException($"A cutpoint with the same pattern has already been added. Pattern: {cutpointPattern}");
        }

        m_CutpointNameMaps[cutpointName] = cutpointPattern;
    }
}