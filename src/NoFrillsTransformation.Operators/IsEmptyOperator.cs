using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class IsEmptyOperator : AbstractOperator, IOperator
    {
        public IsEmptyOperator()
        {
            Type = ExpressionType.IsEmpty;
            Name = "isempty";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.String };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string a = eval.Evaluate(eval, expression.Arguments[0], context);
            return BoolToString(string.IsNullOrEmpty(a));
        }
    }
}
