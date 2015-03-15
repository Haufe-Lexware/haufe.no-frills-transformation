using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine.Operators
{
    // Please note that the FieldNameOperator does not have an Export attribute
    // for MEF; the field name operator is explicitly created when field references
    // are found in the expressions, and must not be present in the operator
    // dictionary (they are not functions/operators).
    class FieldNameOperator : IOperator
    {
        public FieldNameOperator()
        {
        }

        public int ParamCount { get { return 0; } }
        public string Name { get { return ""; } }
        public ExpressionType Type { get { return ExpressionType.FieldName; } }
        public ParamType[] ParamTypes { get { return null; } }
        public ParamType ReturnType { get { return ParamType.String; } }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            throw new InvalidOperationException("The FieldName operator must not be evaluated. This is done internally.");
        }

        public void Configure(string config)
        {
            // No config for this operator.
        }
    }
}
