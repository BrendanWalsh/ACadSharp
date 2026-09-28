using ACadSharp.Entities;
using ACadSharp.IO.Templates;
using System.Collections.Generic;
using System.Linq;

namespace ACadSharp.IO.DWG;

internal class DwgDocumentBuilder : CadDocumentBuilder
{
	/// <summary>
	/// Modeler geometry entities (R2013+) whose "has DS binary data" flag is set:
	/// their ACIS payload lives in the AcDs data section and is attached to them
	/// once that section is read.
	/// </summary>
	public List<ModelerGeometry> AcisDsEntities { get; } = new();

	public DwgReaderConfiguration Configuration { get; }

	public DwgHeaderHandlesCollection HeaderHandles { get; set; } = new();

	public List<CadBlockRecordTemplate> BlockRecordTemplates { get; set; } = new();

	public List<Entity> PaperSpaceEntities { get; } = new();

	public List<Entity> ModelSpaceEntities { get; } = new();

	public override bool KeepUnknownEntities => this.Configuration.KeepUnknownEntities;

	public override bool KeepUnknownNonGraphicalObjects => this.Configuration.KeepUnknownNonGraphicalObjects;

	public override bool IgnoreProxyGraphics => this.Configuration.IgnoreProxyGraphics;

	public DwgDocumentBuilder(ACadVersion version, CadDocument document, DwgReaderConfiguration configuration)
		: base(version, document)
	{
		this.Configuration = configuration;
	}

	public override void BuildDocument()
	{
		this.createMissingHandles();

		//Set the names for the block records before add them to the table
		foreach (var item in this.BlockRecordTemplates)
		{
			item.SetBlockToRecord(this, this.HeaderHandles);
		}

		this.RegisterTables();

		this.BuildTables();

		this.buildDictionaries();

		base.BuildDocument();

		this.HeaderHandles.UpdateHeader(this.DocumentToBuild.Header, this);
		this.restoreViewportActivity();
	}

	private void restoreViewportActivity()
	{
		// DWG stores layout membership, the last active viewport and off flags,
		// not DXF group 68. Reconstruct a consistent activity stack after linking.
		foreach (var layout in this.DocumentToBuild.Layouts.Where(layout => layout.IsPaperSpace))
		{
			int stack = 0;
			int modelViewports = 0;
			foreach (Viewport viewport in layout.Viewports.OrderBy(viewport => viewport != layout.LastActiveViewport))
			{
				if (viewport.Status.HasFlag(ViewportStatusFlags.ViewportOff))
				{
					viewport.ActiveStatus = 0;
				}
				else if (viewport != layout.PaperViewport
					&& ++modelViewports > this.DocumentToBuild.Header.MaxViewportCount)
				{
					viewport.ActiveStatus = -1;
				}
				else
				{
					viewport.ActiveStatus = checked((short)++stack);
				}
			}
		}
	}
}
