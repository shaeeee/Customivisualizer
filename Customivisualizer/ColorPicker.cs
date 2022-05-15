using System;
using System.Collections.Generic;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using ImGuiNET;

namespace Customivisualizer
{
	public class ColorPicker
	{
		private ExcelSheet<Stain>? dyeSheet;

		public ColorPicker(ExcelSheet<Stain>? dyeSheet)
		{
			this.dyeSheet = dyeSheet;
		}

		public void BuildDyeSwatchUI(ref int[] item, ref bool performReload)
		{
			if (dyeSheet == null) return;
			float bWidth = 0;
			float bMargin = 9f;
			float wWidth = ImGui.GetWindowContentRegionMax().X;
			foreach (var i in dyeSheet)
			{
				var cb = BitConverter.GetBytes(i.Color);
				if (ImGui.ColorButton($"{i.Name}", new System.Numerics.Vector4(cb[2] / 255.0f, cb[1] / 255.0f, cb[0] / 255.0f, 1)))
				{
					item[2] = (int)i.RowId;
					performReload = true;
				}
				bWidth += ImGui.GetItemRectSize().X + bMargin;
				if (bWidth < wWidth) ImGui.SameLine();
				else bWidth = 0;
			}
		}

		public System.Numerics.Vector4 GetColor(uint index)
		{
			if (dyeSheet == null) return new System.Numerics.Vector4();
			var row = dyeSheet.GetRow(index);
			if (row == null) return new System.Numerics.Vector4();
			var color = row.Color;
			var cb = BitConverter.GetBytes(color);
			return new System.Numerics.Vector4(cb[2] / 255.0f, cb[1] / 255.0f, cb[0] / 255.0f, 1);
		}
	}
}
