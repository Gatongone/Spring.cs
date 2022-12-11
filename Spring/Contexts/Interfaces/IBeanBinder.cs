using System;
using System.Diagnostics.CodeAnalysis;

namespace Spring
{
    public interface IBeanBinder
    {
        void Bind(string beanName, Type implementType);

        void Bind(Type beanType, Type implementType);

        void Bind<TImplement>(string beanName) => Bind(beanName, typeof(TImplement));

        void Bind<TBean, TImplement>() => Bind(typeof(TBean), typeof(TImplement));

        Type GetImplementType(string beanName);

        Type GetImplementType(Type beanType);

        bool TryGetImplementType(string beanName, [MaybeNullWhen(false)] out Type implementType);

        bool TryGetImplementType(Type beanType, [MaybeNullWhen(false)] out Type implementType);
    }
}