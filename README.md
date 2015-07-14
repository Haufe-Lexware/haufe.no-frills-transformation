# no-frills-transformation

"No frills transformation" (NFT) is intended to be a lightweight transformation engine, having an extensible interface which makes it simple to

* Add Source Readers
* Add Target Writers
* Add Operators (if you can't do with the Custom Operators)

Out of the box, NFT will read:

* CSV files in any encoding
* Salesforce SOQL queries
* SQLite Databases
* MySql Databases
* Oracle Databases
* SQL Server Databases
* From SAP RFCs if they have a TABLE as output value (limited support currently)

and write

* CSV files in any encoding (including with or without UTF-8 BOMs)
* Salesforce Objects (including Upserts and using External IDs)
* Rudimentary XML files

A special "transformation" filter is supported, which currently only has an implementation for

* SAP RFC Transformations: Read the parameters from a source and pass them to the RFC and retrieve the results from that to the output

There may be more to come; and if you have special needs, feel free to reach out and we'll look together what we can do about it.

In an ETL scenario, NFT is not specifically designed to do the "E" nor the "L" part, mostly just "T" tasks. 
But that quickly and efficiently, supporting the basic transformation stuff you might need (and
 with extensibility support if you need something out of the order). For convenience, the "E" is
 supported better than "L", with e.g. a Salesforce Reader for SOQL queries.
 
 Among supported transformations are:

* Copy (nop transformation, copy source to target)
* Lookup (in other sources)
* Filtering (on source data)

Feel free to contribute and create pull requests. I'll check them out and merge them if they make sense.

Head over to the [WIKI](https://github.com/DonMartin76/no-frills-transformation/wiki) for an extensive documentation:

* https://github.com/DonMartin76/no-frills-transformation/wiki

## Binary Downloads

Check out the [releases](https://github.com/DonMartin76/no-frills-transformation/releases) section for binary
packages of NFT.


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
  <Logger type="file" level="info">C:\Projects\no-frills-transformation\scratch\log.txt</Logger>
  <Source config="delim=','">file://C:\temp\users.csv</Source>
  <Target config="delim=','">file://C:\temp\Accounts.csv</Target>
  <LookupMaps>
    <LookupMap keyField="id" name="Status">
      <Source config="delim=','">file://C:\temp\status_mapping.csv</Source>
    </LookupMap>
  </LookupMaps>

  <FilterMode>AND</FilterMode>
  <SourceFilters>
    <!-- In shorter, this is just Contains($SOBID, "A") -->
    <SourceFilter>EqualsIgnoreCase(If(Contains($SOBID, "A"), "hooray", "boo"), "HooRay")</SourceFilter>
  </SourceFilters>

  <Mappings>
    <Mapping>
      <Fields>
        <!-- The target row number -->
        <Field name="RowNo" maxSize="10">TargetRowNum()</Field>
        <!-- The source row number -->
        <Field name="SourceRowNo" maxSize="10">SourceRowNum()</Field>
        <!-- Plain copy of field content -->
        <Field name="SapUserId__c" maxSize="16">LowerCase($SOBID)</Field>
        <!-- Concatenation of two source fields -->
        <Field name="Whatever" maxSize="50">Concat($OTYPE, $OBJID)</Field>
        <!-- status mapping from lookup "Status" (see definition above) -->
        <Field name="Status" maxSize="40">Status($BOGUSTYPE, $salesforce_status)</Field>
      </Fields>
    </Mapping>
  </Mappings>

  <OperatorConfigs>
    <OperatorConfig name="equals">This is a test. It doesn't do anything.</OperatorConfig>"
  </OperatorConfigs>
</Transformation>
```

So, what does this do?
* Defines log output to be written to a file (`log.txt`)
* Reads data from `C:\temp\users.csv`
* Writes data to `C:\temp\Accounts.csv`
* Loads a lookup map into the operator `Status` from `C:\temp\status_mapping.csv`
* Sets the filter mode to `AND` (all filter criteria must be met)
* Specifies a filter by using an expression which evaluates to a boolean
* Specifies a mapping with five output fields, each with a name, max size and an expression
* Passes on a configuration string to the `Equals` operator (see [Operator Configuration](#opconfig) for more information)

Please confer with the later sections for a more detailed specification of the options.


### Running the application

Just call the executable with the full path to the XML configuration file.

```
C:\Temp> NoFrillsTransformation.exe sample_config.xml
Operation completed successfully.
C:\Temp> 
```
If the operation completes without error, the executable with exit with the exit code 0 (Success).
Otherwise, an error message will be output to `stderr` and the exit code unequal 0 (for now, 1).

##### Running on Mac OS X and Linux

If you want to run NFT on Mac OS X, you need the Mono framework, which is the .NET implementation
for non-Windows platforms (such as Linux or Mac OS X).

Download and install Mono prior to running `NoFrillsTransformation.exe`, then proceed as follows:

```bash
$ mono NoFrillsTransformation.exe configFile.xml
Operation finished successfully.
$
```
