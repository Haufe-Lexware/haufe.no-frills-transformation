using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class FileResolveOperator : AbstractUtilsOperator, IOperator
    {
        public FileResolveOperator()
        {
            Type = ExpressionType.Custom;
            Name = "fileresolve";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Any };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string path = eval.Evaluate(eval, expression.Arguments[0], context);
            return context.ResolveFileName(path, false);
        }
    }
}
