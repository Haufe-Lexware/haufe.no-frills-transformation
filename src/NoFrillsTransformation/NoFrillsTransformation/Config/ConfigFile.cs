using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace NoFrillsTransformation.Config
{
    [XmlRoot("Transformation")]
    public class ConfigFileXml
    {
        public SourceTargetXml Source { get; set; }
        public SourceTargetXml Target { get; set; }
        [XmlArray("SourceFilters")]
        [XmlArrayItem("SourceFilters")]
        public SourceFilterXml[] SourceFilters { get; set; }
        [XmlArray("LookupMaps")]
        [XmlArrayItem("LookupMap")]
        public LookupMapXml[] LookupMaps { get; set; }
        [XmlArray("Mappings")]
        [XmlArrayItem("Mapping")]
        public MappingXml[] Mappings { get; set; }
    }

    public class SourceTargetXml
    {
        [XmlText]
        public string Uri { get; set; }
        [XmlAttribute("config")]
        public string Config { get; set; }
    }

    public class SourceFilterXml
    {
        [XmlAttribute("field")]
        public string FieldName { get; set; }
        [XmlAttribute("type")]
        public string TypeName { get; set; }
        [XmlText]
        public string MatchString { get; set; }
    }

    public class LookupMapXml
    {
        [XmlAttribute("key")]
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
        [XmlAttribute("expression")]
        public string Expression { get; set; }
        [XmlAttribute("maxSize")]
        public int MaxSize { get; set; }
    }
}
