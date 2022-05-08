using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Logging;
using System;
using System.Runtime.InteropServices;

namespace Customivisualizer
{
	public class Overrider : IDisposable
	{
		public const int CUSTOMIZE_OFFSET = 0x830;

		private Framework framework;
		private ClientState clientState;
		private CharaDataOverride charaDataOverride;

		public bool Enabled { get; private set; }

		internal Overrider(Framework framework, ClientState clientState, CharaDataOverride charaDataOverride)
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

		private unsafe void Override(Framework? framework = null)
		{
			if (clientState.LocalPlayer == null) return;
			Marshal.StructureToPtr(charaDataOverride.CustomizeData, clientState.LocalPlayer.Address + CUSTOMIZE_OFFSET, false);
		}
	}
}
