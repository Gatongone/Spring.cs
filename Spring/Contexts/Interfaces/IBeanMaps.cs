namespace Spring
{
    public interface IBeanMaps<in TBeanId>
    {
        object GetBean(TBeanId beanId);
    }
}