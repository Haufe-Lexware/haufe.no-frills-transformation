using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine.Operators
{
    // Please note that the StringLiteralOperator does not have an Export attribute
    // for MEF; the string literal operator is explicitly created when string literals
    // are found in the expressions, and must not be present in the operator
    // dictionary (they are not functions/operators).
    class IntLiteralOperator : IOperator
    {
        public IntLiteralOperator()
        {
        }

        public int ParamCount { get { return 0; } }
        public string Name { get { return ""; } }
        public ExpressionType Type { get { return ExpressionType.IntLiteral; } }
        public ParamType[]? ParamTypes { get { return null; } }
        public ParamType ReturnType { get { return ParamType.Int; } }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            throw new InvalidOperationException("The IntLiteral operator must not be evaluated. This is done internally.");
        }

        public void Configure(string? config)
        {
            // No config for this operator.
        }
    }
}
