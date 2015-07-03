using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class FalseOperator : AbstractOperator, IOperator
    {
        public FalseOperator()
        {
            Type = ExpressionType.False;
            Name = "false";
            ParamCount = 0;
            ParamTypes = new ParamType[] { };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            return BoolToString(false);
        }
    }

}
