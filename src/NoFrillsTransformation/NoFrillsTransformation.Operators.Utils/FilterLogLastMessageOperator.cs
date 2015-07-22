using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Operators.Utils
{
    [Export(typeof(NoFrillsTransformation.Interfaces.IOperator))]
    public class FilterLogLastMessageOperator : AbstractUtilsOperator, IOperator
    {
        public FilterLogLastMessageOperator()
        {
            Type = ExpressionType.Custom;
            Name = "filterloglastmessage";
            ParamCount = 0;
            ParamTypes = new ParamType[] { };
            ReturnType = ParamType.Bool;
        }

        private static string _filterLogMessage;
        private static int _filterLogSourceRecord;

        internal static void SetFilterLogMessage(string filterLogMessage, int filterLogSourceRecord)
        {
            _filterLogMessage = filterLogMessage;
            _filterLogSourceRecord = filterLogSourceRecord;
        }

        public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
        {
            if (context.SourceRecordsRead == _filterLogSourceRecord)
                return _filterLogMessage;

            return string.Empty;
        }
    }
}
