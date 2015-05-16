using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class FileContentOperator : AbstractUtilsOperator, IOperator
    {
        public FileContentOperator()
        {
            Type = ExpressionType.Custom;
            Name = "filecontent";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Int };
            ReturnType = ParamType.String;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string path = eval.Evaluate(eval, expression.Arguments[0], context);
            long length = StringToInt(eval.Evaluate(eval, expression.Arguments[1], context));
            if (length <= 0)
                return "";
            string resolvedPath = context.ResolveFileName(path, true);
            using (var fs = File.OpenRead(resolvedPath))
            {
                byte[] content = new byte[length];
                int readBytes = fs.Read(content, 0, (int)length);
                return Encoding.Default.GetString(content, 0, readBytes);
            }
        }
    }
}
