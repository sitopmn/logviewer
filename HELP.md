### Introduction

**LOG VIEWER** is a versatile application for viewing and analyzing log files produced by any other application. 
It is independent from any logging framework or format as it processes line based plain text.
These lines can be searched, filtered and parsed for distilling information out of them. 
A selection of charts is available to visualize the gained information for fast analysis. 
If more in depth analysis is required, it is possible to export the generated data 
as a comma separated values (CSV) file.

### Features

* No installation or infrastructure required
* Open multiple sequential log files at once
* Open archives with multiple sequential log files
* Display different views of the log in multiple tabs
* Automatic encoding & line ending detection
* Fast log file search and display
* Line parsing using patterns and dedicated parsers
* Filter and group lines using parsed value
* Visualize log file data using quick charting functions
* Export processed data for further analysis
* Save queries and searches for later use
* Export/Import saved queries and searches
* Extensible through a plugin API

---

### Querying logs

Log files are filtered using a simple keyword based search. 
More complex questions can be answered using a powerful query language inspired by *SQL* and *LINQ* in .NET

A query generally consists of seven parts which are processed in order:
1. A search operation
2. A text parser
3. A filter
4. A grouping operation
5. A projection operation
6. An ordering operation
7. A result limiting

#### 1. Searching Text

Text lines are matched using boolean phrase queries. Phrases are enclosed in double quotes. 

`"I am a search phrase"`

The search is based on tokenizing the log and the phrases, so results may not be absolutely accurate.
To enforce exact search, an exclamation mark can be prepended to the phrase. This will have a negative impact on the query run time
as an actual inspection of the log file is nessecary before the results are displayed.

`!"I am an exact match"`

Multiple phrases can be combined using the known boolean operators `and`, `or`, `xor` and `not`. Expressions can be grouped using
parentheses.

`("Phrase A" or "Phrase B") and not "Phrase C"`

The special keyword `*` is used to match any line. 

Anything entered in the search box is interpreted as a phrase directly. So no double quotes are required for quick searches.
Queries starting with a double quote or `*` are interpreted as queries in the language described here.

Phrases also support wildcards (`*`) and capturing values from the log file using a field notation. The field name contained in
curly braces is used as a column name in the result list.

`"I start with anything*and {capture} something in the end"`

If a data type conversion should be done while capturing, the data format
specifier is appended to the field name using a colon. Supported formats are `number`, `string` and `time` or 
a .NET date/time format specifier.

`"This reports a value of = {value:number}"`

The matched line is displayed in the *message* column. Any captured values are displayed in a column using the given name.
If a column name conflicts with the query syntax, it is possible to escape it using backquotes in the rest of the query.

#### 2. Parsing Text

If the simple capturing functions of the search phrases are not sufficient for pulling data from the line, a parser can be specified using
the `parse` keyword followed by the parser type. Parsers for `json` and `csv` formats are available. 
By default the message column is parsed but another column can be specified by giving the
appropriate name in parentheses.

`* parse json` or `"Log message: {foo}" parse json(foo)"`

The parser generates columns in the result list containing the data it extracted.

#### 3. Filtering Results

The result list generated from the search and/or the parser can be filtered based on column values using the familiar SQL-where syntax.
A full expression evaluation is implemented supporting mathematical operators, functions and more. 
The supported operators are `+`, `-`, `*`, `/`, `%`, `^`, `and`, `or`, `xor`, `not`. Column types are converted automatically
if required.

`"This reports a value of = {value:number}" where value > 4711 and value <= 15`

Conversion functions:
* number(_string_)
* time(_string_)
* time(_string_, _format as string_)

Mathematical functions:
* abs(_number_)
* sqrt(_number_)
* sin(_number_)
* cos(_number_)
* tan(_number_)
* log(_number_)
* exp(_number_)
* round(_number_)
* ceil(_number_)
* floor(_number_)
* trunc(_number_)
* min(_number_, _number_)
* max(_number_, _number_)
* pow(_number_, _number_)

Binning functions:
* bin(_number_, _number_)
* bin(_time_, _duration as string_)

String functions:
* contains(_string_, _needle_)
* startswith(_string_, _needle_)
* matches(_string_, _pattern_)
* length(_string_)

#### 4. Grouping Results

Grouping of results is supported like in *SQL* using the `group by` keyword. Any number of group keys can be specified each consisting 
of an expression on which the grouping is based on. By default, the expressions are used as result column names but using the `as` keyword
another name can be given.

`"This reports a value of = {value:number}" where value > 4711 and value <= 15 group by value as "Key"`

#### 5. Projection

A projection is given using the `select` keyword like in *SQL*. Multiple projections are separated using a comma.
Each projection consists of an expression or an aggregate function operating on an expression.

`"This reports a value of = {value:number}" select value * 2` 

`"This reports a value of = {value:number}" select distinct(value / 2)`

The supported aggregates are
* count(_expression_)
* distinct(_expression_)
* sum(_expression_)
* mean(_expression_)
* median(_expression_)
* min(_expression_)
* max(_expression_)
* first(_expression_)
* last(_expression_)
* most(_expression_)
* least(_expression_)

#### 6. Ordering The Results

A result odering is given using the `order by` keyword followed by a comma separated list of column names.
An optional sort direction is given when the column name is followed by `asc` for ascending or `desc` for descending order.

`"This reports a value of = {value:number}" select value * 2 as Foo order by Foo desc` 

#### 7. Limiting The Number Of Results

The result set is limited using the keyword `limit` followed by the maximum number of results.

`"This reports a value of = {value:number}" select value * 2 as Foo order by Foo desc limit 15` 

---

### Extending the application

**LOG VIEWER** is extensible via a plugin API based on Microsoft MEF.
The application directory is scanned for assemblies exporting MEF components which are automatically
loaded on startup. Interfaces for basic plugins are defined in `logviewer.exe`:
* `ILog` exported to provide log access
* `ILogIndexer` imported to allow additional processing while indexing the log files
* `IPageView` imported to define views on the log data and compose the menu
* `IPageViewModel` imported to create viewmodels for views
