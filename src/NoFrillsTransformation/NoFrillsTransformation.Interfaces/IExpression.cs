using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface IExpression
    {
        //public ExpressionType Type { get; set; }
        IOperator Operator { get; set; }
        string Content { get; set; }
        //public Expression FirstArgument { get; set; }
        //public Expression SecondArgument { get; set; }
        IExpression[] Arguments { get; set; }
    }
}
