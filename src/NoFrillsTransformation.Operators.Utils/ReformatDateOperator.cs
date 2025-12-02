using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class ReformatDateOperator : AbstractUtilsOperator, IOperator
    {
        public ReformatDateOperator()
        {
            Type = ExpressionType.Custom;
            Name = "reformatdate";
            ParamCount = 3;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.String, ParamType.String };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string dateString = eval.Evaluate(eval, expression.Arguments[0], context);
            string inputFormat = eval.Evaluate(eval, expression.Arguments[1], context);
            string outputFormat = eval.Evaluate(eval, expression.Arguments[2], context);

            if (string.IsNullOrWhiteSpace(dateString))
                throw new ArgumentException("ReformatDateOperator: The date argument (argument 1) is null or whitespace.");
            if (string.IsNullOrWhiteSpace(inputFormat)
                || string.IsNullOrWhiteSpace(outputFormat))
                throw new ArgumentException("ReformatDateOperator: Both the input and output format strings must be provided.");

            DateTime date = DateTime.ParseExact(dateString, inputFormat, CultureInfo.CurrentCulture);

            return date.ToString(outputFormat);
        }
    }
}
