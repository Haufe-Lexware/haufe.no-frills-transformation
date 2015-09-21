using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NoFrillsTransformation.Plugins.Csv;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Salesforce.Config;

namespace NoFrillsTransformation.Plugins.Salesforce
{
    internal class SfdcReader : SfdcBase, ISourceReader //IDisposable// : ISourceReader
    {
        public SfdcReader(IContext context, SoqlQuery query, SfdcConfig config) : base(context, config)
        {
            _query = query;
        }

        // NOTE: Leave out the finalizer altogether if this class doesn't 
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are. 
        ~SfdcReader()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        private SoqlQuery _query;

        private CsvReaderPlugin _csvReader;
        private string _csvOutput;


        public void Initialize()
        {
            _context.Logger.Info("SfdcReader: Initialization started.");
            _csvOutput = GetTempFileName(".csv");
            string sdlFile = CreateMappingFile();
            string confFile = CreateProcessConf();
            CallDataLoader();
            // Now delegate the rest to the CsvReaderPlugin.
            _csvReader = new CsvReaderPlugin(_context, "file://" + _csvOutput, "encoding='utf-8'");
            _context.Logger.Info("SfdcReader: Initialization finished.");
        }

        private void CallDataLoader()
        {
            _context.Logger.Info("SfdcReader: Calling external process Data Loader.");
            string batPath = Path.Combine(_config.DataLoaderDir, "bin");
            string bat = Path.Combine(batPath, "process.bat");

            //int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            string command = string.Format("process.bat \"{0}\" csvExtractProcess", _tempDir);

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.WorkingDirectory = batPath;
            processInfo.CreateNoWindow = false;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            // *** Read the streams ***
            WaitAndLog(_context, process);

            _context.Logger.Info("SfdcReader: External process Data Loader has finished.");
        }

        private string CreateProcessConf()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "NoFrillsTransformation.Plugins.Salesforce.Templates.process-conf.template.xml";
            //var auxList = assembly.GetManifestResourceNames();
            string confTemplate = "";

            // Irky code to avoid CA2202 (do not dispose of objects twice)
            Stream stream = null;
            try
            {
                stream = assembly.GetManifestResourceStream(resourceName);
                using (StreamReader reader = new StreamReader(stream))
                {
                    stream = null;
                    confTemplate = reader.ReadToEnd();
                }
            }
            finally
            {
                if (null != stream)
                    stream.Dispose();
                stream = null;
            }

            string conf = confTemplate;
            if (string.IsNullOrEmpty(_config.LogFileDir))
                _logFile = GetTempFileName(".log");
            else
                _logFile = Path.Combine(_config.LogFileDir, Path.GetRandomFileName() + ".log");

            string useBulkApi = "";
            string bulkApiSerialMode = "";
            GetBulkApiSettings(ref useBulkApi, ref bulkApiSerialMode);

            var replaces = new string[,]
                { 
                  {"%DEBUGLOGFILE%", _logFile },
                  {"%ENDPOINT%", _config.SfdcEndPoint },
                  {"%USERNAME%", _config.SfdcUsername },
                  {"%PASSWORD%", _config.SfdcEncryptedPassword },
                  {"%ENTITY%", _query.Entity },
                  {"%SOQL%", _query.Soql },
                  {"%SDLFILE%", _sdlFile },
                  {"%CSVOUTFILE%", _csvOutput },
                  {"%OUTPUTBULKAPI%", useBulkApi },
                  {"%OUTPUTBULKAPISERIAL%", bulkApiSerialMode }
                };

            int items = replaces.GetLength(0);
            for (int i = 0; i < items; ++i)
            {
                conf = conf.Replace(replaces[i, 0], replaces[i, 1]);
            }

            string configFile = Path.Combine(_tempDir, "process-conf.xml");
            AddTempFile(configFile);
            AddTempFile(Path.Combine(_tempDir, "config.properties"));
            AddTempFile(Path.Combine(_tempDir, "csvAccountExtract_lastRun.properties"));
            File.WriteAllText(configFile, conf);

            _context.Logger.Info("SfdcReader: Created process-conf.xml (" + configFile + ").");

            return configFile;
        }

        private string CreateMappingFile()
        {
            _sdlFile = GetTempFileName(".sdl");
            using (var sr = new StreamWriter(new FileStream(_sdlFile, FileMode.CreateNew), Encoding.GetEncoding("ISO-8859-1")))
            {
                sr.WriteLine("# Automatically generated mapping file");
                foreach (var fieldName in _query.FieldNames)
                {
                    sr.WriteLine("{0}={0}", fieldName);
                }
            }
            _context.Logger.Info("SfdcReader: Created mapping file (" + _sdlFile + ")");
            return _sdlFile;
        }

        #region ISourceReader
        public bool IsEndOfStream { get { return _csvReader.IsEndOfStream; } }
        public void NextRecord() { _csvReader.NextRecord(); }
        public IRecord CurrentRecord { get { return _csvReader.CurrentRecord; } }
        public int FieldCount { get { return _csvReader.FieldCount; } }
        public string[] FieldNames { get { return _csvReader.FieldNames; } }
        public int GetFieldIndex(string fieldName) { return _csvReader.GetFieldIndex(fieldName); }
        public IRecord Query(string key) { return _csvReader.Query(key); }        
        #endregion


        #region IDisposable
        // Dispose() calls Dispose(true)
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (null != _csvReader)
                {
                    _csvReader.Dispose();
                    _csvReader = null;
                }
            }

            base.Dispose(disposing);
            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero) 
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}
        }
        #endregion
    }
}
