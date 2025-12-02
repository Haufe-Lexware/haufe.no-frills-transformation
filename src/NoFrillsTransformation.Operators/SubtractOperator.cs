using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class SubtractOperator : AbstractOperator, IOperator
    {
        public SubtractOperator()
        {
            Type = ExpressionType.Subtract;
            Name = "subtract";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Int, ParamType.Int };
            ReturnType = ParamType.Int;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            return IntToString(
                      StringToInt(eval.Evaluate(eval, expression.Arguments[0], context)) -
                      StringToInt(eval.Evaluate(eval, expression.Arguments[1], context))
                   );
        }
    }
}
