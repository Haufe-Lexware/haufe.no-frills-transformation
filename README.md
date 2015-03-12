# no-frills-transformation

"No frills transformation" (NFT) is intended to be a super lightweight transformation engine, with very limited support for reading and writing stuff, but having an extensible interface.

Out of the box, NFT will read:

* CSV files

and write

* CSV files

because... that's what I currently need ;-)

In an ETL scenario, NFT is neither designed to do the "E" nor the "L" part, just simple "T" tasks. 
But that quickly and efficiently, supporting the basic transformation stuff you might need (and
 with extensibility support if you need something out of the order). Among supported transformations are:

* Copy (nop transformation, copy source to target)
* Lookup (in other sources)
* Filtering (on source data)

Feel free to contribute and create pull requests. I'll check them out and merge them if they make sense.

## Acknowledgements

NFT (the CSV plugin to be more precise) is using parts of the brilliant CSV reader library written by
Sebastien Lorion. You can find the original project page at CodeProject:

http://www.codeproject.com/Articles/9258/A-Fast-CSV-Reader

NFT has pulled in the CSV parts in the source code directly. They can be found here:

https://github.com/DonMartin76/no-frills-transformation/tree/master/src/3rdParty/LumenWorks.Framework.IO

## Introduction

Nah. I hate reading. Let's get into the details on usage.

## Configuration

The basic idea of NFT is that you configure the transformations using an XML file. The XML file contains
the following main parts:

* A data source
* Source filters
* A target
* Lookup map definitions
* Target field definitions

### Sample XML config file

A typical simple XML configuration may look like this:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Transformation>
  <Source config="delim=','">file://C:\temp\users.csv</Source>
  <Target config="delim=','">file://C:\temp\Accounts.csv</Target>
  <LookupMaps>
    <LookupMap keyField="id" name="Status">
      <Source config="delim=','">file://C:\temp\status_mapping.csv</Source>
    </LookupMap>
  </LookupMaps>
  <!--
  <SourceFilters>
    <SourceFilter fieldName="BPTyp" type="equals">account</SourceFilter>
  </SourceFilters>
  -->

  <Mappings>
    <Mapping>
      <Fields>
        <!-- no op field mapping 'name' to 'Name' -->
        <Field name="RowNo" expression="SourceRowNum()" maxSize="10" />
        <Field name="SapUserId__c" expression="$SOBID" maxSize="16"/>
        <Field name="Whatever" expression="Concat($OTYPE, $OBJID)" maxSize="50"/>
        <!-- status mapping from lookup "Status" (see definition above) -->
        <Field name="Status" expression="Status($BOGUSTYPE, $salesforce_status)" maxSize="40"/>
      </Fields>
    </Mapping>
  </Mappings>
</Transformation>
```

## XML specification

### Source and Target tags

(...)

### Filtering

(...)

### Lookup map definitions

(...)

### Mapping definitions

(...)

### Field Expression syntax

(...)

## Extension points

NFT is leveraging MEF ([Microsoft Extension Framework][https://msdn.microsoft.com/en-us/en-en/library/dd460648(v=vs.110).aspx])
for dependency injection (inversion of control). There are two interfaces which may be used to hook into NFT:

```csharp
public interface ISourceReaderFactory
{
    bool CanReadSource(string source);

    ISourceReader CreateReader(string source, string config);
    bool SupportsQuery { get; }
}

public interface ITargetWriterFactory
{
    bool CanWriteTarget(string target);
    ITargetWriter CreateWriter(string target, string[] fieldNames, int[] fieldSizes, string config);
}
```

Both interfaces make use of methods to find out whether the reader/writer can read/write the source/target, based on
the format of the source/target string.

As an example, the CSV Plugin returns true for URIs starting with `file://` and ending with `.csv`.

### Writing plugins

Writing a plugin consists of following these steps:

* Create a new assembly for .NET 4.0 and reference 
** `NoFrillsTransformation.Interfaces` and
** `System.ComponentModel.Composition` (this is the MEF framework)
* Implement `ISourceReaderFactory` and/or `ITargetWriterFactory`
** Mark the classes as `[Export(typeof(ISourceReaderFactory))]` or `[Export(typeof(ITargetWriterFactory))]`, respectively (see below)
* Implement the `ISourceReader` and `IRecord` interfaces for reading sources
* And/or implement the `ITargetWriter` interface for writing to targets

Make sure that the assembly resides in the same directory as the EXE file; NFT should now automatically
pick up the plugin and start reading and/or writing from/to the new source/target.

Snippet from the `CsvWriterFactory`:

```csharp
[Export(typeof(NoFrillsTransformation.Interfaces.ITargetWriterFactory))]
public class CsvWriterFactory : ITargetWriterFactory
{
    public bool CanWriteTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return false;
        string temp = target.ToLowerInvariant();
        if (!temp.StartsWith("file://"))
            return false;
        if (!temp.EndsWith(".csv") && !temp.EndsWith(".txt"))
            return false;
        return true;
    }

    public ITargetWriter CreateWriter(string target, string[] fieldNames, int[] fieldSizes, string config)
    {
        return new CsvWriterPlugin(target, fieldNames, fieldSizes, config);
    }
}
```

Possible extension plugins could be stuff like:
* ODBC reader/writer
* SQLite reader/writer
* XML reader/writer
* Salesforce SOQL reader (that would be cool), Salesforce writer
