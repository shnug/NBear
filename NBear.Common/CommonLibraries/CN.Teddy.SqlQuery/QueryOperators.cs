using System;
using System.Collections.Generic;
using System.Text;

namespace NBear.Common
{
    public enum QueryOperator
    {
        Equal,
        NotEqual,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual,
        Like,
        BitwiseAND,
        BitwiseOR,
        BitwiseXOR,
        BitwiseNOT,
        IsNULL,
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
    }
}
