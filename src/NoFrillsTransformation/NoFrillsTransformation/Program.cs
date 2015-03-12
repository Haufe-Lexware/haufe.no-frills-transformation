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
            (new Program()).Run();
        }

        private void Run()
        {
            /*
            Console.WriteLine("Hello, world.");

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigFileXml));
            ConfigFileXml configFile = null;
            using (var fs = new FileStream(@"C:\Projects\no-frills-transformation\config\sample_config.xml", FileMode.Open))
            {
                configFile = xmlSerializer.Deserialize(fs) as ConfigFileXml;
            }

            using (var tr = new StreamReader(new FileStream(configFile.Source.Uri, FileMode.Open)))
            {
                var csv = new CsvReader(tr, true, ',');
                Console.WriteLine("Number of fields:" + csv.FieldCount);
                while (!csv.EndOfStream)
                {
                    if (!csv.ReadNextRecord())
                        continue;

                    Console.WriteLine("Something: " + csv["SOBID"] + "~");
                }
            }

            var expression = ExpressionParser.ParseExpression("concat(\"MB\", Status($Status, $Meep))", null);
            */

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigFileXml));
            ConfigFileXml configFile = null;
            using (var fs = new FileStream(@"C:\Projects\no-frills-transformation\config\sample_config.xml", FileMode.Open))
            {
                configFile = xmlSerializer.Deserialize(fs) as ConfigFileXml;
            }

            var readerFactory = new ReaderFactory();
            var writerFactory = new WriterFactory();

            Context context = new Context();
            InitLookupMaps(configFile, readerFactory, context);
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

            //var expression = ExpressionParser.ParseExpression("OpptRecTypes(OpptMap($BELEGART, $DeveloperName), $Id)");
            //var more = ExpressionParser.ParseExpression("\"This is a fixed text.\"");
            //var wupf = ExpressionParser.ParseExpression("Concat(TargetRowNum(), Concat(\"-\", Lookilook(SourceRowNum(), $Whatever)))");
            //var meep = ExpressionParser.ParseExpression("Status($BOGUSTYPE, $text)");
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

                    for (int i=0; i<outValues.Length; ++i)
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
            // Filtering not supported yet.
            return true;
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

            for (int i=0; i<fieldCount; ++i)
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
