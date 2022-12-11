using System;
using Spring.Advices;

namespace Spring.Examples.Aspect
{
    [Aspect]
    public static class OderServiceAspect
    {
        [Cutpoint("cutpoint", "[Spring.Examples][*][*][*(*)]")]
        public static void CutPoint() { }

        [After("cutpoint")]
        public static object AfterMethod(object result)
        {
            Console.WriteLine($"{LogTags.ASPECT}{LogTags.LOG} After");
            return result;
        }

        [Before("cutpoint")]
        public static void BeforeMethod()
        {
            Console.WriteLine($"{LogTags.ASPECT}{LogTags.LOG} Before");
        }

        [Catch("cutpoint")]
        public static object CatchMethod(Exception ex,object[] args)
        {
            Console.WriteLine($"{LogTags.ASPECT}{LogTags.LOG} Catch");
            return null;
        }
        
        [Finally("cutpoint")]
        public static void FinallyMethod()
        {
            Console.WriteLine($"{LogTags.ASPECT}{LogTags.LOG} Finally");
        }
        
        [Around("cutpoint")]
        public static object AroundMethod(IMethodReference methodReference,object[] args)
        {
            Console.WriteLine($"{LogTags.ASPECT}{LogTags.LOG} Around");
            return methodReference?.Invoke(args);
        }
    }
}