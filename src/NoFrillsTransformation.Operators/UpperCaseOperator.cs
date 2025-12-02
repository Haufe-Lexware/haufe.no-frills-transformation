using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class UpperCaseOperator : AbstractOperator, IOperator
    {
        public UpperCaseOperator()
        {
            Type = ExpressionType.UpperCase;
            Name = "uppercase";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Any };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            return eval.Evaluate(eval, expression.Arguments[0], context).ToUpperInvariant();
        }
    }

}
