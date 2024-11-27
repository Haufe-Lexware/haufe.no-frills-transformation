using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class ConcatOperator : AbstractOperator, IOperator
    {
        public ConcatOperator()
        {
            Type = ExpressionType.Concat;
            Name = "concat";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Any };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            return eval.Evaluate(eval, expression.Arguments[0], context) +
                   eval.Evaluate(eval, expression.Arguments[1], context);
        }
    }
}
