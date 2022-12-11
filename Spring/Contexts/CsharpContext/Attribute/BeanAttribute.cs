using System;

namespace Spring
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class BeanAttribute : Attribute
    {
        private readonly string m_Value;

        public BeanAttribute(string value) => m_Value = value;

        public BeanAttribute() { }

        public string GetName() => m_Value;
    }
}