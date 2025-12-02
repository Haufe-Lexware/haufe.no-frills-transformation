using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class FileExtOperator : AbstractUtilsOperator, IOperator
    {
        public FileExtOperator()
        {
            Type = ExpressionType.Custom;
            Name = "fileext";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Any };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string path = eval.Evaluate(eval, expression.Arguments[0], context);
            return Path.GetExtension(path);
        }
    }
}
