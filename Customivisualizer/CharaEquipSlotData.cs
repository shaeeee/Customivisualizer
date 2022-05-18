using System.Runtime.InteropServices;

namespace Customivisualizer
{
	[StructLayout(LayoutKind.Explicit)]
	public struct CharaEquipSlotData
	{
		[FieldOffset((int)EquipSlotIndex.Head)] public ushort Head;
		[FieldOffset((int)EquipSlotIndex.HeadVariant)] public byte HeadVariant;
		[FieldOffset((int)EquipSlotIndex.HeadDye)] public byte HeadDye;

		[FieldOffset((int)EquipSlotIndex.Body)] public ushort Body;
		[FieldOffset((int)EquipSlotIndex.BodyVariant)] public byte BodyVariant;
		[FieldOffset((int)EquipSlotIndex.BodyDye)] public byte BodyDye;

		[FieldOffset((int)EquipSlotIndex.Hands)] public ushort Hands;
		[FieldOffset((int)EquipSlotIndex.HandsVariant)] public byte HandsVariant;
		[FieldOffset((int)EquipSlotIndex.HandsDye)] public byte HandsDye;

		[FieldOffset((int)EquipSlotIndex.Legs)] public ushort Legs;
		[FieldOffset((int)EquipSlotIndex.LegsVariant)] public byte LegsVariant;
		[FieldOffset((int)EquipSlotIndex.LegsDye)] public byte LegsDye;

		[FieldOffset((int)EquipSlotIndex.Feet)] public ushort Feet;
		[FieldOffset((int)EquipSlotIndex.FeetVariant)] public byte FeetVariant;
		[FieldOffset((int)EquipSlotIndex.FeetDye)] public byte FeetDye;

		[FieldOffset((int)EquipSlotIndex.Ears)] public ushort Ears;
		[FieldOffset((int)EquipSlotIndex.EarsVariant)] public byte EarsVariant;
		[FieldOffset((int)EquipSlotIndex.EarsDye)] public byte EarsDye;

		[FieldOffset((int)EquipSlotIndex.Neck)] public ushort Neck;
		[FieldOffset((int)EquipSlotIndex.NeckVariant)] public byte NeckVariant;
		[FieldOffset((int)EquipSlotIndex.NeckDye)] public byte NeckDye;

		[FieldOffset((int)EquipSlotIndex.Wrists)] public ushort Wrists;
		[FieldOffset((int)EquipSlotIndex.WristsVariant)] public byte WristsVariant;
		[FieldOffset((int)EquipSlotIndex.WristsDye)] public byte WristsDye;

		[FieldOffset((int)EquipSlotIndex.RRing)] public ushort RRing;
		[FieldOffset((int)EquipSlotIndex.RRingVariant)] public byte RRingVariant;
		[FieldOffset((int)EquipSlotIndex.RRingDye)] public byte RRingDye;

		[FieldOffset((int)EquipSlotIndex.LRing)] public ushort LRing;
		[FieldOffset((int)EquipSlotIndex.LRingVariant)] public byte LRingVariant;
		[FieldOffset((int)EquipSlotIndex.LRingDye)] public byte LRingDye;
	}
}