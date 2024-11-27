using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class TrueOperator : AbstractOperator, IOperator
    {
        public TrueOperator()
        {
            Type = ExpressionType.True;
            Name = "true";
            ParamCount = 0;
            ParamTypes = new ParamType[] { };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            return BoolToString(true);
        }
    }

}
