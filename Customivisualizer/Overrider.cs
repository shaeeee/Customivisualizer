using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Logging;
using System;
using System.Runtime.InteropServices;

namespace Customivisualizer
{
	public class Overrider : IDisposable
	{
		private Framework framework;
		private ClientState clientState;
		private Configuration configuration;
		private CharaCustomizeOverride charaCustomizeOverride;
		private CharaEquipSlotOverride charaEquipSlotOverride;

		public bool Enabled { get; private set; }

		internal Overrider(Framework framework, ClientState clientState, Configuration configuration, CharaCustomizeOverride charaDataOverride, CharaEquipSlotOverride charaEquipSlotOverride)
		{
			this.framework = framework;
			this.clientState = clientState;
			this.configuration = configuration;
			this.charaCustomizeOverride = charaDataOverride;
			this.charaEquipSlotOverride = charaEquipSlotOverride;
		}

		public void Enable()
		{
			if (Enabled) return;
			PluginLog.LogDebug("Overrider enabled");
			this.framework.Update += Override;
			Enabled = true;
		}

		public void Disable()
		{
			if (!Enabled) return;
			PluginLog.LogDebug("Overrider disabled");
			this.framework.Update -= Override;
			Enabled = false;
		}

		public void Dispose()
		{
			Disable();
			GC.SuppressFinalize(this);
		}

		public void ApplyCustom<T>(Override<T> charaOverride) where T : struct
		{
			if (clientState.LocalPlayer == null) return;
			Marshal.StructureToPtr(charaCustomizeOverride.CustomData, clientState.LocalPlayer.Address + charaOverride.Offset, false);
		}

		public void ApplyOriginal<T>(Override<T> charaOverride) where T : struct
		{
			if (clientState.LocalPlayer == null) return;
			PluginLog.LogDebug($"Applied original {typeof(T)} data");
			Marshal.StructureToPtr(charaOverride.OriginalData, clientState.LocalPlayer.Address + charaOverride.Offset, false);
		}

		private unsafe void Override(Framework? framework = null)
		{
			if (charaCustomizeOverride.Dirty || charaEquipSlotOverride.Dirty)
			{
				
			}
			if (this.configuration.ToggleCustomization) ApplyCustom(charaCustomizeOverride);
			if (this.configuration.ToggleEquipSlots) ApplyCustom(charaEquipSlotOverride);
		}
	}
}
