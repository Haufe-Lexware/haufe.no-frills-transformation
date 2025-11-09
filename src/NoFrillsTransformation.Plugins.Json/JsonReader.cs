using NoFrillsTransformation.Interfaces;
using System.Text.Json;

namespace NoFrillsTransformation.Plugins.Json
{
    internal class JsonReader : ISourceReader, IRecord
    {
        private IContext _context;
        private string _fileName;
        private string[] _fieldNames;
        private string[] _fieldValues;
        private Dictionary<string, int> _fieldIndexes = new Dictionary<string, int>();

        private List<Dictionary<string, string>> _records = new List<Dictionary<string, string>>();
        private int _currentIndex = -1;

        public JsonReader(IContext context, string source, string? config)
        {
            this._context = context;
            var tempFileName = source.Substring(source.IndexOf("//") + 2);
            this._fileName = context.ResolveFileName(tempFileName, true);

            string json = File.ReadAllText(_fileName);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Flatten all records
            var allFields = new HashSet<string>();
            if (root.ValueKind == JsonValueKind.Array)
            {
                int idx = 1;
                foreach (var element in root.EnumerateArray())
                {
                    var dict = new Dictionary<string, string>();
                    FlattenJson(element, dict, "", allFields);
                    _records.Add(dict);
                    idx++;
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Try to find the first array in the root, otherwise treat as single object
                bool foundArray = false;
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        int idx = 1;
                        foreach (var element in prop.Value.EnumerateArray())
                        {
                            var dict = new Dictionary<string, string>();
                            FlattenJson(element, dict, prop.Name + "." + idx, allFields);
                            _records.Add(dict);
                            idx++;
                        }
                        foundArray = true;
                    }
                }
                if (!foundArray)
                {
                    var dict = new Dictionary<string, string>();
                    FlattenJson(root, dict, "", allFields);
                    _records.Add(dict);
                }
            }
            else
            {
                throw new Exception("Unsupported JSON root type");
            }

            // Collect all field names
            _fieldNames = allFields.OrderBy(x => x).ToArray();
            _fieldValues = new string[_fieldNames.Length];
            for (int i = 0; i < _fieldNames.Length; i++)
                _fieldIndexes[_fieldNames[i]] = i;
        }

        private void FlattenJson(JsonElement element, Dictionary<string, string> dict, string prefix, HashSet<string> allFields)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        string newPrefix = string.IsNullOrEmpty(prefix) ? prop.Name : prefix + "." + prop.Name;
                        FlattenJson(prop.Value, dict, newPrefix, allFields);
                    }
                    break;
                case JsonValueKind.Array:
                    int idx = 1;
                    foreach (var item in element.EnumerateArray())
                    {
                        string newPrefix = string.IsNullOrEmpty(prefix) ? idx.ToString() : prefix + "." + idx;
                        FlattenJson(item, dict, newPrefix, allFields);
                        idx++;
                    }
                    break;
                default:
                    dict[prefix] = element.ToString();
                    allFields.Add(prefix);
                    break;
            }
        }

        private void ReadRecord()
        {
            if (_currentIndex < 0 || _currentIndex >= _records.Count)
                return;
            var rec = _records[_currentIndex];
            for (int i = 0; i < _fieldNames.Length; i++)
            {
                _fieldValues[i] = rec.TryGetValue(_fieldNames[i], out var val) ? val : string.Empty;
            }
        }

        public IRecord CurrentRecord => this;
        public string this[int index] => _fieldValues[index];
        public string this[string fieldName] => _fieldValues[_fieldIndexes[fieldName]];
        private bool _endOfStream => _currentIndex >= _records.Count || _records.Count == 0;
        public bool IsEndOfStream => _endOfStream;
        public int FieldCount => _fieldNames.Length;
        public string[] FieldNames => _fieldNames;
        public void Dispose() { }
        public int GetFieldIndex(string fieldName) => _fieldIndexes[fieldName];
        public void NextRecord()
        {
            if (_currentIndex + 1 >= _records.Count)
            {
                _currentIndex = _records.Count;
                return;
            }
            _currentIndex++;
            ReadRecord();
        }
        public IRecord Query(string key) => throw new NotImplementedException();
    }
}