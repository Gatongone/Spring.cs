namespace Spring;

public class NonDeriveException : ProxyTypeGenerateException
{
    public NonDeriveException() : base("Type can't be sealed.") { }
}