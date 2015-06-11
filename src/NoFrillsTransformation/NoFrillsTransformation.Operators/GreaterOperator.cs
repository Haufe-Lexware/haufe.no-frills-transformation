using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class GreaterOperator : AbstractOperator, IOperator
    {
        public GreaterOperator()
        {
            Type = ExpressionType.Greater;
            Name = "greater";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Int, ParamType.Int };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            long a = long.Parse(eval.Evaluate(eval, expression.Arguments[0], context));
            long b = long.Parse(eval.Evaluate(eval, expression.Arguments[1], context));
            return BoolToString(a > b);
        }
    }
}
