using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class RegexMatchOperator : AbstractUtilsOperator, IOperator
    {
        public RegexMatchOperator()
        {
            Type = ExpressionType.Custom;
            Name = "regexmatch";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.String, ParamType.Any };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string regex = eval.Evaluate(eval, expression.Arguments[0], context);
            string input = eval.Evaluate(eval, expression.Arguments[1], context);
            return BoolToString(RegexMatch(regex, input));
        }

        // Stolen from MSDN documentation, but removed the statefulness.
        public static bool RegexMatch(string regex, string input)
        {
            if (String.IsNullOrEmpty(input))
                return false;

            // Return true if strIn is in valid e-mail format.
            return Regex.IsMatch(input, regex,
                   RegexOptions.IgnoreCase);
        }
    }
}
