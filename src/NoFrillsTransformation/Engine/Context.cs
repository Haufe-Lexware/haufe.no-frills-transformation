using System;
using System.Collections.Generic;
using System.IO;
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
        public string ConfigFileName { get; set; } = string.Empty;
        public string ResolveFileName(string fileName)
        {
            return ResolveFileName(fileName, true);
        }

        public string ResolveFileName(string fileName, bool hasToExist)
        {
            if (fileName.StartsWith("\\\\"))
            {
                // UNC path require special treatment...
                // If it does not have to exist, we'll take it as is
                if (!hasToExist)
                    return fileName;

                FileInfo fi = new FileInfo(fileName);
                if (!fi.Exists)
                    throw new ArgumentException("Cannot resolve UNC file '" + fileName + "'.");
                return fileName;
            }
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);
            if (hasToExist && Path.IsPathRooted(fileName))
                throw new ArgumentException("Cannot resolve file '" + fileName + "'.");

            string? mainPath = Path.GetDirectoryName(ConfigFileName);
            if (null == mainPath)
                throw new ArgumentException("Cannot resolve file '" + fileName + "'.");
            string includeInMainPath = Path.Combine(mainPath, fileName.Replace("\\\\", "\\"));

            string fullPath = Path.GetFullPath(includeInMainPath);

            if (hasToExist && !File.Exists(fullPath))
                throw new ArgumentException("Cannot resolve file '" + fileName + "'.");
            return fullPath;
        }

        public ILogger Logger { get; set; } = new DummyLogger();
        public ISourceReader SourceReader { get; set; } = new DummySourceReader();
        public ISourceReader[] SourceReaders { get; set; } = [];
        public ISourceTransformer? Transformer { get; set; }
        public ITargetWriter TargetWriter { get; set; } = new DummyTargetWriter();
        public ITargetWriter? FilterTargetWriter { get; set; }
        private Dictionary<string, LookupMap> _lookupMaps = new Dictionary<string, LookupMap>();
        public Dictionary<string, LookupMap> LookupMaps
        {
            get
            {
                return _lookupMaps;
            }
        }
        public int ProgressTick { get; set; }

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
        public FilterDef[] Filters { get; set; } = [];
        public TargetFieldDef[] TargetFields { get; set; } = [];
        public TargetFieldDef[] FilterTargetFields { get; set; } = [];

        public int SourceRecordsRead { get; set; }
        public int SourceRecordsFiltered { get; set; }
        public int SourceRecordsProcessed { get; set; }
        public int TargetRecordsWritten { get; set; }

        public bool InTransform { get; set; }

        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public string ReplaceParameters(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            string o = s;
            foreach (string key in Parameters.Keys)
            {
                o = o.Replace(string.Format("§{0}§", key), Parameters[key]);
            }
            int index = o.IndexOf('§');
            if (index >= 0)
            {
                int endIndex = o.IndexOf('§', index + 1);
                if (endIndex >= 0)
                {
                    string paramName = o.Substring(index + 1, endIndex - index - 1);
                    throw new ArgumentException("Unknown parameter used in string '" + s + "': '" + paramName + "'.");
                }
            }
            return o;

        }

        public bool CurrentRecordMatchesFilter(IEvaluator eval)
        {
            if (null == Filters)
                return true;
            foreach (var filter in Filters)
            {
                bool val = ExpressionParser.StringToBool(eval.Evaluate(eval, filter.Expression, this));
                if (FilterMode.And == FilterMode
                    && !val)
                    return false;
                if (FilterMode.Or == FilterMode
                    && val)
                    return true;
            }
            return (FilterMode.And ==  FilterMode);
        }

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
                    // SourceReader = null;
                }
                if (null != SourceReaders)
                {
                    foreach (var reader in SourceReaders)
                        reader.Dispose();
                    // SourceReaders = null;
                }
                if (null != TargetWriter)
                {
                    TargetWriter.Dispose();
                    // TargetWriter = null;
                }
                if (null != FilterTargetWriter)
                {
                    FilterTargetWriter.Dispose();
                    // FilterTargetWriter = null;
                }
                // The logger must be the last thing to go!
                if (null != Logger)
                {
                    Logger.Dispose();
                    // Logger = null;
                }
            }
        }
        #endregion
    }
}
