using System;
using System.Linq;
using System.Reflection;

namespace Spring
{
    public static class MethodContextExtension
    {
        public static RuntimeAssembly.MethodContext WithCutpointAdvices(this RuntimeAssembly.MethodContext methodContext, MethodInfo method, CutpointAdvices advices)
        {
            return methodContext.WithInterceptor(method.Name, 
                method.GetParameters().Select(param => param.ParameterType).ToArray(), 
                (methodRef, args) =>
            {
                try
                {
                    advices.beforeAdvice?.Invoke();
                    var result = advices.arroundAdvice != null ? advices.arroundAdvice.Invoke(methodRef, args) : methodRef?.Invoke(args);
                    result = advices.afterAdvice?.Invoke(result);
                    return result;
                }
                catch (Exception e)
                {
                    return advices.catchAdvice?.Invoke(e, args);
                }
                finally
                {
                    advices.finallyAdvice?.Invoke();
                }
            });
        }

        public static RuntimeAssembly.MethodContext<T> WithCutpointAdvices<T>(this RuntimeAssembly.MethodContext<T> methodContext, MethodInfo method, CutpointAdvices advices)
        {
            return methodContext.WithInterceptor(method.Name, 
                method.GetParameters().Select(param => param.ParameterType).ToArray(), 
                (methodRef, args) =>
            {
                try
                {
                    advices.beforeAdvice?.Invoke();
                    var result = advices.arroundAdvice != null ? advices.arroundAdvice?.Invoke(methodRef, args) : methodRef?.Invoke(args);
                    result = advices.afterAdvice?.Invoke(result);
                    return result;
                }
                catch (Exception e)
                {
                    return advices.catchAdvice?.Invoke(e, args);
                }
                finally
                {
                    advices.finallyAdvice?.Invoke();
                }
            });
        }
    }
}