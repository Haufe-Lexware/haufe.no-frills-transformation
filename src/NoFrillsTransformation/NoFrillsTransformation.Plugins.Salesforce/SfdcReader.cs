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
    internal class SfdcReader : ISourceReader //IDisposable// : ISourceReader
    {
        public SfdcReader(IContext context, SoqlQuery query, SfdcReaderConfig config)
        {
            _query = query;
            _config = config;
            _context = context;
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
        private SfdcReaderConfig _config;
        private IContext _context;
        private CsvReaderPlugin _csvReader;
        private string _csvOutput;
        private string _sdlFile;
        private string _logFile;
        private string _tempDir;
        private List<string> _tempFiles = new List<string>();

        public void Initialize()
        {
            _context.Logger.Info("SfdcReader: Initialization started.");
            _csvOutput = GetTempFileName(".csv");
            string sdlFile = CreateMappingFile();
            string confFile = CreateProcessConf();
            CallDataLoader();
            // Now delegate the rest to the CsvReaderPlugin.
            _csvReader = new CsvReaderPlugin("file://" + _csvOutput, "");
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
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            process.WaitForExit();

            // *** Read the streams ***
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (!output.Contains("The operation has fully completed"))
            {
                _context.Logger.Error(output);
                _context.Logger.Error(error);
                throw new InvalidOperationException("SFDC extraction via Data Loader failed. See above error message for more information.");
            }

            _context.Logger.Info(output);
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
            _logFile = GetTempFileName(".log");
            var replaces = new string[,]
                { 
                  {"%DEBUGLOGFILE%", _logFile },
                  {"%ENDPOINT%", _config.SfdcEndPoint },
                  {"%USERNAME%", _config.SfdcUsername },
                  {"%PASSWORD%", _config.SfdcEncryptedPassword },
                  {"%ENTITY%", _query.Entity },
                  {"%SOQL%", _query.Soql },
                  {"%SDLFILE%", _sdlFile },
                  {"%CSVOUTFILE%", _csvOutput }
                };

            int items = replaces.GetLength(0);
            for (int i = 0; i < items; ++i)
            {
                conf = conf.Replace(replaces[i, 0], replaces[i, 1]);
            }

            string configFile = Path.Combine(_tempDir, "process-conf.xml");
            _tempFiles.Add(configFile);
            _tempFiles.Add(Path.Combine(_tempDir, "config.properties"));
            _tempFiles.Add(Path.Combine(_tempDir, "csvAccountExtract_lastRun.properties"));
            File.WriteAllText(configFile, conf);

            _context.Logger.Info("SfdcReader: Created process-conf.xml (" + configFile + ").");

            return configFile;
        }

        private string CreateMappingFile()
        {
            _sdlFile = GetTempFileName(".sdl");
            using (var sr = new StreamWriter(_sdlFile))
            {
                foreach (var fieldName in _query.FieldNames)
                {
                    sr.WriteLine("{0}={0}", fieldName);
                }
            }
            _context.Logger.Info("SfdcReader: Created mapping file (" + _sdlFile + ")");
            return _sdlFile;
        }

        private string GetTempFileName(string suffix)
        {
            if (null == _tempDir)
            {
                _tempDir = Path.Combine(Path.GetTempPath(), "nft-" + Path.GetRandomFileName());
                Directory.CreateDirectory(_tempDir);
            }
            string fileName = Path.Combine(_tempDir, Path.GetRandomFileName() + suffix);
            _tempFiles.Add(fileName);
            return fileName;
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
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
            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero) 
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}

            if (_config.KeepTempFiles)
            {
                _context.Logger.Warning("SfdcReader: Not deleting temporary files due to configuration setting.");
                _context.Logger.Warning("SfdcReader: Temp directory: " + _tempDir);
            }

            if (!_config.KeepTempFiles 
                && _tempFiles.Count > 0)
            {
                _context.Logger.Info("SfdcReader: Cleaning up temporary files in directory: " + _tempDir);
                foreach(string file in _tempFiles)
                {
                    try 
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                    }
                    catch (Exception e)
                    {
                        _context.Logger.Warning("Failed to delete temp file '" + file + "': " + e.Message);
                    }
                }
                _tempFiles.Clear();

                
                if (null != _tempDir
                    && Directory.Exists(_tempDir))
                {
                    try
                    {
                        Directory.Delete(_tempDir);
                    }
                    catch (Exception ex) 
                    {
                        _context.Logger.Warning("Failed to delete temp dir: " + _tempDir + ", error: " + ex.Message);
                    }
                    _tempDir = null;
                }
                _context.Logger.Info("SfdcReader: Done cleaning up.");
            }
        }
        #endregion
    }
}
