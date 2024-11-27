using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(IOperator))]
    class HasKeyOperator : AbstractOperator, IOperator 
    {
        public HasKeyOperator()
        {
            Type = ExpressionType.HasKey;
            Name = "haskey";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.String, ParamType.String };
            ReturnType = ParamType.Bool; 
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string lookup = eval.Evaluate(eval, expression.Arguments[0], context);
            string key = eval.Evaluate(eval, expression.Arguments[1], context);

            if (null == expression.CachedLookupMap)
            {
                if (!context.HasLookupMap(lookup))
                    throw new InvalidOperationException("Error in HasKey operator: Lookup map '" + lookup + "' is unknown.");
                expression.CachedLookupMap = context.GetLookupMap(lookup);
            }

            return BoolToString(expression.CachedLookupMap.HasKey(key));
        }
    }
}
