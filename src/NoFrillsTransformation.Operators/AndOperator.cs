using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class AndOperator : AbstractOperator, IOperator
    {
        public AndOperator()
        {
            Type = ExpressionType.And;
            Name = "and";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Bool, ParamType.Bool };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            bool a = StringToBool(eval.Evaluate(eval, expression.Arguments[0], context));
            if (!a)
                return BoolToString(false);
            bool b = StringToBool(eval.Evaluate(eval, expression.Arguments[1], context));
            return BoolToString(b);
        }
    }
}
