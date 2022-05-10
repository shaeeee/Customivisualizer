using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Customivisualizer
{
	public class EquipmentManager
	{
		private static Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Item>? sheet;

		public static void Init(Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Item>? sheet)
		{
			if (sheet == null) return;
			EquipmentManager.sheet = sheet;
			Build(EquipmentManager.sheet);
		}

		public void GetItem()
		{

		}

		private static void Build(Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Item> sheet)
		{
			foreach (var e in sheet)
			{
				//e.ModelMain
			}
		}

		private static void Find()
		{

		}
	}
}
