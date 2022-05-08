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
		// Customize array is 28 long, but the last 2 are not indexed by CustomizeIndex and thus not used here
		private const int RELEVANT_INDICES = 26;

        private Configuration configuration;
		private UIHelper uiHelper;
		private ClientState clientState;
		private CharaDataOverride charaDataOverride;

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return settingsVisible; }
            set { settingsVisible = value; }
        }

		private int[] newCustomizeDataInt = new int[RELEVANT_INDICES];

		private byte[] newCustomizeData = new byte[26];

		public bool ShowInvalidDataWarning { get; set; }

		public PluginUI(Configuration configuration, UIHelper uiHelper, ClientState clientState, CharaDataOverride charaDataOverride)
        {
            this.configuration = configuration;
			this.uiHelper = uiHelper;
			this.clientState = clientState;
			this.charaDataOverride = charaDataOverride;

			// Init custom appearance values
			if (!InitializeSaved())
			{
				InitializeDefaults();
			}
			charaDataOverride.ManualInvokeDataChanged();
		}

		private bool InitializeSaved()
		{
			if (configuration.CustomizationData != null)
			{
				Array.Copy(configuration.CustomizationData, newCustomizeDataInt, RELEVANT_INDICES);
				Array.Copy(configuration.CustomizationData, newCustomizeData, RELEVANT_INDICES);
				charaDataOverride.ApplyCustomizeData(newCustomizeData);
				PluginLog.Debug($"Loaded config {string.Join(", ", newCustomizeData)}");
				return true;
			}
			return false;
		}

		private void InitializeDefaults()
		{
			if (clientState.LocalPlayer == null) return;
			Array.Copy(clientState.LocalPlayer.Customize, newCustomizeDataInt, RELEVANT_INDICES);
			Array.Copy(clientState.LocalPlayer.Customize, newCustomizeData, RELEVANT_INDICES);
		}

		private void SaveAppearance()
		{
			configuration.CustomizationData = new byte[RELEVANT_INDICES];
			Array.Copy(newCustomizeData, configuration.CustomizationData, RELEVANT_INDICES);
			charaDataOverride.ApplyCustomizeData(newCustomizeData);
			configuration.Save();
			PluginLog.Debug($"Saved config {string.Join(", ", configuration.CustomizationData)}");
		}

		private void ResetAppearance()
		{
			configuration.CustomizationData = null;
			configuration.Save();
			InitializeDefaults();
			charaDataOverride.ApplyCustomizeData(newCustomizeData);
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
				if (ShowInvalidDataWarning)
				{
					ImGui.Text($"WARNING: Invalid data detected, change back what you just did, or reset appearance!");
				}

				if (ImGui.Combo($"Override Mode", ref overrideMode, Enum.GetNames<Configuration.Override>(), 2))
				{
					configuration.OverrideMode = (Configuration.Override)overrideMode;
					configuration.Save();
					if (configuration.ToggleCustomization) charaDataOverride.ManualInvokeDataChanged();
				}
				if (ImGui.IsItemHovered())
				{
					ImGui.BeginTooltip();
					ImGui.SetTooltip($"SOFT: Instant ON/OFF, doesn't work in cutscenes/GPose/equipment view.\nHARD: Requires entering new zone to remove changes, works in cutscenes/GPose/equipment view.");
					ImGui.EndTooltip();
				}

				if (ImGui.Checkbox($"Enable custom appearance", ref toggleCustomization))
				{
					configuration.ToggleCustomization = toggleCustomization;
					configuration.Save();
					charaDataOverride.ManualInvokeDataChanged();
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

				if (showCustomize)
				{
					ImGui.BeginTable("t0", configuration.OverrideMode == Configuration.Override.HARD ? 2 : 1, ImGuiTableFlags.SizingStretchProp);
					ImGui.TableNextRow();
					ImGui.TableNextColumn();
					DrawCustomizationOptions();
					if (configuration.OverrideMode == Configuration.Override.HARD)
					{
						ImGui.TableNextColumn();
						DrawExtendedOptions();
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

		private void DrawCustomizationOptions()
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

			for (int i = 0; i < RELEVANT_INDICES; i++)
			{
				ImGui.TableNextRow();
				ImGui.TableNextColumn();
				ImGui.Text($"{Enum.GetName(typeof(CustomizeIndex), i) ?? "Unknown"} = {clientState?.LocalPlayer?.Customize[i]}");
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

		private void DrawExtendedOptions()
		{
			ImGui.Text("This section is a work in progress");
		}
    }
}
