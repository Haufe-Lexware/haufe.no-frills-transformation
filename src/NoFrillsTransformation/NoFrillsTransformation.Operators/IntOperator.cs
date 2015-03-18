using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class IntOperator : AbstractOperator, IOperator
    {
        public IntOperator()
        {
            Type = ExpressionType.Length;
            Name = "int";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Any };
            ReturnType = ParamType.Int;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            long l = StringToInt(eval.Evaluate(eval, expression.Arguments[0], context));
            return IntToString(l);
        }
    }
}
