using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Customivisualizer
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
		public enum Override
		{
			MEM_EDIT,
			CLASSIC,
			HOOK_LOAD
		}

        public int Version { get; set; } = 0;

        public bool ShowCustomize { get; set; }

		public bool ShowEquipSlots { get; set; }

		public bool ToggleCustomization { get; set; }

		public bool ToggleEquipSlots { get; set; }

		public bool AlwaysReload { get; set; }

		public bool UseItemIds { get; set; }

		public Override OverrideMode { get; set; }

		public byte[]? CustomizationData { get; set; }

		public byte[]? EquipSlotData { get; set; }

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
