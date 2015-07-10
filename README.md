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

More information will follow as soon as I put up a proper release (with precompiled binaries).


## XML specification

### Logging tag

*Optional tag*

The syntax for the logging tag is as follows:
```xml
<Logger type="type" level="level">configuration</Logger>
```

| Tag/Attribute | Description|
| ------------- | ----------- |
| `Logging` content | Configuration of the logging component; depends on the logger type (see below) |
| `type` | The logger type; out of the box, `std`, `file` and `nil` are supported (see below) |
| `level` | Log logging level; `info`, `warning` and `error` are supported values. |

[Custom loggers can be implementing using the extension mechanism](#loggers).

##### Logger type `std`

The logger type `std` simply outputs logging information to the console (stdout). It does not require any
configuration; the config string is ignored, if it is present.
```xml
<Logger type="std" level="warning" />
```
This configuration outputs all errors and warnings to the console.

If no logger is given explicitly, the default is the `std` logger at `info` level (quite verbose loggin).

##### Logger type `file`

The logger type `file` outputs the log information to a file. The file name must be specified as the
configuration string of the logger component.
```xml
<Logger type="file" level="error">C:\Temp\log.txt</Logger>
```
This configuration outputs only errors to a log file at `C:\Temp\log.txt`.

##### Logger type `nil`

The `nil` logger type suppresses all kinds of log messages. You still get the exit code of the process
though.
```xml
<Logger type="nil" />
```
The log level is not important in this case, as all logs are suppressed anyway. Same applies to the
configuration: There is no configuration available.

### Source and Target tags

**Mandatory tags**

The syntax for source and target tags is as follows:
```xml
<Source config="config string">source URI</Source>
<Target config="config string">target URI</Source>
```

| Tag/Attribute | Description|
| ------------- | ----------- |
| `Source`		| Tag used for the data source of the transformation; must only be present once in the configuration |
| `Target`		| Tag used for the target of the transformation; must only be present once in the configuration |
| `config`		| Configuration string for the reader/writer. The format of this string is depending on the plugin selected for reading/writing to the URI |

##### CSV Reader Configuration Syntax

The CSV Reader which is supplied with NFT out of the box supports the following configuration settings, supplied in this format:

```
option1=value1 option2=value2
```

| Option | Possible values |
| ------ | --------------- |
| `delim`  | Delimiter character; defaults to `','`. Other normal option is `';'` |
| `multiline` | Switches support for multiline CSV records on or off; possible values are `false` or `true`. Defaults to `true` |


##### CSV Writer Configuration Syntax

The CSV Writer which is supplied with NFT out of the box supports the following configuration settings, supplied in this format:

```
option1=value1 option2=value2
```

| Option | Possible values |
| ------ | --------------- |
| `delim`  | Delimiter character; defaults to `','`. Other normal option is `';'` |
| `encoding` | Defines the output encoding of the CSV; defaults to `UTF-8` |

##### Salesforce SOQL reader configuration

More information to come; but I think I need to split up the documentation, this is getting to large and difficult to read.

### <a name="lookupTag"></a>Lookup map definitions

_Optional tags_

The syntax for defining lookup maps is as follows:
```xml
<LookupMaps>
	<LookupMap keyField="key field" name="operator name">
		<Source config="config string">source URI</Source>
	</LookupMap>
</LookupMaps>
```

The tag `LookupMap` may be defined multiply. The tag `LookupMaps` (the container) must only exist once (or not at all, if not needed).

| Tag/Attribute | Description|
| ------------- | ----------- |
| `LookupMaps`	| Container tag for the lookup maps |
| `LookupMap`	| Tag for defining a lookup map |
| `keyField`	| This attribute defines which field in the source is used for indexing. This is the field which will serve as the key for the lookup map. |
| `name`		| The name of the lookup map. This name can be used in expressions to look up values when transforming to output fields. [See section on lookups](#lookup) below (Expression syntax) |
| `Source`		| Tag used for the data source of the lookup map |
| `config`		| Configuration string for the reader. The format of this string is depending on the plugin selected for reading from the URI |

### Filtering

_Optional tags_

The filtering definition section of the configuration file consists of the following parts:
```xml
<FilterMode>filter mode</FilterMode>
<SourceFilters>
	<SourceFilter>boolean filter expression</SourceFilter>
</SourceFilters>
```

| Tag/Attribute | Description|
| ------------- | ----------- |
| `FilterMode`	| This tag decides on the filtering mode; possible values are `AND` and `OR`. In `AND` mode, all filter criteria must be met. In `OR` mode, one or more criteria must be met. |
| `SourceFilters` | Container tag for source filters. |
| `SourceFilter` | Definition of a filter; any number of this tag may exist. Content of this tag must be an Expression which evaluates to a boolean value. The expression may contain all operators available within NFT, [even including lookups](#lookup) |

**Example:**

```xml
<FilterMode>OR</FilterMode>
<SourceFilters>
	<SourceFilter>StartsWith($LastName, "A")</SourceFilter>
	<SourceFilter>StartsWith($LastName, "B")</SourceFilter>
</SourceFilters>
```
This filter definition will take all source rows in which the field `LastName` starts with either A or B.

### <a name="fields"></a>Field definitions

**Mandatory tags**

Within the `Mappings` tags, the output fields are defined using expressions.
```xml
<Mappings>
  <Mapping>
    <Fields>
	  <!-- Multiple occurance -->
      <Field name="field name" maxSize="max size">field expression</Field>
    </Fields>
  </Mapping>
</Mappings>
```

| Tag/Attribute | Description|
| ------------- | ----------- |
| `Mappings`    | Container tag for mappings |
| `Mapping`     | Container tag for a single mapping. Currently, NFT only supports a single mapping, this might change in the future, though |
| `Fields`      | Container tag for field definitions. |
| `Field`       | Field definition. Must contain a field expression evaluating to a string (or bool, which is converted to `true` or `false`). See below for [possible expression operators](#expressionSyntax). |
| `name`        | The name of the output field; this is the field name as it will be passed to the writer, e.g. for writing a header row (in case of CSV) |
| `maxSize`     | The maximum size of the output field (in characters). This is currently not used by the CSV reader or writer, but may be useful for future plugins, such as database writers. |

**Examples:**

```xml
<Field name="FirstName">$FNAM</Field>
```
Copies (no transformation) the content of the source field `FNAM` into a target field called `FirstName`

```xml
<Field name="FullName">Concat($FNAM, Concat(" ", $LNAM))</Field>
```
Concatenates the source fields `FNAM` and `LNAM` and a space (as a string literal `" "`) and outputs that into a target field `FullName`

```xml
<LookupMaps>
	<LookupMap keyField="statusId" name="StatusLookup">
		<Source config="delim=','">file://C:\Temp\StatusMap.csv</Source>
	</LookupMap>
</LookupMaps>
...
<Field name="Status">StatusLookup($STATUS, $Description)</Field>
...
```
Looks up the `Description` field from a defined lookup with the name `StatusLookup`, using the key in the source field `STATUS`.
This requires the lookup having been defined using a `<LookupMap>` tag (see example code).

### <a name="expressionSyntax"></a>Expression syntax

Expressions can be freely combined, as long as the return types are correct. There are three different return types (currently):
Bool, String and Int (actually 64 Bit integers).

In addition to the operators below, there are three special expressions: Field names, string
literals and integer literals. All three can be used anywhere a string expression can be used (with one exception, see [Lookup](#lookup)).

A field name is given by using the dollar ($) operator: e.g. `$title`, or `$firstName`.

A string literal is defined by putting a string within double quotes, e.g. `"this is a text"`.

Integer literals are defined by simply putting numbers into the expression, like `3`, `65535` or `-1`.

#### And

And operator. Returns `true` if both parameters return true.

| What       | Type |
| ----------- | -------- |
| Syntax | `And(param1, param2)` |
| Parameter 1 | bool |
| Parameter 2 | bool |
| Return type | bool |

**Example:** `And(Contains($title, "Lord"), Contains($author, "Tolkien"))`

#### Add

Add operator. Adds two numeric expressions and returns the sum.

| What       | Type |
| ----------- | -------- |
| Syntax | `Add(param1, param2)` |
| Parameter 1 | int |
| Parameter 2 | int |
| Return type | int |

**Example:** `Add(Int($field1), Int($field2))`

#### Concat

Concatenates two strings.

| What       | Type |
| ----------- | -------- |
| Syntax | `Concat(param1, param2)` |
| Parameter 1 | string |
| Parameter 2 | string |
| Return type | string |

**Example**: `Concat($firstName, Concat(" ", $lastName))`
 

#### Contains, ContainsIgnoreCase

Checks whether parameter 1 contains parameter 2. Comes in two flavors, `Contains` which takes
casing into account, and `ContainsIgnoreCase` which doesn't.

| What       | Type |
| ----------- | -------- |
| Syntax | `Contains[IgnoreCase](param1, param2)` |
| Parameter 1 | string |
| Parameter 2 | string |
| Return type | bool |

**Example**: `ContainsIgnoreCase($companyName, "Ltd.")`


#### Divide

Divide operator. Divides two integer values (param1 / param2). Rounds down.

| What       | Type |
| ----------- | -------- |
| Syntax | `Divide(param1, param2)` |
| Parameter 1 | int |
| Parameter 2 | int |
| Return type | int |


#### EndsWith

Checks whether parameter 1 ends with parameters, returns a boolean value.
This operator ignores the case.

| What       | Type |
| ----------- | -------- |
| Syntax | `EndsWith(param1, param2)` |
| Parameter 1 | string |
| Parameter 2 | string |
| Return type | bool |

**Example**: `EndsWith($companyName, "Ltd.")`


#### Equals, EqualsIgnoreCase

Checks whether to strings are equal. Comes in two flavors, `Equals` and `EqualsIgnoreCase`.

| What       | Type |
| ----------- | -------- |
| Syntax | `Equals[IgnoreCase](param1, param2)` |
| Parameter 1 | string |
| Parameter 2 | string |
| Return type | bool |

**Example**: `Equals($firstName, "Martin")`

#### HasKey

Checks whether a lookup key is present in a lookup map with the given name. Returns a bool.

| What       | Type |
| ----------- | -------- |
| Syntax | `HasKey(lookupName, key)` |
| lookupName | string |
| key | string |
| Return type | bool |

**Example**: `If(HasKey("Status", $StatusId), Status($StatusId, $StatusText), "missing lookup")`

Checks whether the "Status" [lookup map](#lookup) has an entry for the content of the field `StatusId`. If it has,
it returns this lookup, otherwise returns a string literal `"missing lookup"`. Please note that the lookup would
cause an error if doesn't find the key in the lookup map.


#### If

Evaluates the first argument; if it evaluates to `true`, returns the second argument, otherwise
the third.

| What       | Type |
| ----------- | -------- |
| Syntax | `If(condition, param1, param2)` |
| Condition | bool |
| Parameter 1 | string |
| Parameter 2 | string |
| Return type | string |

**Example**: `If(Contains($firstName, "Martin"), "good", "bad")`


#### Int

The `Int` operator converts a string to an integer value, e.g. for use in operators needing integer
parameters. This is typically used if the source columns contain numbers as strings which need
to be used for calculations of sorts.

| What       | Type |
| ----------- | -------- |
| Syntax | `Int(parameter)` |
| parameter | string |
| Return type | int |

**Example**: `Multiply(Int($wage), 10)`

Multiplies the content of the field `wage`, interpreted as an integer value, by 10.

#### Length

Returns the length (in characters) of a string expression.

| What       | Type |
| ----------- | -------- |
| Syntax | `Length(parameter)` |
| parameter | string |
| Return type | int |


#### <a name="lookup"></a>Lookups

Lookups must be defined in the LookupMaps described above. After that, the name of the lookup
map can be used as a binary operator which returns a string value.

| What       | Type |
| ----------- | -------- |
| Syntax | `[LookupName](param1, param2)`, `[LookupName]` is defined with the `name` [attribute of the `LookupMap` tag](#lookupTag). |
| Parameter 1 | string |
| Parameter 2 | string |
| Return type | string |

**Example**:
```xml
<LookupMaps>
  <LookupMap keyField="id" name="StatusLookup">
    <Source config="delim=','">file://C:\Temp\status_mapping.csv</Source>
  </LookupMap>
</LookupMaps>
```

In the [section on the field definitions](#fields) there is a further simple example on how to use lookup maps in expressions.

**Pro tip:** Nested lookups are also allowed. Typical use cases would be string to string mappings, where the source field
and desired outcome may not be strings, but rather indexes. Example:
```xml
<Field name="Status">TargetStatus(SourceStatus($StatusId, $StatusText), $TargetText)</Field>
```
This expression first looks up a `StatusText` from the `SourceStatus` lookup map, then maps that text using the `TargetStatus`
lookup map, and subsequently outputs the `TargetText` of that map into a target field called `Status`. The transformation is thus
the following:

```
Source.StatusId --> SourceStatus.StatusText --> TargetStatus.TargetText --> Target.Status
```

#### LowerCase

Transforms a string into lower case.

| What       | Type |
| ----------- | -------- |
| Syntax | `LowerCase(param1)` |
| Parameter 1 | string |
| Return type | string |

**Example**: `LowerCase($emailAddress)`


#### Modulo

Modulo operator. Divides param1 by param2 and returns the remainder.

| What       | Type |
| ----------- | -------- |
| Syntax | `Modulo(param1, param2)` |
| Parameter 1 | int |
| Parameter 2 | int |
| Return type | int |

#### Multiply

Multiply operator. Multiplies two integer values.

| What       | Type |
| ----------- | -------- |
| Syntax | `Multiply(param1, param2)` |
| Parameter 1 | int |
| Parameter 2 | int |
| Return type | int |

#### Or

Or operator. Returns `true` if one of the parameters return true.

| What       | Type |
| ----------- | -------- |
| Syntax | `Or(param1, param2)` |
| Parameter 1 | bool |
| Parameter 2 | bool |
| Return type | bool |

**Example**: `Or(Contains($title, "Lord"), Contains($title, "Ring"))`


#### StartsWith

Checks whether parameter 1 starts with parameters, returns a boolean value.
This operator ignores the case.

| What       | Type |
| ----------- | -------- |
| Syntax | `StartsWith(param1, param2)` |
| Parameter 1 | string |
| Parameter 2 | string |
| Return type | bool |

**Example**: `StartsWith($lastName, "M")`

#### Substring

The substring operator. Takes one string and two integer parameters.

| What       | Type |
| ----------- | -------- |
| Syntax | `Substring(string, offset, length)` |
| Parameter 1 | string |
| offset | int |
| length | int |
| Return type | int |

The `substring` operator follows these rules:
* If the offset is larger than the string length, the operator returns `""`
* If the length is larger than the string length minus the offset, the operator returns the entire string from the offset on (but does not pad!)
* If the length is negative, the entire string from offset on is returned.


#### Subtract

Subtract operator. Subtracts param2 from param1 (param1-param2).

| What       | Type |
| ----------- | -------- |
| Syntax | `Subtract(param1, param2)` |
| Parameter 1 | int |
| Parameter 2 | int |
| Return type | int |

#### Trim

Trims whitespace. Removes any leading or trailing whitespace from a string.

| What       | Type |
| ----------- | -------- |
| Syntax | `Trim(param1)` |
| Parameter 1 | any |
| Return type | string |

**Example**: `Trim($someField)`


#### UpperCase

Transforms a string into upper case.

| What       | Type |
| ----------- | -------- |
| Syntax | `UpperCase(param1)` |
| Parameter 1 | string |
| Return type | string |

**Example**: `UpperCase($city)`


## Extension points

NFT is leveraging MEF ([Microsoft Extension Framework](https://msdn.microsoft.com/en-us/en-en/library/dd460648(v=vs.110).aspx))
for dependency injection (inversion of control). There are four interfaces which may be used to hook into NFT:

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

public interface IOperator : IEvaluator
{
    ExpressionType Type { get; }
    string Name { get; }
    int ParamCount { get; }
    ParamType[] ParamTypes { get; }
    ParamType ReturnType { get; }

    void Configure(string config);
}

// Derived from
public interface IEvaluator
{
    string Evaluate(IEvaluator eval, IExpression expression, IContext context);
}

public interface ILoggerFactory
{
    string LogType { get; }
    ILogger CreateLogger(string configuration, LogLevel logLevel);
}
```

As to connectivity, i.e. reading and writing, both interfaces make use of methods to find out whether the reader/writer can read/write the source/target, based on
the format of the source/target string.

As an example, the CSV Plugin returns true for URIs starting with `file://` and ending with `.csv`.

For operators, things are even simpler. All you need to do is to implement the `IOperator` interface, using the `ExpressionType.Custom` expression type,
have it exported as `IOperator` via MEF, and the new operator will be picked up automatically by NFT. [See below](#operators) for more information
on writing operators.

Also loggers can be plugged in to NFT, using the `ILoggerFactory` interface, producing `ILogger` instances which can be used for logging purposes.


### Writing plugins

Writing a plugin consists of following these steps:

* Create a new assembly for .NET 4.0 and reference `NoFrillsTransformation.Interfaces` and `System.ComponentModel.Composition` (this is the MEF framework)
* Implement `ISourceReaderFactory` and/or `ITargetWriterFactory`
* Mark the classes as `[Export(typeof(ISourceReaderFactory))]` or `[Export(typeof(ITargetWriterFactory))]`, respectively (see below)
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

    public ITargetWriter CreateWriter(IContext context, string target, string[] fieldNames, int[] fieldSizes, string config)
    {
        return new CsvWriterPlugin(context, target, fieldNames, fieldSizes, config);
    }
}
```

Possible extension plugins could be stuff like:
* ODBC reader/writer
* SQLite reader/writer
* XML reader/writer
* Salesforce SOQL reader, Salesforce writer

### <a name="operators"></a>Writing Plugin operators

Writing custom operators isn't difficult, actually. You have two possibilities how to get started:

 * Forking the repository and adding the operators directly to `NoFrillsTransformation.Operators`
 * Writing your own assembly containing operators which adher to the `IOperator` interface.

If you go for the second method (which enables you to use the latest NFT releases and hook into that), you need to
follow these steps:

* Create a new assembly for .NET 4.0 and reference `NoFrillsTransformation.Interfaces` and `System.ComponentModel.Composition` (this is the MEF framework)
* Implement `IOperator`
* Mark the class as `[Export(typeof(IOperator))]`
* Make sure your assembly is located side by side with `NoFrillsTransformation.exe`

#### Example operator implementation

The following code shows a sample implementation of an operator. Obviously all `using` statements and namespace definitions
have been left out. Another place to check for the code is inside the `NoFrillsTransformation.Operators` assembly.

```csharp
public class ReverseOperator
{
	public ReverseOperator()
	{
		_paramTypes = new ParamType[] { ParamType.Any };
	}

	private ParamType[] _paramTypes;

	public ExpressionType Type { get { return ExpressionType.Custom; } }
	public string Name { get { return "reverse"; } }
	public string ParamCount { get { return 1; } }
	public ParamType[] ParamTypes { get { return _paramTypes; } }
	public ParamType ReturnType { get { return ParamType.String; } }

	public string Evaluate(IEvaluator eval, IExpression expression, IContext context)
	{
		string parameter = eval.Evaluate(eval, expression.Arguments[0], context);
		char[] charArray = parameter.ToCharArray();
		Array.Reverse(charArray);
		return new string(charArray);
	}

	public void Configure(string config)
	{
		// This operator does not need configuring
	}
}
```

This operator implements a reverse operator. Please note the use of the `IEvaluator` in the `Evaluate` method of the
operator. You will get passed the parameter into your `Evaluate` method; this has not yet been evaluated, thus you
need to call that method recursively first, before you actually perform your operator on the output.

This gives you full flexibility in implementing custom operators. In fact, only the three "special" operators are not
implemented in the `Operators` assembly, in the way shown above, and those are the "field name", the "string literal" 
and the "lookup" operators, as these require sepcial functionality. All others rely on this pattern, some taking
information from the passed `IContext` parameter (e.g. the `TargetRowNow` and `SourceRowNum` operators).

#### <a name="opconfig"></a>Operator Configuration

NFT offers a way to configure operators. The built in operators do not support any kind of configuration, but it is
fairly simple to imagine operators which might need configuration. For example operators for encryption or decryption.

Configuration on operator level can be passed from the configuration XML file, in this way:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Transformation>
  <!-- left out the rest -->
  <OperatorConfigs>
    <OperatorConfig name="equals">This is a test.</OperatorConfig>"
  </OperatorConfigs>
</Transformation>
```

The string in the `OperatorConfig` tags will be passed on to the operator's `Configure` method, as shown in the
`IOperator` interface above.

### <a name="loggers"></a>Writing plugin loggers

Writing custom loggers is also possible. As with the operators, you have two possibilities how to get started:

 * Forking the repository and adding the loggers directly to `NoFrillsTransformation.Logging`
 * Writing your own assembly containing operators which adher to the `ILoggerFactory` and `ILogger` interface.

If you go for the second method (which enables you to use the latest NFT releases and hook into that), you need to
follow these steps:

* Create a new assembly for .NET 4.0 and reference `NoFrillsTransformation.Interfaces` and `System.ComponentModel.Composition` (this is the MEF framework)
* Implement `ILoggerFactory`
* Mark the class as `[Export(typeof(ILogger))]`
* Implement your logger class, implementing the `ILogger` interface
* Make sure your assembly is located side by side with `NoFrillsTransformation.exe`
* Add a `Logger` type into your NFT configuration with your newly created log type string, e.g. `loggregate`, `odbc` or whatever you chose as `LogType`.