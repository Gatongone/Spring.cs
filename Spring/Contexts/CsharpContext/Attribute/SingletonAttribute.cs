namespace Spring
{
    public class SingletonAttribute : ScopeAttribute
    {
        public SingletonAttribute() : base(ScopeType.Singleton) { }
    }
}