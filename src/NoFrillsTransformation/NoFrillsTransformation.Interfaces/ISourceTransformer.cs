using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface ISourceTransformer : IDisposable
    {
        void Transform(IContext context, IEvaluator eval);
        bool HasField(string fieldName);
        IRecord CurrentRecord { get; }
        bool HasMoreRecords();
        void NextRecord();
    }
}
