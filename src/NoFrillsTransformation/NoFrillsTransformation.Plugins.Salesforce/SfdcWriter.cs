using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Csv;
using NoFrillsTransformation.Plugins.Salesforce.Config;

namespace NoFrillsTransformation.Plugins.Salesforce
{
    class SfdcWriter : SfdcBase, ITargetWriter
    {
        public SfdcWriter(IContext context, SfdcTarget target, string[] fieldNames, SfdcConfig config)
            : base(context, config)
        {
            _target = target;
            _fieldNames = fieldNames;

            _tempCsvFileName = GetTempFileName(".csv");
            _csvWriter = new CsvWriterPlugin(
                context,
                "file://" + _tempCsvFileName,
                fieldNames,
                new int[] {},
                "delim=',' encoding='ISO-8859-1'" // comma separated for SFDC, plain ISO-8859-1
                );
        }

        private SfdcTarget _target;
        private string[] _fieldNames;
        private CsvWriterPlugin _csvWriter;
        private string _tempCsvFileName;

        public void WriteRecord(string[] fieldValues)
        {
            _csvWriter.WriteRecord(fieldValues);
        }

        public int RecordsWritten
        {
            get 
            {
                return _csvWriter.RecordsWritten;
            }
        }

        public void FinishWrite()
        {
            _csvWriter.FinishWrite();
            try
            {
                _csvWriter.Dispose();
            }
            finally
            {
                _csvWriter = null;
            }

            // Now we can do our SFDC DataLoader action.
            _context.Logger.Info("SfdcWriter: Initialization started.");
            string sdlFile = CreateMappingFile();
            string confFile = CreateProcessConf();
            CallDataLoader();
        }

        private void CallDataLoader()
        {
            _context.Logger.Info("SfdcWriter: Calling external process Data Loader.");
            string batPath = Path.Combine(_config.DataLoaderDir, "bin");
            string bat = Path.Combine(batPath, "process.bat");

            //int exitCode;
            ProcessStartInfo processInfo;
            Process process;

            string command = string.Format("process.bat \"{0}\" csvEntityWrite", _tempDir);

            processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            processInfo.WorkingDirectory = batPath;
            processInfo.CreateNoWindow = false;
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            process = Process.Start(processInfo);
            // *** Read the streams ***
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!output.Contains("The operation has fully completed"))
            {
                _context.Logger.Error(output);
                _context.Logger.Error(error);
                throw new InvalidOperationException("SFDC insert/upsert/delete via Data Loader failed. See above error message for more information.");
            }

            _context.Logger.Info(output);
            _context.Logger.Info("SfdcWriter: External process Data Loader has finished.");
        }

        private string CreateMappingFile()
        {
            _sdlFile = GetTempFileName(".sdl");
            using (var sr = new StreamWriter(new FileStream(_sdlFile, FileMode.CreateNew), Encoding.GetEncoding("ISO-8859-1")))
            {
                sr.WriteLine("# Automatically generated mapping file");
                foreach (var fieldName in _fieldNames)
                {
                    sr.WriteLine("{0}={0}", fieldName);
                }
            }
            _context.Logger.Info("SfdcWriter: Created mapping file (" + _sdlFile + ")");
            return _sdlFile;
        }

        private string CreateProcessConf()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "NoFrillsTransformation.Plugins.Salesforce.Templates.process-conf.write.template.xml";
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
            string statusDir = null;
            if (string.IsNullOrEmpty(_config.LogFileDir))
            {
                _logFile = GetTempFileName(".log");
                statusDir = _tempDir;
            }
            else
            {
                _logFile = Path.Combine(_config.LogFileDir, Path.GetRandomFileName() + ".log");
                statusDir = _config.LogFileDir;
            }
            var externalIdXml = "";
            if (!string.IsNullOrEmpty(_target.ExternalId))
                externalIdXml = string.Format("<entry key=\"sfdc.externalIdField\" value=\"{0}\"/>", _target.ExternalId);
            var outputErrorXml = "";
            if (!string.IsNullOrEmpty(_config.ErrorFileName))
                outputErrorXml = string.Format("<entry key=\"process.outputError\" value=\"{0}\"/>", _config.ErrorFileName);
            var outputSuccessXml = "";
            if (!string.IsNullOrEmpty(_config.SuccessFileName))
                outputSuccessXml = string.Format("<entry key=\"process.outputSuccess\" value=\"{0}\"/>", _config.SuccessFileName);
            var replaces = new string[,]
                { 
                  {"%DEBUGLOGFILE%", _logFile },
                  {"%ENDPOINT%", _config.SfdcEndPoint },
                  {"%USERNAME%", _config.SfdcUsername },
                  {"%PASSWORD%", _config.SfdcEncryptedPassword },
                  {"%ENTITY%", _target.Entity },
                  {"%EXTERNALIDXML%", externalIdXml },
                  {"%OPERATION%", _target.Operation },
                  {"%SDLFILE%", _sdlFile },
                  {"%STATUSDIR%", statusDir },
                  {"%OUTPUTERRORXML%", outputErrorXml },
                  {"%OUTPUTSUCCESSXML%", outputSuccessXml },
                  {"%CSVINFILE%", _tempCsvFileName }
                };

            int items = replaces.GetLength(0);
            for (int i = 0; i < items; ++i)
            {
                conf = conf.Replace(replaces[i, 0], replaces[i, 1]);
            }

            string configFile = Path.Combine(_tempDir, "process-conf.xml");
            AddTempFile(configFile);
            AddTempFile(Path.Combine(_tempDir, "config.properties"));
            AddTempFile(Path.Combine(_tempDir, "csvEntityAction_lastRun.properties"));
            File.WriteAllText(configFile, conf);

            _context.Logger.Info("SfdcWriter: Created process-conf.xml (" + configFile + ").");

            return configFile;
        }

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
                if (null != _csvWriter)
                {
                    _csvWriter.Dispose();
                    _csvWriter = null;
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
