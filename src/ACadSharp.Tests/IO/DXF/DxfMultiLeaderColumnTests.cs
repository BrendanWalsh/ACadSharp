using ACadSharp.Entities;
using ACadSharp.IO;
using System.IO;
using System.Text;
using Xunit;

namespace ACadSharp.Tests.IO.DXF;

public class DxfMultiLeaderColumnTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ReadRepeatedColumnHeights(bool failsafe)
	{
		string dxf = string.Join("\n",
			"0", "SECTION", "2", "HEADER",
			"9", "$ACADVER", "1", "AC1032",
			"0", "ENDSEC",
			"0", "SECTION", "2", "ENTITIES",
			"0", "MULTILEADER", "5", "A1",
			"100", "AcDbEntity", "8", "0",
			"100", "AcDbMLeader", "270", "2",
			"300", "CONTEXT_DATA{",
			"40", "1", "41", "1",
			"290", "1", "304", "Column height regression",
			"173", "2", "293", "0",
			"142", "20", "143", "1", "294", "0",
			"144", "0", "144", "4.25", "144", "4.25", "144", "8.5",
			"295", "1", "296", "0",
			"301", "}", "172", "2",
			"0", "ENDSEC", "0", "EOF", "");

		using MemoryStream input = new MemoryStream(Encoding.ASCII.GetBytes(dxf));
		using DxfReader reader = new DxfReader(input);
		reader.Configuration.Failsafe = failsafe;
		MultiLeader leader = Assert.IsType<MultiLeader>(Assert.Single(reader.Read().Entities));

		Assert.Equal(new double[] { 0, 4.25, 4.25, 8.5 }, leader.ContextData.ColumnSizes);
		Assert.Equal("Column height regression", leader.ContextData.TextLabel);
		Assert.True(leader.ContextData.WordBreak);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void RoundTripColumnHeights(bool binary, bool hasColumns)
	{
		double[] heights = hasColumns ? new double[] { 0, 4.25, 4.25, 8.5 } : new double[0];
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		MultiLeader leader = new MultiLeader();
		leader.ContextData.HasTextContents = true;
		leader.ContextData.TextLabel = "Column height regression";
		leader.ContextData.ColumnType = 2;
		foreach (double height in heights)
		{
			leader.ContextData.ColumnSizes.Add(height);
		}
		document.Entities.Add(leader);

		using MemoryStream output = new MemoryStream();
		DxfWriter.Write(output, document, binary);
		using MemoryStream input = new MemoryStream(output.ToArray());
		using DxfReader reader = new DxfReader(input);
		reader.Configuration.Failsafe = false;
		MultiLeader result = Assert.IsType<MultiLeader>(Assert.Single(reader.Read().Entities));

		Assert.Equal(heights, result.ContextData.ColumnSizes);
		Assert.Equal(leader.ContextData.TextLabel, result.ContextData.TextLabel);
	}
}
