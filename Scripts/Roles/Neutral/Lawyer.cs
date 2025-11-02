using Nebula.Roles.Neutral;
using Nebula.VoiceChat;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Modifier;
using static NetworkedPlayerInfo;

namespace Plana.Roles.Neutral;

public class Lawyer : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private static Team RoleTeam = new Team("teams.lawyer", new Virial.Color(134, 153, 25), TeamRevealType.OnlyMe);
    private Lawyer() : base("lawyer", new(134, 153, 25), RoleCategory.NeutralRole, RoleTeam, [TargetKnowMyIsClient, WinAfterMeetings, WinNeededMeetings, LawyerKnowClientRole,LawyerCannotGuessClient])
    {
        IConfigurationHolder configurationHolder = base.ConfigurationHolder!;
        if (configurationHolder == null)
        {
            return;
        }
        configurationHolder.ScheduleAddRelated(() => new IConfigurationHolder[] { Pursuer.MyRole.ConfigurationHolder! });
    }
    internal static BoolConfiguration TargetKnowMyIsClient = NebulaAPI.Configurations.Configuration("options.role.lawyer.targetknowmyisclient", true);
    static BoolConfiguration WinAfterMeetings = NebulaAPI.Configurations.Configuration("options.role.lawyer.winaftermeetings", true);
    static IntegerConfiguration WinNeededMeetings = NebulaAPI.Configurations.Configuration("options.role.lawyer.winneededmeetings", (1, 20), 5, () => WinAfterMeetings);
    static BoolConfiguration LawyerKnowClientRole = NebulaAPI.Configurations.Configuration("options.role.lawyer.lawyerknowclientrole", true);
    static BoolConfiguration LawyerCannotGuessClient = NebulaAPI.Configurations.Configuration("options.role.lawyer.lawyercannotguessclient", true);
    Citation? HasCitation.Citation => Citations.TheOtherRolesGM;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public Lawyer MyRole = new Lawyer();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        public static GameEnd LawyerWin = NebulaAPI.Preprocessor!.CreateEnd("lawyer1", MyRole.RoleColor, 48);
        public static ExtraWin LawyerExtraWin = NebulaAPI.Preprocessor!.CreateExtraWin("lawyer2", MyRole.RoleColor);
        public static ExtraWin PursuerExtraWin = NebulaAPI.Preprocessor!.CreateExtraWin("lawyer3", MyRole.RoleColor);
        DefinedRole RuntimeRole.Role => MyRole;
        RoleTaskType RuntimeRole.TaskType => RoleTaskType.RoleTask;
        [Local]
        private void EditGuessable(PlayerCanGuessPlayerLocalEvent ev)
        {
            if (LawyerCannotGuessClient)
            {
                if (MyClient != null && ev.Player.PlayerId == MyClient.PlayerId)
                {
                    ev.CanGuess = false;
                }
            }
        }
        [OnlyMyPlayer]
        private void OnCastVoteLocal(PlayerVoteCastLocalEvent ev)
        {
            if (MyClient!=null&&ev.VoteFor!.PlayerId==MyClient.PlayerId)
            {
                new StaticAchievementToken("lawyer.another1");
            }
        }

        public Instance(GamePlayer player) : base(player)
        {
        }
        [OnlyMyPlayer]
        void OnCheckExtraWin(PlayerCheckExtraWinEvent ev)
        {
            if (MyClient==null)
            {
                return;
            }
            if (ev.WinnersMask.Test(MyClient))
            {
                ev.SetWin(true);
                ev.ExtraWinMask.Add(LawyerExtraWin);
            }
        }
        [Local]
        private void OnGameEnd(GameEndEvent ev)
        {
            if (MyClient == null)
            {
                return;
            }
            if (EarlyDead&&ev.EndState.ExtraWins.Test(LawyerExtraWin))
            {
                new StaticAchievementToken("lawyer.challenge2");
            }
            if (ev.EndState.ExtraWins.Test(LawyerExtraWin))
            {
                if (!MyClient.IsDead && !MyPlayer.IsDead)
                {
                    new StaticAchievementToken("lawyer.challenge1");
                }
            }
            if (ev.EndState.EndCondition != NebulaGameEnd.JackalWin)
            {
                return;
            }
            if (!ev.EndState.Winners.Test(MyClient))
            {
                return;
            }
            if (ev.EndState.ExtraWins.Test(PatchManager.tunnyExtra))
            {
                new StaticAchievementToken("lawyer.another3");
            }
        }
        bool meetingstart, EarlyDead;
        static GamePlayer? MyClient;
        static RemoteProcess<GamePlayer> LawyerSelectClient = new RemoteProcess<GamePlayer>("lawyerSelectClient", (p, _) =>
        {
            MyClient = p;
        });
        [Local]
        private void ReflectRoleName(PlayerSetFakeRoleNameEvent ev)
        {
            if (LawyerKnowClientRole)
            {
                if (ev.Player.PlayerId == MyClient!.PlayerId)
                {
                    ev.Set(ev.Player.Role.DisplayColoredName);
                }
            }
        }
        int meetingnum;
        [Local,OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            EarlyDead = !meetingstart;
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            meetingstart = true;
        }
        [Local]
        void OnMeetingEnd(TaskPhaseRestartEvent ev)
        {
            if (WinAfterMeetings)
            {
                meetingnum++;
                if (!MyPlayer.IsDead&&meetingnum >= WinNeededMeetings)
                {
                    NebulaGameManager instance3 = NebulaGameManager.Instance!;
                    if (instance3 == null)
                    {
                        return;
                    }
                    instance3.RpcInvokeSpecialWin(LawyerWin, 1 << (int)this.MyPlayer.PlayerId);
                }
            }
        }
        [OnlyMyPlayer]
        void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == LawyerWin);
        }
        [Local]
        private void DecorateClientName(PlayerDecorateNameEvent ev)
        {
            if (AmOwner && !MyPlayer.IsDead)
            {
                if (MyClient != null && MyClient.PlayerId == ev.Player.PlayerId && !ev.Player.AmOwner)
                {
                    ev.Name += " §".Color(MyRole.RoleColor.ToUnityColor());
                }
            }
        }
        void OnPlayerDead(PlayerDieOrDisconnectEvent ev)
        {
            if (MyClient != null && ev.Player.PlayerId == MyClient.PlayerId)
            {
                MyPlayer.SetRole(Pursuer.MyRole);
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            MyClient = null!;
            if (AmOwner)
            {
                EarlyDead = false;
                meetingstart = false;
                meetingnum = 0;
                var list = NebulaGameManager.Instance?.AllPlayerInfo.Where(p => !p.IsDead && !p.IsDisconnected &&
(p.IsImpostor || (!p.IsCrewmate && p.Role.Role.LocalizedName.Contains("jackal")
)) && !p.TryGetModifier<LawyerClient.Instance>(out var m)).ToList();
                var selectplayer = list![UnityEngine.Random.Range(0, list.Count)];
                LawyerSelectClient.Invoke(selectplayer);
                selectplayer.AddModifier(LawyerClient.MyRole, [MyPlayer.PlayerId]);
                GameOperatorManager.Instance!.Subscribe<PlayerMurderedEvent>(ev =>
                {
                    if (ev.Player.TryGetModifier<LawyerClient.Instance>(out var m)&&ev.Murderer.AmOwner)
                    {
                        new StaticAchievementToken("lawyer.another2");
                    }
                }, NebulaAPI.CurrentGame!);
            }
        }
    }
}
public class Pursuer : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private static Team RoleTeam = new Team("teams.pursuer", new Virial.Color(134, 153, 25), TeamRevealType.OnlyMe);
    private Pursuer() : base("pursuer", new(134, 153, 25), RoleCategory.NeutralRole, RoleTeam,[BlankCooldown,BlankNum,TaskIsCrewmate],false,true,()=> ((ISpawnable)Lawyer.MyRole).IsSpawnable) 
    {
        IConfigurationHolder configurationHolder = base.ConfigurationHolder!;
        if (configurationHolder == null)
        {
            return;
        }
        configurationHolder.ScheduleAddRelated(() => new IConfigurationHolder[] { Lawyer.MyRole.ConfigurationHolder! });
    }
    Citation? HasCitation.Citation => Citations.TheOtherRolesGM;
    bool ISpawnable.IsSpawnable
    {
        get
        {
            return ((ISpawnable)Lawyer.MyRole).IsSpawnable;
        }
    }

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    static FloatConfiguration BlankCooldown = NebulaAPI.Configurations.Configuration("options.role.pursuer.blankcd", (0f, 60f, 2.5f), 7.5f,FloatConfigurationDecorator.Second);
    static IntegerConfiguration BlankNum = NebulaAPI.Configurations.Configuration("options.role.pursuer.blanknum", (0, 20), 5);
    static BoolConfiguration TaskIsCrewmate = NebulaAPI.Configurations.Configuration("options.role.pursuer.TaskIsCrewmate", false);
    static public Pursuer MyRole = new Pursuer();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        RoleTaskType RuntimeRole.TaskType => RoleTaskType.CrewmateTask;
        public Instance(GamePlayer player) : base(player)
        {
        }
        [OnlyMyPlayer]
        private void CheckExtraWin(PlayerCheckExtraWinEvent ev)
        {
            if (!MyPlayer.IsDead)
            {
                ev.SetWin(true);
                ev.ExtraWinMask.Add(Lawyer.Instance.PursuerExtraWin);
            }
        }
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.ExtraWins.Test(Lawyer.Instance.PursuerExtraWin))
            {
                if (useall)
                {
                    new StaticAchievementToken("pursuer.challenge1");
                }
                var target = NebulaGameManager.Instance?.AllPlayerInfo.FirstOrDefault(p => p.TryGetModifier<LawyerClient.Instance>(out var c));
                if (target != null && ev.EndState.Winners.Test(target))
                {
                    new StaticAchievementToken("pursuer.challenge2");
                }
            }
        }
        ModAbilityButton? skillButton;
        static List<GamePlayer> blankPlayer = new List<GamePlayer>();
        RemoteProcess<byte> addblankPlayer = new RemoteProcess<byte>("addblankPlayer", (p, _) =>
        {
            blankPlayer.Add(GamePlayer.GetPlayer(p)!);
        });
        Virial.Media.Image blankimage = NebulaAPI.AddonAsset.GetResource("PursuerButton.png")!.AsImage()!;
        [OnlyMyPlayer]
        void OnDead(PlayerDieOrDisconnectEvent ev)
        {
            (MyPlayer.Tasks as PlayerTaskState)!.BecomeToOutsider();
        }
        bool useall;
        void RuntimeAssignable.OnActivated()
        {
            blankPlayer = new List<GamePlayer>();
            useall = false;
            if (AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor.ToUnityColor(), 0f, 0.25f, 0.4f, 0f);
                int leftuses = BlankNum;
                var myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, (GamePlayer p) => ObjectTrackers.StandardPredicate(p), null, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption, false);
                skillButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, false, false, VirtualKeyInput.Ability, null,BlankCooldown, "blank", blankimage, (ModAbilityButton _) => myTracker.CurrentTarget != null, (ModAbilityButton _) => !MyPlayer.IsDead&&leftuses>0, false).SetLabelType(ModAbilityButton.LabelType.Utility);
                skillButton.ShowUsesIcon(1, leftuses.ToString());
                skillButton.OnClick = (button) =>
                {
                    new StaticAchievementToken("pursuer.common1");
                    leftuses--;
                    button.UpdateUsesIcon(leftuses.ToString());
                    addblankPlayer.Invoke(myTracker.CurrentTarget!.PlayerId);
                    button.StartCoolDown();
                    if (leftuses <= 0)
                    {
                        button.HideUsesIcon();
                        useall = true;
                    }
                };
                if (!MyPlayer.IsDead)
                {
                    (MyPlayer.Tasks as PlayerTaskState)!.BecomeToCrewmate();
                    if (!TaskIsCrewmate)
                    {
                        (MyPlayer.Tasks as PlayerTaskState)!.QuotaReduction(MyPlayer.Tasks.Quota);
                    }
                }
            }
        }
        void OnMeetingStart(MeetingPreStartEvent ev)
        {
            blankPlayer = new List<GamePlayer>();
        }
        private void CheckKill(PlayerCheckKilledEvent ev)
        {
            if (ev.IsMeetingKill)
            {
                return;
            }
            if (ev.Player.PlayerId==ev.Killer.PlayerId)
            {
                return;
            }
            if (blankPlayer.Contains(ev.Killer))
            {
                ev.Result = KillResult.Rejected;
                blankPlayer.Remove(ev.Killer);
                if (ev.Killer.AmOwner)
                {
                    AmongUsUtil.PlayCustomFlash(MyRole.RoleColor.ToUnityColor(), 0f, 0.25f, 0.4f, 0f);
                }
            }
        }
    }
}
    public class LawyerClient : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
    {
        private LawyerClient() : base("lawyerclient", new Virial.Color(134, 153, 25), null, true, () => false)
        {
        }
        Citation? HasCitation.Citation { get { return Citations.TheOtherRolesGM; } }
        static public LawyerClient MyRole = new LawyerClient();
        RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {

        bool RuntimeAssignable.CanBeAwareAssignment
        {
            get
            {
                if (Lawyer.TargetKnowMyIsClient)
                {
                    return true;
                }
                NebulaGameManager instance = NebulaGameManager.Instance!;
                return instance != null && instance.CanSeeAllInfo;
            }
        }

        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public Instance(GamePlayer player) : base(player)
        {
        }
        void RuntimeAssignable.OnActivated()
        {
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if ((Lawyer.TargetKnowMyIsClient&&AmOwner)||canSeeAllInfo)name += " §".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}