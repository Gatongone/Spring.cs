namespace Spring.Advices
{
    public class BeforeAttribute : AdviceAttribute
    {
        public BeforeAttribute(string cutPoint) : base(cutPoint) { }
    }
}