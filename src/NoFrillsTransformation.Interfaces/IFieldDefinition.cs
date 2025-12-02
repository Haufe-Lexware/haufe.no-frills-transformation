using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface IFieldDefinition
    {
        string FieldName { get; set; }
        int FieldSize { get; set; }
        string? Config { get; set; }
    }
}
