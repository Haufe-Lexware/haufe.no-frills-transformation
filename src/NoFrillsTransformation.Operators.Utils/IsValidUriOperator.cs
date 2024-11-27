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
    public class IsValidUriOperator : AbstractUtilsOperator, IOperator
    {
        public IsValidUriOperator()
        {
            Type = ExpressionType.Custom;
            Name = "isvaliduri";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Any };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string uri = eval.Evaluate(eval, expression.Arguments[0], context);
            return BoolToString(IsValidUri(uri));
        }

        // Stolen from MSDN documentation, but removed the statefulness.
        public static bool IsValidUri(string uriName)
        {
            Uri? uriResult;
            return Uri.TryCreate(uriName, UriKind.Absolute, out uriResult)
                          && (null != uriResult)
                          && (uriResult.Scheme == Uri.UriSchemeHttp
                              || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
