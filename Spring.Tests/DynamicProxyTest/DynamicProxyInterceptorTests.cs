namespace Spring.Tests;

public partial class DynamicProxyTests
{
    [Test]
    public void Test_InterceptorWithParams()
    {
        var proxy = RuntimeAssembly.CreateProxy<ClassTest>()
            .WithInterceptor("Func_Params", new[] {typeof(int), typeof(string)}, (method, args) =>
            {
                args[0] = 1;
                return method.Invoke(args);
            })
            .GetResult();
        TestKits.Assert(proxy.Func_Params(0, null) == 2);
    }

    [Test]
    public void Test_InterceptorReturnWithRef()
    {
        var resultKey = "Succeed";
        var proxy = RuntimeAssembly.CreateProxy<ClassTest>()
            .WithInterceptor("Func_String", null, (_, args) => resultKey)
            .GetResult();
        TestKits.Assert(proxy.Func_String().Equals(resultKey));
    }

    [Test]
    public void Test_InterceptorReturnWithStruct()
    {
        var proxy = RuntimeAssembly.CreateProxy<ClassTest>()
            .WithInterceptor("Func_Struct", null, (_, args) => new StructTest(1))
            .GetResult();
        TestKits.AssertNegate(proxy.Func_Struct().num == 0);
    }

    [Test]
    public void Test_InterceptorReturnWithInt32()
    {
        var proxy = RuntimeAssembly.CreateProxy<ClassTest>()
            .WithInterceptor("Func_Int", null, (_, args) => 1)
            .GetResult();
        TestKits.AssertNegate(proxy.Func_Int() == 0);
    }

    [Test]
    public void Test_InterceptorReturnWithInt64()
    {
        var proxy = RuntimeAssembly.CreateProxy<ClassTest>()
            .WithInterceptor("Func_Long", null, (_, args) => 1l)
            .GetResult();
        TestKits.AssertNegate(proxy.Func_Long() == 0);
    }

    [Test]
    public void Test_InterceptorReturnWithFloat32()
    {
        var proxy = RuntimeAssembly.CreateProxy<ClassTest>()
            .WithInterceptor("Func_Float", null, (_, args) => 1f)
            .GetResult();
        TestKits.Assert(proxy.Func_Float() > 0f);
    }

    [Test]
    public void Test_InterceptorReturnWithFloat64()
    {
        var proxy = RuntimeAssembly.CreateProxy<ClassTest>()
            .WithInterceptor("Func_Double", null, (_, args) => 1d)
            .GetResult();
        TestKits.Assert(proxy.Func_Double() > 0d);
    }

    [Test]
    public void Test_InterceptorReturnWithDecimal()
    {
        var proxy = RuntimeAssembly.CreateProxy<ClassTest>()
            .WithInterceptor("Func_Decimal", null, (_, args) => decimal.One)
            .GetResult();
        TestKits.AssertNegate(proxy.Func_Decimal() == decimal.Zero);
    }
}