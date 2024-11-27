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
    public class RegexCaptureOperator : AbstractUtilsOperator, IOperator
    {
        public RegexCaptureOperator()
        {
            Type = ExpressionType.Custom;
            Name = "regexcapture";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.String, ParamType.Any };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string regex = "";
            if (_regex == null) // We will only do this once.
                regex = eval.Evaluate(eval, expression.Arguments[0], context);
            string input = eval.Evaluate(eval, expression.Arguments[1], context);
            return RegexCapture(regex, input);
        }

        private Regex? _regex;

        private string RegexCapture(string regex, string input)
        {
            if (null == _regex)
                _regex = new Regex(regex, RegexOptions.Multiline);

            var match = _regex.Match(input);
            if (!match.Success)
                return "";

            if (match.Groups.Count > 1)
                return match.Groups[1].Value;

            return match.Value;
        }
    }
}
