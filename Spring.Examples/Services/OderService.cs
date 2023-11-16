using System;
using System.Diagnostics;
using System.Reflection;
using Spring.Examples.Repositories;

namespace Spring.Examples.Services;

public class OderService : IOderService, IBeanInitializationPreprocessor, IBeanInitializationPostprocessor, IMemberInjectionPreprocessor, IMemberInjectionPostprocessor
{
    [Inject] public UserService userService;
    [Inject] public IOderRepository oderRepository;

    public virtual void GenerateOder(string userName, string id)
    {
        Console.WriteLine(oderRepository == null);
    }

    #region Bean life cylcle

    public void OnBeforeInitialization(object bean, string? beanName)
    {
        Console.WriteLine($"{LogTags.BEAN}{LogTags.ODER_SERVICE}{LogTags.BEAN_INIT} Ready to init, " +
                          $"type: \"{bean.GetType()}\"");
    }

    public void OnAfterInitialization(object bean, string? beanName)
    {
        Console.WriteLine($"{LogTags.BEAN}{LogTags.ODER_SERVICE}{LogTags.BEAN_INIT} Init Finish," +
                          $" type: \"{bean.GetType()}\"");
    }

    public void OnBeforeInjection(MemberInfo member, object? bean, string? beanName)
    {
        Console.WriteLine($"{LogTags.BEAN}{LogTags.ODER_SERVICE}{LogTags.BEAN_MEMBER_INJECT} " +
                          $"Member: \"{member.Name}\" is ready to inject");
    }

    public void OnAfterInjection(MemberInfo member, object? bean, string? beanName)
    {
        Type memberType = null;

        if (member is FieldInfo field)
            memberType = field.FieldType;

        if (member is PropertyInfo property)
            memberType = property.PropertyType;

        Debug.Assert(memberType != null);

        Console.WriteLine($"{LogTags.BEAN}{LogTags.ODER_SERVICE}{LogTags.BEAN_MEMBER_INJECT} " +
                          $"Member: \"{member.Name}\" has been injected, " +
                          $"type: \"{member.ReflectedType}\"");
    }

    #endregion
}