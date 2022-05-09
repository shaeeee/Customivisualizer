using System.Runtime.InteropServices;

namespace Customivisualizer
{
	[StructLayout(LayoutKind.Explicit)]
	public struct CharaEquipSlotData
	{
		[FieldOffset((int)EquipSlotIndex.Head1)] public uint Head;
		[FieldOffset((int)EquipSlotIndex.Body1)] public uint Body;
		[FieldOffset((int)EquipSlotIndex.Hands1)] public uint Hands;
		[FieldOffset((int)EquipSlotIndex.Legs1)] public uint Legs;
		[FieldOffset((int)EquipSlotIndex.Feet1)] public uint Feet;
		[FieldOffset((int)EquipSlotIndex.Ears1)] public uint Ears;
		[FieldOffset((int)EquipSlotIndex.Neck1)] public uint Neck;
		[FieldOffset((int)EquipSlotIndex.Wrists1)] public uint Wrists;
		[FieldOffset((int)EquipSlotIndex.RRing1)] public uint RRing;
		[FieldOffset((int)EquipSlotIndex.LRing1)] public uint LRing;
	}
}