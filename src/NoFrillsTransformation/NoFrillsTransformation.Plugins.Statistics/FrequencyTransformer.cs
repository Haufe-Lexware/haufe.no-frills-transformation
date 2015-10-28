using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Csv;

namespace NoFrillsTransformation.Plugins.Statistics
{
    class FrequencyTransformer : ISourceTransformer
    {
        private IContext _context;
        private string _target;
        private string _targetConfig;
        private IParameter[] _parameters;
        private Dictionary<string, Dictionary<string, int>> _freqs;

        public FrequencyTransformer(IContext context, string target, string targetConfig, IParameter[] parameters)
        {
            _context = context;
            _target = target;
            _targetConfig = targetConfig;
            _parameters = parameters;

            InitFreqs(parameters);
        }

        private void InitFreqs(IParameter[] parameters)
        {
            _freqs = new Dictionary<string, Dictionary<string, int>>();
            foreach (var param in parameters)
            {
                _freqs[param.Name] = new Dictionary<string, int>();
            }
        }

        public void Transform(IContext context, IEvaluator eval)
        {
            // Special case for the Freq transform: Obey filters already.
            if (!context.CurrentRecordMatchesFilter(eval))
                return;

            foreach (var param in _parameters)
            {
                var value = eval.Evaluate(eval, param.Function, context);
                if (string.IsNullOrEmpty(value))
                    value = "(empty)";
                if (_freqs[param.Name].ContainsKey(value))
                    _freqs[param.Name][value]++;
                else
                    _freqs[param.Name][value] = 1;
            }
        }

        public void FinishTransform()
        {
            try
            {
                using (var csv = new CsvWriterPlugin(_context, _target, new string[] { }, new int[] { }, _targetConfig + " headers='false'"))
                {

                    bool first = true;
                    var header = new string[] { "" };
                    var line = new string[] { "", "" };
                    foreach (var param in _parameters)
                    {
                        if (!first)
                            csv.WriteRecord(new string[] { }); // New line

                        header[0] = param.Name;
                        csv.WriteRecord(header);
                        line[0] = "Value";
                        line[1] = "Frequency";
                        csv.WriteRecord(line);
                        var f = _freqs[param.Name];
                        foreach (var key in f.Keys)
                        {
                            line[0] = key;
                            line[1] = f[key].ToString();
                            csv.WriteRecord(line);
                        }

                        first = false;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("FrequencyTransform: An error occurred while writing the frequency analysis results: " + ex.Message);
            }
        }

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
    }
}
