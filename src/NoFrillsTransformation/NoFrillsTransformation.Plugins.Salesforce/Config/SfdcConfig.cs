using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace NoFrillsTransformation.Plugins.Salesforce.Config
{
    [XmlRoot("SfdcConfig")]
    public class SfdcConfig
    {
        public string DataLoaderDir { get; set; }
        public string LogFileDir { get; set; }
        public string SuccessFileName { get; set; }
        public string ErrorFileName { get; set; }
        public string SfdcUsername { get; set; }
        public string SfdcPassword { get; set; }
        public string SfdcEncryptedPassword { get; set; }
        public string SfdcEndPoint { get; set; }
        public bool KeepTempFiles { get; set; }
        public int LoadBatchSize { get; set; }
        public bool FailOnErrors { get; set; }
        public bool FailOnFirstError { get; set; }
        public string Timezone { get; set; }
        public bool UseBulkApi { get; set; }
        public bool BulkApiSerialMode { get; set; }
        public bool BulkApiZipContent { get; set; }
    }

        //public SourceTargetXml Source { get; set; }
        //public SourceTargetXml Target { get; set; }
        //public string FilterMode { get; set; }
        //[XmlArray("SourceFilters")]
        //[XmlArrayItem("SourceFilter")]
        //public SourceFilterXml[] SourceFilters { get; set; }
        //[XmlArray("LookupMaps")]
        //[XmlArrayItem("LookupMap")]
        //public LookupMapXml[] LookupMaps { get; set; }
        //[XmlArray("Mappings")]
        //[XmlArrayItem("Mapping")]
        //public MappingXml[] Mappings { get; set; }
        //[XmlArray("OperatorConfigs")]
        //[XmlArrayItem("OperatorConfig")]
        //public OperatorConfigXml[] OperatorConfigs { get; set; }
    /*
    public class SourceTargetXml
    {
        [XmlText]
        public string Uri { get; set; }
        [XmlAttribute("config")]
        public string Config { get; set; }
    }

    public class SourceFilterXml
    {
        [XmlText]
        public string Expression { get; set; }
    }

    public class LookupMapXml
    {
        [XmlAttribute("keyField")]
        public string Key { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }
        public SourceTargetXml Source { get; set; }
    }

    public class MappingXml
    {
        [XmlArray("Fields")]
        [XmlArrayItem("Field")]
        public FieldXml[] Fields { get; set; }
    }

    public class FieldXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlText]
        public string Expression { get; set; }
        [XmlAttribute("maxSize")]
        public int MaxSize { get; set; }
    }

    public class OperatorConfigXml
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlText]
        public string Config { get; set; }
    }
     */
}
