using System.Runtime.InteropServices;

namespace Customivisualizer
{
	[StructLayout(LayoutKind.Sequential)]
	public struct CharaCustomizeData
	{
		public byte Race;
		public byte Gender;
		public byte ModelType;
		public byte Height;
		public byte Tribe;
		public byte FaceType;
		public byte HairStyle;
		public byte HasHighlights;
		public byte SkinColor;
		public byte EyeColor;
		public byte HairColor;
		public byte HairColor2;
		public byte FaceFeatures;
		public byte FaceFeaturesColor;
		public byte Eyebrows;
		public byte EyeColor2;
		public byte EyeShape;
		public byte NoseShape;
		public byte JawShape;
		public byte LipStyle;
		public byte LipColor;
		public byte RaceFeatureSize;
		public byte RaceFeatureType;
		public byte BustSize;
		public byte Facepaint;
		public byte FacepaintColor;
	}
}
