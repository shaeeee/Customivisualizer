using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using Dalamud.Hooking;
using Dalamud.Logging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Customivisualizer
{
	public sealed class Plugin : IDalamudPlugin
	{
		private const uint FLAG_INVIS = (1 << 1) | (1 << 11);
		private const int OFFSET_RENDER_TOGGLE = 0x104;
		private const int VIEWING_CUTSCENE_STATUS = 15;

		private const string CHARA_INIT_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B F9 48 8B EA 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ??";
		private const string CHARA_FLAG_SIG = "48 8D 81 70 3D 00 00 84 D2 48 0F 45 C8 48 8B C1 C3";

		//MANUAL SETTER ADDR:	0x7FF6129F72A0
		//AUTO SETTER ADDR:		0x7FF612F78720

		public string Name => "Customivisualizer";

		private const string commandToggle = "/cvtoggle";
		private const string commandConfig = "/cvcfg";

		private const string commandTest = "/cvtest";

		private DalamudPluginInterface PluginInterface { get; init; }
		private CommandManager CommandManager { get; init; }
		private ClientState ClientState { get; init; }
		private SigScanner SigScanner { get; init; }
		private Dalamud.Data.DataManager DataManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

		private delegate IntPtr CharacterInitialize(IntPtr drawObjectPtr, IntPtr customizeDataPtr);
		private unsafe delegate IntPtr SetCharacterFlag(IntPtr ptr, char* flag, IntPtr actorPtr);
		private Hook<CharacterInitialize> charaInitHook;
		private Hook<SetCharacterFlag> setCharacterFlagHook;

		private CharaCustomizeData? lastData;

		private Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.CharaMakeType>? charaSheet;
		private Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.OnlineStatus>? statusSheet;

		private uint? prevOnlineStatus;
		private uint onlineStatus;

		public unsafe Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
			[RequiredVersion("1.0")] ClientState clientState,
			[RequiredVersion("1.0")] SigScanner sigScanner,
			[RequiredVersion("1.0")] Dalamud.Data.DataManager dataManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
			this.ClientState = clientState;
			this.SigScanner = sigScanner;
			this.DataManager = dataManager;

			charaSheet = this.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.CharaMakeType>();
			statusSheet = this.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.OnlineStatus>();

			this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

			FFXIVClientStructs.Resolver.Initialize();

			var charaInitAddr =	this.SigScanner.ScanText(CHARA_INIT_SIG);
			PluginLog.Log($"Found Initialize address: {charaInitAddr.ToInt64():X}");
			this.charaInitHook ??=
				new Hook<CharacterInitialize>(charaInitAddr, CharacterInitializeDetour);
			this.charaInitHook.Enable();

			var charaFlagAddr = this.SigScanner.ScanText(CHARA_FLAG_SIG);
			PluginLog.Log($"Found Status address: {charaFlagAddr.ToInt64():X}");
			this.setCharacterFlagHook ??=
				new Hook<SetCharacterFlag>(charaFlagAddr, SetCharacterFlagDetour);
			this.setCharacterFlagHook.Enable();

			this.PluginUi = new PluginUI(this.Configuration, this, this.ClientState);

			this.CommandManager.AddHandler(commandToggle, new CommandInfo(OnToggleCommand)
			{
				HelpMessage = "Toggles custom appearance."
			});
			this.CommandManager.AddHandler(commandConfig, new CommandInfo(OnConfigCommand)
			{
				HelpMessage = "Opens customization menu."
			});
			this.CommandManager.AddHandler(commandTest, new CommandInfo(OnTestCommand)
			{
				HelpMessage = "Test."
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
			this.CommandManager.RemoveHandler(commandConfig);

			this.CommandManager.RemoveHandler(commandTest);

			this.charaInitHook.Disable();
			this.charaInitHook.Dispose();

			this.setCharacterFlagHook.Disable();
			this.setCharacterFlagHook.Dispose();
			
			RedrawPlayer();
		}

		private void OnToggleCommand(string command, string args)
		{
			this.Configuration.ToggleCustomization = !this.Configuration.ToggleCustomization;
			this.Configuration.Save();

			UpdateCustomizeData();
		}

		private void OnConfigCommand(string command, string args)
		{
			DrawConfigUI();
		}

		private unsafe void OnTestCommand(string command, string args)
		{
			if (this.ClientState.LocalPlayer == null) return;
			var adress = (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)(void*)this.ClientState.LocalPlayer.Address;
			var chara = Marshal.PtrToStructure<CharaCustomizeData>((IntPtr)adress->Character.CustomizeData);
			var sheet = this.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.OnlineStatus>();
			var row = sheet?.GetRow(adress->Character.OnlineStatus);
			PluginLog.Log($"Test: {sheet?.Name}::{row?.Name}({row?.RowId})");
			
		}

		public unsafe string[]? GetRaceAndTribe(int? tribe = null, int? gender = null)
		{
			if (this.ClientState.LocalPlayer == null || charaSheet == null) return null;

			uint index;
			if (tribe == null || gender == null)
			{
				var adress = (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)(void*)this.ClientState.LocalPlayer.Address;
				var chara = Marshal.PtrToStructure<CharaCustomizeData>((IntPtr)adress->Character.CustomizeData);
				index = GetCharaTypeIndex(chara);
			}
			else
			{
				index = GetCharaTypeIndex(tribe.Value, gender.Value);
			}
			var row = charaSheet?.GetRow(index);
			return new string[] {$"{row?.Race.Value?.Feminine.ToString()}", $"{row?.Tribe.Value?.Feminine.ToString()}"};
		}

		// Function to obtain correct CharaMakeType excel row among every race, tribe and gender
		private uint GetCharaTypeIndex(CharaCustomizeData chara)
		{
			return GetCharaTypeIndex(chara.Tribe, chara.Gender);
		}

		private uint GetCharaTypeIndex(int tribe, int gender)
		{
			return (uint)(2u * tribe + gender - 2u);
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
			if (this.ClientState.LocalPlayer == null || !this.Configuration.ToggleCustomization || onlineStatus == VIEWING_CUTSCENE_STATUS) return charaInitHook.Original(drawObjectPtr, customizeDataPtr);

			var customData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);

			var playerDrawObjectPtr = (IntPtr) Marshal.PtrToStructure<FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject>(this.ClientState.LocalPlayer.Address).DrawObject;
			
			PluginLog.LogDebug($"drawObjectPtr: {drawObjectPtr}, LocalPlayer drawObjectPtr: {playerDrawObjectPtr}");

			// EXTREME HACK WARNING: When LocalPlayer drawnObjectPtr is zero during CharacterInitialize, the player seems to always be the actor being initialized.
			if (lastData != null && playerDrawObjectPtr == IntPtr.Zero)
			{
				this.ChangeCustomizeData(customizeDataPtr, lastData.Value);
			}

			return charaInitHook.Original(drawObjectPtr, customizeDataPtr);
		}

		private unsafe uint GetTrueOnlineStatus()
		{
			var player = this.ClientState.LocalPlayer;
			if (player == null) return 0;
			var index = ((FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*) (void*)player.Address)->Character.OnlineStatus;
			var row = statusSheet?.GetRow(index);
			return row != null ? row.RowId : 0;
		}

		private async void OnOnlineStatusChanged()
		{
			await Task.Delay(50);
			var currentData = GetTrueOnlineStatus();
			prevOnlineStatus = prevOnlineStatus == null ? currentData : onlineStatus;
			onlineStatus = currentData;
			PluginLog.LogDebug($"Online status changed from {prevOnlineStatus} to {onlineStatus}");
			if (prevOnlineStatus == VIEWING_CUTSCENE_STATUS) RedrawPlayer();
		}

		private unsafe IntPtr SetCharacterFlagDetour(IntPtr ptr, char* flag, IntPtr actorPtr)
		{
			//PluginLog.LogDebug($"ptr:{ptr.ToInt64()}, flag:{new string(flag)}, actorPtr?:{actorPtr}");
			IntPtr? localPlayerPtr;
			if ((localPlayerPtr = this.ClientState.LocalPlayer?.Address) == null || actorPtr != localPlayerPtr.Value)
			{
				return setCharacterFlagHook.Original(ptr, flag, actorPtr);
			}
			OnOnlineStatusChanged();
			return setCharacterFlagHook.Original(ptr, flag, actorPtr);
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
