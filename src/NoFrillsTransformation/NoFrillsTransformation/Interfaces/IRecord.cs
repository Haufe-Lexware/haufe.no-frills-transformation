using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    interface IRecord
    {
        string this[int index] { get; }
        string this[string fieldName] { get; }
    }
}
