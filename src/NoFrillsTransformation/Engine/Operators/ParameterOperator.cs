using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine.Operators
{
    class ParameterOperator : IOperator
    {
        public ParameterOperator()
        {
        }

        public int ParamCount { get { return 0; } }
        public string Name { get { return ""; } }
        public ExpressionType Type { get { return ExpressionType.Parameter; } }
        public ParamType[]? ParamTypes { get { return null; } }
        public ParamType ReturnType { get { return ParamType.Any; } }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            if (context.ParameterStack.Count == 0)
                throw new InvalidOperationException("Found parameter %" + expression.Content + " without a context parameter stack. Parameter modifier % can only be used in CustomOperator definitions.");
            var paramName = expression.Content.ToLowerInvariant();
            var paramContext = context.ParameterStack.Peek();
            if (!paramContext.Parameters.ContainsKey(paramName))
                throw new InvalidOperationException("Parameter %" + expression.Content + " is unknown.");
            return paramContext.Parameters[paramName];
        }

        public void Configure(string? config)
        {
            // No config for this operator.
        }
    }
}
