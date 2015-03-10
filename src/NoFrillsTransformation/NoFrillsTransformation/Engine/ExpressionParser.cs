using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Engine
{
    class ExpressionParser
    {

    }

    enum ExpressionType
    {
        FieldName,
        StringLiteral,
        Concatenation,
        Lookup
    }

    class Expression
    {
        public ExpressionType Type { get; set; }
        public string Content { get; set; }

    }
}
