using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Logging;
using System;
using System.Runtime.InteropServices;

namespace Customivisualizer
{
	public class Overrider : IDisposable
	{
		private const int CUSTOMIZE_OFFSET = 0x830;
		private const int EQUIPSLOT_OFFSET = 0x808;

		private Framework framework;
		private ClientState clientState;
		private CharaCustomizeOverride charaDataOverride;
		//private CharaEquipSlotOverride charaEquipSlotOverride;

		public bool Enabled { get; private set; }

		internal Overrider(Framework framework, ClientState clientState, CharaCustomizeOverride charaDataOverride)
		{
			this.framework = framework;
			this.clientState = clientState;
			this.charaDataOverride = charaDataOverride;
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

		public void Apply()
		{
			Override(null);
		}

		public void ApplyOriginal()
		{
			if (clientState.LocalPlayer == null) return;
			PluginLog.LogDebug("Applied original");
			Marshal.StructureToPtr(charaDataOverride.OriginalData, clientState.LocalPlayer.Address + CUSTOMIZE_OFFSET, false);
			//Marshal.StructureToPtr(charaEquipSlotOverride.OriginalData, clientState.LocalPlayer.Address + EQUIPSLOT_OFFSET, false);
		}

		private unsafe void Override(Framework? framework = null)
		{
			if (clientState.LocalPlayer == null) return;
			Marshal.StructureToPtr(charaDataOverride.CustomData, clientState.LocalPlayer.Address + CUSTOMIZE_OFFSET, false);
			//Marshal.StructureToPtr(charaEquipSlotOverride.CustomData, clientState.LocalPlayer.Address + EQUIPSLOT_OFFSET, false);
		}
	}
}
