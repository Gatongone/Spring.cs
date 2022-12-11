namespace Spring.Tests
{
    public partial class DynamicProxyTests
    {
        [Test]
        public void Test_InterfaceProxyWorked()
        {
            var isWorked = false;
            var proxy = RuntimeAssembly.CreateProxy<IInterfaceTest>()
                .WithInterceptor("Func_String", null, (method,args) =>
                {
                    isWorked = true;
                    return null;
                })
                .GetResult();
            proxy.Func_String();
            TestKits.Assert(isWorked);
        }

        [Test]
        public void Test_ClassProxyWorked()
        {
            var isWorked = false;
            var proxy = RuntimeAssembly.CreateProxy<ClassTest>()
                .WithInterceptor("Func_String", null, (method,args) =>
                {
                    isWorked = true;
                    return method.Invoke();
                })
                .GetResult();
            proxy.Func_String();
            TestKits.Assert(isWorked);
        }

        [Test]
        public void Test_AbstractClassProxyWorked()
        {
            var isWorked = false;
            var proxy = RuntimeAssembly.CreateProxy<AbstractClassTest>()
                .WithInterceptor("Func_String", null, (method,args) =>
                {
                    isWorked = true;
                    return null;
                })
                .GetResult();
            proxy.Func_String();
            TestKits.Assert(isWorked);
        }
    }
}