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
    public class FileSizeOperator : AbstractUtilsOperator, IOperator
    {
        public FileSizeOperator()
        {
            Type = ExpressionType.Custom;
            Name = "filesize";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Any };
            ReturnType = ParamType.Int;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string path = eval.Evaluate(eval, expression.Arguments[0], context);
            string resolvedPath = context.ResolveFileName(path, true);

            var fi = new FileInfo(resolvedPath);
            return fi.Length.ToString();
        }
    }
}
