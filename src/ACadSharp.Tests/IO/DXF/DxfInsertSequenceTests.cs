using ACadSharp.Entities;
using ACadSharp.Exceptions;
using ACadSharp.IO;
using ACadSharp.Tables;
using CSMath;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace ACadSharp.Tests.IO.DXF;

public class DxfInsertSequenceTests
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ReadHandlelessAttributeSequence(bool terminated)
	{
		string dxf = string.Join("\n",
			"0", "SECTION", "2", "HEADER", "9", "$ACADVER", "1", "AC1009", "0", "ENDSEC",
			"0", "SECTION", "2", "BLOCKS",
			"0", "BLOCK", "2", "SYMBOL", "70", "0", "10", "0", "20", "0", "30", "0",
			"0", "ENDBLK", "0", "ENDSEC",
			"0", "SECTION", "2", "ENTITIES",
			"0", "INSERT", "8", "0", "2", "SYMBOL", "66", "1", "10", "0", "20", "0", "30", "0",
			"0", "ATTRIB", "8", "0", "2", "LABEL", "1", "legacy", "70", "0",
			"10", "0", "20", "0", "30", "0", "40", "1",
			"0", "SEQEND", "8", "0",
			"0", "LINE", "8", "0", "10", "0", "20", "0", "30", "0",
			"11", "1", "21", "0", "31", "0", "0", "ENDSEC", "0", "EOF", "");
		if (!terminated)
		{
			dxf = dxf.Replace("0\nSEQEND\n8\n0\n", string.Empty);
		}
		using MemoryStream input = new MemoryStream(Encoding.ASCII.GetBytes(dxf));
		using DxfReader reader = new DxfReader(input);
		reader.Configuration.Failsafe = false;
		if (!terminated)
		{
			Assert.Throws<DxfException>(() => reader.Read());
			return;
		}

		CadDocument document = reader.Read();
		Assert.Equal(2, document.Entities.Count);
		Insert insert = Assert.Single(document.Entities.OfType<Insert>());
		AttributeEntity attribute = Assert.Single(insert.Attributes);
		Assert.Equal("legacy", attribute.Value);
		Assert.Same(insert, attribute.Owner);
		Assert.Same(insert, insert.Attributes.Seqend.Owner);
		Assert.NotEqual(0UL, attribute.Handle);
		Assert.NotEqual(0UL, insert.Attributes.Seqend.Handle);
	}

	[Theory]
	[InlineData(false, 0)]
	[InlineData(false, 1)]
	[InlineData(false, 2)]
	[InlineData(false, 3)]
	[InlineData(true, 0)]
	[InlineData(true, 1)]
	[InlineData(true, 2)]
	[InlineData(true, 3)]
	public void AttributeSequenceBelongsToInsert(bool inBlock, int ownerMode)
	{
		CadDocument document = new CadDocument(ACadVersion.AC1032);
		BlockRecord symbol = new BlockRecord("SYMBOL");
		document.BlockRecords.Add(symbol);
		BlockRecord container = inBlock ? new BlockRecord("CONTAINER") : document.ModelSpace;
		if (inBlock)
		{
			document.BlockRecords.Add(container);
		}
		Insert insert = new Insert(symbol);
		insert.Attributes.Add(new AttributeEntity { Tag = "FIRST", Value = "alpha", Height = 1 });
		insert.Attributes.Add(new AttributeEntity { Tag = "SECOND", Value = "beta", Height = 1 });
		container.Entities.Add(insert);
		container.Entities.Add(new Line(XYZ.Zero, XYZ.AxisX));

		using MemoryStream output = new MemoryStream();
		DxfWriter.Write(output, document, false);
		byte[] fixture = setSequenceOwners(output.ToArray(), ownerMode, container.Handle);
		using MemoryStream input = new MemoryStream(fixture);
		using DxfReader reader = new DxfReader(input);
		reader.Configuration.Failsafe = false;
		CadDocument result = reader.Read();
		assertSequence(result.BlockRecords[container.Name], insert);

		using MemoryStream binary = new MemoryStream();
		DxfWriter.Write(binary, result, true);
		using MemoryStream binaryInput = new MemoryStream(binary.ToArray());
		using DxfReader binaryReader = new DxfReader(binaryInput);
		binaryReader.Configuration.Failsafe = false;
		assertSequence(binaryReader.Read().BlockRecords[container.Name], insert);

		using MemoryStream dwg = new MemoryStream();
		DwgWriter.Write(dwg, result);
		using MemoryStream dwgInput = new MemoryStream(dwg.ToArray());
		using DwgReader dwgReader = new DwgReader(dwgInput);
		dwgReader.Configuration.Failsafe = false;
		assertSequence(dwgReader.Read().BlockRecords[container.Name], insert);
	}

	private static void assertSequence(BlockRecord container, Insert expected)
	{
		Assert.Equal(2, container.Entities.Count);
		Assert.Single(container.Entities.OfType<Line>());
		Insert actual = Assert.Single(container.Entities.OfType<Insert>());
		Assert.Equal(expected.Handle, actual.Handle);
		Assert.Equal(expected.Attributes.Select(a => a.Handle), actual.Attributes.Select(a => a.Handle));
		Assert.Equal(new[] { "FIRST", "SECOND" }, actual.Attributes.Select(a => a.Tag));
		Assert.Equal(new[] { "alpha", "beta" }, actual.Attributes.Select(a => a.Value));
		Assert.All(actual.Attributes, attribute => Assert.Same(actual, attribute.Owner));
		Assert.Equal(expected.Attributes.Seqend.Handle, actual.Attributes.Seqend.Handle);
		Assert.Same(actual, actual.Attributes.Seqend.Owner);
	}

	private static byte[] setSequenceOwners(byte[] dxf, int mode, ulong owner)
	{
		string[] lines = Encoding.UTF8.GetString(dxf).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
		StringBuilder result = new StringBuilder();
		string entity = string.Empty;
		for (int i = 0; i + 1 < lines.Length; i += 2)
		{
			int code = int.Parse(lines[i]);
			string value = lines[i + 1];
			if (code == 0)
			{
				entity = value;
			}
			if (code == 330 && (entity == "ATTRIB" || entity == "SEQEND"))
			{
				if (mode == 2)
				{
					continue;
				}
				if (mode == 1 || (mode == 3 && entity == "SEQEND"))
				{
					value = owner.ToString("X");
				}
			}
			result.AppendLine(lines[i]);
			result.AppendLine(value);
		}
		return Encoding.UTF8.GetBytes(result.ToString());
	}
}
