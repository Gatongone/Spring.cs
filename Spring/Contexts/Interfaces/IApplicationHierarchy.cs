namespace Spring
{
    public interface IApplicationHierarchy : IApplicationContext
    {
        IApplicationHierarchy GetImplement();
    }
}