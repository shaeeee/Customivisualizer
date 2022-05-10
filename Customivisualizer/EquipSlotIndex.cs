using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Customivisualizer
{
	public enum EquipSlotIndex
	{
		/*
		 *	_2 second byte of model ID
		 *  _V variant byte
		 *  _D dye index byte
		 */

		// Slot 0
		Head = 0,
		Head2 = 1,
		HeadVariant = 2,
		HeadDye = 3,

		// Slot 1
		Body = 4,
		Body2 = 5,
		BodyVariant = 6,
		BodyDye = 7,
		
		// Slot 2
		Hands = 8,
		Hands2 = 9,
		HandsVariant = 10,
		HandsDye = 11,

		// Slot 3
		Legs = 12,
		Legs2 = 13,
		LegsVariant = 14,
		LegsDye = 15,

		// Slot 4
		Feet = 16,
		Feet2 = 17,
		FeetVariant = 18,
		FeetDye = 19,

		// Slot 5
		Ears = 20,
		Ears2 = 21,
		EarsVariant = 22,
		EarsDye = 23,

		// Slot 6
		Neck = 24,
		Neck2 = 25,
		NeckVariant = 26,
		NeckDye = 27,

		// Slot 7
		Wrists = 28,
		Wrists2 = 29,
		WristsVariant = 30,
		WristsDye = 31,

		// Slot 8
		RRing = 32,
		RRing2 = 33,
		RRingVariant = 34,
		RRingDye = 35,

		// Slot 9
		LRing = 36,
		LRing2 = 37,
		LRingVariant = 38,
		LRingDye = 39,
	}
}
