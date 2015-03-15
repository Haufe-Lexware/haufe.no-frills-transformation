using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class ContainsOperator : AbstractOperator, IOperator
    {
        public ContainsOperator()
        {
            Type = ExpressionType.Contains;
            Name = "contains";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Any };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string a = eval.Evaluate(eval, expression.Arguments[0], context);
            string b = eval.Evaluate(eval, expression.Arguments[1], context);
            return BoolToString(a.Contains(b));
        }
    }
}
