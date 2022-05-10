using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Logging;
using System;
using System.Runtime.InteropServices;

namespace Customivisualizer
{
	public class Overrider<T,L> : IDisposable where T : Override<L> where L : struct
	{
		private Framework framework;
		private ClientState clientState;
		private Configuration configuration;
		private Override<L> overrideData;

		public bool Enabled { get; set; }

		internal Overrider(Framework framework, ClientState clientState, Configuration configuration, Override<L> overrideData)
		{
			this.framework = framework;
			this.clientState = clientState;
			this.configuration = configuration;
			this.overrideData = overrideData;

			this.framework.Update += Override;
		}

		public void Dispose()
		{
			this.framework.Update -= Override;
			GC.SuppressFinalize(this);
		}

		private void ApplyCustom()
		{
			if (clientState.LocalPlayer == null) return;
			Marshal.StructureToPtr(overrideData.CustomData, clientState.LocalPlayer.Address + overrideData.Offset, false);
		}

		public void ApplyOriginal()
		{
			if (clientState.LocalPlayer == null || !overrideData.HasOriginalData) return;
			PluginLog.LogDebug($"Applied original {typeof(L)} data");
			Marshal.StructureToPtr(overrideData.OriginalData, clientState.LocalPlayer.Address + overrideData.Offset, false);
		}

		private unsafe void Override(Framework? framework = null)
		{
			if (Enabled && !overrideData.Dirty && this.configuration.OverrideMode == Configuration.Override.MEM_EDIT) ApplyCustom();
		}
	}
}
