using Amongus.GameModes.HideAndSeek;
using AmongUs.Data;
using AmongUs.GameOptions;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Reflection;
using Il2CppSystem.Runtime.CompilerServices;
using Il2CppSystem.Text.RegularExpressions;
using InnerNet;
using MS.Internal.Xml.XPath;
using Nebula.Map;
using Nebula.Modules;
using Nebula.Modules.Cosmetics;
using Nebula.Modules.GUIWidget;
using Nebula.Patches;
using Nebula.Roles.Assignment;
using Nebula.Roles.Complex;
using Nebula.Roles.Impostor;
using Nebula.Roles.Neutral;
using Nebula.Roles.Perks;
using Nebula.Utilities;
using Nebula.VoiceChat;
using Plana.Role.Complex;
using Plana.Roles.Crewmate;
using Plana.Roles.Impostor;
using Plana.Roles.Modifier;
using Plana.Roles.Neutral;
using Rewired.UI.ControlMapper;
using System.Reflection;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Virial;
using Virial.Common;
using Virial.Events.Game.Minimap;
using Virial.Game;
using Virial.Helpers;
using BindingFlags = System.Reflection.BindingFlags;
using Color = UnityEngine.Color;
using DirectoryInfo = System.IO.DirectoryInfo;
using FileInfo = System.IO.FileInfo;
using IEnumerator = System.Collections.IEnumerator;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
using Virial.Runtime;

namespace Plana.Core;

[NebulaPreprocess(PreprocessPhase.BuildAssignmentTypes)]
public static class AssignmentSetUp
{
    public static void Preprocess(NebulaPreprocessor preprocessor)
    {
        try
        {
            PDebug.Log("Add Addon Roles Assignment Types");
            preprocessor.RegisterAssignmentType(() => Yandere.MyRole, (int[] lastArgs, DefinedRole role) => Yandere.GenerateArgument(role), "yandereExRole", new Virial.Color(227, 0, 136), (AbilityAssignmentStatus status, DefinedRole role) => status.HasFlag(AbilityAssignmentStatus.CanLoadToKillNeutral) || status.HasFlag(AbilityAssignmentStatus.CanLoadToMadmate), () => ((ISpawnable)Yandere.MyRole).IsSpawnable && Yandere.YandereRoleOption);
            preprocessor.RegisterAssignmentType(() => Moriarty.MyRole, (int[] lastArgs, DefinedRole role) => Moriarty.GenerateArgument(role), "moriartized", new(106, 252, 45), (AbilityAssignmentStatus status, DefinedRole role) => status.HasFlag(AbilityAssignmentStatus.CanLoadToMadmate), () => ((ISpawnable)Moriarty.MyRole).IsSpawnable && Moriarty.MoriartizedRoleOption);
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
}
[NebulaPreprocess(PreprocessPhase.PostFixStructure)]
public class NebulaCustomEndCriteria
{
    static NebulaCustomEndCriteria()
    {
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new YandereCriteria().Register(NebulaAPI.CurrentGame, null));
        DIManager.Instance.RegisterGeneralModule<IGameModeStandard>(() => new MoriartyCriteria().Register(NebulaAPI.CurrentGame, null));
    }
    public class YandereCriteria : IModule,IGameOperator
    {
        [OnlyHost]
        void YandereWinCheck(GameUpdateEvent ev)
        {
            try
            {
                int totalAlive = 0;
                int yandereteamnum = 0;
                foreach (var p in NebulaGameManager.Instance.AllPlayerInfo)
                {
                    if (p == null || p.IsDead)
                    {
                        continue;
                    }
                    if (!p.IsDead)
                    {
                        totalAlive++;
                    }
                    if (p.Role is Yandere.Instance instance || p.TryGetModifier<YandereLover.Instance>(out var y))
                    {
                        yandereteamnum++;
                    }
                    if (p.Role is Skinner.Instance)
                    {
                        totalAlive--;
                    }
                }
                if (totalAlive <= yandereteamnum)
                {
                    Virial.Game.Game currentGame = NebulaAPI.CurrentGame;
                    if (currentGame != null)
                    {
                        currentGame.TriggerGameEnd(Yandere.Instance.YandereTeamWin, GameEndReason.Situation, null);
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
    }
    public class MoriartyCriteria : IModule,IGameOperator
    {
        [OnlyHost]
        void MoriartyWinCheck(GameUpdateEvent ev)
        {
            try
            {
                static bool isJackalTeam(GamePlayer p) => (p.Role.Role.Team == Jackal.MyTeam) || (p.Modifiers.Any(m => m.Modifier == SidekickModifier.MyRole));
                static bool isMoran(GamePlayer p) => (p.Role.Role == Moran.MyRole) || (p.Modifiers.Any(m => m.Modifier == MoranModifier.MyRole));
                int totalAlive = 0;
                int moriartyTeamNum = 0;
                bool leftImpostor = false;
                bool leftJackal = false;
                bool leftYandere = false;
                foreach (var p in NebulaGameManager.Instance.AllPlayerInfo)
                {
                    if (p == null || p.IsDead)
                    {
                        continue;
                    }
                    if (!p.IsDead)
                    {
                        totalAlive++;
                    }
                    if (Moriarty.Instance.IsSameTeam(p))
                    {
                        moriartyTeamNum++;
                    }
                    Lover.Instance lover;
                    if (p.Role.Role.Team == Nebula.Roles.Impostor.Impostor.MyTeam && (!p.TryGetModifier<Lover.Instance>(out lover) || lover.IsAloneLover) && !p.TryGetModifier<HasLove.Instance>(out var love))
                    {
                        leftImpostor = true;
                    }
                    if (isJackalTeam(p))
                    {
                        leftJackal = true;
                    }
                    if (p.Role is Yandere.Instance)
                    {
                        leftYandere = true;
                    }
                }
                if (moriartyTeamNum * 2 >= totalAlive && !leftImpostor && !leftJackal && !leftYandere)
                {
                    Virial.Game.Game currentGame = NebulaAPI.CurrentGame;
                    if (currentGame != null)
                    {
                        currentGame.TriggerGameEnd(Moriarty.Instance.MoriartyTeamWin, GameEndReason.Situation, null);
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        [OnlyHost]
        void HolmesOnDead(PlayerMurderedEvent ev)
        {
            if (ev.Player.PlayerId == ev.Murderer.PlayerId)
            {
                return;
            }
            if (ev.Dead.Role.Role is Holmes)
            {
                if (ev.Murderer.Role is Moriarty.Instance || ev.Murderer.Role.Role is Moran || ev.Murderer.TryGetModifier<MoranModifier.Instance>(out var m))
                {
                    NebulaAPI.CurrentGame?.TriggerGameEnd(Moriarty.Instance.MoriartyTeamWin, GameEndReason.Situation);
                }
            }
        }
    }
}
    public class PatchManager
{
    //public static PatchManager instance = new PatchManager();
    /*[System.Runtime.CompilerServices.ModuleInitializer]
    internal static void RunOnModuleLoad()
    {
        Init();
    }*/
    static PatchManager()
    {
        Init();
    }
    static Harmony? harmony;
    static AssetBundle? ab;
    public static AudioClip GetSound(string name)
    {
        try
        {
            if (ab == null)
            {
                ab = AssetBundle.LoadFromMemory(NebulaAPI.AddonAsset.GetResource("sounds.bundle")!.AsStream()!.ReadBytes());
            }
            return ab!.LoadAsset(name + ".wav").Cast<AudioClip>();
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
        return null!;
    }
    public static void Init()
    {
        try
        {
            ab = AssetBundle.LoadFromMemory(NebulaAPI.AddonAsset.GetResource("sounds.bundle")!.AsStream()!.ReadBytes());
            audioclips = new Dictionary<string, AudioClip>();
            PDebug.Log("AssetBundleLoaded IsNull:" + (ab == null ? "true" : "false"));
            PDebug.Log("RunPatch");
            harmony = new Harmony("PatchManager");
            harmony.Patch(typeof(ChatController).GetMethod("SendChat"), new HarmonyMethod(typeof(PatchManager).GetMethod("SendChat")));
            harmony.Patch(typeof(ChatController).GetMethod("Update", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("ChatUpdate")));
            harmony.Patch(typeof(NebulaManager).GetMethod("Update"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("Update")));
            harmony.Patch(typeof(NebulaGameEnd).GetMethod("Preprocess", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public), null, new HarmonyMethod(typeof(PatchManager).GetMethod("LoadCustomTeamTip")));
            harmony.Patch(typeof(HudOverrideTask).GetMethod("Initialize"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("CommCamouflagerEffectInitialize")));
            harmony.Patch(typeof(HudOverrideTask).GetMethod("Complete"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("CommCamouflagerEffectComptele")));
            harmony.Patch(typeof(MeetingHud).GetMethod("Start"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("LoverChatMeetingStart")));
            harmony.Patch(typeof(ChatController).GetMethod("AddChat"), new HarmonyMethod(typeof(PatchManager).GetMethod("LoverChatAddChat"), 1));
            harmony.Patch(typeof(HudManager).GetMethod("Update"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("LoverChatEnableChat")));
            harmony.Patch(typeof(AmongUsClient).GetMethod("CoStartGame", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("CoStartGame")));
            harmony.Patch(typeof(ShipStatus).GetMethod("Start"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("ShipStatusStartPos")));
            harmony.Patch(typeof(PlayerVoteArea).GetMethod("SetCosmetics", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SetLevel")));
            try
            {
                harmony.Patch(typeof(NebulaEndCriteria).GetNestedType("CrewmateCriteria", BindingFlags.NonPublic)!.GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic), new HarmonyMethod(typeof(PatchManager).GetMethod("CrewmateCheck")));
                harmony.Patch(typeof(NebulaEndCriteria).GetNestedType("ImpostorCriteria", BindingFlags.NonPublic)!.GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic), new HarmonyMethod(typeof(PatchManager).GetMethod("ImpostorCheck")));
                harmony.Patch(typeof(NebulaEndCriteria).GetNestedType("JackalCriteria", BindingFlags.NonPublic)!.GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic), new HarmonyMethod(typeof(PatchManager).GetMethod("JackalCheck")));
            }
            catch (Exception e)
            {
                PDebug.Log("EndCriteriaPatchError Error:" + e.ToString());
            }
            harmony.Patch(typeof(MeetingCalledAnimation).GetMethod("CoShow"), new HarmonyMethod(typeof(PatchManager).GetMethod("CoShow")));
            harmony.Patch(typeof(SabotageButton).GetMethod("DoClick"), new HarmonyMethod(typeof(PatchManager).GetMethod("MadmatePlusSabotageButton")));
            harmony.Patch(typeof(InfectedOverlay).GetMethod("get_CanUseSabotage"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("MadmatePlusCanSabotage")));
            harmony.Patch(typeof(HudManagerExtension).GetMethod("UpdateHudContent"), new HarmonyMethod(typeof(PatchManager).GetMethod("MadmatePlusButtonPatch")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod("get_FeelLikeHaveCrewmateTasks"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("MadmateFeelLikeHaveCrewmateTasks")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod("get_HasAnyTasks"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("MadmateHasAnyTasks")));
            harmony.Patch(typeof(Vent).GetMethod("CanUse"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("VentCanUse")));
            harmony.Patch(typeof(Guesser.Ability).GetPrivateMethodInfoType("OnMeetingStart"), new HarmonyMethod(typeof(PatchManager).GetMethod("GuesserMeetingStart")));
            harmony.Patch(typeof(Guesser.Ability).GetPrivateMethodInfoType("OnDead"), new HarmonyMethod(typeof(PatchManager).GetMethod("GuesserDead")));
            harmony.Patch(typeof(GuesserModifier.Instance).GetPrivateMethodInfoType("OnMeetingStart"), new HarmonyMethod(typeof(PatchManager).GetMethod("GuesserModifierMeetingStart")));
            harmony.Patch(typeof(GuesserModifier.Instance).GetPrivateMethodInfoType("OnDead"), new HarmonyMethod(typeof(PatchManager).GetMethod("GuesserDead")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod("UpdateRoleText"), null, new HarmonyMethod(typeof(GuesserSystem).GetMethod("GuessedLockTargets")));
            harmony.Patch(typeof(Spectre.Instance).GetPrivateMethodInfoType("CheckAndTrackKiller"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SpectreArrowPatch")), null, null, null);
            harmony.Patch(typeof(Sheriff.Ability).GetPrivateMethodInfoType("CanKill"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SheriffCanKillPatch")), null, null, null);
            harmony.Patch(typeof(Doctor.Ability).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool), typeof(float) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("DoctorShieldButton")));
            harmony.Patch(typeof(PlayerControl).GetMethod("FixedUpdate"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("DoctorOutlineUpdate")));
            harmony.Patch(typeof(KillRequestHandler).GetMethod("RequestKill"), new HarmonyMethod(typeof(PatchManager).GetMethod("RequestKillSheildTarget")), new HarmonyMethod(typeof(PatchManager).GetMethod("KillEnd")));
            harmony.Patch(typeof(ChatBubble).GetMethod("SetName"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("DecorateNameColor")));
            harmony.Patch(typeof(MeetingHud).GetMethod("Close"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("GenerateMadmate")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod("UpdateNameText"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("UpdateNameText")));
            harmony.Patch(typeof(ItemSupplierManager).GetPrivateMethodInfoType("OnGameStart"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("GrowALLFlowers")));
            harmony.Patch(typeof(ItemSupplierManager).GetPrivateMethodInfoType("OnGameStartClient"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("ItemSupplierStartClient")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod("OnGameStart"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("PlayerStartGame")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod("Update"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("PlayerUpdate")));
            harmony.Patch(typeof(PlayerControl).GetMethod("CmdReportDeadBody"), new HarmonyMethod(typeof(PatchManager).GetMethod("CmdReportBody")));
            harmony.Patch(typeof(ShipExtension).GetPrivateStaticMethodInfoType("ModifyPolus"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("ModifyPolus")));
            harmony.Patch(typeof(ShipExtension).GetPrivateStaticMethodInfoType("ModifyEarlierPolus"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("ModifyPolusEarly")));
            var interfaceType = typeof(Virial.Game.IPlayerAbility);
            string getterName = $"{interfaceType.FullName}.get_HideKillButton";
            //harmony.Patch(typeof(Sniper.Ability).GetConstructor(new Type[] { typeof(GamePlayer), typeof(float), typeof(bool) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SniperKillPatch")));
            harmony.Patch(typeof(Sniper.Ability).GetMethod(getterName, BindingFlags.NonPublic | BindingFlags.Instance), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SniperKillPatch2")));
            harmony.Patch(typeof(Sniper.SniperRifle).GetMethod("GetTarget"), new HarmonyMethod(typeof(PatchManager).GetMethod("SniperGetTargetPatch")));
            //harmony.Patch(typeof(Raider.Ability).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("RaiderKillPatch")));
            harmony.Patch(typeof(Raider.Ability).GetMethod(getterName, BindingFlags.NonPublic | BindingFlags.Instance), null, new HarmonyMethod(typeof(PatchManager).GetMethod("RaiderKillPatch2")));
            harmony.Patch(typeof(ProgressTracker).GetMethod("FixedUpdate"), new HarmonyMethod(typeof(PatchManager).GetMethod("ProgressTrackerUpdateFix")));
            harmony.Patch(typeof(FreePlayRoleAllocator).GetMethod("Assign"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("AssignExtraModifier")));
            harmony.Patch(typeof(StandardRoleAllocator).GetMethod("Assign"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("AssignExtraModifier")));
            harmony.Patch(typeof(Sheriff.Ability).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool), typeof(int) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SheriffUnlockKill")));
            harmony.Patch(typeof(DelayPlayDropshipAmbiencePatch).GetMethod("Postfix"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("LobbyStart")));
            harmony.Patch(typeof(Navvy.Ability).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool), typeof(int) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("NavvyCamButton")));
            harmony.Patch(typeof(Lover.Instance).GetPrivateMethodInfo("OnMurdered"), new HarmonyMethod(typeof(PatchManager).GetMethod("LoverOnDisconnectedPatch")));
            harmony.Patch(typeof(NumberOption).GetMethod("Initialize"), new HarmonyMethod(typeof(PatchManager).GetMethod("NumberOptionInitializePrefix")));
            harmony.Patch(typeof(NebulaGameManager).GetMethod("RegisterPlayer"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("AssignHostOp")));
            harmony.Patch(typeof(Psychic.Ability).GetMethod("OnReportDeadBody", BindingFlags.NonPublic | BindingFlags.Instance), new HarmonyMethod(typeof(PatchManager).GetMethod("PsychicReportDeadBody")));
            harmony.Patch(typeof(Collator.Ability).GetMethod("RegisterResult", BindingFlags.NonPublic | BindingFlags.Instance), new HarmonyMethod(typeof(PatchManager).GetMethod("CollatorRegisterResult")));
            harmony.Patch(typeof(Jackal.Instance).GetMethod("get_RoleArgumentsForSidekick"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("JackalArgumentsForSidekick")));
            string released = typeof(IGameOperator).FullName + ".OnReleased";
            harmony.Patch(typeof(Trapper.NiceAbility).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool), typeof(int) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("NiceTrapperActivate")));
            harmony.Patch(typeof(Trapper.EvilAbility).GetConstructor(new Type[] { typeof(GamePlayer), typeof(bool), typeof(int) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("EvilTrapperActivate")));
            harmony.Patch(typeof(Trapper.NiceAbility).GetPrivateMethodInfoType("OnMeetingStart"), new HarmonyMethod(typeof(PatchManager).GetMethod("NiceTrapperMeetingStart")));
            harmony.Patch(typeof(Trapper.EvilAbility).GetPrivateMethodInfoType("OnMeetingStart"), new HarmonyMethod(typeof(PatchManager).GetMethod("EvilTrapperMeetingStart")));
            harmony.Patch(typeof(Trapper.NiceAbility).GetPrivateMethodInfoType(released), new HarmonyMethod(typeof(PatchManager).GetMethod("NiceTrapperReleased")));
            harmony.Patch(typeof(Trapper.EvilAbility).GetPrivateMethodInfoType(released), new HarmonyMethod(typeof(PatchManager).GetMethod("EvilTrapperReleased")));
            harmony.Patch(typeof(Trapper.EvilAbility).GetPrivateMethodInfoType("LocalUpdate"), new HarmonyMethod(typeof(PatchManager).GetMethod("EvilTrapperUpdate")));
            harmony.Patch(typeof(Trapper.Trap).GetPrivateMethodInfoType("Update"), new HarmonyMethod(typeof(PatchManager).GetMethod("TrapUpdate")));
            harmony.Patch(typeof(PolusData).GetMethod("get_SabotageTypes", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SabotageTypesFix")));
            harmony.Patch(typeof(StampHelpers).GetMethod("TryShowStampRingMenu"), new HarmonyMethod(typeof(PatchManager).GetMethod("TryShowStampRingMenu")));
            harmony.Patch(typeof(Jackal.Instance).GetMethod("OnGameEnd", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("JackalGameEnd")));
            string activated = typeof(RuntimeAssignable).FullName + ".OnActivated";
            harmony.Patch(typeof(Sidekick.Instance).GetMethod(activated, AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SidekickMeetingCheckJackal")));
            harmony.Patch(typeof(SidekickModifier.Instance).GetMethod(activated, AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SidekickModifierMeetingCheckJackal")));
            harmony.Patch(typeof(EmergencyMinigame).GetMethod("Update"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("CallMeetingCheck")));
            harmony.Patch(typeof(NebulaGameManager).GetMethod("OnGameStart"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("NebulaGameStart")));
            harmony.Patch(typeof(FreePlayRoleAllocator).GetMethod("Assign"), new HarmonyMethod(typeof(PatchManager).GetMethod("AssignGameMasterPre")));
            harmony.Patch(typeof(StandardRoleAllocator).GetMethod("Assign"), new HarmonyMethod(typeof(PatchManager).GetMethod("AssignGameMasterPre")));
            harmony.Patch(typeof(RoleTable).GetMethod("Determine"), new HarmonyMethod(typeof(PatchManager).GetMethod("DetermineGameMasterPre")));
            harmony.Patch(typeof(MetaAbility).GetConstructor(new Type[0]), new HarmonyMethod(typeof(PatchManager).GetMethod("GameMasterBlockFreePlayAbility")));
            harmony.Patch(typeof(PlayerControl).GetMethod("AdjustLighting"), new HarmonyMethod(typeof(PatchManager).GetMethod("AdjustLightingPatch")));
            harmony.Patch(typeof(GameData).GetMethod("ShowNotification", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("ShowNotification")));
            harmony.Patch(typeof(CheckForEndVotingPatch).GetMethod("ModCalculateVotes"), new HarmonyMethod(typeof(Plana.Roles.Crewmate.SwapSystem).GetMethod("ModCalculateVotesPrefix")));
            harmony.Patch(typeof(MeetingHud).GetMethod("PopulateResults"), new HarmonyMethod(typeof(Plana.Roles.Crewmate.SwapSystem).GetMethod("ShowSwapAnim")));
            harmony.Patch(typeof(HelpScreen).GetMethod("ShowMyRolesSrceen", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("ShowMyRolesScreen")));
            harmony.Patch(typeof(Destroyer.Ability).GetConstructor([typeof(GamePlayer), typeof(bool)]), null, new HarmonyMethod(typeof(PatchManager).GetMethod("RpcDestroyerPatch")));
            string decorateName = typeof(RuntimeAssignable).FullName + ".DecorateNameConstantly";
            harmony.Patch(typeof(Scarlet.Instance).GetMethod(decorateName, AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("ScarletNameColorPatch")));
            harmony.Patch(typeof(ScarletLover.Instance).GetMethod(decorateName, AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("ScarletLoverNameColorPatch")));
            harmony.Patch(typeof(Snatcher.Ability).GetMethod("OnSnatching", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("SnatchPatch")));
            harmony.Patch(typeof(OracleSystem).GetMethod("IsNeutralRole", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("GetNeutralRolePatch")));
            harmony.Patch(typeof(Jackal.Instance).GetMethod("OnDead", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("JackalPromotePatch")));
            harmony.Patch(typeof(TaskPanelBehaviour).GetMethod("Update", AccessTools.all), null, new HarmonyMethod(typeof(PatchManager).GetMethod("TaskPanelPosPatch")));
            harmony.Patch(typeof(Language).GetMethod("Translate"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("TranslatePatch")));
            harmony.Patch(typeof(PlayerModInfo).GetMethod(typeof(GamePlayer).FullName + ".MurderPlayer", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("ModKillPatch")));
            harmony.Patch(typeof(MetaScreen).GetMethod("GenerateWindow", new Type[]
            {
                typeof(Vector2),typeof(Transform),typeof(Vector3),typeof(bool),typeof(bool),typeof(bool),typeof(BackgroundSetting)
            }), new HarmonyMethod(typeof(PatchManager).GetMethod("ModernBGPatch")));
            harmony.Patch(typeof(ShipStatus).GetMethod("UpdateSystem", new Type[] { typeof(SystemTypes), typeof(PlayerControl), typeof(byte) }), null, new HarmonyMethod(typeof(PatchManager).GetMethod("HostRecordInvokeSabotageRpc")));
            harmony.Patch(typeof(ShipStatus).GetMethod("UpdateSystem", new Type[] { typeof(SystemTypes), typeof(PlayerControl), typeof(MessageReader) }), new HarmonyMethod(typeof(PatchManager).GetMethod("ClientRecordInvokeSabotageRpc")));
            harmony.Patch(typeof(NebulaGameManager).GetMethod("OnUpdate"), null, new HarmonyMethod(typeof(Engineer.Instance).GetMethod("EngineerVentTextPatch")));
            harmony.Patch(typeof(TimerImpl).GetMethod("SetAsAbilityCoolDown"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("AbilityTimerFix")));
            harmony.Patch(typeof(ShapeshifterMinigame).GetMethod("Shapeshift"), new HarmonyMethod(typeof(Assistant.Instance).GetMethod("OnShapeshiftPanelSelected")));
            harmony.Patch(typeof(LogicOptionsNormal).GetMethod("GetAnonymousVotes"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("AnonymousVotesFix")));
            harmony.Patch(typeof(PopulateResultPatch).GetMethod("Prefix"), new HarmonyMethod(typeof(PatchManager).GetMethod("ResultPrefix")));
            harmony.Patch(typeof(PopulateResultPatch).GetMethod("ModBloopAVoteIcon", AccessTools.all), new HarmonyMethod(typeof(PatchManager).GetMethod("ModBloopAVoteIconPatch")));
            harmony.Patch(typeof(CreateGameOptions).GetMethod("Confirm"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("CreateGameOptionsLoadingPatch")));
            harmony.Patch(typeof(AmongUsClient).GetMethod("CoJoinOnlineGameFromListing"), null, new HarmonyMethod(typeof(PatchManager).GetMethod("JoinGameLoadingPatch")));
            /*harmony.Patch(typeof(DefinedSingleAssignableTemplate).GetConstructor(new Type[]{typeof(string),typeof(Virial.Color),typeof(RoleCategory),typeof(RoleTeam),
            typeof(bool),typeof(ConfigurationTab),typeof(Func<bool>),typeof(Func<AllocationParameters.ExtraAssignmentInfo[]>),typeof(Func<AllocationParameters.ExtraAssignmentInfo[]>) })
                , null, new HarmonyMethod(typeof(NebulaOptionPatchs).GetMethod("MoriartizedPatch")));*/
            OptionPatchs.StartPatch(harmony);
            harmony.PatchAll(typeof(DarkThemePatchs));
            harmony.PatchAll(typeof(IntroPatchs));
            harmony.PatchAll(typeof(NebulaOptionPatchs));
            harmony.PatchAll(typeof(DiagnosisModePatchs));
            harmony.PatchAll(typeof(BetterStandardRoleAllocator));
            SpeechEater.LoadPatch(harmony);
            Splicer.LoadPatch(harmony);
            Scrambler.LoadPatch(harmony);
            Crow.LoadPatch(harmony);
            CrawlerEngineer.LoadPatch(harmony);
            NinjaFix.LoadPatch(harmony);
            Lover.MyRole.ConfigurationHolder!.AppendConfiguration(ChatOption);
            Lover.MyRole.ConfigurationHolder!.AppendConfiguration(LoverDisConnectedRemoveLover);
            Sheriff.MyRole.ConfigurationHolder!.AppendConfiguration(OnPlayerDeadSheriffUnlockKill);
            Sheriff.MyRole.ConfigurationHolder!.AppendConfiguration(SheriffCanKillMadmateModifierOption);
            Sheriff.MyRole.ConfigurationHolder!.AppendConfiguration(SheriffCanKillSidekickModifierOption);
            Doctor.MyRole.ConfigurationHolder!.AppendConfiguration(DoctorCanUseShieldOption);
            Doctor.MyRole.ConfigurationHolder!.AppendConfiguration(DoctorShieldNoBlockGuessedOption);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(AssignHostOpOption);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(HostIsGameMaster);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(CanGenerateInfected);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(GenerateIfPlayer);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(InfectedHasGuesser);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(InfectedGuessNum);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(InfectedSetRoleImmediately);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(CanGenerateLastImpostor);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(GenerateLastImpostorIfPlayer);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(CannotGenerateHasInfected);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(LastImpostorClockRatioOption);
            Sidekick.MyRole.ConfigurationHolder!.AppendConfiguration(Sidekick.CanWinAsOriginalTeamOption);
            Sidekick.MyRole.ConfigurationHolder!.AppendConfiguration(SidekickCanUseVent);
            ExOptions.AppendConfiguration(IntroShowImpostor);
            GeneralConfigurations.PerkOptions.AppendConfiguration(GameStartGrowALLFlower);
            ExOptions.AppendConfiguration(ImpostorCannotMoveTime);
            ExOptions.AppendConfiguration(CrewmateHasEchoSkill);
            ExOptions.AppendConfiguration(EchoSkillUseTime);
            ExOptions.AppendConfiguration(CannotReport);
            ExOptions.AppendConfiguration(HasHNSMusic);
            ExOptions.AppendConfiguration(HasFinal);
            ExOptions.AppendConfiguration(EnterFinalLeftTaskNum);
            ExOptions.AppendConfiguration(FinalImpostorSpeedUpOption);
            ExOptions.AppendConfiguration(FinalImpostorScanCooldown);
            ExOptions.AppendConfiguration(ShowTaskNum);
            ExOptions.AppendConfiguration(HasFreeChat);
            ExOptions.AppendConfiguration(ImpostorMustKillAllToWin);
            ExOptions.AppendConfiguration(CanSendAdminMessage);
            ExOptions.AppendConfiguration(UseFlashLightMode);
            ExOptions.AppendConfiguration(CrewmateFlashSize);
            ExOptions.AppendConfiguration(ImpostorFlashSize);
            ExOptions.AppendConfiguration(ImpostorHasVitals);
            ExOptions.AppendConfiguration(CrewmateHasVitals);
            ExOptions.AppendConfiguration(CanSeePlayerState);
            ExOptions.AppendConfiguration(CanSeeAllPlayerDead);
            ExOptions.AppendConfiguration(JackalMeetingChat);
            ExOptions.AppendConfiguration(LoverMeetingChat);
            ExOptions.AppendConfiguration(MoriartyMeetingChat);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(DesperateAssignToImp);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(DesperateDoNotAssignToLoverImp);
            GeneralConfigurations.AssignmentOptions.AppendConfiguration(DiagnosisOptions);
            Sniper.MyRole.ConfigurationHolder!.AppendConfiguration(SniperCanUseNormalKill);
            Sniper.MyRole.ConfigurationHolder!.AppendConfiguration(SniperCanLockMini);
            Raider.MyRole.ConfigurationHolder!.AppendConfiguration(RaiderCanUseNormalKill);
            GeneralConfigurations.MapOptions.AppendConfiguration(PolusHasOxygenSabotage);
            GeneralConfigurations.MapOptions.AppendConfiguration(PolusOxygenSabotageTime);
            GeneralConfigurations.MapOptions.AppendConfiguration(CommSabCamouflagerEffectActive);
            GeneralConfigurations.MapOptions.AppendConfiguration(CommSabRandomCosmic);
            GeneralConfigurations.MapOptions.AppendConfiguration(TimeLimit);
            GeneralConfigurations.MapOptions.AppendConfiguration(TimeLimitMin);
            Navvy.MyRole.ConfigurationHolder.AppendConfiguration(NavvyCanUseCamera);
            Psychic.MyRole.ConfigurationHolder!.AppendConfiguration(PsychicSendMessageMode);
            Collator.MyRole.ConfigurationHolder!.AppendConfiguration(collatorSendMessageMode);
            Sidekick.MyRole.ConfigurationHolder.AppendConfiguration(SidekickPromoteToJackalNoJackalizedRole);
            Sidekick.MyRole.ConfigurationHolder.AppendConfiguration(SidekickPromoteToJackalRandomJackalizedRole);
            Jackal.MyRole.ConfigurationHolder!.AppendConfiguration(JackalCanInvokeSabotage);
            Jackal.MyRole.ConfigurationHolder!.AppendConfiguration(NoAliveImpostorSabotageJackalWin);
            Jackal.MyRole.ConfigurationHolder!.AppendConfiguration(JackalUseOldPromoteSidekick);
            IEnumerable<IConfiguration> options = [AccelTrapCost, AccelTrapOnlyCrewmate, DecelTrapCost, DecelTrapOnlyImpostor, PlaceTrapNoMeetingStart];
            Trapper.MyNiceRole.ConfigurationHolder!.AppendConfigurations(options);
            Trapper.MyEvilRole.ConfigurationHolder!.AppendConfigurations(options);
            Trapper.MyEvilRole.ConfigurationHolder!.AppendConfiguration(EvilTrapperKillTrapPLUS);
            Jester.MyRole.ConfigurationHolder!.AppendConfiguration(JesterCanCallMeeting);
            Guesser.MyNiceRole.ConfigurationHolder!.AppendConfigurations([GuesserCanGuessNoSpawnableRole,GuesserAbilityGuessMode]);
            Guesser.MyEvilRole.ConfigurationHolder!.AppendConfigurations([GuesserCanGuessNoSpawnableRole,GuesserAbilityGuessMode]);
            GuesserModifier.MyRole.ConfigurationHolder!.AppendConfigurations([GuesserCanGuessNoSpawnableRole,GuesserAbilityGuessMode]);
            Destroyer.MyRole.ConfigurationHolder!.AppendConfiguration(InVisibleDestroyCanMove);
            Snatcher.MyRole.ConfigurationHolder!.AppendConfiguration(SnatcherCannotGuessSnatchedPlayer);
            Mayor.MyRole.ConfigurationHolder!.AppendConfiguration(MayorExtraVoteAnonymousVote);
            GeneralConfigurations.ExileOptions.AppendConfiguration(ExileShowEvilModifier);
            GeneralConfigurations.ExileOptions.AppendConfiguration(ExileShowAllModifier);
            new ClientOption((ClientOption.ClientOptionType)123, "useairshipSound", ["options.switch.off", "options.switch.on"], 0);
            new ClientOption((ClientOption.ClientOptionType)124, "darktheme", ["options.switch.off", "options.switch.on"], 0);
            new ClientOption((ClientOption.ClientOptionType)125, "ggdGuesserSound", ["options.switch.off", "options.switch.on"], 0);
            new ClientOption((ClientOption.ClientOptionType)126, "mapuseRoleColor", ["options.switch.off", "options.switch.on"], 0);
            new ClientOption((ClientOption.ClientOptionType)127, "alluiUseModern", ["options.switch.off", "options.switch.on"], 1);
            new ClientOption((ClientOption.ClientOptionType)128, "loadingAnim", ["options.switch.off", "options.switch.on"], 1);
            GeneralConfigurations.AssignmentOptions.ScheduleAddRelated(() => [Desperate.MyRole.ConfigurationHolder!]);
            //Jackal.MyRole.ConfigurationHolder.Illustration = NebulaAPI.AddonAsset.GetResource("JackalOption.png")!.AsImage();
            Hint WithImage(string id)
            {
                return new HintWithImage(NebulaAPI.AddonAsset.GetResource("Hints/" + id.HeadUpper() + ".png")!.AsImage()!, new TranslateTextComponent("hint." + id.HeadLower() + ".title"), new TranslateTextComponent("hint." + id.HeadLower() + ".detail"));
            }
            HintManager.RegisterHint(WithImage("AddonDoctor"));
            HintManager.RegisterHint(WithImage("Skinner"));
            HintManager.RegisterHint(WithImage("KindnessJackal"));
            /*NebulaAPI.Preprocessor!.SchedulePreprocess(PreprocessPhase.BuildAssignmentTypes, () =>
            {
                LoadAddonRolesAssignmentTypes();
            });*/
            PDebug.Log("Done!");
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }


    public static Team DreamweaverAndInsomniacsTeam = new Team("teams.DreamweaverAndInsomniacs", new(216, 164, 246), TeamRevealType.OnlyMe);
    public static Team GameMasterTeam = new Team("teams.gm", new(255, 91, 112), TeamRevealType.OnlyMe);
    public static Team ViolatorTeam = new Team("teams.violator", new(255, 91, 112), TeamRevealType.Everyone);
    public static TranslatableTag Refiner = new TranslatableTag("state.refiner");
    public static TranslatableTag DesperateDead = new TranslatableTag("state.desperate");
    public static TranslatableTag Regret = new TranslatableTag("state.regret");
    public static TranslatableTag baddream = new TranslatableTag("state.baddream");
    static BoolConfiguration ChatOption = NebulaAPI.Configurations.Configuration("options.role.lover.ChatActive", false);
    static BoolConfiguration SheriffCanKillMadmateModifierOption = NebulaAPI.Configurations.Configuration("options.role.sheriff.cankillmadmatemodifier", true);
    static BoolConfiguration SheriffCanKillSidekickModifierOption = NebulaAPI.Configurations.Configuration("options.role.sheriff.cankillsidekickmodifier", true);
    static BoolConfiguration DoctorCanUseShieldOption = NebulaAPI.Configurations.Configuration("options.role.doctor.canuseshield", true);
    static BoolConfiguration DoctorShieldNoBlockGuessedOption = NebulaAPI.Configurations.Configuration("options.role.doctor.shieldNoblockGuessed", false, () => DoctorCanUseShieldOption);
    public static BoolConfiguration CanGenerateInfected = NebulaAPI.Configurations.Configuration("options.assignment.canGenerateInfected", false);
    public static IntegerConfiguration GenerateIfPlayer = NebulaAPI.Configurations.Configuration("options.assignment.Genifplayer", (1, 16), 10, () => CanGenerateInfected);
    public static BoolConfiguration InfectedHasGuesser = NebulaAPI.Configurations.Configuration("options.assignment.InfectedHasGuesser", true, () => CanGenerateInfected);
    public static IntegerConfiguration InfectedGuessNum = NebulaAPI.Configurations.Configuration("options.assignment.InfectedGuessNum", (1, 16), 2, () => CanGenerateInfected);
    public static BoolConfiguration InfectedSetRoleImmediately = NebulaAPI.Configurations.Configuration("options.assignment.InfectedSetRoleImmediately", false, () => CanGenerateInfected);
    public static BoolConfiguration CanGenerateLastImpostor = NebulaAPI.Configurations.Configuration("options.assignment.canGenerateLastImpostor", false);
    public static IntegerConfiguration GenerateLastImpostorIfPlayer = NebulaAPI.Configurations.Configuration("options.assignment.GenLastImpostorifPlayer", (1,16),6, () => CanGenerateLastImpostor);
    public static BoolConfiguration CannotGenerateHasInfected = NebulaAPI.Configurations.Configuration("options.assignment.cannotGenerateLastImpostorHasInfected", false, () => CanGenerateInfected&&CanGenerateLastImpostor);
    public static FloatConfiguration LastImpostorClockRatioOption = NebulaAPI.Configurations.Configuration("options.role.lastimpostor.clockRatio", new ValueTuple<float, float, float>(1f, 5f, 0.125f), 1.5f, FloatConfigurationDecorator.Ratio, ()=>CanGenerateLastImpostor);
    static BoolConfiguration SidekickCanUseVent = NebulaAPI.Configurations.Configuration("options.role.sidekick.canusevent", false);
    static BoolConfiguration IntroShowImpostor = NebulaAPI.Configurations.Configuration("options.general.showImpostor", false);
    static BoolConfiguration GameStartGrowALLFlower = NebulaAPI.Configurations.Configuration("options.perk.glowallflower", false);
    public static IConfigurationHolder ExOptions = NebulaAPI.Configurations.Holder("options.extra", [ConfigurationTab.Settings], [Virial.Game.GameModes.FreePlay, Virial.Game.GameModes.Standard]);
    static BoolConfiguration CrewmateHasEchoSkill = NebulaAPI.Configurations.Configuration("options.general.CrewmateHasEcho", false, () => IntroShowImpostor);
    static FloatConfiguration EchoSkillUseTime = NebulaAPI.Configurations.Configuration("options.general.echoskillusetime", (2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second, () => IntroShowImpostor);
    static BoolConfiguration HasHNSMusic = NebulaAPI.Configurations.Configuration("options.general.HasHNSMusic", false, () => IntroShowImpostor);
    static BoolConfiguration CannotReport = NebulaAPI.Configurations.Configuration("options.general.CannotReport", false, () => IntroShowImpostor);
    static BoolConfiguration HasFinal = NebulaAPI.Configurations.Configuration("options.general.hasfinal", false, () => IntroShowImpostor);
    static IntegerConfiguration EnterFinalLeftTaskNum = NebulaAPI.Configurations.Configuration("options.general.enterfinalLefttasknm", (1, 99), 12, () => IntroShowImpostor && HasFinal);
    static BoolConfiguration ShowTaskNum = NebulaAPI.Configurations.Configuration("options.general.ShowTaskNum", false);
    static BoolConfiguration HasFreeChat = NebulaAPI.Configurations.Configuration("options.general.hasfreechat", false);
    static BoolConfiguration ImpostorMustKillAllToWin = NebulaAPI.Configurations.Configuration("options.general.impostorMustKillAllToWin", false);
    static FloatConfiguration FinalImpostorScanCooldown = NebulaAPI.Configurations.Configuration("options.general.FinalImpostorScanCooldown", (0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second, () => IntroShowImpostor);
    static FloatConfiguration ImpostorCannotMoveTime = NebulaAPI.Configurations.Configuration("options.general.ImpostorCannotMoveTime", (0f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second, () => IntroShowImpostor);
    static FloatConfiguration FinalImpostorSpeedUpOption = NebulaAPI.Configurations.Configuration("options.general.FinalImpotorSpeedUp", (0f, 10f, 0.125f), 0.125f, FloatConfigurationDecorator.Ratio, () => IntroShowImpostor && HasFinal);
    static BoolConfiguration ImpostorHasVitals = NebulaAPI.Configurations.Configuration("options.general.ImpostorHasVitals", false);
    static BoolConfiguration CrewmateHasVitals = NebulaAPI.Configurations.Configuration("options.general.CrewmateHasVitals", false);
    static BoolConfiguration CanSeePlayerState = NebulaAPI.Configurations.Configuration("options.general.CanSeePlayerState", false);
    public static BoolConfiguration CanSeeAllPlayerDead = NebulaAPI.Configurations.Configuration("options.general.CanSeeAllPlayerDead", false);
    static BoolConfiguration UseFlashLightMode = NebulaAPI.Configurations.Configuration("options.general.UseFlashLight", false);
    static FloatConfiguration CrewmateFlashSize = NebulaAPI.Configurations.Configuration("options.general.crewmateFlashSize", (0f, 5f, 0.05f), 0.35f, (val) => val.ToString("F2") + Language.Translate("options.cross"), () => UseFlashLightMode);
    static FloatConfiguration ImpostorFlashSize = NebulaAPI.Configurations.Configuration("options.general.impostorFlashSize", (0f, 5f, 0.05f), 0.25f, (val) => val.ToString("F2") + Language.Translate("options.cross"), () => UseFlashLightMode);
    static BoolConfiguration RaiderCanUseNormalKill = NebulaAPI.Configurations.Configuration("options.raider.CanUseNormalKill", false);
    static BoolConfiguration SniperCanUseNormalKill = NebulaAPI.Configurations.Configuration("options.sniper.CanUseNormalKill", false);
    public static BoolConfiguration SniperCanLockMini = NebulaAPI.Configurations.Configuration("options.sniper.CanLockMini", false);
    static BoolConfiguration PolusHasOxygenSabotage = NebulaAPI.Configurations.Configuration("options.map.PolusHasOxygenSab", true);
    static FloatConfiguration PolusOxygenSabotageTime = NebulaAPI.Configurations.Configuration("options.map.PolusOxygenSabTime", (0f, 120f, 2.5f), 45f, FloatConfigurationDecorator.Second, () => PolusHasOxygenSabotage);
    static BoolConfiguration CanSendAdminMessage = NebulaAPI.Configurations.Configuration("options.general.cansendAdminMessage", true);
    static BoolConfiguration OnPlayerDeadSheriffUnlockKill = NebulaAPI.Configurations.Configuration("options.sheriff.playerdeadunlockkill", false);
    static BoolConfiguration NavvyCanUseCamera = NebulaAPI.Configurations.Configuration("options.navvy.canusecamera", true);
    static BoolConfiguration LoverDisConnectedRemoveLover = NebulaAPI.Configurations.Configuration("options.lover.DCRemoveLover", true);
    public static BoolConfiguration DesperateAssignToImp = NebulaAPI.Configurations.Configuration("options.desperate.AssignToAllImpostor", false);
    static BoolConfiguration DesperateDoNotAssignToLoverImp = NebulaAPI.Configurations.Configuration("options.desperate.DoNotAssignToLoverImpostor", false, () => DesperateAssignToImp);
    static BoolConfiguration AssignHostOpOption = NebulaAPI.Configurations.Configuration("options.general.AssignHostOp", true, () => !GeneralConfigurations.AssignOpToHostOption);
    static BoolConfiguration SidekickPromoteToJackalRandomJackalizedRole = NebulaAPI.Configurations.Configuration("options.role.sidekick.promoteRandomJackalizedRole", false, () => Jackal.JackalizedImpostorOption && SidekickPromoteToJackalNoJackalizedRole != null && !SidekickPromoteToJackalNoJackalizedRole);
    static BoolConfiguration SidekickPromoteToJackalNoJackalizedRole = NebulaAPI.Configurations.Configuration("options.role.sidekick.promoteNoJackalizedRole", false, () => Jackal.JackalizedImpostorOption && !SidekickPromoteToJackalRandomJackalizedRole);
    static BoolConfiguration PsychicSendMessageMode = NebulaAPI.Configurations.Configuration("options.role.psychic.sendmessagemode", false);
    static BoolConfiguration collatorSendMessageMode = NebulaAPI.Configurations.Configuration("options.role.collator.sendmessagemode", false);
    public static BoolConfiguration EvilTrapperKillTrapPLUS = NebulaAPI.Configurations.Configuration("options.role.trapper.EvilTrapperKillTrapPLUS", false);
    public static BoolConfiguration AccelTrapOnlyCrewmate = NebulaAPI.Configurations.Configuration("options.role.trapper.AccelTrapOnlyCrewmate", false);
    public static BoolConfiguration DecelTrapOnlyImpostor = NebulaAPI.Configurations.Configuration("options.role.trapper.DecelTrapOnlyImpostor", false);
    public static BoolConfiguration PlaceTrapNoMeetingStart = NebulaAPI.Configurations.Configuration("options.role.trapper.placeTrapNoMeetingStart", false);
    public static IntegerConfiguration AccelTrapCost = NebulaAPI.Configurations.Configuration("options.role.trapper.AccelTrapCost", (1, 5), 1);
    public static IntegerConfiguration DecelTrapCost = NebulaAPI.Configurations.Configuration("options.role.trapper.DecelTrapCost", (1, 5), 1);
    static BoolConfiguration JesterCanCallMeeting = NebulaAPI.Configurations.Configuration("options.role.jester.cancallmeeting", true);
    static BoolConfiguration TimeLimit = NebulaAPI.Configurations.Configuration("options.map.general.timelimit", false);
    static IntegerConfiguration TimeLimitMin = NebulaAPI.Configurations.Configuration("options.map.general.timelimit.min", (1, 120), 35, () => TimeLimit);
    static BoolConfiguration HostIsGameMaster = NebulaAPI.Configurations.Configuration("options.assignments.hostisGM", false);
    static BoolConfiguration InVisibleDestroyCanMove = NebulaAPI.Configurations.Configuration("options.role.destroyer.invisibledestroycanmove", false);
    static BoolConfiguration SnatcherCannotGuessSnatchedPlayer = NebulaAPI.Configurations.Configuration("options.role.snatcher.cannotguessSnatchedPlayer", false);
    static BoolConfiguration JackalUseOldPromoteSidekick = NebulaAPI.Configurations.Configuration("options.role.jackal.useoldpromotesidekick", false);
    public static BoolConfiguration GuesserCanGuessNoSpawnableRole = NebulaAPI.Configurations.Configuration("options.role.guesser.cannotguessnospwanableRole", true);
    static BoolConfiguration JackalCanInvokeSabotage = NebulaAPI.Configurations.Configuration("options.role.jackal.canInvokeSabotage", false);
    static BoolConfiguration NoAliveImpostorSabotageJackalWin = NebulaAPI.Configurations.Configuration("options.role.jackal.noAliveImpostorSabotageWin", false, () => JackalCanInvokeSabotage);
    public static BoolConfiguration ImpostorMeetingChat = NebulaAPI.Configurations.Configuration("options.general.impMeetingChat", false);
    static BoolConfiguration JackalMeetingChat = NebulaAPI.Configurations.Configuration("options.general.jackalMeetingChat", false);
    static BoolConfiguration LoverMeetingChat = NebulaAPI.Configurations.Configuration("options.general.LoverMeetingChat", false);
    static BoolConfiguration MoriartyMeetingChat = NebulaAPI.Configurations.Configuration("options.general.moriartyMeetingChat", false);
    static BoolConfiguration CanInvokeSpecialMeeting = NebulaAPI.Configurations.Configuration("options.diagnosis.caninvokespecialMeeting", false,()=>AmongUsClient.Instance?.AmHost??false);
    static BoolConfiguration DiagnosisIsActive = NebulaAPI.Configurations.Configuration("options.diagnosis.IsActive", false,()=>(AmongUsClient.Instance?.AmHost??false)&&CanInvokeSpecialMeeting);
    static IntegerConfiguration DiagnosisChance = NebulaAPI.Configurations.Configuration("options.diagnosis.chance", new ValueTuple<int,int,int>(0,100,10),0,()=>(AmongUsClient.Instance?.AmHost??false)&&CanInvokeSpecialMeeting&&DiagnosisIsActive,val=>val.ToString()+"%");
    static FloatConfiguration StartKillTime = NebulaAPI.Configurations.Configuration("options.diagnosis.startkilltime", (0f,60f,2.5f),17.5f,FloatConfigurationDecorator.Second, () => (AmongUsClient.Instance?.AmHost ?? false) && CanInvokeSpecialMeeting && DiagnosisIsActive);
    static IntegerConfiguration KillChance = NebulaAPI.Configurations.Configuration("options.diagnosis.killchance", new ValueTuple<int, int, int>(0, 100, 10), 40, () => (AmongUsClient.Instance?.AmHost ?? false) && CanInvokeSpecialMeeting && DiagnosisIsActive, val => val.ToString() + "%");
    static GroupConfiguration DiagnosisOptions = new GroupConfiguration("options.diagnosis.group", [CanInvokeSpecialMeeting, DiagnosisIsActive, DiagnosisChance, StartKillTime, KillChance], ViolatorTeam.Color.ToUnityColor(),()=>AmongUsClient.Instance?.AmHost??false);
    static BoolConfiguration ExileShowEvilModifier = NebulaAPI.Configurations.Configuration("options.exile.showevilmodifier", false,()=>ExileShowAllModifier!=null&&!ExileShowAllModifier);
    static BoolConfiguration ExileShowAllModifier = NebulaAPI.Configurations.Configuration("options.exile.showallmodifier", false,()=>!ExileShowEvilModifier);
    static BoolConfiguration MayorExtraVoteAnonymousVote = NebulaAPI.Configurations.Configuration("options.role.mayor.extravoteAnonymous", false);
    public static BoolConfiguration GuesserAbilityGuessMode = NebulaAPI.Configurations.Configuration("options.role.guesser.abilityguessmode",true);
    static Virial.Media.Image doctorshieldimage = NebulaAPI.AddonAsset.GetResource("DoctorShield.png")!.AsImage(115f)!;
    static Virial.Media.Image globalscanimage = NebulaAPI.AddonAsset.GetResource("GlobalScan.png")!.AsImage(115f)!;
    static GamePlayer? selectPlayer, DoctorPlayer;
    static ModAbilityButton? scanbutton;
    static SpriteRenderer? redFullS;
    static bool isfinal;
    static Minigame? navvyMiniGame;
    public static GameEnd SkeldEnd = NebulaAPI.Preprocessor!.CreateEnd("timeout.skeld", new(0.28235295f, 0.30588236f, 0.32941177f), 255);
    public static GameEnd MiraEnd = NebulaAPI.Preprocessor!.CreateEnd("timeout.mira", new(0.28235295f, 0.30588236f, 0.32941177f), 255);
    public static GameEnd PolusEnd = NebulaAPI.Preprocessor!.CreateEnd("timeout.polus", new(0.28235295f, 0.30588236f, 0.32941177f), 255);
    public static GameEnd AirshipEnd = NebulaAPI.Preprocessor!.CreateEnd("timeout.airship", new(0.28235295f, 0.30588236f, 0.32941177f), 255);
    public static GameEnd FungleEnd = NebulaAPI.Preprocessor!.CreateEnd("timeout.fungle", new(0.28235295f, 0.30588236f, 0.32941177f), 255);
    public static GameEnd NobodyAliveEnd = NebulaAPI.Preprocessor!.CreateEnd("nobodyalive", new(0.28235295f, 0.30588236f, 0.32941177f), 255);
    static bool FlashlightEnabled = false;
    [HarmonyPatch]
    public class BetterStandardRoleAllocator
    {
        static List<(byte player, DefinedRole role, int chance)> RoleUpList = new List<(byte player, DefinedRole role, int chance)>();
        static List<(byte player, RoleCategory role, int chance)> CategoryUpList = new List<(byte player, RoleCategory role, int chance)>();
        public static void Reload()
        {
            RoleUpList.Clear();
            CategoryUpList.Clear();
        }
        public static void AddRoleUp(byte player,DefinedRole role,int chance=100)
        {
            if (RoleUpList.Any(r=>r.player==player)||CategoryUpList.Any(r=>r.player==player))
            {
                PDebug.Log("Player " + player.ToString() + " Added UpList");
                return;
            }
            RoleUpList.Add(ValueTuple.Create(player, role, chance));
        }
        public static void AddCategoryUp(byte player, RoleCategory c, int chance = 100)
        {
            if (RoleUpList.Any(r => r.player == player) || CategoryUpList.Any(r => r.player == player))
            {
                PDebug.Log("Player " + player.ToString() + " Added UpList");
                return;
            }
            CategoryUpList.Add(ValueTuple.Create(player, c, chance));
        }
        public record BetterRoleChance(DefinedRole role, AllocationParameters? param = null)
        {
            public int count = (param ?? role.AllocationParameters)?.RoleCountSum ?? 0;
            public int left = (param ?? role.AllocationParameters)?.RoleCountSum ?? 0;
            public int cost = (param ?? role.AllocationParameters)?.TeamCost ?? 1;
            public int otherCost = (param ?? role.AllocationParameters)?.OtherCost ?? 1;
            public AllocationParameters? Param => param ?? role.AllocationParameters;
        }

        public static void OnSetRole(IRoleAllocator allocator,DefinedRole role, params List<BetterRoleChance>[] pool)
        {
            foreach (var remove in GeneralConfigurations.exclusiveAssignmentOptions.Select(e => e.OnAssigned(role)))
            {
                foreach (var removeRole in remove)
                {
                    foreach (var p in pool)
                    {
                        p.RemoveAll(r => r.role == removeRole);
                    }
                }
            }
            GameOperatorManager.Instance?.Run<RoleAllocatorSetRoleEvent>(new RoleAllocatorSetRoleEvent(allocator, role, delegate (DefinedRole role)
            {
                foreach (List<BetterRoleChance> p in pool)
                {
                    p.RemoveAll(r => r.role == role);
                }
                
            }), false);
        }


        public static void CategoryAssign(IRoleAllocator allocator,IRoleTable rtable, int left, List<byte> main, List<byte> others, List<BetterRoleChance> rolePool, List<BetterRoleChance>[] allRolePool, Action<DefinedRole, byte>? onSelected = null)
        {
            RoleTable table = (rtable as RoleTable)!;
            if (left < 0) left = 15;
            void OnSelected(BetterRoleChance selected)
            {
                if (onSelected != null)
                {
                    onSelected.Invoke(selected.role, main[0]);
                    main.RemoveAt(0);
                }
                else
                {
                    var player = main[0];
                    table.SetRole(main[0], selected.role);
                    main.RemoveAt(0);

                    selected.Param?.TeamAssignment?.Do(assignment =>
                    {
                        var param = assignment.Assigner.Invoke(selected.role, player);
                        table.SetRole(main[0], param.role, param.argument);
                        OnSetRole(allocator,selected.role, allRolePool);
                        main.RemoveAt(0);
                    });

                    selected.Param?.OthersAssignment?.Do(assignment =>
                    {
                        var param = assignment.Assigner.Invoke(selected.role, player);
                        table.SetRole(others[0], param.role, param.argument);
                        OnSetRole(allocator,selected.role, allRolePool);
                        others.RemoveAt(0);
                    });
                }

                left -= selected.cost;

                //割り当て済み役職を排除
                selected.left--;
                if (selected.left == 0) rolePool.Remove(selected);

                //排他的割り当てを考慮
                OnSetRole(allocator,selected.role, allRolePool);
            }

            bool left100Roles = true;
            while (main.Count > 0 && left > 0 && rolePool.Count > 0)
            {
                //コスト超過によって割り当てられない役職を弾く
                rolePool.RemoveAll(c =>
                {
                    if (main == others)
                        return c.cost + c.otherCost > left && c.cost + c.otherCost > main.Count;
                    else
                        return c.cost > left && c.cost > main.Count && c.otherCost > others.Count;
                });

                //100%割り当て役職が残っている場合
                if (left100Roles)
                {
                    var roles100 = rolePool.Where(r => r.Param!.GetRoleChance(r.count - r.left + 1) == 100f);
                    if (roles100.Any(r => true))
                    {
                        //役職を選択する
                        OnSelected(roles100.ToArray().Random());
                        continue;
                    }
                    else
                    {
                        left100Roles = false;
                    }
                }

                //100%役職がもう残っていない場合
                var sum = rolePool.Sum(r => r.Param!.GetRoleChance(r.count - r.left + 1));
                var random = System.Random.Shared.NextSingle() * sum;
                foreach (var r in rolePool)
                {
                    random -= r.Param!.GetRoleChance(r.count - r.left + 1);
                    if (random < 0f)
                    {
                        //役職を選択する
                        OnSelected(r);
                        break;
                    }
                }
            }
        }
        public static void SingleSetRole(IRoleAllocator allocator,IRoleTable rtable,byte player, List<byte> main, List<byte> others, BetterRoleChance targetRole, List<BetterRoleChance> rolePool, List<BetterRoleChance>[] allRolePool)
        {
            RoleTable table = (rtable as RoleTable)!;
            void OnSelected(BetterRoleChance selected)
            {
                table.SetRole(player, selected.role);
                selected.Param?.TeamAssignment?.Do(assignment =>
                {
                    var param = assignment.Assigner.Invoke(selected.role, player);
                    table.SetRole(main[0], param.role, param.argument);
                    OnSetRole(allocator, param.role, allRolePool);
                    main.RemoveAt(0);
                });

                selected.Param?.OthersAssignment?.Do(assignment =>
                {
                    var param = assignment.Assigner.Invoke(selected.role, player);
                    table.SetRole(others[0], param.role, param.argument);
                    OnSetRole(allocator, param.role, allRolePool);
                    others.RemoveAt(0);
                });
                selected.left--;
                if (selected.left == 0) rolePool.Remove(selected);
                OnSetRole(allocator,selected.role, allRolePool);
            }
            OnSelected(targetRole);
        }
        [HarmonyPatch(typeof(StandardRoleAllocator), "Assign"), HarmonyPrefix]
        public static bool Assign(StandardRoleAllocator __instance,List<byte> impostors, List<byte> others)
        {
            if (CanInvokeSpecialMeeting && DiagnosisIsActive && Helpers.Prob(DiagnosisChance / 100f))
            {
                RoleTable table = new();
                var customChances = AssignmentType.AllTypes.Select((AssignmentType t) => new List<BetterRoleChance>(from r in Nebula.Roles.Roles.AllRoles.Where(delegate (DefinedRole r)
                {
                    if (t.Predicate(r.AssignmentStatus, r))
                    {
                        AllocationParameters customAllocationParameters = r.GetCustomAllocationParameters(t)!;
                        return ((customAllocationParameters != null) ? customAllocationParameters.RoleCountSum : 0) > 0;
                    }
                    return false;
                })select new BetterRoleChance(r, r.GetCustomAllocationParameters(t))
                {
                    cost = 1,
                    otherCost = 0
                })).ToArray<List<BetterRoleChance>>();
                //ロールプールを作る
                List<BetterRoleChance> GetRolePool(RoleCategory category) => new(Nebula.Roles.Roles.AllRoles.Where(r => r.Category == category && (r.AllocationParameters?.RoleCountSum ?? 0) > 0).Select(r => new BetterRoleChance(r) { cost = 1, otherCost = 0 }));
                List<BetterRoleChance> crewmateRoles = GetRolePool(RoleCategory.CrewmateRole);
                List<BetterRoleChance> neutralRoles = new List<BetterRoleChance>() { new BetterRoleChance(Violator.MyRole) { count = 1, left = 1, cost = 1, otherCost = 0 } };
                List<List<BetterRoleChance>> list = new();
                list.Add(crewmateRoles);
                list.Add(neutralRoles);
                list.AddRange(customChances);
                var allRoles = list.ToArray();
                List<byte> players = new List<byte>();
                players.AddRange(impostors);
                players.AddRange(others);
                if (DiagnosisChance == 100)
                {
                    table.SetRole(PlayerControl.LocalPlayer.PlayerId, Violator.MyRole);
                    players.Remove(PlayerControl.LocalPlayer.PlayerId);
                }
                else
                {
                    CategoryAssign(__instance,table, 1, players, players, neutralRoles, allRoles);
                }
                void AssignCustomAbilities(RoleCategory category)
                {
                    foreach (var assignmentType in AssignmentType.AllTypes)
                    {
                        if (assignmentType.IsActive&&assignmentType.RelatedRole.Category == category)
                        {
                            var targetRole = assignmentType.RelatedRole;
                            List<byte> players = (from r in table.roles
                                                  where r.Item1 == targetRole
                                                  select r.Item3).ToList<byte>();
                            CategoryAssign(__instance,table, players.Count, players, players, customChances[assignmentType.Id], allRoles, delegate (DefinedRole role, byte player)
                            {
                                table.EditRole(player, (ValueTuple<DefinedRole, int[]> last) => (last.Item1,assignmentType.EditArguments(last.Item2, role)));
                            });
                        }
                    }
                }
                int left = GeneralConfigurations.AssignmentCrewmateOption;
                if (left < 0)
                {
                    left = 24;
                }
                CategoryAssign(__instance,table, left, players, players, crewmateRoles, allRoles);
                AssignCustomAbilities(RoleCategory.CrewmateRole);
                foreach (var p in players) table.SetRole(p, Crewmate.MyRole);
                foreach (var m in Nebula.Roles.Roles.AllAllocatableModifiers().OrderBy(im => im.AssignPriority)) m.TryAssign(table);
                table.Determine();
            }
            else
            {
                try
                {
                    RoleTable table = new();
                   var customChances = AssignmentType.AllTypes.Select((AssignmentType t) => new List<BetterRoleChance>(from r in Nebula.Roles.Roles.AllRoles.Where(delegate (DefinedRole r)
                    {
                        if (t.Predicate(r.AssignmentStatus, r))
                        {
                            AllocationParameters customAllocationParameters = r.GetCustomAllocationParameters(t)!;
                            return ((customAllocationParameters != null) ? customAllocationParameters.RoleCountSum : 0) > 0;
                        }
                        return false;
                    }) select new BetterRoleChance(r, r.GetCustomAllocationParameters(t))
                    {
                        cost = 1,
                        otherCost = 0
                    })).ToArray<List<BetterRoleChance>>();
                    //ロールプールを作る
                    List<BetterRoleChance> GetRolePool(RoleCategory category) => new(Nebula.Roles.Roles.AllRoles.Where(r => r.Category == category && (r.AllocationParameters?.RoleCountSum ?? 0) > 0).Select(r => new BetterRoleChance(r) { cost = 1, otherCost = 0 }));
                    List<BetterRoleChance> crewmateRoles = GetRolePool(RoleCategory.CrewmateRole);
                    List<BetterRoleChance> impostorRoles = GetRolePool(RoleCategory.ImpostorRole);
                    List<BetterRoleChance> neutralRoles = GetRolePool(RoleCategory.NeutralRole);
                    List<List<BetterRoleChance>> list = new();
                    list.Add(crewmateRoles);
                    list.Add(impostorRoles);
                    list.Add(neutralRoles);
                    list.AddRange(customChances);
                    var allRoles = list.ToArray();
                    int impnums = GeneralConfigurations.AssignmentImpostorOption, neunums = GeneralConfigurations.AssignmentNeutralOption, crewnums = GeneralConfigurations.AssignmentCrewmateOption < 0 ? 24 : GeneralConfigurations.AssignmentCrewmateOption;
                    foreach (var tuple in RoleUpList)
                    {
                        try
                        {
                            if (allRoles.Any(r => r.Any(r => r.role.Id == tuple.role.Id)))
                            {
                                if (Helpers.Prob(tuple.chance / 100f))
                                {
                                    if (impostors.Contains(tuple.player))
                                    {
                                        if (tuple.role.Category != RoleCategory.ImpostorRole)
                                        {
                                            impostors.Remove(tuple.player);
                                            var otherplayer = others.Random();
                                            impostors.Add(otherplayer);
                                            others.Remove(otherplayer);
                                        }
                                        else
                                        { impostors.Remove(tuple.player); }
                                    }
                                    else if (others.Contains(tuple.player))
                                    {
                                        if (tuple.role.Category == RoleCategory.ImpostorRole)
                                        {
                                            var imp = impostors.Random();
                                            impostors.Remove(imp);
                                            others.Add(imp);
                                        }
                                        others.Remove(tuple.player);
                                    }
                                    switch (tuple.role.Category)
                                    {
                                        case RoleCategory.ImpostorRole:
                                            impnums--;
                                            break;
                                        case RoleCategory.NeutralRole:
                                            neunums--;
                                            break;
                                        case RoleCategory.CrewmateRole:
                                            crewnums--;
                                            break;
                                    }
                                    BetterRoleChance targetRoleChance = allRoles.FirstOrDefault(r => r.Any(r => r.role.Id == tuple.role.Id))!.FirstOrDefault(r => r.role.Id == tuple.role.Id)!;
                                    var rolepool = targetRoleChance!.role.Category == RoleCategory.ImpostorRole ? impostorRoles : targetRoleChance.role
                                        .Category == RoleCategory.NeutralRole ? neutralRoles : crewmateRoles;
                                    SingleSetRole(__instance,table, tuple.player, targetRoleChance.role.Category == RoleCategory.ImpostorRole ? impostors : others, others, targetRoleChance, rolepool, allRoles);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            PDebug.Log("RoleUpError:" + e.ToString());
                        }
                    }
                    foreach (var tuple in CategoryUpList)
                    {
                        try
                        {
                            if (Helpers.Prob(tuple.chance / 100f))
                            {
                                var usedRoleList = tuple.role == RoleCategory.ImpostorRole ? impostorRoles : tuple.role == RoleCategory.NeutralRole ? neutralRoles : crewmateRoles;

                                if (impostors.Contains(tuple.player))
                                {
                                    if (tuple.role != RoleCategory.ImpostorRole)
                                    {
                                        impostors.Remove(tuple.player);
                                        var otherplayer = others.Random();
                                        impostors.Add(otherplayer);
                                        others.Remove(otherplayer);
                                    }
                                    else
                                    { impostors.Remove(tuple.player); }
                                }
                                else if (others.Contains(tuple.player))
                                {
                                    if (tuple.role == RoleCategory.ImpostorRole)
                                    {
                                        var imp = impostors.Random();
                                        impostors.Remove(imp);
                                        others.Add(imp);
                                    }
                                    others.Remove(tuple.player);
                                }
                                switch (tuple.role)
                                {
                                    case RoleCategory.ImpostorRole:
                                        usedRoleList = impostorRoles;
                                        impnums--;
                                        break;
                                    case RoleCategory.NeutralRole:
                                        usedRoleList = neutralRoles;
                                        neunums--;
                                        break;
                                    case RoleCategory.CrewmateRole:
                                        usedRoleList = crewmateRoles;
                                        crewnums--;
                                        break;
                                }
                                CategoryAssign(__instance,table, 1, [tuple.player], others, usedRoleList, allRoles);
                            }
                        }
                        catch (Exception e)
                        {
                            PDebug.Log("CategoryUpError:" + e.ToString());
                        }
                    }
                    void AssignCustomAbilities(RoleCategory category)
                    {
                        foreach (var assignmentType in AssignmentType.AllTypes)
                        {
                            if (assignmentType.IsActive && assignmentType.RelatedRole.Category == category)
                            {
                                var targetRole = assignmentType.RelatedRole;
                                List<byte> players = (from r in table.roles
                                                      where r.Item1 == targetRole
                                                      select r.Item3).ToList<byte>();
                                CategoryAssign(__instance,table, players.Count, players, players, customChances[assignmentType.Id], allRoles, delegate (DefinedRole role, byte player)
                                {
                                    table.EditRole(player, (ValueTuple<DefinedRole, int[]> last) => (last.Item1, assignmentType.EditArguments(last.Item2, role)));
                                });
                            }
                        }
                    }
                    CategoryAssign(__instance,table, impnums, impostors, others, impostorRoles, allRoles);
                    AssignCustomAbilities(RoleCategory.ImpostorRole);
                    CategoryAssign(__instance,table, neunums, others, others, neutralRoles, allRoles);
                    var jackals = table.roles.Where(r => r.role == Jackal.MyRole).Select(r => r.playerId).ToList();
                    //ジャッカルIDの割り振り
                    for (int i = 0; i < jackals.Count; i++) table.EditRole(jackals[i], (last) => (last.role, [i]));
                    //ジャッカル化役職の割り当て
                    AssignCustomAbilities(RoleCategory.NeutralRole);
                    CategoryAssign(__instance,table, crewnums, others, others, crewmateRoles, allRoles);
                    AssignCustomAbilities(RoleCategory.CrewmateRole);
                    foreach (var p in impostors) table.SetRole(p, Impostor.MyRole);
                    foreach (var p in others) table.SetRole(p, Crewmate.MyRole);
                    var leaders = table.roles.Where(r => r.role == OutlawLeaderShower.MyRole).Select(r => r.playerId).ToList();
                    leaders.Do(p => table.EditRole(p, last => (OutlawLeader.MyRole, null)));
                    var assistants = table.roles.Where(r => r.role == AssistantShower.MyRole).Select(r => r.playerId).ToList();
                    assistants.Do(p => table.EditRole(p, last => (Assistant.MyRole, null)));
                    var dunners = table.roles.Where(r => r.role == DunnerShower.MyRole).Select(r => r.playerId).ToList();
                    dunners.Do(p => table.EditRole(p, last =>(Dunner.MyRole, null)));
                    foreach (var m in Nebula.Roles.Roles.AllAllocatableModifiers().OrderBy(im => im.AssignPriority)) m.TryAssign(table);
                    table.Determine();
                }
                catch (Exception e)
                {
                    PDebug.Log("StandardRoleAssign Error:" + e.ToString());
                }
            }
            Reload();
            return false;
        }
    }
    [HarmonyPatch]
    public class DiagnosisModePatchs
    {
        public static bool IsEnabled
        {
            get
            {
                return NebulaGameManager.Instance!.AllPlayerInfo.Any(p => p.Role is Violator.Instance);
            }
        }
        static RemoteProcess<int> SetExileMessageRpc = new("SetExileMessageRpc", (m, _) =>
        {
            ExileMessageType = m;
        });
        [HarmonyPatch(typeof(ExileControllerBeginPatch), "Postfix"), HarmonyPrefix]
        public static bool BlockNoSExile()
        {
            if (IsEnabled)
            {
                return false;
            }
            return true;
        }
        public static IEnumerable<RuntimeModifier> GetModifiers(GamePlayer player,bool isevil)
        {
            if (isevil)
            {
                foreach (var modifier in player.Modifiers)
                {
                    if (modifier.Modifier is Damned||modifier.Modifier is Desperate||modifier.Modifier is Disperser||modifier.Modifier is ExtraMission||modifier.Modifier is JailerModifier
                        ||modifier.Modifier is LawyerClient||modifier.Modifier is MadmatePLUS||modifier.Modifier is MoranModifier||modifier.Modifier is Scrambler||modifier.Modifier
                        is SidekickModifier||modifier.Modifier is SkinnerDog||modifier.Modifier is Trilemma||modifier.Modifier is InsomniacsModifier||modifier.Modifier is DreamweaverModifier)
                    {
                        yield return modifier;
                    }
                }    
            }
            else
            {
                foreach (var m in player.Modifiers) yield return m;
            }
        }
        public static string GetModifierInfo(string text,IEnumerable<RuntimeModifier> modifiers)
        {
            var newtext = text;
            foreach (var m in modifiers)
            {
                var t=m.OverrideRoleName(newtext, false);
                if (!string.IsNullOrEmpty(t))
                {
                    newtext = t;
                }
                m.DecorateNameConstantly(ref newtext, true);
            }
            return newtext;
        }
        [HarmonyPatch(typeof(ExileControllerBeginPatch), "Postfix"), HarmonyPostfix]
        public static void NosExilePos(ref ExileController.InitProperties init)
        {
            bool exiledJester=false;
            if (init.networkedPlayer != null)
            {
                if (MeetingHudExtension.IsObvious)
                {
                    ExileController.Instance.completeString = Language.Translate("game.meeting.obvious");
                }
                else if ((MeetingHudExtension.ExiledAll?.Length ?? 0) > 1)
                {
                    ExileController.Instance.completeString = Language.Translate("game.meeting.multiple");
                }
                else if (GeneralConfigurations.ShowRoleOfExiled && GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor)
                {
                    var player = NebulaGameManager.Instance!.GetPlayer(init.networkedPlayer.PlayerId);
                    if (player != null)
                    {
                        string roletext = player.Role.Role.DisplayName;
                        if (ExileShowEvilModifier || ExileShowAllModifier)
                        {
                            roletext = GetModifierInfo(roletext, GetModifiers(player, ExileShowEvilModifier));
                        }
                        ExileController.Instance.completeString = Language.Translate("game.meeting.roleText").Replace("%PLAYER%", init.networkedPlayer.PlayerName).Replace("%ROLE%", roletext);
                        if (player.Role.Role is Nebula.Roles.Neutral.Jester)
                        {
                            exiledJester = true;
                        }
                    }
                }
            }
            if (!IsEnabled&&!exiledJester)
            {
                if (GeneralConfigurations.ConfirmEjectTargetOption.GetValue() == 0)
                {
                    string confirmText = ExileController.Instance.ImpostorText.text;
                    int exiledBitFlag = 0;
                    foreach (PlayerControl exiled in MeetingHudExtension.ExiledAll)
                    {
                        exiledBitFlag |= 1 << (int)exiled.PlayerId;
                    }
                    bool IsDead(GamePlayer p)
                    {
                        return p.IsDead || (exiledBitFlag & (1 << (int)p.PlayerId)) != 0;
                    }
                    int num = Player.AllPlayers.Count((Player p) => !IsDead(p) && (p.IsImpostor || OutlawLeader.IsOutlawTeam(p)));
                    confirmText = Language.Translate((num > 1) ? "exile.confirmEject.impostor.p" : "exile.confirmEject.impostor.s").Replace("%NUM%", num.ToString());
                    Player localPlayer = Player.LocalPlayer;
                    int i = GeneralConfigurations.ConfirmEjectConditionOption.GetValue();
                    bool flag = true;
                    switch (i)
                    {
                        case 0:
                            flag = true;
                            break;
                        case 1:
                            flag = localPlayer.IsImpostor || OutlawLeader.IsOutlawTeam(localPlayer);
                            break;
                        case 2:
                            flag = localPlayer.Role.Role.IsKiller;
                            break;
                        case 3:
                            flag = !localPlayer.IsCrewmate;
                            break;
                    }
                    if (!flag)
                    {
                        confirmText = "";
                    }
                    ExileController.Instance.ImpostorText.text = confirmText;
                }
                if (ExileMessageType != 0)
                {
                    ExileController.Instance.completeString = Language.Translate("options.diagnosis.meeting.exile." + ExileMessageType.ToString());
                    ExileController.Instance.ImpostorText.text = "";
                }
            }
        }
        [HarmonyPatch(typeof(ExileController), "Begin"), HarmonyPostfix]
        public static void ExilePostfix(ExileController __instance, ref ExileController.InitProperties init)
        {
            if (!IsEnabled)
            {
                return;
            }
            GameOperatorManager.Instance?.Run(new ExileSceneStartEvent(MeetingHudExtension.ExiledAllModCache!));
            bool lockedJester = false;
            if (init.networkedPlayer != null)
            {

                if (MeetingHudExtension.IsObvious)
                {
                    __instance.completeString = Language.Translate("game.meeting.obvious");
                }
                else if ((MeetingHudExtension.ExiledAll?.Length ?? 0) > 1)
                {
                    __instance.completeString = Language.Translate("game.meeting.multiple");
                }
                else if (GeneralConfigurations.ShowRoleOfExiled && GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor)
                {
                    var player = NebulaGameManager.Instance!.GetPlayer(init.networkedPlayer.PlayerId);
                    if (player != null)
                    {
                        string text = player.Role.Role.DisplayName;
                        if (ExileShowEvilModifier||ExileShowAllModifier)
                        {
                            text=GetModifierInfo(text, GetModifiers(player, ExileShowEvilModifier));
                        }
                        __instance.completeString = Language.Translate("game.meeting.roleText").Replace("%PLAYER%", init.networkedPlayer.PlayerName).Replace("%ROLE%", text);
                        if (player.Role.Role == Nebula.Roles.Neutral.Jester.MyRole)
                        {
                            __instance.ImpostorText.text = Language.Translate("game.meeting.roleJesterText");
                            lockedJester = true;
                        }
                    }
                }
            }
            if (ExileMessageType != 0)
            {
                __instance.completeString = Language.Translate("options.diagnosis.meeting.exile." + ExileMessageType.ToString());
            }
            var texts = GameOperatorManager.Instance?.Run(new FixExileTextEvent(MeetingHudExtension.ExiledAllModCache!)).GetTexts();
            __instance.ImpostorText.rectTransform.pivot = new(0.5f, 1f);
            __instance.ImpostorText.rectTransform.sizeDelta = new(11.555f, 2f);
            __instance.ImpostorText.alignment = TMPro.TextAlignmentOptions.Top;
            if (init.confirmImpostor&&!lockedJester)
            {
                string confirmText = __instance.ImpostorText.text;
                int exiledBitFlag = 0;
                foreach (PlayerControl exiled in MeetingHudExtension.ExiledAll!)
                {
                    exiledBitFlag |= 1 << (int)exiled.PlayerId;
                }
                //bool IsDead(GamePlayer p) => p.IsDead || (exiledBitFlag & (1 << (int)p.PlayerId)) != 0;
                switch (GeneralConfigurations.ConfirmEjectTargetOption.GetValue())
                {
                    case 0:
                        {
                            int num = Violator.Instance.LeftImpostorNum;
                            confirmText = Language.Translate((num > 1) ? "exile.confirmEject.impostor.p" : "exile.confirmEject.impostor.s").Replace("%NUM%", num.ToString());
                            break;
                        }
                    case 1:
                        {
                            int num = Violator.Instance.LeftImpostorNum;
                            confirmText = Language.Translate((num > 1) ? "exile.confirmEject.killer.p" : "exile.confirmEject.killer.s").Replace("%NUM%", num.ToString());
                            break;
                        }
                    case 2:
                        {
                            int num = Violator.Instance.LeftImpostorNum;
                            confirmText = Language.Translate((num > 1) ? "exile.confirmEject.nonCrewmate.p" : "exile.confirmEject.nonCrewmate.s").Replace("%NUM%", num.ToString());
                            break;
                        }
                }
                Player localPlayer = Player.LocalPlayer!;
                int i = GeneralConfigurations.ConfirmEjectConditionOption.GetValue();
                bool flag = false;
                switch (i)
                {
                    case 0:
                        flag = true;
                        break;
                    case 1:
                        flag = localPlayer!.IsImpostor;
                        break;
                    case 2:
                        flag = localPlayer!.Role.Role.IsKiller;
                        break;
                    case 3:
                        flag = !localPlayer!.IsCrewmate;
                        break;
                    default:
                        break;
                }
                if (!flag)
                {
                    confirmText = "";
                }
                __instance.ImpostorText.text = confirmText;
            }
            if (init.networkedPlayer != null)
            {
                if (GeneralConfigurations.ShowRoleOfExiled && GameOptionsManager.Instance.currentNormalGameOptions.ConfirmImpostor)
                {
                    var role = NebulaGameManager.Instance!.GetPlayer(init.networkedPlayer.PlayerId)?.Role;
                    if (role != null)
                    {
                        if (role.Role == Nebula.Roles.Neutral.Jester.MyRole) __instance.ImpostorText.text = Language.Translate("game.meeting.roleJesterText");
                    }
                }
            }
            if (ExileMessageType!=0)
            {
                __instance.ImpostorText.text = "";
            }
            if (texts != null && texts.Count > 0)
            {
                if (!init.confirmImpostor) __instance.ImpostorText.text = "";
                init.confirmImpostor = true;

                __instance.ImpostorText.rectTransform.anchoredPosition3D += new Vector3(0f, 0.1f, 0f);

                var text = __instance.ImpostorText.text;
                text = "<line-height=90%>" + text;
                texts.Do(str => text += "<br>" + str);
                __instance.ImpostorText.text = text;
            }
            else
            {
                __instance.ImpostorText.rectTransform.anchoredPosition3D += new Vector3(0f, 0.19f, 0f);
            }
        }
        [HarmonyPatch(typeof(OracleSystem), "GetCurrentRolePool"), HarmonyPostfix]
        public static void OracleGetCurrentPoolPatch(ref object __result)
        {
            if (!DiagnosisModePatchs.IsEnabled)
            {
                return;
            }
            OracleSystem.RolePool set = new([], [], []);
            bool IsCrewmateRole(DefinedRole role) => role.Category == RoleCategory.CrewmateRole && !role.IsMadmate;
            NebulaGameManager.Instance!.AllPlayerInfo.Do(p =>
            {
                if (IsCrewmateRole(p.Role.ExternalRecognitionRole))
                    set.CrewmateRoles.Add(p.Role.ExternalRecognitionRole);
            });
            var addedlist = new List<DefinedRole>();
            for (int i = 0; i < GameOptionsManager.Instance.currentGameOptions.GetAdjustedNumImpostorsModded(PlayerControl.AllPlayerControls.Count); i++)
            {
                var role = new List<DefinedRole>(Nebula.Roles.Roles.AllRoles.Where(r => r.Category == RoleCategory.ImpostorRole && r.IsSpawnable)).Random();
                if (addedlist.Contains(role))
                {
                    i--;
                    continue;
                }
                set.ImpostorRoles.Add(role);
                addedlist.Add(role);
            }
            for (int i = 0; i < GeneralConfigurations.AssignmentNeutralOption; i++)
            {
                var role = new List<DefinedRole>(Nebula.Roles.Roles.AllRoles.Where(r => r.Category == RoleCategory.NeutralRole && r.IsSpawnable && !(r is GameMaster) && !(r is Violator)&&!(r is Spectre)&&!(r is SpectreFollower)&&!(r is SpectreImmoralist))).Random();
                if (addedlist.Contains(role))
                {
                    i--;
                    continue;
                }
                set.NeutralRoles.Add(role);
                addedlist.Add(role);
            }
            __result = set;
        }
        [HarmonyPatch(typeof(OracleSystem), "GetExcludedRoles"), HarmonyPostfix]
        public static void OracleGetExcludedRolesPatch(GamePlayer oracle, ref HashSet<DefinedRole> __result)
        {
            __result.Add(GameMaster.MyRole);
            __result.Add(Violator.MyRole);
        }
        [HarmonyPatch(typeof(ModAbilityButtonImpl), "get_OnClick"), HarmonyPostfix]
        public static void OnUseButton(ModAbilityButtonImpl __instance, ref Action<ModAbilityButtonImpl> __result)
        {
            try
            {
                if (!IsEnabled)
                {
                    return;
                }
                if (currentKillTime > StartKillTime)
                {
                    var local = GamePlayer.LocalPlayer;
                    if (local == null)
                    {
                        return;
                    }
                    if (local.Role is Violator.Instance)
                    {
                        return;
                    }
                    List<GamePlayer> closeplayers = new List<GamePlayer>();
                    foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                    {
                        if ((p.TruePosition - local.TruePosition).ToUnityVector().magnitude <= 1f)
                        {
                            closeplayers.Add(p);
                        }
                    }
                    Violator.Instance violator = null!;
                    var playerddd = NebulaGameManager.Instance?.AllPlayerInfo?.FirstOrDefault(p => p.Role is Violator.Instance);
                    if (playerddd != null)
                    {
                        violator = (playerddd!.Role as Violator.Instance)!;
                    }
                    if (violator == null)
                    {
                        return;
                    }
                    currentKillTime -= UnityEngine.Random.Range(2f, 6f);
                    NebulaManager.Instance.StartDelayAction(UnityEngine.Random.Range(3f, 11f), () =>
                    {
                        if (MeetingHud.Instance)
                        {
                            return;
                        }
                        if (Helpers.Prob(1f-(KillChance/100f)))
                        {
                            return;
                        }
                        var deadplayer = local;
                        if (Helpers.Prob(0.4f))
                        {
                            deadplayer = closeplayers.Random();
                        }
                        closeplayers.Clear();
                        foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                        {
                            if ((p.TruePosition - deadplayer.TruePosition).ToUnityVector().magnitude <= 2.7f && p.PlayerId != deadplayer.PlayerId&&!(p.Role is Violator.Instance))
                            {
                                closeplayers.Add(p);
                            }
                        }
                        if (closeplayers.Count >= 1)
                        {
                            int killtype = UnityEngine.Random.Range(0, 3);
                            if (killtype == 0 && usebubblenum >= Bubblegun.maxBubblesOption)
                            {
                                killtype = UnityEngine.Random.Range(1, 3);
                            }
                            if (killtype == 0)
                            {
                                usebubblenum++;
                                SetUseBubbleNum.Invoke(usebubblenum);
                                if (deadplayer.IsDead)
                                {
                                    return;
                                }
                                bool isbait = deadplayer.Role.GetAbility<Bait.Ability>() != null ||deadplayer.Role.GetAbility<Provocateur.Ability>()!=null|| deadplayer.Modifiers.Any(r => r is BaitM);
                                if (isbait)
                                {
                                    Bubblegun.RpcBubbleKill.Invoke((deadplayer, deadplayer, deadplayer.Position, 0));
                                }
                                else
                                {
                                    Bubblegun.RpcBubbleKill.Invoke((violator.MyPlayer, deadplayer, deadplayer.Position, 0));
                                }
                                closeplayers.Do(p =>
                                {
                                    if (p.IsDead)
                                    {
                                        return;
                                    }
                                    if (!(p.Role is Violator.Instance))
                                    {
                                        bool isbaits = p.Role.GetAbility<Bait.Ability>() != null || p.Role.GetAbility<Provocateur.Ability>() != null || p.Modifiers.Any(r => r is BaitM);
                                        Bubblegun.RpcBubbleKill.Invoke((isbaits ? p : violator.MyPlayer, p, p.Position, 0));
                                    }
                                });
                            }
                            else
                            {
                                if (killtype == 1)
                                {
                                    NebulaManager.Instance.StartDelayAction(1f, () =>
                                    {
                                        NebulaAsset.PlaySE(VanillaAsset.HnSTransformClip.Clip, deadplayer.VanillaPlayer.transform.position, 0.8f, Berserker.berserkSEStrengthOption, 0.8f, false);
                                    });
                                }
                                if (deadplayer.IsDead)
                                {
                                    return;
                                }
                                if (!(deadplayer.Role is Violator.Instance))
                                {
                                    bool isbait = deadplayer.Role.GetAbility<Bait.Ability>() != null || deadplayer.Role.GetAbility<Provocateur.Ability>() != null || deadplayer.Modifiers.Any(r => r is BaitM);
                                    if (isbait)
                                    {
                                        deadplayer.Suicide(killtype == 1 ? PlayerStates.Dead : PlayerState.Drill, killtype == 1 ? EventDetails.Kill : null, KillParameter.RemoteKill);
                                    }
                                    else
                                    {
                                        violator.MyPlayer.MurderPlayer(deadplayer, killtype == 1 ? PlayerStates.Dead : PlayerState.Drill, killtype == 1 ? EventDetails.Kill : null, KillParameter.RemoteKill);
                                    }
                                }
                                closeplayers.Do(p =>
                                {
                                    if (p.IsDead)
                                    {
                                        return;
                                    }
                                    if (!(p.Role is Violator.Instance))
                                    {
                                        bool isb = p.Role.GetAbility<Bait.Ability>() != null || p.Role.GetAbility<Provocateur.Ability>() != null || p.Modifiers.Any(r => r is BaitM);
                                        if (isb)
                                        {
                                            p.Suicide(killtype == 1 ? PlayerStates.Dead : PlayerState.Drill, killtype == 1 ? EventDetails.Kill : null, KillParameter.RemoteKill);
                                        }
                                        else
                                        {
                                            violator.MyPlayer.MurderPlayer(p, killtype == 1 ? PlayerStates.Dead : PlayerState.Drill, killtype == 1 ? EventDetails.Kill : null, KillParameter.RemoteKill);
                                        }
                                    }
                                });
                            }
                        }
                        else
                        {
                            if (deadplayer.IsDead)
                            {
                                return;
                            }
                            if (!(deadplayer.Role is Violator.Instance))
                            {
                                bool isb = deadplayer.Role.GetAbility<Bait.Ability>() != null || deadplayer.Role.GetAbility<Provocateur.Ability>() != null || deadplayer.Modifiers.Any(r => r is BaitM);
                                if (isb)
                                {
                                    deadplayer.Suicide(PlayerStates.Dead, EventDetails.Kill, KillParameter.RemoteKill);
                                }
                                else
                                {
                                    violator.MyPlayer.MurderPlayer(deadplayer, PlayerStates.Dead, EventDetails.Kill, KillParameter.RemoteKill);
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        [HarmonyPatch(typeof(EmergencyMinigame), "Begin"), HarmonyPostfix]
        public static void BeginPost(EmergencyMinigame __instance)
        {
            try
            {
                isSpecial = false;
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        static Rect bgRect = new Rect(1, 500, 503, 503);
        [HarmonyPatch(typeof(EmergencyMinigame), "Update"), HarmonyPostfix]
        public static void EmergencyButtonUpdate(EmergencyMinigame __instance)
        {
            if (GamePlayer.LocalPlayer!.Role is Violator.Instance)
            {
                return;
            }
            if (!CanInvokeSpecialMeeting)
            {
                return;
            }
            if (Input.GetMouseButtonDown(1))
            {
                isSpecial = !isSpecial;
            }
            if (isSpecial)
            {
                var spr = __instance.transform.FindChild("Background").GetComponent<SpriteRenderer>();
                var stream = NebulaAPI.AddonAsset!.GetResource("SpecialMeetingButton.png")!.AsStream();
                var texture = GraphicsHelper.LoadTextureFromStream(stream!);
                var sp = Sprite.Create(texture, bgRect, Vector2.one / 2);
                if (Background == null)
                {
                    Background = spr.sprite;
                }
                spr.sprite = sp;
                spr = __instance.transform.FindChild("Background").GetChild(0).GetComponent<SpriteRenderer>();
                sp = Sprite.Create(texture, bg2 == null ? spr.sprite.rect : bg2.rect, Vector2.one / 2);
                if (bg2 == null)
                {
                    bg2 = spr.sprite;
                }
                spr.sprite = sp;
                spr = __instance.transform.FindChild("MeetingButton").GetChild(0).GetComponent<SpriteRenderer>();
                sp = Sprite.Create(texture, meetingbutton == null ? spr.sprite.rect : meetingbutton.rect, Vector2.one / 2);
                if (meetingbutton == null)
                {
                    meetingbutton = spr.sprite;
                }
                spr.sprite = sp;
                if (__instance.state == 1)
                {
                    __instance.StatusText.text = Language.Translate("options.diagnosis.meeting.status");
                    __instance.NumberText.text = !usedSpecialMeeting ? "1" : "0";
                    __instance.ButtonActive = !usedSpecialMeeting;
                    __instance.ClosedLid.gameObject.SetActive(!__instance.ButtonActive);
                    __instance.OpenLid.gameObject.SetActive(__instance.ButtonActive);
                }
            }
            else
            {
                if (Background != null && bg2 != null && meetingbutton != null)
                {
                    __instance.transform.FindChild("Background").GetComponent<SpriteRenderer>().sprite = Background;
                    __instance.transform.FindChild("Background").GetChild(0).GetComponent<SpriteRenderer>().sprite = bg2;
                    __instance.transform.FindChild("MeetingButton").GetChild(0).GetComponent<SpriteRenderer>().sprite = meetingbutton;
                    if (__instance.state == 1)
                    {
                        int remainingEmergencies = PlayerControl.LocalPlayer.RemainingEmergencies;
                        __instance.StatusText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.EmergencyCount, new Il2CppReferenceArray<Il2CppSystem.Object>(new Il2CppSystem.Object[] { PlayerControl.LocalPlayer.Data.PlayerName }));
                        __instance.NumberText.text = remainingEmergencies.ToString();
                    }
                }
                if (__instance.state == 1)
                {
                    __instance.StatusText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.EmergencyCount, new Il2CppReferenceArray<Il2CppSystem.Object>(new Il2CppSystem.Object[] { PlayerControl.LocalPlayer.Data.PlayerName })) + Language.Translate("options.diagnosis.meetingbutton.tips");
                }
            }
        }
        [HarmonyPatch(typeof(EmergencyMinigame), "CallMeeting"), HarmonyPrefix]
        public static bool CallMeeting(EmergencyMinigame __instance)
        {
            if (GamePlayer.LocalPlayer!.Role is Violator.Instance)
            {
                return true;
            }
            if (isSpecial)
            {
                if (!Enumerable.Any<PlayerTask>(PlayerControl.LocalPlayer.myTasks.GetFastEnumerator(), new Func<PlayerTask, bool>(PlayerTask.TaskIsEmergency)) && !usedSpecialMeeting && __instance.ButtonActive)
                {
                    __instance.StatusText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.EmergencyRequested, new Il2CppReferenceArray<Il2CppSystem.Object>(Array.Empty<Il2CppSystem.Object>()));
                    if (Constants.ShouldPlaySfx())
                    {
                        SoundManager.Instance.PlaySound(__instance.ButtonSound, false, 1f, null);
                    }
                    PlayerControl.LocalPlayer.CmdReportDeadBody(null);
                    __instance.ButtonActive = false;
                    VibrationManager.Vibrate(1f, 1f, 0.2f, VibrationManager.VibrationFalloff.None, null, false, "");
                    RpcStartSpecialMeeting.Invoke(true);
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(PlayerModInfo), "UpdateNameText"), HarmonyPrefix]
        public static bool UpdateNameText(TextMeshPro nameText, bool onMeeting = false, bool showDefaultName = false)
        {
            if (isSpecialMeeting && onMeeting)
            {
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(MeetingHud), "UpdateTimerText"), HarmonyPostfix]
        public static void UpdateTimerText(MeetingHud __instance, StringNames key)
        {
            if (isSpecialMeeting && key == StringNames.MeetingVotingEnds)
            {
                var str = string.Format(DestroyableSingleton<TranslationController>.Instance.GetString(key, new Il2CppReferenceArray<Il2CppSystem.Object>(Array.Empty<Il2CppSystem.Object>())), "∞");
                __instance.TimerText.text = str;
            }
        }
        [HarmonyPatch(typeof(CheckForEndVotingPatch), "Prefix"), HarmonyPrefix]
        public static bool BlockNoS()
        {
            if (isSpecialMeeting)
            {
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(NebulaEndCriteria.CrewmateCriteria), "OnTaskUpdate"), HarmonyPrefix]
        public static bool CrewmateTaskCheck()
        {
            if (!IsEnabled)
            {
                return true;
            }
            int quota = 0;
            int completed = 0;
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo)
            {
                if (p.IsDisconnected) continue;

                if (!p.Tasks.IsCrewmateTask) continue;
                quota += p.Tasks.Quota;
                completed += p.Tasks.TotalCompleted;
            }
            if (quota > 0 && quota <= completed) NebulaAPI.CurrentGame?.TriggerGameEnd(NobodyAliveEnd, GameEndReason.Special);
            return false;
        }
        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting)), HarmonyPrefix]
        public static bool CheckEndVotingPatch(MeetingHud __instance)
        {
            if (!isSpecialMeeting)
            {
                return true;
            }
            //投票が済んでない場合、なにもしない
            if (!__instance.playerStates.All((PlayerVoteArea ps) =>
            {
                if (ps.TargetPlayerId == yesId || ps.TargetPlayerId == noId)
                {
                    if (GamePlayer.GetPlayer(ps.TargetPlayerId)!.IsDead)
                    {
                        return true;
                    }
                }
                return ps.AmDead || ps.DidVote || !MeetingHudExtension.HasVote(ps.TargetPlayerId);
            }))
            {
                return false;
            }

            {
                Dictionary<byte, int> dictionary = ModCalculateVotes(__instance);
                KeyValuePair<byte, int> max = dictionary.MaxPairV2(out bool tie);

                List<byte> extraVotes = new();

                if (tie)
                {
                    Dictionary<byte, GamePlayer?> voteForMap = new();

                    foreach (var state in __instance.playerStates)
                    {
                        if (!state.DidVote) continue;
                        voteForMap[state.TargetPlayerId] = NebulaGameManager.Instance?.GetPlayer(state.VotedFor);
                    }

                    foreach (var target in GameOperatorManager.Instance?.Run(new MeetingTieVoteHostEvent(voteForMap))?.ExtraVotes ?? [])
                    {
                        dictionary.AddValueV2(target?.PlayerId ?? 253, 1);
                        extraVotes.Add(target?.PlayerId ?? 253);
                    }

                    //再計算する
                    max = dictionary.MaxPairV2(out tie);
                }

                bool ResultisYes = max.Key == yesId;
                GamePlayer exiled = null!;
                GamePlayer[] exiledAll = [];
                var violator = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => p.Role is Violator.Instance);
                var mask = BitMasks.AsPlayer();
                if (violator != null)
                {
                    if (!violator.IsDead)
                    {
                        mask.Add(violator);
                    }
                }
                if (!tie)
                {
                    if (IsEnabled)
                    {
                        if (ResultisYes)
                        {
                            SetExileMessageRpc.Invoke(1);
                            GameOperatorManager.Instance!.Subscribe<TaskPhaseStartEvent>(ev =>
                            {
                                NebulaAPI.CurrentGame!.TriggerGameEnd(NebulaGameEnd.CrewmateWin, GameEndReason.Special);
                            }, NebulaAPI.CurrentGame!);
                        }
                        else
                        {
                            SetExileMessageRpc.Invoke(2);
                            GameOperatorManager.Instance!.Subscribe<TaskPhaseStartEvent>(ev =>
                            {
                                NebulaAPI.CurrentGame!.TriggerGameEnd(NobodyAliveEnd, GameEndReason.Special, mask);
                            }, NebulaAPI.CurrentGame!);
                            exiledAll = GamePlayer.AllPlayers.Where(v => !v.IsDead && v.PlayerId != violator?.PlayerId).ToArray();
                            exiled = exiledAll.First();
                        }
                    }
                    else
                    {
                        if (ResultisYes)
                        {
                            SetExileMessageRpc.Invoke(1);
                            GameOperatorManager.Instance!.Subscribe<TaskPhaseStartEvent>(ev =>
                            {
                                NebulaAPI.CurrentGame!.TriggerGameEnd(NobodyAliveEnd, GameEndReason.Special);
                            }, NebulaAPI.CurrentGame!);
                            exiledAll = GamePlayer.AllPlayers.Where(v => !v.IsDead).ToArray();
                            exiled = exiledAll.First();
                        }
                        else
                        {
                            SetExileMessageRpc.Invoke(3);
                        }
                    }
                }
                if (tie)
                {
                    RpcStartSpecialMeeting.Invoke(false);
                }
                List<MeetingHud.VoterState> allStates = new();

                //記名投票分
                foreach (var state in __instance.playerStates)
                {
                    if (!state.DidVote) continue;

                    if (!MeetingHudExtension.WeightMap.TryGetValue((byte)state.TargetPlayerId, out var vote)) vote = 1;

                    for (int i = 0; i < vote; i++)
                    {
                        allStates.Add(new MeetingHud.VoterState
                        {
                            VoterId = state.TargetPlayerId,
                            VotedForId = state.VotedFor
                        });
                    }
                }

                //追加投票分
                foreach (var votedFor in extraVotes)
                {
                    allStates.Add(new MeetingHud.VoterState
                    {
                        VoterId = byte.MaxValue,
                        VotedForId = votedFor
                    });
                }

                //Debug.Log($"Exiled: ({string.Join(',', (exiledAll ?? []).Select(b => b.ToString()))})");
                MeetingModRpc.RpcModCompleteVoting.Invoke((allStates, exiled?.PlayerId ?? byte.MaxValue, exiledAll?.Select(e => e.PlayerId).ToArray() ?? [], tie, false));
                //サーバー用、プレイヤーは全員このメッセージを無視する
                //__instance.RpcVotingComplete(allStates.ToArray(), Helpers.GetPlayer(exiled?.PlayerId)?.Data, tie);
            }
            return false;
        }
        [HarmonyPatch(typeof(MeetingHud), "UpdateButtons"), HarmonyPrefix]
        public static bool UpdateButtonBlock(MeetingHud __instance)
        {
            if (isSpecialMeeting)
            {
                if (PlayerControl.LocalPlayer.Data.IsDead && !__instance.amDead)
                {
                    __instance.SetForegroundForDead();
                }
                if (AmongUsClient.Instance.AmHost)
                {
                    for (int i = 0; i < __instance.playerStates.Length; i++)
                    {
                        var state = __instance.playerStates[i];
                        if (state.TargetPlayerId == yesId || state.TargetPlayerId == noId)
                        {
                            state.SetDead(false, false, false);
                            state.SetEnabled();
                        }
                    }
                }
                return false;
            }
            return true;
        }
        [HarmonyPatch(typeof(MeetingHudExtension), "CanVoteFor", typeof(byte)), HarmonyPostfix]
        public static void MeetingHudCanVoteFor(MeetingHud __instance, byte playerId, ref bool __result)
        {
            if (!isSpecialMeeting)
            {
                return;
            }
            if (playerId == yesId || playerId == noId)
            {
                __result = true;
            }
        }
        [HarmonyPatch(typeof(MeetingPlayerButtonManager),"Update"),HarmonyPostfix]
        public static void BlockMeetingAction(MeetingPlayerButtonManager __instance)
        {
            if (isSpecialMeeting)
            {
                __instance.ResetActions();
            }
        }
        public static int ExileMessageType = 0;
        public static Dictionary<byte, int> ModCalculateVotes(MeetingHud __instance)
        {
            Dictionary<byte, int> dictionary = new();

            List<string> log = new();
            for (int i = 0; i < __instance.playerStates.Length; i++)
            {
                PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                var player = NebulaGameManager.Instance?.GetPlayer(playerVoteArea.TargetPlayerId);
                if (player?.IsDead ?? true) continue;

                bool didVote = playerVoteArea.VotedFor != 252 && playerVoteArea.VotedFor != 255 && playerVoteArea.VotedFor != 254;
                if (!MeetingHudExtension.WeightMap.TryGetValue((byte)playerVoteArea.TargetPlayerId, out var vote)) vote = 1;
                var ev = GameOperatorManager.Instance!.Run(new PlayerFixVoteHostEvent(player, didVote, NebulaGameManager.Instance?.GetPlayer(playerVoteArea.VotedFor), vote));

                if (ev.DidVote)
                {
                    dictionary.AddValueV2(ev.VoteTo?.PlayerId ?? PlayerVoteArea.SkippedVote, ev.Vote);
                    playerVoteArea.VotedFor = ev.VoteTo?.PlayerId ?? PlayerVoteArea.SkippedVote;
                    MeetingHudExtension.WeightMap[player.PlayerId] = ev.Vote;
                }
                else
                {
                    playerVoteArea.VotedFor = PlayerVoteArea.MissedVote;
                }
            }


            return dictionary;
        }


        static RemoteProcess<bool> RpcStartSpecialMeeting = new("RpcStartspecialMeeting", (m, _) =>
        {
            usedSpecialMeeting = m;
            isSpecialMeeting = m;
        });
        public static bool startmeeting;
        public static void OnSpecialMeetingUpdate()
        {
            if ((MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted || MeetingHud.Instance.state == MeetingHud.VoteStates.Voted) && !startmeeting)
            {
                startmeeting = true;
                NebulaManager.Instance.StartDelayAction(1f, () =>
                {
                    MeetingModRpc.RpcChangeVotingStyle.LocalInvoke((255, false, 10000000f, false, false));
                    MeetingHudExtension.CanShowPhotos = false;
                    foreach (var p in MeetingHud.Instance.playerStates) p.SetDisabled();
                    var sMeeting = UnityHelper.CreateObject<SpecialMeetingHud>("SpecialMeeting", MeetingHud.Instance.transform, Vector3.zero);
                    sMeeting.Begin(() =>
                    {
                        int votemask = 0;
                        votemask |= 1 << yesId;
                        votemask |= 1 << noId;
                        MeetingModRpc.RpcChangeVotingStyle.LocalInvoke((votemask, false, 10000000f, false, false));
                    });
                });
            }
        }

        public static bool isSpecialMeeting;
        static bool isSpecial;
        public static bool usedSpecialMeeting;
        static Sprite? Background, bg2, meetingbutton;
        public static byte yesId = 1, noId = 1;
        static Image SpecialMeetingButton = NebulaAPI.AddonAsset.GetResource("SpecialMeetingButton.png")!.AsImage()!;
        /*static readonly RemoteProcess RpcJusticePlusMeeting = new("JusticePlusMeeting",
    _ => {
        MeetingModRpc.RpcChangeVotingStyle.LocalInvoke((255, false, 2000000f, true, false));
        MeetingHudExtension.CanShowPhotos = false;
        foreach (var p in MeetingHud.Instance.playerStates) p.SetDisabled();
        var sMeeting = UnityHelper.CreateObject<SpecialMeetingHud>("SpecialMeeting", MeetingHud.Instance.transform, Vector3.zero);
        sMeeting.Begin(() =>
        {
            int votemask = 0;
            votemask |= 1 << 1;
            votemask |= 1 << 2;
            MeetingModRpc.RpcChangeVotingStyle.LocalInvoke((votemask, false, 2000000f, true, false));
        });
    });*/
        public class SpecialMeetingHud : MonoBehaviour
        {
            static SpecialMeetingHud() => ClassInjector.RegisterTypeInIl2Cpp<SpecialMeetingHud>();
            static private readonly SpriteLoader meetingBackMaskSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.MeetingUIMask.png", 100f);
            static private readonly SpriteLoader meetingReticleSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeMeetingReticle.png", 100f);
            static private readonly Image meetingViewSprite = NebulaAPI.AddonAsset.GetResource("SpecialMeetingView.png")!.AsImage()!;
            static private readonly Image votingHolderLeftSprite = NebulaAPI.AddonAsset.GetResource("SpecialMeetingLeft.png")!.AsImage()!;
            static private readonly Image votingHolderRightSprite = NebulaAPI.AddonAsset.GetResource("SpecialMeetingRight.png")!.AsImage()!;
            static private readonly SpriteLoader votingHolderMaskSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeHolderMask.png", 120f);
            static private readonly SpriteLoader votingHolderFlashSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeHolderFlash.png", 120f);
            static private readonly SpriteLoader votingHolderBlurSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeHolderFlashBlur.png", 120f);

            SpriteRenderer? Background1, Background2, BackView, UpView;

            // 位置参数
            Vector3 leftPos = new Vector3(-2f, 0f, 0f);
            Vector3 rightPos = new Vector3(2f, 0f, 0f);
            Vector3 CenterPos = new Vector3(0f, 0f, 0f);
            Vector3 leftOutPos = new Vector3(-10f, 0f, 0f);
            Vector3 rightOutPos = new Vector3(10f, 0f, 0f);

            private IEnumerator CoAnimColor(SpriteRenderer renderer, Color color1, Color color2, float duration)
            {
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    renderer.color = Color.Lerp(color1, color2, t / duration);
                    yield return null;
                }
                renderer.color = color2;
            }

            private IEnumerator CoAnimColorRepeat(SpriteRenderer renderer, Color color1, Color color2, float duration)
            {
                while (true)
                {
                    yield return CoAnimColor(renderer, color1, color2, duration);
                    yield return CoAnimColor(renderer, color2, color1, duration);
                }
            }
            public void Begin(Action onMeetingStart)
            {
                StartCoroutine(SetUpJusticeMeeting(MeetingHud.Instance, onMeetingStart).WrapToIl2Cpp());
            }

            private static string[] RandomTexts = ["(despired)", "terminus", "<revolt>", "solitary", "bona vacantia", "despotism", "pizza", "elitism", "suspicion", "justice", "outsider", "discrepancy", "purge", "uniformity", "conviction", "tribunal", "triumph", "heroism", "u - majority"];
            private static string[] RandomAltTexts = ["HERO", "victor", "supreme", "the one", "genius", "prodigy", "detective", "clairvoyant"];
            IEnumerator CoDisappearVotingArea(MeetingHud meetingHud, float duration)
            {
                var states = meetingHud.playerStates.OrderBy(i => Guid.NewGuid()).ToArray();
                var interval = duration / states.Length;
                foreach (var state in states)
                {
                    state.gameObject.SetActive(false);
                    yield return Effects.Wait(interval);
                }
            }

            IEnumerator CoAnimBackLine(GameObject parent)
            {
                var lineRenderer = UnityHelper.CreateObject<SpriteRenderer>("Line", parent.transform, new(0.01f, 0f, -18f));
                lineRenderer.sprite = VanillaAsset.FullScreenSprite;
                lineRenderer.color = Color.black.AlphaMultiplied(0.85f);
                lineRenderer.transform.localScale = new(20f, 0f, 1f);

                float t = 0f;
                float p = 0f;
                while (t < 2f && parent)
                {
                    p += (1 - p).Delta(3.5f, 0.01f);
                    lineRenderer.transform.localScale = new(8.79f, p * 0.76f, 1f);
                    t += Time.deltaTime;
                    yield return null;
                }

                yield return Effects.Wait(1.25f);
                t = 0f;
                while (t < 1f && parent)
                {
                    p -= p.Delta(6.9f, 0.01f);
                    lineRenderer.transform.localScale = new(8.79f, p * 0.76f, 1f);
                    t += Time.deltaTime;
                    yield return null;
                }
                UnityEngine.Object.Destroy(parent);
            }

            IEnumerator CoAnimIntroText(GameObject parent)
            {
                var introText = UnityHelper.CreateObject<TextMeshNoS>("IntroText", parent.transform, new(0f, 0f, -18.5f));
                introText.Font = NebulaAsset.JusticeFont;
                introText.FontSize = 0.48f;
                introText.TextAlignment = Virial.Text.TextAlignment.Center;
                introText.Pivot = new(0.5f, 0.5f);
                introText.Text = "";
                introText.Material = UnityHelper.GetMeshRendererMaterial();
                introText.Color = JusticePlus.MyRole.UnityColor;
                /*IEnumerator CoAnimTextColor(TextMeshNoS tm, Color color1, Color color2, float duration)
                {
                    float t = 0f;
                    while (t < duration)
                    {
                        t += Time.deltaTime;
                        tm.Color = Color.Lerp(color1, color2, t / duration);
                        yield return null;
                    }
                    tm.Color = color2;
                }*/
                string completedText = "A special meeting begins...";
                for (int i = 0; i < completedText.Length; i++)
                {
                    introText.Text = completedText.Substring(0, i + 1);
                    yield return Effects.Wait(0.072f);
                }
                for (int i = 0; i < 3; i++)
                {
                    introText.gameObject.SetActive(false);
                    yield return Effects.Wait(0.04f);
                    introText.gameObject.SetActive(true);
                    yield return Effects.Wait(0.04f);
                }
            }

            IEnumerator CoPlayAlertFlash()
            {
                yield return Effects.Wait(0.15f);
                for (int i = 0; i < 3; i++)
                {
                    AmongUsUtil.PlayCustomFlash(Color.red, 0.2f, 0.2f, 0.3f, 0.6f);
                    yield return Effects.Wait(1.0f + 0.3f);
                }
            }
            IEnumerator SetUpJusticeMeeting(MeetingHud meetingHud, Action onMeetingStart)
            {
                yesId = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(p => !p.Disconnected && !p.IsDead)!.PlayerId;
                noId = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(p => !p.Disconnected && p.PlayerId != yesId)!.PlayerId;
                meetingHud.TimerText.gameObject.SetActive(false);
                StartCoroutine(CoDisappearVotingArea(meetingHud, 2.3f).WrapToIl2Cpp());
                yield return Effects.Wait(0.1f);
                StartCoroutine(CoDisappearVotingArea(meetingHud, 2.3f).WrapToIl2Cpp());
                yield return Effects.Wait(0.9f);
                NebulaAsset.PlaySE(NebulaAudioClip.Justice2);
                //GameObject.Destroy(introObj);
                meetingHud.TimerText.gameObject.SetActive(true);
                PDebug.Log("SpecialMeetingStart");
                onMeetingStart.Invoke();

                //タイトルテキストが少し右にずれているので修正
                meetingHud.TitleText.transform.localPosition = new(-0.25f, 2.2f, -1f);
                meetingHud.TitleText.text = Language.Translate("options.diagnosis.meeting.title");
                string textid = string.Empty;
                switch (AmongUsUtil.CurrentMapId)
                {
                    case 0:
                    case 3:
                    case 4:
                        textid = "skeldandairship";
                        break;
                    case 1:
                    case 2:
                        textid = "miraandpolus";
                        break;
                    case 5:
                        textid = "fungle";
                        break;
                }
                meetingHud.TitleText.text += Environment.NewLine + Language.Translate("options.diagnosis.meeting.prefix").Replace("%TYPE%", Language.Translate("options.diagnosis.meeting." + textid));
                //背景を作る
                var backObj = UnityHelper.CreateObject<SortingGroup>("JusticeBackground", transform, new(0f, 0f, 7f));
                var mask = UnityHelper.CreateObject<SpriteMask>("JusticeMask", backObj.transform, Vector3.zero);
                mask.transform.localScale = new(1.2f, 1f, 1f);
                mask.sprite = meetingBackMaskSprite.GetSprite();
                PDebug.Log("ShowAnimEnd");
                var backReticle = UnityHelper.CreateSpriteRenderer("JusticeBackReticle", backObj.transform, new(0f, 0f, -0.2f));
                backReticle.transform.localScale = new(0.69f, 0.69f, 1f);
                backReticle.sprite = meetingReticleSprite.GetSprite();
                backReticle.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

                var reticleText = NebulaAsset.InstantiateText("ReticleText", backObj.transform, new(0f, -1.9f, -0.2f), NebulaAsset.JusticeFont, 0.42f, Virial.Text.TextAlignment.Center, new(0.5f, 0.5f), "", new(0.7f, 0.7f, 0.7f, 0.4f));

                IEnumerator CoAnimText(string text)
                {
                    reticleText.Text = "";
                    reticleText.gameObject.SetActive(true);
                    yield return null;
                    string targetText = text;
                    for (int i = 1; i <= targetText.Length; i++)
                    {
                        reticleText.Text = targetText.Substring(0, i);
                        yield return Effects.Wait(0.08f);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        reticleText.gameObject.SetActive(false);
                        yield return Effects.Wait(0.05f);
                        reticleText.gameObject.SetActive(true);
                        yield return Effects.Wait(0.05f);
                    }

                    yield return Effects.Wait(5f + System.Random.Shared.NextSingle() * 8f);

                    for (int i = 0; i < 3; i++)
                    {
                        reticleText.gameObject.SetActive(false);
                        yield return Effects.Wait(0.05f);
                        reticleText.gameObject.SetActive(true);
                        yield return Effects.Wait(0.05f);
                    }
                    reticleText.gameObject.SetActive(false);
                    yield return Effects.Wait(0.6f + System.Random.Shared.NextSingle() * 0.6f);
                }
                IEnumerator CoRepeatAnimText()
                {
                    string[] texts = [];
                    int index = 0;
                    while (true)
                    {
                        if (index == texts.Length)
                        {
                            texts = RandomTexts.OrderBy(_ => Guid.NewGuid()).ToArray();
                            index = 0;
                        }

                        yield return CoAnimText(texts[index++]);
                    }
                }
                StartCoroutine(CoRepeatAnimText().WrapToIl2Cpp());
                PDebug.Log("GenBackView");

                BackView = UnityHelper.CreateSpriteRenderer("JusticeBackView", backObj.transform, new(0f, 0f, -0.2f));
                BackView.transform.localScale = new(1f, 1f, 1f);
                BackView.sprite = meetingViewSprite.GetSprite();
                BackView.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                IEnumerator CoAnimView()
                {
                    BackView.color = new(0.5f, 0.5f, 0.5f, 0.4f);

                    while (true)
                    {
                        yield return Effects.Wait(5.5f + System.Random.Shared.NextSingle() * 3f);

                        switch (System.Random.Shared.Next(3))
                        {
                            case 0:
                                yield return CoAnimColor(BackView, new(1f, 0.3f, 0.3f, 0.5f), new(0.5f, 0.5f, 0.5f, 0.5f), 0.8f);
                                break;
                            case 1:
                                yield return CoAnimColor(BackView, new(0.9f, 0.3f, 0.3f, 0.5f), new(0.5f, 0.5f, 0.5f, 0.5f), 0.3f);
                                yield return Effects.Wait(0.2f);
                                yield return CoAnimColor(BackView, new(1f, 0.3f, 0.3f, 0.5f), new(0.5f, 0.5f, 0.5f, 0.5f), 1.4f);
                                break;
                            case 2:
                                yield return CoAnimColor(BackView, new(0.5f, 0.5f, 0.5f, 0.5f), new(1f, 1f, 1f, 0.75f), 0.2f);
                                yield return CoAnimColor(BackView, new(1f, 1f, 1f, 0.75f), new(0.5f, 0.5f, 0.5f, 0.5f), 0.8f);
                                break;
                        }
                    }
                }
                PDebug.Log("CoAnimView");
                StartCoroutine(CoAnimView().WrapToIl2Cpp());
                PDebug.Log("AnimViewEnd");
                meetingHud.playerStates.Do(p => p.gameObject.SetActive(false));

                var boardPassGame = VanillaAsset.MapAsset[2].CommonTasks.FirstOrDefault(p => p.MinigamePrefab.name == "BoardingPassGame")?.MinigamePrefab.TryCast<BoardPassGame>();
                void SpawnVotingArea(bool yes, Vector3 localPos, Virial.Media.Image holder)
                {
                    IEnumerator CoSpawnPlayerArea(bool isyes)
                    {
                        yield return Effects.Wait(1.3f);
                        PDebug.Log("GenSpecialMeetingPlayerArea");
                        var back = UnityHelper.CreateSpriteRenderer("JusticePlayerArea", transform, localPos + new Vector3(0f, 0f, 6f));
                        back.gameObject.AddComponent<SortingGroup>();
                        back.transform.localScale = new(0.6f, 0.6f, 1f);
                        back.sprite = holder.GetSprite();
                        //back.material = HatManager.Instance.PlayerMaterial;
                        yield return StartCoroutine(CoAnimColor(back, new UnityEngine.Color(1f, 1f, 1f, 0f), new UnityEngine.Color(1f, 1f, 1f, 1f), 0.5f).WrapToIl2Cpp());
                        try
                        {
                            var mask = UnityHelper.CreateObject<SortingGroup>("Masked", back.transform, new(0f, 0f, -0.5f));
                            var maskRenderer = UnityHelper.CreateObject<SpriteMask>("Mask", mask.transform, Vector3.zero);
                            maskRenderer.sprite = votingHolderMaskSprite.GetSprite();
                            PDebug.Log("SetVoteAreaPos");
                            PlayerVoteArea playerVoteArea = MeetingHud.Instance.playerStates.FirstOrDefault(p => p.TargetPlayerId == (isyes ? yesId : noId))!;
                            if (playerVoteArea != null)
                            {
                                //playerVoteArea.transform.localScale = new Vector2(0.85f, 0.85f);
                                playerVoteArea.Background.sprite = ShipStatus.Instance.CosmeticsCache.GetNameplate("1111111").Image;
                                playerVoteArea.PlayerIcon.gameObject.SetActive(false);
                                playerVoteArea.LevelNumberText.transform.parent.gameObject.SetActive(false);
                                playerVoteArea.NameText.text = Language.Translate("options.diagnosis.meeting." + (isyes ? "yes" : "no"));
                                playerVoteArea.ColorBlindName.text = "";//Language.Translate("options.diagnosis.meeting." + (isyes ? "yes" : "no"));
                                playerVoteArea.SetDead(false, false, false);
                                playerVoteArea.gameObject.SetActive(true);
                                playerVoteArea.gameObject.transform.localPosition = localPos + new Vector3(-0.22f, 0f, -0.9f);
                            }
                        }
                        catch (Exception E)
                        {
                            PDebug.Log(E);
                        }
                    }
                    StartCoroutine(CoSpawnPlayerArea(yes).WrapToIl2Cpp());
                }
                SpawnVotingArea(true, leftPos, votingHolderLeftSprite);
                SpawnVotingArea(false, rightPos, votingHolderRightSprite);

                yield break;
            }
        }
    }
    static RemoteProcess<int> SetUseBubbleNum = new("SetUseBubblenum", (m, _) =>
    {
        usebubblenum = m;
    });
    public static int usebubblenum;
    static List<byte> votedPlayerId = new List<byte>();
    public static void JoinGameLoadingPatch(AmongUsClient __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)128)==0)
        {
            return;
        }
        var overlay = UnityEngine.Object.Instantiate<SpriteRenderer>(DestroyableSingleton<TransitionFade>.Instance.overlay, null);
        overlay.transform.position = DestroyableSingleton<TransitionFade>.Instance.overlay.transform.position;
        IEnumerator CoFadeInIf()
        {
            if (AmongUsClient.Instance.ClientId<0)
            {
                yield return Effects.ColorFade(overlay, Color.black, Color.clear, 0.2f);
                UnityEngine.Object.Destroy(overlay);
            }    
        }
        Il2CppSystem.Collections.IEnumerator[] array = new Il2CppSystem.Collections.IEnumerator[4];
        array[0] = Effects.ColorFade(overlay, Color.clear, Color.black, 0.2f);
        array[1] = ManagedEffects.Action(delegate
        {
            NebulaManager.Instance.StartCoroutine(HintManager.CoShowHint(0.6f).WrapToIl2Cpp());
        }).WrapToIl2Cpp();
        array[2] = __result;
        array[3] = CoFadeInIf().WrapToIl2Cpp();
        __result = Effects.Sequence(array);
    }
    public static void CreateGameOptionsLoadingPatch()
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)128) == 0)
        {
            return;
        }
        NebulaManager.Instance.StartCoroutine(HintManager.CoShowHint(0.8f).WrapToIl2Cpp());
    }
    public static void ResultPrefix()
    {
        votedPlayerId = new List<byte>();
    }
    public static bool ModBloopAVoteIconPatch(NetworkedPlayerInfo? voterPlayer, int index, Transform parent, bool isExtra)
    {
        if (!MayorExtraVoteAnonymousVote)
        {
            return true;
        }
        var __instance = MeetingHud.Instance;
        SpriteRenderer spriteRenderer = GameObject.Instantiate<SpriteRenderer>(__instance.PlayerVotePrefab);
        if ((GameManager.Instance.LogicOptions.GetAnonymousVotes() && !(NebulaGameManager.Instance?.CanSeeAllInfo ?? false)) || voterPlayer == null)
            PlayerMaterial.SetColors(Palette.DisabledGrey, spriteRenderer);
        else
        {
            if (voterPlayer.Object.ToNebulaPlayer().Role.GetAbility<Mayor.Ability>() != null&&votedPlayerId.Contains(voterPlayer.PlayerId))
            {
                var list = GameData.Instance.AllPlayers.GetFastEnumerator().Where(p => !p.IsDead).ToList();
                PlayerMaterial.SetColors(list[UnityEngine.Random.Range(0,list.Count)].DefaultOutfit.ColorId, spriteRenderer);
            }
            else
            {
                PlayerMaterial.SetColors(voterPlayer.DefaultOutfit.ColorId, spriteRenderer);
            }
        }
        if (voterPlayer != null)
        {
            votedPlayerId.Add(voterPlayer.PlayerId);
        }
        spriteRenderer.transform.SetParent(parent);
        spriteRenderer.transform.localScale = Vector3.zero;
        __instance.StartCoroutine(Effects.Bloop((float)index * 0.3f + (isExtra ? 0.85f : 0f), spriteRenderer.transform, 1f, isExtra ? 0.5f : 0.7f));

        if (isExtra)
            __instance.StartCoroutine(Effects.Sequence(Effects.Wait((float)index * 0.3f + 0.85f), ManagedEffects.Action(() => parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer)).WrapToIl2Cpp()));
        else
            parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer);
        return false;
    }
    public static void AnonymousVotesFix(ref bool __result)
    {
        if (__result&&NebulaGameManager.Instance!=null&&GamePlayer.LocalPlayer!=null)
        {
            if (GamePlayer.LocalPlayer.Role.GetAbility<CrawlerEngineer.Ability>()!=null&&CrawlerEngineer.CrawlerHasWatching)
            {
                __result = false;
            }
        }
    }
    public static void AbilityTimerFix(TimerImpl __instance)
    {
        __instance.SetPredicate(() => PlayerControl.LocalPlayer.IsKillTimerEnabled);
    }
    public static void ClientRecordInvokeSabotageRpc(SystemTypes systemType, PlayerControl player,MessageReader msgReader)//Prefix
    {
        RpcSabotageLogForAdmin.Invoke(((byte)systemType, msgReader.PeekByte(), player.PlayerId));
    }
    public static void HostRecordInvokeSabotageRpc(SystemTypes systemType, PlayerControl player, byte amount)//Postfix
    {
        RpcSabotageLogForAdmin.Invoke(((byte)systemType, amount, player.PlayerId));
    }
    static RemoteProcess<(byte,byte,byte)> RpcSabotageLogForAdmin = new("RpcLogForAdmin", (message, _) =>
    {
        if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/LOG"))
        {
            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/LOG/recordSabotageRpc"))
            {
                return;
            }
        }
        else
        {
            return;
        }
        if (IsPlusAdmin(PlayerControl.LocalPlayer))
        {
            if ((SystemTypes)message.Item1 == SystemTypes.Sabotage)
            {
                var system = (SystemTypes)message.Item2;
                var player=PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(p => p.PlayerId == message.Item3);
                if (player==null)
                {
                    PDebug.Log("Not Found Invoke player");
                    return;
                }
                PDebug.Log("Invoke Sabotage:" + system + " Invoker:" + player.name);
            }
        }
    });
    public static void ModernBGPatch(ref BackgroundSetting background)
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)127) == 1)
        {
            background = BackgroundSetting.Modern;
        }
    }
    public static bool ModKillPatch(GamePlayer __instance, IPlayerlike player, CommunicableTextTag playerState, CommunicableTextTag eventDetail, ref KillParameter killParams, KillCondition killCondition, Action<KillResult> callBack)
    {
        var syncs = NebulaSyncObject.GetObjects<Nightmare.NightmareSeed>(Nightmare.disposableNightSeedOption?Nightmare.NightmareSeed.MyTempTag:Nightmare.NightmareSeed.MyTag).ToEnumerable();
        if (syncs.Any(obj =>
        {
            if (player.Position.Distance(obj.Position) < Nightmare.darknessSizeOption * 1.54f)
            {
                if (obj.ActualOwner.TryGetAbility<Nightmare.Ability>(out var ability))
                {
                    return ability.EffectIsActive;
                }
            }
            return false;
        }))
        {
            killParams = killParams & ~KillParameter.WithOverlay;
        }
        return true;
        /*if (playerState == PlayerState.Sniped) 
        {
            NebulaGameManager.Instance?.KillRequestHandler.RequestKill(__instance, player, playerState, eventDetail, killParams & ~KillParameter.WithOverlay, killCondition, (result) =>
            {
                callBack(result);
                /*if (result==KillResult.Kill)
                {
                    SniperKillShowAnimRpc.Invoke(player.RealPlayer.PlayerId);
                }*
            });
            return false;
        }
        return true;*/
    }
    /*static RemoteProcess<byte> SniperKillShowAnimRpc = new RemoteProcess<byte>("SniperShowAnimRpc", (message,_)=>
    {
        var player = GamePlayer.GetPlayer(message);
        if (player!=null&&player.AmOwner)
        {
            HudManager.Instance.KillOverlay.ShowKillAnimation(player.ToAUPlayer().Data, player.ToAUPlayer().Data);
        }
    });*/
    public static void TranslatePatch(string translationKey, ref string __result)
    {
        if (Doomsayer.CanUseOracle)
        {
            switch (translationKey)
            {
                case "role.doomsayer.name":
                    __result = Language.Translate("role.doomsayer2.name");
                    break;
                case "teams.doomsayer":
                    __result = Language.Translate("teams.doomsayer2");
                    break;
                case "end.doomsayer":
                    __result = Language.Translate("end.doomsayertext2");
                    break;
            }
        }
    }
    public static bool JackalPromotePatch(Jackal.Instance __instance, PlayerDieOrDisconnectEvent ev)
    {
        if (JackalUseOldPromoteSidekick)
        {
            if (ev.Player.IsDisconnected)
            {
                if (!ev.Player.ToAUPlayer())
                {
                    if (ev.Player.PlayerState != PlayerStates.Alive && ev.Player.PlayerState != PlayerStates.Revived)
                    {
                        return false;
                    }
                }
                if (ev.Player.ToAUPlayer().Data.IsDead)
                {
                    return false;
                }
            }
            __instance.PromoteSidekick(ev is PlayerExiledEvent);
            return false;
        }
        return true;
    }
    public static void GetNeutralRolePatch(DefinedRole role, ref bool __result)
    {
        if (role is GameMaster)
        {
            __result = false;
        }
    }
    public static void SnatchPatch(RuntimeRole __instance, GamePlayer target, bool isMatched)
    {
        if (SnatcherCannotGuessSnatchedPlayer)
        {
            if (isMatched)
            {
                GameOperatorManager.Instance!.Subscribe<PlayerCanGuessPlayerLocalEvent>(ev =>
                {
                    if (ev.Guesser.AmOwner)
                    {
                        if (ev.Target.PlayerId == target.PlayerId)
                        {
                            ev.CanGuess = false;
                        }
                    }
                }, __instance, 101);
            }
        }
    }
    public static bool ScarletLoverNameColorPatch(ScarletLover.Instance __instance, ref string name, bool canSeeAllInfo)
    {
        UnityEngine.Color loverColor = new Color32(138, 26, 49, 255);
        var myFlirtatious = __instance.MyScarlet;
        RuntimeRole? myFlirtatiousRuntimeRole = myFlirtatious;
        bool canSee = false;
        bool canSeeAll = false;

        if (__instance.MyPlayer.AmOwner) canSee = true;
        if ((myFlirtatiousRuntimeRole?.AmOwner ?? false) || canSeeAllInfo) canSeeAll = true;

        if (canSee || canSeeAll) name += ((canSeeAll && __instance.AmFavorite) ? " ♥" : " ♡").Color(loverColor);
        return false;
    }
    public static bool ScarletNameColorPatch(RuntimeRole __instance, ref string name, bool canSeeAllInfo)
    {
        UnityEngine.Color loverColor = new Color32(138, 26, 49, 255);
        if (GamePlayer.LocalPlayer!.GetModifiers<ScarletLover.Instance>().Any((ScarletLover.Instance f) => f.FlirtatiousId == (__instance as Scarlet.Instance)!.FlirtatiousId)) name += " ♡".Color(loverColor);
        return false;
    }
    static System.Collections.IEnumerator CoDestroyKill(PlayerControl myPlayer, PlayerControl target, Vector3 targetPos, bool moveToLeft)
    {
        myPlayer.moveable = false;
        target.moveable = false;
        if (InVisibleDestroyCanMove && myPlayer.ToNebulaPlayer().IsInvisible)
        {
            target.moveable = true;
        }
        //キルされる相手は今の操作を中断させられる。
        if (target.AmOwner && Minigame.Instance)
        {
            try
            {
                Minigame.Instance.Close();
                Minigame.Instance.Close();
            }
            catch
            {
            }
        }

        //自身が動いている間は相手側もうまいこと動かそうとさせる。
        bool myDone = false;
        var myAnim = Effects.Sequence(myPlayer.MyPhysics.WalkPlayerTo(Destroyer.Ability.GetDestroyKillPosition(targetPos, moveToLeft), 0.001f, 1f, false), ManagedEffects.Action(() => myDone = true).WrapToIl2Cpp());
        var targetAnim = target.MyPhysics.WalkPlayerTo(targetPos, 0.001f, 1f, false);

        var targetCoroutine = NebulaManager.Instance.StartCoroutine(targetAnim);
        var myCoroutine = NebulaManager.Instance.StartCoroutine(myAnim);

        float leftCount = 1.5f;
        while (leftCount > 0f && !myDone)
        {
            leftCount -= Time.deltaTime;
            yield return null;
        }

        if (!myDone)
        {
            NebulaManager.Instance.StopCoroutine(myCoroutine);
        }
        try
        {
            NebulaManager.Instance.StopCoroutine(targetCoroutine);
        }
        catch { }

        target.NetTransform.SnapTo((Vector2)targetPos - target.Collider.offset);
        myPlayer.MyPhysics.body.velocity = Vector2.zero;
        target.MyPhysics.body.velocity = Vector2.zero;

        myPlayer.MyPhysics.FlipX = !moveToLeft;
        target.MyPhysics.FlipX = !moveToLeft;

        //キルモーション

        System.Collections.IEnumerator CoMonitorMeeting()
        {
            while (true)
            {
                if (MeetingHud.Instance)
                {
                    if (!target.Data.IsDead && myPlayer.AmOwner)
                    {
                        myPlayer.GetModInfo()?.MurderPlayer(target.GetModInfo()!, PlayerState.Crushed, null, KillParameter.WithAssigningGhostRole);
                    }
                    yield break;
                }
                yield return null;
            }
        }

        SizeModulator sizeModulator = new(Vector2.one, 10000f, false, 100, "nebula::destroyer", false, false);
        PlayerModInfo.RpcAttrModulator.LocalInvoke((target.PlayerId, sizeModulator, true));

        Coroutine monitorMeetingCoroutine = NebulaManager.Instance.StartCoroutine(CoMonitorMeeting().WrapToIl2Cpp());

        if (myPlayer.AmOwner)
        {
            NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.DestroyKill, myPlayer, target);
        }

        var handRenderer = UnityHelper.CreateObject<SpriteRenderer>("KillerHand", null, Vector3.zero, LayerExpansion.GetPlayersLayer());
        handRenderer.gameObject.AddComponent<PlayerColorRenderer>().SetPlayer(myPlayer.GetModInfo());
        handRenderer.sprite = DestroyerAssets.HandSprite[1].GetSprite();
        handRenderer.flipX = !moveToLeft;
        handRenderer.transform.localScale = new(0.5f, 0.5f, 1f);
        handRenderer.transform.localPosition = targetPos + new Vector3(moveToLeft ? -0.2f : 0.2f, 0.9f, -1f);
        System.Collections.IEnumerator FreshHandAlpha()
        {
            while (true)
            {
                var alpha = myPlayer.cosmetics.currentBodySprite.BodySprite.color.a;
                var color = handRenderer.color;
                color.a = alpha;
                handRenderer.color = color;
                yield return null;
            }
        }
        Coroutine FreshHandCoroutine = NebulaManager.Instance.StartCoroutine(FreshHandAlpha());
        yield return new WaitForSeconds(0.15f);

        //死体を生成
        DeadBody deadBody = GameObject.Instantiate<DeadBody>(GameManager.Instance.deadBodyPrefab[0]);
        GameObject deadBodyObj = deadBody.gameObject;
        deadBody.enabled = Destroyer.CanReportKillSceneOption;
        deadBody.Reported = !Destroyer.CanReportKillSceneOption;
        deadBody.ParentId = target.PlayerId;
        deadBody.transform.localPosition = target.GetTruePosition();
        foreach (var r in deadBody.bodyRenderers) r.enabled = false;
        var splatter = deadBody.bloodSplatter;
        target.SetPlayerMaterialColors(deadBody.bloodSplatter);
        splatter.gameObject.SetActive(false);

        var modSplatterRenderer = UnityHelper.CreateObject<SpriteRenderer>("ModSplatter", null, target.transform.position, splatter.gameObject.layer);
        modSplatterRenderer.sharedMaterial = splatter.sharedMaterial;
        var modSplatter = modSplatterRenderer.gameObject.AddComponent<ModAnimator>();

        var targetModInfo = target.GetModInfo()!;
        target.Visible = true;
        target.inVent = false;
        targetModInfo.Unbox().WillDie = true;
        System.Collections.IEnumerator CoScale(float startScale, float goalScale, float duration, NebulaAudioClip audioClip, bool playKillSE = false)
        {
            var alpha = myPlayer.cosmetics.currentBodySprite.BodySprite.color.a;
            handRenderer.sprite = DestroyerAssets.HandSprite[2].GetSprite();
            yield return new WaitForSeconds(0.15f);

            float scale = startScale;
            float p = 0f;
            handRenderer.sprite = DestroyerAssets.HandSprite[3].GetSprite();

            float randomX = 0f;
            float randomTimer = 0f;

            if (!MeetingHud.Instance) NebulaAsset.PlaySE(audioClip, target.transform.position, 0.8f, Destroyer.KillSEStrengthOption, 1f);

            int sePhase = 0;
            float[] seTime = [0.45f, 0.62f, 0.98f];

            while (p < 1f)
            {
                scale = startScale + (goalScale - startScale) * p;
                handRenderer.transform.localPosition = targetPos + new Vector3(randomX + (moveToLeft ? -0.15f : 0.15f), scale * 0.55f + 0.4f, -1f);
                if (!targetModInfo.IsDead) sizeModulator.Size.y = scale;

                randomTimer -= Time.deltaTime;
                if (randomTimer < 0f)
                {
                    randomX = ((float)System.Random.Shared.NextDouble() - 0.5f) * 0.07f;
                    randomTimer = 0.05f;
                }

                if (playKillSE && !MeetingHud.Instance)
                {
                    if (sePhase < seTime.Length && p > seTime[sePhase])
                    {
                        if (sePhase < 2)
                        {
                            //血を出す
                            splatter.gameObject.SetActive(false);
                            deadBody.transform.localScale = new(sePhase == 0 ? 0.7f : -0.7f, 0.7f, 0.7f);
                            splatter.gameObject.SetActive(true);
                        }
                        else if (sePhase == 2)
                        {
                            modSplatter.PlayOneShot(Destroyer.Ability.spriteModBlood, 12f, true);
                        }

                        NebulaAsset.PlaySE(target.KillSfx, target.transform.position + new Vector3(((float)System.Random.Shared.NextDouble() - 0.5f) * 0.05f, ((float)System.Random.Shared.NextDouble() - 0.5f) * 0.05f, 0f), 0.6f, 1.4f, 1f);
                        sePhase++;
                    }
                }

                p += Time.deltaTime / duration;

                yield return null;
            }

            sizeModulator.Size.y = goalScale;
            handRenderer.transform.localPosition = targetPos + new Vector3(moveToLeft ? -0.12f : 0.12f, startScale * 0.55f + 0.21f, -1f);
            handRenderer.sprite = DestroyerAssets.HandSprite[2].GetSprite();

        }

        int phases = Destroyer.PhasesOfDestroyingOption - 1;

        for (int i = 0; i < phases; i++)
        {
            yield return CoScale(
                1f - 0.4f / phases * i,
                1f - 0.4f / phases * (i + 1),
                1.2f, (i % 2 == 0) ? NebulaAudioClip.Destroyer1 : NebulaAudioClip.Destroyer2);
            yield return new WaitForSeconds(0.7f);
        }
        yield return CoScale(phases == 0 ? 1f : 0.6f, 0f, 3.2f, NebulaAudioClip.Destroyer3, true);

        if (myPlayer.AmOwner && !target.Data.IsDead)
        {
            myPlayer.GetModInfo()?.MurderPlayer(target.GetModInfo()!, PlayerState.Crushed, null, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole, KillCondition.TargetAlive);
        }
        try
        {
            NebulaManager.Instance.StopCoroutine(FreshHandCoroutine);
        }
        catch
        {

        }
        try
        {
            NebulaManager.Instance.StopCoroutine(monitorMeetingCoroutine);
        }
        catch
        {
            //会議に入って停止に失敗しても何もしない
        }

        if (Destroyer.LeaveKillEvidenceOption)
        {
            var bloodRenderer = UnityHelper.CreateObject<SpriteRenderer>("DestroyerBlood", null, (targetPos + new Vector3(0f, 0.1f, 0f)).AsWorldPos(true));
            bloodRenderer.sprite = Destroyer.Ability.spriteBloodPuddle.GetSprite();
            bloodRenderer.color = DynamicPalette.PlayerColors[target.CurrentOutfit.ColorId];
            bloodRenderer.transform.localScale = new(0.45f, 0.45f, 1f);
        }

        GameObject.Destroy(handRenderer.gameObject);
        if (deadBodyObj) GameObject.Destroy(deadBodyObj);

        myPlayer.moveable = true;

        //こちらの目線で死ぬまで待つ
        while (!targetModInfo.IsDead) yield return null;

        target.moveable = true;
        target.inVent = false;
        target.onLadder = false;
        target.inMovingPlat = false;

        yield return new WaitForSeconds(0.2f);

        PlayerModInfo.RpcRemoveAttrByTag.LocalInvoke((targetModInfo.PlayerId, "nebula::destroyer"));

        //血しぶきを片付ける
        yield return new WaitForSeconds(0.5f);
        GameObject.Destroy(modSplatter.gameObject);
    }
    static internal readonly RemoteProcess<(GamePlayer player, GamePlayer target, Vector2 targetPosition, bool moveToLeft)> RpcCoDestroyKillFix = new(
    "DestroyerKillFix",
    (message, _) =>
    {
        NebulaManager.Instance.StartCoroutine(CoDestroyKill(message.player.ToAUPlayer(), message.target.ToAUPlayer(), message.targetPosition, message.moveToLeft).WrapToIl2Cpp());
    }
    );
    public static void RpcDestroyerPatch(Destroyer.Ability __instance, GamePlayer player, bool isUsurped)
    {
        if (player.AmOwner)
        {
            NebulaManager.Instance.StartDelayAction(0.1f, delegate
            {
                GameOperatorManager.Instance?.AllOperators.Do(p =>
                {
                    if (p is ModAbilityButtonImpl button)
                    {
                        if (button.VanillaButton.buttonLabelText.text == Language.Translate("button.label.destroyerKill"))
                        {
                            button.Availability = (button) => false;
                            button.Visibility = (button) => false;
                        }
                    }
                });
                AchievementToken<int> achChallengeToken = new("destroyer.challenge", 0, (val, _) => val >= 3 && (NebulaGameManager.Instance?.EndState?.Winners.Test(__instance.MyPlayer) ?? false));

                var destroyButton = NebulaAPI.Modules.PlayerlikeKillButton(__instance, __instance.MyPlayer, new Virial.Events.Player.PlayerInteractParameter(RealPlayerOnly: true, IsKillInteraction: true), true, Virial.Compat.VirtualKeyInput.Kill, "destroyer.kill",
    Destroyer.KillCoolDownOption.GetCoolDown(__instance.MyPlayer.TeamKillCooldown), "destroyerKill", Virial.Components.ModAbilityButton.LabelType.Impostor,
    null, (player, _) =>
    {
        //左右どちらでキルすればよいか考える
        var targetTruePos = player.RealPlayer.TruePosition.ToUnityVector();
        var targetPos = player.Position.ToUnityVector();
        var canMoveToLeft = Destroyer.Ability.CheckCanMove(__instance.MyPlayer.ToAUPlayer(), Destroyer.Ability.GetDestroyKillPosition(targetPos, true), out var leftDis);
        var canMoveToRight = Destroyer.Ability.CheckCanMove(__instance.MyPlayer.ToAUPlayer(), Destroyer.Ability.GetDestroyKillPosition(targetPos, false), out var rightDis);
        bool moveToLeft = false;
        if (canMoveToLeft && canMoveToRight && leftDis < rightDis) moveToLeft = true;
        else if (!canMoveToRight) moveToLeft = true;

        __instance.SetPrivateField("lastKilling", player.RealPlayer);

        RpcCoDestroyKillFix.Invoke((__instance.MyPlayer, player!.RealPlayer, targetTruePos, moveToLeft));

        new StaticAchievementToken("destroyer.common1");
        new StaticAchievementToken("destroyer.common2");
        achChallengeToken.Value++;
        NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
    }, filterHeavier: p => Destroyer.Ability.CheckDestroyKill(__instance.MyPlayer.ToAUPlayer(), p.RealPlayer.ToAUPlayer().transform.position)).SetLabelType(ModAbilityButton.LabelType.Impostor)
     .SetAsUsurpableButton(__instance);
                destroyButton.OnBroken = _ => Snatcher.RewindKillCooldown();
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(destroyButton.GetKillButtonLike());
                __instance.SetPrivateField("destroyButton", destroyButton);
            });
        }
    }
    public static bool ShowMyRolesScreen(MetaScreen outsideScreen, out Virial.Media.Image? backImage, ref IMetaWidgetOld __result)
    {
        MetaWidgetOld widget = new();
        Virial.Compat.Artifact<GUIScreen> inner = null!;
        var assigned = GamePlayer.LocalPlayer!.AllAssigned().Where(a => a.CanBeAwareAssignment).ToList();
        widget.Append(assigned.Select(a => a.AssignableOnHelp).Smooth(),
            (role) => new MetaWidgetOld.Button(() =>
            {
                var doc = DocumentManager.GetDocument("role." + role.InternalName);
                if (doc == null) return;

                inner.Do(screen =>
                {
                    screen.SetWidget(doc.Build(inner), out _);
                    outsideScreen.ClearBackImage();
                    outsideScreen.SetBackImage(role.ConfigurationHolder?.Illustration, 0.2f);
                });
            }, HelpScreen.RoleTitleAttrUnmasked)
            {
                RawText = role.DisplayColoredName,
                Alignment = IMetaWidgetOld.AlignmentOption.Center
            }, 5, 4, 0, 0.6f);
        var assignable = GamePlayer.LocalPlayer!.Role.AssignableOnHelp.First();
        var num = Mathf.Clamp((assigned.Count / 5) + (assigned.Count % 5 == 0 ? 0 : 1), 1, 4);
        var scrollView = new GUIScrollView(GUIAlignment.Left, new(7.4f, HelpScreen.HelpHeight - (0.675f * num)), () =>
        {
            var doc = DocumentManager.GetDocument("role." + assignable.InternalName);
            return doc?.Build(inner) ?? GUIEmptyWidget.Default;
        });
        inner = scrollView.Artifact;

        widget.Append(new MetaWidgetOld.WrappedWidget(scrollView));

        backImage = assignable.ConfigurationHolder?.Illustration;

        __result = widget;
        return false;
    }

    public static void MadmateHasAnyTasks(GamePlayer __instance, ref bool __result)
    {
        if (!MadmatePLUS.CanSeeAllImpostorOption && MadmatePLUS.CanIdentifyImpostorsOption > 0 && __instance.TryGetModifier<MadmatePLUS.Instance>(out var m))
        {
            __result = true;
        }
    }
    public static void MadmateFeelLikeHaveCrewmateTasks(GamePlayer __instance, ref bool __result)
    {
        if (!MadmatePLUS.CanSeeAllImpostorOption && __instance.TryGetModifier<MadmatePLUS.Instance>(out var m))
        {
            __result = false;
        }
    }
    public static void SetFlashLight(bool useFlashLight)
    {
        FlashlightEnabled = useFlashLight;
        PlayerControl.LocalPlayer.AdjustLighting();
    }
    public static bool AdjustLightingPatch(PlayerControl __instance)
    {
        if (PlayerControl.LocalPlayer != __instance) return false;
        if (GamePlayer.LocalPlayer != null && !(GamePlayer.LocalPlayer.Role.Role is Splicer))
        {
            if (!UseFlashLightMode)
            {
                return true;
            }
        }
        float num = 0f;
        bool flashFlag = false;
        if (FlashlightEnabled) flashFlag = FlashlightEnabled;
        else if (__instance.IsFlashlightEnabled()) flashFlag = true;
        else if (__instance.lightSource.useFlashlight) flashFlag = true;

        if (__instance.IsFlashlightEnabled())
        {
            if (__instance.Data.Role.IsImpostor)
                GameOptionsManager.Instance.CurrentGameOptions.TryGetFloat(FloatOptionNames.ImpostorFlashlightSize, out num);
            else
                GameOptionsManager.Instance.CurrentGameOptions.TryGetFloat(FloatOptionNames.CrewmateFlashlightSize, out num);
        }
        else if (__instance.lightSource.useFlashlight || FlashlightEnabled)
        {
            num = __instance.lightSource.flashlightSize;
        }
        if (UseFlashLightMode)
        {
            if (__instance.Data.Role.IsImpostor)
            {
                num = ImpostorFlashSize;
            }
            else
            {
                num = CrewmateFlashSize;
            }
        }
        __instance.SetFlashlightInputMethod();
        __instance.lightSource.SetupLightingForGameplay(flashFlag, num, __instance.TargetFlashlight.transform);
        return false;
    }
    public static bool GameMasterBlockFreePlayAbility()
    {
        if (HostIsGameMaster)
        {
            return false;
        }
        return true;
    }
    public static bool DetermineGameMasterPre(IRoleTable __instance)
    {
        if (HostIsGameMaster)
        {
            try
            {
                List<NebulaRPCInvoker> allInvokers = new List<NebulaRPCInvoker>();
                var table = __instance as RoleTable;
                __instance.SetRole(PlayerControl.LocalPlayer.PlayerId, GameMaster.MyRole);
                foreach (ValueTuple<DefinedRole, int[], byte> role in table!.roles)
                {
                    allInvokers.Add(PlayerModInfo.RpcSetAssignable.GetInvoker(new ValueTuple<byte, int, int[], RoleType>(role.Item3, role.Item1.Id, role.Item2, RoleType.Role)));
                }
                foreach (ValueTuple<DefinedModifier, int[], byte> modifier in table.modifiers)
                {
                    allInvokers.Add(PlayerModInfo.RpcSetAssignable.GetInvoker(new ValueTuple<byte, int, int[], RoleType>(modifier.Item3, modifier.Item1.Id, modifier.Item2, RoleType.Modifier)));
                }
                allInvokers.Add(NebulaGameManager.RpcStartGame.GetInvoker());
                CombinedRemoteProcess.CombinedRPC.Invoke(allInvokers.ToArray());
                return false;
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        return true;
    }
    public static void AssignGameMasterPre(List<byte> impostors, List<byte> others)
    {
        try
        {
            if (HostIsGameMaster)
            {
                var player = PlayerControl.LocalPlayer;
                if (player == null)
                {
                    PDebug.Log("Host is null");
                    return;
                }
                if (AmongUsClient.Instance.AmHost)
                {
                    if (impostors.Contains(player.PlayerId))
                    {
                        impostors.Remove(player.PlayerId);
                        var p2 = others.Random();
                        impostors.Add(p2);
                        others.Remove(p2);
                    }
                    else if (others.Contains(player.PlayerId))
                    {
                        others.Remove(player.PlayerId);
                    }
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    public static float currentKillTime;
    public static void NebulaGameStart(Game __instance)
    {
        try {
            
            SwapSystem.playerId1 = SwapSystem.playerId2 = SwapSystem.NonSelected;
            GuesserSystem.playerGuess = new Dictionary<byte, int>();
            usebubblenum = 0;
            currentKillTime = 0f;
            DiagnosisModePatchs.yesId = 1;
            DiagnosisModePatchs.noId = 2;
            DiagnosisModePatchs.ExileMessageType = 0;
            DiagnosisModePatchs.isSpecialMeeting = false;
            DiagnosisModePatchs.usedSpecialMeeting = false;
            DiagnosisModePatchs.startmeeting = false;
            GameOperatorManager.Instance!.Subscribe<GameUpdateEvent>(ev =>
            {
                if (DiagnosisModePatchs.IsEnabled)
                {
                    if (!MeetingHud.Instance && !ExileController.Instance)
                    {
                        currentKillTime += Time.deltaTime;
                    }
                }
            }, __instance);
            GameOperatorManager.Instance.Subscribe<TaskPhaseStartEvent>(ev =>
            {
                currentKillTime = 0f;
                DiagnosisModePatchs.isSpecialMeeting = false;
                DiagnosisModePatchs.ExileMessageType = 0;
                EvilTrapperKillCD = 15f;
                if (CanGenerateLastImpostor)
                {
                    if (!AmongUsClient.Instance.AmHost)
                    {
                        return;
                    }
                    if (NebulaGameManager.Instance == null)
                    {
                        return;
                    }
                    var allplayer = NebulaGameManager.Instance.AllPlayerInfo;
                    if (allplayer.Count(p => !p.IsDead && !p.IsDisconnected) >= GenerateLastImpostorIfPlayer && allplayer.Count(p => !p.IsDead && !p.IsDisconnected && p.IsImpostor) == 1)
                    {
                        if (CannotGenerateHasInfected&&allplayer.Any(p=>p.TryGetModifier<Infected.Instance>(out var i)))
                        {
                            return;
                        }
                        if (allplayer.Any(p => p.TryGetModifier<LastImpostor.Instance>(out var li)))
                        {
                            return;
                        }
                        var players = allplayer.Where(p => !p.IsDead &&p.IsImpostor && !p.TryGetModifier<HasLove.Instance>(out var h)).ToList();
                        int index = UnityEngine.Random.Range(0, players.Count);
                        if (players != null && players[index] != null)
                        {
                            players[index].AddModifier(LastImpostor.MyRole);
                        }
                    }
                }
            }, __instance);
            GameOperatorManager.Instance.Subscribe<GameUpdateEvent>(ev =>
            {
                if (DiagnosisModePatchs.isSpecialMeeting)
                {
                    DiagnosisModePatchs.OnSpecialMeetingUpdate();
                }
                /*if (Input.GetKey(KeyCode.RightControl))
                {
                    if (Input.GetKey(KeyCode.A))
                    {
                        spr.transform.localPosition -= new Vector3(0.01f, 0f);
                    }
                    if (Input.GetKey(KeyCode.D))
                    {
                        spr.transform.localPosition += new Vector3(0.01f, 0f);
                    }
                    if (Input.GetKey(KeyCode.W))
                    {
                        spr.transform.localPosition += new Vector3(0f, 0.01f);
                    }
                    if (Input.GetKey(KeyCode.S))
                    {
                        spr.transform.localPosition -= new Vector3(0f, 0.01f);
                    }
                    if (Input.GetKey(KeyCode.U))
                    {
                        spr.transform.localScale += new Vector3(0.01f, 0.01f);
                    }
                    if (Input.GetKey(KeyCode.J))
                    {
                        spr.transform.localScale -= new Vector3(0.01f, 0.01f);
                    }
                }
                if (Input.GetKeyDown(KeyCode.P))
                {
                    PDebug.Log(spr.transform.localScale);
                    PDebug.Log(spr.transform.localPosition);
                }*/
            }, __instance);
            GameOperatorManager.Instance.Subscribe<PlayerDieEvent>(ev =>
            {
                if (ev.Player.AmOwner)
                {
                    var syncs = NebulaSyncObject.GetObjects<Nightmare.NightmareSeed>(Nightmare.disposableNightSeedOption ? Nightmare.NightmareSeed.MyTempTag : Nightmare.NightmareSeed.MyTag).ToEnumerable();
                    if (syncs.Any(obj =>
                    {
                        if (ev.Player.Position.Distance(obj.Position) < Nightmare.darknessSizeOption * 1.54f)
                        {
                            if (obj.ActualOwner.TryGetAbility<Nightmare.Ability>(out var ability))
                            {
                                return ability.EffectIsActive;
                            }
                        }
                        return false;
                    }))
                    {
                        if (ev.Player.PlayerState == PlayerState.Pseudocide)
                        {
                            NebulaManager.Instance.ScheduleDelayAction(()=>GameOperatorManager.Instance.AllOperators.Do(p =>
                            {
                                if (p is ModAbilityButtonImpl button)
                                {
                                    button.InactivateEffect();
                                }
                            }));
                            ev.Player.Unbox().MyState = baddream;
                        }
                        IEnumerator NightmareKillAnim()
                        {
                            if (Constants.ShouldPlaySfx())
                            {
                                var vanillaAnim = HudManager.Instance.KillOverlay.KillAnims[0];
                                SoundManager.Instance.PlaySound(vanillaAnim.Stinger, false, 1f, null).volume = vanillaAnim.StingerVolume;
                            }
                            SpriteRenderer spr = UnityHelper.CreateSpriteRenderer("NightmareKillOverlay", HudManager.Instance.transform, Vector3.zero, LayerExpansion.GetUILayer());
                            spr.sprite = NebulaAPI.AddonAsset.GetResource("nightmarekill/nightmarekill1.png")!.AsImage(100f)!.GetSprite();
                            spr.transform.localScale = new Vector2(0.84f, 0.84f);
                            for (int i = 1; i <= 15; i++)
                            {
                                spr.sprite = NebulaAPI.AddonAsset.GetResource($"nightmarekill/nightmarekill{i}.png")!.AsImage(100f)!.GetSprite();
                                yield return Effects.Wait(0.125f).WrapToManaged();
                            }
                            UnityEngine.Object.Destroy(spr);
                        }
                        NebulaManager.Instance.StartCoroutine(NightmareKillAnim().WrapToIl2Cpp());
                    }
                }
            }, __instance);
            GameOperatorManager.Instance.Subscribe<EndCriteriaMetEvent>(ev =>
            {
                if (JackalCanInvokeSabotage && NoAliveImpostorSabotageJackalWin && ev.OverwrittenEndReason == GameEndReason.Sabotage &&
                __instance.GetAllPlayers().Count(p => !p.IsDead && p.IsImpostor) <= 0 &&
                __instance.GetAllPlayers().Count(p => !p.IsDead && p.Role is Jackal.Instance) > 0)
                {
                    ev.TryOverwriteEnd(NebulaGameEnd.JackalWin, GameEndReason.Sabotage);
                }
                if (__instance.GetAllPlayers().Count(p => !p.IsDead) <= 0)
                {
                    ev.TryOverwriteEnd(NobodyAliveEnd, GameEndReason.Situation);
                }
            }, __instance);
            GameOperatorManager.Instance.Subscribe<PlayerCheckCanKillLocalEvent>(ev =>
            {
                if (ev.Player.IsImpostor && ev.Target.IsImpostor && NebulaGameManager.Instance!.AllPlayerInfo.Any(p => !p.IsDead && p.IsImpostor && p.TryGetModifier<HasLove.Instance>(out var h)))
                {
                    ev.SetAsCanKillForcedly();
                }
            }, __instance, 101);
            GameOperatorManager.Instance.Subscribe<MapOpenSabotageEvent>(ev =>
            {
                if (!HudManager.Instance.SabotageButton.gameObject.activeSelf)
                {
                    MapBehaviour.Instance.infectedOverlay.gameObject.SetActive(false);
                    if (ClientOption.GetValue((ClientOption.ClientOptionType)126) == 1)
                    {
                        MapBehaviour.Instance.ColorControl.SetColor(GamePlayer.LocalPlayer!.Role.Role.Color.ToUnityColor());
                    }
                    else
                    {
                        MapBehaviour.Instance.ColorControl.SetColor(new UnityEngine.Color(0.05f, 0.2f, 1f, 1f));
                    }
                }
                if (GamePlayer.LocalPlayer!.Role is Jackal.Instance && JackalCanInvokeSabotage && HudManager.Instance.SabotageButton.gameObject.activeSelf)
                {
                    MapBehaviour.Instance.ColorControl.SetColor(Jackal.MyRole.RoleColor.ToUnityColor());
                }
                if (ClientOption.GetValue((ClientOption.ClientOptionType)126) == 1)
                {
                    MapBehaviour.Instance.ColorControl.SetColor(GamePlayer.LocalPlayer.Role.Role.Color.ToUnityColor());
                }
            }, __instance, 101);
            GameOperatorManager.Instance.Subscribe<MapOpenNormalEvent>(ev =>
            {
                if (HudManager.Instance.SabotageButton.gameObject.activeSelf && !MeetingHud.Instance)
                {
                    MapBehaviour.Instance.infectedOverlay.gameObject.SetActive(true);
                    if (GamePlayer.LocalPlayer!.Role is Jackal.Instance && JackalCanInvokeSabotage)
                    {
                        MapBehaviour.Instance.ColorControl.SetColor(Jackal.MyRole.RoleColor.ToUnityColor());
                    }
                    else
                    {
                        MapBehaviour.Instance.ColorControl.SetColor(Palette.ImpostorRed);
                    }
                    ConsoleJoystick.SetMode_Sabotage();
                }
                if (ClientOption.GetValue((ClientOption.ClientOptionType)126) == 1)
                {
                    MapBehaviour.Instance.ColorControl.SetColor(GamePlayer.LocalPlayer!.Role.Role.Color.ToUnityColor());
                }
            }, __instance, 101);
            GameOperatorManager.Instance.Subscribe<MeetingPreStartEvent>(ev =>
            {
                CurrentChatId = 0;
                currentIndex = 0;
                CanUseChatList = new List<int>();
                CanUseChatList.Add(0);
                if (ImpostorMeetingChat)
                {
                    var crawler = NebulaGameManager.Instance?.AllPlayerInfo.FirstOrDefault(p => p.Role.Role is CrawlerEngineer);
                    if (crawler != null)
                    {
                        if (GamePlayer.LocalPlayer!.IsImpostor)
                        {
                            if (!GamePlayer.LocalPlayer.IsDead)
                            {
                                CanUseChatList.Add(1);
                            }
                        }
                    }
                }
                if (JackalMeetingChat)
                {
                    if (!(GamePlayer.LocalPlayer!.Role is Jackal.Instance) && !(GamePlayer.LocalPlayer.Role is Sidekick.Instance) && !GamePlayer.LocalPlayer.Modifiers.Any(r => r is SidekickModifier.Instance))
                    {
                    }
                    else if (!GamePlayer.LocalPlayer.IsDead)
                    {
                        CanUseChatList.Add(2);
                    }
                }
                if (LoverMeetingChat)
                {
                    if (!GamePlayer.LocalPlayer!.IsDead && GamePlayer.LocalPlayer.Modifiers.Any(r => r is Lover.Instance))
                    {
                        CanUseChatList.Add(3);
                    }
                }
                if (MoriartyMeetingChat)
                {
                    if (!GamePlayer.LocalPlayer!.IsDead)
                    {
                        if (GamePlayer.LocalPlayer.Role is Moriarty.Instance||GamePlayer.LocalPlayer.TryGetAbility<Moran.Ability>(out var a)||GamePlayer.LocalPlayer.TryGetModifier<MoranModifier.Instance>(out var m))
                        {
                            CanUseChatList.Add(4);
                        }
                    }
                }
            }, __instance);
            GameOperatorManager.Instance.Subscribe<ExileScenePreStartEvent>(ev =>
            {
                CurrentChatId = 0;
                currentIndex = 0;
            }, __instance);
            GameOperatorManager.Instance.Subscribe<GameEndEvent>(ev =>
            {
                CurrentChatId = 0;
                currentIndex = 0;
            }, __instance);
            VitalsMinigame OpenSpecialVitalsMinigame()
            {
                VitalsMinigame? vitalsMinigame = null;
                foreach (RoleBehaviour role in RoleManager.Instance.AllRoles)
                {
                    if (role.Role == RoleTypes.Scientist)
                    {
                        vitalsMinigame = UnityEngine.Object.Instantiate(role.gameObject.GetComponent<ScientistRole>().VitalsPrefab, Camera.main.transform, false);
                        break;
                    }
                }
                if (vitalsMinigame == null) return null!;
                vitalsMinigame.transform.SetParent(Camera.main.transform, false);
                vitalsMinigame.transform.localPosition = new Vector3(0.0f, 0.0f, -50f);
                vitalsMinigame.Begin(null);

                ConsoleTimer.MarkAsNonConsoleMinigame();

                return vitalsMinigame;
            }
            if (CrewmateHasVitals && !GamePlayer.LocalPlayer!.IsImpostor)
            {
                var sprite = HudManager.Instance.UseButton.fastUseSettings[ImageNames.VitalsButton].Image;
                var vitalButton = NebulaAPI.Modules.AbilityButton(__instance, GamePlayer.LocalPlayer, Virial.Compat.VirtualKeyInput.Ability, 0f, "vital", new WrapSpriteLoader(() => sprite), null, _ => !GamePlayer.LocalPlayer.IsDead);
                vitalButton.OnClick = (button) =>
                {
                    VitalsMinigame? vitalsMinigame = OpenSpecialVitalsMinigame();
                    ConsoleTimer.MarkAsNonConsoleMinigame();
                };
                vitalButton.SetLabelType(ModAbilityButton.LabelType.Utility);
                vitalButton.ResetKeyBinding();
            }
            if (ImpostorHasVitals && GamePlayer.LocalPlayer!.IsImpostor)
            {
                var vitalButton = NebulaAPI.Modules.AbilityButton(__instance, GamePlayer.LocalPlayer, Virial.Compat.VirtualKeyInput.Ability, 0f, "vital", Quack.Ability.buttonSprite, null, _ => !GamePlayer.LocalPlayer.IsDead);
                vitalButton.OnClick = (button) =>
                {
                    VitalsMinigame? vitalsMinigame = OpenSpecialVitalsMinigame();

                    ConsoleTimer.MarkAsNonConsoleMinigame();
                };
                vitalButton.SetLabelType(ModAbilityButton.LabelType.Utility);
                vitalButton.ResetKeyBinding();
            }
            if (CanSeeAllPlayerDead)
            {
                GameOperatorManager.Instance.Subscribe<PlayerDieEvent>(ev =>
                {
                    UnityEngine.Object.Instantiate(GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab, HudManager.Instance.transform.parent).Show(ev.Player.ToAUPlayer(), 0);
                }, __instance, 101);
            }
            string GetFormattedTime(float seconds)
            {
                int minute = Mathf.FloorToInt(seconds / 60);
                int second = Mathf.FloorToInt(seconds % 60);
                return $"{minute:D2}:{second:D2}";
            }
            System.Collections.IEnumerator CoAnimTextColor(TextMeshPro tm, UnityEngine.Color color1, UnityEngine.Color color2, float duration)
            {
                float t = 0f;
                while (t < duration)
                {
                    t += Time.deltaTime;
                    tm.color = UnityEngine.Color.Lerp(color1, color2, t / duration);
                    yield return null;
                }
                tm.color = color2;
            }
            /*GameOperatorManager.Instance.Subscribe<GameEndEvent>(ev =>
            {
                hidelevelPlayers = new List<byte>();
            }, __instance);
            if (ClientOption.GetValue((ClientOption.ClientOptionType)126) == 1)
            {
                SendMyHideInfo.Invoke(PlayerControl.LocalPlayer.PlayerId);
            }*/
            if (TimeLimit)
            {

                try
                {
                    float second = TimeLimitMin * 60;
                    //float totalsecond = TimeLimitMin * 60;
                    NoSGUIText noSGUIText = new NoSGUIText(GUIAlignment.Top, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.OverlayTitle), new RawTextComponent(GetFormattedTime(second)));
                    Size size;
                    noSGUIText.PostBuilder = tm =>
                    {
                        tm.color = Palette.White;
                        if (second > 300 && second <= 600)
                        {
                            tm.color = new Color32(255, 242, 219, 255);
                        }
                        else if (second <= 300)
                        {
                            tm.color = new Color32(255, 45, 38, 255);
                        }
                    };
                    GameObject gameObject = noSGUIText.Instantiate(new Anchor(new Virial.Compat.Vector2(1f, 0.5f), new Virial.Compat.Vector3(0f, 0f, 0f)), new Size(100f, 100f), out size);
                    gameObject.transform.SetParent(HudManager.Instance.SettingsButton.transform);
                    gameObject.transform.localPosition = new Vector3(-3.75f, 0.18f, 0f);
                    UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    var lifespan = new GameObjectLifespan(gameObject);
                    TextMeshPro countText = gameObject.GetComponent<TextMeshPro>();
                    countText.text = GetFormattedTime(second);
                    GameOperatorManager.Instance.Subscribe<GameUpdateEvent>(ev =>
                    {
                        /*if (Input.GetKey(KeyCode.RightControl))
                        {
                            if (Input.GetKey(KeyCode.W))
                            {
                                gameObject.transform.localPosition += new Vector3(0f, 0.01f, 0f);
                            }
                            if (Input.GetKey(KeyCode.A))
                            {
                                gameObject.transform.localPosition += new Vector3(-0.01f, 0f, 0f);
                            }
                            if (Input.GetKey(KeyCode.S))
                            {
                                gameObject.transform.localPosition += new Vector3(0f, -0.01f, 0f);
                            }
                            if (Input.GetKey(KeyCode.D))
                            {
                                gameObject.transform.localPosition += new Vector3(0.01f, 0f, 0f);
                            }
                        }
                        if (Input.GetKeyDown(KeyCode.P))
                        {
                            PDebug.Log(gameObject.transform.localPosition);
                        }*/
                        second -= Time.deltaTime;
                        if (second >= 0)
                        {
                            countText.text = GetFormattedTime(second);
                            if (Mathf.FloorToInt(second) == 630)
                            {
                                NebulaManager.Instance.StartCoroutine(CoAnimTextColor(countText, countText.color, new Color32(255, 242, 219, 255), 30));
                            }
                            else if (Mathf.FloorToInt(second) == 330)
                            {
                                NebulaManager.Instance.StartCoroutine(CoAnimTextColor(countText, countText.color, new Color32(255, 45, 38, 255), 30));
                            }
                        }
                        if (second <= 0)
                        {
                            if (AmongUsClient.Instance.AmHost)
                            {
                                var game = NebulaAPI.CurrentGame;
                                if (game == null)
                                {
                                    return;
                                }
                                switch (AmongUsUtil.CurrentMapId)
                                {
                                    case 0:
                                        game.TriggerGameEnd(SkeldEnd, GameEndReason.SpecialSituation);
                                        break;
                                    case 1:
                                        game.TriggerGameEnd(MiraEnd, GameEndReason.SpecialSituation);
                                        break;
                                    case 2:
                                        game.TriggerGameEnd(PolusEnd, GameEndReason.SpecialSituation);
                                        break;
                                    case 3:
                                        game.TriggerGameEnd(SkeldEnd, GameEndReason.SpecialSituation);
                                        break;
                                    case 4:
                                        game.TriggerGameEnd(AirshipEnd, GameEndReason.SpecialSituation);
                                        return;
                                    case 5:
                                        game.TriggerGameEnd(FungleEnd, GameEndReason.SpecialSituation);
                                        break;
                                    default:
                                        return;
                                }
                            }
                        }
                    }, lifespan, 100);
                    GameOperatorManager.Instance.Subscribe<GameEndEvent>(ev =>
                    {
                        if (gameObject != null)
                        {
                            UnityEngine.Object.Destroy(gameObject);
                        }
                        gameObject = null!;
                    }, lifespan, 100);
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log("ExStartGameError Error:"+e.ToString());
        }
    }
    public static void CallMeetingCheck(EmergencyMinigame __instance)
    {
        if (GamePlayer.LocalPlayer == null)
        {
            return;
        }
        if (GamePlayer.LocalPlayer.Role is Jester.Instance && !JesterCanCallMeeting)
        {
            __instance.StatusText.text = Language.Translate("role.jester.cannotmeeting");
            __instance.NumberText.text = string.Empty;
            __instance.ClosedLid.gameObject.SetActive(true);
            __instance.OpenLid.gameObject.SetActive(false);
            __instance.ButtonActive = false;
        }
    }
    public static void SidekickMeetingCheckJackal(Sidekick.Instance __instance)
    {
        if (__instance.MyPlayer.AmOwner)
        {
            GameOperatorManager.Instance?.Subscribe<TaskPhaseRestartEvent>(ev =>
            {
                if (__instance.MyPlayer.IsDead)
                {
                    return;
                }
                if (!NebulaGameManager.Instance!.AllPlayerInfo.Any(p => !p.IsDead && Jackal.IsJackalLeader(p, __instance.JackalTeamId, true)))
                {
                    var jackal = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => Jackal.IsJackalLeader(p, __instance.JackalTeamId, true));
                    if (jackal != null)
                    {
                        __instance.MyPlayer.SetRole(Jackal.MyRole, (jackal as Jackal.Instance)!.RoleArgumentsForSidekick);
                    }
                }
            }, __instance, 100);
        }
    }
    public static void SidekickModifierMeetingCheckJackal(SidekickModifier.Instance __instance)
    {
        if (__instance.MyPlayer.AmOwner)
        {
            GameOperatorManager.Instance?.Subscribe<TaskPhaseRestartEvent>(ev =>
            {
                if (__instance.MyPlayer.IsDead)
                {
                    return;
                }

                if (!NebulaGameManager.Instance!.AllPlayerInfo.Any(p => !p.IsDead && Jackal.IsJackalLeader(p, __instance.JackalTeamId, true)))
                {
                    var jackal = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => Jackal.IsJackalLeader(p, __instance.JackalTeamId, true));
                    if (jackal != null)
                    {
                        __instance.MyPlayer.SetRole(Jackal.MyRole, (jackal as Jackal.Instance)!.RoleArgumentsForSidekick);
                    }
                }
            }, __instance, 100);
        }
    }
    public static void JackalGameEnd(Jackal.Instance __instance, GameEndEvent ev)
    {
        if (ev.EndState.EndCondition != NebulaGameEnd.JackalWin)
        {
            return;
        }
        if (!ev.EndState.Winners.Test(__instance.MyPlayer))
        {
            return;
        }
        if (ev.EndState.EndReason != GameEndReason.Situation)
        {
            return;
        }
        if (NebulaGameManager.Instance!.AllPlayerInfo.Count(p => !p.IsDead) == 2 &&
            NebulaGameManager.Instance!.AllPlayerInfo.Count(p => Jackal.IsJackalTeam(p, __instance.JackalTeamId)) == 2 && __instance.MyPlayer.AmOwner)
        {
            new StaticAchievementToken("jackal.challenge");
            return;
        }
    }
    public static bool TryShowStampRingMenu(Func<bool> showWhile)
    {
        bool gameIsNotStated = !GameManager.Instance || !GameManager.Instance.GameHasStarted;
        bool inMeeting = MeetingHud.Instance || ExileController.Instance || IntroCutscene.Instance || NebulaPreSpawnMinigame.PreSpawnMinigame;
        bool isDead = PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.IsDead;
        bool canSeeSomeStamps = inMeeting || (isDead && NebulaGameManager.Instance!.CanSeeAllInfo) || gameIsNotStated;
        bool shouldShowStampMenu = canSeeSomeStamps;
        bool shouldShowEmoteMenu = !inMeeting && !isDead;
        if (shouldShowStampMenu)
        {
            if (ModSingleton<ShowUp>.Instance.CanUseStamps)
            {
                var stamps = StampManager.GetTableStamps().ToArray();
                NebulaManager.Instance.ShowRingMenu(stamps.Select(stamp => new RingMenu.RingMenuElement(stamp.GetStampWidget(null, PlayerControl.LocalPlayer.PlayerId, GUIAlignment.Center, false, stamps.Length < 7 ? 0.45f : 0.4f), () =>
                {
                    StampManager.SendStamp(stamp);
                })).ToArray(), showWhile, () => DebugScreen.Push(Language.Translate("ui.error.stamp.notLoaded"), 3f));
            }
            else
            {
                DebugScreen.Push(Language.Translate("ui.error.stamp.notAllowed"), 3f);
            }
        }
        else if (shouldShowEmoteMenu)
        {
            if (ModSingleton<ShowUp>.Instance.CanUseStamps && (gameIsNotStated || ModSingleton<ShowUp>.Instance.CanUseEmotes))
            {
                var emotes = EmoteManager.AllEmotes.Where(e => !LobbyBehaviour.Instance || e.Value.CanPlayInLobby);
                NebulaManager.Instance.ShowRingMenu(emotes.Select(emote => new RingMenu.RingMenuElement(emote.Value.LocalIconSupplier, () =>
                {
                    EmoteManager.SendLocalEmote(emote.Key);
                })).ToArray(), showWhile, null);
            }
            else
            {
                //DebugScreen.Push(Language.Translate("ui.error.emote.notAllowed"), 3f);
            }
        }
        return false;
    }
    public static void SabotageTypesFix(ref SystemTypes[] __result)
    {
        if (!PolusHasOxygenSabotage)
        {
            return;
        }
        __result = [SystemTypes.Laboratory, SystemTypes.Comms, SystemTypes.Electrical, SystemTypes.LifeSupp];
    }
    public static bool TrapUpdate(Trapper.Trap __instance)
    {
        if (!AccelTrapOnlyCrewmate && !DecelTrapOnlyImpostor)
        {
            return true;
        }
        if (GamePlayer.LocalPlayer == null)
        {
            return true;
        }
        if (__instance.TypeId < 2 && __instance.Color.a >= 1f)
        {
            //加減速トラップはそれぞれで処理する

            if (__instance.Position.Distance(PlayerControl.LocalPlayer.transform.position) < Trapper.SpeedTrapSizeOption * 0.35f)
            {
                if (AccelTrapOnlyCrewmate)
                {
                    if (__instance.TypeId == 0 && !GamePlayer.LocalPlayer.IsCrewmate)
                    {
                        return false;
                    }
                }
                if (DecelTrapOnlyImpostor)
                {
                    if (__instance.TypeId == 1 && !GamePlayer.LocalPlayer!.IsImpostor)
                    {
                        return false;
                    }
                }
                var invoker = PlayerModInfo.RpcAttrModulator.GetInvoker((PlayerControl.LocalPlayer.PlayerId,
                    new SpeedModulator(__instance.TypeId == 0 ? Trapper.AccelRateOption : Trapper.DecelRateOption, Vector2.one, true, Trapper.SpeedTrapDurationOption, false, 50, "nebula.trap" + __instance.TypeId), false));

                if (NebulaGameManager.Instance?.HavePassed(__instance.GetPrivateField<float>("lastAccelTime"), 0.3f) ?? false)
                {
                    __instance.SetPrivateField("lastAccelTime", NebulaGameManager.Instance!.CurrentTime);
                    invoker.InvokeSingle();
                }
                else
                {
                    invoker.InvokeLocal();
                }
            }
        }
        return false;
    }
    static float EvilTrapperKillCD = 15f;
    public static bool EvilTrapperUpdate(Trapper.EvilAbility __instance)
    {
        if (!EvilTrapperKillTrapPLUS)
        {
            return true;
        }
        if (EvilTrapperKillCD>0f)
        {
            EvilTrapperKillCD -= Time.deltaTime;
            return false;
        }
        __instance.GetPrivateField<List<Trapper.Trap>>("killTraps").RemoveAll((killTrap) =>
        {
            foreach (var p in NebulaGameManager.Instance!.AllPlayerInfo)
            {
                if (p.AmOwner) continue;
                if (p.IsDead || p.ToAUPlayer().Data.Role.IsImpostor) continue;

                if (p.ToAUPlayer().transform.position.Distance(killTrap.Position) < Trapper.KillTrapSizeOption * 0.35f)
                {
                    using (RPCRouter.CreateSection("TrapKill"))
                    {
                        __instance.MyPlayer.MurderPlayer(p, PlayerState.Trapped, EventDetail.Trap, KillParameter.RemoteKill, KillCondition.TargetAlive);
                        Trapper.RpcTrapKill.Invoke(killTrap.ObjectId);
                        __instance.GetPrivateField<AchievementToken<int>>("acTokenChallenge")!.Value++;
                    }

                    return true;
                }
            }
            return false;
        });
        return false;
    }
    public static bool NiceTrapperReleased(Trapper.NiceAbility __instance)
    {
        if (__instance.AmOwner)
        {
            TrapperSystem.OnInactivated(__instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("commTraps"));
        }
        return false;
    }
    public static bool EvilTrapperReleased(Trapper.EvilAbility __instance)
    {
        if (__instance.AmOwner)
        {
            TrapperSystem.OnInactivated(__instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("killTraps"));
        }
        return false;
    }
    public static bool EvilTrapperMeetingStart(Trapper.EvilAbility __instance)
    {
        TrapperSystem.OnMeetingStart(__instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("killTraps"));
        return false;
    }
    public static bool NiceTrapperMeetingStart(Trapper.NiceAbility __instance)
    {
        TrapperSystem.OnMeetingStart(__instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("commTraps"));
        __instance.GetPrivateField<AchievementToken<ValueTuple<bool, int>>>("acTokenChallenge").Value.Item2 = 0;
        return false;
    }
    public static void NiceTrapperActivate(Trapper.NiceAbility __instance, GamePlayer player, bool isUsurped, int leftCost)
    {
        if (player.AmOwner)
        {
            NebulaManager.Instance.StartDelayAction(0.1f, delegate
            {
                GameOperatorManager.Instance?.AllOperators.Do(p =>
                {
                    if (p is ModAbilityButtonImpl button)
                    {
                        if (button.VanillaButton.buttonLabelText.text == Language.Translate("button.label.place"))
                        {
                            button.Availability = (button) => false;
                            button.Visibility = (button) => false;
                        }
                    }
                });
                TrapperSystem.OnActivated(__instance, false, new ValueTuple<int, int>[]
            {
            new ValueTuple<int, int>(0, AccelTrapCost),
            new ValueTuple<int, int>(1, DecelTrapCost),
            new ValueTuple<int, int>(2, Trapper.CostOfCommTrapOption)
            }, __instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("commTraps"), leftCost);
            });
        }
    }
    public static void EvilTrapperActivate(Trapper.EvilAbility __instance, GamePlayer player, bool isUsurped, int leftCost)
    {
        if (player.AmOwner)
        {
            NebulaManager.Instance.StartDelayAction(0.1f, delegate
            {
                GameOperatorManager.Instance?.AllOperators.Do(p =>
                {
                    if (p is ModAbilityButtonImpl button)
                    {
                        if (button.VanillaButton.buttonLabelText.text == Language.Translate("button.label.place"))
                        {
                            button.Availability = (button) => false;
                            button.Visibility = (button) => false;
                        }
                    }
                });
                TrapperSystem.OnActivated(__instance, true, new ValueTuple<int, int>[]
            {
            new ValueTuple<int, int>(0, AccelTrapCost),
            new ValueTuple<int, int>(1, DecelTrapCost),
            new ValueTuple<int, int>(3, Trapper.CostOfKillTrapOption)
            }, __instance.GetPrivateField<List<Trapper.Trap>>("localTraps"), __instance.GetPrivateField<List<Trapper.Trap>>("killTraps"), leftCost);
            });
        }
    }
    public static bool CollatorRegisterResult(Collator.Ability __instance, ValueTuple<Player, RoleTeam> player1, ValueTuple<Player, RoleTeam> player2)
    {
        if (!collatorSendMessageMode&&!DiagnosisModePatchs.IsEnabled)
        {
            return true;
        }
        if (collatorSendMessageMode)
        {
            try
            {
                bool matched = player1.Item2 == player2.Item2;
                if (DiagnosisModePatchs.IsEnabled)
                {
                    matched = true;
                }
                if (player1.Item1.IsImpostor && player2.Item1.IsImpostor)
                {
                    new StaticAchievementToken("collator.common4");
                }
                if (!matched)
                {
                    new StaticAchievementToken("collator.common3");
                }
                var acTokenChallenge = __instance.GetPrivateField<AchievementToken<EditableBitMask<GamePlayer>>>("acTokenChallenge");
                if (acTokenChallenge != null)
                {
                    acTokenChallenge.Value.Add(player1.Item1).Add(player2.Item1);
                }
                NebulaAPI.IncrementStatsEntry("stats.collator.collating", 1);
                if (matched)
                {
                    NebulaAPI.IncrementStatsEntry("stats.collator.matched");
                }
                else
                {
                    NebulaAPI.IncrementStatsEntry("stats.collator.unmatched");
                }
                var mp = PlayerControl.LocalPlayer;
                var name = mp.name;
                mp.SetName(Language.Translate("role.collator.ui.title").Color(new UnityEngine.Color(37, 159, 148)));
                var message = Language.Translate("role.collator.ui.target") + ":" +
                    Environment.NewLine +
                    player1.Item1.Unbox().ColoredDefaultName
                    + Environment.NewLine
                    + player2.Item1.Unbox().ColoredDefaultName
                    + Environment.NewLine
                    + Environment.NewLine
                    + Language.Translate("role.collator.ui.result") + ":" +
                    (matched ? Language.Translate("role.collator.ui.matched").Color(UnityEngine.Color.green) : Language.Translate("role.collator.ui.unmatched").Color(UnityEngine.Color.red)).Bold();
                HudManager.Instance.Chat.AddChat(mp, message);
                mp.SetName(name);
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        else
        {
            bool matched = player1.Item2 == player2.Item2;
            if (DiagnosisModePatchs.IsEnabled)
            {
                matched = true;
            }
            if (player1.Item1.IsImpostor && player2.Item1.IsImpostor) new StaticAchievementToken("collator.common4");
            if (!matched) new StaticAchievementToken("collator.common3");
            var acTokenChallenge = __instance.GetPrivateField<AchievementToken<EditableBitMask<GamePlayer>>>("acTokenChallenge");
            if (acTokenChallenge != null)
            {
                acTokenChallenge.Value.Add(player1.Item1).Add(player2.Item1);
            }

            NebulaAPI.IncrementStatsEntry("stats.collator.collating", 1);
            if (matched)
            {
                NebulaAPI.IncrementStatsEntry("stats.collator.matched");
            }
            else
            {
                NebulaAPI.IncrementStatsEntry("stats.collator.unmatched");
            }

            NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaGUIWidgetEngine.API.VerticalHolder(Virial.Media.GUIAlignment.Left,
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("role.collator.ui.title")),
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(Virial.Text.AttributeAsset.OverlayContent),
                new RawTextComponent(
                    Language.Translate("role.collator.ui.target") + ":<br>"
                    + "  " + player1.Item1.Unbox().ColoredDefaultName + "<br>"
                    + "  " + player2.Item1.Unbox().ColoredDefaultName + "<br>"
                    + "<br>"
                    + Language.Translate("role.collator.ui.result") + ": " + (matched ? Language.Translate("role.collator.ui.matched").Color(UnityEngine.Color.green) : Language.Translate("role.collator.ui.unmatched").Color(UnityEngine.Color.red)).Bold()
                )))
                , MeetingOverlayHolder.IconsSprite[2], Collator.MyRole.RoleColor);
        }
        return false;
    }
    public static bool PsychicReportDeadBody(Psychic.Ability __instance, ReportDeadBodyEvent ev)
    {
        if (!PsychicSendMessageMode&&!DiagnosisModePatchs.IsEnabled)
        {
            return true;
        }
        if (PsychicSendMessageMode)
        {
            try
            {
                if (ev.Reporter.AmOwner && ev.Reported != null)
                {
                    NebulaAPI.IncrementStatsEntry("stats.psychic.messages", 1);
                    List<ValueTuple<string, string>> cand = new List<ValueTuple<string, string>>();
                    if (NebulaGameManager.Instance == null)
                    {
                        return false;
                    }
                    float num = NebulaGameManager.Instance.CurrentTime - ev.Reported!.DeathTime!.Value;
                    if (num > 20f)
                    {
                        __instance.SetPrivateField("lastReported", ev.Reported);
                    }
                    int aboutTime = (int)(num + 2.5f);
                    aboutTime -= aboutTime % 5;
                    if (aboutTime > 0)
                    {
                        cand.Add(new ValueTuple<string, string>("elapsedTime", Language.Translate("options.role.psychic.message.elapsedTime").Replace("%SEC%", aboutTime.ToString())));
                    }
                    if (ev.Reported.MyKiller != null && ev.Reported.MyKiller != ev.Reported)
                    {
                        cand.Add(new ValueTuple<string, string>("killersColor", Language.Translate("options.role.psychic.message.killersColor").Replace("%COLOR%", Language.Translate(DynamicPalette.IsLightColor(DynamicPalette.PlayerColors[(int)ev.Reported.MyKiller.PlayerId]) ? "options.role.psychic.message.inner.lightColor" : "options.role.psychic.message.inner.darkColor"))));
                        if (!DiagnosisModePatchs.IsEnabled)
                        {
                            cand.Add(new ValueTuple<string, string>("killersRole", Language.Translate("options.role.psychic.message.killersRole").Replace("%ROLE%", ev.Reported.MyKiller.Role.DisplayColoredName)));
                            if (ev.Reported.MyKiller.IsDead)
                            {
                                cand.Add(new ValueTuple<string, string>("killerIsDead", Language.Translate("options.role.psychic.message.killerIsDead")));
                            }
                        }
                    }
                    cand.Add(new ValueTuple<string, string>("myRole", Language.Translate("options.role.psychic.message.myRole").Replace("%ROLE%", ev.Reported.Role.DisplayColoredName)));
                    if (ev.Reported.PlayerState != PlayerState.Dead)
                    {
                        cand.Add(new ValueTuple<string, string>("myState", Language.Translate("options.role.psychic.message.myState").Replace("%STATE%", ev.Reported.PlayerState.Text)));
                    }
                    ValueTuple<string, string> valueTuple = cand.Random<ValueTuple<string, string>>();
                    string tag = valueTuple.Item1;
                    string rawText = valueTuple.Item2;
                    var name = Language.Translate("options.role.psychic.message.header") + ":" + ev.Reported.Unbox().ColoredDefaultName;
                    var mp = PlayerControl.LocalPlayer;
                    var mpname = mp.name;
                    mp.SetName(name);
                    HudManager.Instance.Chat.AddChat(mp, rawText);
                    mp.SetName(mpname);
                    new StaticAchievementToken("psychic.common1");
                    new StaticAchievementToken("psychic.common2." + tag);
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
            return false;
        }
        else if (ev.Reporter.AmOwner && ev.Reported != null)
        {
            NebulaAPI.IncrementStatsEntry("stats.psychic.messages", 1);
            List<ValueTuple<string, string>> cand = new List<ValueTuple<string, string>>();
            if (NebulaGameManager.Instance == null)
            {
                return false;
            }
            float num = NebulaGameManager.Instance.CurrentTime - ev.Reported!.DeathTime!.Value;
            if (num > 20f)
            {
                __instance.SetPrivateField("lastReported", ev.Reported);
            }
            int aboutTime = (int)(num + 2.5f);
            aboutTime -= aboutTime % 5;
            if (aboutTime > 0)
            {
                cand.Add(new ValueTuple<string, string>("elapsedTime", Language.Translate("options.role.psychic.message.elapsedTime").Replace("%SEC%", aboutTime.ToString())));
            }
            if (ev.Reported.MyKiller != null && ev.Reported.MyKiller != ev.Reported)
            {
                cand.Add(new ValueTuple<string, string>("killersColor", Language.Translate("options.role.psychic.message.killersColor").Replace("%COLOR%", Language.Translate(DynamicPalette.IsLightColor(DynamicPalette.PlayerColors[(int)ev.Reported.MyKiller.PlayerId]) ? "options.role.psychic.message.inner.lightColor" : "options.role.psychic.message.inner.darkColor"))));
                if (!DiagnosisModePatchs.IsEnabled)
                {
                    cand.Add(new ValueTuple<string, string>("killersRole", Language.Translate("options.role.psychic.message.killersRole").Replace("%ROLE%", ev.Reported.MyKiller.Role.DisplayColoredName)));
                    if (ev.Reported.MyKiller.IsDead)
                    {
                        cand.Add(new ValueTuple<string, string>("killerIsDead", Language.Translate("options.role.psychic.message.killerIsDead")));
                    }
                }
            }
            cand.Add(new ValueTuple<string, string>("myRole", Language.Translate("options.role.psychic.message.myRole").Replace("%ROLE%", ev.Reported.Role.DisplayColoredName)));
            if (ev.Reported.PlayerState != PlayerState.Dead)
            {
                cand.Add(new ValueTuple<string, string>("myState", Language.Translate("options.role.psychic.message.myState").Replace("%STATE%", ev.Reported.PlayerState.Text)));
            }
            (string tag, string rawText) = cand.Random();
            NebulaAPI.CurrentGame?.GetModule<MeetingOverlayHolder>()?.RegisterOverlay(NebulaGUIWidgetEngine.API.VerticalHolder(Virial.Media.GUIAlignment.Left,
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(Virial.Text.AttributeAsset.OverlayTitle), new TranslateTextComponent("options.role.psychic.message.header")),
                new NoSGUIText(Virial.Media.GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(Virial.Text.AttributeAsset.OverlayContent), new RawTextComponent(ev.Reported.Unbox().ColoredDefaultName + "<br>" + rawText)))
                , MeetingOverlayHolder.IconsSprite[3], Psychic.MyRole.RoleColor);

            new StaticAchievementToken("psychic.common1");
            new StaticAchievementToken("psychic.common2." + tag);
            return false;
        }
        return true;
    }
    public static void JackalArgumentsForSidekick(ref int[] __result)
    {
        if (__result == null || !Jackal.JackalizedImpostorOption)
        {
            return;
        }
        int jackalizedid = __result[4];
        if (SidekickPromoteToJackalNoJackalizedRole)
        {
            var arguments = Jackal.GenerateArgument(__result[0], null);
            arguments[1] = __result[1];
            arguments[2] = __result[2];
            arguments[3] = __result[3];
            __result = arguments;
        }
        else if (SidekickPromoteToJackalRandomJackalizedRole)
        {
            var jackalType = AssignmentType.AllTypes.FirstOrDefault(r => r.RelatedRole == Jackal.MyRole);
            var jackalizedRoles = Nebula.Roles.Roles.AllRoles.Where(r => r.AssignmentStatus.HasFlag(AbilityAssignmentStatus.CanLoadToKillNeutral) && (r.GetCustomAllocationParameters(jackalType)?.RoleCountSum ?? 0) > 0).ToList();
            var arguments = Jackal.GenerateArgument(__result[0], jackalizedRoles[UnityEngine.Random.Range(0, jackalizedRoles.Count)]);
            arguments[1] = __result[1];
            arguments[2] = __result[2];
            arguments[3] = __result[3];
            __result = arguments;
        }
    }
    public static bool TeamChatActive;
    //public static Dictionary<int, NebulaOptionPatchs.ExStandardAssignmentParameters> MoriartizedRoleDic = new Dictionary<int, NebulaOptionPatchs.ExStandardAssignmentParameters>();
    //public static Dictionary<int, NebulaOptionPatchs.ExStandardAssignmentParameters> YandereRoleDic = new Dictionary<int, NebulaOptionPatchs.ExStandardAssignmentParameters>();
    public static void LoverChatMeetingStart()
    {
        MeetingStartTime = DateTime.UtcNow;
    }
    public static bool LoverChatAddChat(ChatController __instance, PlayerControl sourcePlayer)
    {
        if (HasFreeChat)
        {
            return true;
        }
        var p = GamePlayer.LocalPlayer;
        if (p == null)
        {
            return true;
        }
        if (!ChatOption)
        {
            return true;
        }
        if (__instance != HudManager.Instance.Chat)
        {
            return true;
        }
        if (p.Role.GetAbility<Busker.Ability>() != null && p.PlayerState == PlayerState.Pseudocide)
        {
            return MeetingHud.Instance != null || LobbyBehaviour.Instance != null;
        }
        bool shouldSeeMessage = p.IsDead || p.TryGetModifier<Lover.Instance>(out var l) && (sourcePlayer.PlayerId == p.PlayerId ||
            (l.MyLover != null && l.MyLover.Get() != null && sourcePlayer.PlayerId == l.MyLover.Get().PlayerId)) || sourcePlayer.PlayerId == p.PlayerId;
        if (DateTime.UtcNow - MeetingStartTime < TimeSpan.FromSeconds(1.0))
        {
            return shouldSeeMessage;
        }
        return MeetingHud.Instance != null || LobbyBehaviour.Instance != null || shouldSeeMessage;
    }
    public static void LoverChatEnableChat(HudManager __instance)
    {
        if (HasFreeChat)
        {
            if (!__instance.Chat.isActiveAndEnabled)
            {
                __instance.Chat.SetVisible(true);
                __instance.Chat.gameObject.SetActive(true);
            }
            return;
        }
        if (!ChatOption)
        {
            return;
        }
        if (GamePlayer.LocalPlayer == null || __instance == null || __instance.Chat == null)
        {
            return;
        }
        var p = GamePlayer.LocalPlayer;
        if (p == null)
        {
            return;
        }
        if (p.TryGetModifier<Lover.Instance>(out var l))
        {
            if (!__instance.Chat.isActiveAndEnabled)
            {
                __instance.Chat.SetVisible(true);
                __instance.Chat.gameObject.SetActive(true);
            }
        }
    }
    [HarmonyPatch]
    public class NebulaOptionPatchs
    {

        [HarmonyPatch(typeof(StartOptionMenuPatch), "Postfix"), HarmonyPrefix]
        public static bool BlockNoS()
        {
            return false;
        }
        [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start)), HarmonyPrefix]
        public static void OptionStart(OptionsMenuBehaviour __instance)
        {
            __instance.transform.localPosition = new(0, 0, -700f);

            foreach (var button in __instance.GetComponentsInChildren<CustomButton>(true))
            {
                if (button.name != "DoneButton") continue;

                button.onClick.AddListener(() =>
                {
                    if (AmongUsClient.Instance && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Started)
                        HudManager.Instance.ShowVanillaKeyGuide();
                });
            }
            var tabs = new List<TabGroup>(__instance.Tabs.ToArray());

            PassiveButton passiveButton;

            //設定項目を追加する

            GameObject nebulaTab = new("NebulaTab");
            nebulaTab.transform.SetParent(__instance.transform);
            nebulaTab.transform.localScale = new Vector3(1f, 1f, 1f);
            nebulaTab.SetActive(false);

            var nebulaScreen = MetaScreen.GenerateScreen(new(5f, 4.5f), nebulaTab.transform, new(0f, -0.28f, -10f), false, false, false);
            void SetNebulaWidget()
            {
                var buttonAttr = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new Vector2(1.45f, 0.22f) };
                MetaWidgetOld nebulaWidget = new();
                nebulaWidget.Append(ClientOption.AllOptions.Values.Where(o => o.ShowOnClientSetting), (option) => new MetaWidgetOld.Button(() =>
                {
                    option.Increment();
                    SetNebulaWidget();
                }, buttonAttr)
                {
                    RawText = option.DisplayName + " : " + option.DisplayValue,
                    PostBuilder = (button, _, _) =>
                    {
                        var detail = option.DisplayDetail;
                        if (detail != null)
                        {
                            button.OnMouseOver.AddListener(() => NebulaManager.Instance.SetHelpWidget(button, detail));
                            button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(button));
                        }
                    }
                }, 3, -1, 0, 0.51f);
                nebulaWidget.Append(new MetaWidgetOld.VerticalMargin(0.2f));
                List<MetaWidgetOld.Button> bottomButtons = new();
                void AddBottomButton(string translationKey, Action action)
                {
                    bottomButtons.Add(new MetaWidgetOld.Button(action, buttonAttr)
                    {
                        TranslationKey = "config.client." + translationKey,
                        Alignment = IMetaWidgetOld.AlignmentOption.Center,
                        PostBuilder = (button, _, _) =>
                        {
                            if (Language.TryTranslate("config.client." + translationKey + ".detail", out var detail))
                            {
                                button.OnMouseOver.AddListener(() => NebulaManager.Instance.SetHelpWidget(button, detail));
                                button.OnMouseOut.AddListener(() => NebulaManager.Instance.HideHelpWidgetIf(button));
                            }
                        }
                    });
                }

                if (ModSingleton<NoSVCRoom>.Instance != null)
                {
                    AddBottomButton("vcSettings", () => NoSVCRoom.VCSettings.OpenSettingScreen(__instance, null, false));
                    AddBottomButton("vcRejoin", () => ModSingleton<NoSVCRoom>.Instance?.Rejoin());
                }
                if (!AmongUsClient.Instance || AmongUsClient.Instance.GameState == InnerNetClient.GameStates.NotJoined)
                {
                    AddBottomButton("vcServerSettings",()=>NoSVCRoom.VCSettings.OpenServerSettingScreen(__instance));
                }
                /*if (NebulaGameManager.Instance?.VoiceChatManager != null)
                {
                    AddBottomButton("vcSettings", () => NebulaGameManager.Instance?.VoiceChatManager?.OpenSettingScreen(__instance));
                    AddBottomButton("vcRejoin", () => NebulaGameManager.Instance?.VoiceChatManager?.Rejoin());
                }*/

                if (!AmongUsClient.Instance || AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
                {
                    AddBottomButton("keyBindings", () =>
                    {
                        __instance.OpenTabGroup(tabs.Count - 1);
                        SetKeyBindingWidget();
                    });
                }

                AddBottomButton("webhook", () => ClientOption.ShowWebhookSetting());
                AddBottomButton("social", () => ClientOption.ShowSocialSetting());

                nebulaWidget.Append(bottomButtons, b => b, 3, -1, 0, 0.51f);

                nebulaScreen.SetWidget(nebulaWidget);
            }

            GameObject keyBindingTab = new GameObject("KeyBindingTab");
            keyBindingTab.transform.SetParent(__instance.transform);
            keyBindingTab.transform.localScale = new Vector3(1f, 1f, 1f);
            keyBindingTab.SetActive(false);

            var keyBindingScreen = MetaScreen.GenerateScreen(new(5f, 4.5f), keyBindingTab.transform, new(0f, -0.28f, -10f), false, false, false);

            IKeyAssignment? currentAssignment = null;

            void SetKeyBindingWidget()
            {
                MetaWidgetOld keyBindingWidget = new();
                TMPro.TextMeshPro? text = null;
                keyBindingWidget.Append(IKeyAssignment.AllKeyAssignments, (assignment) =>
                new MetaWidgetOld.Button(() =>
                {
                    currentAssignment = assignment;
                    SetKeyBindingWidget();
                }, new(TextAttributeOld.NormalAttr) { Size = new Vector2(2.2f, 0.21f) })
                { RawText = assignment.DisplayName + " : " + (currentAssignment == assignment ? Language.Translate("input.recording") : ButtonEffect.KeyCodeInfo.GetKeyDisplayName(assignment.KeyInput)), PostBuilder = (_, _, t) => text = t }, 2, -1, 0, 0.48f);
                keyBindingScreen.SetWidget(keyBindingWidget);
            }

            void CoUpdate()
            {
                if (currentAssignment != null && Input.anyKeyDown)
                {
                    foreach (var keyCode in ButtonEffect.KeyCodeInfo.AllKeyInfo.Values)
                    {
                        if (Input.GetKeyDown(keyCode.keyCode))
                        {
                            currentAssignment.KeyInput = keyCode.keyCode;
                            currentAssignment = null;
                            SetKeyBindingWidget();
                            break;
                        }
                    }
                }
            }

            keyBindingScreen.gameObject.AddComponent<ScriptBehaviour>().UpdateHandler += CoUpdate;

            SetNebulaWidget();
            SetKeyBindingWidget();

            //タブを追加する

            tabs[^1] = (GameObject.Instantiate(tabs[1], null));
            var nebulaButton = tabs[^1];
            nebulaButton.gameObject.name = "NebulaButton";
            nebulaButton.transform.SetParent(tabs[0].transform.parent);
            nebulaButton.transform.localScale = new Vector3(1f, 1f, 1f);
            nebulaButton.Content = nebulaTab;
            var textObj = nebulaButton.transform.FindChild("Text_TMP").gameObject;
            textObj.GetComponent<TextTranslatorTMP>().enabled = false;
            textObj.GetComponent<TMPro.TMP_Text>().text = "NoS";

            tabs.Add((GameObject.Instantiate(tabs[1], null)));
            var keyBindingTabButton = tabs[^1];
            keyBindingTabButton.gameObject.name = "KeyBindingButton";
            keyBindingTabButton.transform.SetParent(tabs[0].transform.parent);
            keyBindingTabButton.transform.localScale = new Vector3(1f, 1f, 1f);
            keyBindingTabButton.Content = keyBindingTab;
            keyBindingTabButton.gameObject.SetActive(false);

            passiveButton = nebulaButton.gameObject.GetComponent<PassiveButton>();
            passiveButton.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                __instance.OpenTabGroup(tabs.Count - 2);
                SetNebulaWidget();
            }
            ));

            float y = tabs[0].transform.localPosition.y, z = tabs[0].transform.localPosition.z;
            if (tabs.Count == 4)
                for (int i = 0; i < 3; i++) tabs[i].transform.localPosition = new Vector3(1.7f * (float)(i - 1), y, z);
            else if (tabs.Count == 5)
                for (int i = 0; i < 4; i++) tabs[i].transform.localPosition = new Vector3(1.62f * ((float)i - 1.5f), y, z);

            __instance.Tabs = new Il2CppReferenceArray<TabGroup>(tabs.ToArray());
        }
    }
    [HarmonyPatch]
    public class IntroPatchs
    {
        [HarmonyPatch(typeof(ShowIntroPatch), "Prefix"), HarmonyPrefix]
        public static bool BlockNoSShow(ref bool __result)
        {
            __result = true;
            return false;
        }
        [HarmonyPatch(typeof(IntroCutscene), "CoBegin"), HarmonyPrefix]
        public static bool CoShowIntro(IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result)
        {
            NebulaAPI.CurrentGame!.AddModule(GeneralConfigurations.CurrentGameMode.InstantiateModule());
            __result = CoBegin(__instance).WrapToIl2Cpp();
            try
            {
                if (IntroShowImpostor)
                {
                    PDebug.Log("addComponent");
                    if (CrewmateHasEchoSkill)
                    {
                        LogicMusic = new NormalGameLogicHnSMusic(GameManager.Instance).Register(NebulaAPI.CurrentGame);
                        LogicDangerLevel = new NormalGameLogicHnSDangerLevel(GameManager.Instance).Register(NebulaAPI.CurrentGame);
                    }
                    PDebug.Log("addend");
                }
                if (CanSeePlayerState)
                {
                    new CrewmateStateTrackerTrigger().Register(NebulaAPI.CurrentGame);
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
            return false;
        }

        static System.Collections.IEnumerator CoBegin(IntroCutscene __instance)
        {
            IntroCutscene.Instance = __instance;
            HudManager.Instance.HideGameLoader();
            __instance.HideAndSeekPanels.SetActive(false);
            __instance.CrewmateRules.SetActive(false);
            __instance.ImpostorRules.SetActive(false);
            __instance.ImpostorName.gameObject.SetActive(false);
            __instance.ImpostorTitle.gameObject.SetActive(false);
            IEnumerable<PlayerControl> shownPlayers = PlayerControl.AllPlayerControls.GetFastEnumerator().OrderBy(p => p.AmOwner ? 0 : 1);
            var myInfo = GamePlayer.LocalPlayer;
            var impostors = GameOptionsManager.Instance.CurrentGameOptions.GetAdjustedNumImpostorsModded(PlayerControl.AllPlayerControls.Count);
            if (myInfo!.Role.Role.Team == Nebula.Roles.Crewmate.Crewmate.MyTeam||myInfo.Role.Role.Team==ViolatorTeam)
            {
                __instance.ImpostorText.text = Language.Translate("intro.impostors").Replace("%NUM%", impostors.ToString()).Replace("%NEUNUM%", NebulaAPI.Configurations.GetSharableVariable<int>("options.assignment.neutral")!.Value.ToString());
            }
            else
            {
                __instance.ImpostorText.gameObject.SetActive(false);
            }
            switch (myInfo?.Role.Role.Team.RevealType)
            {
                case Virial.Assignable.TeamRevealType.OnlyMe:
                    shownPlayers = new PlayerControl[] { PlayerControl.LocalPlayer };
                    break;
                case Virial.Assignable.TeamRevealType.Teams:
                    shownPlayers = shownPlayers.Where(p => p.GetModInfo()?.Role.Role.Team == myInfo.Role.Role.Team);
                    if (myInfo.Role.Role.Team==Impostor.MyTeam)
                    {
                        shownPlayers = shownPlayers.Where(p => p.GetModInfo()?.Role is OutlawLeader.Instance||!OutlawLeader.IsOutlawTeam(p.GetModInfo()));
                    }
                    break;
            }

            if (GeneralConfigurations.MapFlipXOption || GeneralConfigurations.MapFlipYOption)
            {
                Vector2 vec = new(1f, 1f);
                if (GeneralConfigurations.MapFlipXOption)
                {
                    PlayerModInfo.RpcAttrModulator.LocalInvoke((myInfo!.PlayerId, new AttributeModulator(PlayerAttributes.FlipX, 100000f, true, 0, null, false), true));
                    vec.x = -1f;
                }
                if (GeneralConfigurations.MapFlipYOption)
                {
                    PlayerModInfo.RpcAttrModulator.LocalInvoke((myInfo!.PlayerId, new AttributeModulator(PlayerAttributes.FlipY, 100000f, true, 0, null, false), true));
                    vec.y = -1f;
                }
                PlayerModInfo.RpcAttrModulator.LocalInvoke((myInfo!.PlayerId, new SpeedModulator(1f, vec, true, 100000f, true, 0, null, false), true));
            }

            yield return CoShowTeam(__instance, myInfo!, shownPlayers.ToArray(), 3f);
            yield return CoShowRole(__instance, myInfo!);
            ShipStatus.Instance.StartSFX();
            GameObject.Destroy(__instance.gameObject);
            if (IntroShowImpostor && LogicMusic != null)
            {
                LogicMusic.StartMusicWithIntro();
            }
            NebulaGameManager.Instance?.OnGameStart();
            DestroyableSingleton<HudManager>.Instance.ShowVanillaKeyGuide();
        }

        static System.Collections.IEnumerator CoShowTeam(IntroCutscene __instance, GamePlayer myInfo, PlayerControl[] shownPlayers, float duration)
        {
            if (__instance.overlayHandle == null)
            {
                __instance.overlayHandle = DestroyableSingleton<DualshockLightManager>.Instance.AllocateLight();
            }
            yield return ShipStatus.Instance.CosmeticsCache.PopulateFromPlayers();
            SoundManager.Instance.PlaySound(__instance.IntroStinger, false, 1f, null);
            UnityEngine.Color c = myInfo.Role!.Role.Team.Color.ToUnityColor();
            var toColor = myInfo.IsModMadmate() ? Palette.ImpostorRed : c;
            if (myInfo.Role.Role is Violator)
            {
                toColor = Violator.MyRole.UnityColor;
                c = NebulaTeams.CrewmateTeam.Color.ToUnityColor();
            }
            float madFadeBegin = 1.8f;
            float madFadeEnd = 2.4f;
            Vector3 position = __instance.BackgroundBar.transform.position;
            position.y -= 0.25f;
            __instance.BackgroundBar.transform.position = position;
            __instance.BackgroundBar.material.SetColor("_Color", c);
            var team = myInfo.Role.Role.Team;
            if (myInfo.Role.Role is Violator)
            {
                team = NebulaTeams.CrewmateTeam;
            }
            __instance.TeamTitle.text = Language.Translate(team.TranslationKey);
            __instance.TeamTitle.color = c;
            try
            {
                int maxDepth = Mathf.CeilToInt(7.5f);
                for (int i = 0; i < shownPlayers.Length; i++)
                {
                    PlayerControl playerControl = shownPlayers[i];
                    if (playerControl)
                    {
                        NetworkedPlayerInfo data = playerControl.Data;
                        if (data != null)
                        {
                            PoolablePlayer poolablePlayer = __instance.CreatePlayer(i, maxDepth, data, false);
                            if (i == 0 && data.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                            {
                                __instance.ourCrewmate = poolablePlayer;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
            __instance.overlayHandle.color = c;
            UnityEngine.Color fade = UnityEngine.Color.black;
            UnityEngine.Color impColor = UnityEngine.Color.white;
            Vector3 titlePos = __instance.TeamTitle.transform.localPosition;
            float timer = 0f;
            bool ismadmate = myInfo.IsModMadmate();
            bool isviolator = myInfo.Role.Role is Violator;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float num = Mathf.Min(1f, timer / duration);
                __instance.Foreground.material.SetFloat("_Rad", __instance.ForegroundRadius.ExpOutLerp(num * 2f));
                fade.a = Mathf.Lerp(1f, 0f, num * 3f);
                __instance.FrontMost.color = fade;
                float p = timer < madFadeBegin ? 0f : timer > madFadeEnd ? 1f : (timer - madFadeBegin) / (madFadeEnd - madFadeBegin);
                __instance.BackgroundBar.material.SetColor("_Color", UnityEngine.Color.Lerp(c, toColor, p));
                toColor.a = Mathf.Clamp(FloatRange.ExpOutLerp(num, 0f, 1f), 0f, 1f);
                c.a = Mathf.Clamp(FloatRange.ExpOutLerp(num, 0f, 1f), 0f, 1f);
                __instance.TeamTitle.color = ismadmate||isviolator ? UnityEngine.Color.Lerp(c, toColor, p) : c;
                if (ismadmate && p >= 0.79f)
                {
                    __instance.TeamTitle.text = OutlawLeader.IsOutlawTeam(myInfo) ? Language.Translate("role.outlawLeaderShower.name") : Language.Translate(team.TranslationKey).ToUpper()== "CREWMATE"?"MADMATE":Language.Translate("role.madmate.name");
                    __instance.ImpostorText.text = Language.Translate("role.madmateplus.generalBlurb");
                }
                if (isviolator && p >= 0.79f)
                {
                    __instance.TeamTitle.text = Language.Translate(team.TranslationKey).ToUpper()=="CREWMATE"?"VIOLATOR":Language.Translate(ViolatorTeam.TranslationKey);
                    __instance.ImpostorText.text = Language.Translate("intro.violator");
                }
                __instance.RoleText.color = ismadmate||isviolator? UnityEngine.Color.Lerp(c, toColor, p) : c;
                if (ismadmate||isviolator)
                {
                    impColor = UnityEngine.Color.Lerp(UnityEngine.Color.white, toColor, p);
                }
                impColor.a = Mathf.Lerp(0f, 1f, (num - 0.3f) * 3f);
                __instance.ImpostorText.color = impColor;
                titlePos.y = 2.7f - num * 0.3f;
                __instance.TeamTitle.transform.localPosition = titlePos;
                __instance.overlayHandle.color = c.AlphaMultiplied(Mathf.Min(1f, timer * 2f));
                yield return null;
            }
            timer = 0f;
            while (timer < 1f)
            {
                timer += Time.deltaTime;
                float num2 = timer / 1f;
                fade.a = Mathf.Lerp(0f, 1f, num2 * 3f);
                __instance.FrontMost.color = fade;
                __instance.overlayHandle.color = c.AlphaMultiplied(1f - fade.a);
                yield return null;
            }
            yield break;
        }

        static System.Collections.IEnumerator CoShowRole(IntroCutscene __instance, GamePlayer myInfo)
        {
            var role = myInfo.Role;
            var displayrolename = role.DisplayIntroRoleName;
            var displayblurb = role.DisplayIntroBlurb;
            bool ismadmateplus = myInfo.Modifiers.Any(r => r is MadmatePLUS.Instance);
            if (ismadmateplus)
            {
                displayrolename = Language.Translate("role.madmateplus.prefix") + role.DisplayName;
                displayblurb = Language.Translate("role.madmateplus.generalBlurb");
            }
            __instance.RoleText.text = displayrolename;
            __instance.RoleBlurbText.text = displayblurb;
            __instance.RoleBlurbText.transform.localPosition = new(0.0965f, -2.12f, -36f);
            __instance.RoleBlurbText.rectTransform.sizeDelta = new(12.8673f, 0.7f);
            __instance.RoleBlurbText.alignment = TMPro.TextAlignmentOptions.Top;

            foreach (var m in myInfo.Modifiers)
            {
                string? mBlurb = m.DisplayIntroBlurb;
                if (mBlurb != null) __instance.RoleBlurbText.text += "\n" + mBlurb;
            }
            var c = role.Role.Color.ToUnityColor();
            if (ismadmateplus)
            {
                c = Palette.ImpostorRed;
            }
            __instance.RoleText.color = c;
            __instance.YouAreText.color = c;
            __instance.RoleBlurbText.color = c;
            SoundManager.Instance.PlaySound(PlayerControl.LocalPlayer.Data.Role.IntroSound, false, 1f, null);
            __instance.YouAreText.gameObject.SetActive(true);
            __instance.RoleText.gameObject.SetActive(true);
            __instance.RoleBlurbText.gameObject.SetActive(true);
            if (__instance.ourCrewmate == null)
            {
                __instance.ourCrewmate = __instance.CreatePlayer(0, 1, PlayerControl.LocalPlayer.Data, false);
                __instance.ourCrewmate.gameObject.SetActive(false);
            }
            __instance.ourCrewmate.gameObject.SetActive(true);
            __instance.ourCrewmate.transform.localPosition = new Vector3(0f, -1.05f, -18f);
            __instance.ourCrewmate.transform.localScale = new Vector3(1f, 1f, 1f);
            __instance.ourCrewmate.ToggleName(false);
            yield return new WaitForSeconds(2.5f);
            __instance.YouAreText.gameObject.SetActive(false);
            __instance.RoleText.gameObject.SetActive(false);
            __instance.RoleBlurbText.gameObject.SetActive(false);
            __instance.ourCrewmate.gameObject.SetActive(false);
            yield break;
        }
    }
    [HarmonyPatch]
    public class DarkThemePatchs
    {
        [HarmonyPatch(typeof(ChatController), "Update"), HarmonyPostfix]
        public static void Postfix(ChatController __instance)
        {
            if (ClientOption.AllOptions[(ClientOption.ClientOptionType)124].Value == 1)
            {
                ChatBubble chatBubble = __instance.chatBubblePool.Prefab.CastFast<ChatBubble>();
                chatBubble.TextArea.overrideColorTags = false;
                chatBubble.TextArea.color = UnityEngine.Color.white;
                chatBubble.Background.color = UnityEngine.Color.black;
                /*FreeChatInputField inputField = __instance.freeChatField;
                if (inputField != null)
                {
                    inputField.background.color = new Color32(40, 40, 40, byte.MaxValue);
                    inputField.textArea.compoText.Color(UnityEngine.Color.white);
                    inputField.textArea.outputText.color = UnityEngine.Color.white;
                }*/
                Color32 color = new Color32(40, 40, 40, byte.MaxValue);
                __instance.freeChatField.background.color = color;
                __instance.freeChatField.textArea.compoText.Color(UnityEngine.Color.white);
                __instance.freeChatField.textArea.outputText.color = UnityEngine.Color.white;
                __instance.quickChatField.background.color = color;
                __instance.quickChatField.text.color = UnityEngine.Color.white;
            }
            else
            {
                __instance.freeChatField.textArea.outputText.color = UnityEngine.Color.black;
            }
        }
    }
    public class OptionPatchs
    {
        public static void StartPatch(Harmony harmony)
        {
            try
            {
                var t = typeof(OptionPatchs);
                var interfaceType = typeof(Virial.Configuration.IOrderedSharableEntry);
                harmony.Patch(typeof(IntegerConfigurationValue).GetMethod(interfaceType.FullName + ".ChangeValue", AccessTools.all), null, new HarmonyMethod(t.GetMethod("IntergerChangeValue")));
                harmony.Patch(typeof(BoolConfigurationImpl).GetMethod("UpdateValue", AccessTools.all), null, new HarmonyMethod(t.GetMethod("BoolChangeValue")));
                harmony.Patch(typeof(FloatConfigurationValue).GetMethod(interfaceType.FullName + ".ChangeValue", AccessTools.all), null, new HarmonyMethod(t.GetMethod("FloatChangeValue")));
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        static string lastMessageKey = "";
        static RemoteProcess<(string, string)> SetOptionMessageRpc = new RemoteProcess<(string, string)>("SetOptionMessage", (message, _) =>
        {
            var notifier = HudManager.Instance.Notifier;
            var key = message.Item1;
            var item = message.Item2;
            if (lastMessageKey.Contains(key) && notifier.activeMessages.Count > 0)
            {
                notifier.activeMessages[notifier.activeMessages.Count - 1].UpdateMessage(item);
            }
            else
            {
                lastMessageKey = key;
                LobbyNotificationMessage newMessage = UnityEngine.Object.Instantiate<LobbyNotificationMessage>(notifier.notificationMessageOrigin, Vector3.zero, Quaternion.identity, notifier.transform);
                newMessage.transform.localPosition = new Vector3(0f, 0f, -2f);
                var action = () => notifier.OnMessageDestroy(newMessage);
                newMessage.SetUp(item, notifier.settingsChangeSprite, notifier.settingsChangeColor, action);
                notifier.ShiftMessages();
                notifier.AddMessageToQueue(newMessage);
            }
            SoundManager.Instance.PlaySoundImmediate(notifier.settingsChangeSound, false, 1f, 1f, null);
        });
        public static void IntergerChangeValue(ISharableVariable<int> __instance)
        {
            var key = __instance.GetPrivateField<string>("name");
            if (key.Contains("diagnosis"))
            {
                return;
            }
            string valueStr = __instance.CurrentValue.ToString();
            if (key.Contains("secondaryChance"))
            {
                valueStr += Language.Translate("options.percentage");
            }
            else if (key.Contains("chance"))
            {
                valueStr += Language.Translate("options.percentage");
            }
            string item = "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + Language.Translate(key) + Language.Translate("UpdateOptionMessage") + valueStr + "</font>";
            SetOptionMessageRpc.Invoke((key, item));
        }
        public static void BoolChangeValue(BoolConfiguration __instance)
        {
            try
            {
                BoolConfigurationImpl instance = (__instance as BoolConfigurationImpl)!;
                var val = __instance.GetPrivateField<ISharableVariable<bool>>("val");
                var key = val.Name;
                if (key.Contains("diagnosis"))
                {
                    return;
                }
                int boolstr = __instance ? 1 : 0;
                string item = "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + Language.Translate(key) + Language.Translate("UpdateOptionMessage") + Language.Translate("BoolOption" + boolstr) + "</font>";
                SetOptionMessageRpc.Invoke((key, item));
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        public static void FloatChangeValue(ISharableVariable<float> __instance)
        {
            var key = __instance.GetPrivateField<string>("name");
            if (key.Contains("diagnosis"))
            {
                return;
            }
            string FormatFloat(float number)
            {
                string str = number.ToString();

                if (!str.Contains('.'))
                    return str;

                string[] parts = str.Split('.');
                if (parts[1].Length < 2)
                    return str;
                else
                    return number.ToString("F2");
            }
            var value = FormatFloat(__instance.CurrentValue) + key switch
            {
                _ when key.Contains("coolDown") || key.Contains("duration") ||
                       key.Contains("immediate") || key.Contains("relative") ||
                       key.Contains("Cooldown") || key.Contains("Duration") => Language.Translate("options.sec"),
                _ when key.Contains("ratio") => Language.Translate("options.cross"),
                _ when key.Contains("roleChance") => Language.Translate("options.percentage"),
                _ when key.Contains("type") => "",
                _ when key.Contains("Range") => Language.Translate("options.cross"),
                _ => "",
            };
            string item = "<font=\"Barlow-Black SDF\" material=\"Barlow-Black Outline\">" + Language.Translate(key) + Language.Translate("UpdateOptionMessage") + value + "</font>";
            SetOptionMessageRpc.Invoke((key, item));
        }
    }
    public static bool NumberOptionInitializePrefix(NumberOption __instance)
    {
        switch (__instance.Title)
        {
            case StringNames.GameVotingTime:
                __instance.ValidRange = new(0, 600);
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
            case StringNames.GameShortTasks:
            case StringNames.GameLongTasks:
            case StringNames.GameCommonTasks:
                __instance.ValidRange = new(0, 90);
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
            case StringNames.GameKillCooldown:
                __instance.ValidRange = new(0, 180);
                __instance.Increment = 0.5f;
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
            case StringNames.GamePlayerSpeed:
            case StringNames.GameCrewLight:
            case StringNames.GameImpostorLight:
                __instance.Increment = 0.05f;
                __instance.Value = (float)Math.Round(__instance.Value, 2);
                break;
        }
        return true;
    }
    public static void AssignHostOp(PlayerControl player, ref GamePlayer __result)
    {
        if (!GeneralConfigurations.AssignOpToHostOption && AssignHostOpOption && player.AmHost() && GeneralConfigurations.CurrentGameMode == Virial.Game.GameModes.Standard)
        {
            __result.Unbox().PermissionHolder.AddPermission(Permissions.OpPermission, false);
        }
        if (HostIsGameMaster && player.AmHost())
        {
            __result.Unbox().PermissionHolder.AddPermission(Permissions.OpPermission, false);
        }
    }
    public static bool LoverOnDisconnectedPatch(Lover.Instance __instance, PlayerDieOrDisconnectEvent ev)
    {
        if (LoverDisConnectedRemoveLover)
        {
            GamePlayer myLover = __instance.MyLover.Get();
            if (myLover != null || !myLover!.IsDead)
            {
                if (ev is PlayerDisconnectEvent)
                {
                    myLover.RemoveModifier(Lover.MyRole);
                    return false;
                }
            }
        }
        return true;
    }
    public static bool SniperGetTargetPatch(Sniper.SniperRifle __instance, float width, float maxLength, ref IPlayerlike __result)
    {
        try
        {
            if (SniperCanLockMini)
            {
                return true;
            }
            float minLength = maxLength;
            IPlayerlike result = null!;
            var CanKillHidingPlayerOption = NebulaAPI.Configurations.Configuration("options.role.sniper.canKillHidingPlayer", false, null, null);
            var CanKillImpostorOption = NebulaAPI.Configurations.Configuration("options.role.sniper.canKillImpostor", false, null, null);
            var renderer = __instance.CallPrivateMethod<SpriteRenderer>("get_Renderer");
            foreach (var p in GamePlayer.AllPlayerlikes)
            {
                if (p.IsDead || p.AmOwner || ((!CanKillHidingPlayerOption) && p.Logic.InVent || p.IsDived)) continue;
                if (p.RealPlayer.TryGetModifier<Mini.Instance>(out var mini)&&mini.IsMini())
                {
                    continue;
                }
                //仲間は無視
                if (!CanKillImpostorOption && !__instance.Owner.CanKill(p.RealPlayer)) continue;

                //吹っ飛ばされているプレイヤーは無視しない

                //不可視なプレイヤーは無視
                if (p.IsInvisible || p.WillDie) continue;

                var pos = p.TruePosition.ToUnityVector();
                Vector2 diff = pos - (Vector2)renderer.transform.position;

                //移動と回転を施したベクトル
                var vec = diff.Rotate(-renderer.transform.eulerAngles.z);

                if (vec.x > 0 && vec.x < minLength && Mathf.Abs(vec.y) < width * 0.5f)
                {
                    result = p;
                    minLength = vec.x;
                }
            }
            __result = result!;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
            return true;
        }
        return false;
    }
    public static void NavvyCamButton(AbstractPlayerUsurpableAbility __instance, GamePlayer player, bool isUsurped)
    {
        if (NavvyCanUseCamera && __instance.AmOwner && AmongUsUtil.CurrentMapId != 1)
        {
            var cambutton = NebulaAPI.Modules.AbilityButton(__instance, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.SecondaryAbility, null);
            cambutton.Availability = (button) => __instance.MyPlayer.CanMove;
            cambutton.Visibility = (button) => !__instance.MyPlayer.IsDead;
            cambutton.SetAsUsurpableButton(__instance);
            (cambutton as ModAbilityButtonImpl)!.SetSprite(HudManager.Instance.UseButton.fastUseSettings[ImageNames.CamsButton].Image);
            cambutton.OnClick = delegate (ModAbilityButton bfutton)
            {
                byte mapId = AmongUsUtil.CurrentMapId;
                if (mapId != 1)
                {
                    if (navvyMiniGame == null)
                    {
                        SystemConsole systemConsole3 = UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault((SystemConsole x) => x.gameObject.name.Contains("Surv_Panel"))!;
                        if (mapId == 0 || mapId == 3)
                        {
                            systemConsole3 = UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault((SystemConsole x) => x.gameObject.name.Contains("SurvConsole"))!;
                        }
                        else if (mapId == 4)
                        {
                            systemConsole3 = UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault((SystemConsole x) => x.gameObject.name.Contains("task_cams"))!;
                        }
                        else if (mapId==5)
                        {
                            systemConsole3= UnityEngine.Object.FindObjectsOfType<SystemConsole>().FirstOrDefault((SystemConsole x) => x.gameObject.name.Contains("BinocularsSecurityConsole"))!;
                        }
                        if (systemConsole3 == null || Camera.main == null)
                        {
                            return;
                        }
                        navvyMiniGame = UnityEngine.Object.Instantiate<Minigame>(systemConsole3.MinigamePrefab, Camera.main.transform, false);
                    }
                    navvyMiniGame.transform.SetParent(Camera.main.transform, false);
                    navvyMiniGame.transform.localPosition = new UnityEngine.Vector3(0f, 0f, -50f);
                    navvyMiniGame.Begin(null);
                }
            };
            cambutton.SetLabelType(ModAbilityButton.LabelType.Crewmate);
            cambutton.SetLabel("navvyCam");
        }
    }
    public static void LobbyStart(LobbyBehaviour __instance)
    {
        var logoHolder = UnityHelper.CreateObject("MoreRolesLogoHolder", HudManager.Instance.transform.FindChild("NebulaLogoHolder"),new(0f,-0.87f) /*new global::UnityEngine.Vector3(-4.15f, 1.88f)*/, null);
        logoHolder.AddComponent<SortingGroup>();
        //logoHolder.SetAsUIAspectContent(AspectPosition.EdgeAlignments.LeftTop, new UnityEngine.Vector3(1.15f, 0.26f));
        SpriteRenderer spriteRenderer = UnityHelper.CreateObject<SpriteRenderer>("MoreRolesLogo", logoHolder.transform, new UnityEngine.Vector3(-0.26f, 0.24f, 0f), null);
        spriteRenderer.sprite = NebulaAPI.AddonAsset.GetResource("ModLogo1.png")!.AsImage(100f)!.GetSprite();
        spriteRenderer.color = new global::UnityEngine.Color(1f, 1f, 1f, 0.75f);
        spriteRenderer.transform.localScale = new global::UnityEngine.Vector3(0.5f, 0.5f, 1f);
        NoSGUIText noSGUIText = new NoSGUIText(GUIAlignment.Right, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.VersionShower), new RawTextComponent($"<size=125%>Plan 17 Add-On</size> v" +(NebulaAPI.AddonAsset as NebulaAddon)!.Version+ Environment.NewLine +
            Language.Translate("MoreRolesmodinfo")));
        Size size;
        GameObject gameObject = noSGUIText.Instantiate(new Anchor(new global::Virial.Compat.Vector2(0.5f, 0.5f), new global::Virial.Compat.Vector3(0f, 0f, 0f)), new Size(100f, 100f), out size);
        gameObject.transform.SetParent(logoHolder.transform, false);
        gameObject.transform.localPosition = new UnityEngine.Vector3(-0.48f, -0.34f, -0.1f);
        /*System.Collections.IEnumerator CoUpdateLogo()
        {
            while (true)
            {
                logoHolder.SetActive(ClientOption.GetValue(ClientOption.ClientOptionType.ShowNoSLogoInLobby) == 1);
                yield return null;
            }
        }*/
        //__instance.StartCoroutine(CoUpdateLogo().WrapToIl2Cpp());
        /*GameOperatorManager.Instance.Subscribe<GameStartEvent>(delegate (GameStartEvent _)
        {
            UnityEngine.Object.Destroy(logoHolder);
        }, new GameObjectLifespan(logoHolder), 100);*/
    }
    public static void SheriffUnlockKill(Sheriff.Ability __instance)
    {
        try
        {
            if (!OnPlayerDeadSheriffUnlockKill)
            {
                return;
            }
            var killbutton = __instance.GetPrivateField<ModAbilityButtonImpl>("killButton");
            if (killbutton.VanillaButton.transform.FindChild("Overlay") == null)
            {
                return;
            }
            var lockspr = killbutton.VanillaButton.transform.FindChild("Overlay").gameObject;
            GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>(op =>
            {
                foreach (DeadBody dead in Helpers.AllDeadBodies())
                {
                    var ma = (dead.TruePosition - __instance.MyPlayer.TruePosition.ToUnityVector()).magnitude;
                    if (ma <= 6f)
                    {
                        if (lockspr)
                        {
                            UnityEngine.Object.Destroy(lockspr!.gameObject);
                            lockspr = null;
                        }
                    }
                }
            }, __instance, 100);
        }
        catch (Exception ex)
        {
            PDebug.Log(ex);
        }
    }
    public static void AssignExtraModifier(List<byte> impostors, List<byte> others)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            foreach (var player in GameData.Instance.AllPlayers)
            {
                if (player == null || player.Object == null)
                {
                    continue;
                }
                var p = player.Object.ToNebulaPlayer();
                if (p == null)
                {
                    return;
                }
                if (killGuardPlayers != null && killGuardPlayers.ContainsKey(player.Object))
                {
                    p.AddModifier(firstdead.MyRole, [killGuardPlayers[player.Object]]);
                }
                if (DesperateAssignToImp)
                {
                    if (p.IsImpostor)
                    {
                        if (DesperateDoNotAssignToLoverImp && p.TryGetModifier<Lover.Instance>(out var m))
                        {
                            continue;
                        }
                        p.AddModifier(Desperate.MyRole);
                    }
                }
            }
            killGuardPlayers = new Dictionary<PlayerControl, int>();
        }
    }
    public static bool ProgressTrackerUpdateFix(ProgressTracker __instance)
    {
        if (PlayerTask.PlayerHasTaskOfType<IHudOverrideTask>(PlayerControl.LocalPlayer))
        {
            __instance.TileParent.enabled = false;
            return false;
        }
        if (!__instance.TileParent.enabled)
        {
            __instance.TileParent.enabled = true;
        }
        GameData instance = GameData.Instance;
        if (instance)
        {
            try
            {
                int num = (TutorialManager.InstanceExists ? 1 : (instance.AllPlayers.Count - GameManager.Instance.LogicOptions.NumImpostors));
                num -= instance.AllPlayers.GetFastEnumerator().Count((NetworkedPlayerInfo p) => p.Disconnected);
                int TotalTask = 0;
                int completedTasks = 0;
                foreach (GamePlayer p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                {
                    if (!p.IsDisconnected && p.Tasks.IsCrewmateTask)
                    {
                        TotalTask += p.Tasks.Quota;
                        completedTasks += p.Tasks.TotalCompleted;
                    }
                }
                switch (GameManager.Instance.LogicOptions.GetTaskBarMode())
                {
                    case TaskBarMode.Normal:
                        break;
                    case TaskBarMode.MeetingOnly:
                        if (!MeetingHud.Instance)
                        {
                            goto IL_0112;
                        }
                        break;
                    case TaskBarMode.Invisible:
                        __instance.gameObject.SetActive(false);
                        goto IL_0112;
                }
                if (IntroShowImpostor && HasFinal && !isfinal && TotalTask - completedTasks <= EnterFinalLeftTaskNum)
                {
                    isfinal = true;
                    SoundManager.instance.PlaySound(GameManagerCreator.Instance.HideAndSeekManagerPrefab.FinalHideAlertSFX, false, 1f);
                    NebulaManager.Instance.StartCoroutine(CoAnimColor(__instance.TileParent.material, __instance.TileParent.material.GetColor("_FullColor"), Palette.ImpostorRed, 1f).WrapToIl2Cpp());
                    //__instance.TileParent.material.SetColor("_FullColor", Palette.ImpostorRed);
                    System.Collections.IEnumerator CoAnimColor(Material m, UnityEngine.Color color1, UnityEngine.Color color2, float duration)
                    {
                        float t = 0f;
                        while (t < duration)
                        {
                            t += Time.deltaTime;
                            m.SetColor("_FullColor", UnityEngine.Color.Lerp(color1, color2, t / duration));
                            yield return null;
                        }
                        m.SetColor("_FullColor", color2);
                    }
                    /*Game currentGame = NebulaAPI.CurrentGame;
                    if (currentGame != null)
                    {
                        TitleShower module = currentGame.GetModule<TitleShower>();
                        if (module != null)
                        {
                            module.SetText(Language.Translate("general.OnEnterFinal"), Palette.ImpostorRed, 6f);
                        }
                    }*/

                    EditableBitMask<GamePlayer> mask = BitMasks.AsPlayer();
                    NebulaTeams.ImpostorTeam.Color.ToUnityColor().ToHSV(out var h, out _, out _);
                    foreach (GamePlayer p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                    {
                        if (!p.AmOwner && !p.IsDead && !mask.Test(p) && !p.IsImpostor)
                        {
                            mask.Add(p);
                            AmongUsUtil.Ping([p.Position], false, false, postProcess: ping => ping.gameObject.SetHue(360 - h));
                        }
                    }
                    var player = GamePlayer.LocalPlayer;
                    if (player != null && player.IsImpostor && scanbutton == null)
                    {
                        scanbutton = NebulaAPI.Modules.AbilityButton(NebulaAPI.CurrentGame!, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.SidekickAction, null);
                        scanbutton.Availability = (button) => player.CanMove;
                        scanbutton.Visibility = (button) => !player.IsDead;
                        scanbutton.SetImage(globalscanimage);
                        scanbutton.CoolDownTimer = NebulaAPI.Modules.Timer(NebulaAPI.CurrentGame!, FinalImpostorScanCooldown).SetAsAbilityTimer().Start();
                        scanbutton.OnClick = delegate (ModAbilityButton button)
                        {
                            NebulaManager.Instance.StartCoroutine(CoEcho(player.Position.ToUnityVector()).WrapToIl2Cpp());
                            System.Collections.IEnumerator CoEcho(UnityEngine.Vector2 position)
                            {
                                EditableBitMask<GamePlayer> pMask = BitMasks.AsPlayer();
                                float radious = 0f;
                                var circle = EffectCircle.SpawnEffectCircle(null, player.Position.ToUnityVector(), NebulaTeams.ImpostorTeam.Color.ToUnityColor(), 0f, null, true);
                                circle.OuterRadius = () => radious;
                                NebulaTeams.ImpostorTeam.Color.ToUnityColor().ToHSV(out var hue, out _, out _);
                                bool isFirst = true;
                                while (radious < 60f)
                                {
                                    if (MeetingHud.Instance) break;

                                    radious += Time.deltaTime * 7.5f;
                                    foreach (GamePlayer p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                                    {
                                        if (!p.AmOwner && !p.IsDead && !pMask.Test(p) && p.Position.Distance(position) < radious && !p.IsImpostor)
                                        {
                                            pMask.Add(p);
                                            AmongUsUtil.Ping([p.Position], false, isFirst, postProcess: ping => ping.gameObject.SetHue(360 - hue));
                                            isFirst = false;
                                        }
                                    }
                                    yield return null;
                                }
                                circle.Disappear();
                            }
                            scanbutton.StartCoolDown();
                        };
                        scanbutton.SetLabel("globalscan");
                        if (HudManager.Instance.AdminButton != null)
                        {
                            SoundManager.instance.PlaySound(HudManager.Instance.AdminButton.RevealSFX, false);
                        }
                    }
                }
                float num2 = (float)completedTasks / (float)TotalTask * (float)num;
                if (TotalTask == 0)
                {
                    num2 = num;
                }
                __instance.curValue = Mathf.Lerp(__instance.curValue, num2, Time.fixedDeltaTime * 2f);
                if (ShowTaskNum)
                {
                    var text = __instance.gameObject.transform.FindChild("TitleText_TMP");
                    text.GetComponent<TextMeshPro>().text = Language.Translate("vanilla.general.completedTasks").Replace("%TOTALTASK%", TotalTask.ToString()).Replace("%COMPLETEDTASK%", completedTasks.ToString());
                }
            IL_0112:
                __instance.TileParent.material.SetFloat("_Buckets", (float)num);
                __instance.TileParent.material.SetFloat("_FullBuckets", __instance.curValue);
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        return false;
    }
    public static bool CmdReportBody()
    {
        if (IntroShowImpostor)
        {
            return !CannotReport;
        }
        return true;
    }
    public static void RaiderKillPatch2(Raider.Ability __instance, ref bool __result)
    {
        if (__result)
        {
            __result = !RaiderCanUseNormalKill;
        }
    }
    public static void SniperKillPatch2(Sniper.Ability __instance, ref bool __result)
    {
        if (__result)
        {
            __result = !SniperCanUseNormalKill;
        }
    }
    private static Material highlightMaterial = null!;
    private static Material GetHighlightMaterial()
    {
        if (highlightMaterial != null)
        {
            return new Material(highlightMaterial);
        }
        foreach (UnityEngine.Object mat in Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<Material>()))
        {
            if (mat.name == "HighlightMat")
            {
                highlightMaterial = mat.TryCast<Material>()!;
                break;
            }
        }
        return new Material(highlightMaterial);
    }

    private static Console Consolize<C>(GameObject obj, SpriteRenderer renderer = null!) where C : global::Console
    {
        obj.layer = LayerMask.NameToLayer("ShortObjects");
        global::Console console = obj.GetComponent<global::Console>();
        UnityEngine.Object component = obj.GetComponent<PassiveButton>();
        Collider2D collider = obj.GetComponent<Collider2D>();
        if (!console)
        {
            console = obj.AddComponent<C>();
            console.checkWalls = true;
            console.usableDistance = 0.7f;
            console.TaskTypes = new TaskTypes[0];
            console.ValidTasks = new Il2CppReferenceArray<TaskSet>(0L);
            List<global::Console> list = ShipStatus.Instance.AllConsoles.ToList<global::Console>();
            list.Add(console);
            ShipStatus.Instance.AllConsoles = new Il2CppReferenceArray<global::Console>(list.ToArray());
        }
        if (console.Image == null)
        {
            if (renderer != null)
            {
                console.Image = renderer;
            }
            else
            {
                console.Image = obj.GetComponent<SpriteRenderer>();
                console.Image.material = GetHighlightMaterial();
            }
        }
        if (!component)
        {
            PassiveButton passiveButton = obj.AddComponent<PassiveButton>();
            passiveButton.OnMouseOut = new UnityEvent();
            passiveButton.OnMouseOver = new UnityEvent();
            passiveButton._CachedZ_k__BackingField = 0.1f;
            passiveButton.CachedZ = 0.1f;
        }
        if (!collider)
        {
            CircleCollider2D circleCollider2D = obj.AddComponent<CircleCollider2D>();
            circleCollider2D.radius = 0.4f;
            circleCollider2D.isTrigger = true;
        }
        return console;
    }
    public static void ModifyPolusEarly()
    {
        if (PolusHasOxygenSabotage)
        {
            try
            {
                PDebug.Log("GetSkeldAdmin");
                List<global::Console> list = ShipStatus.Instance.AllConsoles.ToList<global::Console>();
                PDebug.Log(VanillaAsset.MapAsset[0].gameObject.name);
                var oxygenConsole = VanillaAsset.MapAsset[0].gameObject.transform.Find("LifeSupport/Ground/LifeSuppTank/NoOxyConsole");
                PDebug.Log("Done");
                var oxygenA = UnityEngine.Object.Instantiate(oxygenConsole);
                PDebug.Log("GenerateOxygenA");
                PlainShipRoom roomObj;
                oxygenA.SetParent(ShipStatus.Instance.FastRooms.TryGetValue(SystemTypes.LifeSupp, out roomObj) ? roomObj.transform : ShipStatus.Instance.transform);
                oxygenA.transform.localPosition = new UnityEngine.Vector3(0.42f, 1.65f, 0f);
                Console console = Consolize<Console>(oxygenA.gameObject, null!);
                console.Room = SystemTypes.LifeSupp;
                console.ConsoleId = 0;
                list.Add(console);
                oxygenConsole = VanillaAsset.MapAsset[0].gameObject.transform.Find("Admin/Ground/admin_walls/NoOxyConsole");
                var oxygenB = UnityEngine.Object.Instantiate(oxygenConsole);
                PDebug.Log("GenerateOxygenB");
                oxygenB.SetParent(ShipStatus.Instance.FastRooms.TryGetValue(SystemTypes.Laboratory, out roomObj) ? roomObj.transform : ShipStatus.Instance.transform);
                oxygenB.transform.localPosition = new UnityEngine.Vector3(-0.83f, 1.78f, 0f);
                console = Consolize<Console>(oxygenB.gameObject, null!);
                console.Room = SystemTypes.Laboratory;
                console.ConsoleId = 1;
                list.Add(console);
                ShipStatus.Instance.AllConsoles = new Il2CppReferenceArray<Console>(list.ToArray());
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
    }
    public static void ModifyPolus()
    {
        if (PolusHasOxygenSabotage)
        {
            try
            {
                PDebug.Log("ModifyPolus");
                GameOperatorManager.Instance!.Subscribe<MapInstantiateEvent>(delegate (MapInstantiateEvent ev)
                {
                    try
                    {
                        PDebug.Log("GenerateOxygenButton");
                        InfectedOverlay infected = MapBehaviour.Instance.infectedOverlay;
                        GameObject restoreOxygen = UnityEngine.Object.Instantiate<GameObject>(VanillaAsset.MapAsset[0].MapPrefab.infectedOverlay.transform.GetChild(4).GetChild(0).gameObject, infected.transform);
                        restoreOxygen.transform.localPosition = new UnityEngine.Vector3(-3.72f, -1.55f, -2f);
                        restoreOxygen.transform.localScale = new UnityEngine.Vector3(0.8f, 0.8f, 1f);
                        SpriteRenderer renderer = restoreOxygen.GetComponent<SpriteRenderer>();
                        renderer.SetCooldownNormalizedUvs();
                        ButtonBehavior button = restoreOxygen.GetComponent<ButtonBehavior>();
                        button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                        button.OnClick.AddListener(delegate
                        {
                            if (!infected.CanUseSabotage)
                            {
                                return;
                            }
                            ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Sabotage, 8);
                        });
                        List<ButtonBehavior> allButtons = infected.allButtons.ToList<ButtonBehavior>();
                        allButtons.Add(button);
                        infected.allButtons = allButtons.ToArray();
                        GameOperatorManager.Instance.Subscribe<GameHudUpdateEvent>(delegate (GameHudUpdateEvent ev)
                        {
                            if (infected.sabSystem != null)
                            {
                                float perc = (infected.DoorsPreventingSabotage ? 1f : infected.sabSystem.PercentCool);
                                if (renderer)
                                {
                                    renderer.material.SetFloat("_Percent", perc);
                                }
                            }
                        }, new GameObjectLifespan(MapBehaviour.Instance.gameObject), 100);
                    }
                    catch (Exception ex)
                    {
                        PDebug.Log(ex);
                    }
                }, NebulaAPI.CurrentGame!, 100);
                LifeSuppSystemType LifeSuppSystem = new LifeSuppSystemType(PolusOxygenSabotageTime);
                ShipStatus.Instance.Systems[SystemTypes.LifeSupp] = LifeSuppSystem.CastFast<ISystemType>();
                GameOperatorManager.Instance.Subscribe<GameUpdateEvent>(delegate (GameUpdateEvent ev)
                {
                    if (LifeSuppSystem.Countdown < 0)
                    {
                        if (NebulaAPI.CurrentGame != null)
                        {
                            NebulaAPI.CurrentGame.TriggerGameEnd(NebulaGameEnds.ImpostorGameEnd, GameEndReason.Sabotage);
                        }
                        LifeSuppSystem.Countdown = 10000;
                    }
                }, new GameObjectLifespan(ShipStatus.Instance.gameObject), 100);
                ShipStatus.Instance.Systems[SystemTypes.Sabotage].CastFast<SabotageSystemType>().specials.Add(LifeSuppSystem.CastFast<IActivatable>());
                List<PlayerTask> specialTasks = ShipStatus.Instance.SpecialTasks.ToList<PlayerTask>();
                specialTasks.Add(VanillaAsset.MapAsset[0].SpecialTasks[3]);
                ShipStatus.Instance.SpecialTasks = specialTasks.ToArray();
            }
            catch (Exception e)
            {
                PDebug.Log("GenerateSabotageButtonError Error:" + e.ToString());
            }
        }
    }
    public static void PlayerStartGame(GamePlayer __instance)
    {
        try
        {
            navvyMiniGame = null!;
            scanbutton = null!;
            SpectreExArrowList = new List<TrackingArrowAbility>();
            SetFlashLight(UseFlashLightMode);
            if (!IntroShowImpostor)
            {
                return;
            }
            if (CrewmateHasEchoSkill && __instance.AmOwner)
            {
                redFullS = AmongUsUtil.GenerateFullscreen(NebulaTeams.ImpostorTeam.Color.ToUnityColor());
                var color = redFullS.color;
                color.a = 0f;
                redFullS.color = color;
            }
            nomusictime = 0f;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    static float LastUseSkill;
    static float nomusictime;
    static AudioSource? MusicSource;
    static bool selectSpawnEnd;
    static Dictionary<string, AudioClip> audioclips = new Dictionary<string, AudioClip>();
    public static void PlayerUpdate(GamePlayer __instance)
    {
        if (!IntroShowImpostor)
        {
            return;
        }
        if (isfinal && __instance.AmOwner && __instance.IsImpostor)
        {
            __instance.GainSpeedAttribute(1f+FinalImpostorSpeedUpOption, 1f, false, 0, "impostor::speedup");
        }
        /*if (HasHNSMusic && __instance.AmOwner && __instance.IsImpostor)
        {
            if (__instance.IsImpostor)
            {
                var hidenseekManager = GameManagerCreator.Instance.HideAndSeekManagerPrefab;
                var collection = hidenseekManager.MusicCollection;
                if (MusicSource == null)
                {
                    MusicSource = SoundManager.instance.PlaySound(collection.ImpostorShortMusic, true);
                    MusicSource.name = "ImpostorMusic";
                }
            }
            if (__instance.IsImpostor)
                return;
        }*/
        if (CrewmateHasEchoSkill && __instance.AmOwner && !__instance.IsImpostor)
        {
            LastUseSkill += Time.deltaTime;
            if (LastUseSkill >= EchoSkillUseTime)
            {
                LastUseSkill = 0f;
                NebulaManager.Instance.StartCoroutine(CoEcho(__instance.Position.ToUnityVector()).WrapToIl2Cpp());
                System.Collections.IEnumerator CoEcho(UnityEngine.Vector2 position)
                {
                    EditableBitMask<GamePlayer> pMask = BitMasks.AsPlayer();
                    float radious = 0f;
                    var circle = EffectCircle.SpawnEffectCircle(null, __instance.Position.ToUnityVector(), NebulaTeams.ImpostorTeam.Color.ToUnityColor(), 0f, null, true);
                    circle.OuterRadius = () => radious;
                    NebulaTeams.ImpostorTeam.Color.ToUnityColor().ToHSV(out var hue, out _, out _);
                    bool isFirst = true;
                    while (radious < NebulaAPI.Configurations.Configuration("options.role.echo.echoRange", new ValueTuple<float, float, float>(2.5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Ratio, null, null))
                    {
                        if (MeetingHud.Instance) break;

                        radious += Time.deltaTime * 5f;
                        foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                        {
                            if (!p.AmOwner && !p.IsDead && !pMask.Test(p) && p.Position.Distance(position) < radious && p.IsImpostor)
                            {
                                pMask.Add(p);
                                AmongUsUtil.Ping([p.Position], false, isFirst, postProcess: ping => ping.gameObject.SetHue(360 - hue));
                                isFirst = false;
                            }
                        }
                        yield return null;
                    }
                    circle.Disappear();
                }
            }
            /*if (nomusictime < ImpostorCannotMoveTime)
            {
                nomusictime += Time.deltaTime;
                return;
            }*/
            var color = redFullS!.color;
            List<float> alphas = new List<float>();
            //List<float> distances = new List<float>();
            var impostors = PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => p.ToNebulaPlayer().IsImpostor).ToArray();
            foreach (var player in impostors)
            {
                alphas.Add((-0.1f * (player.GetTruePosition() - __instance.ToAUPlayer().GetTruePosition()).magnitude) + 0.8f);
                //distances.Add((player.GetTruePosition() - __instance.ToAUPlayer().GetTruePosition()).magnitude);
            }
            color.a = Mathf.Clamp(Mathf.Max(new Il2CppStructArray<float>(alphas.ToArray())), isfinal ? 0.1f : 0f, 0.4f);
            redFullS.color = color;
            color = redFullS.color;
            /*if (HasHNSMusic)
            {
                var hidenseekManager = GameManagerCreator.Instance.HideAndSeekManagerPrefab;
                var collection = hidenseekManager.MusicCollection;
                float distance;
                if (distances.Count <= 0)
                {
                    distance = 1000f;
                }
                else
                {
                    distance = Mathf.Min(new Il2CppStructArray<float>(distances.ToArray()));
                }
                if (MusicSource == null)
                {
                    var audio = collection.NormalMusic;
                    MusicSource = SoundManager.instance.PlaySound(audio, true);
                    MusicSource.name = "Normal";
                }
                if (distance > 8f && MusicSource.name != "Normal")
                {
                    var audio = collection.NormalMusic;
                    MusicSource.Stop();
                    MusicSource.clip = audio;
                    MusicSource.Play();
                    MusicSource.name = "Normal";
                }
                else if (distance > 2.5f && distance <= 8f && MusicSource.name != "Danger1")
                {
                    var audio = collection.DangerLevel1Music;
                    MusicSource.Stop();
                    MusicSource.clip = audio;
                    MusicSource.Play();
                    MusicSource.name = "Danger1";
                }
                else if (distance <= 2.5f && MusicSource.name != "Danger2")
                {
                    var audio = collection.DangerLevel2Music;
                    MusicSource.Stop();
                    MusicSource.clip = audio;
                    MusicSource.Play();
                    MusicSource.name = "Danger2";
                }
            }*/
        }
    }
    public static void UpdateNameText(GamePlayer __instance, TextMeshPro nameText, bool onMeeting = false, bool showDefaultName = false)
    {
        if (IntroShowImpostor && __instance.IsImpostor)
        {
            nameText.color = NebulaTeams.ImpostorTeam.Color.ToUnityColor();
        }
    }

    public static void ItemSupplierStartClient()
    {
        if (IntroShowImpostor)
        {
            isfinal = false;
            var impostors = PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => p.ToNebulaPlayer().IsImpostor).ToArray();
            for (int i = 0; i < impostors.Length; i++)
            {
                var obj = UnityEngine.Object.Instantiate(GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab, HudManager.Instance.transform.parent);
                obj.text.gameObject.SetActive(false);
                if (i != 0)
                {
                    obj.sfx = null;
                }
                obj.transform.position += new UnityEngine.Vector3(i == 0 ? 0 : i == 1 ? 2.5f : -2.5f, 0f);
                obj.Show(impostors[i], 0);
                impostors[i].ToNebulaPlayer().GainSpeedAttribute(0f, ImpostorCannotMoveTime, false, 0, "NebulaHNS:CannotMove");
            }
            if (CannotReport)
            {
                ShipStatus.Instance.BreakEmergencyButton();
            }
        }
    }
    public static void GrowALLFlowers(IGameOperator __instance)
    {
        if (!GameStartGrowALLFlower)
        {
            return;
        }
        foreach (ItemSupplier itemSupplier in (__instance as ItemSupplierManager)!.GetPrivateField<List<ItemSupplier>>("allSuppliers"))
        {
            int age = itemSupplier.GetPrivateField<int>("age");
            while (age < 3)
            {
                itemSupplier.TryGrow();
                age = itemSupplier.GetPrivateField<int>("age");
            }
        }
    }
    public static void GenerateMadmate()
    {
        try
        {
            if (!CanGenerateInfected)
            {
                return;
            }
            if (!AmongUsClient.Instance.AmHost)
            {
                return;
            }
            if (NebulaGameManager.Instance == null)
            {
                return;
            }
            var allplayer = NebulaGameManager.Instance.AllPlayerInfo;
            if (allplayer.Count(p => !p.IsDead && !p.IsDisconnected) >= GenerateIfPlayer && allplayer.Count(p => !p.IsDead && !p.IsDisconnected && p.IsImpostor && !p.TryGetModifier<MadmatePLUS.Instance>(out var i)) == 1 && !allplayer.Any(p => p.TryGetModifier<Infected.Instance>(out var i)))
            {
                var players = allplayer.Where(p => !p.IsDead && !p.IsDisconnected && p.IsCrewmate && !p.IsModMadmate() && !p.TryGetModifier<HasLove.Instance>(out var h)).ToList();
                int index = UnityEngine.Random.Range(0, players.Count);
                if (players != null && players[index] != null)
                {
                    players[index].AddModifier(Infected.MyRole);
                }
            }
        }
        catch (Exception ex)
        {
            PDebug.Log(ex);
        }
    }
    public static void DecorateNameColor(ChatBubble __instance, string playerName, bool isDead, bool voted)
    {
        if (ClientOption.AllOptions[(ClientOption.ClientOptionType)124].Value == 1)
        {
            if (isDead)
            {
                __instance.Background.color = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 0.6f);
            }
            else
            {
                __instance.Background.color = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 1f);
            }
            __instance.TextArea.color = UnityEngine.Color.white;
        }
        try
        {
            GameOperatorManager instance = GameOperatorManager.Instance!;
            var p = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(p => p.PlayerName == playerName)?.Object.ToNebulaPlayer();
            var local = GamePlayer.LocalPlayer;
            PlayerDecorateNameEvent ev = ((instance != null) ? instance.Run<PlayerDecorateNameEvent>(new PlayerDecorateNameEvent(p, ""), false) : null);
            if (ev.Color != null && p != null)
            {
                __instance.NameText.color = ev.Color.Value.ToUnityColor();
            }
            if (local != null && p != null && p.PlayerId == local.PlayerId)
            {
                __instance.NameText.color = p.Role.Role.Color.ToUnityColor();
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    public static bool HasDoctorAbility (GamePlayer p)
    {
        if (p.Role.GetAbility<Doctor.Ability>()!=null)
        {
            return true;
        }
        if (p.Role.GetAbility<Doctor.UsurpedAbility>()!=null)
        {
            return true;
        }
        return false;
    }
    public static bool RequestKillSheildTarget(GamePlayer killer, IPlayerlike target, CommunicableTextTag playerState)
    {
        if (killer.PlayerId != target.RealPlayer.PlayerId && selectPlayer != null && target != null && target.RealPlayer.PlayerId == selectPlayer.PlayerId && DoctorPlayer != null && !DoctorPlayer.IsDead && HasDoctorAbility(DoctorPlayer))
        {
            if (DoctorShieldNoBlockGuessedOption && playerState == PlayerStates.Guessed)
            {
                return true;
            }
            TryKillShieldPlayer.Invoke((killer, target.RealPlayer, true));
            return false;
        }
        return true;
    }
    public static void KillEnd(GamePlayer killer, IPlayerlike target)
    {
    }
    public static void DoctorOutlineUpdate(PlayerControl __instance)
    {
        try
        {
            if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
            {
                return;
            }
            if (PlayerControl.LocalPlayer == __instance)
            {
                foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls)
                {
                    if (selectPlayer != null && playerControl.PlayerId == selectPlayer.PlayerId && DoctorPlayer != null && !DoctorPlayer.IsDead &&HasDoctorAbility(DoctorPlayer))
                    {
                        playerControl.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", 1f);
                        playerControl.cosmetics.currentBodySprite.BodySprite.material.SetColor("_OutlineColor", new Color32(0, 221, 255, 255));
                    }
                    else
                    {
                        playerControl.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Outline", 0f);
                    }
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    public static void DoctorShieldButton(Doctor.Ability __instance, GamePlayer player, bool isUsurped, float vitalTimer)
    {
        if (DoctorCanUseShieldOption && !isUsurped && __instance.AmOwner)
        {
            ObjectTracker<GamePlayer> myTracker = ObjectTrackers.ForPlayer(__instance, null, __instance.MyPlayer, p => ObjectTrackers.StandardPredicate(p) && selectPlayer == null, new Color32(0, 221, 255, 255), false, false);
            var selectbutton = NebulaAPI.Modules.AbilityButton(__instance, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.SecondaryAbility, null);
            selectbutton.Availability = (button) => myTracker.CurrentTarget != null && __instance.MyPlayer.CanMove;
            selectbutton.Visibility = (button) => !__instance.MyPlayer.IsDead && selectPlayer == null;
            (selectbutton as Virial.Components.ModAbilityButton).SetImage(doctorshieldimage);
            selectbutton.OnClick = delegate (ModAbilityButton button)
            {
                setShieldPlayer.Invoke((myTracker.CurrentTarget!, __instance.MyPlayer!));
            };
            selectbutton.SetLabel("doctorshield");
            selectbutton.SetLabelType(ModAbilityButton.LabelType.Crewmate);
        }
    }
    public static void SheriffCanKillPatch(Sheriff.Ability __instance, GamePlayer target, ref bool __result)
    {
        if (!__result)
        {
            if (target.IsModMadmate())
            {
                __result = SheriffCanKillMadmateModifierOption;
            }
            else if (target.TryGetModifier<SidekickModifier.Instance>(out var sidekick))
            {
                __result = SheriffCanKillSidekickModifierOption;
            }
        }
        if (DiagnosisModePatchs.IsEnabled)
        {
            var prob = 0.5f - 0.1f * NebulaGameManager.Instance!.AllPlayerInfo.Count(p => p.MyKiller != null && p.MyKiller.PlayerId == __instance.MyPlayer.PlayerId);
            __result = Helpers.Prob(prob);
        }
    }
    public static List<TrackingArrowAbility> SpectreExArrowList = new List<TrackingArrowAbility>();
    public static void SpectreArrowPatch(RuntimeRole __instance, GamePlayer player)
    {
        if (SpectreExArrowList == null)
        {
            SpectreExArrowList = new List<TrackingArrowAbility>();
        }
        SpectreExArrowList.RemoveAll(delegate (TrackingArrowAbility a)
        {
            if (a.MyPlayer == player)
            {
                a.Release();
                return true;
            }
            return false;
        });
        if (player.AmOwner)
        {
            return;
        }
        if (player.Role.Role is Knight)
        {
            SpectreExArrowList.Add(new TrackingArrowAbility(player, 0f, Knight.MyRole.RoleColor.ToUnityColor(), false).Register(__instance));
        }
        if (player.Role.Role is Knighted)
        {
            SpectreExArrowList.Add(new TrackingArrowAbility(player, 0f, Knighted.MyRole.RoleColor.ToUnityColor(), false).Register(__instance));
        }
        if (player.Role.Role is Yandere)
        {
            SpectreExArrowList.Add(new TrackingArrowAbility(player, 0f, Yandere.MyRole.RoleColor.ToUnityColor(), false).Register(__instance));
        }
        if (player.Role.Role is Moran || player.TryGetModifier<MoranModifier.Instance>(out var m))
        {
            SpectreExArrowList.Add(new TrackingArrowAbility(player, 0f, Moriarty.MyRole.RoleColor.ToUnityColor(), false).Register(__instance));
        }
    }
    public static bool GuesserDead()
    {
        GuesserSystem.OnDead();
        return false;
    }
    public static bool GuesserMeetingStart(Guesser.Ability __instance)
    {
        if (!__instance.GetPrivateField<bool>("awareOfUsurpation"))
        {
            var leftGuess = __instance.GetPrivateField<int>("leftGuess");
            GuesserSystem.OnMeetingStart(leftGuess, delegate
            {
                leftGuess--;
                var flag = __instance.GetPrivateField<bool>("awareOfUsurpation");
                __instance.SetPrivateField("awareOfUsurpation", flag |= __instance.IsUsurped);
            }, false, () => !__instance.IsUsurped);
        }
        return false;
    }
    public static bool GuesserModifierMeetingStart(GuesserModifier.Instance __instance)
    {
        if (__instance.MyPlayer.Role.Role is Guesser)
        {
            return false;
        }
        GuesserSystem.OnMeetingStart(__instance.LeftGuess, delegate
        {
            __instance.LeftGuess--;
        });
        return false;
    }
    public static void VentCanUse(Vent __instance, ref float __result, NetworkedPlayerInfo pc, ref bool canUse, ref bool couldUse)
    {
        if (pc == null || pc.Object == null)
        {
            return;
        }
        var player = pc.Object.ToNebulaPlayer();
        if (player == null)
        {
            return;
        }
        if (player.TryGetModifier<MadmatePLUS.Instance>(out var madmate))
        {
            couldUse = true;
            float num = float.MaxValue;
            if (!pc.Object.inVent || !(Vent.currentVent == __instance))
            {
                couldUse &= (player != null && MadmatePLUS.CanUseVentOption) || pc.Object.inVent || pc.Object.walkingToVent;
                if (player == null || player.Role.TaskType != RoleTaskType.NoTask)
                {
                    couldUse &= !pc.Object.MustCleanVent(__instance.Id);
                }
                couldUse &= !pc.IsDead && pc.Object.CanMove;
            }
            ISystemType systemType;
            if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out systemType))
            {
                VentilationSystem ventilationSystem = systemType.Cast<VentilationSystem>();
                if (ventilationSystem != null && ventilationSystem.IsVentCurrentlyBeingCleaned(__instance.Id))
                {
                    couldUse = false;
                }
            }
            canUse = couldUse;
            if (canUse)
            {
                UnityEngine.Vector3 center = pc.Object.Collider.bounds.center;
                UnityEngine.Vector3 position = __instance.transform.position;
                num = UnityEngine.Vector2.Distance(center, position);
                canUse &= num <= __instance.UsableDistance && !PhysicsHelpers.AnythingBetween(pc.Object.Collider, center, position, Constants.ShipOnlyMask, false);
            }
            __result = num;
        }
        else if (player.TryGetModifier<SidekickModifier.Instance>(out var sidekick) || player.Role.Role.LocalizedName.Contains("sidekick"))
        {
            couldUse = true;
            float num = float.MaxValue;
            if (!pc.Object.inVent || !(Vent.currentVent == __instance))
            {
                couldUse &= (player != null && SidekickCanUseVent) || pc.Object.inVent || pc.Object.walkingToVent;
                if (player == null || player.Role.TaskType != RoleTaskType.NoTask)
                {
                    couldUse &= !pc.Object.MustCleanVent(__instance.Id);
                }
                couldUse &= !pc.IsDead && pc.Object.CanMove;
            }
            ISystemType systemType;
            if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out systemType))
            {
                VentilationSystem ventilationSystem = systemType.Cast<VentilationSystem>();
                if (ventilationSystem != null && ventilationSystem.IsVentCurrentlyBeingCleaned(__instance.Id))
                {
                    couldUse = false;
                }
            }
            canUse = couldUse;
            if (canUse)
            {
                UnityEngine.Vector3 center = pc.Object.Collider.bounds.center;
                UnityEngine.Vector3 position = __instance.transform.position;
                num = UnityEngine.Vector2.Distance(center, position);
                canUse &= num <= __instance.UsableDistance && !PhysicsHelpers.AnythingBetween(pc.Object.Collider, center, position, Constants.ShipOnlyMask, false);
            }
            __result = num;
        }
    }
    public static bool MadmatePlusButtonPatch(HudManager manager)
    {
        manager.UseButton.Refresh();
        if (Player.LocalPlayer == null)
        {
            return true;
        }
        NebulaGameManager instance = NebulaGameManager.Instance!;
        if (instance != null && instance.GameState == NebulaGameStates.NotStarted)
        {
            manager.ReportButton.ToggleVisible(false);
            manager.KillButton.ToggleVisible(false);
            manager.SabotageButton.ToggleVisible(false);
            manager.ImpostorVentButton.ToggleVisible(false);
            return false;
        }
        bool flag = PlayerControl.LocalPlayer.Data != null && PlayerControl.LocalPlayer.Data.IsDead;
        GamePlayer modPlayer = GamePlayer.LocalPlayer;
        RuntimeRole modRole = ((modPlayer != null) ? modPlayer.Role : null)!;
        manager.ReportButton.ToggleVisible(!flag && modRole != null && modRole.CanReport! && GameManager.Instance.CanReportBodies() && ShipStatus.Instance != null);
        manager.KillButton.ToggleVisible((modPlayer == null || modPlayer.ShowKillButton) && !flag);
        manager.SabotageButton.ToggleVisible(modRole != null && modRole.CanInvokeSabotage);
        manager.ImpostorVentButton.ToggleVisible(!flag && ((modRole != null && modRole.CanUseVent) || PlayerControl.LocalPlayer.walkingToVent || PlayerControl.LocalPlayer.inVent));
        manager.MapButton.gameObject.SetActive(true);
        if (modPlayer != null && modPlayer.TryGetModifier<MadmatePLUS.Instance>(out var madmate) && !modPlayer.IsImpostor)
        {
            manager.SabotageButton.ToggleVisible(madmate != null && MadmatePLUS.CanInvokeSabotageOption);
            manager.ImpostorVentButton.ToggleVisible(!flag && ((madmate != null && MadmatePLUS.CanUseVentOption) || PlayerControl.LocalPlayer.walkingToVent || PlayerControl.LocalPlayer.inVent));
        }
        if (modPlayer != null && modPlayer.Role is Jackal.Instance && JackalCanInvokeSabotage)
        {
            manager.SabotageButton.ToggleVisible(true);
        }
        if (modPlayer != null && (modPlayer.Role is Sidekick.Instance || modPlayer.TryGetModifier<SidekickModifier.Instance>(out var sidekick)))
        {
            manager.ImpostorVentButton.ToggleVisible(!flag && ((modPlayer != null && SidekickCanUseVent) || PlayerControl.LocalPlayer.walkingToVent || PlayerControl.LocalPlayer.inVent));
        }
        if (modPlayer != null && modPlayer.TryGetModifier<MoranModifier.Instance>(out var moran))
        {
            manager.KillButton.ToggleVisible(false);
        }
        if (modPlayer != null && modPlayer.TryGetModifier<HasLove.Instance>(out var h))
        {
            manager.SabotageButton.ToggleVisible(false);
        }
        if (modPlayer != null && CannotReport)
        {
            manager.ReportButton.ToggleVisible(!flag && modRole != null && !CannotReport && GameManager.Instance.CanReportBodies() && ShipStatus.Instance != null);
        }
        return false;
    }
    public static void MadmatePlusCanSabotage(ref bool __result)
    {
        if (GamePlayer.LocalPlayer == null)
        {
            return;
        }
        if (MadmatePLUS.CanInvokeSabotageOption && GamePlayer.LocalPlayer.TryGetModifier<MadmatePLUS.Instance>(out var madmate))
        {
            __result = true;
        }
    }
    public static bool MadmatePlusSabotageButton(SabotageButton __instance)
    {
        if (PlayerControl.LocalPlayer.Data.Role.IsImpostor && !PlayerControl.LocalPlayer.inVent)
        {
            return true;
        }
        if (GamePlayer.LocalPlayer == null)
        {
            return true;
        }
        DestroyableSingleton<HudManager>.Instance.ToggleMapVisible(new MapOptions
        {
            Mode = MapOptions.Modes.Sabotage
        });
        return false;
    }
    static PlayerControl? camousCosmic;
    static BoolConfiguration CommSabCamouflagerEffectActive = NebulaAPI.Configurations.Configuration("options.map.commsabCamouflager", false);
    static BoolConfiguration CommSabRandomCosmic = NebulaAPI.Configurations.Configuration("options.map.commsabRandomCosmic", false, () => CommSabCamouflagerEffectActive);
    public static GamePlayer? RandomCFPlayer;
    static RemoteProcess<bool> commsabCFEffect = new RemoteProcess<bool>("CommSabEffect", delegate (bool message, bool _)
    {
        string tag = "CommsCFEffect";
        foreach (PlayerControl pl in PlayerControl.AllPlayerControls)
        {
            var p = pl.ToNebulaPlayer();
            if (message)
            {
                if (CommSabRandomCosmic && RandomCFPlayer != null)
                {
                    p.Unbox().AddOutfit(new OutfitCandidate(RandomCFPlayer.GetOutfit(50), tag, 100, true));
                }
                else
                {
                    p.Unbox().AddOutfit(new OutfitCandidate(NebulaGameManager.Instance!.UnknownOutfit, tag, 100, true));
                }
            }
            else
            {
                p.Unbox().RemoveOutfit(tag);
            }
        }
    }
    );
    static RemoteProcess<GamePlayer> SetRandomCFPlayer = new RemoteProcess<GamePlayer>("CommSabEffect2", delegate (GamePlayer message, bool _)
    {
        RandomCFPlayer = message;
    });
    public static ExtraWin tunnyExtra = NebulaAPI.Preprocessor!.CreateExtraWin("tunny", Tunny.MyRole.RoleColor), knightedExtra = NebulaAPI.Preprocessor!.CreateExtraWin("knighted", Knighted.MyRole.RoleColor);
    public static void CoShow(MeetingCalledAnimation __instance)
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)123) == 1)
        {
            __instance.Stinger = ((__instance.Stinger == ShipStatus.Instance.EmergencyOverlay.Stinger) ? VanillaAsset.MapAsset[4].EmergencyOverlay : VanillaAsset.MapAsset[4].ReportOverlay).Stinger;
        }
    }
    public static List<byte> hidelevelPlayers = new List<byte>();
    static RemoteProcess<byte> SendMyHideInfo = new RemoteProcess<byte>("SendMyHideInfo", (id, _) =>
    {
        if (hidelevelPlayers == null)
        {
            hidelevelPlayers = new List<byte>();
        }
        hidelevelPlayers.Add(id);
    });
    public static void SetLevel(PlayerVoteArea __instance, NetworkedPlayerInfo playerInfo)
    {
        if (hidelevelPlayers.Contains(__instance.TargetPlayerId))
        {
            __instance.LevelNumberText.text = "???";
        }
    }
    public static void ShipStatusStartPos(ShipStatus __instance)
    {
        var now = DateTime.Now;
        bool flag = false;
        if (now.Month == 9 && (now.Day==13||now.Day == 14 || now.Day == 15))
        {
            flag = true;
        }
        if (now.Month == 6 && now.Day == 13)
        {
            flag = true;
        }
        if (!flag)
        {
            return;
        }
        var mapid = AmongUsUtil.CurrentMapId;
        if (mapid == 0 || mapid == 3)
        {
            __instance.transform.FindChild("BirthdayDecorSkeld")?.gameObject.SetActive(true);
        }
    }
    public static void CoStartGame()
    {
        try
        {
            if (DataManager.Settings.Language.CurrentLanguage == SupportedLangs.SChinese)
            {
                var now = DateTime.Now;
                if (now.Month == 6 && now.Day == 13)
                {
                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "祝kc生日快乐！");
                    /*new RemoteProcess("SChat", delegate (bool _)
                    {
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "祝kc生日快乐！");
                    }).Invoke();*/
                }
                if (now.Month == 9 && (now.Day == 14 || now.Day == 15))
                {
                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "今天是开发者Plana的生日!");
                    /*new RemoteProcess("SChat2", delegate (bool _)
                    {
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "今天是开发者Plana的生日!");
                    }).Invoke();*/
                }
                //HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "test");
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
        if (AmongUsClient.Instance.AmHost)
        {
            setShieldPlayer.Invoke(new ValueTuple<GamePlayer, GamePlayer>(null!, null!));
        }
    }
    public static void CommCamouflagerEffectInitialize()
    {
        if (CommSabCamouflagerEffectActive)
        {
            if (CommSabRandomCosmic)
            {
                List<PlayerControl> RandomSelectTarget = new List<PlayerControl>();
                foreach (var p in GameData.Instance.AllPlayers)
                {
                    if (p == null || p.Object == null)
                    {
                        continue;
                    }
                    RandomSelectTarget.Add(p.Object);
                }
                var Cosmic = RandomSelectTarget[UnityEngine.Random.Range(0, RandomSelectTarget.Count)];
                SetRandomCFPlayer.LocalInvoke(Cosmic.ToNebulaPlayer());
            }
            commsabCFEffect.LocalInvoke(true);
        }
    }
    public static void CommCamouflagerEffectComptele()
    {
        if (CommSabCamouflagerEffectActive)
        {
            commsabCFEffect.LocalInvoke(false);
        }
    }
    static DateTime MeetingStartTime = DateTime.MinValue;
    public static ClientData GetClient(PlayerControl player)
    {
        try
        {
            var client = AmongUsClient.Instance.allClients
                .ToArray().FirstOrDefault(cd => cd.Character.PlayerId == player.PlayerId);
            return client!;
        }
        catch
        {
            return null!;
        }
    }
    static bool IsPlusAdmin(PlayerControl player)
    {
        var code = GetClient(player).FriendCode;
        return code is "logospruce#7295" //Plana
        or "soupycloak#8540"//KC
        or "alphacook#6624"//Nonalus
        or "freepit#9942";//白糖
    }
    static bool IsAdmin(PlayerControl player, bool isadminmessage = false)
    {
        var code = GetClient(player).FriendCode;
        if (isadminmessage && code is "marlymoor#2246")
        {
            return true;
        }
        return code is "logospruce#7295" //Plana
            or "soupycloak#8540"//KC
            or "alphacook#6624"//Nonalus
            or "freepit#9942";//白糖
            //or "gladtin#6575";//001
    }
    static bool IsSponser(PlayerControl player,bool isadminmessage=false)
    {
        if (IsAdmin(player,isadminmessage))
        {
            return true;
        }
        var code = GetClient(player).FriendCode;
        return code is "mireepic#9666"//忆梦
            or "primebling#0938"//饭团
            or "vertexfar#2463"//宝月
            or "bayrating#7056";//bonnymine
    }
    static Dictionary<PlayerControl, int> killGuardPlayers = new Dictionary<PlayerControl, int>();
    static RemoteProcess<byte> SetSendKickPlayer = new RemoteProcess<byte>("SetSendKickPlayerRPC", (message, _) =>
    {
        SendKickPlayer = PlayerControl.AllPlayerControls.GetFastEnumerator()!.FirstOrDefault(p => p.PlayerId == message)!;
    });
    static RemoteProcess<(int clientid, bool isban)> KickPlayer = new RemoteProcess<(int clientid, bool isban)>("KickPlayerRPC", (message, _) =>
    {
        if (AmongUsClient.Instance.AmHost)
        {
            AmongUsClient.Instance.KickPlayer(message.clientid, message.isban);
        }
        NebulaManager.Instance.StartDelayAction(1.5f, () =>
        {
            SendKickPlayer = null!;
        });
    });
    static PlayerControl? SendKickPlayer;
    public static bool ShowNotification(string playerName, DisconnectReasons reason)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            return false;
        }
        if (SendKickPlayer == null)
        {
            return true;
        }
        if (reason == DisconnectReasons.Banned)
        {
            NetworkedPlayerInfo data = GetClient(SendKickPlayer).Character.Data;
            if (data == null)
            {
                data = GameData.Instance.GetHost();
            }
            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PlayerWasBannedBy, [playerName, data.PlayerName]));
            return false;
        }
        else if (reason == DisconnectReasons.Kicked)
        {
            NetworkedPlayerInfo data2 = GetClient(SendKickPlayer).Character.Data;
            if (data2 == null)
            {
                data2 = GameData.Instance.GetHost();
            }
            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PlayerWasKickedBy, [playerName, data2.PlayerName]));
            return false;
        }
        return true;
    }
    static TextMeshPro? chatTeamText;
    static List<int>? CanUseChatList;
    static int CurrentChatId, currentIndex;
    public static void ChatUpdate(ChatController __instance)
    {
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
            __instance.freeChatField.textArea.SetText(__instance.freeChatField.textArea.text + GUIUtility.systemCopyBuffer);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
        {
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);
            __instance.freeChatField.textArea.SetText("");
        }
        var srmt = HudManager.Instance.Chat.sendRateMessageText;
        if (!MeetingHud.Instance||LobbyBehaviour.Instance)
        {
            if (chatTeamText!=null)
            {
                chatTeamText.gameObject.SetActive(false);
            }
            if (srmt != null)
            {
               srmt.transform.localPosition = new Vector3(-3.3719f, -1.5046f, -5f);
            }
            return;
        }
        try
        {
            if (MeetingHud.Instance)
            {
                if ((CanUseChatList?.Count??0)<=1)
                {
                    chatTeamText?.gameObject?.SetActive(false);
                    if (srmt != null)
                    {
                        srmt.transform.localPosition = new Vector3(-3.3719f, -1.5046f, -5f);
                    }
                    return;
                }
                if (chatTeamText == null)
                {
                    TextMeshPro countText = UnityEngine.Object.Instantiate<TextMeshPro>(MeetingHud.Instance.TimerText, HudManager.Instance.Chat.freeChatField.transform);
                    countText.gameObject.SetActive(true);
                    countText.gameObject.GetComponent<TextTranslatorTMP>().enabled = false;
                    countText.alignment = TextAlignmentOptions.Left;
                    countText.transform.localPosition = new UnityEngine.Vector3(-1.24f, 0.49f);
                    countText.color = Palette.White;
                    chatTeamText = countText;
                }
                if (chatTeamText != null)
                {
                    chatTeamText.gameObject.SetActive(true);
                }
                if (srmt != null)
                {
                    srmt.transform.localPosition = new Vector3(-0.74f, -1.5f, -5f);
                }
                if (Input.GetMouseButtonDown(1))
                {
                    currentIndex += 1;
                    int nextindex = currentIndex;
                    if (nextindex >= CanUseChatList!.Count)
                    {
                        nextindex = 0;
                        currentIndex = 0;
                    }
                    CurrentChatId = CanUseChatList[nextindex];
                    if (CurrentChatId==0)
                    {
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.public").Color("green"));
                        var color = UnityEngine.Color.white;
                        __instance.freeChatField.background.color = color;
                        __instance.quickChatField.background.color = color;
                        __instance.freeChatField.textArea.outputText.color = UnityEngine.Color.black;
                    }
                }
                if (CurrentChatId == 0)
                {
                    chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.public").Color("green"));
                }
                UnityEngine.Color c = UnityEngine.Color.white;
                switch (CurrentChatId)
                {
                    case 1:
                        c = GroupConfigurationColor.ImpostorRed;
                        __instance.freeChatField.background.color = c;
                        __instance.quickChatField.background.color = c;
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.impostortext").Color(Palette.ImpostorRed));
                        break;
                    case 2:
                        c = Jackal.MyRole.UnityColor;
                        __instance.freeChatField.background.color = c;
                        __instance.quickChatField.background.color = c;
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.jackaltext").Color(c));
                        break;
                    case 3:
                        c = Lover.Colors[0];
                        __instance.freeChatField.background.color = c;
                        __instance.quickChatField.background.color = c;
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.lovertext").Color(c));
                        break;
                    case 4:
                        c = Moriarty.MyRole.UnityColor;
                        __instance.freeChatField.background.color = c;
                        __instance.quickChatField.background.color = c;
                        chatTeamText!.text = Language.Translate("chat.current").Replace("%CHAT%", Language.Translate("chat.moriartytext").Color(c));
                        break;
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    public static bool SendChat(ChatController __instance)
    {
        string text = __instance.freeChatField.textArea.text;
        if (LobbyBehaviour.Instance||PlayerControl.LocalPlayer.Data.IsDead)
        {
            CurrentChatId = 0;
        }
        if (CurrentChatId!=0)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            __instance.freeChatField.Clear();
            switch (CurrentChatId)
            {
                case 1:
                    var crawler = NebulaGameManager.Instance?.AllPlayerInfo.FirstOrDefault(p => p.Role.Role is CrawlerEngineer);
                    if (crawler == null)
                    {
                        return false;
                    }
                    if (!GamePlayer.LocalPlayer!.IsImpostor)
                    {
                        return false;
                    }
                    if (GamePlayer.LocalPlayer.IsDead)
                    {
                        return false;
                    }
                    RpcSendTeamChat.Invoke((0, PlayerControl.LocalPlayer.PlayerId, text));
                    break;
                case 2:
                    if (!(GamePlayer.LocalPlayer!.Role is Jackal.Instance) && !(GamePlayer.LocalPlayer.Role is Sidekick.Instance) && !GamePlayer.LocalPlayer.Modifiers.Any(r => r is SidekickModifier.Instance))
                    {
                        return false;
                    }
                    if (GamePlayer.LocalPlayer.IsDead)
                    {
                        return false;
                    }
                    RpcSendTeamChat.Invoke((1, PlayerControl.LocalPlayer.PlayerId, text));
                    break;
                case 3:
                    if (!GamePlayer.LocalPlayer!.Modifiers.Any(r => r is Lover.Instance))
                    {
                        return false;
                    }
                    if (GamePlayer.LocalPlayer.IsDead)
                    {
                        return false;
                    }
                    RpcSendTeamChat.Invoke((2, PlayerControl.LocalPlayer.PlayerId, text));
                    break;
                case 4:
                    if (!(GamePlayer.LocalPlayer!.Role is Moriarty.Instance)&&!GamePlayer.LocalPlayer.TryGetAbility<Moran.Ability>(out var a)&&!GamePlayer.LocalPlayer.TryGetModifier<MoranModifier.Instance>(out var m))
                    {
                        return false;
                    }
                    if (GamePlayer.LocalPlayer.IsDead)
                    {
                        return false;
                    }
                    RpcSendTeamChat.Invoke((3, PlayerControl.LocalPlayer.PlayerId, text));
                    break;
            }
            return false;
        }
        string[] strs = text.Trim().Split(' ', StringSplitOptions.None);
        string command = strs[0];
        switch (command.ToLower())
        {
            case "/重载概率提升":
            case "/reloadup":
                if (!AmongUsClient.Instance.AmHost)
                {
                    break;
                }
                __instance.freeChatField.Clear();
                BetterStandardRoleAllocator.Reload();
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, Language.Translate("chatcommand.up.reload"));
                return false;
            case "/职业概率提升":
            case "/概率提升":
            case "/roleup":
            case "/up":
                if (!AmongUsClient.Instance.AmHost)
                {
                    break;
                }
                __instance.freeChatField.Clear();
                if (strs.Length<3)
                {
                    return false;
                }
                var targetplayer222 = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(p => p.Object.name.Contains(strs[1]));
                var role = Nebula.Roles.Roles.AllRoles.FirstOrDefault(r => r.DisplayName == strs[2]);
                var chance = 100;
                if (strs.Length == 4)
                {
                    chance = Convert.ToInt32(strs[3]);
                }
                BetterStandardRoleAllocator.AddRoleUp(targetplayer222!.PlayerId, role!, chance);
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, Language.Translate("chatcommand.up.add").Replace("%PLAYER%", targetplayer222.Object.name).Replace("%ROLE%", role!.DisplayName).Replace("%CHANCE%", chance.ToString()));
                return false;
            case "/categoryup":
            case "/阵营概率提升":
                if (!AmongUsClient.Instance.AmHost)
                {
                    break;
                }
                __instance.freeChatField.Clear();
                if (strs.Length < 3)
                {
                    return false;
                }
                var tplayer = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(p => p.Object.name.Contains(strs[1]));
                RoleCategory category = RoleCategory.CrewmateRole;
                switch(strs[2].ToLower())
                {
                    case "impostor":
                    case "imp":
                    case "伪装者":
                        category = RoleCategory.ImpostorRole;
                        break;
                    case "neutral":
                    case "neu":
                    case "第三方":
                        category = RoleCategory.NeutralRole;
                        break;
                    case "crewmate":
                    case "crew":
                    case "船员":
                        category = RoleCategory.CrewmateRole;
                        break;
                }
                var c = 100;
                if (strs.Length == 4)
                {
                    c= Convert.ToInt32(strs[3]);
                }
                BetterStandardRoleAllocator.AddCategoryUp(tplayer!.PlayerId, category, c);
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, Language.Translate("chatcommand.up.add").Replace("%PLAYER%", tplayer.Object.name).Replace("%ROLE%",category.ToString()).Replace("%CHANCE%", c.ToString()));
                return false;
            case "/moriartychat":
            case "/mc":
                __instance.freeChatField.Clear();
                if (strs.Length >= 2)
                {
                    if (!MeetingHud.Instance)
                    {
                        return false;
                    }
                    if (!MoriartyMeetingChat)
                    {
                        return false;
                    }
                    if (!(GamePlayer.LocalPlayer!.Role is Moriarty.Instance) && !GamePlayer.LocalPlayer.TryGetAbility<Moran.Ability>(out var a) && !GamePlayer.LocalPlayer.TryGetModifier<MoranModifier.Instance>(out var m))
                    {
                        return false;
                    }
                    if (GamePlayer.LocalPlayer.IsDead)
                    {
                        return false;
                    }
                    RpcSendTeamChat.Invoke((3, PlayerControl.LocalPlayer.PlayerId, string.Join(" ", strs.Skip(1))));
                }
                return false;
            case "/impchat":
            case "/ic":
                __instance.freeChatField.Clear();
                if (strs.Length >= 2)
                {
                    if (!MeetingHud.Instance)
                    {
                        return false;
                    }
                    if (!ImpostorMeetingChat)
                    {
                        return false;
                    }
                    var crawler = NebulaGameManager.Instance?.AllPlayerInfo.FirstOrDefault(p => p.Role.Role is CrawlerEngineer);
                    if (crawler == null)
                    {
                        return false;
                    }
                    if (!GamePlayer.LocalPlayer!.IsImpostor)
                    {
                        return false;
                    }
                    if (GamePlayer.LocalPlayer.IsDead)
                    {
                        return false;
                    }
                    RpcSendTeamChat.Invoke((0, PlayerControl.LocalPlayer.PlayerId, string.Join(" ", strs.Skip(1))));
                }
                return false;
            case "/jackalchat":
            case "/jc":
                __instance.freeChatField.Clear();
                if (strs.Length >= 2)
                {
                    if (!MeetingHud.Instance)
                    {
                        return false;
                    }
                    if (!JackalMeetingChat)
                    {
                        return false;
                    }
                    if (!(GamePlayer.LocalPlayer!.Role is Jackal.Instance)&&!(GamePlayer.LocalPlayer.Role is Sidekick.Instance)&&!GamePlayer.LocalPlayer.Modifiers.Any(r=>r is SidekickModifier.Instance))
                    {
                        return false;
                    }
                    if (GamePlayer.LocalPlayer.IsDead)
                    {
                        return false;
                    }
                    RpcSendTeamChat.Invoke((1, PlayerControl.LocalPlayer.PlayerId, string.Join(" ", strs.Skip(1))));
                }
                return false;
            case "/loverchat":
            case "/lc":
                __instance.freeChatField.Clear();
                if (strs.Length >= 2)
                {
                    if (!MeetingHud.Instance)
                    {
                        return false;
                    }
                    if (!LoverMeetingChat)
                    {
                        return false;
                    }
                    if (!GamePlayer.LocalPlayer!.Modifiers.Any(r => r is Lover.Instance))
                    {
                        return false;
                    }
                    if (GamePlayer.LocalPlayer.IsDead)
                    {
                        return false;
                    }
                    RpcSendTeamChat.Invoke((2, PlayerControl.LocalPlayer.PlayerId, string.Join(" ", strs.Skip(1))));
                }
                return false;
            case "/startmeeting":
            case "/发起会议":
                if (!AmongUsClient.Instance.AmHost && !IsAdmin(PlayerControl.LocalPlayer))
                {
                    break;
                }
                if (MeetingHud.Instance)
                {
                    break;
                }
                PlayerControl.LocalPlayer.ReportDeadBody(null);
                break;
            case "/kick":
                if (!AmongUsClient.Instance.AmHost && !IsAdmin(PlayerControl.LocalPlayer))
                {
                    break;
                }
                if (strs.Length < 2) break;
                var targetplayer = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(info => info.Object.name.Contains(strs[1]));
                if (targetplayer == null) break;
                SetSendKickPlayer.Invoke((PlayerControl.LocalPlayer.PlayerId));
                NebulaManager.Instance.StartDelayAction(1f, () =>
                {
                    KickPlayer.Invoke((targetplayer.ClientId, false));
                });
                break;
            case "/ban":
                if (!AmongUsClient.Instance.AmHost && !IsAdmin(PlayerControl.LocalPlayer))
                {
                    break;
                }
                if (strs.Length < 2) break;
                var target = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(info => info.Object.name.Contains(strs[1]));
                if (target == null) break;
                SetSendKickPlayer.Invoke((PlayerControl.LocalPlayer.PlayerId));
                NebulaManager.Instance.StartDelayAction(1f, () =>
                {
                    KickPlayer.Invoke((target.ClientId, true));
                });
                break;
            case "/setkillguardplayer":
            case "/设置首刀保护":
            case "/skgp":
                if (!AmongUsClient.Instance.AmHost)
                {
                    break;
                }
                foreach (var player in GameData.Instance.AllPlayers)
                {
                    if (player.name.Contains(strs[1]))
                    {
                        if (killGuardPlayers.ContainsKey(player.Object) && strs.Length >= 3 && strs[2] == "remove")
                        {
                            if (killGuardPlayers.ContainsKey(player.Object))
                            {
                                killGuardPlayers.Remove(player.Object);
                            }
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, Language.Translate("chatcommand.setkillguardplayer.remove").Replace("%PLAYER%",player.Object.name));
                            break;
                        }
                        else if (!killGuardPlayers.ContainsKey(player.Object))
                        {
                            killGuardPlayers.Add(player.Object, strs.Length >= 3 && strs[2] != null ? Convert.ToInt32(strs[2]) : 1);
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, Language.Translate("chatcommand.setkillguardplayer.add").Replace("%PLAYER%", player.Object.name));
                            break;
                        }
                    }
                }
                break;
            case "/skipmeeting":
            case "/跳过会议":
                if (!AmongUsClient.Instance.AmHost && !IsAdmin(PlayerControl.LocalPlayer))
                {
                    break;
                }
                if (!MeetingHud.Instance)
                {
                    break;
                }
                MeetingHud.Instance.ForceSkipAll();
                break;
            case "/win":
                if (strs.Length > 1)
                {
                    if (!AmongUsClient.Instance.AmHost && !IsAdmin(PlayerControl.LocalPlayer))
                    {
                        break;
                    }
                    CommandtriggerGameEnd.Invoke(strs[1]);
                }
                break;
            case "/s":
            case "/say":
                if (strs.Length > 1)
                {
                    if (!AmongUsClient.Instance.AmHost && !IsSponser(PlayerControl.LocalPlayer, true))
                    {
                        break;
                    }
                    if (!CanSendAdminMessage)
                    {
                        break;
                    }
                    RpcSendChat.Invoke(new ValueTuple<byte, string>(PlayerControl.LocalPlayer.PlayerId, string.Join(" ", strs.Skip(1))));
                }
                break;
            case "/warning":
                if (strs.Length > 2)
                {
                    if (!AmongUsClient.Instance.AmHost && !IsSponser(PlayerControl.LocalPlayer, true))
                    {
                        break;
                    }
                    var player = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(info => info.Object.name.Contains(strs[1]));
                    if (player == null) break;
                    RpcSendMessage.Invoke(new ValueTuple<byte, string,bool>(player.PlayerId, Language.Translate("chatcommand.warning").Replace("%PLAYER%",player.Object.name).Replace("%REASON%",string.Join(" ", strs.Skip(2))),true));
                }
                break;
            case "/sendmessage":
                __instance.freeChatField.Clear();
                if (strs.Length >= 2)
                {
                    if (!AmongUsClient.Instance.AmHost && !IsAdmin(PlayerControl.LocalPlayer))
                    {
                        return false;
                    }
                    var player = GameData.Instance.AllPlayers.GetFastEnumerator().FirstOrDefault(info => info.Object.name.Contains(strs[1]));
                    if (player == null) return false;
                    string t = string.Join(" ", strs.Skip(2));
                    RpcSendMessage.Invoke(new ValueTuple<byte, string,bool>(player.PlayerId, t,false));
                    return false;
                }
                return false;
            case "/selectcamouflagerplayercosmic":
            case "/scfpc":
            case "/设置小黑人形象":
                if (strs.Length > 1)
                {
                    if (!AmongUsClient.Instance.AmHost && !IsSponser(PlayerControl.LocalPlayer))
                    {
                        break;
                    }
                    camousCosmic = null!;
                    foreach (var player in GameData.Instance.AllPlayers)
                    {
                        if (player.name.Contains(strs[1]))
                        {
                            camousCosmic = player.Object;
                            RpcCamousSet.Invoke(new ValueTuple<byte>(player.Object.PlayerId));
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, Language.Translate("chatcommand.scfpc.set").Replace("%PLAYER%",player.Object.name));
                            break;
                        }
                    }
                }
                break;
        }
        return true;
    }
    static RemoteProcess<string> CommandtriggerGameEnd = new RemoteProcess<string>("CommandInvokeGameEnd", (s, _) =>
    {
        if (AmongUsClient.Instance.AmHost)
        {
            var game = NebulaAPI.CurrentGame;
            if (game == null)
            {
                return;
            }
            switch (s)
            {
                case "crewmate":
                    game.TriggerGameEnd(NebulaGameEnds.CrewmateGameEnd, GameEndReason.Situation);
                    break;
                case "task":
                    game.TriggerGameEnd(NebulaGameEnds.CrewmateGameEnd, GameEndReason.Task);
                    break;
                case "impostor":
                    game.TriggerGameEnd(NebulaGameEnds.ImpostorGameEnd, GameEndReason.Situation);
                    break;
                case "sabotage":
                    game.TriggerGameEnd(NebulaGameEnds.ImpostorGameEnd, GameEndReason.Sabotage);
                    break;
                case "jackal":
                    game.TriggerGameEnd(NebulaGameEnds.JackalGameEnd, GameEndReason.Situation);
                    break;
                case "jester":
                    game.TriggerGameEnd(NebulaGameEnds.JesterGameEnd, GameEndReason.Special);
                    break;
                case "lovers":
                    game.TriggerGameEnd(NebulaGameEnds.LoversGameEnd, GameEndReason.Situation);
                    break;
                case "avenger":
                    game.TriggerGameEnd(NebulaGameEnds.AvengerGameEnd, GameEndReason.Special);
                    break;
                case "arsonist":
                    game.TriggerGameEnd(NebulaGameEnds.ArsonistGameEnd, GameEndReason.Special);
                    break;
                case "paparazzo":
                    game.TriggerGameEnd(NebulaGameEnds.PaparazzoGameEnd, GameEndReason.Special);
                    break;
                case "vulture":
                    game.TriggerGameEnd(NebulaGameEnds.VultureGameEnd, GameEndReason.Special);
                    break;
                case "yandere":
                    game.TriggerGameEnd(Yandere.Instance.YandereTeamWin, GameEndReason.Situation);
                    break;
                case "doomsayer":
                    game.TriggerGameEnd(Doomsayer.Instance.DoomsayerTeamWin, GameEndReason.Special);
                    break;
                case "lawyer":
                    game.TriggerGameEnd(Lawyer.Instance.LawyerWin, GameEndReason.Special);
                    break;
                case "moriarty":
                    game.TriggerGameEnd(Moriarty.Instance.MoriartyTeamWin, GameEndReason.Situation);
                    break;
                case "nogame":
                    game.TriggerGameEnd(NebulaGameEnds.NoGameEnd, GameEndReason.Special);
                    break;
            }
        }
    });
    static float RefreshTime = 1f;
    public static RemoteProcess<ValueTuple<byte, bool>> CamouRPC = new RemoteProcess<ValueTuple<byte, bool>>("CamouflageEffect", delegate (ValueTuple<byte, bool> message, bool _)
    {
        string tag = "Camo" + message.Item1.ToString();
        if (camousCosmic == null)
        {
            return;
        }
        foreach (PlayerControl pl in PlayerControl.AllPlayerControls)
        {
            var p = pl.ToNebulaPlayer();
            if (message.Item2)
            {
                p.Unbox().AddOutfit(new OutfitCandidate(camousCosmic.ToNebulaPlayer().GetOutfit(50), tag, 100, true));
            }
            else
            {
                p.Unbox().RemoveOutfit(tag);
            }
        }
    }, true);
    public static void Update()
    {
        try
        {
            if (RefreshTime > 0)
            {
                RefreshTime -= Time.fixedDeltaTime;
                return;
            }
            if (NebulaAPI.CurrentGame != null)
            {
                RefreshTime = 1f;
                if (camousCosmic != null)
                {
                    Camouflager.RpcCamouflage = CamouRPC;
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }
    }
    public static void LoadCustomTeamTip()
    {
        RegisterWinCondTip(Yandere.Instance.YandereTeamWin, () => ((ISpawnable)Yandere.MyRole).IsSpawnable, "yandere", null!);
        RegisterWinCondTip(Doomsayer.Instance.DoomsayerTeamWin, () => ((ISpawnable)Doomsayer.MyRole).IsSpawnable, "doomsayer", null!);
        RegisterWinCondTip(Lawyer.Instance.LawyerWin, () => ((ISpawnable)Lawyer.MyRole).IsSpawnable, "lawyer1", null!);
        RegisterWinCondTip(Lawyer.Instance.LawyerWin, () => ((ISpawnable)Lawyer.MyRole).IsSpawnable, "lawyer2", null!);
        RegisterWinCondTip(Moriarty.Instance.MoriartyTeamWin, () => ((ISpawnable)Moriarty.MyRole).IsSpawnable, "moriarty1", str => str.Replace("%IMPOSTOR%", Language.Translate("document.tip.winCond.teams.impostor").Color(Impostor.MyTeam.Color.ToUnityColor()).Replace("%JACKAL%", Language.Translate("document.tip.winCond.teams.jackal").Color(Jackal.MyTeam.Color.ToUnityColor()))));
        RegisterWinCondTip(Moriarty.Instance.MoriartyTeamWin, () => ((ISpawnable)Moriarty.MyRole).IsSpawnable && Moriarty.CanWinByKillHolmes, "moriarty2", null!);
        RegisterWinCondTip(Skinner.Instance.SkinnerWin, () => ((ISpawnable)Skinner.MyRole).IsSpawnable, "skinner", null!);
    }
    private static void RegisterWinCondTip(GameEnd gameEnd, Func<bool> predicate, string name, Func<string, string> decorator = null!)
    {
        NebulaAPI.RegisterTip(new WinConditionTip(gameEnd, predicate, () => Language.Translate("document.tip.winCond." + name + ".title"), delegate
        {
            string text = Language.Translate("document.tip.winCond." + name);
            Func<string, string> decorator2 = decorator;
            return ((decorator2 != null) ? decorator2(text) : null) ?? text;
        }));
    }
    public static bool CrewmateCheck(GameUpdateEvent ev)
    {
        try
        {
            static bool isJackalTeam(GamePlayer p) => p.Role.Role.Team == Jackal.MyTeam || p.Modifiers.Any(m => m.Modifier == SidekickModifier.MyRole);
            static bool isMoran(GamePlayer p) => (p.Role.Role == Moran.MyRole) || (p.Modifiers.Any(m => m.Modifier == MoranModifier.MyRole));
            if (DiagnosisModePatchs.IsEnabled)
            {
                return false;
            }
            bool flag = PlayerControl.AllPlayerControls.ToArray().Any(delegate (PlayerControl pl)
            {
                var p = pl.ToNebulaPlayer();
                if (p.IsDead)
                {
                    return false;
                }
                if (p.TryGetModifier<HasLove.Instance>(out var h))
                {
                    return false;
                }
                if (p.Role.Role.Team == Impostor.MyTeam)
                {
                    return true;
                }
                if (p.Role is Yandere.Instance)
                {
                    return true;
                }
                if (isMoran(p))
                {
                    return true;
                }
                if (!isJackalTeam(p))
                {
                    return false;
                }
                return true;
            });

            if (flag)
            {
                return false;
            }
            Virial.Game.Game currentGame = NebulaAPI.CurrentGame!;
            if (currentGame == null)
            {
                return false;
            }
            currentGame.TriggerGameEnd(NebulaGameEnd.CrewmateWin, GameEndReason.Situation, null);
            return false;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
            return true;
        }
    }
    public static bool ImpostorCheck(GameUpdateEvent ev)
    {
        try
        {
            static bool isJackalTeam(GamePlayer p) => p.Role.Role.Team == Jackal.MyTeam || p.Modifiers.Any(m => m.Modifier == SidekickModifier.MyRole);
            static bool isMoran(GamePlayer p) => (p.Role.Role == Moran.MyRole) || (p.Modifiers.Any(m => m.Modifier == MoranModifier.MyRole));
            int impostors = 0;
            int totalAlive = 0;
            bool leftJackal = false, leftYandere = false, leftMoran = false;
            foreach (PlayerControl pl in PlayerControl.AllPlayerControls)
            {
                var p = pl.ToNebulaPlayer();
                if (!p.IsDead)
                {
                    totalAlive++;
                    Lover.Instance lover;
                    if (p.Role.Role.Team == Impostor.MyTeam && (!p.TryGetModifier<Lover.Instance>(out lover!) || lover.IsAloneLover) && !p.TryGetModifier<HasLove.Instance>(out var love))
                    {
                        impostors++;
                    }
                    if (isJackalTeam(p))
                    {
                        leftJackal = true;
                    }
                    if (p.Role is Yandere.Instance)
                    {
                        leftYandere = true;
                    }
                    if (isMoran(p))
                    {
                        leftMoran = true;
                    }
                }
            }
            if (ImpostorMustKillAllToWin)
            {
                if (impostors == totalAlive)
                {
                    Virial.Game.Game currentGame = NebulaAPI.CurrentGame!;
                    if (currentGame == null)
                    {
                        return false;
                    }
                    currentGame.TriggerGameEnd(NebulaGameEnd.ImpostorWin, GameEndReason.Situation, null);
                }
                return false;
            }
            if (leftJackal || leftYandere || leftMoran)
            {
                return false;
            }
            if (impostors * 2 >= totalAlive)
            {
                Virial.Game.Game currentGame = NebulaAPI.CurrentGame!;
                if (currentGame == null)
                {
                    return false;
                }
                currentGame.TriggerGameEnd(NebulaGameEnd.ImpostorWin, GameEndReason.Situation, null);
            }
            return false;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
            return true;
        }
    }
    public static bool JackalCheck(GameUpdateEvent ev)
    {
        try
        {
            List<Jackal.Instance> allAliveJackals = new List<Jackal.Instance>();
            int totalAlive = 0;
            bool leftImpostors = false, leftYandere = false, leftLoveJackal = false, leftMoran = false;
            static bool isJackalTeam(GamePlayer p) => (!p.Modifiers.Any(m => m.Modifier == HasLove.MyRole) && p.Role.Role.Team == Jackal.MyTeam) || ((!p.Modifiers.Any(m => m.Modifier == HasLove.MyRole) && p.Modifiers.Any(m => m.Modifier == SidekickModifier.MyRole)));
            static bool isMoran(GamePlayer p) => (p.Role.Role == Moran.MyRole) || (p.Modifiers.Any(m => m.Modifier == MoranModifier.MyRole));
            static bool IsHasLove(GamePlayer p) => p.Modifiers.Any(m => m.Modifier == HasLove.MyRole);
            allAliveJackals.Clear();
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                var p = player.ToNebulaPlayer();
                if (p.IsDead) continue;
                if (p.Role.Role.Team == Impostor.MyTeam) leftImpostors = true;
                if (p.Role is Yandere.Instance) leftYandere = true;
                if (isMoran(p)) leftMoran = true;
                if (p.Role is Jackal.Instance jRole)
                {
                    allAliveJackals.Add(jRole);
                    if (IsHasLove(p))
                    {
                        leftLoveJackal = true;
                    }
                }
                totalAlive++;
            }
            var jackalAliveEx = new List<Jackal.Instance>(allAliveJackals);
            foreach (var p in allAliveJackals)
            {
                if (IsHasLove(p.MyPlayer))
                {
                    jackalAliveEx.Remove(p);
                }
            }
            if (jackalAliveEx.Count <= 0)
            {
                return false;
            }
            allAliveJackals = jackalAliveEx;
            ulong jackalMask = 0;
            int teamCount = 0;
            int winningJackalTeams = 0;
            ulong completeWinningJackalMask = 0;

            //全ジャッカルに対して、各チームごとに勝敗を調べる
            foreach (var jackal in allAliveJackals)
            {
                //ジャッカル陣営の数をカウントする
                ulong myMask = 1ul << jackal!.JackalTeamId;
                if ((jackalMask & myMask) == 0) teamCount++;
                else continue; //既に考慮したチームはスキップしてよい
                jackalMask |= myMask;

                //死亡しておらず、同チーム、かつラバーズでないか相方死亡ラバー
                int aliveJackals = PlayerControl.AllPlayerControls.ToArray().Count(pl =>
                {
                    Lover.Instance lover;
                    var p = pl.ToNebulaPlayer();
                    return !p.IsDead && isJackalTeam(p) && jackal.IsSameTeam(p) && (!p.TryGetModifier<Lover.Instance>(out lover!) || lover.IsAloneLover) && !p.IsModMadmate();
                });
                //完全殲滅勝利
                if (aliveJackals == totalAlive) completeWinningJackalMask |= myMask;
                //キル勝利
                if (aliveJackals * 2 >= totalAlive && !leftImpostors && !leftYandere && !leftLoveJackal && !leftMoran) winningJackalTeams++;
            }

            //キル勝利のトリガー
            if (teamCount == 1 && winningJackalTeams > 0) NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.JackalWin, GameEndReason.Situation);
            //完全殲滅勝利のトリガー
            if (completeWinningJackalMask != 0)
            {
                allAliveJackals.Do(j => j.IsDefeatedJackal = (completeWinningJackalMask & (1ul << j.JackalTeamId)) == 0ul);
                NebulaAPI.CurrentGame?.TriggerGameEnd(NebulaGameEnd.JackalWin, GameEndReason.Situation);
            }
            return false;
        }
        catch (Exception e)
        {
            PDebug.Log(e);
            return true;
        }
    }
    static RemoteProcess<(int,byte,string)> RpcSendTeamChat = new("SendMeetingTeamChat",
((int,byte,string) message, bool _) =>
{
    var sourcePlayer = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(p => p.PlayerId == message.Item2);
    if (sourcePlayer==null)
    {
        return;
    }
    var localPlayer = GamePlayer.LocalPlayer;
    if (localPlayer.Role.GetAbility<Busker.Ability>()!=null&&localPlayer.PlayerState==PlayerState.Pseudocide)
    {
        return;
    }
    var nsp = sourcePlayer.ToNebulaPlayer();
    Jackal.Instance jackal = (nsp.Role as Jackal.Instance)!;
    Sidekick.Instance sidekick = (nsp.Role as Sidekick.Instance)!;
    SidekickModifier.Instance sidekickm = null!;
    if (localPlayer!.IsDead)
    {
        if (message.Item1==1)
        {
            var originname = sourcePlayer.name;
            sourcePlayer.SetName(originname + Language.Translate("chat.jackal").Color(Jackal.MyRole.UnityColor));
            HudManager.Instance.Chat.AddChat(sourcePlayer, message.Item3);
            sourcePlayer.SetName(originname);
        }
    }
    else if (jackal!=null||sidekick!=null||nsp.TryGetModifier<SidekickModifier.Instance>(out sidekickm!))
    {
        if (message.Item1 == 1)
        {
            if (jackal!=null)
            {
                if (Jackal.IsJackalTeam(localPlayer, jackal.JackalTeamId))
                {
                    var originname = sourcePlayer.name;
                    sourcePlayer.SetName(originname + Language.Translate("chat.jackal").Color(Jackal.MyRole.UnityColor));
                    HudManager.Instance.Chat.AddChat(sourcePlayer, message.Item3);
                    sourcePlayer.SetName(originname);
                }
            }
            else if (sidekick!= null)
            {
                if (Jackal.IsJackalTeam(localPlayer,sidekick.JackalTeamId))
                {
                    var originname = sourcePlayer.name;
                    sourcePlayer.SetName(originname + Language.Translate("chat.jackal").Color(Jackal.MyRole.UnityColor));
                    HudManager.Instance.Chat.AddChat(sourcePlayer, message.Item3);
                    sourcePlayer.SetName(originname);
                }
            }
            else if (sidekickm != null)
            {
                if (Jackal.IsJackalTeam(localPlayer, sidekickm.JackalTeamId))
                {
                    var originname = sourcePlayer.name;
                    sourcePlayer.SetName(originname + Language.Translate("chat.jackal").Color(Jackal.MyRole.UnityColor));
                    HudManager.Instance.Chat.AddChat(sourcePlayer, message.Item3);
                    sourcePlayer.SetName(originname);
                }
            }
        }
    }
    switch (message.Item1)
    {
        case 0:
            if (localPlayer.IsImpostor && nsp.IsImpostor || localPlayer.IsDead)
            {
                var originname = sourcePlayer.name;
                sourcePlayer.SetName(originname + Language.Translate("chat.impostor").Color(Palette.ImpostorRed));
                HudManager.Instance.Chat.AddChat(sourcePlayer, message.Item3);
                sourcePlayer.SetName(originname);
            }
            break;
        case 2:
            var p = localPlayer;
            if (p.TryGetModifier<Lover.Instance>(out var l) && (sourcePlayer.PlayerId == p.PlayerId ||
            (l.MyLover != null && l.MyLover.Get() != null && sourcePlayer.PlayerId == l.MyLover.Get().PlayerId)))
            {
                var originname = sourcePlayer.name;
                sourcePlayer.SetName(originname + Language.Translate("chat.lover").Color(Lover.Colors[0]));
                HudManager.Instance.Chat.AddChat(sourcePlayer, message.Item3);
                sourcePlayer.SetName(originname);
            }
            else if (GamePlayer.LocalPlayer.IsDead)
            {
                var originname = sourcePlayer.name;
                sourcePlayer.SetName(originname + Language.Translate("chat.lover").Color(Lover.Colors[0]));
                HudManager.Instance.Chat.AddChat(sourcePlayer, message.Item3);
                sourcePlayer.SetName(originname);
            }
            break;
        case 3:
            var local = localPlayer;
            var localIsMoriarty = local.Role is Moriarty.Instance || local.TryGetAbility<Moran.Ability>(out var ab) || local.Modifiers.Any(r => r is MoranModifier.Instance);
            var sourceIsMoriarty = nsp.Role is Moriarty.Instance || nsp.TryGetAbility<Moran.Ability>(out var a) || nsp.Modifiers.Any(r => r is MoranModifier.Instance);
            if ((localIsMoriarty&&sourceIsMoriarty)||local.IsDead)
            {
                var originname = sourcePlayer.name;
                sourcePlayer.SetName(originname + Language.Translate("chat.moriarty").Color(Moriarty.MyRole.UnityColor));
                HudManager.Instance.Chat.AddChat(sourcePlayer, message.Item3);
                sourcePlayer.SetName(originname);
            }
            break;
    }
});
    static RemoteProcess<ValueTuple<byte, string>> RpcSendChat = new("SendAdminChat",
(ValueTuple<byte, string> message, bool _) =>
{
    foreach (var p in GameData.Instance.AllPlayers)
    {
        if (p.PlayerId == message.Item1)
        {
            var messagename = ("Admin Message Sender:" + p.Object.name).Color(UnityEngine.Color.green);
            if (p.Object.AmHost())
            {
                messagename = "Host Message".Color(UnityEngine.Color.red);
            }
            else if (IsAdmin(p.Object, true))
            {
                messagename = ("Admin Message Sender:" + p.Object.name).Color(UnityEngine.Color.green);
            }
            else if (IsSponser(p.Object, true))
            {
                messagename = ("Sponser Message Sender:" + p.Object.name).Color(UnityEngine.Color.cyan);
            }
            if (p.IsDead)
            {
                var p2 = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault((PlayerControl x) => !x.Data.IsDead);
                if (p2 != null)
                {
                    var p2name = p2.name;
                    p2.SetName(messagename);
                    HudManager.Instance.Chat.AddChat(p2, message.Item2, false);
                    p2.SetName(p2name);
                }
                break;
            }
            var senderName = p.Object.name;
            p.Object.SetName(messagename);
            HudManager.Instance.Chat.AddChat(p.Object, message.Item2, false);
            p.Object.SetName(senderName);
            break;
        }
    }
});
    static RemoteProcess<ValueTuple<byte, string,bool>> RpcSendMessage = new("SendMessageChat",
(ValueTuple<byte, string,bool> message, bool _) =>
{
    foreach (var p in GameData.Instance.AllPlayers)
    {
        if (p.PlayerId == message.Item1)
        {
            if (message.Item3&&p.IsDead)
            {
                var replacePlayer = PlayerControl.AllPlayerControls.GetFastEnumerator().FirstOrDefault(p => !p.Data.IsDead);
                HudManager.Instance.Chat.AddChat(replacePlayer, message.Item2);
                return;
            }
            HudManager.Instance.Chat.AddChat(p.Object, message.Item2, false);
        }
    }
});
    static RemoteProcess<ValueTuple<byte>> RpcCamousSet = new("CamousSet",
(ValueTuple<byte> message, bool _) =>
{
    foreach (var p in GameData.Instance.AllPlayers)
    {
        if (p.PlayerId == message.Item1)
        {
            camousCosmic = p.Object;
            break;
        }
    }
});
    static RemoteProcess<ValueTuple<GamePlayer, GamePlayer>> setShieldPlayer = new RemoteProcess<(Player, Player)>("DocterSetShield", delegate (ValueTuple<GamePlayer, GamePlayer>
    message, bool _)
    {
        selectPlayer = message.Item1;
        DoctorPlayer = message.Item2;
    });
    internal static RemoteProcess<ValueTuple<GamePlayer, GamePlayer, bool>> TryKillShieldPlayer = new RemoteProcess<ValueTuple<GamePlayer, GamePlayer, bool>>("TryKillShieldPlayer", delegate (ValueTuple<GamePlayer, GamePlayer, bool>
    message, bool _)
    {
        if (message.Item1.AmOwner || message.Item2.AmOwner)
        {
            PlayerControl player = message.Item2.ToAUPlayer();
            if (player == null)
            {
                return;
            }
            player.ShowFailedMurder();
            if (message.Item3)
            {
                AmongUsUtil.PlayCustomFlash(UnityEngine.Color.red, 0.5f, 0.5f, 0.75f);
            }
        }
    });
    internal static RemoteProcess<GamePlayer> SetVisible = new RemoteProcess<Player>("SetPlayerVisible", (p, _) =>
    {
        if (p == null || p.ToAUPlayer() == null)
        {
            return;
        }
        p.ToAUPlayer().Visible = true;
    });
    public static void TaskPanelPosPatch(TaskPanelBehaviour __instance)
    {
        if (LogicDangerLevel != null&&IntroShowImpostor)
        {
            __instance.closedPosition = new Vector3(__instance.closedPosition.x, 1.6f, __instance.closedPosition.z);
            __instance.openPosition = new Vector3(__instance.openPosition.x, 1.6f, __instance.openPosition.z);
            Vector3 vector3 = new Vector3(Mathf.SmoothStep(__instance.closedPosition.x, __instance.openPosition.x, __instance.timer), Mathf.SmoothStep(__instance.closedPosition.y, __instance.openPosition.y, __instance.timer), __instance.openPosition.z);
            __instance.transform.localPosition = AspectPosition.ComputePosition(AspectPosition.EdgeAlignments.LeftTop, vector3);
        }
    }
    static NormalGameLogicHnSDangerLevel? LogicDangerLevel;
    static NormalGameLogicHnSMusic? LogicMusic;
    public class NormalGameLogicHnSDangerLevel : IGameOperator
    {
        GameManager Manager;
        public NormalGameLogicHnSDangerLevel(GameManager manager)
        {
            Manager = manager;
        }
        void OnRoleSet(PlayerRoleSetEvent ev)
        {
            if (ev.Role.Role.Category == RoleCategory.ImpostorRole)
            {
                impostors?.Add(ev.Player.ToAUPlayer());
            }
        }
        void OnGameStart(GameStartEvent ev)
        {
            firstMusicActivation = true;
            if (!GamePlayer.LocalPlayer!.IsImpostor)
            {
                dangerMeter = HudManager.Instance.DangerMeter;
                dangerMeter.gameObject.SetActive(true);
            }
            impostors = new List<PlayerControl>();
            foreach (var player in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
            {
                if (player.IsImpostor)
                {
                    impostors.Add(player.ToAUPlayer());
                }
            }
            scaryMusicDistance = 55f * GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.PlayerSpeedMod);
            veryScaryMusicDistance = 15f * GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.PlayerSpeedMod);
            if (scaryMusicDistance < veryScaryMusicDistance)
            {
                float num = veryScaryMusicDistance;
                float num2 = scaryMusicDistance;
                scaryMusicDistance = num;
                veryScaryMusicDistance = num2;
            }
        }

        void OnPlayerDisconnect(PlayerDisconnectEvent ev)
        {
            if (ev.Player.IsImpostor)
            {
                List<PlayerControl> list = impostors!;
                if (list == null)
                {
                    return;
                }
                list.Remove(ev.Player.ToAUPlayer());
            }
        }

        void OnGameEnd(GameEndEvent ev)
        {
            impostors = null;
        }

        void Update(GameUpdateEvent ev)
        {
            PlayerControl localPlayer = PlayerControl.LocalPlayer;
            if (impostors == null || localPlayer == null)
            {
                return;
            }
            if (impostors.Count <= 0)
            {
                return;
            }
            float num = float.MaxValue;
            foreach (PlayerControl playerControl in impostors)
            {
                if (!(playerControl == null))
                {
                    float sqrMagnitude = (playerControl.transform.position - localPlayer.transform.position).sqrMagnitude;
                    if (sqrMagnitude < scaryMusicDistance && num > sqrMagnitude)
                    {
                        num = sqrMagnitude;
                    }
                }
            }
            if (nomusictime < ImpostorCannotMoveTime)
            {
                nomusictime += Time.deltaTime;
                dangerLevel1 = 0f;
                dangerLevel2 = 0f;
            }
            else
            {
                if (firstMusicActivation)
                {
                    firstMusicActivation = false;
                    firstCrossfadeCountdown = 3f;
                    LogicMusic!.SetMusicCrossfadeSpeed(0.6f);
                }
                if (firstCrossfadeCountdown > 0f)
                {
                    firstCrossfadeCountdown -= Time.deltaTime;
                    if (firstCrossfadeCountdown <= 0f)
                    {
                        LogicMusic!.SetMusicCrossfadeSpeed(5f);
                    }
                }
                dangerLevel1 = Mathf.Clamp01((scaryMusicDistance - num) / (scaryMusicDistance - veryScaryMusicDistance));
                dangerLevel2 = Mathf.Clamp01((veryScaryMusicDistance - num) / veryScaryMusicDistance);
            }
            UpdateDangerMeter();
            UpdateDangerMusic();
        }

        private void UpdateDangerMusic()
        {
            var localPlayer = GamePlayer.LocalPlayer;
            if (localPlayer != null && localPlayer.IsDead)
            {
                LogicMusic!.ResetMusic();
                return;
            }
            LogicMusic!.SetMusicValues(dangerLevel1, dangerLevel2);
        }

        private void UpdateDangerMeter()
        {
            if (dangerMeter == null)
            {
                return;
            }
            dangerMeter.SetDangerValue(dangerLevel1, dangerLevel2);
        }
        private DangerMeter? dangerMeter;
        private List<PlayerControl>? impostors;
        private float scaryMusicDistance;
        private float veryScaryMusicDistance;
        private float dangerLevel1;
        private float dangerLevel2;
        private bool firstMusicActivation;
        private float firstCrossfadeCountdown;
    }

    public class NormalGameLogicHnSMusic : IGameOperator
    {
        GameManager Manager;
        public NormalGameLogicHnSMusic(GameManager manager)
        {
            Manager = manager;
        }

        void OnGameStart(GameStartEvent ev)
        {
            musicCollection = GameManagerCreator.Instance.HideAndSeekManagerPrefab.MusicCollection;
            InitMusic();
            ResetMusic();
        }

        void OnGameEnd(GameEndEvent ev)
        {
            ResetMusic();
        }
        private void InitMusic()
        {
            if (!HasHNSMusic)
            {
                return;
            }
            if (normalSource == null)
            {
                normalSource = SoundManager.Instance.GetNamedSfxSource(musicNames[LogicHnSMusic.HideAndSeekMusicTrack.Normal]);
            }
            normalSource.outputAudioMixerGroup = SoundManager.Instance.MusicChannel;
            normalSource.clip = musicCollection!.NormalMusic;
            normalSource.loop = true;
            if (taskSource == null)
            {
                taskSource = SoundManager.Instance.GetNamedSfxSource(musicNames[LogicHnSMusic.HideAndSeekMusicTrack.Task]);
            }
            taskSource.outputAudioMixerGroup = SoundManager.Instance.MusicChannel;
            taskSource.volume = 0f;
            taskSource.clip = musicCollection.TaskMusic;
            taskSource.loop = true;
            if (dangerLevel1Source == null)
            {
                dangerLevel1Source = SoundManager.Instance.GetNamedSfxSource(musicNames[LogicHnSMusic.HideAndSeekMusicTrack.DangerLevel1]);
            }
            dangerLevel1Source.outputAudioMixerGroup = SoundManager.Instance.MusicChannel;
            dangerLevel1Source.volume = 0f;
            dangerLevel1Source.clip = musicCollection.DangerLevel1Music;
            dangerLevel1Source.loop = true;
            if (dangerLevel2Source == null)
            {
                dangerLevel2Source = SoundManager.Instance.GetNamedSfxSource(musicNames[LogicHnSMusic.HideAndSeekMusicTrack.DangerLevel2]);
            }
            dangerLevel2Source.outputAudioMixerGroup = SoundManager.Instance.MusicChannel;
            dangerLevel2Source.volume = 0f;
            dangerLevel2Source.clip = musicCollection.DangerLevel2Music;
            dangerLevel2Source.loop = true;
            normalSource.Play();
            taskSource.Play();
            dangerLevel1Source.Play();
            dangerLevel2Source.Play();
            SyncMusic();
        }

        public void StartMusicWithIntro()
        {
            if (!HasHNSMusic)
            {
                return;
            }
            AudioClip audioClip = musicCollection!.ImpostorShortMusic;
            try
            {
                var totalTask = 0;
                foreach (var p in NebulaGameManager.Instance?.AllPlayerInfo ?? [])
                {
                    if (!p.IsDisconnected && p.Tasks.IsCrewmateTask)
                    {
                        totalTask += p.Tasks.Quota;
                    }
                }
                audioClip = totalTask < 45 ? musicCollection.ImpostorShortMusic : musicCollection.ImpostorLongMusic;
                if (AprilFoolsMode.ShouldHorseAround())
                {
                    audioClip = musicCollection.ImpostorRanchMusic;
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
            ImpostorMusic = SoundManager.Instance.PlaySound(audioClip, true, 1f, SoundManager.Instance.MusicChannel);
        }

        public void SetTaskState(bool isDoingTask)
        {
            this.isDoingTask = isDoingTask;
        }
        AudioSource? ImpostorMusic;
        void Update(GameUpdateEvent ev)
        {
            if (ImpostorMusic==null&&GamePlayer.LocalPlayer!.IsImpostor)
            {
                ImpostorMusic = SoundManager.Instance.PlaySound(musicCollection!.ImpostorShortMusic, true, 1f, SoundManager.Instance.MusicChannel);
            }
            if (normalSource == null || taskSource == null || dangerLevel1Source == null || dangerLevel2Source == null)
            {
                return;
            }
            if (Time.unscaledTime > lastMusicSyncTime + 1f)
            {
                SyncMusic();
            }
            normalSource.volume = Mathf.Lerp(normalSource.volume, normalVolume, Time.fixedDeltaTime * musicLerpSpeed);
            taskSource.volume = Mathf.Lerp(taskSource.volume, taskVolume, Time.fixedDeltaTime * musicLerpSpeed);
            dangerLevel1Source.volume = Mathf.Lerp(dangerLevel1Source.volume, dangerLevel1Volume, Time.fixedDeltaTime * musicLerpSpeed);
            dangerLevel2Source.volume = Mathf.Lerp(dangerLevel2Source.volume, dangerLevel2Volume, Time.fixedDeltaTime * musicLerpSpeed);
        }

        private void SyncMusic()
        {
            taskSource!.timeSamples = normalSource!.timeSamples;
            dangerLevel1Source!.timeSamples = normalSource!.timeSamples;
            dangerLevel2Source!.timeSamples = normalSource!.timeSamples;
            lastMusicSyncTime = Time.unscaledTime;
        }

        public void ResetMusic()
        {
            SetMusicValues(0f, 0f);
        }

        public void SetMusicCrossfadeSpeed(float lerpSpeed)
        {
            musicLerpSpeed = lerpSpeed;
        }

        public void SetMusicValues(float dangerLevel1, float dangerLevel2)
        {
            if (GamePlayer.LocalPlayer != null && GamePlayer.LocalPlayer.IsImpostor)
            {
                return;
            }
            if (normalSource == null || taskSource == null || dangerLevel1Source == null || dangerLevel2Source == null)
            {
                return;
            }
            normalVolume = (isDoingTask ? 0f : 1f);
            taskVolume = (isDoingTask ? 1f : 0f);
            dangerLevel1Volume = 0f;
            dangerLevel2Volume = 0f;
            if (dangerLevel1 > 0f)
            {
                dangerLevel1Volume = dangerLevel1;
                if (isDoingTask)
                {
                    taskVolume = 1f - dangerLevel1;
                }
                else
                {
                    normalVolume = 1f - dangerLevel1;
                }
            }
            if (dangerLevel2 > 0f)
            {
                dangerLevel2Volume = dangerLevel2;
                dangerLevel1Volume = 1f - dangerLevel2;
            }
        }

        private HideAndSeekMusicCollection? musicCollection;

        private float lastMusicSyncTime;

        private bool isDoingTask;

        private float normalVolume;

        private float taskVolume;

        private float dangerLevel1Volume;

        private float dangerLevel2Volume;

        private AudioSource? normalSource;

        private AudioSource? taskSource;

        private AudioSource? dangerLevel1Source;

        private AudioSource? dangerLevel2Source;

        private float musicLerpSpeed = 5f;

        private readonly Dictionary<LogicHnSMusic.HideAndSeekMusicTrack, string> musicNames = new Dictionary<LogicHnSMusic.HideAndSeekMusicTrack, string>
    {
        {
            LogicHnSMusic.HideAndSeekMusicTrack.Normal,
            "HnS_Music_Normal"
        },
        {
            LogicHnSMusic.HideAndSeekMusicTrack.Task,
            "HnS_Music_Task"
        },
        {
            LogicHnSMusic.HideAndSeekMusicTrack.DangerLevel1,
            "HnS_Music_DangerLevel1"
        },
        {
            LogicHnSMusic.HideAndSeekMusicTrack.DangerLevel2,
            "HnS_Music_DangerLevel2"
        }
    };
    }
    public class CrewmateStateTrackerTrigger : IGameOperator
    {
        void OnDieOrDC(PlayerDieOrDisconnectEvent ev)
        {
            if (ev is PlayerDisconnectEvent&&!ev.Player.IsImpostor)
            {
                HudManager.Instance.CrewmatesKilled.OnCrewmateDisconnect();
                return;
            }
            if (ev.Player.AmOwner&&LogicMusic!=null)
            {
                LogicMusic.ResetMusic();
            }
            HudManager.Instance.NotifyOfDeath();
        }
        void OnUpdate(GameHudUpdateEvent ev)
        {
            if (!HudManager.Instance.CrewmatesKilled.gameObject.activeSelf)
            {
                HudManager.Instance.CrewmatesKilled.gameObject.SetActive(true);
            }
        }
    }
}