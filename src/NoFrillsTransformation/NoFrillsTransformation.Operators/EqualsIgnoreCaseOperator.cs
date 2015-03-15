using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class EqualsIgnoreCaseOperator : AbstractOperator, IOperator
    {
        public EqualsIgnoreCaseOperator()
        {
            Type = ExpressionType.EqualsIgnoreCase;
            Name = "equalsignorecase";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Any };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            var compType = StringComparison.InvariantCultureIgnoreCase;
            return BoolToString(
                eval.Evaluate(eval, expression.Arguments[0], context).Equals(
                    eval.Evaluate(eval, expression.Arguments[1], context),
                    compType));
        }
    }
}
