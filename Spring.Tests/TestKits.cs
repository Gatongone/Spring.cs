using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Spring.Tests
{
    public static class TestKits
    {
        private static readonly Dictionary<string, bool> s_AssertResultMaps = new();

        public static void RunTest(Action action, string testName)
        {
            s_AssertResultMaps[testName] = true;
            action.Invoke();
            var succeed = s_AssertResultMaps[testName];
            Console.WriteLine($"  {GetAssertResultString(succeed)} {testName}");
        }

        public static void AssertNegate(bool result, [CallerMemberName] string testName = "") => s_AssertResultMaps[testName] = s_AssertResultMaps[testName] && !result;
        
        
        public static void Assert(bool result, [CallerMemberName] string testName = "") =>s_AssertResultMaps[testName] = s_AssertResultMaps[testName] && result;
        

        public static void TryAssert(Action action, [CallerMemberName] string testName = "")
        {
            try
            {
                action.Invoke();
            }
            catch (Exception)
            {
                s_AssertResultMaps[testName] = false;
                throw;
            }
        }
        private static string GetAssertResultString(bool result) => result ? "√" : "×";
    }
}