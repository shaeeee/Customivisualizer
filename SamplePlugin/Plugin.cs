using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using Dalamud.Hooking;
using Dalamud.Logging;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using System.Threading.Tasks;

namespace SamplePlugin
{
	public sealed class Plugin : IDalamudPlugin
	{
		private const uint FLAG_INVIS = (1 << 1) | (1 << 11);
		private const int OFFSET_RENDER_TOGGLE = 0x104;

		public string Name => "Sample Plugin";

		private const string commandToggle = "/natoggle";

		private DalamudPluginInterface PluginInterface { get; init; }
		private CommandManager CommandManager { get; init; }
		private ClientState ClientState { get; init; }
		private SigScanner SigScanner { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

		private delegate IntPtr CharacterIsMount(IntPtr actor);
		private delegate IntPtr CharacterInitialize(IntPtr drawObjectPtr, IntPtr customizeDataPtr);
		
		private Hook<CharacterInitialize> charaInitHook;

		private CharaCustomizeData? lastData;

		public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
			[RequiredVersion("1.0")] ClientState clientState,
			[RequiredVersion("1.0")] SigScanner sigScanner)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
			this.ClientState = clientState;
			this.SigScanner = sigScanner;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

			FFXIVClientStructs.Resolver.Initialize();

			var charaInitAddr = this.SigScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B F9 48 8B EA 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ??");
			PluginLog.Log($"Found Initialize address: {charaInitAddr.ToInt64():X}");
			this.charaInitHook ??=
				new Hook<CharacterInitialize>(charaInitAddr, CharacterInitializeDetour);
			this.charaInitHook.Enable();

			this.PluginUi = new PluginUI(this.Configuration, this, this.ClientState);

			this.CommandManager.AddHandler(commandToggle, new CommandInfo(OnToggleCommand)
			{
				HelpMessage = "Toggles custom appearance."
			});

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

			if (this.Configuration.ToggleCustomization)
			{
				UpdateCustomizeData();
			}
        }

        public void Dispose()
        {
            this.PluginUi.Dispose();
			this.CommandManager.RemoveHandler(commandToggle);

			this.charaInitHook.Disable();
			this.charaInitHook.Dispose();
			
			RedrawPlayer();
		}

		private void OnToggleCommand(string command, string args)
		{
			this.Configuration.ToggleCustomization = !this.Configuration.ToggleCustomization;
			this.Configuration.Save();

			UpdateCustomizeData();
		}

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }

		public void UpdateCustomizeData()
		{
			lastData = ByteArrayToStruct(this.PluginUi.NewCustomizeData);
			RedrawPlayer();
		}

		private unsafe IntPtr CharacterInitializeDetour(IntPtr drawObjectPtr, IntPtr customizeDataPtr)
		{
			if (this.ClientState.LocalPlayer == null || !this.Configuration.ToggleCustomization) return charaInitHook.Original(drawObjectPtr, customizeDataPtr);

			var customData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);

			var playerDrawObjectPtr = (IntPtr) Marshal.PtrToStructure<FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject>(this.ClientState.LocalPlayer.Address).DrawObject;
			
			PluginLog.Log($"drawObjectPtr: {drawObjectPtr}, LocalPlayer drawObjectPtr: {playerDrawObjectPtr}");

			// EXTREME HACK WARNING: When LocalPlayer drawnObjectPtr is zero during CharacterInitialize, the player seems to always be the actor being initialized.
			if (lastData != null && playerDrawObjectPtr == IntPtr.Zero)
			{
				this.ChangeCustomizeData(customizeDataPtr, lastData.Value);
			}

			return charaInitHook.Original(drawObjectPtr, customizeDataPtr);
		}

		private CharaCustomizeData ByteArrayToStruct(byte[] customize)
		{
			GCHandle handle = GCHandle.Alloc(customize, GCHandleType.Pinned);
			CharaCustomizeData customizeData;
			try
			{
				customizeData = Marshal.PtrToStructure<CharaCustomizeData>(handle.AddrOfPinnedObject());
			}
			finally
			{
				handle.Free();
			}
			return customizeData;
		}

		private void ChangeCustomizeData(IntPtr customizeDataPtr, CharaCustomizeData targetCustomizeData)
		{
			var customData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);

			customData.Race = targetCustomizeData.Race;
			
			// Special-case Hrothgar gender to prevent fuckery
			customData.Gender = (Race)targetCustomizeData.Race switch
			{
				Race.HROTHGAR => 0, // Force male for Hrothgar
				_ => targetCustomizeData.Gender
			};
			
			customData.Tribe = (byte)(customData.Race * 2 - targetCustomizeData.Tribe % 2);

			// Constrain body type to 0-1 so we don't crash the game
			customData.ModelType = (byte)(targetCustomizeData.ModelType % 2);

			customData.FaceType = targetCustomizeData.FaceType;
			customData.HairStyle = targetCustomizeData.HairStyle;
			customData.HasHighlights = targetCustomizeData.HasHighlights;
			customData.SkinColor = targetCustomizeData.SkinColor;
			customData.EyeColor = targetCustomizeData.EyeColor;
			customData.HairColor = targetCustomizeData.HairColor;
			customData.HairColor2 = targetCustomizeData.HairColor2;
			customData.FaceFeatures = targetCustomizeData.FaceFeatures;
			customData.FaceFeaturesColor = targetCustomizeData.FaceFeaturesColor;
			customData.Eyebrows = targetCustomizeData.Eyebrows;
			customData.EyeColor2 = targetCustomizeData.EyeColor2;
			customData.EyeShape = targetCustomizeData.EyeShape;
			customData.NoseShape = targetCustomizeData.NoseShape;
			customData.JawShape = targetCustomizeData.JawShape;
			customData.LipStyle = targetCustomizeData.LipStyle;

			// Hrothgar have a limited number of lip colors?
			customData.LipColor = (Race)targetCustomizeData.Race switch
			{
				Race.HROTHGAR => (byte)(customData.LipColor % 5 + 1),
				_ => targetCustomizeData.LipColor
			};

			customData.RaceFeatureSize = targetCustomizeData.RaceFeatureSize;
			customData.RaceFeatureType = targetCustomizeData.RaceFeatureType;
			customData.BustSize = targetCustomizeData.BustSize;
			customData.Facepaint = targetCustomizeData.Facepaint;
			customData.FacepaintColor = targetCustomizeData.FacepaintColor;

			Marshal.StructureToPtr(customData, customizeDataPtr, true);

			// Reflect potential special case in UI
			this.PluginUi.NewCustomizeDataInt[(int)CustomizeIndex.Gender] = customData.Gender;
			this.PluginUi.NewCustomizeDataInt[(int)CustomizeIndex.Tribe] = customData.Tribe;
			this.PluginUi.NewCustomizeDataInt[(int)CustomizeIndex.ModelType] = customData.ModelType;
			this.PluginUi.NewCustomizeDataInt[(int)CustomizeIndex.LipColor] = customData.LipColor;
		}

		private async void RedrawPlayer()
		{
			var actor = this.ClientState.LocalPlayer;
			if (actor == null) return;
			try
			{
				var addrRenderToggle = actor.Address + OFFSET_RENDER_TOGGLE;
				var val = Marshal.ReadInt32(addrRenderToggle);

				// Trigger a rerender
				val |= (int)FLAG_INVIS;
				Marshal.WriteInt32(addrRenderToggle, val);
				await Task.Delay(100);
				val &= ~(int)FLAG_INVIS;
				Marshal.WriteInt32(addrRenderToggle, val);
			}
			catch (Exception ex)
			{
				PluginLog.LogError(ex.ToString());
			}
		}
	}
}
