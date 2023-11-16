using System;

namespace Spring.Advices;

[AttributeUsage(AttributeTargets.Method)]
public class AdviceAttribute : Attribute
{
    private readonly string m_CutPoint;

    public AdviceAttribute(string cutPoint) => m_CutPoint = cutPoint;

    public string GetCutPoint() => m_CutPoint;
}