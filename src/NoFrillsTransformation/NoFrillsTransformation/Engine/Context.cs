using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Config;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    enum FilterMode
    {
        And,
        Or
    }

    class Context : IContext, IDisposable
    {
        public ILogger Logger { get; set; }
        public ISourceReader SourceReader { get; set; }
        public ITargetWriter TargetWriter { get; set; }
        private Dictionary<string, LookupMap> _lookupMaps = new Dictionary<string, LookupMap>();
        public Dictionary<string, LookupMap> LookupMaps
        {
            get
            {
                return _lookupMaps;
            }
        }

        public bool HasLookupMap(string id)
        {
            return LookupMaps.ContainsKey(id);
        }

        public ILookupMap GetLookupMap(string id)
        {
            return LookupMaps[id];
        }

        private Dictionary<string, IOperator> _operators = new Dictionary<string, IOperator>();
        public Dictionary<string, IOperator> Operators
        {
            get
            {
                return _operators;
            }
        }
        public bool HasOperator(string name)
        {
            return _operators.ContainsKey(name);
        }
        public IOperator GetOperator(string name)
        {
            return _operators[name];
        }

        private Stack<ParameterContext> _parameterStack = new Stack<ParameterContext>();
        public Stack<ParameterContext> ParameterStack
        {
            get
            {
                return _parameterStack;
            }
        }

        public FilterMode FilterMode { get; set; }
        public FilterDef[] Filters { get; set; }
        public TargetFieldDef[] TargetFields { get; set; }

        public int SourceRecordsRead { get; set; }
        public int SourceRecordsFiltered { get; set; }
        public int SourceRecordsProcessed { get; set; }
        public int TargetRecordsWritten { get; set; }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != SourceReader)
                {
                    SourceReader.Dispose();
                    SourceReader = null;
                }
                if (null != TargetWriter)
                {
                    TargetWriter.Dispose();
                    TargetWriter = null;
                }
                // The logger must be the last thing to go!
                if (null != Logger)
                {
                    Logger.Dispose();
                    Logger = null;
                }
            }
        }
        #endregion
    }
}
