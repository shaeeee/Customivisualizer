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
using Dalamud.Data;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Conditions;

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

		private const string commandToggle = "/cv";
		private const string commandConfig = "/cvcfg";

		private DalamudPluginInterface PluginInterface { get; init; }
		private CommandManager CommandManager { get; init; }
		private ClientState ClientState { get; init; }
		private SigScanner SigScanner { get; init; }
		private DataManager DataManager { get; init; }
		private Framework Framework { get; init; }
		private ObjectTable ObjectTable { get; init; }
		private Condition Condition { get; init; }

        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }
		private UIHelper UIHelper { get; init; }
		private CharaDataOverride CharaDataOverride { get; init; }
		private Overrider Overrider { get; init; }

		private delegate IntPtr CharacterInitialize(IntPtr drawObjectPtr, IntPtr customizeDataPtr);
		private unsafe delegate IntPtr SetCharacterFlag(IntPtr ptr, char* flag, IntPtr actorPtr);
		private Hook<CharacterInitialize> charaInitHook;
		private Hook<SetCharacterFlag> setCharacterFlagHook;

		private Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.CharaMakeType>? charaSheet;
		private Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.OnlineStatus>? statusSheet;

		private bool inCutscene;
		private bool outCutsceneRedrawQueued;

		public unsafe Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
			[RequiredVersion("1.0")] ClientState clientState,
			[RequiredVersion("1.0")] SigScanner sigScanner,
			[RequiredVersion("1.0")] DataManager dataManager,
			[RequiredVersion("1.0")] Framework framework,
			[RequiredVersion("1.0")] ObjectTable objectTable,
			[RequiredVersion("1.0")] Condition condition)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
			this.ClientState = clientState;
			this.SigScanner = sigScanner;
			this.DataManager = dataManager;
			this.Framework = framework;
			this.ObjectTable = objectTable;
			this.Condition = condition;

			charaSheet = this.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.CharaMakeType>();
			statusSheet = this.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.OnlineStatus>();

			this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

			FFXIVClientStructs.Resolver.Initialize();

			var charaInitAddr =	this.SigScanner.ScanText(CHARA_INIT_SIG);
			PluginLog.LogDebug($"Found Initialize address: {charaInitAddr.ToInt64():X}");
			this.charaInitHook ??=
				new Hook<CharacterInitialize>(charaInitAddr, CharacterInitializeDetour);
			this.charaInitHook.Enable();

			var charaFlagAddr = this.SigScanner.ScanText(CHARA_FLAG_SIG);
			PluginLog.LogDebug($"Found Status address: {charaFlagAddr.ToInt64():X}");
			this.setCharacterFlagHook ??=
				new Hook<SetCharacterFlag>(charaFlagAddr, SetCharacterFlagDetour);
			this.setCharacterFlagHook.Enable();

			this.CharaDataOverride = new();
			this.CharaDataOverride.DataChanged += OnCharaDataChanged;

			this.UIHelper = new UIHelper(this.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.CharaMakeType>());
			this.Overrider = new Overrider(this.Framework, clientState, this.CharaDataOverride);
			this.PluginUi = new PluginUI(this.Configuration, this.UIHelper, this.ClientState, this.CharaDataOverride);

			this.CommandManager.AddHandler(commandToggle, new CommandInfo(OnToggleCommand)
			{
				HelpMessage = "Toggles custom appearance."
			});
			this.CommandManager.AddHandler(commandConfig, new CommandInfo(OnConfigCommand)
			{
				HelpMessage = "Opens customization menu."
			});

			this.PluginInterface.UiBuilder.Draw += DrawUI;
			this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            this.PluginUi.Dispose();

			this.CommandManager.RemoveHandler(commandToggle);
			this.CommandManager.RemoveHandler(commandConfig);

			this.charaInitHook.Disable();
			this.charaInitHook.Dispose();

			this.setCharacterFlagHook.Disable();
			this.setCharacterFlagHook.Dispose();

			this.Overrider.Dispose();

			this.CharaDataOverride.DataChanged -= OnCharaDataChanged;
			
			RedrawPlayer();
		}

		private void OnCharaDataChanged(object? sender, EventArgs e)
		{
			if (this.Configuration.OverrideMode == Configuration.Override.HARD && this.Configuration.ToggleCustomization && !Overrider.Enabled)
			{
				Overrider.Enable();
			}
			if ((this.Configuration.OverrideMode == Configuration.Override.SOFT || !this.Configuration.ToggleCustomization) && Overrider.Enabled)
			{
				Overrider.Disable();
			}
			RedrawPlayer();
		}

		private void OnToggleCommand(string command, string args)
		{
			var allArgs = args.Split(" ");
			switch (allArgs[0])
			{
				default:
					this.Configuration.ToggleCustomization = !this.Configuration.ToggleCustomization;
					this.Configuration.Save();

					RedrawPlayer();
					break;
				case "oe":
					this.Overrider.Enable();
					break;
				case "od":
					this.Overrider.Disable();
					break;
				case "r":
					RedrawPlayer();
					break;
				case "test":
					OnTestCommand();
					break;
				case "l":
					OnLookup();
					break;
				case "l2":
					OnLookup2();
					break;
				case "hex":
					ToHex(allArgs[1]);
					break;
				case "dec":
					ToDecimal(allArgs[1]);
					break;

			}
		}

		private void OnConfigCommand(string command, string args)
		{
			DrawConfigUI();
		}

		private unsafe void OnTestCommand()
		{
			if (this.ClientState.LocalPlayer == null) return;
			var battleChara = (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)(void*)this.ClientState.LocalPlayer.Address;
			byte[] bytes = new byte[26];
			Marshal.Copy((IntPtr)battleChara->Character.EquipSlotData, bytes, 0, 26);
			PluginLog.Log($"EquipSlotData: {string.Join(", ", bytes)}");
		}

		private void OnLookup()
		{
			var s = this.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.EquipSlotCategory>();
			for (uint i = 0; i < s?.RowCount; i++)
			{
				var r = s?.GetRow(i);
				PluginLog.Log($"{i:00}::SoulCrystal({r?.SoulCrystal:00}), Mainhand({r?.MainHand:00}), Offhand({r?.OffHand:00}), Head({r?.Head:00}), Body({r?.Body:00})");
			}
		}

		private void OnLookup2()
		{
			var s = this.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>();
			var r = s?.GetRow(16623);
			PluginLog.Log($"{r?.Name}");
		}

		private void ToHex(string value)
		{
			PluginLog.Log($"0x{int.Parse(value):X}");
		}

		private void ToDecimal(string value)
		{
			PluginLog.Log($"{Convert.ToInt32(value, 16)}");
		}

		private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }

		private unsafe IntPtr CharacterInitializeDetour(IntPtr drawObjectPtr, IntPtr customizeDataPtr)
		{
			if (this.ClientState.LocalPlayer == null || !this.Configuration.ToggleCustomization) return charaInitHook.Original(drawObjectPtr, customizeDataPtr);
			return this.Configuration.OverrideMode switch
			{
				Configuration.Override.SOFT => SoftOverride(drawObjectPtr, customizeDataPtr),
				Configuration.Override.HARD => charaInitHook.Original(drawObjectPtr, customizeDataPtr),
				_ => charaInitHook.Original(drawObjectPtr, customizeDataPtr),
			};
		}

		private unsafe IntPtr GetDrawObjectPtr()
		{
			if (this.ClientState.LocalPlayer == null) return IntPtr.Zero;
			var battleChara = (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)(void*)this.ClientState.LocalPlayer.Address;
			return (IntPtr)battleChara->Character.GameObject.DrawObject;
		}

		private bool InCutscene()
		{
			return Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
					   Condition[ConditionFlag.WatchingCutscene] ||
					   Condition[ConditionFlag.WatchingCutscene78];
		}

		private IntPtr SoftOverride(IntPtr drawObjectPtr, IntPtr customizeDataPtr)
		{
			if (InCutscene()) return charaInitHook.Original(drawObjectPtr, customizeDataPtr);

			var oldCustomizeData = Marshal.PtrToStructure<CharaCustomizeData>(customizeDataPtr);

			// Back up original customize data
			byte[] origData = new byte[28];
			Marshal.Copy(customizeDataPtr, origData, 0, 28);

			var playerDrawObjectPtr = GetDrawObjectPtr();

			// If the pointer is zero, player is chara being loaded (except in cutscenes/GPose/equipment view)
			if (playerDrawObjectPtr == IntPtr.Zero)
			{
				PluginLog.Log($"CharaInit");
				this.CharaDataOverride.ChangeCustomizeData(customizeDataPtr);
			}

			IntPtr result;
			try
			{
				result = charaInitHook.Original(drawObjectPtr, customizeDataPtr);
				this.PluginUi.ShowInvalidDataWarning = false;
			}
			catch (Exception)
			{
				// Restore backed up customize data
				Marshal.Copy(origData, 0, customizeDataPtr, 28);
				result = charaInitHook.Original(drawObjectPtr, customizeDataPtr);
				this.PluginUi.ShowInvalidDataWarning = true;
			}

			return result;
		}

		private IntPtr HardOverride(IntPtr drawObjectPtr, IntPtr customizeDataPtr)
		{
			Overrider.Apply();
			// CustomizeDataPtr points to a copy of actual CustomizeData, so let SoftOverride take care of the immediate effect
			return SoftOverride(drawObjectPtr, customizeDataPtr);
		}

		private async void OnOnlineStatusChanged()
		{
			inCutscene = InCutscene() || inCutscene;
			if (!InCutscene() && inCutscene && !outCutsceneRedrawQueued && this.Configuration.ToggleCustomization)
			{
				outCutsceneRedrawQueued = true;
				await Task.Delay(200);
				RedrawPlayer();
				outCutsceneRedrawQueued = false;
				inCutscene = false;
			}
		}

		private unsafe IntPtr SetCharacterFlagDetour(IntPtr ptr, char* flag, IntPtr actorPtr)
		{
			IntPtr? localPlayerPtr;
			if ((localPlayerPtr = this.ClientState.LocalPlayer?.Address) == null || actorPtr != localPlayerPtr.Value)
			{
				return setCharacterFlagHook.Original(ptr, flag, actorPtr);
			}
			OnOnlineStatusChanged();
			return setCharacterFlagHook.Original(ptr, flag, actorPtr);
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
