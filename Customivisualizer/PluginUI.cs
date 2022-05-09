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
		
		private int[] newCustomizeDataInt;
		private int[] newEquipSlotDataInt;

		private byte[] newCustomizeData;
		private byte[] newEquipSlotData;

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

			newCustomizeDataInt = new int[CharaCustomizeOverride.SIZE];
			newEquipSlotDataInt = new int[CharaEquipSlotOverride.SIZE];

			newCustomizeData = new byte[CharaCustomizeOverride.SIZE];
			newEquipSlotData = new byte[CharaEquipSlotOverride.SIZE];

			// Init custom appearance values
			InitializeData();
			charaDataOverride.ManualInvokeDataChanged();
		}

		private void UpdatePlayer()
		{
			player = clientState.LocalPlayer;
		}

		private void InitializeData()
		{
			if (!InitializeCustomizeData()) InitializeCustomizeDefaults();
			if (!InitializeEquipSlotData()) InitializeEquipSlotDefaults();
		}

		private bool InitializeCustomizeData()
		{
			if (configuration.CustomizationData != null)
			{
				Array.Copy(configuration.CustomizationData, newCustomizeDataInt, CharaCustomizeOverride.SIZE);
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
				Array.Copy(configuration.EquipSlotData, newEquipSlotDataInt, CharaEquipSlotOverride.SIZE);
				Array.Copy(configuration.EquipSlotData, newEquipSlotData, CharaEquipSlotOverride.SIZE);
				charaEquipSlotOverride.Apply(newEquipSlotData);
				return true;
			}
			return false;
		}

		private void InitializeDefaults()
		{
			InitializeCustomizeDefaults();
			InitializeEquipSlotDefaults();
		}

		private void InitializeCustomizeDefaults()
		{
			var player = clientState.LocalPlayer;
			if (player == null) return;
			Array.Copy(player.Customize, newCustomizeDataInt, CharaCustomizeOverride.SIZE);
			Array.Copy(player.Customize, newCustomizeData, CharaCustomizeOverride.SIZE);
		}

		private void InitializeEquipSlotDefaults()
		{
			var player = clientState.LocalPlayer;
			Array.Copy(uiHelper.GetEquipSlotValues(player), newEquipSlotDataInt, CharaEquipSlotOverride.SIZE);
			Array.Copy(uiHelper.GetEquipSlotValues(player), newEquipSlotData, CharaEquipSlotOverride.SIZE);
		}

		private void SaveAppearance()
		{
			configuration.CustomizationData = new byte[CharaCustomizeOverride.SIZE];
			Array.Copy(newCustomizeData, configuration.CustomizationData, CharaCustomizeOverride.SIZE);
			charaCustomizeOverride.Apply(newCustomizeData);
			configuration.Save();
		}

		private void ResetAppearance()
		{
			configuration.CustomizationData = null;
			configuration.Save();
			InitializeDefaults();
			charaCustomizeOverride.Apply(newCustomizeData);
		}

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
			var toggleCustomization = configuration.ToggleCustomization;
			var alwaysReload = configuration.AlwaysReload;
			var showCustomize = configuration.ShowCustomize;

            if (ImGui.Begin("Config", ref settingsVisible, ImGuiWindowFlags.AlwaysAutoResize))
            {
				if (ImGui.Combo($"Override Mode", ref overrideMode, Enum.GetNames<Configuration.Override>(), 3))
				{
					configuration.OverrideMode = (Configuration.Override)overrideMode;
					configuration.Save();
					if (configuration.ToggleCustomization) charaCustomizeOverride.ManualInvokeDataChanged();
				}
				if (ImGui.IsItemHovered())
				{
					ImGui.BeginTooltip();
					ImGui.SetTooltip($"" +
						$"MEM_EDIT: Instant ON/OFF. May break with game updates.\n" +
						$"CLASSIC: Instant ON/OFF. Doesn't work in cutscenes/GPose/equipment view. Least likely to break with game updates.\n" +
						$"HOOK_LOAD: Requires entering new zone to reflect changes. Less likely to break with game updates.");
					ImGui.EndTooltip();
				}

				if (ImGui.Checkbox($"Enable custom appearance", ref toggleCustomization))
				{
					configuration.ToggleCustomization = toggleCustomization;
					configuration.Save();
					charaCustomizeOverride.ManualInvokeDataChanged();
				};

				if (ImGui.Checkbox($"Always update appearance", ref alwaysReload))
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

				if (ImGui.Checkbox($"Show Customize array", ref showCustomize))
				{
					configuration.ShowCustomize = showCustomize;
					configuration.Save();
				}
				
				ImGui.Spacing();
				ImGui.Spacing();

				if (showCustomize)
				{	
					bool showEquipSlots = configuration.OverrideMode == Configuration.Override.HOOK_LOAD || configuration.OverrideMode == Configuration.Override.MEM_EDIT;
					ImGui.BeginTable("t0", showEquipSlots ? 2 : 1, ImGuiTableFlags.SizingStretchProp);

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
				if (ImGui.Button($"Update Appearance"))
				{
					SaveAppearance();
				}
				ImGui.SameLine();
				if (ImGui.Button($"Reset Appearance"))
				{
					ResetAppearance();
				}
			}

			ImGui.End();
        }

		private void DrawCustomizeOptions()
		{
			var raceAndTribe = uiHelper.GetRaceAndTribe(newCustomizeData);

			ImGui.BeginTable("t1", 3, ImGuiTableFlags.SizingStretchProp);
			ImGui.TableSetupColumn("c0", ImGuiTableColumnFlags.WidthFixed, 150);
			ImGui.TableSetupColumn("c1", ImGuiTableColumnFlags.WidthFixed, 100);
			ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthFixed, 150);

			bool performReload = false;

			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text($"Memory values");
			ImGui.TableNextColumn();
			ImGui.Text($"Custom values");
			ImGui.TableNextColumn();
			ImGui.Text($"Description");

			for (int i = 0; i < CharaCustomizeOverride.SIZE; i++)
			{
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Text($"{Enum.GetName(typeof(CustomizeIndex), i) ?? "Unknown"} = {player?.Customize[i]}");
				ImGui.SameLine();
				ImGui.TableNextColumn();
				// For special cases
				switch ((CustomizeIndex)i)
				{
					case CustomizeIndex.Race:
						ImGui.PushItemWidth(-1);
						if (ImGui.InputInt($"{i}", ref newCustomizeDataInt[i]) && configuration.AlwaysReload) performReload = true;
						ImGui.PopItemWidth();
						ImGui.TableNextColumn();
						ImGui.Text($"{raceAndTribe?[0]}");
						newCustomizeData[i] = (byte)newCustomizeDataInt[i];
						break;

					case CustomizeIndex.Gender:
						UIHelper.AdjustGender(newCustomizeDataInt[(int)CustomizeIndex.Race], ref newCustomizeDataInt[i]);
						ImGui.PushItemWidth(-1);
						if (ImGui.InputInt($"{i}", ref newCustomizeDataInt[i]) && configuration.AlwaysReload) performReload = true;
						ImGui.PopItemWidth();
						ImGui.TableNextColumn();
						ImGui.Text(newCustomizeDataInt[i] == 0 ? "Masculine" : "Feminine");
						newCustomizeData[i] = (byte)newCustomizeDataInt[i];
						break;

					case CustomizeIndex.Tribe:
						UIHelper.AdjustTribe(newCustomizeDataInt[(int)CustomizeIndex.Race], ref newCustomizeDataInt[i]);
						ImGui.PushItemWidth(-1);
						if (ImGui.InputInt($"{i}", ref newCustomizeDataInt[i]) && configuration.AlwaysReload) performReload = true;
						ImGui.PopItemWidth();
						ImGui.TableNextColumn();
						ImGui.TableSetupColumn("c2", ImGuiTableColumnFlags.WidthFixed, 300);
						ImGui.Text($"{raceAndTribe?[1]}");
						newCustomizeData[i] = (byte)newCustomizeDataInt[i];
						break;

					default:
						ImGui.PushItemWidth(-1);
						if (ImGui.InputInt($"{i}", ref newCustomizeDataInt[i]) && configuration.AlwaysReload) performReload = true;
						ImGui.PopItemWidth();
						newCustomizeData[i] = (byte)newCustomizeDataInt[i];
						break;
				}
			}

			if (performReload)
			{
				SaveAppearance();
			}

			ImGui.EndTable();
		}

		private void DrawEquipSlotOptions()
		{
			ImGui.BeginTable("t2", 3, ImGuiTableFlags.SizingStretchProp);
			ImGui.TableSetupColumn("c3", ImGuiTableColumnFlags.WidthFixed, 100);
			ImGui.TableSetupColumn("c4", ImGuiTableColumnFlags.WidthFixed, 100);
			ImGui.TableSetupColumn("c5", ImGuiTableColumnFlags.WidthFixed, 150);

			bool performReload = false;

			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.Text($"Memory values");
			ImGui.TableNextColumn();
			ImGui.Text($"Custom values");
			ImGui.TableNextColumn();
			ImGui.Text($"Description");

			byte[] equipSlots = uiHelper.GetEquipSlotValues(player);

			for (int i = 0; i < CharaEquipSlotOverride.SIZE; i++)
			{
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Text($"{Enum.GetName(typeof(EquipSlotIndex), i) ?? "Unknown"} = {equipSlots[i]}");
				ImGui.SameLine();
				ImGui.TableNextColumn();
				ImGui.PushItemWidth(-1);
				if (ImGui.InputInt($"{i}", ref newEquipSlotDataInt[i]) && configuration.AlwaysReload) performReload = true;
				ImGui.PopItemWidth();
				newEquipSlotData[i] = (byte)newEquipSlotDataInt[i];
			}

			if (performReload)
			{
				SaveAppearance();
			}

			ImGui.EndTable();
		}
    }
}
