using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface IContext
    {
        string ConfigFileName { get; }
        string ResolveFileName(string filePath);
        string ResolveFileName(string filePath, bool hasToExist);

        ILogger Logger { get; set; }
        ISourceReader SourceReader { get; set; }
        ISourceTransformer Transformer { get; set; }
        ITargetWriter TargetWriter { get; set; }
        ITargetWriter FilterTargetWriter { get; set; }

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
        bool InTransform { get; }

        Dictionary<string, string> Parameters { get; }
        string ReplaceParameters(string input);
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
