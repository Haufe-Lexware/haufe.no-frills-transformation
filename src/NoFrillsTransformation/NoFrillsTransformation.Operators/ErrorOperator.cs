using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class ErrorOperator : AbstractOperator, IOperator
    {
        public ErrorOperator()
        {
            Type = ExpressionType.Error;
            Name = "error";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Any };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string errorString = eval.Evaluate(eval, expression.Arguments[0], context);

            throw new InvalidOperationException(errorString);
        }
    }

}
