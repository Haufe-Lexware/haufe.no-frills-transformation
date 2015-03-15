using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine.Operators
{
    // Note that this operator does not have an export attribute. This is
    // on purpose, as these are created as the lookup maps are read; each
    // lookup map translates to one instance of the LookupOperator, all having
    // different names.
    class LookupOperator : IOperator
    {
        public LookupOperator(string name)
        {
            _name = name;
            _paramTypes = new ParamType[] { ParamType.String, ParamType.String };
        }

        private string _name;
        private ParamType[] _paramTypes;

        public int ParamCount { get { return 2; } }
        public string Name { get { return _name; } }
        public ExpressionType Type { get { return ExpressionType.Lookup; } }
        public ParamType[] ParamTypes { get { return _paramTypes; } }
        public ParamType ReturnType { get { return ParamType.String; } }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            throw new InvalidOperationException("The Evaluate() method of the LookupOperator must never be called.");
        }

        public void Configure(string config)
        {
            // No config for this operator.
        }
    }
}
