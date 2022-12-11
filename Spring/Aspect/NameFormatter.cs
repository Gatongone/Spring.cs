using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Spring
{
    public static partial class RuntimeAssembly
    {
        private static string ReasonTypeNamespace(Type targetType) => $"{ASSEMBLY_NAME_DEFINE}.{targetType.Assembly.GetName().Name}.{targetType.Namespace ?? "Default"}";
        private static string GetInvokeHandleFieldName(MethodReference method) => $"invoke_handle_{method.Name}{MergeParamsString(method.Parameters)}";
        private static string GetInterceptorFieldName(MethodReference method) => $"interceptor_{method.Name}{MergeParamsString(method.Parameters)}";
        // private static string GetBeforeAdviceFieldName(MethodReference method) => $"advice_before_{method.Name}{MergeParamsString(method.Parameters)}";
        // private static string GetAfterAdviceFieldName(MethodReference method) => $"advice_after_{method.Name}{MergeParamsString(method.Parameters)}";
        // private static string GetThrowAdviceFieldName(MethodReference method) => $"advice_throw_{method.Name}{MergeParamsString(method.Parameters)}";
        // private static string GetFinallyAdviceFieldName(MethodReference method) => $"advice_finally_{method.Name}{MergeParamsString(method.Parameters)}";
        // private static string GetBeforeAdviceFieldName(string methodName, IReadOnlyCollection<Type>? parameters) => $"advice_before_{methodName}{MergeParamsString(parameters)}";
        // private static string GetAfterAdviceFieldName(string methodName, IReadOnlyCollection<Type>? parameters) => $"advice_after_{methodName}{MergeParamsString(parameters)}";
        // private static string GetThrowAdviceFieldName(string methodName, IReadOnlyCollection<Type>? parameters) => $"advice_throw_{methodName}{MergeParamsString(parameters)}";
        // private static string GetFinallyAdviceFieldName(string methodName, IReadOnlyCollection<Type>? parameters) => $"advice_finally_{methodName}{MergeParamsString(parameters)}";
        public static string GetInterceptorFieldName(string methodName, IReadOnlyCollection<Type>? parameters) => $"interceptor_{methodName}{MergeParamsString(parameters)}";
        public static string GetInvokeHandleFieldName(string methodName, IReadOnlyCollection<Type>? parameters) => $"invoke_handle_{methodName}{MergeParamsString(parameters)}";

        private static string MergeParamsString(IReadOnlyCollection<Type>? parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return string.Empty;
            var paramFormat = parameters.Select(param => param.FullName?.Replace(".", "__"));
            var strBuilder = new StringBuilder();
            strBuilder.Append("_");
            foreach (var str in paramFormat)
            {
                if (str == null)
                    throw new ArgumentNullException();
                strBuilder.Append(str);
                strBuilder.Append("__");
            }

            strBuilder.Remove(strBuilder.Length - 2, 2);
            return strBuilder.ToString();
        }

        private static string MergeParamsString(IEnumerable<ParameterDefinition>? parameters)
        {
            if (parameters == null || !parameters.Any())
                return string.Empty;
            var paramFormat = parameters.Select(param => param.ParameterType.FullName.Replace(".", "__"));
            var strBuilder = new StringBuilder();
            strBuilder.Append("_");
            foreach (var str in paramFormat)
            {
                strBuilder.Append(str);
                strBuilder.Append("__");
            }

            strBuilder.Remove(strBuilder.Length - 2, 2);
            return strBuilder.ToString();
        }
    }
}