namespace Spring.Tests;

public abstract class AbstractClassTest
{
    public abstract void Func_Void();
    public abstract void Func_Void_Params(object arg0);
    public abstract int Func_Int();
    public abstract StructTest Func_Struct();
    public abstract long Func_Long();
    public abstract decimal Func_Decimal();
    public abstract float Func_Float();
    public abstract double Func_Double();
    public abstract string Func_String();
}

public class AbstractClassProxyExample : AbstractClassTest
{
    public override void Func_Void()
    {
        throw new System.NotImplementedException();
    }

    public override void Func_Void_Params(object arg0)
    {
        throw new System.NotImplementedException();
    }

    public override int Func_Int()
    {
        throw new System.NotImplementedException();
    }

    public override StructTest Func_Struct()
    {
        throw new System.NotImplementedException();
    }

    public override long Func_Long()
    {
        throw new System.NotImplementedException();
    }

    public override decimal Func_Decimal()
    {
        throw new System.NotImplementedException();
    }

    public override float Func_Float()
    {
        throw new System.NotImplementedException();
    }

    public override double Func_Double()
    {
        throw new System.NotImplementedException();
    }

    public override string Func_String()
    {
        throw new System.NotImplementedException();
    }
}