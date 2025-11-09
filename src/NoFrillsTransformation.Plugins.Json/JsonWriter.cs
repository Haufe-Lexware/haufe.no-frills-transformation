using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Json
{
    internal class JsonWriter : ITargetWriter
    {
        private IContext _context;
        private string _fileName;
        private string[] _fieldNames;
        private int[] _fieldSizes;
        private string _config;
        private List<Dictionary<string, object?>> _records = new();
        private int _recordsWritten = 0;
        private bool _finished = false;

        public JsonWriter(IContext context, string target, string[] fieldNames, int[] fieldSizes, string? config)
        {
            _context = context;
            var tempFileName = target.StartsWith("json") ? target.Substring(7) : target.Substring(7); // json:// or file://
            _fileName = context.ResolveFileName(tempFileName, false);
            _fieldNames = fieldNames;
            _fieldSizes = fieldSizes;
            _config = config ?? string.Empty;
            ValidateFieldNames(_fieldNames);
        }

        public void WriteRecord(string[] fieldValues)
        {
            var root = new Dictionary<string, object?>();
            for (int i = 0; i < _fieldNames.Length; ++i)
            {
                if (!string.IsNullOrEmpty(fieldValues[i]))
                {
                    InsertNested(root, _fieldNames[i], fieldValues[i]);
                }
            }
            _records.Add(root);
            _recordsWritten++;
        }

        public int RecordsWritten => _recordsWritten;

        public void FinishWrite()
        {
            if (_finished) return;
            // Remove empty nested objects recursively
            var cleanedRecords = _records.Select(RemoveEmptyNested).Where(r => r.Count > 0).ToList();
            using var stream = new FileStream(_fileName, FileMode.Create, FileAccess.Write);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(stream, cleanedRecords, options);
            _finished = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_finished)
            {
                FinishWrite();
            }
        }

        private void InsertNested(Dictionary<string, object?> root, string fieldName, string value)
        {
            var parts = fieldName.Split('.');
            Dictionary<string, object?> current = root;
            for (int i = 0; i < parts.Length; ++i)
            {
                if (string.IsNullOrWhiteSpace(parts[i]))
                    throw new ArgumentException($"Invalid field name: '{fieldName}'");
                if (int.TryParse(parts[i], out _))
                    throw new ArgumentException($"Arrays are not supported in JSON output: '{fieldName}'");
                if (i == parts.Length - 1)
                {
                    if (current.ContainsKey(parts[i]))
                        throw new ArgumentException($"Duplicate property or conflict for '{fieldName}'");
                    current[parts[i]] = value;
                }
                else
                {
                    if (!current.ContainsKey(parts[i]))
                    {
                        current[parts[i]] = new Dictionary<string, object?>();
                    }
                    else if (current[parts[i]] is not Dictionary<string, object?>)
                    {
                        throw new ArgumentException($"Cannot have both '{parts[i]}' and '{fieldName}' as property and subproperty.");
                    }
                    current = (Dictionary<string, object?>)current[parts[i]]!;
                }
            }
        }

        private void ValidateFieldNames(string[] fieldNames)
        {
            var rootProps = new HashSet<string>();
            var allPaths = new HashSet<string>();
            foreach (var name in fieldNames)
            {
                var parts = name.Split('.');
                string path = "";
                for (int i = 0; i < parts.Length; ++i)
                {
                    if (string.IsNullOrWhiteSpace(parts[i]))
                        throw new ArgumentException($"Invalid field name: '{name}'");
                    if (int.TryParse(parts[i], out _))
                        throw new ArgumentException($"Arrays are not supported in JSON output: '{name}'");
                    path = path.Length == 0 ? parts[i] : path + "." + parts[i];
                    if (allPaths.Contains(path) && i < parts.Length - 1)
                        throw new ArgumentException($"Cannot have both '{path}' as property and '{name}' as subproperty.");
                }
                allPaths.Add(name);
            }
            // Check for conflicts: e.g. prop1 and prop1.subprop
            foreach (var name in fieldNames)
            {
                foreach (var other in fieldNames)
                {
                    if (name == other) continue;
                    if (other.StartsWith(name + "."))
                        throw new ArgumentException($"Cannot have both '{name}' and '{other}' as property and subproperty.");
                }
            }
        }

        private Dictionary<string, object?> RemoveEmptyNested(Dictionary<string, object?> dict)
        {
            var result = new Dictionary<string, object?>();
            foreach (var kvp in dict)
            {
                if (kvp.Value is Dictionary<string, object?> subDict)
                {
                    var cleaned = RemoveEmptyNested(subDict);
                    if (cleaned.Count > 0)
                        result[kvp.Key] = cleaned;
                }
                else if (kvp.Value is string s)
                {
                    if (!string.IsNullOrEmpty(s))
                        result[kvp.Key] = s;
                }
                else if (kvp.Value != null)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            return result;
        }
    }
}
