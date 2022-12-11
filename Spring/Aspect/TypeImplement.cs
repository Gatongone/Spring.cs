using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Spring
{
    public static partial class RuntimeAssembly
    {
        //invokeHandle will be null when type is abstract.
        private static TypeDefinition InheritFromTargetType(ModuleDefinition module, TypeDefinition targetType, string namespaceName, string className, out Dictionary<string, (FieldDefinition interceptor, FieldDefinition? invokeHandle, MethodDefinition parentMethod)> callerMap)
        {
            var proxyDefinition = new TypeDefinition(namespaceName, className, TYPE_ATTRIBUTE);

            //Extend from parent
            ExtendsFromClass(module, proxyDefinition, targetType);

            var invokeHandleInit = AddInvokeHandles(module, proxyDefinition, targetType, out callerMap);

            //Override abstract methods
            OverrideNonAbstractMethods(module, proxyDefinition, targetType);

            //Create ctors
            CreateConstructors(invokeHandleInit, module, proxyDefinition, targetType);
            return proxyDefinition;
        }
        
        //invokeHandle will be null when type is abstract.
        private static void OverrideNonAbstractMethods(ModuleDefinition module, TypeDefinition originType, TypeDefinition parentType)
        {
            var virtualMethods = parentType.Methods.Where(method => !method.IsFinal && method.IsPublic && method.IsVirtual && !method.IsSpecialName && !method.IsSpecialName && !method.IsRuntimeSpecialName && !method.IsAbstract).ToArray();
            if (virtualMethods.Length == 0)
                return;
            //Build override methods
            for (var index = virtualMethods.Length - 1; index >= 0; index--)
            {
                var parentMethod = virtualMethods[index];
                var returnRef = module.ImportReference(parentMethod.ReturnType);
                //override the parent's virtual method
                var overrideMethod = new MethodDefinition(parentMethod.Name, PUBLIC_METHOD_ATTRIBUTE | MethodAttributes.Virtual, returnRef);
                CopyParameters(module, overrideMethod, parentMethod);
                originType.Methods.Add(overrideMethod);
            }
        }

        private static void ExtendsFromClass(ModuleDefinition module, TypeDefinition originType, TypeDefinition parentType)
        {
            var typeRef = module.ImportReference(parentType);
            if (!parentType.IsInterface)
            {
                originType.BaseType = typeRef;
                if (parentType.IsAbstract)
                {
                    foreach (var method in parentType.Methods.Where(method => !method.IsSpecialName && !method.IsRuntimeSpecialName && method.IsAbstract && !method.IsPrivate && method.IsVirtual))
                    {
                        var methodDefinition = new MethodDefinition(method.Name, method.IsFamily ? PROTECTED_ABSTRACT_METHOD_ATTRIBUTE : PUBLIC_ABSTRACT_METHOD_ATTRIBUTE, module.ImportReference(method.ReturnType));
                        CopyParameters(module, methodDefinition, method);
                        CreateEmptyMethodBody(module, methodDefinition, method);
                        originType.Methods.Add(methodDefinition);
                    }
                }
            }
            else
            {
                originType.BaseType = module.ImportReference(typeof(object));
                originType.Interfaces.Add(new InterfaceImplementation(typeRef));
                foreach (var method in parentType.Methods.Where(method => !method.IsSpecialName && !method.IsRuntimeSpecialName && method.IsVirtual))
                {
                    var methodDefinition = new MethodDefinition(method.Name, IMPLEMENT_METHOD_ATTRIBUTE, module.ImportReference(method.ReturnType));
                    CopyParameters(module, methodDefinition, method);
                    CreateEmptyMethodBody(module, methodDefinition, method);
                    originType.Methods.Add(methodDefinition);
                }
            }
        }

        private static void CopyParameters(ModuleDefinition module, IMethodSignature origin, IMethodSignature target)
        {
            foreach (var parameter in target.Parameters)
            {
                origin.Parameters.Add(new ParameterDefinition(module.ImportReference(parameter.ParameterType)));
            }
        }

        private static void CreateConstructors(MethodReference invokeHandleInit, ModuleDefinition module, TypeDefinition originType, TypeDefinition parentType)
        {
            var parentCtors = parentType.Methods.Where(method => method.IsConstructor && method.IsPublic);

            //Non params ctor
            var emptyCtor = new MethodDefinition(".ctor", CTOR_ATTRIBUTE, module.ImportReference(typeof(void)));
            var ins = emptyCtor.Body.Instructions;
            ins.Add(Instruction.Create(OpCodes.Ldarg_0));
            ins.Add(Instruction.Create(OpCodes.Call, invokeHandleInit));
            ins.Add(Instruction.Create(OpCodes.Ldarg_0));
            var parentEmptyCtor = parentCtors.FirstOrDefault(method => method.Parameters?.Count == 0);
            if (parentType.IsInterface || parentEmptyCtor == null)
                ins.Add(Instruction.Create(OpCodes.Call, module.ImportReference(typeof(object).GetConstructor(Array.Empty<Type>()))));
            else
                ins.Add(Instruction.Create(OpCodes.Call, module.ImportReference(parentEmptyCtor)));
            ins.Add(Instruction.Create(OpCodes.Ret));
            originType.Methods.Add(emptyCtor);

            //Ctor with params
            foreach (var parentCtor in parentCtors)
            {
                if (parentCtor.Parameters.Count == 0)
                    continue;
                var ctor = new MethodDefinition(".ctor", CTOR_ATTRIBUTE, module.ImportReference(typeof(void)));
                ins = ctor.Body.Instructions;
                ins.Add(Instruction.Create(OpCodes.Ldarg_0));
                ins.Add(Instruction.Create(OpCodes.Call, invokeHandleInit));
                ins.Add(Instruction.Create(OpCodes.Ldarg_0));

                foreach (var parameter in parentCtor.Parameters)
                {
                    ctor.Parameters.Add(parameter);
                    ins.Add(Instruction.Create(OpCodes.Ldarg_S, parameter));
                }

                ins.Add(Instruction.Create(OpCodes.Call, module.ImportReference(parentCtor)));
                ins.Add(Instruction.Create(OpCodes.Ret));
                originType.Methods.Add(ctor);
            }
        }

        private static MethodDefinition AddInvokeHandles(ModuleDefinition module, TypeDefinition originType, TypeDefinition parentType, out Dictionary<string, (FieldDefinition interceptor, FieldDefinition? invokeHandle, MethodDefinition parentMethod)> callerMap)
        {
            var method = new MethodDefinition("_InitInvokeHandles", PRIVATE_METHOD_ATTRIBUTE, module.ImportReference(typeof(void)));
            var ins = method.Body.Instructions;
            var invokeHandleRef = module.ImportReference(typeof(InvokeHandle));
            var funcRef = module.ImportReference(typeof(Interceptor));
            var objectRef = module.ImportReference(typeof(object));
            var objectsRef = module.ImportReference(typeof(object[]));
            callerMap = new Dictionary<string, (FieldDefinition, FieldDefinition, MethodDefinition)>();

            foreach (var parentMethod in parentType.Methods.Where(method => !method.IsFinal && method.IsPublic && !method.IsSpecialName && method.IsVirtual && method.IsNewSlot))
            {
                var isVoid = parentMethod.ReturnType.FullName.Equals(module.ImportReference(typeof(void)).FullName);
                var isAbstract = parentMethod.IsAbstract;
                var callerName = GetInterceptorFieldName(parentMethod);
                var parentCaller = new FieldDefinition(callerName, FieldAttributes.Private, funcRef);
                originType.Fields.Add(parentCaller);
                callerMap[callerName] = (parentCaller, null, parentMethod)!;

                if (isAbstract) continue;

                //Build parent methods' proxy
                var proxyMethod = new MethodDefinition($"_ProxyMethod_{parentMethod.Name}{MergeParamsString(parentMethod.Parameters)}", PRIVATE_METHOD_ATTRIBUTE, objectRef);
                var anIns = proxyMethod.Body.Instructions;
                var parameters = parentMethod.Parameters;
                var ldloc = Instruction.Create(OpCodes.Ldloc_0);

                proxyMethod.Body.InitLocals = true;
                proxyMethod.Parameters.Add(new ParameterDefinition(objectsRef));
                proxyMethod.Body.Variables.Add(new VariableDefinition(objectRef));
                anIns.Add(Instruction.Create(OpCodes.Ldarg_0));
                for (var i = 0; i < parentMethod.Parameters.Count; i++)
                {
                    var isValue = parameters[i].ParameterType.IsValueType;
                    anIns.Add(Instruction.Create(OpCodes.Ldarg_1));
                    anIns.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                    anIns.Add(Instruction.Create(OpCodes.Ldelem_Ref));
                    anIns.Add(Instruction.Create(isValue ? OpCodes.Unbox_Any : OpCodes.Castclass, module.ImportReference(parameters[i].ParameterType)));
                }

                anIns.Add(Instruction.Create(OpCodes.Call, module.ImportReference(parentMethod)));
                if (parentMethod.ReturnType.IsValueType)
                    anIns.Add(Instruction.Create(OpCodes.Box, module.ImportReference(parentMethod.ReturnType)));
                if (isVoid)
                    anIns.Add(Instruction.Create(OpCodes.Ldnull));
                anIns.Add(Instruction.Create(OpCodes.Stloc_0));
                anIns.Add(Instruction.Create(OpCodes.Br_S, ldloc));
                anIns.Add(ldloc);
                anIns.Add(Instruction.Create(OpCodes.Ret));
                originType.Methods.Add(proxyMethod);

                //Add caller to body
                var invokeHandle = new FieldDefinition(GetInvokeHandleFieldName(parentMethod), FieldAttributes.Private, invokeHandleRef);
                var funcType = typeof(Func<object[], object>);
                callerMap[callerName] = (parentCaller, invokeHandle, parentMethod);
                originType.Fields.Add(invokeHandle);

                ins.Add(Instruction.Create(OpCodes.Ldarg_0));
                ins.Add(Instruction.Create(OpCodes.Ldarg_0));
                ins.Add(Instruction.Create(OpCodes.Ldftn, proxyMethod));
                ins.Add(Instruction.Create(OpCodes.Newobj, module.ImportReference(funcType.GetConstructor(new[] {typeof(object), typeof(IntPtr)}))));
                ins.Add(Instruction.Create(OpCodes.Newobj, module.ImportReference(typeof(InvokeHandle).GetConstructor(new[] {funcType}))));
                ins.Add(Instruction.Create(OpCodes.Stfld, invokeHandle));
            }

            ins.Add(Instruction.Create(OpCodes.Ret));
            originType.Methods.Add(method);
            return method;
        }
    }
}