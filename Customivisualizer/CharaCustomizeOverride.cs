using System;
using System.Runtime.InteropServices;

namespace Customivisualizer
{
	public class CharaCustomizeOverride : Override<CharaCustomizeData>
	{
		public const int SIZE = 26;

		public void ChangeCustomizeData(IntPtr customizeDataPtr)
		{
			var customData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);

			customData.Race = CustomData.Race;
			customData.Gender = CustomData.Gender;
			customData.Tribe = CustomData.Tribe;
			// Constrain body type to 0-1 so we don't crash the game
			customData.ModelType = (byte)(CustomData.ModelType % 2);
			customData.Height = CustomData.Height;
			customData.FaceType = CustomData.FaceType;
			customData.HairStyle = CustomData.HairStyle;
			customData.HasHighlights = CustomData.HasHighlights;
			customData.SkinColor = CustomData.SkinColor;
			customData.EyeColor = CustomData.EyeColor;
			customData.HairColor = CustomData.HairColor;
			customData.HairColor2 = CustomData.HairColor2;
			customData.FaceFeatures = CustomData.FaceFeatures;
			customData.FaceFeaturesColor = CustomData.FaceFeaturesColor;
			customData.Eyebrows = CustomData.Eyebrows;
			customData.EyeColor2 = CustomData.EyeColor2;
			customData.EyeShape = CustomData.EyeShape;
			customData.NoseShape = CustomData.NoseShape;
			customData.JawShape = CustomData.JawShape;
			customData.LipStyle = CustomData.LipStyle;
			customData.LipColor = CustomData.LipColor;
			customData.RaceFeatureSize = CustomData.RaceFeatureSize;
			customData.RaceFeatureType = CustomData.RaceFeatureType;
			customData.BustSize = CustomData.BustSize;
			customData.Facepaint = CustomData.Facepaint;
			customData.FacepaintColor = CustomData.FacepaintColor;

			Marshal.StructureToPtr(customData, customizeDataPtr, true);
		}
	}
}
