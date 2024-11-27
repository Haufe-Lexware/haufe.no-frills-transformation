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
    public class FileTempNameOperator : AbstractUtilsOperator, IOperator
    {
        public FileTempNameOperator()
        {
            Type = ExpressionType.Custom;
            Name = "filetempname";
            ParamCount = 1;
            ParamTypes = new ParamType[] { ParamType.Any };
            ReturnType = ParamType.String;
        }

        private Dictionary<string, string> _tempNames = new Dictionary<string, string>();
        private Dictionary<string, int> _tempSourceRows = new Dictionary<string, int>();

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            string key = eval.Evaluate(eval, expression.Arguments[0], context);
            // Have we seen the temp file name for this key for this record before?
            if (_tempSourceRows.ContainsKey(key))
            {
                if (context.SourceRecordsRead == _tempSourceRows[key])
                    return _tempNames[key];
            }

            // We need a new one...
            string randomName = Path.GetRandomFileName();
            _tempSourceRows[key] = context.SourceRecordsRead;
            _tempNames[key] = randomName;

            return randomName;
        }
    }
}
