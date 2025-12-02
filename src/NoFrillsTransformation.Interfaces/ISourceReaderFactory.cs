using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoFrillsTransformation.Interfaces
{
    public interface ISourceReaderFactory
    {
        bool CanReadSource(string source);

        ISourceReader CreateReader(IContext context, string source, string? config);
        bool SupportsQuery { get; }
    }
}
