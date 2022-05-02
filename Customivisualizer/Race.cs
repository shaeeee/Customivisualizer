using System.Collections.Generic;

namespace Customivisualizer
{
	public enum Race : byte
	{
		[Display("Hyur")]
		HYUR = 1,
		[Display("Elezen")]
		ELEZEN = 2,
		[Display("Lalafell")]
		LALAFELL = 3,
		[Display("Miqo'te")]
		MIQOTE = 4,
		[Display("Roegadyn")]
		ROEGADYN = 5,
		[Display("Au Ra")]
		AU_RA = 6,
		[Display("Hrothgar")]
		HROTHGAR = 7,
		[Display("Viera")]
		VIERA = 8
	}

	[System.AttributeUsage(System.AttributeTargets.All)]
	public class Display : System.Attribute
	{
		private readonly string _value;

		public Display(string value)
		{
			_value = value;
		}

		public string Value => _value;
	}

	public class RaceMappings
	{
		public static readonly Dictionary<Race, int> RaceHairs = new()
		{
			{ Race.HYUR, 13 },
			{ Race.ELEZEN, 12 },
			{ Race.LALAFELL, 13 },
			{ Race.MIQOTE, 12 },
			{ Race.ROEGADYN, 13 },
			{ Race.AU_RA, 12 },
			{ Race.HROTHGAR, 8 },
			{ Race.VIERA, 17 },
		};
	}
}