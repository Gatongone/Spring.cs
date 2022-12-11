namespace Spring
{
    public interface IBeanInitializationPreprocessor
    {
        void OnBeforeInitialization(object bean, string? beanName);
    }
}