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
		private IntPtr? PROC_BASE_ADDR;
		
		private const uint FLAG_INVIS = (1 << 1) | (1 << 11);
		private const int OFFSET_RENDER_TOGGLE = 0x104;
		private const int VIEWING_CUTSCENE_STATUS = 15;

		private const string CHARA_INIT_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B F9 48 8B EA 48 81 C1 ?? ?? ?? ?? E8 ?? ?? ?? ??";
		private const string CHARA_FLAG_SIG = "48 8D 81 70 3D 00 00 84 D2 48 0F 45 C8 48 8B C1 C3";
		private const string CHARA_LOAD_SIG = "48 89 5C 24 10 48 89 6C 24 18 56 57 41 57 48 83 EC 30 48 8B F9 4D 8B F9 8B CA 49 8B D8 8B EA";

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
		private CharaCustomizeOverride CharaCustomizeOverride { get; init; }
		private CharaEquipSlotOverride CharaEquipSlotOverride { get; init; }
		private Overrider Overrider { get; init; }

		private delegate IntPtr InitializeCharacter(IntPtr drawObjectPtr, IntPtr customizeDataPtr);
		private unsafe delegate IntPtr SetCharacterFlag(IntPtr ptr, char* flag, IntPtr actorPtr);
		private delegate IntPtr LoadCharacter(IntPtr actorPtr, IntPtr v2, IntPtr customizeDataPtr, IntPtr v4, IntPtr baseOffset);
		
		private Hook<InitializeCharacter> initializeCharacterHook;
		private Hook<SetCharacterFlag> setCharacterFlagHook;
		private Hook<LoadCharacter> loadCharacterHook;

		private Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.CharaMakeType>? charaSheet;
		private Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.OnlineStatus>? statusSheet;
		private Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Item>? itemSheet;

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
			itemSheet = this.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>();

			this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

			FFXIVClientStructs.Resolver.Initialize();

#pragma warning disable CS8601 // Possible null reference assignment.
			AddHook(CHARA_INIT_SIG, nameof(CHARA_INIT_SIG), InitializeCharacterDetour, ref initializeCharacterHook);
			AddHook(CHARA_FLAG_SIG, nameof(CHARA_FLAG_SIG), SetCharacterFlagDetour, ref setCharacterFlagHook);
			AddHook(CHARA_LOAD_SIG, nameof(CHARA_LOAD_SIG), LoadCharacterDetour, ref loadCharacterHook);
#pragma warning restore CS8601 // Possible null reference assignment.

			this.CharaCustomizeOverride = new();
			this.CharaEquipSlotOverride = new();

			this.UIHelper = new UIHelper(this.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.CharaMakeType>());
			this.Overrider = new Overrider(this.Framework, this.ClientState, this.Configuration, this.CharaCustomizeOverride, this.CharaEquipSlotOverride);
			this.PluginUi = new PluginUI(this.Configuration, this.UIHelper, this.ClientState, this.CharaCustomizeOverride, this.CharaEquipSlotOverride);

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

			this.Framework.Update += FrameworkOnUpdate;
        }

		private void AddHook<T>(string sig, string name, T detour, ref Hook<T> hook) where T : Delegate
		{
			var addr = this.SigScanner.ScanText(sig);
			PluginLog.LogDebug($"Found {name} address: {addr.ToInt64():X}");
			hook ??=
				new Hook<T>(addr, detour);
			hook.Enable();
		}

		public void Dispose()
        {
            this.PluginUi.Dispose();

			this.CommandManager.RemoveHandler(commandToggle);
			this.CommandManager.RemoveHandler(commandConfig);

			this.initializeCharacterHook.Disable();
			this.initializeCharacterHook.Dispose();

			this.setCharacterFlagHook.Disable();
			this.setCharacterFlagHook.Dispose();

			this.loadCharacterHook.Disable();
			this.loadCharacterHook.Dispose();

			this.Framework.Update -= FrameworkOnUpdate;

			OnCustomizeChanged(true);
			OnEquipSlotChanged(true);

			this.Overrider.Dispose();
		}

		private void FrameworkOnUpdate(Framework framework)
		{
			if (this.CharaCustomizeOverride.Dirty) OnCustomizeChanged();
			//if (this.CharaEquipSlotOverride.Dirty) OnEquipSlotChanged();
			if (this.CharaCustomizeOverride.Dirty || this.CharaEquipSlotOverride.Dirty)
			{
				this.CharaCustomizeOverride.Dirty = false;
				this.CharaEquipSlotOverride.Dirty = false;
				RedrawPlayer();
			}
		}

		private void OnCustomizeChanged(bool terminate = false)
		{
			if (this.Configuration.OverrideMode == Configuration.Override.MEM_EDIT && this.Configuration.ToggleCustomization)
			{
				var player = this.ClientState.LocalPlayer;
				if (player != null) this.CharaCustomizeOverride.SetOriginal(player.Customize);
				if (!this.Overrider.Enabled) this.Overrider.Enable();
			}
			if ((this.Configuration.OverrideMode == Configuration.Override.CLASSIC || this.Configuration.OverrideMode == Configuration.Override.HOOK_LOAD || !this.Configuration.ToggleCustomization || terminate))
			{
				this.Overrider.ApplyOriginal(this.CharaCustomizeOverride);
				this.Overrider.Disable();
			}
			
		}
		private void OnEquipSlotChanged(bool terminate = false)
		{
			if (this.Configuration.OverrideMode == Configuration.Override.MEM_EDIT && this.Configuration.ToggleEquipSlots)
			{
				this.CharaEquipSlotOverride.SetOriginal(CharaEquipSlotOverride.GetEquipSlotValues(this.ClientState.LocalPlayer));
				if (!this.Overrider.Enabled) this.Overrider.Enable();
			}
			if ((this.Configuration.OverrideMode == Configuration.Override.CLASSIC || this.Configuration.OverrideMode == Configuration.Override.HOOK_LOAD || !this.Configuration.ToggleEquipSlots || terminate))
			{
				this.Overrider.ApplyOriginal(this.CharaEquipSlotOverride);
				this.Overrider.Disable();
			}
		}

		private unsafe void OnToggleCommand(string command, string args)
		{
			var allArgs = args.Split(" ");
			switch (allArgs[0])
			{
				case "":
					this.Configuration.ToggleCustomization = !this.Configuration.ToggleCustomization;
					this.Configuration.Save();

					this.CharaCustomizeOverride.Dirty = true;
					break;
				case "r":
					RedrawPlayer();
					break;
				case "reload":
					ReloadPlayer();
					break;
				case "e":
					PrintEquipSlotData();
					break;
				case "l":
					OnLookup();
					break;
				case "l2":
					OnLookup2();
					break;
				case "t":
					this.Configuration.ShowEquipSlot = !this.Configuration.ShowEquipSlot;
					this.Configuration.Save();
					break;
				case "hex":
					PluginLog.Log($"0x{int.Parse(allArgs[1]):X}");
					break;
				case "dec":
					PluginLog.Log($"{Convert.ToInt32(allArgs[1], 16)}");
					break;
				case "b":
					PluginLog.Log($"{PROC_BASE_ADDR?.ToInt64():X}");
					break;
				default:
					PluginLog.Log($"No such argument: \"{args}\"");
					break;
			}
		}

		private void OnConfigCommand(string command, string args)
		{
			DrawConfigUI();
		}

		private void DrawUI()
		{
			this.PluginUi.Draw();
		}

		private void DrawConfigUI()
		{
			this.PluginUi.SettingsVisible = true;
		}

		#region Debug

		private unsafe void PrintEquipSlotData()
		{
			var equipSlots = 10;
			var size = 4 * equipSlots;
			if (this.ClientState.LocalPlayer == null) return;
			var battleChara = (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)(void*)this.ClientState.LocalPlayer.Address;
			
			byte[] bytes = new byte[size];
			Marshal.Copy((IntPtr)battleChara->Character.EquipSlotData, bytes, 0, size);
			string output = $"EquipSlotData\n\n";
			try
			{
				for (int i = 0; i < size; i += 4)
				{
					output = output.Insert(output.Length - 1, $"Slot {i / 4}: ");
					for (int j = i; j < i + 4; j++)
					{
						output = output.Insert(output.Length - 1, $"{Convert.ToString(bytes[j], 16)}, ");
						output = output.TrimEnd(',',' ');
					}
					output = output.Insert(output.Length - 1, $"\n");
				}
				PluginLog.Log(output);
			}
			catch (Exception e)
			{
				PluginLog.LogError($"{e}");
			}
		}

		private unsafe void OnLookup()
		{
			if (this.ClientState.LocalPlayer == null) return;
			var battleChara = (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)(void*)this.ClientState.LocalPlayer.Address;
			PluginLog.Log($"PlayerPtr:{((IntPtr)battleChara).ToInt64():X}, CustomizeDataPtr:{((IntPtr)battleChara->Character.CustomizeData).ToInt64():X}, EquipSlotDataPtr:{((IntPtr)battleChara->Character.EquipSlotData).ToInt64():X}, DrawObjectPtr:{GetDrawObjectPtr().ToInt64():X}");
		}

		private void OnLookup2()
		{
			var s = itemSheet;
			var r = s?.GetRow(16623);
			PluginLog.Log($"{r?.ModelSub}");
		}

		#endregion
		#region Character initialization

		private unsafe IntPtr InitializeCharacterDetour(IntPtr drawObjectPtr, IntPtr customizeDataPtr)
		{
			if (this.ClientState.LocalPlayer != null && this.Configuration.ToggleCustomization && this.Configuration.OverrideMode == Configuration.Override.CLASSIC)
			{
				return SoftOverride(drawObjectPtr, customizeDataPtr);
			}
			else return initializeCharacterHook.Original(drawObjectPtr, customizeDataPtr);
		}

		private unsafe IntPtr GetDrawObjectPtr()
		{
			if (this.ClientState.LocalPlayer == null) return IntPtr.Zero;
			var battleChara = (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)(void*)this.ClientState.LocalPlayer.Address;
			return (IntPtr)battleChara->Character.GameObject.DrawObject;
		}

		private IntPtr SoftOverride(IntPtr drawObjectPtr, IntPtr customizeDataPtr)
		{
			if (InCutscene()) return initializeCharacterHook.Original(drawObjectPtr, customizeDataPtr);

			var playerDrawObjectPtr = GetDrawObjectPtr();

			// If the pointer is zero, player is chara being loaded (except in cutscenes/GPose/equipment view)
			if (playerDrawObjectPtr == IntPtr.Zero)
			{
				PluginLog.LogDebug($"ActorInit customizeDataPtr:{customizeDataPtr.ToInt64():X}");
				
				this.CharaCustomizeOverride.ChangeCustomizeData(customizeDataPtr);
				
				return initializeCharacterHook.Original(drawObjectPtr, customizeDataPtr); ;
			}
			else return initializeCharacterHook.Original(drawObjectPtr, customizeDataPtr);
		}

		#endregion
		#region Character flags

		private bool InCutscene()
		{
			return Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
					   Condition[ConditionFlag.WatchingCutscene] ||
					   Condition[ConditionFlag.WatchingCutscene78];
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

		#endregion
		#region Character loading

		// Back up original character data every time character is loaded, and if using HOOK_LOAD also replace data.
		private IntPtr LoadCharacterDetour(IntPtr actorPtr, IntPtr v2, IntPtr customizeDataPtr, IntPtr equipSlotDataPtr, IntPtr baseAddress)
		{
			var player = this.ClientState.LocalPlayer;
			if (player != null && actorPtr == player.Address)
			{
				PluginLog.LogDebug($"LoadActor actorPtr:{actorPtr.ToInt64():X}, v2:{v2.ToInt64():X3}, customizeDataPtr:{customizeDataPtr.ToInt64():X11}, equipSlotDataPtr:{equipSlotDataPtr.ToInt64():X11}, baseAddress:{baseAddress.ToInt64():X}");

				if (this.Configuration.ToggleCustomization || this.Configuration.ToggleEquipSlots)
				{
					if (!PROC_BASE_ADDR.HasValue) PROC_BASE_ADDR = baseAddress;

					this.CharaCustomizeOverride.SetOriginal(Override.PtrToByteArray(customizeDataPtr, CharaCustomizeOverride.SIZE));
					this.CharaEquipSlotOverride.SetOriginal(Override.PtrToByteArray(equipSlotDataPtr, CharaEquipSlotOverride.SIZE));

					if (this.Configuration.OverrideMode == Configuration.Override.HOOK_LOAD)
					{
						if (this.Configuration.ToggleCustomization) this.CharaCustomizeOverride.ChangeCustomizeData(customizeDataPtr);
						if (this.Configuration.ToggleEquipSlots) this.CharaEquipSlotOverride.ChangeEquipSlotData(equipSlotDataPtr);
					}
					return loadCharacterHook.Original(actorPtr, v2, customizeDataPtr, equipSlotDataPtr, baseAddress);
				}
			}
			return loadCharacterHook.Original(actorPtr, v2, customizeDataPtr, equipSlotDataPtr, baseAddress);
		}

		// This will not crash the game, but the player won't get reloaded properly either, TODO look at hooking higher level function
		private void ReloadPlayer()
		{
			var actor = this.ClientState.LocalPlayer;
			if (actor == null || !PROC_BASE_ADDR.HasValue) return;

			var cHandle = GCHandle.Alloc(this.CharaCustomizeOverride.OriginalData, GCHandleType.Pinned);
			var eHandle = GCHandle.Alloc(this.CharaEquipSlotOverride.OriginalData, GCHandleType.Pinned);

			Marshal.StructureToPtr(this.CharaCustomizeOverride.OriginalData, cHandle.AddrOfPinnedObject(), false);
			Marshal.StructureToPtr(this.CharaCustomizeOverride.OriginalData, eHandle.AddrOfPinnedObject(), false);

			loadCharacterHook.Original(actor.Address, IntPtr.Zero, cHandle.AddrOfPinnedObject(), eHandle.AddrOfPinnedObject(), PROC_BASE_ADDR.Value);

			cHandle.Free();
			eHandle.Free();
		}

		#endregion
		#region Character rendering

		private bool IsRedrawing()
		{
			var actor = this.ClientState.LocalPlayer;
			if (actor == null) return false;
			var addrRenderToggle = actor.Address + OFFSET_RENDER_TOGGLE;
			var val = Marshal.ReadInt32(addrRenderToggle);
			return (val & (int)FLAG_INVIS) == FLAG_INVIS;
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

		#endregion
	}
}
