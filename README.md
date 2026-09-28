# ACadSharp
![Build&Test](https://github.com/DomCr/ACadSharp/actions/workflows/csharp.yml/badge.svg) ![License](https://img.shields.io/github/license/DomCr/ACadSharp) ![nuget](https://img.shields.io/nuget/v/Acadsharp) ![downloads](https://img.shields.io/nuget/dt/ACadSharp) [![Coverage Status](https://coveralls.io/repos/github/DomCR/ACadSharp/badge.svg?branch=master)](https://coveralls.io/github/DomCR/ACadSharp?branch=master) 
---

C# library to read/write cad files like dxf/dwg.

### dwg-workbench fork

The `dwg-workbench/multileader-column-heights` branch is based on
`3a1c66b9932f9e2dbfbbf3f03660c97d52e49b02` (3.8.0). It preserves repeated
MULTILEADER context group-code 144 values when reading and writing DXF,
including zero heights and their original order. This is a targeted field
preservation fix, not a claim of complete drawing fidelity.

The DXF reader also consumes INSERT attribute sequences structurally, retaining
their attributes and SEQEND as children even when owner handles name the block
or are absent. This avoids treating a sequence terminator as a standalone DWG
entity. A missing required terminator is reported as a DXF error.

The preservation fixes also retain hatch styles, seeds, pixel sizes and
gradient RGB values; attribute lock flags; valid text/dimension spacing
defaults; dimension block-reference flags; and parent XDATA before INSERT or
POLYLINE child sequences. RGB-to-index conversion selects the closest palette
entry instead of returning the first partial match.

DXF readback also retains XREF-qualified table names, dimension measurements
and flags, explicitly read viewport activation status, and the layout's
last-active-viewport reference. INSERT attribute
flags precede parent XDATA, and ByBlock color no longer forces opaque
transparency when reading DWG. Optional default alignment points, line-spacing
factors and non-derived zero hatch pixel sizes are not invented during export.
DWG viewport activity is reconstructed after linking layouts: the saved
last-active viewport comes first, other enabled viewports follow in layout
order, off viewports stay off, and the active model-viewport limit is respected.
This creates a consistent DXF activity stack without equating stack position
with viewport ID. It does not reproduce an unsaved interactive CAD session.

With the submodules initialized, a .NET 10 SDK and .NET 8 runtime can run the
focused regressions:

```console
dotnet test src/ACadSharp.Tests/ACadSharp.Tests.csproj -p:TargetFrameworks=net8.0 --filter "FullyQualifiedName~DxfMultiLeaderColumnTests|FullyQualifiedName~DxfInsertSequenceTests|FullyQualifiedName~DxfPreservationTests"
```

Check the [documentation](https://domcr.github.io/ACadSharp/index.html) for specific information about the library.

### Features

ACadSharp allows to read or create CAD files using .Net and also extract or modify existing content in the files, the main features may be listed as: 

- Read/Write Dxf binary files
- Read/Write Dxf ASCII files
- Read Dwg files
- Write Dwg files
- Extract/Modify the geometric information from the different [entities](https://help.autodesk.com/view/OARX/2021/ENU/?guid=GUID-7D07C886-FD1D-4A0C-A7AB-B4D21F18E484) in the model
- Control over the table elements like Blocks, Layers and Styles, allows you to read, create or modify the different tables

For other format export like svg, image, json or pdf check :construction: [ACadSharp.Formats](https://github.com/DomCR/ACadSharp.Formats) :construction:.

#### Compatible Dwg/Dxf versions:

|      | DxfReader | DxfWriter | DwgReader | DwgWriter |
------ | :-------: | :-------: | :-------: | :-------: |
AC1009 |    :heavy_check_mark:    |   :x:     |    :x:    |    :x:    |
AC1012 |    :heavy_check_mark:    |   :heavy_check_mark:     |    :x:    |    :x:    |
AC1014 |    :heavy_check_mark:    |   :heavy_check_mark:     |    :heavy_check_mark:    |    :heavy_check_mark:    |
AC1015 |    :heavy_check_mark:    |   :heavy_check_mark:     |    :heavy_check_mark:    |    :heavy_check_mark:    |
AC1018 |    :heavy_check_mark:    |   :heavy_check_mark:     |    :heavy_check_mark:    |    :heavy_check_mark:    |
AC1021 |    :heavy_check_mark:    |   :heavy_check_mark:     |    :heavy_check_mark:    |    :x:                   |
AC1024 |    :heavy_check_mark:    |   :heavy_check_mark:     |    :heavy_check_mark:    |    :heavy_check_mark:    |
AC1027 |    :heavy_check_mark:    |   :heavy_check_mark:     |    :heavy_check_mark:    |    :heavy_check_mark:    |
AC1032 |    :heavy_check_mark:    |   :heavy_check_mark:     |    :heavy_check_mark:    |    :heavy_check_mark:    |

Code Example
---

```c#
public static void Main()
{
	string path = "sample.dwg";
	CadDocument doc = DwgReader.Read(path, onNotification);
}

// Process a notification from the reader
private static void onNotification(object sender, NotificationEventArgs e)
{
	Console.WriteLine(e.Message);
}
```

For more code examples [check](https://github.com/DomCR/ACadSharp/tree/master/src/ACadSharp.Examples).

Building
---

Before building run:

```console
git submodule update --init --recursive
```

This command will clone the submodules necessary to build the project.