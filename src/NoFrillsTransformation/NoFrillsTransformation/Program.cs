using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using LumenWorks.Framework.IO.Csv;
using NoFrillsTransformation.Config;
using NoFrillsTransformation.Engine;

namespace NoFrillsTransformation
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (VerifyArguments(args))
                    (new Program()).Run(args[0]);
                Console.WriteLine("Operation finished successfully.");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Operation failed: " + e.Message);
            }
        }

        private static bool VerifyArguments(string[] args)
        {
            if (args.Length == 0)
                throw new ArgumentException("Missing argument. Use -help for instructions.");
            if (args[0].Equals("-help"))
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  NoFrillsTransformation.exe <config file>");
                return false;
            }
            return true;
        }

        private void Run(string configFileName)
        {
            ConfigFileXml configFile = null;
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigFileXml));
                using (var fs = new FileStream(configFileName, FileMode.Open))
                {
                    configFile = xmlSerializer.Deserialize(fs) as ConfigFileXml;
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("Could not read XML config file: " + e.Message);
            }

            var readerFactory = new ReaderFactory();
            var writerFactory = new WriterFactory();

            Context context = new Context();

            InitLookupMaps(configFile, readerFactory, context);
            ReadFilters(configFile, context);
            ReadMappings(configFile, context);

            using (var reader = readerFactory.CreateReader(configFile.Source.Uri, configFile.Source.Config))
            {
                context.SourceReader = reader;
                using (var writer = writerFactory.CreateWriter(configFile.Target.Uri, context.TargetFields, configFile.Target.Config))
                {
                    context.TargetWriter = writer;

                    Process(context);
                }
            }
        }

        private void Process(Context context)
        {
            try
            {
                string[] outValues = new string[context.TargetFields.Length];

                while (!context.SourceReader.IsEndOfStream)
                {
                    context.SourceReader.NextRecord();
                    context.SourceRecordsRead++;

                    if (!RecordMatchesFilter(context))
                    {
                        context.SourceRecordsFiltered++;
                        continue;
                    }
                    context.SourceRecordsProcessed++;

                    for (int i = 0; i < outValues.Length; ++i)
                    {
                        outValues[i] = ExpressionParser.EvaluateExpression(context.TargetFields[i].Expression, context);
                    }

                    context.TargetWriter.WriteRecord(outValues);
                    context.TargetRecordsWritten++;
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("A processing error occurred in source row " + context.SourceRecordsRead + ": " + e.Message);
            }
        }

        private bool RecordMatchesFilter(Context context)
        {
            foreach (var filter in context.Filters)
            {
                bool val = ExpressionParser.StringToBool(ExpressionParser.EvaluateExpression(filter.Expression, context));
                if (FilterMode.And == context.FilterMode
                    && !val)
                    return false;
                if (FilterMode.Or == context.FilterMode
                    && val)
                    return true;
            }
            return (FilterMode.And == context.FilterMode);
        }

        private void InitLookupMaps(ConfigFileXml configFile, ReaderFactory readerFactory, Context context)
        {
            if (null == configFile.LookupMaps)
                return; // No lookup maps
            foreach (var lookupMap in configFile.LookupMaps)
            {
                context.LookupMaps.Add(lookupMap.Name, LookupMapFactory.CreateLookupMap(lookupMap, readerFactory));
            }
        }

        private void ReadFilters(ConfigFileXml configFile, Context context)
        {
            try
            {
                context.FilterMode = FilterMode.And; // Default to and
                if (!string.IsNullOrWhiteSpace(configFile.FilterMode))
                {
                    string mode = configFile.FilterMode.ToLower();
                    if (mode.Equals("or"))
                        context.FilterMode = FilterMode.Or;
                }
                if (null == configFile.SourceFilters
                    || configFile.SourceFilters.Length == 0)
                    return; // No filters
                int filterCount = configFile.SourceFilters.Length;
                context.Filters = new FilterDef[filterCount];
                for (int i = 0; i < filterCount; ++i)
                {
                    var filterXml = configFile.SourceFilters[i];
                    context.Filters[i] = new FilterDef { Expression = ExpressionParser.ParseExpression(filterXml.Expression) };
                    if (!ExpressionParser.IsBoolExpression(context.Filters[i].Expression))
                        throw new InvalidOperationException("Source filter expression mismatch: Expression '" + 
                            filterXml.Expression + "' does not evaluate to a boolean value.");
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("Could not read/parse filter definitions: " + e.Message);
            }
        }

        private void ReadMappings(ConfigFileXml configFile, Context context)
        {
            if (null == configFile.Mappings)
                throw new ArgumentException("Configuration file misses Mappings section.");
            if (configFile.Mappings.Length == 0)
                throw new ArgumentException("Configuration file does not have a valid Mapping (<Mapping> tag missing).");
            if (configFile.Mappings.Length > 1)
                throw new ArgumentException("Multiple Mapping tags are not allowed currently.");
            var map = configFile.Mappings[0]; // Pick first mapping; it might be extended later on.
            if (null == map.Fields)
                throw new ArgumentException("Missing field definitions in mapping.");
            int fieldCount = map.Fields.Length;

            //context.TargetFieldNames = new string[] { };
            //context.TargetFieldSizes = new int[] { }; 
            context.TargetFields = new TargetFieldDef[fieldCount];

            for (int i = 0; i < fieldCount; ++i)
            {
                var field = map.Fields[i];
                try
                {
                    var tfd = new TargetFieldDef();
                    tfd.FieldName = field.Name;
                    tfd.FieldSize = field.MaxSize;
                    tfd.Expression = ExpressionParser.ParseExpression(field.Expression);

                    context.TargetFields[i] = tfd;
                }
                catch (Exception e)
                {
                    throw new ArgumentException("An error occurred while parsing field '" + field.Name + "': " + e.Message);
                }
            }
        }
    }
}
