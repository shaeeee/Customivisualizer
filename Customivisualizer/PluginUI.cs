using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Customivisualizer
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
		// Customize array is 28 long, but the last 2 are not indexed by CustomizeIndex and thus not used here
		private const int RELEVANT_INDICES = 26;

        private Configuration configuration;
		private Plugin plugin;
		private Dalamud.Game.ClientState.ClientState clientState;

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

		public int[] NewCustomizeDataInt = new int[RELEVANT_INDICES];

		private byte[] newCustomizeData = new byte[RELEVANT_INDICES];

		public byte[] NewCustomizeData { get { return this.newCustomizeData; } private set { } }

		public PluginUI(Configuration configuration, Plugin plugin, Dalamud.Game.ClientState.ClientState clientState)
        {
            this.configuration = configuration;
			this.plugin = plugin;
			this.clientState = clientState;

			// Init custom appearance values
			if (!InitializeSaved())
			{
				InitializeDefaults();
			}
		}

		private bool InitializeSaved()
		{
			if (this.configuration.CustomizationData != null)
			{
				PluginLog.Debug($"Successfully loaded saved appearance configuration");
				Array.Copy(this.configuration.CustomizationData, NewCustomizeDataInt, RELEVANT_INDICES);
				Array.Copy(this.configuration.CustomizationData, NewCustomizeData, RELEVANT_INDICES);
				return true;
			}
			return false;
		}

		private void InitializeDefaults()
		{
			if (this.clientState.LocalPlayer == null) return;
			Array.Copy(this.clientState.LocalPlayer.Customize, NewCustomizeDataInt, RELEVANT_INDICES);
			Array.Copy(this.clientState.LocalPlayer.Customize, NewCustomizeData, RELEVANT_INDICES);
		}

		private void SaveAppearance()
		{
			this.configuration.CustomizationData = new byte[RELEVANT_INDICES];
			Array.Copy(newCustomizeData, this.configuration.CustomizationData, RELEVANT_INDICES);
			this.configuration.Save();
			plugin.UpdateCustomizeData();
		}

		private void ResetAppearance()
		{
			this.configuration.CustomizationData = null;
			this.configuration.Save();
			InitializeDefaults();
			plugin.UpdateCustomizeData();
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

			bool toggleCustomization = this.configuration.ToggleCustomization;
			bool showCustomize = this.configuration.ShowCustomize;

            if (ImGui.Begin("Config", ref this.settingsVisible))
            {
				if (ImGui.Checkbox("Enable custom appearance", ref toggleCustomization))
				{
					plugin.UpdateCustomizeData();
				};

				this.configuration.ToggleCustomization = toggleCustomization;
				this.configuration.Save();

				ImGui.Checkbox("Show Customize array", ref showCustomize);
				
				this.configuration.ShowCustomize = showCustomize;
				this.configuration.Save();

				ImGui.Spacing();

				if (showCustomize)
				{
					ImGui.BeginTable("t1", 2);
					
					for (int i = 0; i < RELEVANT_INDICES; i++)
					{
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.Text($"{Enum.GetName(typeof(Dalamud.Game.ClientState.Objects.Enums.CustomizeIndex), i) ?? "Unknown"} = {clientState?.LocalPlayer?.Customize[i]}");
						ImGui.SameLine();
						ImGui.TableNextColumn();
						ImGui.PushItemWidth(-1);
						ImGui.InputInt($"l{i}", ref NewCustomizeDataInt[i]);
						newCustomizeData[i] = (byte)NewCustomizeDataInt[i];
						ImGui.PopItemWidth();
					}
					ImGui.EndTable();
					ImGui.Spacing();
				}
				if (ImGui.Button("Save Appearance"))
				{
					SaveAppearance();
				}
				ImGui.SameLine();
				if (ImGui.Button("Reset Appearance"))
				{
					ResetAppearance();
				}
			}

			ImGui.End();
        }
    }
}
