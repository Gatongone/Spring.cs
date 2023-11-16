using System;

namespace Spring.Tests;

public class ClassTest
{
    public virtual void Func_Void() { }

    public virtual int Func_Params(int num, string str)
    {
        num += 1;
        return num;
    }

    public virtual int Func_Int() => default;
    public virtual StructTest Func_Struct() => default;
    public virtual long Func_Long() => default;
    public virtual decimal Func_Decimal() => Decimal.Zero;
    public virtual float Func_Float() => default;
    public virtual double Func_Double() => default;
    public virtual string Func_String() => "Origin";
}

public class ClassProxyExample : ClassTest
{
    private IMethodReference m_InvokeHandle_Func_Decimal;
    private IMethodReference m_InvokeHandle_Func_Params;
    private IMethodReference m_InvokeHandle_Func_Void;
    public Interceptor interceptor_Func_Params;
    public Interceptor interceptor_Func_Decimal;
    public Interceptor interceptor_Func_Void;

    public ClassProxyExample()
    {
        InitInvokeHandles();
    }

    private void InitInvokeHandles()
    {
        m_InvokeHandle_Func_Decimal = new InvokeHandle(_ProxyMethod_Func_Decimal);
        m_InvokeHandle_Func_Params = new InvokeHandle(_ProxyMethod_Func_Params);
        m_InvokeHandle_Func_Void = new InvokeHandle(_ProxyMethod_Func_Void);
    }

    private object _ProxyMethod_Func_Void(object[] args)
    {
        base.Func_Void();
        return null;
    }

    private object _ProxyMethod_Func_Decimal(object[] args)
    {
        return base.Func_Decimal();
    }

    private object _ProxyMethod_Func_Params(object[] args)
    {
        return base.Func_Params((int) args[0], (string) args[1]);
    }

    public override void Func_Void()
    {
        if (interceptor_Func_Void != null)
        {
            interceptor_Func_Void.Invoke(m_InvokeHandle_Func_Void, null);
        }
        else
            base.Func_Void();
    }

    public override decimal Func_Decimal()
    {
        if (interceptor_Func_Decimal != null)
        {
            return (decimal) interceptor_Func_Decimal.Invoke(m_InvokeHandle_Func_Decimal, null);
        }
        return base.Func_Decimal();
    }

    public override int Func_Params(int num, string str)
    {
        if (interceptor_Func_Params != null)
        {
            return (int) interceptor_Func_Params.Invoke(m_InvokeHandle_Func_Params, new object[] {num, str});
        }
        return base.Func_Params(num, str);
    }
}