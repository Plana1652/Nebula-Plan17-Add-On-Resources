using Plana.Core;
using Plana.Roles.Neutral;

namespace Plana.Roles.Crewmate;

public class LoveMesseger : DefinedRoleTemplate, DefinedRole, HasCitation
{
    private record RegretExtraDeadInfo(GamePlayer Target) : GamePlayer.ExtraDeadInfo(PatchManager.Regret)
    {
        public override string ToStateText() => "for " + Target.PlayerName;
    }
    static private readonly RemoteProcess<(GamePlayer lm, GamePlayer target)> RpcShareExtraInfo = new("ShareExInfoLM",
        (message, _) => {
            message.lm.PlayerStateExtraInfo = new RegretExtraDeadInfo(message.target);
        }
    );
    private LoveMesseger(): base("lovemesseger", new(255, 164, 204), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [CooldownOption, TargetCrewmateDeadOption, TargetNeutralDeadOption,TargetImpostorDeadOption, SealAbilityUntilReportingDeadBodiesOption]) {
        IConfigurationHolder configurationHolder = base.ConfigurationHolder;
        if (configurationHolder != null)
        {
            configurationHolder.AddTags(new ConfigurationTag[] { ConfigurationTags.TagFunny });
        }
        base.ConfigurationHolder.Illustration = NebulaAPI.AddonAsset.GetResource("LoveMessegerOption.png").AsImage(115f);
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    internal static FloatConfiguration CooldownOption = NebulaAPI.Configurations.Configuration("options.role.lovemesseger.cooldown", (5f, 40f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    internal static BoolConfiguration TargetCrewmateDeadOption = NebulaAPI.Configurations.Configuration("options.role.lovemesseger.crewmate", false);
    internal static BoolConfiguration TargetNeutralDeadOption = NebulaAPI.Configurations.Configuration("options.role.lovemesseger.neutral", false);
    internal static BoolConfiguration TargetImpostorDeadOption = NebulaAPI.Configurations.Configuration("options.role.lovemesseger.impostor", true);
    internal static BoolConfiguration SealAbilityUntilReportingDeadBodiesOption = NebulaAPI.Configurations.Configuration("options.role.lovemesseger.beforeReportUseSkill", true);
    static public LoveMesseger MyRole = new LoveMesseger();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable,ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        static private Virial.Media.Image buttonImage = NebulaAPI.AddonAsset.GetResource("LMButton.png").AsImage(115f);
        int GenerateModifierNum = 1;
        ModAbilityButton skillButton;
        private void OnGameStarted(GameStartEvent ev)
        {
            skillButton = null;
            GenerateModifierNum = 1;
            setColorNamePlayer = null;
            selectplayer = null;
        }
        [Local]
        private void OnGameEnd(GameEndEvent ev)
        {
            PDebug.Log("GameEnd");
            if (selectplayer == null)
            {
                return;
            }
            if (ev.EndState.EndCondition == NebulaGameEnd.JesterWin && selectplayer.Role is Jester.Instance)
            {
                new StaticAchievementToken("lovemesseger.another1");
                return;
            }
            if (ev.EndState.EndCondition == NebulaGameEnd.ScarletWin && selectplayer.Role.Role.InternalName.Contains("scarlet"))
            {
                new StaticAchievementToken("lovemesseger.another3");
                return;
            }
            int alive = 0;
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if (!player.Data.IsDead)
                {
                    alive++;
                }
            }
            if (alive <= 5 && ev.EndState.EndCondition == NebulaGameEnd.CrewmateWin && selectPlayerIsJackal && !selectplayer.IsDead)
            {
                new StaticAchievementToken("lovemesseger.challenge2");
            }
        }
            [Local]
            private void DecorateSidekickColor(PlayerDecorateNameEvent ev)
            {
                if (MyPlayer != null && MyPlayer.Role is LoveMesseger.Instance)
                {
                    if (setColorNamePlayer != null && ev.Player.PlayerId == setColorNamePlayer.PlayerId)
                    {
                        ev.Color = new Virial.Color?(MyRole.RoleColor);
                    }
                    else if (ev.Player.PlayerId == MyPlayer.PlayerId)
                    {
                        ev.Color = new Virial.Color?(MyRole.RoleColor);
                    }
                }
            }
        bool selectPlayerIsJackal = false;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                selectplayer = null;
                GenerateModifierNum = 1;
                SpriteRenderer lockSprite = null;
                ObjectTracker<GamePlayer> myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer,p=> ObjectTrackers.StandardPredicate(p)&&selectplayer==null, MyRole.RoleColor.ToUnityColor(), false, false);
                skillButton = NebulaAPI.Modules.AbilityButton(this,false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability, null);
                skillButton.SetImage(buttonImage);
                skillButton.Availability = (ModAbilityButton button) => myTracker.CurrentTarget != null && this.MyPlayer.CanMove&&lockSprite==null;
                skillButton.Visibility = (ModAbilityButton button) => !this.MyPlayer.IsDead&&GenerateModifierNum>0;
                skillButton.ShowUsesIcon(4,GenerateModifierNum.ToString());
                    if (SealAbilityUntilReportingDeadBodiesOption)
                    {
                        if (!new List<PlayerControl>(PlayerControl.AllPlayerControls.ToArray()).Any((PlayerControl p) => p.Data.IsDead))
                        {
                        lockSprite = (skillButton as ModAbilityButtonImpl).VanillaButton.AddLockedOverlay();
                        }
                    }
                lockspr = lockSprite;
                this.skillButton.OnClick = delegate (ModAbilityButton button)
                {
                    try
                    {
                        var player = myTracker.CurrentTarget;
                        var targetRoleCategory = player.Role.Role.Category;
                        if ((TargetCrewmateDeadOption && targetRoleCategory == RoleCategory.CrewmateRole) ||
                            (TargetNeutralDeadOption && targetRoleCategory == RoleCategory.NeutralRole) ||
                            (TargetImpostorDeadOption && targetRoleCategory == RoleCategory.ImpostorRole))
                        {
                            new StaticAchievementToken("lovemesseger.another2");
                            MyPlayer.Suicide(PatchManager.Regret, null, KillParameter.NormalKill, null);
                            RpcShareExtraInfo.Invoke((MyPlayer, player));
                            return;
                        }
                        if (player.TryGetModifier<SkinnerDog.Instance>(out var d))
                        {
                            new StaticAchievementToken("lovemesseger.another2");
                            MyPlayer.Suicide(PatchManager.Regret, null, KillParameter.NormalKill, null);
                            RpcShareExtraInfo.Invoke((MyPlayer,player));
                            return;
                        }
                        bool targetisjackal = player.Role is Jackal.Instance;
                        if (player.Role is Skinner.Instance)
                        {
                            var dogs=NebulaGameManager.Instance!.AllPlayerInfo.Where(p =>!p.IsDead&& p.TryGetModifier<SkinnerDog.Instance>(out var d)); 
                            dogs.Do(p => p.AddModifier(HasLove.MyRole));
                            if (dogs.Any(p=>p.Role is Jackal.Instance))
                            {
                                targetisjackal = true;
                            }
                        }
                        setColorNamePlayer = player;
                        selectplayer = player;
                        new StaticAchievementToken("lovemesseger.common1");
                        if (targetisjackal)
                        {
                            try
                            {
                            il_1:;
                                new StaticAchievementToken("lovemesseger.challenge1");
                                if (NebulaGameManager.Instance.AllPlayerInfo.Count(p => !p.IsDead)<=5)
                                {
                                    selectPlayerIsJackal = true;
                                }
                                foreach (var p in NebulaGameManager.Instance.AllPlayerInfo)
                                {
                                    PDebug.Log("Processing SidekickToJackal PlayerId:"+p.PlayerId);
                                    if (p==null||p.IsDead)
                                    {
                                        continue;
                                    }
                                    using (RPCRouter.CreateSection("Sidekick"))
                                    {
                                        if (p.Role is Sidekick.Instance || p.Modifiers.FirstOrDefault((RuntimeModifier m) => m is SidekickModifier.Instance) != null)
                                        {
                                            PDebug.Log("SidekickToJackal");
                                            p.SetRole(Jackal.MyRole, (myTracker.CurrentTarget.Role as Jackal.Instance).RoleArgumentsForSidekick);
                                            p.RemoveModifier(SidekickModifier.MyRole);
                                            break;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                PDebug.Log("Sidekick To Jackal Fail Error:" + ex.ToString());
                            }
                        }
                        player.AddModifier(HasLove.MyRole, null);
                    }
                    catch (Exception e)
                    {
                        PDebug.Log("LoveMessegerGenerateModifierFailed Error:" + e.ToString());
                    }
                    GenerateModifierNum -= 1;
                    if (GenerateModifierNum > 0)
                    {
                        button.UpdateUsesIcon(GenerateModifierNum.ToString());
                    }
                    button.StartCoolDown();
                };
                skillButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, CooldownOption).SetAsAbilityTimer().Start();
                skillButton.StartCoolDown();
                this.skillButton.SetLabel("Love");
            }
        }
        public GamePlayer selectplayer,setColorNamePlayer;
        SpriteRenderer lockspr;
        private void OnMeetingStart(MeetingStartEvent ev)
        {
            if (lockspr)
            {
                    UnityEngine.Object.Destroy(lockspr.gameObject);
                    lockspr = null;
            }
        }
        private void OnPreMeetingStart(MeetingPreStartEvent ev)
        {
        }
        void OnMeetingPreEnd(MeetingEndEvent ev)
        {
        }
    }
}
[NebulaRPCHolder]
public class HasLove : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    private HasLove() : base("haslove", new(255, 164, 204),null,true,()=>false)
    {
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public HasLove MyRole = new HasLove();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        public Instance(GamePlayer player) : base(player)
        {
        }
        [OnlyMyPlayer]
        private void BlockWins(PlayerBlockWinEvent ev)
        {
            ev.IsBlocked = ev.GameEnd != NebulaGameEnd.CrewmateWin;
        }
        [OnlyMyPlayer]
        private void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.IsWin = ev.GameEnd == NebulaGameEnd.CrewmateWin;
        }
        void RuntimeAssignable.OnActivated()
        {
            if (base.AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor.ToUnityColor(), 0f, 0.25f, 0.4f, 0f);
            }
        }
        [Local]
        private void DecorateHasLoveColor(PlayerDecorateNameEvent ev)
        {
            if (MyPlayer!=null&&MyPlayer.Modifiers.Any((RuntimeModifier m) => m is HasLove.Instance))
            {
                if (ev.Player != null && ev.Player.Role is LoveMesseger.Instance)
                {
                    ev.Color = new Virial.Color?(MyRole.RoleColor);
                }
                if (ev.Player != null && ev.Player.PlayerId == MyPlayer.PlayerId)
                {
                    ev.Color = new Virial.Color?(MyRole.RoleColor);
                }
            }
        }
        string RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort)
        {
            return Language.Translate("role.lovemesseger.prefix").Color(MyRole.RoleColor.ToUnityColor())+lastRoleName;
        }
    }
}

