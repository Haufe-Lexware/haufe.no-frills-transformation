using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class NotOperator : AbstractOperator, IOperator
    {
        public NotOperator()
        {
            Type = ExpressionType.Not;
            Name = "not";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Bool };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            bool a = StringToBool(eval.Evaluate(eval, expression.Arguments[0], context));
            return BoolToString(!a);
        }
    }
}
