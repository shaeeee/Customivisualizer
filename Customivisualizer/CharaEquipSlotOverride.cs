using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Customivisualizer
{
	public class CharaEquipSlotOverride
	{
		public static int GetFullModelID(byte b1, byte b2)
		{
			return b2 << 8 | b1;
		}
	}
}
