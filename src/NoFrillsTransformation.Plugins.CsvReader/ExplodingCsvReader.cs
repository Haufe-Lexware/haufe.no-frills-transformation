using NoFrillsTransformation.Interfaces;

namespace NoFrillsTransformation.Plugins.Csv
{
    internal class ExplodingCsvReader : ConfigurableBase, ISourceReader, IRecord
    {
        private CsvReaderPlugin _csvReader;
        public ExplodingCsvReader(IContext context, string source, string? config)
        {
            ReadConfig(config);
            if (string.IsNullOrEmpty(_explodeField))
                throw new ArgumentException("ExplodingCsvReader: Configuration explodefield must be set.");
            _csvReader = new CsvReaderPlugin(context, source, config);
            // Make sure we know the explode field in the CSV reader; check the field names, don't
            // use GetFieldIndex() because it will throw an exception if the field is not found.
            if (null == _csvReader.FieldNames)
                throw new ArgumentException("ExplodingCsvReader: CSV reader does not have field names.");
            _explodeFieldIndex = Array.IndexOf(_csvReader.FieldNames, _explodeField);
            if (_explodeFieldIndex < 0)
                throw new ArgumentException("ExplodingCsvReader: Explode field '" + _explodeField + "' not found in CSV reader.");
        }

        private string _explodeChar = ",";
        private string _explodeField = "";
        private bool _explodeTrim = false;
        private int _explodeFieldIndex = -1;
        private string[]? _currentExplodeRecord;
        private int _currentExplodeIndex = -1;

        protected override void SetConfig(string parameter, string configuration)
        {
            switch (parameter)
            {
                case "explodechar":
                    if (configuration.Length != 1)
                        throw new ArgumentException("Invalid explodechar setting: Explode character must be a single character (got: '" + configuration + "')");
                    _explodeChar = configuration;
                    break;

                case "explodefield":
                    _explodeField = configuration;
                    break;

                case "explodetrim":
                    _explodeTrim = BoolFromString(configuration);
                    break;
            }
        }

        public bool IsEndOfStream
        {
            get
            {
                return _csvReader.IsEndOfStream;
            }
        }

        public IRecord CurrentRecord => this;

        public int FieldCount => _csvReader.FieldCount;

        public string[] FieldNames => _csvReader.FieldNames;

        public string this[int index]
        {
            get
            {
                if (null == _currentExplodeRecord)
                    throw new InvalidOperationException("ExplodingCsvReader: No record available.");
                if (index == _explodeFieldIndex)
                    return _currentExplodeRecord[_currentExplodeIndex];
                return _csvReader[index];
            }
        }

        public string this[string fieldName]
        {
            get
            {
                if (null == _currentExplodeRecord)
                    throw new InvalidOperationException("ExplodingCsvReader: No record available.");
                if (fieldName == _explodeField)
                    return _currentExplodeRecord[_currentExplodeIndex];
                return _csvReader[fieldName];
            }
        }


        public void Dispose()
        {
            // Hmm
        }

        public int GetFieldIndex(string fieldName)
        {
            return _csvReader.GetFieldIndex(fieldName);
        }

        public void NextRecord()
        {
            if (_currentExplodeRecord == null || _currentExplodeIndex >= _currentExplodeRecord.Length - 1)
            {
                // Read the next record from the CSV reader
                do
                {
                    _csvReader.NextRecord();
                    if (_csvReader.IsEndOfStream)
                    {
                        _currentExplodeRecord = null;
                        return;
                    }
                    string explodeContent = _csvReader[_explodeFieldIndex];
                    if (string.IsNullOrEmpty(explodeContent))
                    {
                        _currentExplodeRecord = null;
                    }
                    else
                    {
                        // Explode the explode field, trim if necessary
                        _currentExplodeRecord = explodeContent.Split(_explodeChar[0]);
                        if (_explodeTrim)
                        {
                            for (int i = 0; i < _currentExplodeRecord.Length; ++i)
                            {
                                _currentExplodeRecord[i] = _currentExplodeRecord[i].Trim();
                            }
                        }
                        _currentExplodeIndex = 0;
                    }
                }
                while (_currentExplodeRecord == null || _currentExplodeRecord.Length == 0);
            }
            else
            {
                _currentExplodeIndex++;
            }
        }

        public IRecord Query(string key)
        {
            throw new InvalidOperationException("The Exploding CSV Reader (ExplodingCsvReader) does not support the Query() operator.");
        }
    }
}