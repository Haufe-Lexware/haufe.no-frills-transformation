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

### Lookup map definitions

(...)

### Mapping definitions

(...)

### Field Expression syntax

(...)
