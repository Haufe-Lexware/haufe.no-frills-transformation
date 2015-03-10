using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using LumenWorks.Framework.IO.Csv;
using NoFrillsTransformation.Config;

namespace NoFrillsTransformation
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, world.");

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigFileXml));
            ConfigFileXml configFile = null;
            using (var fs = new FileStream(@"C:\Projects\no-frills-transformation\config\sample_config.xml", FileMode.Open))
            {
                configFile = xmlSerializer.Deserialize(fs) as ConfigFileXml;
            }

            using (var tr = new StreamReader(new FileStream(configFile.SourceFile, FileMode.Open)))
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
        }

    }
}
