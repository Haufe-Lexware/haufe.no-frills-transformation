using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class TimeSpanMonthsOperator : AbstractUtilsOperator, IOperator
    {
        public TimeSpanMonthsOperator()
        {
            Type = ExpressionType.Custom;
            Name = "timespanmonths";
            ParamCount = 3;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Any, ParamType.String };
            ReturnType = ParamType.Int;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string beginDateString = eval.Evaluate(eval, expression.Arguments[0], context);
            string endDateString = eval.Evaluate(eval, expression.Arguments[1], context);
            string formatString = eval.Evaluate(eval, expression.Arguments[2], context);

            if (string.IsNullOrWhiteSpace(beginDateString)
                || string.IsNullOrWhiteSpace(endDateString))
                throw new ArgumentException("TimeSpanMonthsOperator: One or both arguments are null or whitespace.");
            if (string.IsNullOrWhiteSpace(formatString))
                throw new ArgumentException("TimeSpanMonthsOperator: A format string must be provided");

            DateTime beginDate = DateTime.ParseExact(beginDateString, formatString, CultureInfo.CurrentCulture);
            DateTime endDate = DateTime.ParseExact(endDateString, formatString, CultureInfo.CurrentCulture);

            int beginYear = beginDate.Year;
            int endYear = endDate.Year;

            int beginMonth = beginDate.Month;
            int endMonth = endDate.Month;

            return ((endYear - beginYear) * 12 + (endMonth - beginMonth)).ToString();
        }
    }
}
