using Plana.Core;

namespace Plana.Roles.Neutral;

public class YTrackingTargetArrowAbility : FlexibleLifespan, IGameOperator
{
    public bool ShowArrow { get; set; } = true;
    public GamePlayer MyPlayer
    {
        get
        {
            return this.target;
        }
    }

    public YTrackingTargetArrowAbility(GamePlayer target, float interval, global::UnityEngine.Color color)
    {
        this.target = target;
        this.interval = interval;
        this.timer = -1f;
        this.color = color;
    }

    private void Update(GameUpdateEvent ev)
    {
        if (ExileController.Instance)
        {
            this.timer = -1f;
        }
        else
        {
            this.timer -= Time.deltaTime;
            if (this.timer < 0f)
            {
                if (this.arrow == null)
                {
                    this.arrow = new Arrow(null, true, false, false)
                    {
                        TargetPos = target.TruePosition
                    }.SetColor(this.color).Register(this);
                }
                this.arrow.TargetPos = this.target.Position;
                this.timer = interval;
            }
        }
        if (this.arrow != null)
        {
            this.arrow.IsActive = ShowArrow&&!this.target.IsDead && !MeetingHud.Instance && !ExileController.Instance;
        }
    }

    private GamePlayer target;

    private float interval;

    private float timer;

    private Arrow arrow;

    private global::UnityEngine.Color color;
}
[NebulaRPCHolder]
public class Yandere : DefinedRoleTemplate, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder
{
    public static Team MyTeam = new Team("teams.yandere", new Virial.Color(227, 0, 136), TeamRevealType.OnlyMe);
    public static FloatConfiguration AddDistance = NebulaAPI.Configurations.Configuration("options.role.yandere.addTargetDistance", new ValueTuple<float, float, float>(0f, 20f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);
    private Yandere() : base("yandere", new Virial.Color(227, 0, 136), RoleCategory.NeutralRole, MyTeam, [BaseKillCD, MaxKillCD, MinKillCD, TargetDeadKillCD, AddDistance, AddDuration, AddCoolDown, killCorrectSubCD, killWrongAddCD, ActiveTargetArrow, TargetArrowUpdateTime, ActiveKillTargetArrow, KillTargetArrowUpdateTime, StayKillCD, CanUseVent, HasImpostorVision, YandereRoleOption])
    {
        IConfigurationHolder configurationHolder = base.ConfigurationHolder;
        if (configurationHolder != null)
        {
            configurationHolder.AddTags(new ConfigurationTag[] { Nebula.Configuration.ConfigurationTags.TagFunny });
        }
        base.ConfigurationHolder.Illustration = NebulaAPI.AddonAsset.GetResource("YandereOption.png").AsImage(115f);
        MetaAbility.RegisterCircle(new EffectCircleInfo("role.yandere.targetdistanceRange", () => AddDistance, () => null, base.RoleColor.ToUnityColor()));
    }
    public static int[] GenerateArgument(DefinedRole role)
    {
        return new int[] { (role != null) ? role.Id : (-1) };
    }
    bool DefinedRole.IsKiller => true;
    Citation? HasCitation.Citation { get { return PCitations.ExtremeRoles; } }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, Nebula.Roles.Roles.GetRole(arguments.Get(0, -1)), arguments.Skip(1).ToArray());
    public static FloatConfiguration BaseKillCD = NebulaAPI.Configurations.Configuration("options.role.yandere.killcd", new ValueTuple<float, float, float>(10f, 60f, 2.5f), 25f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration MinKillCD = NebulaAPI.Configurations.Configuration("options.role.yandere.minKillCD", new ValueTuple<float, float, float>(0f, 60f, 2.5f), 0f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration MaxKillCD = NebulaAPI.Configurations.Configuration("options.role.yandere.maxKillCD", new ValueTuple<float, float, float>(0f, 60f, 2.5f), 45f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration TargetDeadKillCD = NebulaAPI.Configurations.Configuration("options.role.yandere.Prokillcd", new ValueTuple<float, float, float>(10f, 60f, 2.5f), 17.5f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration AddDuration = NebulaAPI.Configurations.Configuration("options.role.yandere.addTargetDuration", new ValueTuple<float, float, float>(0f, 60f, 1f), 5f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration AddCoolDown = NebulaAPI.Configurations.Configuration("options.role.yandere.addcooldown", new ValueTuple<float, float, float>(5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration killCorrectSubCD = NebulaAPI.Configurations.Configuration("options.role.yandere.killcorrectAddCD", new ValueTuple<float, float, float>(0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    public static FloatConfiguration killWrongAddCD = NebulaAPI.Configurations.Configuration("options.role.yandere.killwrongAddCD", new ValueTuple<float, float, float>(0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    public static BoolConfiguration ActiveTargetArrow = NebulaAPI.Configurations.Configuration("options.role.yandere.activetargetarrow", true);
    public static FloatConfiguration TargetArrowUpdateTime = NebulaAPI.Configurations.Configuration("options.role.yandere.targetupdatetime", new ValueTuple<float, float, float>(0f, 30f, 2.5f), 0f, FloatConfigurationDecorator.Second);
    public static BoolConfiguration ActiveKillTargetArrow = NebulaAPI.Configurations.Configuration("options.role.yandere.activekilltargetarrow", true);
    public static BoolConfiguration StayKillCD = NebulaAPI.Configurations.Configuration("options.role.yandere.aftermeetingstaycd", false);
    public static BoolConfiguration CanUseVent = NebulaAPI.Configurations.Configuration("options.role.yandere.canusevent", true);
    public static BoolConfiguration HasImpostorVision = NebulaAPI.Configurations.Configuration("options.role.yandere.hasImpostorVision", true);
    public static FloatConfiguration KillTargetArrowUpdateTime = NebulaAPI.Configurations.Configuration("options.role.yandere.killtargetupdatetime", new ValueTuple<float, float, float>(0f, 30f, 2.5f), 0f, FloatConfigurationDecorator.Second);
    public static BoolConfiguration YandereRoleOption = NebulaAPI.Configurations.Configuration("options.role.yandere.yandererole", false);
    static public Yandere MyRole = new Yandere();
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(Virial.Game.Player player, int[] arguments)
    {
        var role = Nebula.Roles.Roles.GetRole(arguments.Get(0, -1));
        var ability = role?.GetUsurpedAbility(player, arguments.Skip(1).ToArray());
        if (ability != null) return new UsurpedYandereAbility(player, role!, ability);
        return null;
    }

    private class UsurpedYandereAbility : FlexibleLifespan, IUsurpableAbility
    {
        public DefinedRole Role => YandereRole;
        private DefinedRole YandereRole;
        private IUsurpableAbility YandereAbility;
        public IUsurpableAbility Ability => YandereAbility;
        bool IUsurpableAbility.IsUsurped => YandereAbility.IsUsurped;
        bool IUsurpableAbility.Usurp() => YandereAbility.Usurp();
        public GamePlayer MyPlayer { get; private init; }
        public bool AmOwner => MyPlayer.AmOwner;

        public UsurpedYandereAbility(GamePlayer player, DefinedRole role, IUsurpableAbility ability)
        {
            this.MyPlayer = player;
            YandereRole = role;
            YandereAbility = ability.Register(this);
        }

        IEnumerable<IPlayerAbility> IPlayerAbility.SubAbilities => [YandereAbility];
        int[] IPlayerAbility.AbilityArguments => [YandereRole.Id, .. YandereAbility.AbilityArguments];
    }

    string DefinedRole.GetDisplayName(IPlayerAbility ability)
    {
        if (ability is UsurpedYandereAbility a) return a.Role.GetDisplayName(a.Ability);
        return (this as DefinedRole).DisplayName;
    }

    string DefinedRole.GetDisplayShort(IPlayerAbility ability)
    {
        if (ability is UsurpedYandereAbility a) return a.Role.GetDisplayShort(a.Ability);
        return (this as DefinedRole).DisplayShort;
    } 

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public DefinedRole? MyExRole { get; set; }
        public IPlayerAbility? ExAbility { get; private set; } = null;
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => MyExRole != null ? [MyRole, MyExRole] : [MyRole];
        private int[] StoredExArgument { get; set; }
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => ExAbility != null ? [ExAbility, .. ExAbility.SubAbilities] : [];
        ModAbilityButton killButton;
        static GamePlayer targetPlayer;
        public Instance(GamePlayer player, DefinedRole? yandereRole, int[] yandereRoleArgument): base(player)
        {
            MyExRole = yandereRole;
            StoredExArgument = yandereRoleArgument;
        }
        int[]? RuntimeAssignable.RoleArguments 
        {
            get
            {

                if (ExAbility != null)
                    return (new int[] { MyExRole.Id }).Concat(ExAbility.AbilityArguments).ToArray() ;
                return MyExRole==null?null:[MyExRole.Id];
            }
        }
        int[]? RuntimeRole.UsurpedAbilityArguments => (ExAbility?.AbilityArguments ?? []).Prepend(MyExRole?.Id ?? -1).ToArray();
        string RuntimeRole.DisplayIntroRoleName
        {
            get
            {
                return ((RuntimeAssignable)this).DisplayName;
            }
        }

        string RuntimeRole.DisplayIntroBlurb
        {
            get
            {
                string text = Language.Translate("role.yandere.InGameblurb")+ " ♥";
                var player = targetPlayer;
                if (MyExRole!=null)
                {
                    text = MyExRole.DisplayIntroBlurb +Environment.NewLine+ text;
                }
                return text.Replace("%NAME%", (((player != null) ? player.Name : null) ?? "ERROR").Color(MyRole.RoleColor.ToUnityColor()));
            }
        }
        string RuntimeAssignable.DisplayColoredName
        {
            get
            {
                return ((RuntimeAssignable)this).DisplayName.Color(MyRole.UnityColor);
            }
        }

        string RuntimeAssignable.DisplayName
        {
            get
            {
                return MyExRole != null ? Language.Translate("role.yandere.prefix") + MyExRole!.GetDisplayName(ExAbility) : (MyRole as DefinedAssignable).DisplayName;
            }
        }
        List<GamePlayer> killplayer = new List<GamePlayer>();
        private YTrackingTargetArrowAbility arrowAbility;
        List<YTrackingTargetArrowAbility> killTargetArrow = new List<YTrackingTargetArrowAbility>();
        Dictionary<byte, float> targetDic = new Dictionary<byte, float>();
        public static GameEnd YandereTeamWin = NebulaAPI.Preprocessor!.CreateEnd("yandere", MyRole.RoleColor, 45);
        void BlockTriggerEnd(EndCriteriaPreMetEvent ev)
        {
            if (ev.GameEnd != NebulaGameEnd.LoversWin&&ev.GameEnd!=YandereTeamWin && !MyPlayer.IsDead && ev.EndReason == GameEndReason.Situation)
            {
                ev.Reject();
            }
        }
        bool isSameTeam(GamePlayer p)
        {
            if (p == null)
            {
                return false;
            }
            if (p.TryGetModifier<YandereLover.Instance>(out var l))
            {
                return true;
            }
            if (p.Role is Yandere.Instance)
            {
                return true;
            }
            return false;
        }
        private void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == YandereTeamWin && isSameTeam(ev.Player));
        }
        [Local]
        private void DecoratetargetColor(PlayerDecorateNameEvent ev)
        {
            if (AmOwner && !MyPlayer.IsDead)
            {
                if (targetPlayer != null && targetPlayer.PlayerId == ev.Player.PlayerId && !ev.Player.AmOwner)
                {
                    ev.Name += " ♥".Color(MyRole.RoleColor.ToUnityColor());
                    if (!targetPlayer.TryGetModifier<YandereLover.Instance>(out var m))
                    {
                        targetPlayer.AddModifier(YandereLover.MyRole);
                    }
                }
            }
        }

        float cooldown = AddCoolDown;
        [Local]
        private void OnMeetingStart(MeetingStartEvent ev)
        {
            cooldown = AddCoolDown;
        }
        bool RuntimeRole.CanMoveInVent
        {
            get
            {
                return CanUseVent;
            }
        }

        bool RuntimeRole.CanUseVent
        {
            get
            {
                return CanUseVent;
            }
        }

        bool RuntimeRole.HasImpostorVision
        {
            get
            {
                return HasImpostorVision;
            }
        }

        bool RuntimeRole.IgnoreBlackout
        {
            get
            {
                return HasImpostorVision;
            }
        }
        bool meetingstart;
        bool loverDeadEarly;
        void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.EndCondition == YandereTeamWin&&ev.EndState.Winners.Test(MyPlayer)&&MyPlayer.AmOwner)
            {
                new StaticAchievementToken("yandere.challenge2");
                if (loverDeadEarly)
                {
                    new StaticAchievementToken("yandere.another3");
                }
                var target = NebulaGameManager.Instance?.AllPlayerInfo?.FirstOrDefault(p => p.TryGetModifier<YandereLover.Instance>(out var m));
                if (target!=null&&ev.EndState.Winners.Test(target))
                {
                    new StaticAchievementToken("yandere.challenge3");
                }
                if (Allkillplayers.Count>=5)
                {
                    new StaticAchievementToken("yandere.challenge1");
                }
                if (target.MyKiller!=null&&!Allkillplayers.Any(p=>p.PlayerId==target.MyKiller.PlayerId))
                {
                    new StaticAchievementToken("yandere.another2");
                }
            }
        }
        [Local]
        void OnPlayerDie(PlayerDieEvent ev)
        {
            if (!meetingstart&&ev.Player.TryGetModifier<YandereLover.Instance>(out var i))
            {
                loverDeadEarly = true;
            }
        }
        [Local]
        void MeetingStart(MeetingStartEvent ev)
        {
            meetingstart = false;
        }
        [Local]
        private void Update(GameUpdateEvent ev)
        {
            try
            {
                if (cooldown > 0f)
                {
                    if (!MeetingHud.Instance && !ExileController.Instance)
                    {
                        cooldown -= Time.deltaTime;
                    }
                    return;
                }
                foreach (NetworkedPlayerInfo playerInfo in GameData.Instance.AllPlayers)
                {
                    byte playerId = playerInfo.PlayerId;
                    float playerProgress;
                    if (targetDic.TryGetValue(playerId, out playerProgress))
                    {
                        if (!playerInfo.Disconnected && !playerInfo.IsDead && MyPlayer.PlayerId != playerId && targetPlayer.PlayerId != playerId && !playerInfo.Object.inVent)
                        {
                            PlayerControl @object = playerInfo.Object;
                            if (@object)
                            {
                                var pos = targetPlayer.ToAUPlayer().GetTruePosition();
                                UnityEngine.Vector2 vector = @object.GetTruePosition() - pos;
                                float magnitude = vector.magnitude;
                                if (magnitude <= AddDistance && !PhysicsHelpers.AnyNonTriggersBetween(pos, vector.normalized, magnitude, Constants.ShipAndObjectsMask))
                                {
                                    playerProgress += Time.deltaTime;
                                }
                                else
                                {
                                    playerProgress = 0f;
                                }
                            }
                        }
                        else
                        {
                            playerProgress = 0f;
                        }
                        if (playerProgress >= AddDuration && !killplayer.Contains(GamePlayer.GetPlayer(playerId)))
                        {
                            try
                            {
                                killplayer.Add(GamePlayer.GetPlayer(playerId));
                                killTargetArrow.Add(new YTrackingTargetArrowAbility(GamePlayer.GetPlayer(playerId), TargetArrowUpdateTime, Virial.Color.Red.ToUnityColor()).Register<YTrackingTargetArrowAbility>(this));
                                targetDic.Remove(playerId);
                            }
                            catch (Exception e)
                            {
                                PDebug.Log(e);
                            }
                        }
                        else
                        {
                            targetDic[playerId] = playerProgress;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        int correctkillnum, wrongkillnum;
        List<GamePlayer> Allkillplayers;
        [Local]
        void EditMyKillCooldown(ResetKillCooldownLocalEvent ev)
        {
            IEnumerable<IKillButtonLike> buttons =NebulaAPI.CurrentGame?.KillButtonLikeHandler.KillButtonLike;
            var max = 0f;
            if (targetPlayer?.IsDead ?? true)
            {
                max=TargetDeadKillCD;
            }
            else
            {
                float num = BaseKillCD + correctkillnum * -killCorrectSubCD + wrongkillnum * killWrongAddCD;
                num = Mathf.Clamp(num, MinKillCD, MaxKillCD);
                max = num;
            }
            foreach (var button in buttons)
            {
                if (button is ModKillButtonHandler handler)
                {
                    handler.button.CoolDownTimer = NebulaAPI.Modules.Timer(this,max).Start(null);
                }
            }
        }
        [Local]
        void OnTaskStart(TaskPhaseRestartEvent ev)
        {
            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
        }
        [OnlyMyPlayer]
        private void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (ev.Target.TryGetModifier<YandereLover.Instance>(out var lover))
            {
                ev.SetAsCannotKillBasically();
            }
        }
        private void OnMeetingEnd(MeetingPreEndEvent ev)
        {
            PDebug.Log("OnMeetingEnd");
            if (!StayKillCD)
            {
                wrongkillnum = 0;
                correctkillnum = 0;
            }
            killplayer = new List<GamePlayer>();
            targetDic = new Dictionary<byte, float>();
            foreach (NetworkedPlayerInfo player in GameData.Instance.AllPlayers)
            {
                targetDic.Add(player.PlayerId, 0f);
            }
            foreach (var arrow in killTargetArrow)
            {
                arrow.Release();
                arrow.ShowArrow = false;
            }
            killTargetArrow = new List<YTrackingTargetArrowAbility>();
            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
        }
        void RuntimeRole.Usurp()
        {
            (ExAbility as IUsurpableAbility)?.Usurp();
        }
        void RuntimeAssignable.OnActivated()
        {
            try
            {
                if (AmOwner)
                {

                    loverDeadEarly = false;
                    meetingstart = false;
                    targetPlayer = null;
                    if (targetPlayer == null)
                    {
                        List<PlayerControl> RandomSelectTarget = new List<PlayerControl>();
                        PlayerControl lover = null;
                        foreach (var p in GameData.Instance.AllPlayers)
                        {
                            if (p == null || p.Object == null)
                            {
                                continue;
                            }
                            if (p.PlayerId == MyPlayer.PlayerId)
                            {
                                continue;
                            }
                            if (p.Object.ToNebulaPlayer() == null)
                            {
                                continue;
                            }
                            if (p.Object.ToNebulaPlayer().TryGetModifier<YandereLover.Instance>(out var m))
                            {
                                lover = p.Object;
                                break;
                            }
                            RandomSelectTarget.Add(p.Object);
                        }
                        targetPlayer = lover != null ? GamePlayer.GetPlayer(lover.PlayerId) : GamePlayer.GetPlayer(RandomSelectTarget[UnityEngine.Random.Range(0, RandomSelectTarget.Count)].PlayerId);
                        if (lover == null)
                        {
                            targetPlayer?.AddModifier(YandereLover.MyRole);
                        }
                    }
                    cooldown = AddCoolDown;
                    wrongkillnum = 0;
                    correctkillnum = 0;
                    killplayer = new List<GamePlayer>();
                    targetDic = new Dictionary<byte, float>();
                    foreach (NetworkedPlayerInfo player in GameData.Instance.AllPlayers)
                    {
                        targetDic.Add(player.PlayerId, 0f);
                    }
                    if (ActiveTargetArrow && targetPlayer != null && !targetPlayer.IsDead)
                    {
                        arrowAbility = new YTrackingTargetArrowAbility(targetPlayer, TargetArrowUpdateTime, MyRole.RoleColor.ToUnityColor()).Register<YTrackingTargetArrowAbility>(this);
                    }
                    var myTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeLocalKillablePredicate(p), null, Nebula.Roles.Impostor.Impostor.CanKillHidingPlayerOption);

                    var killButton = NebulaAPI.Modules.KillButton(this, MyPlayer, true, Virial.Compat.VirtualKeyInput.Kill,
                        BaseKillCD, "kill", ModAbilityButton.LabelType.Impostor, null!,
                        (target, _) =>
                        {
                            var cancelable = GameOperatorManager.Instance?.Run(new PlayerTryVanillaKillLocalEventAbstractPlayerEvent(MyPlayer, target));
                            if (!(cancelable?.IsCanceled ?? false))
                            {
                                //キャンセルされなければキルを実行する
                                MyPlayer.MurderPlayer(target, PlayerState.Dead, EventDetail.Kill, Virial.Game.KillParameter.NormalKill);
                            }

                            //クールダウンをリセットする
                            if (cancelable?.ResetCooldown ?? false) NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                        },
                        null,
                        _ => myTracker.CurrentTarget != null && !MyPlayer.IsDived,
                        _ => MyPlayer.AllowToShowKillButtonByAbilities
                        );
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
                    GameOperatorManager.Instance!.Subscribe<GameEndEvent>(ev =>
                        {
                            var target = NebulaGameManager.Instance?.AllPlayerInfo?.FirstOrDefault(p => p.TryGetModifier<YandereLover.Instance>(out var m));
                            if (ev.EndState.EndCondition != YandereTeamWin && ev.EndState.Winners.Test(target) && ev.EndState.Winners.Test(MyPlayer))
                            {
                                new StaticAchievementToken("yandere.another1");
                            }
                        }, NebulaAPI.CurrentGame!);
                    GameOperatorManager.Instance!.Subscribe<PlayerKillPlayerEvent>(ev =>
                    {
                        if (ev.Murderer.AmOwner)
                        {
                            if (ev.Dead.PlayerState == PlayerState.Guessed)
                            {
                                return;
                            }
                            if (!targetPlayer?.IsDead ?? false)
                            {
                                if (killplayer.Contains(ev.Dead))
                                {
                                    correctkillnum++;
                                    new StaticAchievementToken("yandere.common1");
                                }
                                else
                                {
                                    wrongkillnum++;
                                }
                            }
                            Allkillplayers.Add(ev.Dead);
                        }
                    }, this);
                }
                var ability = MyExRole?.GetAbilityOnRole(MyPlayer, AbilityAssignmentStatus.KillersSide, StoredExArgument);
                if (ability != null)
                {
                    ExAbility = ability?.Register(this);
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
    }
}
public class YandereLover : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>,HasCitation
{
    private YandereLover() : base("yandereLover", new(227, 0, 136), null,true,()=>false)
    {
    }
    Citation? HasCitation.Citation { get { return PCitations.ExtremeRoles; } }
    static public YandereLover MyRole = new YandereLover();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        bool RuntimeAssignable.CanBeAwareAssignment => NebulaGameManager.Instance!=null ? NebulaGameManager.Instance.CanSeeAllInfo:false;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (canSeeAllInfo) name += " ♥".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}