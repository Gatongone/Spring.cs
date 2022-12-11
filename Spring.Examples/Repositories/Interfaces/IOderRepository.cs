namespace Spring.Examples.Repositories
{
    public interface IOderRepository
    {
        string[] GetOders(string userName);
        void AddOder(string userName, string oderId);
        bool RemoveOder(string oderId);
        bool RemoveOrders(string userName);
    }
}