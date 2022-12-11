using System;

namespace Spring
{
    public class CutpointAdvices
    {
        public Action beforeAdvice;
        public Func<object,object> afterAdvice;
        public Action finallyAdvice;
        public Func<IMethodReference,object[],object> arroundAdvice;
        public Func<Exception, object[], object?> catchAdvice;
    }
}