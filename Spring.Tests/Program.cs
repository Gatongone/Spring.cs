using System;

namespace Spring.Tests;

public class Program
{
    public interface ITest
    {
        object MakeMoney(int count);
    }

    static void Main(string[] args)
    {
        var proxy = RuntimeAssembly.CreateProxy<ITest>().WithInterceptor("MakeMoney", new[] {typeof(int)}, (method, args) =>
        {
            Console.WriteLine(args[0]);
            return "FUCKER";
        }).GetResult();
        Console.WriteLine(proxy.MakeMoney(4));
                
        // foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        // {
        //     var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
        //         .Where(method => method.GetCustomAttribute<TestAttribute>() != null);
        //     if (!methods.Any())
        //         continue;
        //     Console.WriteLine($"{type.Name}: ");
        //     object instance;
        //
        //     if ((type.Attributes & TypeAttributes.Sealed) != 0 && (type.Attributes & TypeAttributes.Abstract) != 0)
        //         instance = null;
        //     else
        //         instance = Activator.CreateInstance(type);
        //
        //     foreach (var method in methods)
        //     {
        //         TestKits.RunTest(() => method.Invoke(instance, null), method.Name);
        //     }
        // }
    }
}