using Dalamud.Logging;
using Dalamud.Game.ClientState.Objects.Enums;
using ImGuiNET;
using System;
using Dalamud.Game.ClientState;

namespace Customivisualizer
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return settingsVisible; }
            set {
				if (!settingsVisible && value) InitializeData();
				settingsVisible = value;
			}
        }

        private readonly Configuration configuration;
		private readonly UIHelper uiHelper;
		private readonly ClientState clientState;
		private readonly CharaCustomizeOverride charaCustomizeOverride;
		private readonly CharaEquipSlotOverride charaEquipSlotOverride;

		private byte[] newCustomizeData;
		private byte[] newEquipSlotData;

		private int[] equipSearchFields;

		private Dalamud.Game.ClientState.Objects.SubKinds.PlayerCharacter? player;

		public PluginUI(
			Configuration configuration,
			UIHelper uiHelper,
			ClientState clientState,
			CharaCustomizeOverride charaDataOverride,
			CharaEquipSlotOverride charaEquipSlotOverride)
        {
            this.configuration = configuration;
			this.uiHelper = uiHelper;
			this.clientState = clientState;
			this.charaCustomizeOverride = charaDataOverride;
			this.charaEquipSlotOverride = charaEquipSlotOverride;

			newCustomizeData = new byte[CharaCustomizeOverride.SIZE];
			newEquipSlotData = new byte[CharaEquipSlotOverride.SIZE];

			equipSearchFields = new int[CharaEquipSlotOverride.SLOTS];

			// Init custom appearance values
			InitializeData();
		}

		private void UpdatePlayer()
		{
			player = clientState.LocalPlayer;
		}

		#region Init, Save, Reset

		private void InitializeData()
		{
			if (!InitializeCustomizeData()) InitializeCustomizeDefaults();
			if (!InitializeEquipSlotData()) InitializeEquipSlotDefaults();
		}

		private bool InitializeCustomizeData()
		{
			if (configuration.CustomizationData != null)
			{
				Array.Copy(configuration.CustomizationData, newCustomizeData, CharaCustomizeOverride.SIZE);
				charaCustomizeOverride.Apply(newCustomizeData);
				return true;
			}
			return false;
		}

		private bool InitializeEquipSlotData()
		{
			if (configuration.EquipSlotData != null)
			{
				Array.Copy(configuration.EquipSlotData, newEquipSlotData, CharaEquipSlotOverride.SIZE);
				charaEquipSlotOverride.Apply(newEquipSlotData);
				return true;
			}
			return false;
		}

		private void InitializeCustomizeDefaults()
		{
			var player = clientState.LocalPlayer;
			if (player == null) return;
			Array.Copy(player.Customize, newCustomizeData, CharaCustomizeOverride.SIZE);
		}

		private void InitializeEquipSlotDefaults()
		{
			var player = clientState.LocalPlayer;
			Array.Copy(CharaEquipSlotOverride.GetEquipSlotValues(player), newEquipSlotData, CharaEquipSlotOverride.SIZE);
		}

		private void SaveCustomizeData()
		{
			configuration.CustomizationData = new byte[CharaCustomizeOverride.SIZE];
			Array.Copy(newCustomizeData, configuration.CustomizationData, CharaCustomizeOverride.SIZE);
			charaCustomizeOverride.Apply(newCustomizeData, configuration.ToggleCustomization);
			configuration.Save();
		}

		private void SaveEquipSlotData()
		{
			configuration.EquipSlotData = new byte[CharaEquipSlotOverride.SIZE];
			Array.Copy(newEquipSlotData, configuration.EquipSlotData, CharaEquipSlotOverride.SIZE);
			charaEquipSlotOverride.Apply(newEquipSlotData, configuration.ToggleEquipSlots);
			configuration.Save();
		}

		private void ResetCustomizeData()
		{
			configuration.CustomizationData = null;
			configuration.Save();
			InitializeCustomizeDefaults();
			charaCustomizeOverride.Apply(newCustomizeData);
		}

		private void ResetEquipSlotData()
		{
			configuration.EquipSlotData = null;
			configuration.Save();
			InitializeEquipSlotDefaults();
			charaEquipSlotOverride.Apply(newEquipSlotData);
		}

		#endregion

		public void Dispose()
        {
            
        }

        public void Draw()
        {
			// This is our only draw handler attached to UIBuilder, so it needs to be
			// able to draw any windows we might have open.
			// Each method checks its own visibility/state to ensure it only draws when
			// it actually makes sense.
			// There are other ways to do this, but it is generally best to keep the number of
			// draw delegates as low as possible.
			UpdatePlayer();
            DrawSettingsWindow();
        }

		public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

			var overrideMode = (int)configuration.OverrideMode;
			var alwaysReload = configuration.AlwaysReload;
			var showCustomize = configuration.ShowCustomize;
			var showEquipSlots = configuration.ShowEquipSlots;

            if (ImGui.Begin("Customivisualizer Config", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
				if (ImGui.Combo($"Override Mode", ref overrideMode, Enum.GetNames<Configuration.Override>(), 3))
				{
					configuration.OverrideMode = (Configuration.Override)overrideMode;
					configuration.Save();
					if (configuration.ToggleCustomization) charaCustomizeOverride.Dirty = true;
				}
				if (ImGui.IsItemHovered())
				{
					ImGui.BeginTooltip();
					ImGui.SetTooltip($"" +
						$"MEM_EDIT: Instant ON/OFF. May break with game updates.\n" +
						$"CLASSIC: Instant ON/OFF. Doesn't work in cutscenes/GPose/equipment view. Least likely to break with game updates.\n" +
						$"HOOK_LOAD: Requires entering new zone to reflect changes. Less likely to break with game updates.\n" +
						$"If you dont know which to pick, use MEM_EDIT.");
					ImGui.EndTooltip();
				}

				if (ImGui.Checkbox($"Always refresh character", ref alwaysReload))
				{
					if (ImGui.IsItemHovered())
					{
						ImGui.BeginTooltip();
						ImGui.SetTooltip($"Should character be redrawn as soon as you change a value?");
						ImGui.EndTooltip();
					}
					configuration.AlwaysReload = alwaysReload;
					configuration.Save();
				}

				if (ImGui.Checkbox($"Show data editor", ref showCustomize))
				{
					configuration.ShowCustomize = showCustomize;
					configuration.Save();
				}
				ImGui.SameLine();
				if (this.configuration.OverrideMode == Configuration.Override.HOOK_LOAD || configuration.OverrideMode == Configuration.Override.MEM_EDIT) {
					if (ImGui.Checkbox($"Show equipment editor", ref showEquipSlots))
					{
						configuration.ShowEquipSlots = showEquipSlots;
						configuration.Save();
					}
				}
				
				ImGui.Spacing();
				ImGui.Spacing();

				if (showCustomize)
				{	
					showEquipSlots = configuration.ShowEquipSlots && (configuration.OverrideMode == Configuration.Override.HOOK_LOAD || configuration.OverrideMode == Configuration.Override.MEM_EDIT);
					ImGui.BeginTable("t0", showEquipSlots ? 2 : 1, ImGuiTableFlags.SizingStretchProp);
					ImGui.TableSetupColumn("c01", ImGuiTableColumnFlags.WidthFixed, 430);
					ImGui.TableSetupColumn("c02", ImGuiTableColumnFlags.WidthFixed, 480);
					ImGui.TableNextRow();
					ImGui.TableNextColumn();
					ImGui.Text($"[Appearance Data]");

					if (showEquipSlots)
					{
						ImGui.TableNextColumn();
						ImGui.Text($"[Equip Slot Data] (WORK IN PROGRESS)");
					}

					ImGui.Spacing();
					ImGui.TableNextRow();
					ImGui.TableSetColumnIndex(0);
					
					DrawCustomizeOptions();
					
					if (showEquipSlots)
					{
						ImGui.TableNextColumn();
						DrawEquipSlotOptions();
					}
					
					ImGui.EndTable();
					ImGui.Spacing();
				}
			}

			ImGui.End();
        }

		private void DrawCustomizeOptions()
		{
			var toggleCustomization = configuration.ToggleCustomization;
			if (ImGui.Checkbox($"Enable appearance override", ref toggleCustomization))
			{
				configuration.ToggleCustomization = toggleCustomization;
				configuration.Save();
				charaCustomizeOverride.Dirty = true;
			};

			var raceAndTribe = uiHelper.GetRaceAndTribe(newCustomizeData);

			ImGui.BeginTable("t1", 4, ImGuiTableFlags.SizingStretchProp);
			ImGui.TableSetupColumn("c0", ImGuiTableColumnFlags.WidthFixed, 130);
			ImGui.TableSetupColumn("c0.5", ImGuiTableColumnFlags.WidthFixed, 50);
			ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, 100);
			ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthFixed, 150);

			bool performReload = false;

			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text($"Feature");
			ImGui.TableNextColumn();
			ImGui.Text($"Memory");
			ImGui.TableNextColumn();
			ImGui.Text($"Custom values");
			ImGui.TableNextColumn();
			ImGui.Text($"Description");

			for (int i = 0; i < CharaCustomizeOverride.SIZE; i++)
			{
				var input = (int)newCustomizeData[i];

				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Text($"{Enum.GetName(typeof(CustomizeIndex), i) ?? "Unknown"}");
				ImGui.TableNextColumn();
				ImGui.Text($"{player?.Customize[i]}");
				ImGui.SameLine();
				ImGui.TableNextColumn();
				// For special cases
				switch ((CustomizeIndex)i)
				{
					case CustomizeIndex.Race:
						ImGui.PushItemWidth(-1);
						if (ImGui.InputInt($"{i}", ref input) && configuration.AlwaysReload) performReload = true;
						ImGui.PopItemWidth();
						ImGui.TableNextColumn();
						ImGui.Text($"{raceAndTribe?[0]}");
						newCustomizeData[i] = (byte)input;
						break;

					case CustomizeIndex.Gender:
						UIHelper.AdjustGender(newCustomizeData[(int)CustomizeIndex.Race], ref input);
						ImGui.PushItemWidth(-1);
						if (ImGui.InputInt($"{i}", ref input) && configuration.AlwaysReload) performReload = true;
						ImGui.PopItemWidth();
						ImGui.TableNextColumn();
						ImGui.Text(input == 0 ? "Masculine" : "Feminine");
						newCustomizeData[i] = (byte)input;
						break;

					case CustomizeIndex.Tribe:
						UIHelper.AdjustTribe(newCustomizeData[(int)CustomizeIndex.Race], ref input);
						ImGui.PushItemWidth(-1);
						if (ImGui.InputInt($"{i}", ref input) && configuration.AlwaysReload) performReload = true;
						ImGui.PopItemWidth();
						ImGui.TableNextColumn();
						ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthFixed, 300);
						ImGui.Text($"{raceAndTribe?[1]}");
						newCustomizeData[i] = (byte)input;
						break;

					default:
						ImGui.PushItemWidth(-1);
						if (ImGui.InputInt($"{i}", ref input) && configuration.AlwaysReload) performReload = true;
						ImGui.PopItemWidth();
						newCustomizeData[i] = (byte)input;
						break;
				}
			}

			if (performReload)
			{
				SaveCustomizeData();
			}

			ImGui.EndTable();

			if (ImGui.Button($"Update Appearance"))
			{
				SaveCustomizeData();
			}
			ImGui.SameLine();
			if (ImGui.Button($"Reset Appearance"))
			{
				ResetCustomizeData();
			}
		}

		private void DrawEquipSlotOptions()
		{
			var toggleEquipSlots = this.configuration.ToggleEquipSlots;
			if (ImGui.Checkbox($"Enable equipment override", ref toggleEquipSlots))
			{
				configuration.ToggleEquipSlots = toggleEquipSlots;
				configuration.Save();
				charaEquipSlotOverride.Dirty = true;
			};

			ImGui.BeginTable("t2", 6, ImGuiTableFlags.SizingStretchProp);
			ImGui.TableSetupColumn("c3", ImGuiTableColumnFlags.WidthFixed, 50);
			ImGui.TableSetupColumn("c3.1", ImGuiTableColumnFlags.WidthFixed, 45);
			ImGui.TableSetupColumn("c3.2", ImGuiTableColumnFlags.WidthFixed, 45);
			ImGui.TableSetupColumn("c3.3", ImGuiTableColumnFlags.WidthFixed, 45);
			ImGui.TableSetupColumn("c4", ImGuiTableColumnFlags.WidthFixed, 150);
			ImGui.TableSetupColumn("c5", ImGuiTableColumnFlags.WidthFixed, 120);

			bool performReload = false;

			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text($"Slot");
			ImGui.TableNextColumn();
			ImGui.Text($"ID");
			ImGui.TableNextColumn();
			ImGui.Text($"Variant");
			ImGui.TableNextColumn();
			ImGui.Text($"Dye");
			ImGui.TableNextColumn();
			ImGui.Text($"Custom data");
			ImGui.TableNextColumn();
			ImGui.Text($"Item ID search");

			byte[] equipSlots = CharaEquipSlotOverride.GetEquipSlotValues(player);
			var slotBytes = 4;
			for (int i = 0; i < CharaEquipSlotOverride.SLOTS; i++)
			{
				var slot = i * slotBytes;
				var modelId = BitConverter.ToUInt16(equipSlots, slot);
				var item = EquipmentHelper.BytesToItem(newEquipSlotData, slot);
				var itemId = equipSearchFields[i];

				ImGui.TableNextRow();
				
				ImGui.TableNextColumn();
				ImGui.Text($"{Enum.GetName(typeof(EquipSlotIndex), slot)}");
				ImGui.TableNextColumn();
				ImGui.Text($"{modelId}");
				ImGui.TableNextColumn();
				ImGui.Text($"{equipSlots[slot+2]}");
				ImGui.TableNextColumn();
				ImGui.Text($"{equipSlots[slot+3]}");
				ImGui.TableNextColumn();
				ImGui.PushItemWidth(-1); 
				if (ImGui.InputInt3($"##label {i}z", ref item[0]) && configuration.AlwaysReload) performReload = true;
				ImGui.PopItemWidth();
				ImGui.TableNextColumn();

				var bytes = EquipmentHelper.ItemToBytes(item);

				ImGui.InputInt($"##label {i}z", ref itemId, 0, 1);
				ImGui.SameLine(80);
				if (ImGui.Button($"Go ##label {i}w"))
				{
					if (configuration.AlwaysReload) performReload = true;
					bytes = EquipmentHelper.GetItem((uint)itemId);
				}

				newEquipSlotData[slot] = bytes[0];
				newEquipSlotData[slot+1] = bytes[1];
				newEquipSlotData[slot+2] = bytes[2];
				newEquipSlotData[slot+3] = bytes[3];

				equipSearchFields[i] = itemId;
			}

			if (performReload)
			{
				SaveEquipSlotData();
			}

			ImGui.EndTable();

			if (ImGui.Button($"Update Equipment"))
			{
				SaveEquipSlotData();
			}
			ImGui.SameLine();
			if (ImGui.Button($"Reset Equipment"))
			{
				ResetEquipSlotData();
			}
		}
    }
}
