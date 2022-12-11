using System.Reflection;

namespace Spring
{
    public interface IMemberInjectionPostprocessor
    {
        void OnAfterInjection(MemberInfo member, object? bean, string? beanName);
    }
}