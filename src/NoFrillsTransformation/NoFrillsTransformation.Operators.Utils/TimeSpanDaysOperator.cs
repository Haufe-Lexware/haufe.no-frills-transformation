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
    public class TimeSpanDaysOperator : AbstractUtilsOperator, IOperator
    {
        public TimeSpanDaysOperator()
        {
            Type = ExpressionType.Custom;
            Name = "timespandays";
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
                throw new ArgumentException("TimeSpanDaysOperator: One or both arguments are null or whitespace.");
            if (string.IsNullOrWhiteSpace(formatString))
                throw new ArgumentException("TimeSpanDaysOperator: A format string must be provided");

            DateTime beginDate;
            DateTime endDate;
            try { beginDate = DateTime.ParseExact(beginDateString, formatString, CultureInfo.CurrentCulture); }
            catch (Exception ex)
            {
                throw new ArgumentException("TimeSpanDaysOperator: Begin Date ('" + beginDateString + "') could not be parsed. Wrong format? Exception: " + ex.Message);
            }
            try { endDate = DateTime.ParseExact(endDateString, formatString, CultureInfo.CurrentCulture); }
            catch (Exception ex)
            {
                throw new ArgumentException("TimeSpanDaysOperator: End Date ('" + endDateString + "') could not be parsed. Wrong format? Exception: " + ex.Message);
            }

            return ((long)Math.Ceiling  ((endDate - beginDate).TotalDays)).ToString();
        }
    }
}
