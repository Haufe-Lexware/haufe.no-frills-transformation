using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public enum ExpressionType
    {
        Custom,
        SourceRowNum,
        TargetRowNum,
        FieldName,
        StringLiteral,
        Concat,
        Lookup,
        HasKey,
        LowerCase,
        UpperCase,
        Equals,
        EqualsIgnoreCase,
        Contains,
        ContainsIgnoreCase,
        StartsWith,
        EndsWith,
        And,
        Or,
        Not,
        If,
        Trim,
        Int,
        IntLiteral,
        Substring,
        Length,
        Add,
        Subtract,
        Divide,
        Multiply,
        Modulo,
    }
}
