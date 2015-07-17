using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class FilterLogOperator : AbstractUtilsOperator, IOperator
    {
        public FilterLogOperator()
        {
            Type = ExpressionType.Custom;
            Name = "filterlog";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Bool, ParamType.Any };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            var filterResult = eval.Evaluate(eval, expression.Arguments[0], context);
            if (!StringToBool(filterResult))
            {
                var logString = eval.Evaluate(eval, expression.Arguments[1], context);
                context.Logger.Info(logString);
            }

            return filterResult;
        }
    }
}
