using AsmResolver.PE.DotNet.Cil;
using Nebula.Patches;
using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Impostor;
using Plana.Roles.Neutral;

namespace Plana.Roles.Crewmate;

public class Violator : DefinedRoleTemplate, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder
{

    private Violator() : base("violator", PatchManager.ViolatorTeam.Color, RoleCategory.NeutralRole,PatchManager.ViolatorTeam,null,true,true,()=>false) { }

    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable)
    {
        return base.CanLoadDefaultTemplate(assignable) && !(assignable is Lover);
    }
    bool DefinedAssignable.ShowOnHelpScreen => false;
    bool DefinedAssignable.ShowOnFreeplayScreen => false;
    bool IGuessed.CanBeGuess => false;

    static public Violator MyRole = new Violator();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        DefinedRole RuntimeRole.ExternalRecognitionRole
        {
            get
            {
                return Nebula.Roles.Roles.AllRoles.Where(r => r.Category == RoleCategory.NeutralRole && r.IsSpawnable && !(r is GameMaster) && !(r is Violator) && !(r is Spectre) && !(r is SpectreFollower) && !(r is SpectreImmoralist)).ToList().Random(); ;
            }
        }
        bool RuntimeRole.CanInvokeSabotage => true;
        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.CanMoveInVent => true;
        bool RuntimeRole.HasImpostorVision => true;
        bool RuntimeRole.IgnoreBlackout => true;
        bool RuntimeRole.CanSeeOthersFakeSabotage => true;
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => MyExRole != null ? [MyRole, MyExRole] : [MyRole];
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => MyExAbility != null ? [MyExAbility, .. MyExAbility.SubAbilities] : [];
        public Instance(GamePlayer player) : base(player)
        {
        }
        void OnCheckGameEnd(EndCriteriaMetEvent ev)
        {
            if (ev.EndReason==GameEndReason.Sabotage)
            {
                ev.TryOverwriteEnd(PatchManager.NobodyAliveEnd, GameEndReason.Sabotage, 1 << MyPlayer.PlayerId);
            }
        }
        IPlayerAbility MyExAbility;
        DefinedRole MyExRole;
        MetaScreen OpenRoleSelectWindow(List<DefinedRole> rolelist, Func<DefinedRole, bool> predicate, string underText, Action<DefinedRole> onSelected)
        {
            var window = MetaScreen.GenerateWindow(new UnityEngine.Vector2(7.6f, 4.2f), HudManager.Instance.transform, new UnityEngine.Vector3(0, 0, -50f), true, false);
            var t = window.transform.parent.Find("CloseButton");
            if (t != null)
            {
                UnityEngine.Object.Destroy(t.gameObject);
            }
            MetaWidgetOld widget = new();
            MetaWidgetOld inner = new();
            var roles = rolelist.Where(predicate).ToList();
            var list = new List<DefinedRole>();
            while (list.Count < 3)
            {
                var role = roles.Random();
                if (list.Contains(role))
                {
                    roles.Remove(role);
                    continue;
                }
                list.Add(role);
                roles.Remove(role);
            }
            inner.Append(list, r => new MetaWidgetOld.Button(() => onSelected.Invoke(r), ButtonAttribute) { RawText = r.DisplayColoredName, PostBuilder = (_, renderer, _) => renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask }, 4, -1, 0, 0.59f);
            MetaWidgetOld.ScrollView scroller = new(new(6.9f, 3.8f), inner, true) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
            widget.Append(scroller);
            widget.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttr) { MyText = new RawTextComponent(underText), Alignment = IMetaWidgetOld.AlignmentOption.Center });
            window.SetWidget(widget);
            System.Collections.IEnumerator CoCloseOnResult()
            {
                while (MeetingHud.Instance.state == MeetingHud.VoteStates.Discussion) yield return null;
                if (SelectRoleScreen != null)
                {
                    MyPlayer.SetRole(roles[UnityEngine.Random.Range(0, list.Count)]);
                    MyPlayer.AddModifier(DreamweaverModifier.MyRole);
                    if (MyPlayer.Role.Role.LocalizedName.Contains("speechEater") && MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var guesser))
                    {
                        MyPlayer.RemoveModifier(GuesserModifier.MyRole);
                    }
                    window.CloseScreen();
                }
            }
            window.StartCoroutine(CoCloseOnResult().WrapToIl2Cpp());
            return window;
        }
        MetaScreen SelectRoleScreen;
        FlexibleLifespan lifespan;
        [Local]
        void OnGameStart(GameStartEvent ev)
        {
            if (MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var m))
            {
                MyPlayer.RemoveModifier(GuesserModifier.MyRole);
            }
            SelectRoleScreen =OpenRoleSelectWindow(new List<DefinedRole>(Nebula.Roles.Roles.AllRoles),(p => p.AssignmentStatus.HasFlag(AbilityAssignmentStatus.Killers)), Language.Translate("role.infected.selectImpRoleText"), (r) =>
            {
                MyExRole = r;
                var jackalability = r.GetAbilityOnRole(MyPlayer,AbilityAssignmentStatus.Killers,Array.Empty<int>());
                if (jackalability == null)
                {
                    PDebug.Log("Ability is null");
                }
                if (lifespan != null)
                {
                    lifespan.Release();
                    GameOperatorManager.Instance?.WrapUpDeadLifespans();
                    lifespan = null;
                }
                lifespan = new FlexibleLifespan();
                MyExAbility = jackalability?.Register(lifespan);
                SelectRoleScreen.CloseScreen();
            });
            var window = MetaScreen.GenerateWindow(new UnityEngine.Vector2(7.6f, 4.2f), HudManager.Instance.transform, new UnityEngine.Vector3(0, 0, -50f), true, false,false,BackgroundSetting.Modern);
            List<GUIWidget> widgets = new List<GUIWidget>();
            widgets.Add(new NoSGUIText(GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.OverlayTitle), new RawTextComponent((this as RuntimeAssignable).DisplayColoredName)));
            widgets.Add(new NoSGUIText(GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.OverlayContent), new RawTextComponent(Language.Translate("options.role.violator.Realdetail"))));
            VerticalWidgetsHolder verticalWidgetsHolder = new VerticalWidgetsHolder(GUIAlignment.Left, widgets);
            //MetaWidgetOld.ScrollView scroller = new(new(6.9f, 3.8f), w, true) { Alignment = IMetaWidgetOld.AlignmentOption.Left };
            window.SetWidget(new MetaWidgetOld.WrappedWidget(NebulaGUIWidgetEngine.API.ScrollView(GUIAlignment.Center, new(6.9f, 3.8f), "", verticalWidgetsHolder, out var _)));
        }
        [Local]
        void OnTaskStart(TaskPhaseRestartEvent ev)
        {
            if (MyPlayer.IsDead)
            {
                return;
            }
            SelectRoleScreen = OpenRoleSelectWindow(new List<DefinedRole>(Nebula.Roles.Roles.AllRoles), (p => p.AssignmentStatus.HasFlag(AbilityAssignmentStatus.Killers)), Language.Translate("role.infected.selectImpRoleText"), (r) =>
            {
                MyExRole = r;
                if (MyExAbility != null)
                {
                    MyExAbility.OnReleased();
                }
                var jackalability = r.GetAbilityOnRole(MyPlayer,AbilityAssignmentStatus.Killers, Array.Empty<int>());
                if (jackalability == null)
                {
                    PDebug.Log("Ability is null");
                }
                if (lifespan!=null)
                {
                    lifespan.Release();
                    GameOperatorManager.Instance?.WrapUpDeadLifespans();
                    lifespan = null;
                }
                lifespan = new FlexibleLifespan();
                MyExAbility = jackalability?.Register(lifespan);
                SelectRoleScreen?.CloseScreen();
            });
        }
        bool isselected;
        [Local]
        void OnUpdate(GameHudUpdateEvent ev)
        { 
            if (!MeetingHud.Instance)
            {
                return;
            }
            if ((MeetingHudExtension.VotingTimer<=15f||MeetingHud.Instance.GetVotesRemaining()<=3)&&!isselected)
            {
                isselected = true;
                SelectLeftImpWindow=OpenSelectWindow(Language.Translate("roles.violator.selectleftimpnum"), result =>
                {
                    SetLeftImpNumRpc.Invoke(result);
                    SelectLeftImpWindow?.CloseScreen();
                });
            }
        }
        MetaScreen SelectLeftImpWindow;
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            isselected = false;
            GuesserSystem.DoomsayerOnMeetingStart(() => { },true,false);
            //MyPlayer.SetRole(Violator.MyRole);
        }
        static TextAttributeOld ButtonAttribute = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new(1.3f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(2f, 1f, 2f);
        MetaScreen OpenSelectWindow(string underText, Action<int> onSelected)
        {
            var window = MetaScreen.GenerateWindow(new UnityEngine.Vector2(7.6f, 4.2f), HudManager.Instance.transform, new UnityEngine.Vector3(0, 0, -50f), true, false);
            var t = window.transform.parent.Find("CloseButton");
            if (t != null)
            {
                UnityEngine.Object.Destroy(t.gameObject);
            }
            MetaWidgetOld widget = new();
            MetaWidgetOld inner = new();
            List<int> list = new List<int>();
            for (int i = 0; i <= GameOptionsManager.Instance.currentGameOptions.GetAdjustedNumImpostorsModded(PlayerControl.AllPlayerControls.Count)+1; i++)
            {
                list.Add(i);
            }
            inner.Append(list, r => new MetaWidgetOld.Button(() => onSelected.Invoke(r), ButtonAttribute) { RawText = r.ToString().Color(Palette.ImpostorRed), PostBuilder = (_, renderer, _) => renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask }, 4, -1, 0, 0.59f);
            MetaWidgetOld.ScrollView scroller = new(new(6.9f, 3.8f), inner, true) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
            widget.Append(scroller);
            widget.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttr) { MyText = new RawTextComponent(underText), Alignment = IMetaWidgetOld.AlignmentOption.Center });
            window.SetWidget(widget);
            System.Collections.IEnumerator CoCloseOnResult()
            {
                while (MeetingHud.Instance.state != MeetingHud.VoteStates.Results) yield return null;
                if (window != null)
                {
                    int alives = NebulaGameManager.Instance.AllPlayerInfo.Count(p => !p.IsDead);
                    int deads = NebulaGameManager.Instance.AllPlayerInfo.Count(p => p.IsDead);
                    int maximp = GameOptionsManager.Instance.currentGameOptions.GetAdjustedNumImpostorsModded(PlayerControl.AllPlayerControls.Count);
                    int leftimp = maximp;
                    if (alives>=deads*2/3)
                    {
                        leftimp = maximp;
                    }
                    else if (alives>=deads/3)
                    {
                        leftimp = maximp - 1;
                    }
                    else
                    {
                        leftimp = 0;
                    }
                    SetLeftImpNumRpc.Invoke(leftimp);
                }
            }
            window.StartCoroutine(CoCloseOnResult().WrapToIl2Cpp());
            return window;
        }
        public static int LeftImpostorNum;
        static RemoteProcess<int> SetLeftImpNumRpc = new("SetLINRpc", (message, _) =>
        {
            LeftImpostorNum = message;
        });
        ModAbilityButton killButton;
        [OnlyHost]
        void WinCheck(GameUpdateEvent ev)
        {
            try
            {
                int totalAlive = 0;
                foreach (var player in GameData.Instance.AllPlayers)
                {
                    if (player == null || player.Object == null)
                    {
                        continue;
                    }
                    var p = player.Object.ToNebulaPlayer();
                    if (p == null || p.IsDead)
                    {
                        continue;
                    }
                    if (!p.IsDead)
                    {
                        totalAlive++;
                    }
                }
                if (!MyPlayer.IsDead && totalAlive <=1 )
                {
                    Virial.Game.Game currentGame = NebulaAPI.CurrentGame;
                    if (currentGame != null)
                    {
                        var mask = BitMasks.AsPlayer();
                        mask.Add(MyPlayer);
                        currentGame.TriggerGameEnd(PatchManager.NobodyAliveEnd, GameEndReason.Situation, mask);
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                var myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.KillablePredicate(MyPlayer), Palette.ImpostorRed, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption, false);
                killButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, true, false, VirtualKeyInput.Kill, null, AmongUsUtil.VanillaKillCoolDown, "kill", null, (ModAbilityButton _) => myTracker.CurrentTarget != null, (ModAbilityButton _) => base.MyPlayer.AllowToShowKillButtonByAbilities, false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                killButton.OnClick = (button) =>
                {
                    var player = myTracker.CurrentTarget;
                    MyPlayer.MurderPlayer(player!, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }
    }
}
