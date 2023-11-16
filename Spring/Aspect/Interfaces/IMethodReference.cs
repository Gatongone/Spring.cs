namespace Spring;

public interface IMethodReference
{
    public object Invoke(params object[] args);
}