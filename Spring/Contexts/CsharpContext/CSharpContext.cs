using System;
using System.Linq;
using System.Reflection;
using Spring.Advices;

namespace Spring
{
    public sealed class CSharpContext : ApplicationContext
    {
        private readonly Type m_ConfigType;
        private IBeanDefinitionsCollection m_BeanDefinitions;

        public CSharpContext(Type configType) => m_ConfigType = configType;

        public override IApplicationHierarchy GetImplement() => this;

        public override IBeanDefinitionsCollection GetEnvironment()
        {
            if (m_BeanDefinitions != null)
                return m_BeanDefinitions;

            m_BeanDefinitions = new BeanDefinitionsCollection();
            var scanInfos = m_ConfigType.GetCustomAttributes()
                .Where(attribute => attribute.GetType().IsAssignableTo(typeof(ITypeScanningInfo)))
                .Select(scanInfo => (ITypeScanningInfo) scanInfo);
            foreach (var scanInfo in scanInfos)
            {
                AnalyseScanningInfo(scanInfo);
            }

            return m_BeanDefinitions;
        }

        protected override bool NeedMemberInject(MemberInfo member)
        {
            return member.GetCustomAttribute<InjectAttribute>() != null;
        }

        protected override object OnMemberInjection(MemberInfo member, object bean)
        {
            Type beanType = null;
            if (member is PropertyInfo property)
                beanType = property.PropertyType;
            if (member is FieldInfo field)
                beanType = field.FieldType;
            if (beanType == null)
                throw new BeanResolveException("MemberInfo can't be resolve. ");
            var beanAttribute = beanType.GetCustomAttribute<BeanAttribute>();
            if (beanAttribute == null && !m_BeanDefinitions.Contains(beanType))
                throw new BeanResolveException($"\"{beanType}\" is not a bean. ");
            return GetBean(beanType, beanAttribute?.GetName());
        }

        private void AnalyseScanningInfo(ITypeScanningInfo typeScanningInfo)
        {
            var scanAssemblyName = typeScanningInfo.GetAssemblyPattern();
            var targetAssembly = Assembly.Load(scanAssemblyName);
            var targetTypes = targetAssembly.GetTypes()
                .Where(typeScanningInfo.MatchType)
                .ToList();

            foreach (var type in targetTypes)
            {
                ResolveAspectAttribute(type);
                ResolveBeanAttribute(type);
                ResolveBeanRegisterAttribute(type);
            }
        }

        /// <summary>
        /// Collect bean definition from target assembly.
        /// </summary>
        private void ResolveBeanAttribute(Type type)
        {
            var beanAttribute = type.GetCustomAttribute<BeanAttribute>();
            if (beanAttribute == null)
                return;
            var beanDefinition = new BeanDefinition();
            var scope = type.GetCustomAttribute<ScopeAttribute>();
            beanDefinition.scope = scope?.scope ?? ScopeType.Singleton;
            beanDefinition.type = type;

            //Collect BeanDefinitions
            var beanName = beanAttribute.GetName();
            if (!string.IsNullOrEmpty(beanName))
            {
                m_BeanDefinitions.Add(beanName, beanDefinition);
            }
            else
            {
                m_BeanDefinitions.Add(type, beanDefinition);
            }

            //Bind Beans
            var bindAttribute = type.GetCustomAttribute<BindAttribute>();
            if (bindAttribute == null)
                return;
            if (!string.IsNullOrEmpty(beanName))
                Bind(beanName, bindAttribute.GetImplementType());
            else
                Bind(type, bindAttribute.GetImplementType());
        }

        /// <summary>
        /// Register aspect advice methods
        /// </summary>
        /// <exception cref="InvalidAspectException">Throw out when Aspect is not a static class or without a empty construction</exception>
        /// <exception cref="ScanningException">Throw out when cutpoint pattern doesn't match any advices</exception>
        private void ResolveAspectAttribute(Type type)
        {
            var aspectAttribute = type.GetCustomAttribute<AspectAttribute>();
            if (aspectAttribute == null)
                return;
            object instance = null;
            try
            {
                if (!type.IsAbstract && !type.IsSealed)
                {
                    instance = Activator.CreateInstance(type);
                }
            }
            catch (Exception)
            {
                throw new InvalidAspectException("Aspect class must be a static class or have a empty construction. ");
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                //Get cutpoint
                var cutPoint = method.GetCustomAttribute<CutpointAttribute>();
                if (cutPoint != null)
                {
                    var value = cutPoint.GetValue();
                    var name = cutPoint.GetName();
                    if (!cutpointMaps.ContainsCutpoint(cutPoint.GetValue()))
                    {
                        cutpointMaps.AddCutpoint(value, cutPoint);
                    }

                    if (!string.IsNullOrEmpty(name))
                        cutpointMaps.AddCutpointNameMap(name, value);
                    continue;
                }

                //Collect advice methods
                var advice = method.GetCustomAttribute<AdviceAttribute>();
                if (advice == null) continue;

                var cutPointPattern = advice.GetCutPoint();

                if (!cutpointMaps.TryGetCutpoint(cutPointPattern, out var scanningInfo))
                {
                    scanningInfo = new CutpointAttribute(cutPointPattern);
                    cutpointMaps.AddCutpoint(cutPointPattern, scanningInfo);
                }
                else
                {
                    cutPointPattern = scanningInfo.GetValue();
                }

                if (!cutpointMaps.TryGetCutpointAdvice(cutPointPattern, out var cutpointAdvices))
                {
                    throw new ScanningException($"Cutpoint pattern doesn't match any advices. Pattern: \"{cutPointPattern}\". ");
                }

                switch (advice)
                {
                    case BeforeAttribute:
                    {
                        var action = instance == null ? method.CreateDelegate<Action>() : method.CreateDelegate<Action>(instance);
                        cutpointAdvices.beforeAdvice = action;
                        break;
                    }
                    case AfterAttribute:
                    {
                        var action = instance == null ? method.CreateDelegate<Func<object, object>>() : method.CreateDelegate<Func<object, object>>(instance);
                        cutpointAdvices.afterAdvice = action;
                        break;
                    }
                    case FinallyAttribute:
                    {
                        var action = instance == null ? method.CreateDelegate<Action>() : method.CreateDelegate<Action>(instance);
                        cutpointAdvices.finallyAdvice = action;
                        break;
                    }
                    case CatchAttribute:
                    {
                        var action = instance == null ? method.CreateDelegate<Func<Exception, object[], object>>() : method.CreateDelegate<Func<Exception, object[], object>>(instance);
                        cutpointAdvices.catchAdvice = action;
                        break;
                    }
                    case AroundAttribute:
                    {
                        var action = instance == null ? method.CreateDelegate<Func<IMethodReference, object[], object>>() : method.CreateDelegate<Func<IMethodReference, object[], object>>(instance);
                        cutpointAdvices.arroundAdvice = action;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Register bean from custom creation method, and add it into IOC.
        /// </summary>
        /// <exception cref="ScanningException">Throw when bean register isn't static class or prototype factory bean have arguments</exception>
        private void ResolveBeanRegisterAttribute(Type type)
        {
            if (type.GetCustomAttribute<BeanRegisterAttribute>() == null)
                return;
            if (!type.IsSealed && !type.IsAbstract)
                throw new ScanningException("Bean register can only be static. ");
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                var factoryBean = method.GetCustomAttribute<ProxyAttribute>();
                if (factoryBean == null) continue;

                var scopeType = method.GetCustomAttribute<ScopeAttribute>()?.scope ?? ScopeType.Singleton;
                if (scopeType != ScopeType.Prototype && method.GetParameters().Length > 0)
                    throw new ScanningException($"Proxy bean must be prototype. Method: {method.DeclaringType}.{method.Name}");
                if(method.ReturnType == typeof(void))
                    throw new ScanningException($"Proxy bean can't return void. Method: {method.DeclaringType}.{method.Name}");

                //Register bean definition with method
                //Bean factory will create bean with the custom proxy method instead of dynamic proxy method.
                var beanDefinition = new BeanDefinition
                {
                    scope = scopeType,
                    proxyFactory = method,
                    type = method.ReturnType
                };
                m_BeanDefinitions.Add(method.ReturnType , beanDefinition);
            }
        }
    }
}