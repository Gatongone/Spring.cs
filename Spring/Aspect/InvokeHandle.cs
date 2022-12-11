using System;

namespace Spring
{
    public class InvokeHandle : IMethodReference
    {
        private Func<object[],object> m_Func;
        public InvokeHandle(Func<object[],object> func)
        {
            m_Func = func;
        }

        public object Invoke(params object[] args)
        {
            return m_Func?.Invoke(args);
        }
    }
}