using System;
using System.Collections.Generic;
using System.Globalization;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Csv;

namespace NoFrillsTransformation.Plugins.Statistics
{
    class SumTransformer : BaseTransformer
    {
        private enum NumericType { Integer, Double }
        
        private class SumData
        {
            public NumericType Type { get; set; }
            public long IntSum { get; set; }
            public double DoubleSum { get; set; }
            
            public SumData()
            {
                Type = NumericType.Integer;
                IntSum = 0;
                DoubleSum = 0.0;
            }
        }

        private Dictionary<string, Dictionary<string, SumData>> _sums;

        public SumTransformer(IContext context, string target, string? targetConfig, IParameter[] parameters)
            : base(context, target, targetConfig, parameters)
        {
            // Validate that all parameters have Key and Value
            foreach (var param in parameters)
            {
                if (param.Key == null || param.Value == null)
                {
                    throw new ArgumentException(
                        $"SumTransformer requires all parameters to have Key and Value elements. Parameter '{param.Name}' is missing Key or Value."
                    );
                }
            }

            _sums = new Dictionary<string, Dictionary<string, SumData>>();
            InitSums(parameters);
        }

        private void InitSums(IParameter[] parameters)
        {
            foreach (var param in parameters)
            {
                _sums[param.Name] = new Dictionary<string, SumData>();
            }
        }

        public override void Transform(IContext context, IEvaluator eval)
        {
            // Obey filters
            if (!context.CurrentRecordMatchesFilter(eval))
                return;

            foreach (var param in _parameters)
            {
                // Evaluate the key expression
                var key = eval.Evaluate(eval, param.Key!, context);
                if (string.IsNullOrEmpty(key))
                    key = "(empty)";

                // Evaluate the value expression
                var valueStr = eval.Evaluate(eval, param.Value!, context);
                
                // Parse the numeric value
                var (numericType, intValue, doubleValue) = ParseNumericValue(valueStr, param.Name, key);

                // Get or create the sum data for this key
                if (!_sums[param.Name].ContainsKey(key))
                {
                    _sums[param.Name][key] = new SumData();
                }

                var sumData = _sums[param.Name][key];

                // Add the value based on the type
                if (numericType == NumericType.Double || sumData.Type == NumericType.Double)
                {
                    // Convert to double if needed
                    if (sumData.Type == NumericType.Integer)
                    {
                        sumData.DoubleSum = sumData.IntSum;
                        sumData.Type = NumericType.Double;
                    }
                    
                    if (numericType == NumericType.Integer)
                    {
                        sumData.DoubleSum += intValue;
                    }
                    else
                    {
                        sumData.DoubleSum += doubleValue;
                    }
                }
                else
                {
                    // Both are integers
                    sumData.IntSum += intValue;
                }
            }
        }

        private (NumericType type, long intValue, double doubleValue) ParseNumericValue(string valueStr, string paramName, string key)
        {
            // Treat empty strings as zero
            if (string.IsNullOrEmpty(valueStr))
            {
                return (NumericType.Integer, 0, 0.0);
            }

            // Try parsing as integer first
            if (long.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long intValue))
            {
                return (NumericType.Integer, intValue, 0.0);
            }

            // Try parsing as double
            if (double.TryParse(valueStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double doubleValue))
            {
                return (NumericType.Double, 0, doubleValue);
            }

            // Not a numeric value - throw exception
            throw new InvalidOperationException(
                $"SumTransformer: Non-numeric value encountered for parameter '{paramName}' with key '{key}': '{valueStr}'"
            );
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

                        line[0] = "Key";
                        line[1] = "Sum";
                        csv.WriteRecord(line);
                        
                        var sums = _sums[param.Name];
                        foreach (var key in sums.Keys)
                        {
                            var sumData = sums[key];
                            line[0] = key;
                            
                            if (sumData.Type == NumericType.Integer)
                            {
                                line[1] = sumData.IntSum.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                line[1] = sumData.DoubleSum.ToString(CultureInfo.InvariantCulture);
                            }
                            
                            csv.WriteRecord(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("SumTransform: An error occurred while writing the sum analysis results: " + ex.Message);
            }
        }
    }
}
