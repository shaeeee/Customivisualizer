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
		 *	_1 second byte of model ID 
		 *  _2 first byte of model ID
		 *  _V variant byte
		 *  _D dye index byte
		 */

		// Slot 0
		Head1 = 0,
		Head2 = 1,
		HeadV = 2,
		HeadD = 3,

		// Slot 1
		Body1 = 4,
		Body2 = 5,
		BodyV = 6,
		BodyD = 7,
		
		// Slot 2
		Hands1 = 8,
		Hands2 = 9,
		HandsV = 10,
		HandsD = 11,

		// Slot 3
		Legs1 = 12,
		Legs2 = 13,
		LegsV = 14,
		LegsD = 15,

		// Slot 4
		Feet1 = 16,
		Feet2 = 17,
		FeetV = 18,
		FeetD = 19,

		// Slot 5
		Ears1 = 20,
		Ears2 = 21,
		EarsV = 22,
		EarsD = 23,

		// Slot 6
		Neck1 = 24,
		Neck2 = 25,
		NeckV = 26,
		NeckD = 27,

		// Slot 7
		Wrists1 = 28,
		Wrists2 = 29,
		WristsV = 30,
		WristsD = 31,

		// Slot 8
		RRing1 = 32,
		RRing2 = 33,
		RRingV = 34,
		RRingD = 35,

		// Slot 9
		LRing1 = 36,
		LRing2 = 37,
		LRingV = 38,
		LRingD = 39,
	}
}
