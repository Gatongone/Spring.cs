using System;
using System.Collections.Generic;
using System.Reflection;

namespace Spring.Examples.Services;

[Bean]
public class UserService : IBeanInitializationPreprocessor, IBeanInitializationPostprocessor, IMemberInjectionPreprocessor, IMemberInjectionPostprocessor
{
    [Inject] 
    public IOderService oderService;

    public void OnBeforeInitialization(object bean, string? beanName)
    {
        Console.WriteLine($"{LogTags.BEAN}{LogTags.USER_SERVICE}{LogTags.BEAN_INIT} Ready to init, " +
                          $"type: \"{bean.GetType()}\"");
    }

    public void OnAfterInitialization(object bean, string? beanName)
    {
        Console.WriteLine($"{LogTags.BEAN}{LogTags.USER_SERVICE}{LogTags.BEAN_INIT} Init Finish," +
                          $" type: \"{bean.GetType()}\"");
    }

    public void OnBeforeInjection(MemberInfo member, object? bean, string? beanName)
    {
        Console.WriteLine($"{LogTags.BEAN}{LogTags.USER_SERVICE}{LogTags.BEAN_MEMBER_INJECT} " +
                          $"Member: \"{member.Name}\" is ready to inject");
    }

    public void OnAfterInjection(MemberInfo member, object? bean, string? beanName)
    {
        Type memberType = null;

        if (member is FieldInfo field)
            memberType = field.FieldType;

        if (member is PropertyInfo property)
            memberType = property.PropertyType;

        if (member.Name == nameof(oderService))
            Console.WriteLine($"{LogTags.BEAN}{LogTags.USER_SERVICE}{LogTags.BEAN_MEMBER_INJECT} " +
                              $"Member: \"{member.Name}\" has been injected, " +
                              $"type: \"{oderService.GetType()}\"");
    }
}