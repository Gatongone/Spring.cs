using System.Reflection;

namespace Spring
{
    public interface IMemberInjectionPreprocessor
    {
        void OnBeforeInjection(MemberInfo member, object? bean, string? beanName);
    }
}