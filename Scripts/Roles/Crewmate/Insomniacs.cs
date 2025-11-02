using Plana.Core;
using Plana.Roles.Impostor;
using Plana.Roles.Neutral;

namespace Plana.Roles.Crewmate;

public class Insomniacs : DefinedRoleTemplate, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder
{
    private Insomniacs() : base("insomniacs", new(216,164,246), RoleCategory.CrewmateRole, PatchManager.DreamweaverAndInsomniacsTeam, [KillCoolDownOption,KillCatGetModifierOption,DeadBodyArrowOption,DreamweaverDeadToCrewmateOption,NoSleepNoWinOption]
    ,false,true,()=> ((ISpawnable)Dreamweaver.MyRole).IsSpawnable) 
    {
        IConfigurationHolder configurationHolder = base.ConfigurationHolder;
        if (configurationHolder == null)
        {
            return;
        }
        configurationHolder.ScheduleAddRelated(() => new IConfigurationHolder[] { Dreamweaver.MyRole.ConfigurationHolder });
    }
    bool ISpawnable.IsSpawnable
    {
        get
        {
            return ((ISpawnable)Dreamweaver.MyRole).IsSpawnable;
        }
    }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    internal static FloatConfiguration KillCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.insomniacs.killCoolDown", (2.5f,60f,2.5f),30f,FloatConfigurationDecorator.Second);
    static BoolConfiguration DeadBodyArrowOption = NebulaAPI.Configurations.Configuration("options.role.insomniacs.ShowDeadBodyArrow", false);
    internal static BoolConfiguration DreamweaverDeadToCrewmateOption = NebulaAPI.Configurations.Configuration("options.role.insomniacs.OnDreamweaverDeadToCrewmate", true);
    static BoolConfiguration NoSleepNoWinOption = NebulaAPI.Configurations.Configuration("options.role.insomniacs.IfNoSleepNoWin", true);
    public static BoolConfiguration KillCatGetModifierOption = NebulaAPI.Configurations.Configuration("options.role.insomniacs.killcatoption", true);
    static public Insomniacs MyRole = new Insomniacs();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(GamePlayer player) : base(player)
        {
        }
        ModAbilityButton skillButton;
        Virial.Media.Image skillImage = NebulaAPI.AddonAsset.GetResource("trytosleep.png")!.AsImage(115f)!;
        bool sleep;
        [OnlyMyPlayer]
        private void BlockWins(PlayerBlockWinEvent ev)
        {
            if (NoSleepNoWinOption)
            {
                ev.IsBlocked |=!sleep;
            }
        }
        [Local,OnlyMyPlayer]
        void OnMurdered(PlayerMurderedEvent ev)
        {
            if (ev.Murderer.IsImpostor)
            {
                new StaticAchievementToken("insomniacs.another5");
                if (ev.Murderer.Role is Dreamweaver.Instance)
                {
                    new StaticAchievementToken("insomniacs.another4");
                }
            }
        }
        RoleTaskType RuntimeRole.TaskType => RoleTaskType.NoTask;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                sleep = false;
                ObjectTracker<Virial.Game.DeadBody> deadTracker = ObjectTrackers.ForDeadBody(this, null, MyPlayer, p => !sleep);
                if (DeadBodyArrowOption)
                {
                    DeadbodyArrowAbility ability = new DeadbodyArrowAbility().Register(this);
                    GameOperatorManager instance = GameOperatorManager.Instance;
                    if (instance != null)
                    {
                        instance.Subscribe<GameUpdateEvent>(delegate (GameUpdateEvent ev)
                        {
                            ability.ShowArrow = !this.MyPlayer.IsDead&&!sleep;
                        }, this, 100);
                    }
                }
                skillButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability);
                skillButton.Availability = (button) => deadTracker.CurrentTarget!=null&&MyPlayer.CanMove;
                skillButton.Visibility = (button) => !MyPlayer.IsDead&&!sleep;
                skillButton.SetLabel("trytosleep");
                skillButton.SetImage(skillImage);
                skillButton.OnClick = (button) =>
                {
                    sleep = true;
                    NebulaGameManager instance = NebulaGameManager.Instance;
                    if (instance != null)
                    {
                        instance.RpcDoGameAction(this.MyPlayer, this.MyPlayer.Position, GameActionTypes.EatCorpseAction);
                    }
                    AmongUsUtil.RpcCleanDeadBody(deadTracker.CurrentTarget, this.MyPlayer.PlayerId, EventDetail.Eat);
                    var role = deadTracker.CurrentTarget.Player.Role;
                    if (role != null)
                    {
                        switch (role.Role.Category)
                        {
                            case RoleCategory.CrewmateRole:
                                new StaticAchievementToken("insomniacs.another3");
                                break;
                            case RoleCategory.ImpostorRole:
                                new StaticAchievementToken("insomniacs.another2");
                                break;
                            case RoleCategory.NeutralRole:
                                new StaticAchievementToken("insomniacs.another1");
                                break;
                        }
                    }
                    var nextArgs = role.RoleArguments;
                    if (role.Role is Knight || role.Role is Sheriff)
                    {
                        nextArgs = Array.Empty<int>();
                    }
                    var nextRole = role.Role;
                    if (nextRole is Jester)
                    {
                        nextRole = Nebula.Roles.Crewmate.Crewmate.MyRole;
                    }
                    MyPlayer.SetRole(nextRole, nextArgs);
                    if (instance != null)
                    {
                        List<GamePlayer> list = instance.AllPlayerInfo.Where((GamePlayer p) => !p.IsDead && (p.Role is Dreamweaver.Instance || p.Modifiers.Any(m => m is DreamweaverModifier.Instance))).ToList();
                        if (list.Count == 0)
                        {
                            return;
                        }
                    }
                    MyPlayer.AddModifier(InsomniacsModifier.MyRole);
                    TextMeshPro text = PlayerControl.LocalPlayer.cosmetics.nameText;
                    text.StartCoroutine(AnimationEffects.CoPlayRoleNameEffect(text.transform.parent, new UnityEngine.Vector3(0f, 0.185f, -0.1f), MyRole.RoleColor.ToUnityColor(), text.gameObject.layer, 1.4285715f).WrapToIl2Cpp());
                };
                skillButton.StartCoolDown();
                skillButton.SetLabelType(ModAbilityButton.LabelType.Crewmate);
                GameOperatorManager.Instance.Subscribe<GameEndEvent>(ev =>
                {
                    if (ev.EndState.EndCondition == NebulaGameEnd.ImpostorWin)
                    {
                        if (MyPlayer.TryGetModifier<InsomniacsModifier.Instance>(out var i)&&MyPlayer.TryGetModifier<DreamweaverModifier.Instance>(out var d))
                        {
                            new StaticAchievementToken("insomniacs.challenge2");
                        }
                    }
                }, NebulaAPI.CurrentGame);
            }
        }
    }
}
public class InsomniacsModifier : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    private InsomniacsModifier() : base("insomniacsM", new(216, 164, 246), null, true, () => false)
    {
    }
    bool DefinedAssignable.ShowOnHelpScreen
    {
        get
        {
            return false;
        }
    }
    bool DefinedAssignable.ShowOnFreeplayScreen
    {
        get
        {
            return false;
        }
    }
    IEnumerable<DefinedAssignable> DefinedAssignable.AchievementGroups => [Insomniacs.MyRole, Insomniacs.MyRole, MyRole];
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    static public InsomniacsModifier MyRole = new InsomniacsModifier();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        ModAbilityButton killButton;
        bool RuntimeModifier.MyCrewmateTaskIsIgnored => true;
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public Instance(GamePlayer player) : base(player)
        {
        }
        [Local]
        private void OnMeetingEnd(TaskPhaseStartEvent ev)
        {
            NebulaGameManager instance = NebulaGameManager.Instance;
            List<GamePlayer> list = instance.AllPlayerInfo.Where((GamePlayer p) => !p.IsDead && (p.Role is Dreamweaver.Instance || p.Modifiers.Any(m => m is DreamweaverModifier.Instance))).ToList();
            if (list.Count<=0&&!MyPlayer.IsDead)
            {
                MyPlayer.RemoveModifier(MyRole);
            }
        }
        [OnlyMyPlayer]
        private void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.IsWin |= ev.GameEnd == NebulaGameEnd.ImpostorWin;
        }

        [OnlyMyPlayer]
        private void BlockWins(PlayerBlockWinEvent ev)
        {
            ev.IsBlocked |= ev.GameEnd == NebulaGameEnd.CrewmateWin;
        }
        [Local]
        private void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (!ev.Player.AmOwner && (ev.Player.Role is Dreamweaver.Instance || ev.Player.TryGetModifier<DreamweaverModifier.Instance>(out var m)))
            {
                ev.Color = new Virial.Color(233, 96, 174);
            }
        }
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.EndCondition==NebulaGameEnd.ImpostorWin)
            {
                new StaticAchievementToken("insomniacs.challenge1");
            }
        }
        void RuntimeAssignable.OnInactivated()
        {
            if (AmOwner)
            {
                Game currentGame = NebulaAPI.CurrentGame;
                if (currentGame != null)
                {
                    TitleShower module = currentGame.GetModule<TitleShower>();
                    if (module != null)
                    {
                        module.SetText(Language.Translate("role.insomniacs.onRemoveModifier"), MyRole.RoleColor.ToUnityColor(), 6f);
                    }
                }
                if (!(MyPlayer.Role is Dreamweaver.Instance) && !MyPlayer.TryGetModifier<DreamweaverModifier.Instance>(out var d))
                {
                    if (Insomniacs.DreamweaverDeadToCrewmateOption)
                    {
                        MyPlayer.SetRole(Nebula.Roles.Crewmate.Crewmate.MyRole);
                    }
                }
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor.ToUnityColor(), 0f, 0.8f, 0.7f, 0f);
                if (!meetingstart)
                {
                    new StaticAchievementToken("insomniacs.another6");
                }
            }
        }
        bool meetingstart;
        [OnlyMyPlayer]
        void OnCheckKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (ev.Target.Role is Dreamweaver.Instance||ev.Target.TryGetModifier<DreamweaverModifier.Instance>(out var dm))
            {
                ev.SetAsCannotKillBasically();
            }
        }
        void OnMeetingStart(MeetingStartEvent ev)
        {
            meetingstart = true;
        }
        List<TrackingArrowAbility> tracking = new List<TrackingArrowAbility>();
        [Local]
        void OnSetRole(PlayerRoleSetEvent ev)
        {
            var p = tracking.FirstOrDefault(r => r.MyPlayer.PlayerId == ev.Player.PlayerId);
            if (ev.Role is Dreamweaver.Instance)
            {
                if (p == null)
                {
                    tracking.Add(new TrackingArrowAbility(ev.Player, 0f, new UnityEngine.Color32(233, 96, 174, 255), false).Register(this));
                }
            }
        }
        [Local]
        void OnSetModifier(PlayerModifierSetEvent ev)
        {
            var p = tracking.FirstOrDefault(r => r.MyPlayer.PlayerId == ev.Player.PlayerId);
            if (ev.Modifier is DreamweaverModifier.Instance)
            {
                if (p == null)
                {
                    tracking.Add(new TrackingArrowAbility(ev.Player, 0f, new UnityEngine.Color32(233, 96, 174, 255), false).Register(this));
                }
            }
        }
        [Local]
        void OnRemoveModifier(PlayerModifierRemoveEvent ev)
        {
            var p = tracking.FirstOrDefault(r => r.MyPlayer.PlayerId == ev.Player.PlayerId);
            if (p != null&&ev.Modifier is DreamweaverModifier.Instance)
            {
                p.Release();
                tracking.Remove(p);
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                new StaticAchievementToken("insomniacs.common1");
                var role = MyPlayer.Role.Role;
                if (role.Category == RoleCategory.ImpostorRole || role.LocalizedName.Contains("jackal")|| role.LocalizedName.Contains("avenger") || role.LocalizedName.Contains("knighted") || role.LocalizedName.Contains("sheriff") || role.LocalizedName.Contains("knight") || role.LocalizedName.Contains("yandere"))
                {
                    return;
                }
                else
                {
                    ObjectTracker<GamePlayer> killTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.KillablePredicate(MyPlayer),null, false, false);
                    killButton = NebulaAPI.Modules.AbilityButton(this, false, true, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Kill, null);
                    killButton.Availability = (button) => killTracker.CurrentTarget != null && MyPlayer.CanMove;
                    killButton.Visibility = (button) => !MyPlayer.IsDead;
                    this.killButton.OnClick = delegate (ModAbilityButton button)
                    {
                        var player = killTracker.CurrentTarget;
                        MyPlayer.MurderPlayer(player, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill, (result) =>
                        {
                            if (result!=KillResult.Kill&&player.Role is SchrodingerCat.Instance)
                            {
                                if (Insomniacs.KillCatGetModifierOption)
                                {
                                    player.AddModifier(InsomniacsModifier.MyRole);
                                }
                            }
                        });
                        NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    };
                    
                    killButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, Insomniacs.KillCoolDownOption).SetAsKillCoolTimer().Start(null);
                    killButton.StartCoolDown();
                    killButton.SetLabel("kill");
                    killButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                }
                List<GamePlayer> list = NebulaGameManager.Instance.AllPlayerInfo.Where((GamePlayer p) => !p.IsDead && (p.Role is Dreamweaver.Instance || p.Modifiers.Any(m => m is DreamweaverModifier.Instance))).ToList();
                if (list.Count == 0)
                {
                    return;
                }
                foreach (var p in list)
                {
                    tracking.Add(new TrackingArrowAbility(p, 0f, new UnityEngine.Color32(233, 96, 174,255), false).Register(this));
                }
            }
        }
        string RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort)
        {
            return Language.Translate("role.insomniacs.prefix").Color(MyRole.RoleColor.ToUnityColor()) + lastRoleName;
        }
        void OnSheriffKill(SheriffCheckKillEvent ev)
        {
            if (ev.Player.Role.Role.LocalizedName.Contains("sheriff") && ev.Player.IsModMadmate())
            {
                ev.CanKill = false;
            }
            else if (ev.Target.PlayerId == MyPlayer.PlayerId)
            {
                ev.CanKill = true;
            }
        }
        void OnCollactorCheckTeam(CollatorCheckTeamEvent ev)
        {
            if (ev.Target.PlayerId == MyPlayer.PlayerId)
            {
                var value = NebulaAPI.Configurations.Configuration("options.role.collator.madmateIsClassifiedAs", new string[] { "options.role.collator.madmateIsClassifiedAs.impostor", "options.role.collator.madmateIsClassifiedAs.crewmate" }, 0, null, null).GetValue();
                ev.Team = value == 0 ? NebulaTeams.ImpostorTeam : NebulaTeams.CrewmateTeam;
            }
        }
    }
}