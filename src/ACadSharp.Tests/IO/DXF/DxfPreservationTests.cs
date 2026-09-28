using ACadSharp.Entities;
using ACadSharp.Extensions;
using ACadSharp.IO;
using ACadSharp.Tables;
using CSMath;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace ACadSharp.Tests.IO.DXF;

public class DxfPreservationTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void HatchSettingsSurviveDxfAndDwg(bool enabled)
	{
		string dxf = string.Join("\n",
			"0", "SECTION", "2", "HEADER", "9", "$ACADVER", "1", "AC1032", "0", "ENDSEC",
			"0", "SECTION", "2", "ENTITIES",
			"0", "HATCH", "5", "A1", "100", "AcDbEntity", "8", "0", "100", "AcDbHatch",
			"10", "0", "20", "0", "30", "3", "210", "0", "220", "0", "230", "1",
			"2", "GRID", "70", "0", "71", "0", "91", "1", "92", "7",
			"72", "0", "73", "1", "93", "4",
			"10", "0", "20", "0", "10", "10", "20", "0",
			"10", "10", "20", "10", "10", "0", "20", "10", "97", "0",
			"75", "1", "76", "1", "52", "0", "41", "1", "77", "0", "78", "1",
			"53", "0", "43", "0", "44", "0", "45", "0", "46", "1",
			"79", "2", "49", "1", "49", "-1",
			"47", "0.25", "98", "2", "10", "1", "20", "1", "10", "2", "20", "2",
			"450", enabled ? "1" : "0", "451", "0", "460", "0.3", "461", "0.25",
			"452", "0", "462", "0.2", "453", "2",
			"463", "0", "63", "1", "421", "255",
			"463", "1", "63", "4", "421", "16776960", "470", "LINEAR",
			"0", "ENDSEC", "0", "EOF", "");
		using MemoryStream input = new MemoryStream(Encoding.ASCII.GetBytes(dxf));
		using DxfReader reader = new DxfReader(input);
		reader.Configuration.Failsafe = false;
		reader.Configuration.CreateDefaults = true;
		CadDocument document = reader.Read();

		foreach (CadDocument candidate in new[] { document, roundTrip(document, false, false),
			roundTrip(document, false, true), roundTrip(document, true) })
		{
			Hatch hatch = Assert.IsType<Hatch>(Assert.Single(candidate.Entities));
			Assert.Equal(HatchStyleType.Outer, hatch.Style);
			Assert.Equal(3, hatch.Elevation);
			Assert.Equal(0.25, hatch.PixelSize);
			Assert.Equal(new[] { new XY(1, 1), new XY(2, 2) }, hatch.SeedPoints);
			Assert.Equal(new double[] { 1, -1 }, Assert.Single(hatch.Pattern.Lines).DashLengths);
			Assert.Equal(enabled, hatch.GradientColor.Enabled);
			Assert.Equal("LINEAR", hatch.GradientColor.Name);
			Assert.Equal(0.3, hatch.GradientColor.Angle);
			Assert.Equal(0.25, hatch.GradientColor.Shift);
			Assert.Equal(0.2, hatch.GradientColor.ColorTint);
			Assert.Equal(new[] { new Color(0, 0, 255), new Color(255, 255, 0) },
				hatch.GradientColor.Colors.Select(c => c.Color));
		}
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void AttributeLockIsSeparateFromVersion(bool includeVersion)
	{
		string dxf = string.Join("\n", new[] {
			"0", "SECTION", "2", "HEADER", "9", "$ACADVER", "1", "AC1032", "0", "ENDSEC",
			"0", "SECTION", "2", "ENTITIES", "0", "ATTDEF", "5", "A1",
			"100", "AcDbEntity", "8", "0", "100", "AcDbText",
			"10", "0", "20", "0", "30", "0", "40", "1", "1", "attribute",
			"100", "AcDbAttributeDefinition" }
			.Concat(includeVersion ? new[] { "280", "0" } : Array.Empty<string>())
			.Concat(new[] { "2", "LABEL", "70", "0", "280", "1",
				"0", "ENDSEC", "0", "EOF", "" }));
		using MemoryStream input = new MemoryStream(Encoding.ASCII.GetBytes(dxf));
		using DxfReader reader = new DxfReader(input);
		reader.Configuration.Failsafe = false;
		reader.Configuration.CreateDefaults = true;
		CadDocument document = reader.Read();
		foreach (CadDocument candidate in new[] { document, roundTrip(document, false), roundTrip(document, true) })
		{
			AttributeDefinition attribute = Assert.IsType<AttributeDefinition>(Assert.Single(candidate.Entities));
			Assert.True(attribute.IsLocked);
			Assert.Equal(0, attribute.Version);
			Assert.Equal("LABEL", attribute.Tag);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void DimensionDefaultsAndFlagsSurviveDwg(bool userPosition)
	{
		Assert.Equal(LineSpacingStyleType.AtLeast, new MText().LineSpacingStyle);
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		BlockRecord block = new BlockRecord("*D1");
		document.BlockRecords.Add(block);
		DimensionLinear dimension = new DimensionLinear {
			FirstPoint = XYZ.Zero, SecondPoint = new XYZ(5, 0, 0), DefinitionPoint = new XYZ(0, 2, 0),
			Block = block, Flags = DimensionType.Linear | DimensionType.BlockReference,
			IsTextUserDefinedLocation = userPosition, UnknownFlag = true, FlipArrow1 = true
		};
		document.Entities.Add(dimension);
		DimensionLinear result = Assert.IsType<DimensionLinear>(Assert.Single(roundTrip(document, true).Entities));
		Assert.Equal(dimension.Flags, result.Flags);
		Assert.Equal(userPosition, result.IsTextUserDefinedLocation);
		Assert.Equal(LineSpacingStyleType.AtLeast, result.LineSpacingStyle);
		Assert.Equal(1.0, result.LineSpacingFactor);
		Assert.True(result.UnknownFlag);
		Assert.True(result.FlipArrow1);
		DimensionLinear dxf = Assert.IsType<DimensionLinear>(Assert.Single(roundTrip(roundTrip(document, true), false).Entities));
		Assert.True(dxf.UnknownFlag);
		Assert.True(dxf.FlipArrow1);
		Assert.Equal(5.0, dxf.Measurement);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ByBlockColorKeepsIndependentTransparency(bool byLayer)
	{
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		MText text = new MText { Value = "ByBlock", Color = Color.ByBlock,
			Transparency = byLayer ? Transparency.ByLayer : Transparency.Opaque };
		document.Entities.Add(text);
		MText result = Assert.IsType<MText>(Assert.Single(roundTrip(document, true).Entities));
		Assert.Equal(text.Transparency, result.Transparency);
	}

	[Theory]
	[InlineData("drawing|S-TEXT", true)]
	[InlineData("drawing|nested|S-TEXT", true)]
	[InlineData("|S-TEXT", false)]
	[InlineData("drawing||S-TEXT", false)]
	[InlineData("drawing|bad/name", false)]
	[InlineData("ordinary", true)]
	public void XrefTableNamesValidateTheirComponents(string name, bool valid)
	{
		Assert.Equal(valid, new Layer(name).HasValidDxfName());
	}

	[Fact]
	public void XrefLayersSurviveDxfReadback()
	{
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		string name = "drawing|nested|S-TEXT";
		document.Layers.Add(new Layer(name));
		Assert.True(roundTrip(roundTrip(document, true), false).Layers.Contains(name));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void DwgViewportActivityUsesLayoutStateNotViewportIds(bool selectLast)
	{
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		Viewport paper = Assert.Single(document.PaperSpace.Layout.Viewports);
		Viewport first = new Viewport();
		Viewport last = new Viewport();
		Viewport off = new Viewport { Status = ViewportStatusFlags.ViewportOff };
		document.PaperSpace.Entities.Add(first);
		document.PaperSpace.Entities.Add(last);
		document.PaperSpace.Entities.Add(off);
		document.PaperSpace.Layout.LastActiveViewport = selectLast ? last : paper;

		CadDocument result = roundTrip(document, true);
		Assert.Equal(selectLast ? 2 : 1, result.GetCadObject<Viewport>(paper.Handle).ActiveStatus);
		Assert.Equal(selectLast ? 3 : 2, result.GetCadObject<Viewport>(first.Handle).ActiveStatus);
		Assert.Equal(selectLast ? 1 : 3, result.GetCadObject<Viewport>(last.Handle).ActiveStatus);
		Assert.Equal(0, result.GetCadObject<Viewport>(off.Handle).ActiveStatus);
	}

	[Fact]
	public void DwgViewportActivityRespectsMaximumWithoutCountingPaperImage()
	{
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		Assert.Single(document.PaperSpace.Layout.Viewports);
		document.Header.MaxViewportCount = 2;
		for (int i = 0; i < 3; i++)
		{
			document.PaperSpace.Entities.Add(new Viewport());
		}
		document.PaperSpace.Layout.LastActiveViewport = document.PaperSpace.Layout.PaperViewport;
		CadDocument result = roundTrip(document, true);
		Assert.Equal(new short[] { 1, 2, 3, -1 },
			result.PaperSpace.Layout.Viewports.Select(viewport => viewport.ActiveStatus));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void LayoutLastActiveViewportSurvivesDxfAndDwg(bool binary)
	{
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		document.PaperSpace.Entities.Add(new Viewport());
		Viewport active = new Viewport();
		document.PaperSpace.Entities.Add(active);
		document.PaperSpace.Layout.LastActiveViewport = active;

		CadDocument dxf = roundTrip(document, false, binary);
		CadDocument dwg = roundTrip(dxf, true);
		foreach (CadDocument result in new[] { dxf, dwg, roundTrip(dwg, false, binary) })
		{
			Assert.Same(result.GetCadObject<Viewport>(active.Handle), result.PaperSpace.Layout.LastActiveViewport);
		}
	}

	[Theory]
	[InlineData((short)-1)]
	[InlineData((short)0)]
	[InlineData((short)3)]
	public void DxfViewportActiveStatusIsRead(short status)
	{
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		Viewport viewport = new Viewport { ActiveStatus = status };
		document.PaperSpace.Entities.Add(viewport);
		Viewport result = roundTrip(document, false).GetCadObject<Viewport>(viewport.Handle);
		Assert.Equal(status, result.ActiveStatus);
	}

	[Theory]
	[InlineData(0, 0, 255, 5)]
	[InlineData(255, 255, 0, 2)]
	[InlineData(255, 0, 0, 1)]
	[InlineData(250, 3, 3, 1)]
	public void ApproximateColorUsesNearestPaletteEntry(byte r, byte g, byte b, byte index)
	{
		Assert.Equal(index, Color.ApproxIndex(r, g, b));
	}

	private static CadDocument roundTrip(CadDocument document, bool dwg, bool binary = false)
	{
		using MemoryStream output = new MemoryStream();
		if (dwg)
		{
			DwgWriter.Write(output, document);
			using MemoryStream input = new MemoryStream(output.ToArray());
			using DwgReader reader = new DwgReader(input);
			reader.Configuration.Failsafe = false;
			return reader.Read();
		}
		else
		{
			DxfWriter.Write(output, document, binary);
			using MemoryStream input = new MemoryStream(output.ToArray());
			using DxfReader reader = new DxfReader(input);
			reader.Configuration.Failsafe = false;
			return reader.Read();
		}
	}
}
