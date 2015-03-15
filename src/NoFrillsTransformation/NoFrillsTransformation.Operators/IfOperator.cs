using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class IfOperator : AbstractOperator, IOperator
    {
        public IfOperator()
        {
            Type = ExpressionType.If;
            Name = "if";
            ParamCount = 3;
            ParamTypes = new ParamType[] { ParamType.Bool, ParamType.Any, ParamType.Any };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            bool cond = StringToBool(eval.Evaluate(eval, expression.Arguments[0], context));
            if (cond)
                return eval.Evaluate(eval, expression.Arguments[1], context);
            return eval.Evaluate(eval, expression.Arguments[2], context);
        }
    }
}
