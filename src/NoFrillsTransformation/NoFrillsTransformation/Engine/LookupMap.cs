using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Config;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Engine
{
    class LookupMapFactory
    {
        public static LookupMap CreateLookupMap(LookupMapXml config, ReaderFactory readerFactory)
        {
            try
            {
                // First find a suitable reader
                using (var reader = readerFactory.CreateReader(config.Source.Uri, config.Source.Config))
                {
                    var lm = new LookupMap(reader.FieldNames);
                    lm.Id = config.Name;
                    lm.KeyField = config.Key;
                    int keyIndex = -1;
                    try
                    {
                        keyIndex = reader.GetFieldIndex(config.Key);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Cannot find key field '" + config.Key + "' in lookup source '" + config.Source.Uri + "'.");
                    }

                    int fieldCount = reader.FieldCount;

                    // Read data and store in memory.
                    while (!reader.IsEndOfStream)
                    {
                        reader.NextRecord(); 
                        var record = reader.CurrentRecord;
                        var values = new string[fieldCount];
                        for (int i = 0; i < fieldCount; ++i)
                        {
                            values[i] = record[i];
                        }
                        lm.Rows[values[keyIndex]] = values;
                    }

                    return lm;
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to create a reader for lookup map at source '" + config.Source.Uri + "'. " + e.Message);
            }
        }
    }

    class LookupMap
    {
        internal LookupMap(string[] fieldNames)
        {
            _fieldNames = fieldNames;
        }
        public string Id { get; set; }
        public string KeyField { get; set; }
        public string GetValue(string key, string fieldName)
        {
            return GetValue(key, GetFieldIndex(fieldName));
        }
        public string GetValue(string key, int fieldIndex)
        {
            return Rows[key][fieldIndex];
        }
        private string[] _fieldNames;
        public int GetFieldIndex(string fieldName)
        {
            for (int i=0; i<_fieldNames.Length; ++i)
            {
                if (_fieldNames[i].Equals(fieldName, StringComparison.InvariantCulture))
                    return i;
            }
            return -1;
        }
        private Dictionary<string, string[]> _rows = new Dictionary<string, string[]>();
        internal Dictionary<string, string[]> Rows
        { 
            get 
            {
                return _rows;
            } 
        }
    }
}
