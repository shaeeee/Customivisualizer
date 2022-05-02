﻿using Dalamud.Game.ClientState.Objects.Enums;
using System.Runtime.InteropServices;

namespace Customivisualizer
{
	[StructLayout(LayoutKind.Explicit)]
	public struct CharaCustomizeData
	{
		[FieldOffset((int)CustomizeIndex.Race)] public byte Race;
		[FieldOffset((int)CustomizeIndex.Gender)] public byte Gender;
		[FieldOffset((int)CustomizeIndex.Tribe)] public byte Tribe;
		[FieldOffset((int)CustomizeIndex.ModelType)] public byte ModelType;
		[FieldOffset((int)CustomizeIndex.FaceType)] public byte FaceType;
		[FieldOffset((int)CustomizeIndex.HairStyle)] public byte HairStyle;
		[FieldOffset((int)CustomizeIndex.HasHighlights)] public byte HasHighlights;
		[FieldOffset((int)CustomizeIndex.SkinColor)] public byte SkinColor;
		[FieldOffset((int)CustomizeIndex.EyeColor)] public byte EyeColor;
		[FieldOffset((int)CustomizeIndex.HairColor)] public byte HairColor;
		[FieldOffset((int)CustomizeIndex.HairColor2)] public byte HairColor2;
		[FieldOffset((int)CustomizeIndex.FaceFeatures)] public byte FaceFeatures;
		[FieldOffset((int)CustomizeIndex.FaceFeaturesColor)] public byte FaceFeaturesColor;
		[FieldOffset((int)CustomizeIndex.Eyebrows)] public byte Eyebrows;
		[FieldOffset((int)CustomizeIndex.EyeColor2)] public byte EyeColor2;
		[FieldOffset((int)CustomizeIndex.EyeShape)] public byte EyeShape;
		[FieldOffset((int)CustomizeIndex.NoseShape)] public byte NoseShape;
		[FieldOffset((int)CustomizeIndex.JawShape)] public byte JawShape;
		[FieldOffset((int)CustomizeIndex.LipStyle)] public byte LipStyle;
		[FieldOffset((int)CustomizeIndex.LipColor)] public byte LipColor;
		[FieldOffset((int)CustomizeIndex.RaceFeatureSize)] public byte RaceFeatureSize;
		[FieldOffset((int)CustomizeIndex.RaceFeatureType)] public byte RaceFeatureType;
		[FieldOffset((int)CustomizeIndex.BustSize)] public byte BustSize;
		[FieldOffset((int)CustomizeIndex.Facepaint)] public byte Facepaint;
		[FieldOffset((int)CustomizeIndex.FacepaintColor)] public byte FacepaintColor;
	}
}