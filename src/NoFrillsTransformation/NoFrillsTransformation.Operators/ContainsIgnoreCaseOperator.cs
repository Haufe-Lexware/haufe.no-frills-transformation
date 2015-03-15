using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class ContainsIgnoreCaseOperator : AbstractOperator, IOperator
    {
        public ContainsIgnoreCaseOperator()
        {
            Type = ExpressionType.ContainsIgnoreCase;
            Name = "containsignorecase";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Any };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string a = eval.Evaluate(eval, expression.Arguments[0], context).ToUpperInvariant();
            string b = eval.Evaluate(eval, expression.Arguments[1], context).ToUpperInvariant();
            return BoolToString(a.Contains(b));
        }    
    }
}
