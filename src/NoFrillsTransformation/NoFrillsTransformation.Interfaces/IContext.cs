using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface IContext
    {
        ILogger Logger { get; set; }
        ISourceReader SourceReader { get; set; }
        ITargetWriter TargetWriter { get; set; }

        bool HasLookupMap(string id);
        ILookupMap GetLookupMap(string id);

        bool HasOperator(string name);
        IOperator GetOperator(string name);

        //bool HasParameter(string name);
        //IExpression GetParameter(string name);
        Stack<ParameterContext> ParameterStack { get; }

        int SourceRecordsRead { get; }
        int SourceRecordsFiltered { get; }
        int SourceRecordsProcessed { get; }
        int TargetRecordsWritten { get; }
    }

    public class ParameterContext
    {
        private Dictionary<string, string> _parameters = null;
        public Dictionary<string, string> Parameters
        {
            get
            {
                if (null == _parameters)
                    _parameters = new Dictionary<string, string>();
                return _parameters;
            }
        }
    }
}
