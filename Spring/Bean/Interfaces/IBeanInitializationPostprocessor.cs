namespace Spring;

public interface IBeanInitializationPostprocessor
{
    void OnAfterInitialization(object bean, string? beanName);
}