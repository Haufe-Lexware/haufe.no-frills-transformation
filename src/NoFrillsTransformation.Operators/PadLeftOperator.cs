using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class PadLeftOperator : AbstractOperator, IOperator
    {
        public PadLeftOperator()
        {
            Type = ExpressionType.PadLeft;
            Name = "padleft";
            ParamCount = 3;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Int, ParamType.Any };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string content = eval.Evaluate(eval, expression.Arguments[0], context);
            int padSize;
            try
            {
                padSize = int.Parse(eval.Evaluate(eval, expression.Arguments[1], context));
            }
            catch (Exception e)
            {
                throw new ArgumentException("For the PadLeft operator, the second parameter needs to be a valid integer: " + e.Message);
            }
            string padChar = eval.Evaluate(eval, expression.Arguments[2], context);
            if (padChar.Length != 1)
                throw new ArgumentException("For the PadLeft operator, the third parameter has to be a string of length exactly 1.");

            if (content.Length >= padSize)
                return content;

            var sb = new StringBuilder();
            for (int i = 0; i < padSize - content.Length; ++i)
                sb.Append(padChar);
            sb.Append(content);

            return sb.ToString();
        }
    }
}
