using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Customivisualizer
{
	public class EquipmentHelper
	{
		private static Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Item>? sheet;

		public static void Init(Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Item>? sheet)
		{
			if (sheet == null) return;
			EquipmentHelper.sheet = sheet;
		}

		public static byte[] GetItem(uint id)
		{
			byte[] def = new byte[3];
			if (sheet == null) return def;
			var item = sheet.GetRow(id);
			if (item == null) return def;
			return BitConverter.GetBytes(item.ModelMain);
		}

		public static int[] BytesToItem(byte[] data, int slot)
		{
			return new int[] { BitConverter.ToUInt16(data, slot), data[slot + 2], data[slot + 3] };
		}

		public static byte[] ItemToBytes(int[] data)
		{
			var modelId = BitConverter.GetBytes(data[0]);
			return new byte[] { modelId[0], modelId[1], (byte)data[1], (byte)data[2] };
		}
	}
}
