using Spring.Examples.Services;

namespace Spring.Examples
{
    public static class Program
    {
        public static void Main()
        {
            IBeanFactory beanFactory = new CSharpContext(typeof(AppConfig));
            var oderService = beanFactory.GetBean<IOderService>();
            oderService.GenerateOder("Gatongone","GDG-000912");
        }
    }
}