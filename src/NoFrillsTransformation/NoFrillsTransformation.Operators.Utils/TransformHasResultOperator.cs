using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class TransformHasResultOperator : AbstractUtilsOperator, IOperator
    {
        public TransformHasResultOperator()
        {
            Type = ExpressionType.Custom;
            Name = "transformhasresult";
            ParamCount = 0;
            ParamTypes = new ParamType[] { };
            ReturnType = ParamType.Bool;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            if (null == context.Transformer)
                throw new InvalidOperationException("TransformHasResult() operator is only valid if a SourceTransform has been defined.");

            return BoolToString(context.Transformer.HasResult());
        }    
    }
}
