# Coding Style

This document describes the coding style used for code in this repository.


## Overview


### Goals And Scope

A coding style guide is a set of conventions (sometimes arbitrary) about how to write code for a project resp. codebase.
It is much easier to read and understand a large codebase when all the code in it is in a consistent style.

The guiding principles for this style guide are:

* Facilitate a consistent style for source code of all languages.
  There are conventions which are applicable in general, followed by language-specific conventions
  (which extend the general conventions, or sometimes override them).
* Follow language-specific default coding style conventions when no corresponding conventions are provided here.
* Don't force guidelines; there may be occasions where deviating from a guideline actually improves readability.
  However, when there are too many exceptions then maybe the rule resp. guideline needs to be updated.
* Use tools for automated formatting where applicable. This applies to conventions which are unambiguous.
  However, don't force auto-formatting, especially when there is room for interpretation.


### References

* https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md
* https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/framework-design-guidelines-digest.md
* https://google.github.io/styleguide/


## General Coding Style


### Directories And Files


#### Source code is in the '/src' directory

All source code is organized in 'projects' (or libraries, modules, packages, etc.); every such project resides within a
directory in the top-level */src* directory. There are no separate top-level directories for special kinds of source code
(for example, unit tests).

There may be exceptions for files which relate to multiple projects (for example, .NET solution files,
*directory.packages.props*).


#### Documentation is in the '/docs' directory

Dedicated documentation files (high-level and low-level concepts, supplementary documentation - like this file, etc.)
are located in the top-level */docs* directory. 

The organization of directories and files in the */docs* directory is independent from the */src* directory.


#### Naming of directories and files follows the programming language defaults

Each 'project' (or library, module, package, etc.) in the */src* directory typically uses one specific programming
language; naming and casing for files and sub-directories should follow this programming language's defaults.


#### The directory separator character is '/'

Historically, Linux/Mac uses '/' as directory separator whereas Windows uses '\\'. Modern programming languages
are capable of parsing Windows directory/file paths with '/' used as directory separator;
this makes '/' the 'more platform-independent' choice.

'/' should be used as directory separator character in source code and documentation where possible.

Note: Some Windows applications generate or overwrite directory separators with '\\' (for example, Visual Studio
when saving solution/project files); in this case it's recommended to change directory separators using a (different)
text editor.


#### Use UTF-8 encoding and Unix-style (LF) line endings

All text-based files should use UTF-8 encoding (no BOM) and Unix-style line endings (LF) unless the file format
explicitly requires otherwise.

Most editors can be configured correspondingly or respect the *.editorconfig* file.

Note: Some Windows applications generate or overwrite text files with other encoding resp. line endings
(for example, Visual Studio creates UTF-8 BOM files and auto-inserts CRLF even when confiugred otherwise);
in this case it's recommended to change encoding and line endings using a (different) text editor.


#### Text files end with an empty line

All text-based files (source code, JSON, XML, markdown, scripts, etc.) should end with a single empty line
(no trailing empty lines) unless the file format explicitly requires otherwise.


#### Avoid spurious whitespace

All text-based file formats which support a concept of non-significant whitespace (most programming language
and markup file formats) should not contain any spurious, non-significant whitespace, especially whitespace
at the end of a line.

Most editors support visualization of non-displayable characters which can aid detection of such unwanted whitespace.


### Source File Structure


#### Use 2 spaces per level of indentation

Most source code file formats support a concept of blocks and nesting. For every level of nesting
the corresponding source code resp. text should be indented by two space characters. Avoid TAB characters
unless the file format explicitly requires otherwise.

Most editors support increasing/decreasing indentation using keyboard shortcuts (TAB key).


#### Insert two empty lines between top-level sections

Most source code file formats have a typical, language-specific structure where a source file can be viewed
as a sequence of (top-level) sections. For example:

* A C# source file starts with `using` directives, followed by `namespace` and `class`/`struct`/etc. declarations.
* A Java source file starts with `import` statements, followed by a `class`/`interface` declaration.
* Optionally, a source file may start (or end) with a comment containing an organization/copyright statement.

Between such 'sections' there should be two empty lines.

This facilitates visual separation of a source file's top-level sections in contrast to single empty lines
in the source code (which may be used for visual separation of code blocks). Depending on the file format
there may be room for interpretation what is considered 'top-level'.


## C# Coding Style


### Source File Structure


#### Group 'using' directives by category, then sort alphabetically

The `using` directives should be specified at the top of a file before `namespace` declarations.
They should be grouped:

1. All `System.*` usings
2. Third party resp. library usings
3. Own project/organization usings

Then the `using` directives should be sorted alphabetically per group.

Consider adding one blank line between groups.


### Language Syntax


#### Use file scoped namespace declarations

A file scoped `namespace` declaration (introduced in .NET 6/C# 10) sets a namespace for a source file
without the need to put contained elements inside a block (using curly braces). This allows to write
source code starting at zero indentation (as it is technically not nested inside a namespace block statement).
See also https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/namespace.

Every C# source file should use a file scoped `namespace` declaration after the `using` directives
followed by the actual source code (top-level statement, no indentation).


## Other


#### NuGet depedency versioning is set up to use centralized package management (file `/directory.packages.props`).
