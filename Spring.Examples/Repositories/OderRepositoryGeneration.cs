using System;
using System.Collections.Generic;
using System.Linq;

namespace Spring.Examples.Repositories;

[BeanRegister]
public static class OderRepositoryGeneration
{
    [Proxy]
    public static IOderRepository GenerateOderRepository()
    {
        return new OderRepository();
    }
        
    private class OderRepository : IOderRepository
    {
        private readonly Dictionary<string, string> m_Oder2UserMaps = new();
        private readonly Dictionary<string, HashSet<string>> m_User2OderMaps = new();

        public virtual string[] GetOders(string userName)
        {
            if (m_User2OderMaps.ContainsKey(userName))
                return m_User2OderMaps[userName].ToArray();
            throw new Exception("User doesn't exist.");
        }

        public virtual void AddOder(string userName, string oderId)
        {
            if (!m_User2OderMaps.TryGetValue(userName, out var oders))
            {
                oders = new HashSet<string>();
                m_User2OderMaps[userName] = oders;
            }

            if (oders.Contains(oderId))
                throw new Exception("Oder has existed");
            oders.Add(oderId);
            m_Oder2UserMaps.Add(oderId, userName);
        }

        public virtual bool RemoveOder(string oderId)
        {
            if (!m_Oder2UserMaps.ContainsKey(oderId))
                return false;
            var userName = m_Oder2UserMaps[oderId];
            m_Oder2UserMaps.Remove(oderId);
            return m_User2OderMaps.Remove(userName);
        }

        public virtual bool RemoveOrders(string userName)
        {
            if (!m_User2OderMaps.ContainsKey(userName))
                return false;
            foreach (var oder in m_User2OderMaps[userName])
            {
                if (!m_Oder2UserMaps.Remove(oder))
                    throw new Exception("Oder mapping error.");
            }
            return true;
        }
    }
}