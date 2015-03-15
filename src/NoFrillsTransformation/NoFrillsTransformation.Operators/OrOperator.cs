using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class OrOperator : AbstractOperator, IOperator
    {
        public OrOperator()
        {
            Type = ExpressionType.Or;
            Name = "or";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Bool, ParamType.Bool };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            bool a = StringToBool(eval.Evaluate(eval, expression.Arguments[0], context));
            bool b = StringToBool(eval.Evaluate(eval, expression.Arguments[1], context));
            return BoolToString(a || b);
        }
    }

}
