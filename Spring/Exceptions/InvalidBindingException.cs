using System;

namespace Spring;

public class InvalidBindingException(string msg) : Exception(msg);