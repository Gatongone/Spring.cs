namespace Spring.Advices;

public class AroundAttribute : AdviceAttribute
{
    public AroundAttribute(string cutPoint) : base(cutPoint) { }
}