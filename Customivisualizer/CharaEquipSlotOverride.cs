using System;
using System.Collections.Generic;

namespace Customivisualizer
{
	public class CharaEquipSlotOverride : Override<CharaEquipSlotData>
	{
		public const int SIZE = 40;

		public static int GetModelID(byte b1, byte b2)
		{
			return b2 << 8 | b1;
		}
	}
}
