namespace Spring
{
    public class PrototypeAttribute : ScopeAttribute
    {
        public PrototypeAttribute() : base(ScopeType.Prototype) { }
    }
}