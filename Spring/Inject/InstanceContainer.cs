using System;
using System.Collections.Generic;

namespace Spring
{
    public class InstanceContainer : IInstanceContainer
    {
        private readonly Dictionary<Type, object?> m_Type2SingletonsMap = new();
        private readonly Dictionary<string, object?> m_Name2SingletonsMap = new();
        private readonly Dictionary<string, Type> m_Name2PrototypeMap = new();
        private readonly Dictionary<Type, Type> m_Type2PrototypeMap = new();
        private readonly HashSet<Type> m_PrototypeSet = new();
        public IInstanceContainer value => this;

        public void BindSingleton(Type type, object? obj) => m_Type2SingletonsMap[type] = obj;

        public void BindSingleton(string name, object? obj) => m_Name2SingletonsMap[name] = obj;

        public void BindPrototype(string name, Type type) => m_Name2PrototypeMap[name] = type;

        public void BindPrototype(Type type, Type mappingType) => m_Type2PrototypeMap[type] = mappingType;

        public bool RemoveSingletonBinding(string name) => m_Name2SingletonsMap.Remove(name);

        public bool RemoveSingletonBinding(Type type) => m_Type2SingletonsMap.Remove(type);

        public bool RemovePrototypeBinding(string name) => m_Name2PrototypeMap.Remove(name);

        public bool RemovePrototypeBinding(Type type) => m_PrototypeSet.Remove(type) | m_Type2PrototypeMap.Remove(type);

        public bool ContainsSingleton(string name) => m_Name2SingletonsMap.ContainsKey(name);

        public bool ContainsSingleton(Type type) => m_Type2SingletonsMap.ContainsKey(type);

        public bool ContainsPrototype(string name) => m_Name2PrototypeMap.ContainsKey(name);

        public bool ContainsPrototype(Type type) => m_PrototypeSet.Contains(type);


        public object? RequireSingleton(string name, Type type)
        {
            if (!m_Name2SingletonsMap.TryGetValue(name, out var result))
            {
                result = Activator.CreateInstance(type);
                if (result != null)
                    m_Name2SingletonsMap.Add(name, result);
                else
                    throw new ResolveObjectException($"object resolve failed, object named \"{type}\" can't create instance.");
            }

            return result;
        }

        public object? RequireSingleton(Type type)
        {
            if (!m_Type2SingletonsMap.TryGetValue(type, out var result))
            {
                result = Activator.CreateInstance(type);
                if (result != null)
                    m_Type2SingletonsMap.Add(type, result);
                else
                    throw new ResolveObjectException($"object resolve failed, object typed \"{type}\" can't create instance.");
            }

            return result;
        }

        public object? RequirePrototype(string name, params object[] args)
        {
            if (!m_Name2PrototypeMap.TryGetValue(name, out var type))
            {
                m_Name2PrototypeMap.Add(name, type);
            }

            var result = args is {Length: 0} ? Activator.CreateInstance(type) : Activator.CreateInstance(type, args);
            if (result == null)
                throw new ResolveObjectException($"object resolve failed, object named \"{type}\" can't create instance.");
            return result;
        }

        public object? RequirePrototype(Type type, params object[] args)
        {
            if (!m_PrototypeSet.Contains(type))
            {
                m_PrototypeSet.Add(type);
            }

            if (m_Type2PrototypeMap.ContainsKey(type))
            {
                type = m_Type2PrototypeMap[type];
            }

            if (!m_PrototypeSet.Contains(type))
            {
                m_PrototypeSet.Add(type);
            }

            var result = args is {Length: 0} ? Activator.CreateInstance(type) : Activator.CreateInstance(type, args);
            if (result == null)
                throw new ResolveObjectException($"object resolve failed, object typed \"{type}\" can't create instance.");
            return result;
        }
    }
}