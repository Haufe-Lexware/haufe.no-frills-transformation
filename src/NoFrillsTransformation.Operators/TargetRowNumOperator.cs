using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class TargetRowNumOperator : AbstractOperator, IOperator
    {
        public TargetRowNumOperator()
        {
            Type = ExpressionType.TargetRowNum;
            Name = "targetrownum";
            ParamCount = 0;
            ParamTypes = new ParamType[] { };
            ReturnType = ParamType.Int;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            return (context.TargetRecordsWritten + 1).ToString();
        }
    }
}
