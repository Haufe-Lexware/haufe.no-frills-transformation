using System;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Statistics
{
    abstract class BaseTransformer : ISourceTransformer
    {
        protected IContext _context;
        protected string _target;
        protected string _targetConfig;
        protected bool _omitParameters = false;
        protected IParameter[] _parameters;

        public BaseTransformer(IContext context, string target, string? targetConfig, IParameter[] parameters)
        {
            _context = context;
            _target = target;
            _targetConfig = targetConfig ?? string.Empty;
            if (_targetConfig.ToLowerInvariant().Contains("omitparameters=true"))
                _omitParameters = true;
            _parameters = parameters;
        }

        public abstract void Transform(IContext context, IEvaluator eval);

        public abstract void FinishTransform();

        public bool HasField(string fieldName)
        {
            // We ain't got no fields. We just take stuff.
            return false;
        }

        public IRecord CurrentRecord
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool HasMoreRecords()
        {
            return false;
        }

        public bool HasResult()
        {
            return false;
        }

        public void NextRecord()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        protected string GetFileNameWithParameter(string targetFileName, string parameterName)
        {
            // Find the last dot to separate the file extension
            int lastDotIndex = targetFileName.LastIndexOf('.');
            
            if (lastDotIndex > 0)
            {
                // Inject parameter name before the extension
                string baseName = targetFileName.Substring(0, lastDotIndex);
                string extension = targetFileName.Substring(lastDotIndex);
                return $"{baseName}_{parameterName}{extension}";
            }
            else
            {
                // No extension found, just append parameter name
                return $"{targetFileName}_{parameterName}";
            }
        }
    }
}
