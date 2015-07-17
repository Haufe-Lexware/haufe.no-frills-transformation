using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class EqualsOperator : AbstractOperator, IOperator
    {
        public EqualsOperator()
        {
            Type = ExpressionType.Equals;
            Name = "equals";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Any };
            ReturnType = ParamType.Bool;
        }

        private bool _ignoreCase = false;

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            var compType = _ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            return BoolToString(
                eval.Evaluate(eval, expression.Arguments[0], context).Equals(
                    eval.Evaluate(eval, expression.Arguments[1], context),
                    compType));
        }

        public override void Configure(string config)
        {
            _ignoreCase = config != null ? config.Equals("ignorecase", StringComparison.InvariantCultureIgnoreCase) : false;
        }
    }
}
