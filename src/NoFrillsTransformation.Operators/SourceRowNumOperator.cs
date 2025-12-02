using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class SourceRowNumOperator : AbstractOperator, IOperator
    {
        public SourceRowNumOperator()
        {
            Type = ExpressionType.SourceRowNum;
            Name = "sourcerownum";
            ParamCount = 0;
            ParamTypes = new ParamType[] {};
            ReturnType = ParamType.Int;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            return context.SourceRecordsRead.ToString();
        }
    }
}
