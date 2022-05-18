using Dalamud.Game.ClientState.Objects.Enums;
using System;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Customivisualizer
{
	public class UIHelper
	{
		public ExcelSheet<CharaMakeType>? charaSheet { get; private set; }
		public ExcelSheet<Stain>? dyeSheet { get; private set; }

		public UIHelper(ExcelSheet<CharaMakeType>? charaSheet, ExcelSheet<Stain>? dyeSheet)
		{
			this.charaSheet = charaSheet;
			this.dyeSheet = dyeSheet;
		}

		public static void AdjustTribe(int race, ref int tribe)
		{
			tribe = race * 2 - tribe % 2;
		}

		public static void AdjustGender(int race, ref int gender)
		{
			gender = (Race)race == Race.HROTHGAR ? 0 : gender % 2;
		}

		public unsafe string[]? GetRaceAndTribe(byte[] charaCustomizeData)
		{
			if (charaSheet == null) return null;

			uint index = GetCharaTypeIndex(charaCustomizeData[(int)CustomizeIndex.Tribe], charaCustomizeData[(int)CustomizeIndex.Gender]);
			
			var row = charaSheet?.GetRow(index);
			return new string[] { $"{row?.Race.Value?.Feminine.ToString()}", $"{row?.Tribe.Value?.Feminine.ToString()}" };
		}

		private static uint GetCharaTypeIndex(int tribe, int gender)
		{
			return (uint)(2u * tribe + gender - 2u);
		}

		public unsafe CharaEquipSlotData GetEquipSlotData(Dalamud.Game.ClientState.Objects.SubKinds.PlayerCharacter? player)
		{
			if (player == null) return new CharaEquipSlotData();
			var bChara = (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)(void*)player.Address;
			return Marshal.PtrToStructure<CharaEquipSlotData>((IntPtr)bChara->Character.EquipSlotData);
		}
	}
}
