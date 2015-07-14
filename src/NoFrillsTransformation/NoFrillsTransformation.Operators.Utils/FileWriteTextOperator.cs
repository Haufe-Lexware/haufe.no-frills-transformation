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
    public class FileWriteTextOperator : AbstractUtilsOperator, IOperator
    {
        public FileWriteTextOperator()
        {
            Type = ExpressionType.Custom;
            Name = "filewritetext";
            ParamCount = 2;
            ParamTypes = new ParamType[] { ParamType.Any, ParamType.Any };
            ReturnType = ParamType.String;
        }

        private string _encodingString = "UTF-8";
        private Encoding _encoding = Encoding.UTF8;

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string path = eval.Evaluate(eval, expression.Arguments[0], context);
            string content = eval.Evaluate(eval, expression.Arguments[1], context);

            string resolvedPath = context.ResolveFileName(path, false);

            File.WriteAllText(resolvedPath, content, _encoding);

            return resolvedPath;
        }

        public override void Configure(string config)
        {
            base.Configure(config);

            _encodingString = config;
            _encoding = Encoding.GetEncoding(config);
        }
    }
}
