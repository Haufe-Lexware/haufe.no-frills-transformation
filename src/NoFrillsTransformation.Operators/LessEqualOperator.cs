using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class LessEqualOperator : AbstractOperator, IOperator
    {
        public LessEqualOperator()
        {
            Type = ExpressionType.LessEqual;
            Name = "lessequal";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Any };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            if (expression.Arguments[0].Operator.ReturnType == ParamType.Int
                && expression.Arguments[1].Operator.ReturnType == ParamType.Int)
            {
                long a = long.Parse(eval.Evaluate(eval, expression.Arguments[0], context));
                long b = long.Parse(eval.Evaluate(eval, expression.Arguments[1], context));
                return BoolToString(a <= b);
            }
            else
            {
                string a = eval.Evaluate(eval, expression.Arguments[0], context);
                string b = eval.Evaluate(eval, expression.Arguments[1], context);
                return BoolToString(a.CompareTo(b) <= 0);
            }
        }
    }
}
