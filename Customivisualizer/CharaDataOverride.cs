using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Customivisualizer
{
	public class CharaDataOverride
	{
		public event EventHandler? DataChanged;

		public CharaCustomizeData CustomizeData { get; private set; }

		public void ApplyCustomizeData(byte[] customizeData)
		{
			CustomizeData = CustomizeArrayToStruct(customizeData);
			OnDataChanged();
		}

		public void ManualInvokeDataChanged()
		{
			OnDataChanged();
		}

		private void OnDataChanged()
		{
			DataChanged?.Invoke(this, EventArgs.Empty);
		}

		private static CharaCustomizeData CustomizeArrayToStruct(byte[] customizeArray)
		{
			GCHandle handle = GCHandle.Alloc(customizeArray, GCHandleType.Pinned);
			CharaCustomizeData customizeData;
			try
			{
				customizeData = Marshal.PtrToStructure<CharaCustomizeData>(handle.AddrOfPinnedObject());
			}
			finally
			{
				handle.Free();
			}
			return customizeData;
		}

		public void ChangeCustomizeData(IntPtr customizeDataPtr)
		{
			var customData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);

			customData.Race = CustomizeData.Race;
			customData.Gender = CustomizeData.Gender;
			customData.Tribe = CustomizeData.Tribe;
			// Constrain body type to 0-1 so we don't crash the game
			customData.ModelType = (byte)(CustomizeData.ModelType % 2);
			customData.FaceType = CustomizeData.FaceType;
			customData.HairStyle = CustomizeData.HairStyle;
			customData.HasHighlights = CustomizeData.HasHighlights;
			customData.SkinColor = CustomizeData.SkinColor;
			customData.EyeColor = CustomizeData.EyeColor;
			customData.HairColor = CustomizeData.HairColor;
			customData.HairColor2 = CustomizeData.HairColor2;
			customData.FaceFeatures = CustomizeData.FaceFeatures;
			customData.FaceFeaturesColor = CustomizeData.FaceFeaturesColor;
			customData.Eyebrows = CustomizeData.Eyebrows;
			customData.EyeColor2 = CustomizeData.EyeColor2;
			customData.EyeShape = CustomizeData.EyeShape;
			customData.NoseShape = CustomizeData.NoseShape;
			customData.JawShape = CustomizeData.JawShape;
			customData.LipStyle = CustomizeData.LipStyle;
			customData.LipColor = CustomizeData.LipColor;
			customData.RaceFeatureSize = CustomizeData.RaceFeatureSize;
			customData.RaceFeatureType = CustomizeData.RaceFeatureType;
			customData.BustSize = CustomizeData.BustSize;
			customData.Facepaint = CustomizeData.Facepaint;
			customData.FacepaintColor = CustomizeData.FacepaintColor;

			Marshal.StructureToPtr(customData, customizeDataPtr, true);
		}
	}
}
