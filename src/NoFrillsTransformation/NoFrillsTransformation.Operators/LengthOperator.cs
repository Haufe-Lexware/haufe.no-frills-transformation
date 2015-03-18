using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class LengthOperator : AbstractOperator, IOperator
    {
        public LengthOperator()
        {
            Type = ExpressionType.Length;
            Name = "length";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Any };
            ReturnType = ParamType.Int;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            return IntToString(eval.Evaluate(eval, expression.Arguments[0], context).Length);
        }
    }
}
