using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class FilterOnceOperator : AbstractUtilsOperator, IOperator
    {
        public FilterOnceOperator()
        {
            Type = ExpressionType.Custom;
            Name = "filteronce";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Any };
            ReturnType = ParamType.Bool;
        }

        private bool _caseSensitive = false;
        private HashSet<string> _seenValues = new HashSet<string>();

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string value = eval.Evaluate(eval, expression.Arguments[0], context);
            if (!_caseSensitive)
                value = value.ToLowerInvariant();
            if (_seenValues.Contains(value))
                return BoolToString(false);
            _seenValues.Add(value);
            return BoolToString(true);
        }

        public override void Configure(string config)
        {
            base.Configure(config);
            if (StringToBool(config))
                _caseSensitive = true;
        }
    }
}
