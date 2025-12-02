using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NoFrillsTransformation.Interfaces;
using NoFrillsTransformation.Plugins.Salesforce.Config;

namespace NoFrillsTransformation.Plugins.Salesforce
{
    public class SfdcBase : IDisposable
    {
        internal SfdcBase(IContext context, SfdcConfig config)
        {
            _config = config;
            _context = context;
        }

        ~SfdcBase()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        protected SfdcConfig _config;
        protected IContext _context; 

        protected string _sdlFile;
        protected string _logFile;
        protected string _tempDir;
        private List<string> _tempFiles = new List<string>();

        protected void WaitAndLog(IContext _context, Process process)
        {
            bool fullyCompleted = false;
            string logLine = null;
            while ((logLine = process.StandardOutput.ReadLine()) != null)
            {
                var type = logLine.Length >= 24 ? logLine.Substring(24, 4).Trim().ToLowerInvariant() : "warn";
                var text = "Data Loader: " + (logLine.Length > 30 ? logLine.Substring(30) : logLine);
                switch (type)
                {
                    case "info": _context.Logger.Info(text); break;
                    case "warn": _context.Logger.Warning(text); break;
                    default: _context.Logger.Error(type + " ** " + text); break;
                }

                fullyCompleted = fullyCompleted | logLine.Contains("The operation has fully completed");
            }

            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!fullyCompleted)
            {
                _context.Logger.Error(logLine);
                _context.Logger.Error(error);
                throw new InvalidOperationException("SFDC operations via Data Loader failed. See above error message for more information.");
            }

            //_context.Logger.Info(output);
        }

        public static void GetBulkApiSettings(IContext context, SfdcConfig config, ref string useBulkApi, ref string bulkApiSerialMode, ref string bulkApiZipContent)
        {
            if (context.Parameters.ContainsKey("usebulkapi"))
            {
                string bulkApiYesNo = context.Parameters["usebulkapi"];
                bool result = false;
                if (Boolean.TryParse(bulkApiYesNo, out result))
                {
                    context.Logger.Info("SfdcBase: UseBulkApi from parameter: " + result);
                    config.UseBulkApi = result;
                }
                else
                {
                    context.Logger.Warning("SfdcBase: Invalid parameter for UseBulkApi: '" + bulkApiYesNo + "'. Defaulting to 'false'.");
                    config.UseBulkApi = false;
                }
            }
            if (config.UseBulkApi)
            {
                context.Logger.Info("SfdcBase: Using Bulk API.");
                useBulkApi = "<entry key=\"sfdc.useBulkApi\" value=\"true\" />";

                if (context.Parameters.ContainsKey("bulkapiserialmode"))
                {
                    string bulkSerial = context.Parameters["bulkapiserialmode"];
                    bool result = false;
                    if (Boolean.TryParse(bulkSerial, out result))
                    {
                        context.Logger.Info("SfdcBase: BulkApiSerialMode from parameter: " + result);
                        config.BulkApiSerialMode = result;
                    }
                    else
                    {
                        context.Logger.Warning("SfdcBase: Invalid parameter for BulkApiSerialMode: '" + bulkSerial + "'. Defaulting to 'false'.");
                        config.BulkApiSerialMode = false;
                    }
                }

                if (config.BulkApiSerialMode)
                {
                    bulkApiSerialMode = "<entry key=\"sfdc.bulkApiSerialMode\" value=\"true\" />";
                }
            }
            if (config.UseBulkApi)
            {
                if (context.Parameters.ContainsKey("bulkapizipcontent"))
                {
                    string bulkZip = context.Parameters["bulkapizipcontent"];
                    bool result = false;
                    if (Boolean.TryParse(bulkZip, out result))
                    {
                        context.Logger.Info("SfdcBase: BulkApiZipContent from parameter: " + result);
                        config.BulkApiZipContent = result;
                    }
                    else
                    {
                        context.Logger.Warning("SfdcBase: Invalid parameter for BulkApiZipContent: '" + bulkZip + "'. Defaulting to 'false'.");
                        config.BulkApiZipContent = false;
                    }
                }

                if (config.BulkApiZipContent)
                {
                    bulkApiZipContent = "<entry key=\"sfdc.bulkApiZipContent\" value=\"true\" />";
                }
            }
        }

        public static void GetBatchSizeSettings(IContext context, SfdcConfig config, string useBulkApi)
        {
            bool bulk = !string.IsNullOrEmpty(useBulkApi);
            int maxBatchSize = bulk ? 10000 : 200;

            if (config.LoadBatchSize == 0)
            {
                if (context.Parameters.ContainsKey("sfdcloadbatchsize"))
                {
                    string loadBatchSizeString = context.Parameters["sfdcloadbatchsize"];
                    int loadBatchSize = 0;
                    if (!int.TryParse(loadBatchSizeString, out loadBatchSize))
                    {
                        context.Logger.Warning("SfdcBase: Invalid LoadBatchSize from Parameter 'sfdcloadbatchsize': " + loadBatchSizeString + ", defaults to " + maxBatchSize + ".");
                        config.LoadBatchSize = maxBatchSize;
                    }
                    else
                    {
                        context.Logger.Info("SfdcBase: Setting LoadBatchSize from parameter 'sfdcloadbatchsize' to " + loadBatchSize);
                        config.LoadBatchSize = loadBatchSize;
                    }
                }
                else
                {
                    context.Logger.Info("SfdcBase: LoadBatchSize not specified, defaulting to " + maxBatchSize + ".");
                    config.LoadBatchSize = maxBatchSize;
                }
            }
            if (config.LoadBatchSize < 1 || config.LoadBatchSize > maxBatchSize)
            {
                context.Logger.Warning("SfdcBase: Invalid LoadBatchSize " + config.LoadBatchSize + ". Defaults to " + maxBatchSize + ".");
            }
        }

        public static void GetInsertNullsSetting(IContext context, SfdcConfig config)
        {
            bool insertNulls = config.InsertNulls;
            if (context.Parameters.ContainsKey("sfdcinsertnulls"))
            {
                string insertNullsString = context.Parameters["sfdcinsertnulls"];
                if (bool.TryParse(insertNullsString, out insertNulls))
                {
                    context.Logger.Info("SfdcBase: Setting SFDC InsertNulls from parameter 'sfdcinsertnulls' to " + insertNulls);
                    config.InsertNulls = insertNulls;
                }
                else
                {
                    context.Logger.Warning("SfdcBase: Invalid 'sfdcinsertnulls' parameter: '" + insertNullsString + "', keeping '" + insertNulls + "'.");
                }
            }
        }

        protected string GetTempFileName(string suffix)
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

        protected void AddTempFile(string fileName)
        {
            _tempFiles.Add(fileName);
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
            }
            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero) 
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}

            if (_config.KeepTempFiles)
            {
                _context.Logger.Warning("SfdcBase: Not deleting temporary files due to configuration setting.");
                _context.Logger.Warning("SfdcBase: Temp directory: " + _tempDir);
            }

            if (!_config.KeepTempFiles
                && _tempFiles.Count > 0)
            {
                _context.Logger.Info("SfdcBase: Cleaning up temporary files in directory: " + _tempDir);
                foreach (string file in _tempFiles)
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
                _context.Logger.Info("SfdcBase: Done cleaning up.");
            }
        }

    }
}
