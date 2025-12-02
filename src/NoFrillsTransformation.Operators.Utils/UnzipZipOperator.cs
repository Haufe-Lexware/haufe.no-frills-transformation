using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Operators.Utils.Zip;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class UnzipZipFileOperator : AbstractUtilsOperator, IOperator
    {
        public UnzipZipFileOperator()
        {
            Type = ExpressionType.Custom;
            Name = "unzipzipfile";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Any };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string path = eval.Evaluate(eval, expression.Arguments[0], context);
            string targetPath = eval.Evaluate(eval, expression.Arguments[1], context);
            string resolvedPath = context.ResolveFileName(path, true);

            using (ZipStorer zs = ZipStorer.Open(resolvedPath, FileAccess.Read))
            {
                // Misuse 
                if (!ZipStorer.CopyZipFile(zs, targetPath))
                    throw new ArgumentException("UnzipZipOperator: Could not recreate zip '" + resolvedPath + "'.");
            }

            return targetPath;
        }
    }
}
