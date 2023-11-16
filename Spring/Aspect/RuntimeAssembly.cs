using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Spring
{
    public delegate object? Interceptor(IMethodReference methodReference, object[] args);

    public static partial class RuntimeAssembly
    {
        private const Mono.Cecil.TypeAttributes TYPE_ATTRIBUTE = Mono.Cecil.TypeAttributes.Class | Mono.Cecil.TypeAttributes.Public;
        private const Mono.Cecil.MethodAttributes CTOR_ATTRIBUTE = Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName;
        private const Mono.Cecil.MethodAttributes IMPLEMENT_METHOD_ATTRIBUTE = Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Final | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.NewSlot | Mono.Cecil.MethodAttributes.Virtual;
        private const Mono.Cecil.MethodAttributes PUBLIC_ABSTRACT_METHOD_ATTRIBUTE = Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.Virtual;
        private const Mono.Cecil.MethodAttributes PROTECTED_ABSTRACT_METHOD_ATTRIBUTE = Mono.Cecil.MethodAttributes.Family | Mono.Cecil.MethodAttributes.HideBySig | Mono.Cecil.MethodAttributes.Virtual;
        private const Mono.Cecil.MethodAttributes PUBLIC_METHOD_ATTRIBUTE = Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.HideBySig;
        private const Mono.Cecil.MethodAttributes PRIVATE_METHOD_ATTRIBUTE = Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.HideBySig;
        private const string ASSEMBLY_NAME_DEFINE = "Spring.Runtime";
        private static readonly Dictionary<string, Type> s_ProxyTypeMaps;
        private static readonly Dictionary<string, AssemblyDefinition> s_AssemblyCacheMaps;
        private static readonly AssemblyDefinition s_SpringDynamicAssemblyDefinition;

        private static byte[] s_AssemblyBuffer;

        // private static readonly MethodReference s_ActionCaller;
        // private static readonly MethodReference s_ActionWithResultCaller;
        // private static readonly MethodReference s_ActionWithExceptionCaller;
        // private static readonly MethodReference s_ActionWithParam;
        private static readonly MethodReference s_InvokeHandleInvoker;

        static RuntimeAssembly()
        {
            //Create runtime assembly definition
            s_ProxyTypeMaps = new Dictionary<string, Type>();
            s_AssemblyCacheMaps = new Dictionary<string, AssemblyDefinition>();
            var assemblyNameDefinition = new AssemblyNameDefinition(ASSEMBLY_NAME_DEFINE, Version.Parse("1.0.0"));
            s_SpringDynamicAssemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyNameDefinition, ASSEMBLY_NAME_DEFINE, ModuleKind.Dll);

            //Set caller
            var mainModule = s_SpringDynamicAssemblyDefinition.MainModule;
            // s_ActionCaller = mainModule.ImportReference(typeof(Action).GetMethod("Invoke", Array.Empty<Type>()));
            // s_ActionWithExceptionCaller = mainModule.ImportReference(typeof(Func<Exception, object>).GetMethod("Invoke", new[] {typeof(Exception)}));
            // s_ActionWithResultCaller = mainModule.ImportReference(typeof(Func<object, object>).GetMethod("Invoke", new[] {typeof(object)}));
            // s_ActionWithParam = mainModule.ImportReference(typeof(Action<object>).GetMethod("Invoke", new[] {typeof(object)}));
            Interceptor a;
            s_InvokeHandleInvoker = mainModule.ImportReference(typeof(Interceptor).GetMethod("Invoke", new[] {typeof(IMethodReference), typeof(object[])}));
        }

        public static MethodContext<T> CreateProxy<T>()
        {
            var targetType = typeof(T);
            if ((targetType.Attributes & System.Reflection.TypeAttributes.Sealed) != 0)
                throw new NonDeriveException();

            var namespaceName = ReasonTypeNamespace(targetType);
            var proxyTypeFullName = $"{namespaceName}.{targetType.Name}";

            if (s_ProxyTypeMaps.TryGetValue(proxyTypeFullName, out var proxyType))
                return new MethodContext<T>(Activator.CreateInstance(proxyType));

            proxyType = CreateProxyType(targetType, namespaceName, proxyTypeFullName);
            return new MethodContext<T>(Activator.CreateInstance(proxyType));
        }

        public static MethodContext CreateProxy(Type targetType)
        {
            if ((targetType.Attributes & System.Reflection.TypeAttributes.Sealed) != 0)
                throw new NonDeriveException();

            var namespaceName = ReasonTypeNamespace(targetType);
            var proxyTypeFullName = $"{namespaceName}.{targetType.Name}";

            if (s_ProxyTypeMaps.TryGetValue(proxyTypeFullName, out var proxyType))
                return new MethodContext(Activator.CreateInstance(proxyType));

            proxyType = CreateProxyType(targetType, namespaceName, proxyTypeFullName);
            return new MethodContext(Activator.CreateInstance(proxyType));
        }

        private static Type? CreateProxyType(Type targetType, string namespaceName, string typeName)
        {
            var mainModule = s_SpringDynamicAssemblyDefinition.MainModule;

            //Return type
            if (s_ProxyTypeMaps.TryGetValue(typeName, out var proxyType))
            {
                return proxyType;
            }

            //Load assembly from location or cache
            var assemblyName = targetType.Assembly.GetName().Name;

            if (!s_AssemblyCacheMaps.TryGetValue(assemblyName, out var assemblyDefinition))
            {
                var readerParameters = new ReaderParameters
                {
                    InMemory = true,
                    ReadWrite = false,
                    ReadingMode = ReadingMode.Deferred
                };
                assemblyDefinition = AssemblyDefinition.ReadAssembly(targetType.Assembly.Location, readerParameters);
                s_AssemblyCacheMaps[assemblyName] = assemblyDefinition;
            }

            //Get TypeDefinition 
            if (!mainModule.AssemblyReferences.Any(nameRef => nameRef.FullName.Equals(assemblyDefinition.FullName)))
            {
                mainModule.AssemblyReferences.Add(assemblyDefinition.Name);
            }
            
            //Get type definition from module types or nestedType
            var type = assemblyDefinition.MainModule.Types
                .FirstOrDefault(type => type.FullName.Equals(targetType.FullName, StringComparison.Ordinal)) ?? assemblyDefinition.MainModule.Types
                .SelectMany(typeDef => typeDef.NestedTypes)
                .FirstOrDefault(nestedType => nestedType.FullName.Replace('/', '+').Equals(targetType.FullName, StringComparison.Ordinal));
            
            //Invalid Type
            if (type == null) throw new ProxyTypeGenerateException($"{targetType} does not exist in main module.");

            //Extern from target type
            var proxyDefinition = InheritFromTargetType(mainModule, type, namespaceName, targetType.Name, out var callerMap);

            //Inject advices
            foreach (var publicMethod in proxyDefinition.Methods.Where(method => method.IsPublic && !method.IsSpecialName && !method.IsRuntimeSpecialName && method.IsVirtual))
            {
                var methodName = GetInterceptorFieldName(publicMethod);
                if (!callerMap.TryGetValue(methodName, out var interceptorInfos))
                    throw new ProxyTypeGenerateException($"Method \"{methodName}\" can't be found.");
                if (interceptorInfos.invokeHandle != null)
                    AddParentMethodInvokerToNonAbstractMethodBody(mainModule, publicMethod, in interceptorInfos.interceptor, in interceptorInfos.invokeHandle, in interceptorInfos.parentMethod);
                else
                    AddParentMethodInvokerToAbstractMethodBody(mainModule, publicMethod, in interceptorInfos.interceptor, in interceptorInfos.parentMethod);
            }

            //Add to module
            mainModule.Types.Add(proxyDefinition);
            mainModule.ImportReference(proxyDefinition);

            //Memory resident
            using (var stream = new MemoryStream())
            {
                s_SpringDynamicAssemblyDefinition.Write(stream);
                s_AssemblyBuffer = stream.GetBuffer();
                stream.Flush();
            }

            //Write into assembly
            var assembly = Assembly.Load(s_AssemblyBuffer);
            proxyType = assembly.GetType(typeName);

            if (proxyType != null && !s_ProxyTypeMaps.ContainsKey(typeName))
            {
                s_ProxyTypeMaps.Add(typeName, proxyType);
            }

            return proxyType;
        }

        private static void AddParentMethodInvokerToAbstractMethodBody(ModuleDefinition module, MethodDefinition publicMethod, in FieldDefinition interceptor, in MethodDefinition parentMethod)
        {
            VariableDefinition retVar = null;
            VariableDefinition retVar2 = null;
            VariableDefinition boolVar = new VariableDefinition(module.ImportReference(typeof(bool)));
            var isVoid = publicMethod.ReturnType.FullName.Equals(module.ImportReference(typeof(void)).FullName, StringComparison.Ordinal);
            var returnRef = module.ImportReference(publicMethod.ReturnType);
            var ins = publicMethod.Body.Instructions;
            var brsEnd = Instruction.Create(OpCodes.Ret);
            var castType = publicMethod.ReturnType.IsValueType ? Instruction.Create(OpCodes.Unbox_Any, returnRef) : Instruction.Create(OpCodes.Castclass, returnRef);
            var isBasicValue = CreateBasicTypeDefaultValue(returnRef, out var brfalseEnd, out var ins1);
            publicMethod.Body.Variables.Add(boolVar);
            if (!isVoid)
            {
                retVar = new VariableDefinition(returnRef);
                publicMethod.Body.Variables.Add(retVar);
                if (!returnRef.IsValueType)
                {
                    brfalseEnd = Instruction.Create(OpCodes.Ldnull);
                }
                else if (!isBasicValue)
                {
                    retVar2 = new VariableDefinition(returnRef);
                    publicMethod.Body.Variables.Add(retVar2);
                    brfalseEnd = Instruction.Create(OpCodes.Ldloca_S, retVar2);
                }
            }
            else
            {
                brfalseEnd = Instruction.Create(OpCodes.Ret);
            }

            ins.Add(Instruction.Create(OpCodes.Ldarg_0));
            ins.Add(Instruction.Create(OpCodes.Ldfld, interceptor));
            ins.Add(Instruction.Create(OpCodes.Ldnull));
            ins.Add(Instruction.Create(OpCodes.Cgt_Un));
            ins.Add(Instruction.Create(OpCodes.Stloc, boolVar));
            ins.Add(Instruction.Create(OpCodes.Ldloc, boolVar));
            ins.Add(Instruction.Create(OpCodes.Brfalse_S, brfalseEnd));

            ins.Add(Instruction.Create(OpCodes.Ldarg_0));
            ins.Add(Instruction.Create(OpCodes.Ldfld, interceptor));
            ins.Add(Instruction.Create(OpCodes.Ldnull));
            var parameters = publicMethod.Parameters;
            if (parameters.Count > 0)
            {
                ins.Add(Instruction.Create(OpCodes.Ldc_I4, publicMethod.Parameters.Count));
                ins.Add(Instruction.Create(OpCodes.Newarr, module.ImportReference(typeof(object))));
                for (var index = 0; index < parameters.Count; index++)
                {
                    var parameter = parameters[index];
                    ins.Add(Instruction.Create(OpCodes.Dup));
                    ins.Add(Instruction.Create(OpCodes.Ldc_I4, index));
                    ins.Add(Instruction.Create(OpCodes.Ldarg, parameter));
                    if (parameter.ParameterType.IsValueType)
                    {
                        ins.Add(Instruction.Create(OpCodes.Box, module.ImportReference(parameter.ParameterType)));
                    }

                    ins.Add(Instruction.Create(OpCodes.Stelem_Ref));
                }
            }
            else
                ins.Add(Instruction.Create(OpCodes.Ldnull));

            ins.Add(Instruction.Create(OpCodes.Callvirt, s_InvokeHandleInvoker));
            if (!isVoid)
            {
                var ldloc = Instruction.Create(OpCodes.Ldloc, retVar);
                ins.Add(castType);
                ins.Add(Instruction.Create(OpCodes.Stloc, retVar));
                ins.Add(Instruction.Create(OpCodes.Br_S, ldloc));

                ins.Add(brfalseEnd);
                if (isBasicValue)
                {
                    ins.Add(ins1);
                }
                else if (returnRef.IsValueType)
                {
                    ins.Add(Instruction.Create(OpCodes.Initobj, returnRef));
                    ins.Add(Instruction.Create(OpCodes.Ldloc, retVar2));
                }

                ins.Add(Instruction.Create(OpCodes.Stloc, retVar));
                ins.Add(Instruction.Create(OpCodes.Br_S, ldloc));
                ins.Add(ldloc);
                ins.Add(Instruction.Create(OpCodes.Ret));
            }
            else
            {
                ins.Add(Instruction.Create(OpCodes.Pop));
                ins.Add(brfalseEnd);
            }
        }

        private static void AddParentMethodInvokerToNonAbstractMethodBody(ModuleDefinition module, MethodDefinition publicMethod, in FieldDefinition interceptor, in FieldDefinition invokeHandle, in MethodDefinition parentMethod)
        {
            VariableDefinition retVar = null;
            var isVoid = publicMethod.ReturnType.FullName.Equals(module.ImportReference(typeof(void)).FullName, StringComparison.Ordinal);
            var returnRef = module.ImportReference(publicMethod.ReturnType);
            var ins = publicMethod.Body.Instructions;

            publicMethod.Body.Variables.Add(new VariableDefinition(module.ImportReference(typeof(bool))));

            if (!isVoid)
            {
                publicMethod.Body.InitLocals = true;
                retVar = new VariableDefinition(returnRef);
                publicMethod.Body.Variables.Add(retVar);
            }

            var brfalseEnd = Instruction.Create(OpCodes.Ldarg_0);
            var ldLoc = isVoid ? null : Instruction.Create(OpCodes.Ldloc, retVar);
            var brsEnd = isVoid ? Instruction.Create(OpCodes.Ret) : ldLoc;
            var castType = publicMethod.ReturnType.IsValueType ? Instruction.Create(OpCodes.Unbox_Any, returnRef) : Instruction.Create(OpCodes.Castclass, returnRef);

            ins.Add(Instruction.Create(OpCodes.Ldarg_0));
            ins.Add(Instruction.Create(OpCodes.Ldfld, interceptor));
            ins.Add(Instruction.Create(OpCodes.Ldnull));
            ins.Add(Instruction.Create(OpCodes.Cgt_Un));
            ins.Add(Instruction.Create(OpCodes.Stloc_0));
            ins.Add(Instruction.Create(OpCodes.Ldloc_0));
            ins.Add(Instruction.Create(OpCodes.Brfalse_S, brfalseEnd));
            ins.Add(Instruction.Create(OpCodes.Nop));
            ins.Add(Instruction.Create(OpCodes.Ldarg_0));
            ins.Add(Instruction.Create(OpCodes.Ldfld, interceptor));
            ins.Add(Instruction.Create(OpCodes.Ldarg_0));
            ins.Add(Instruction.Create(OpCodes.Ldfld, invokeHandle));
            //Add Params
            var parameters = publicMethod.Parameters;
            if (parameters.Count > 0)
            {
                ins.Add(Instruction.Create(OpCodes.Ldc_I4, parameters.Count));
                ins.Add(Instruction.Create(OpCodes.Newarr, module.ImportReference(typeof(object))));

                for (var index = 0; index < parameters.Count; index++)
                {
                    var parameter = parameters[index];
                    ins.Add(Instruction.Create(OpCodes.Dup));
                    ins.Add(Instruction.Create(OpCodes.Ldc_I4, index));
                    ins.Add(Instruction.Create(OpCodes.Ldarg, parameter));
                    if (parameter.ParameterType.IsValueType)
                    {
                        ins.Add(Instruction.Create(OpCodes.Box, module.ImportReference(parameter.ParameterType)));
                    }

                    ins.Add(Instruction.Create(OpCodes.Stelem_Ref));
                }
            }
            else
                ins.Add(Instruction.Create(OpCodes.Ldnull));

            ins.Add(Instruction.Create(OpCodes.Callvirt, s_InvokeHandleInvoker));
            if (!isVoid)
            {
                ins.Add(castType);
                ins.Add(Instruction.Create(OpCodes.Stloc, retVar));
            }
            else
                ins.Add(Instruction.Create(OpCodes.Pop));

            ins.Add(Instruction.Create(OpCodes.Br_S, brsEnd));
            ins.Add(brfalseEnd);
            for (var index = 0; index < parameters.Count; index++)
            {
                ins.Add(Instruction.Create(OpCodes.Ldarg, parameters[index]));
            }

            ins.Add(Instruction.Create(OpCodes.Call, module.ImportReference(parentMethod)));
            if (retVar != null)
            {
                ins.Add(Instruction.Create(OpCodes.Stloc, retVar));
                ins.Add(Instruction.Create(OpCodes.Br_S, ldLoc));
            }

            ins.Add(brsEnd);
            if (!isVoid)
            {
                ins.Add(Instruction.Create(OpCodes.Ret));
            }
        }

        // private static void CreateAdviceOnMethod(ModuleDefinition module, TypeDefinition type, MethodDefinition method)
        // {
        //     var beforeField = new FieldDefinition(GetBeforeAdviceFieldName(method), Mono.Cecil.FieldAttributes.Private, module.ImportReference(typeof(Action)));
        //     var afterField = new FieldDefinition(GetAfterAdviceFieldName(method), Mono.Cecil.FieldAttributes.Private, module.ImportReference(typeof(Func<object, object>)));
        //     var throwField = new FieldDefinition(GetThrowAdviceFieldName(method), Mono.Cecil.FieldAttributes.Private, module.ImportReference(typeof(Func<Exception, object>)));
        //     var finallyField = new FieldDefinition(GetFinallyAdviceFieldName(method), Mono.Cecil.FieldAttributes.Private, module.ImportReference(typeof(Action<object>)));
        //     var aroundField = new FieldDefinition(GetFinallyAdviceFieldName(method), Mono.Cecil.FieldAttributes.Private, module.ImportReference(typeof(Action<IMethodReference>)));
        //
        //     type.Fields.Add(beforeField);
        //     type.Fields.Add(afterField);
        //     type.Fields.Add(throwField);
        //     type.Fields.Add(finallyField);
        //     type.Fields.Add(aroundField);
        //
        //     //Add advice actions' calling
        //     var isVirtual = method.IsVirtual;
        //     var isVoid = method.ReturnType.FullName == module.ImportReference(typeof(void)).FullName;
        //     var isValue = method.ReturnType.IsValueType;
        //     var ins = method.Body.Instructions;
        //     var il = method.Body.GetILProcessor();
        //     var resultType = module.ImportReference(method.ReturnType);
        //     var resultVar = new VariableDefinition(resultType);
        //     var returnVar = new VariableDefinition(resultType);
        //     var exceptionVar = new VariableDefinition(module.ImportReference(typeof(Exception)));
        //     var handleEnd = isVoid ? il.Create(OpCodes.Nop) : il.Create(OpCodes.Ldloc, returnVar);
        //     var beforeCall = il.Create(OpCodes.Callvirt, s_ActionCaller);
        //     var afterCall = il.Create(OpCodes.Callvirt, s_ActionWithResultCaller);
        //     var throwCall = il.Create(OpCodes.Callvirt, s_ActionWithExceptionCaller);
        //     var finallyCall = il.Create(OpCodes.Callvirt, s_ActionWithParam);
        //     var ret = il.Create(OpCodes.Ret);
        //     var tryStart = il.Create(OpCodes.Nop);
        //     var tryEnd = il.Create(OpCodes.Nop);
        //     var finallyStart = il.Create(OpCodes.Nop);
        //     var finallyEnd = il.Create(OpCodes.Endfinally);
        //     var finallyCallEnd = il.Create(OpCodes.Nop);
        //     var first = ins.FirstOrDefault();
        //     var beforeLdarg_0 = ins[0];
        //
        //     //Add local variables
        //     method.Body.InitLocals = true;
        //     if (!isVoid)
        //     {
        //         method.Body.Variables.Add(resultVar);
        //         method.Body.Variables.Add(returnVar);
        //     }
        //
        //     method.Body.Variables.Add(exceptionVar);
        //
        //     #region ResultInit
        //
        //     //If method has return type, it should be init on beginning
        //     if (!isVoid)
        //     {
        //         //Store result variable
        //         if (!isVirtual)
        //             il.InsertAfter(ins.Last(), il.Create(OpCodes.Stloc, resultVar));
        //
        //         //Init result
        //         if (!isValue)
        //         {
        //             il.InsertBefore(ins.FirstOrDefault(), il.Create(OpCodes.Stloc, resultVar));
        //             il.InsertBefore(ins.FirstOrDefault(), il.Create(OpCodes.Ldnull));
        //         }
        //         else
        //         {
        //             if (CreateBasicTypeDefaultValue(resultType, out var instruction1, out var instruction2))
        //             {
        //                 il.InsertBefore(ins.FirstOrDefault(), il.Create(OpCodes.Stloc, resultVar));
        //                 if (instruction2 != null)
        //                 {
        //                     il.InsertBefore(ins.FirstOrDefault(), instruction2);
        //                 }
        //
        //                 il.InsertBefore(ins.FirstOrDefault(), instruction1);
        //             }
        //             else
        //             {
        //                 il.InsertBefore(ins.FirstOrDefault(), il.Create(OpCodes.Initobj, resultType));
        //                 il.InsertBefore(ins.FirstOrDefault(), il.Create(OpCodes.Ldloca_S, resultVar));
        //             }
        //         }
        //     }
        //
        //     #endregion
        //
        //     //Try start
        //     il.InsertBefore(first, tryStart);
        //
        //     #region Before
        //
        //     //Before
        //     il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
        //     il.InsertBefore(first, il.Create(OpCodes.Ldfld, beforeField));
        //     il.InsertBefore(first, il.Create(OpCodes.Dup));
        //     il.InsertBefore(first, il.Create(OpCodes.Brtrue_S, beforeCall));
        //     il.InsertBefore(first, il.Create(OpCodes.Pop));
        //     il.InsertBefore(first, il.Create(OpCodes.Br_S, beforeLdarg_0));
        //     il.InsertBefore(first, beforeCall);
        //
        //     #endregion
        //
        //     #region After
        //
        //     //After
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldarg_0));
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldfld, afterField));
        //     if (!isVoid)
        //     {
        //         var resultLdloc = il.Create(OpCodes.Ldloc, resultVar);
        //         var castClass = il.Create(isValue ? OpCodes.Unbox_Any : OpCodes.Castclass, resultType);
        //         var boolVar = new VariableDefinition(module.ImportReference(typeof(bool)));
        //         method.Body.Variables.Add(boolVar);
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldnull));
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Cgt_Un));
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Stloc, boolVar));
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldloc, boolVar));
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Brfalse_S, resultLdloc));
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldarg_0));
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldfld, afterField));
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldloc, resultVar));
        //         if (isValue)
        //             il.InsertAfter(ins.Last(), il.Create(OpCodes.Box, resultType));
        //         il.InsertAfter(ins.Last(), afterCall);
        //         il.InsertAfter(ins.Last(), castClass);
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Stloc, resultVar));
        //         il.InsertAfter(ins.Last(), resultLdloc);
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Stloc, returnVar));
        //     }
        //     else
        //     {
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Dup));
        //         var ldNull = il.Create(OpCodes.Ldnull);
        //         var nop = il.Create(OpCodes.Nop);
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Brtrue_S, ldNull));
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Pop));
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Br_S, nop));
        //         il.InsertAfter(ins.Last(), ldNull);
        //         il.InsertAfter(ins.Last(), afterCall);
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Pop));
        //         il.InsertAfter(ins.Last(), nop);
        //     }
        //
        //     #endregion
        //
        //     //TryEnd
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Leave, handleEnd));
        //     //CatchStart
        //     il.InsertAfter(ins.Last(), tryEnd);
        //
        //     #region Catch
        //
        //     //Try-Catch
        //     var exceptionLdloc = il.Create(OpCodes.Ldloc, exceptionVar);
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Stloc, exceptionVar));
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldarg_0));
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldfld, throwField));
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Dup));
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Brtrue_S, exceptionLdloc));
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Pop));
        //     if (!isVoid)
        //     {
        //         var castClass = il.Create(isValue ? OpCodes.Unbox_Any : OpCodes.Castclass, resultType);
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldnull));
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Br_S, castClass));
        //         il.InsertAfter(ins.Last(), exceptionLdloc);
        //         il.InsertAfter(ins.Last(), throwCall);
        //         il.InsertAfter(ins.Last(), castClass);
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Stloc, returnVar));
        //     }
        //     else
        //     {
        //         var nop = il.Create(OpCodes.Nop);
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Br_S, nop));
        //         il.InsertAfter(ins.Last(), exceptionLdloc);
        //         il.InsertAfter(ins.Last(), throwCall);
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Pop));
        //         il.InsertAfter(ins.Last(), nop);
        //     }
        //
        //     #endregion
        //
        //     //CatchEnd
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Leave, handleEnd));
        //
        //     #region Finally
        //
        //     //Finally
        //     var finallyLdloc = isVoid ? il.Create(OpCodes.Ldnull) : il.Create(OpCodes.Ldloc, resultVar);
        //     il.InsertAfter(ins.Last(), finallyStart);
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldarg_0));
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Ldfld, finallyField));
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Dup));
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Brtrue_S, finallyLdloc));
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Pop));
        //     il.InsertAfter(ins.Last(), il.Create(OpCodes.Br_S, finallyCallEnd));
        //     il.InsertAfter(ins.Last(), finallyLdloc);
        //     if (!isVoid)
        //         il.InsertAfter(ins.Last(), il.Create(OpCodes.Box, resultType));
        //     il.InsertAfter(ins.Last(), finallyCall);
        //     il.InsertAfter(ins.Last(), finallyCallEnd);
        //     il.InsertAfter(ins.Last(), finallyEnd);
        //
        //     #endregion
        //
        //     //End
        //     il.InsertAfter(ins.Last(), handleEnd);
        //     il.InsertAfter(handleEnd, ret);
        //
        //     //Add catch handle
        //     var catchHandler = new ExceptionHandler(ExceptionHandlerType.Catch)
        //     {
        //         TryStart = tryStart,
        //         TryEnd = tryEnd,
        //         HandlerStart = tryEnd,
        //         HandlerEnd = handleEnd,
        //         CatchType = module.ImportReference(typeof(Exception))
        //     };
        //     //Add finally handle
        //     var finallyHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
        //     {
        //         TryStart = ins.FirstOrDefault(),
        //         TryEnd = handleEnd,
        //         HandlerStart = finallyStart,
        //         HandlerEnd = finallyEnd.Next
        //     };
        //     method.Body.ExceptionHandlers.Add(catchHandler);
        //     method.Body.ExceptionHandlers.Add(finallyHandler);
        // }

        /// <summary>
        /// Create basic value type's instruction
        /// </summary>
        /// <param name="typeReference">type which has imported</param>
        /// <param name="instruction_1">first instruction if it have</param>
        /// <param name="instruction_2">second instruction if it have</param>
        /// <returns>Is basic value type exclude struct</returns>
        private static bool CreateBasicTypeDefaultValue(TypeReference typeReference, out Instruction instruction_1, out Instruction instruction_2)
        {
            instruction_1 = instruction_2 = Instruction.Create(OpCodes.Nop);
            if (!typeReference.IsValueType)
            {
                return false;
            }

            switch (typeReference.Name)
            {
                case "Int16":
                case "Int32":
                case "UInt16":
                case "UInt32":
                case "SByte":
                case "Byte":
                case "Boolean":
                case "Char":
                    instruction_1 = Instruction.Create(OpCodes.Ldc_I4_0);
                    return true;
                case "Int64":
                case "UInt64":
                    instruction_1 = Instruction.Create(OpCodes.Ldc_I4_0);
                    instruction_2 = Instruction.Create(OpCodes.Conv_I8);
                    return true;
                case "Single":
                    instruction_1 = Instruction.Create(OpCodes.Ldc_R4, 0.0f);
                    return true;
                case "Double":
                    instruction_1 = Instruction.Create(OpCodes.Ldc_R8, 0.0d);
                    return true;
                case "Decimal":
                    instruction_1 = Instruction.Create(OpCodes.Ldsfld, typeReference.Module.ImportReference(
                        typeReference.Module.ImportReference(typeof(decimal))
                            .Resolve().Fields
                            .First(field => field.Name.Equals("Zero"))));
                    return true;
            }

            return false;
        }

        private static void CreateEmptyMethodBody(ModuleDefinition mainModule, MethodDefinition methodDefinition, MethodDefinition parentMethod)
        {
            var ins = methodDefinition.Body.Instructions;
            if (parentMethod.IsPublic)
            {
                ins.Add(Instruction.Create(OpCodes.Nop));
                return;
            }

            if (parentMethod.ReturnType.FullName == mainModule.ImportReference(typeof(void)).FullName)
            {
                ins.Add(Instruction.Create(OpCodes.Nop));
            }
            else if (!parentMethod.ReturnType.IsValueType)
            {
                ins.Add(Instruction.Create(OpCodes.Ldnull));
            }
            else
            {
                if (CreateBasicTypeDefaultValue(mainModule.ImportReference(methodDefinition.ReturnType), out var instruction1, out var instruction2))
                {
                    ins.Add(instruction1);
                    if (instruction2 != null)
                    {
                        ins.Add(instruction2);
                    }
                }
                else
                {
                    var valueType = mainModule.ImportReference(parentMethod.ReturnType);
                    var valueVar = new VariableDefinition(valueType);
                    methodDefinition.Body.Variables.Add(valueVar);
                    ins.Add(Instruction.Create(OpCodes.Ldloca_S, valueVar));
                    ins.Add(Instruction.Create(OpCodes.Initobj, valueType));
                    ins.Add(Instruction.Create(OpCodes.Ldloc, valueVar));
                }
            }

            ins.Add(Instruction.Create(OpCodes.Ret));
        }
    }
}