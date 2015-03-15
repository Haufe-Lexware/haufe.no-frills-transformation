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

    class Context : IContext
    {
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
        public FilterMode FilterMode { get; set; }
        public FilterDef[] Filters { get; set; }
        public TargetFieldDef[] TargetFields { get; set; }

        public int SourceRecordsRead { get; set; }
        public int SourceRecordsFiltered { get; set; }
        public int SourceRecordsProcessed { get; set; }
        public int TargetRecordsWritten { get; set; }
    }
}
