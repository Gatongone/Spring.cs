namespace Spring.Tests
{
    public interface IInterfaceTest
    {
        void Func_Void();
        void Func_Void_Params(int arg0,object arg1);
        int Func_Int();
        StructTest Func_Struct();
        long Func_Long();
        decimal Func_Decimal();
        float Func_Float();
        double Func_Double();
        string Func_String();
    }

    public class InterfaceProxyExample : IInterfaceTest
    {
        public Interceptor interceptor_Func_Void;
        public Interceptor interceptor_Func_Void_Params;
        public Interceptor interceptor_Func_Int;
        public Interceptor interceptor_Func_Struct;
        public Interceptor interceptor_Func_Long;
        public Interceptor interceptor_Func_Decimal;
        public Interceptor interceptor_Func_Float;
        public Interceptor interceptor_Func_Double;
        public Interceptor interceptor_Func_String;

        public void Func_Void()
        {
            if (interceptor_Func_Void != null)
            {
                interceptor_Func_Void.Invoke(null, null);
            }
        }

        public void Func_Void_Params(int arg0, object arg1)
        {
            if (interceptor_Func_Void != null)
            {
                interceptor_Func_Void_Params.Invoke(null, new object[] {arg0, arg1});
            }
        }

        public int Func_Int()
        {
            if (interceptor_Func_Int != null)
            {
                return (int) interceptor_Func_Int.Invoke(null, null);
            }

            return 0;
        }

        public StructTest Func_Struct()
        {
            if (interceptor_Func_Struct != null)
            {
                return (StructTest) interceptor_Func_Struct.Invoke(null, null);
            }

            return default;
        }

        public long Func_Long()
        {
            if (interceptor_Func_Long != null)
            {
                return (long) interceptor_Func_Long.Invoke(null, null);
            }

            return 0L;
        }

        public decimal Func_Decimal()
        {
            if (interceptor_Func_Decimal != null)
            {
                return (decimal) interceptor_Func_Decimal.Invoke(null, null);
            }

            return decimal.Zero;
        }

        public float Func_Float()
        {
            if (interceptor_Func_Float != null)
            {
                return (float) interceptor_Func_Float.Invoke(null, null);
            }

            return 0f;
        }

        public double Func_Double()
        {
            if (interceptor_Func_Double != null)
            {
                return (double) interceptor_Func_Double.Invoke(null, null);
            }

            return 0d;
        }

        public string Func_String()
        {
            if (interceptor_Func_String != null)
            {
                return (string) interceptor_Func_String.Invoke(null, null);
            }

            return null;
        }
    }
}