using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class ReplaceOperator : AbstractOperator, IOperator
    {
        public ReplaceOperator()
        {
            Type = ExpressionType.Replace;
            Name = "replace";
            ParamCount = 3;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Any, ParamType.Any };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string haystack = eval.Evaluate(eval, expression.Arguments[0], context);
            string needle = eval.Evaluate(eval, expression.Arguments[1], context);
            string replace = eval.Evaluate(eval, expression.Arguments[2], context);

            return haystack.Replace(needle, replace);
        }
    }
}
