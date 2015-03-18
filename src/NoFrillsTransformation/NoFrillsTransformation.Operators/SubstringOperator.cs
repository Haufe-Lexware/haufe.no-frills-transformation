using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class SubstringOperator : AbstractOperator, IOperator
    {
        public SubstringOperator()
        {
            Type = ExpressionType.Substring;
            Name = "substring";
            ParamCount = 3;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Int, ParamType.Int };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string s = eval.Evaluate(eval, expression.Arguments[0], context);
            int offset = (int)StringToInt(eval.Evaluate(eval, expression.Arguments[1], context));
            int length = (int)StringToInt(eval.Evaluate(eval, expression.Arguments[2], context));

            if (offset >= s.Length)
                return "";
            if (length > s.Length - offset
                || length < 0)
                return s.Substring(offset);
            return s.Substring(offset, length);
        }
    }
}
