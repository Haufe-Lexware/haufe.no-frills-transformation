using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Csv;

namespace NoFrillsTransformation.Plugins.Statistics
{
    class FrequencyTransformer : BaseTransformer
    {
        private Dictionary<string, Dictionary<string, int>> _freqs;

        public FrequencyTransformer(IContext context, string target, string? targetConfig, IParameter[] parameters)
            : base(context, target, targetConfig, parameters)
        {
            _freqs = new Dictionary<string, Dictionary<string, int>>();

            InitFreqs(parameters);
        }

        private void InitFreqs(IParameter[] parameters)
        {
            foreach (var param in parameters)
            {
                _freqs[param.Name] = new Dictionary<string, int>();
            }
        }

        public override void Transform(IContext context, IEvaluator eval)
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

        public override void FinishTransform()
        {
            try
            {
                foreach (var param in _parameters)
                {
                    // Generate filename with parameter name injected before file extension
                    string targetFileName = GetFileNameWithParameter(_target, param.Name);
                    
                    using (var csv = new CsvWriterPlugin(_context, targetFileName, new string[] { }, new int[] { }, _targetConfig + " headers='false'"))
                    {
                        var line = new string[] { "", "" };
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
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("FrequencyTransform: An error occurred while writing the frequency analysis results: " + ex.Message);
            }
        }
    }
}
