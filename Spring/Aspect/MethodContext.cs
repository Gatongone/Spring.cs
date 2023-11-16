using System;
using BindingFlags = System.Reflection.BindingFlags;

namespace Spring;

public partial class RuntimeAssembly
{
    public static Func<IMethodReference, object> func;

    public struct MethodContext<T>
    {
        private object m_Result;

        public MethodContext(object obj)
        {
            m_Result = obj;
        }

        public MethodContext<T> WithInterceptor(string methodName, Type[] parameters, Interceptor interceptor)
        {
            var field = m_Result.GetType().GetField(GetInterceptorFieldName(methodName, parameters), BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new ProxyTypeGenerateException($"Can't not generate interceptor from method \"{methodName}\". ");
            field.SetValue(m_Result, interceptor);
            return this;
        }

        public T GetResult()
        {
            return (T) m_Result;
        }
    }

    public struct MethodContext
    {
        private object m_Result;

        public MethodContext(object obj)
        {
            m_Result = obj;
        }

        public MethodContext WithInterceptor(string methodName, Type[] parameters, Interceptor interceptor)
        {
            var field = m_Result.GetType().GetField(GetInterceptorFieldName(methodName, parameters), BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
                throw new ProxyTypeGenerateException($"Can't not generate interceptor from method \"{methodName}\". ");
            field.SetValue(m_Result, interceptor);
            return this;
        }

        public object GetResult()
        {
            return m_Result;
        }
    }
}