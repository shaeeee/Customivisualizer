using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Customivisualizer
{
	public class CharaEquipSlotOverride : Override<CharaEquipSlotData>
	{
		public const int SLOTS = 10;
		public const int SIZE = 40;

		public override int Offset => 0x808;

		// no validation on this for now
		public void ChangeEquipSlotData(IntPtr equipSlotDataPtr)
		{
			var customData = Marshal.PtrToStructure<CharaEquipSlotData>(equipSlotDataPtr);

			customData.Head =			CustomData.Head;
			customData.HeadVariant =	CustomData.HeadVariant;
			customData.HeadDye =		CustomData.HeadDye;
			customData.Body =			CustomData.Body;
			customData.BodyVariant =	CustomData.BodyVariant;
			customData.BodyDye =		CustomData.BodyDye;
			customData.Hands =			CustomData.Hands;
			customData.HandsVariant =	CustomData.HandsVariant;
			customData.HandsDye =		CustomData.HandsDye;
			customData.Legs =			CustomData.Legs;
			customData.LegsVariant =	CustomData.LegsVariant;
			customData.LegsDye =		CustomData.LegsDye;
			customData.Feet =			CustomData.Feet;
			customData.FeetVariant =	CustomData.FeetVariant;
			customData.FeetDye = 		CustomData.FeetDye;
			customData.Ears =			CustomData.Ears;
			customData.EarsVariant =	CustomData.EarsVariant;
			customData.EarsDye =		CustomData.EarsDye;
			customData.Neck =			CustomData.Neck;
			customData.NeckVariant =	CustomData.NeckVariant;
			customData.NeckDye =		CustomData.NeckDye;
			customData.Wrists =			CustomData.Wrists;
			customData.WristsVariant =  CustomData.WristsVariant;
			customData.WristsDye =		CustomData.WristsDye;
			customData.RRing =			CustomData.RRing;
			customData.RRingVariant =	CustomData.RRingVariant;
			customData.RRingDye =		CustomData.RRingDye;
			customData.LRing =			CustomData.LRing;
			customData.LRingVariant =	CustomData.LRingVariant;
			customData.LRingDye =		CustomData.LRingDye;

			Marshal.StructureToPtr(customData, equipSlotDataPtr, true);
		}

		public static unsafe byte[] GetEquipSlotValues(Dalamud.Game.ClientState.Objects.SubKinds.PlayerCharacter? player)
		{
			if (player == null) return new byte[SIZE];
			var bChara = (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)(void*)player.Address;
			byte[] bytes = new byte[SIZE];
			Marshal.Copy((IntPtr)bChara->Character.EquipSlotData, bytes, 0, SIZE);
			return bytes;
		}
	}
}
