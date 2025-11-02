using Nebula.Roles.Complex;
using Plana.Core;

namespace Plana.Roles.Neutral;

public class Doomsayer : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private static Team RoleTeam = new Team("teams.doomsayer", new Virial.Color(0f, 1f, 0.5f), TeamRevealType.OnlyMe);
    private Doomsayer() : base("doomsayer", new UnityEngine.Color(0f,1f,0.5f).ToNebulaColor(), RoleCategory.NeutralRole, RoleTeam, [guessCorrectNumToWin,guessCanKillPlayer,GuessShowKillAnim,PatchManager.GuesserCanGuessNoSpawnableRole,
    new GroupConfiguration("role.doomsayer.oracleGroup",[CanUseOracle,OracleCooldown,OracleGetRoleNum,GetRoleIsSpawnable,BeforeMeetingStartNoSecondSkill],new(0f,1f,0.5f))]) 
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Guesser.MyNiceRole.ConfigurationHolder,Guesser.MyEvilRole.ConfigurationHolder,GuesserModifier.MyRole.ConfigurationHolder]);
        Guesser.MyNiceRole.ConfigurationHolder?.ScheduleAddRelated(() => [ConfigurationHolder]);
        Guesser.MyEvilRole.ConfigurationHolder?.ScheduleAddRelated(() => [ConfigurationHolder]);
        GuesserModifier.MyRole.ConfigurationHolder?.ScheduleAddRelated(() => [ConfigurationHolder]);
    }
    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable)
    {
        return base.CanLoadDefaultTemplate(assignable) && !(assignable is Nebula.Roles.Complex.GuesserModifier);
    }
    Citation? HasCitation.Citation => PCitations.TownOfUs;

    static private IntegerConfiguration guessCorrectNumToWin = NebulaAPI.Configurations.Configuration("options.role.guessnum", (1, 15), 3);
    static private BoolConfiguration guessCanKillPlayer = NebulaAPI.Configurations.Configuration("options.role.guesscankillPlayer",true);
    static public BoolConfiguration GuessShowKillAnim = NebulaAPI.Configurations.Configuration("options.role.GuessKillShowAnim", true);
    static public BoolConfiguration CanUseOracle = NebulaAPI.Configurations.Configuration("options.role.canuseoracle", false);
    static public BoolConfiguration BeforeMeetingStartNoSecondSkill = NebulaAPI.Configurations.Configuration("options.role.beforemeetingstartnosecondskill", true,()=>CanUseOracle);
    static public FloatConfiguration OracleCooldown = NebulaAPI.Configurations.Configuration("options.role.oracleCooldown",(0f,60f,2.5f),20f,FloatConfigurationDecorator.Second,()=>CanUseOracle);
    static public IntegerConfiguration OracleGetRoleNum = NebulaAPI.Configurations.Configuration("options.role.OracleGetRoleNum", (1,20),8,()=>CanUseOracle);
    static public BoolConfiguration GetRoleIsSpawnable = NebulaAPI.Configurations.Configuration("options.role.getroleisspawnable", true, () => CanUseOracle);
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public Doomsayer MyRole = new Doomsayer();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        public static GameEnd DoomsayerTeamWin = NebulaAPI.Preprocessor.CreateEnd("doomsayer", MyRole.RoleColor, 47);
        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        public static Action afterGuess;
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            GameObject binder = UnityHelper.CreateObject("DoomsayerWinCheck", MeetingHud.Instance.SkipVoteButton.transform.parent, MeetingHud.Instance.SkipVoteButton.transform.localPosition, null);
            GameOperatorManager instance = GameOperatorManager.Instance;
            if (instance != null)
            {
                instance.Subscribe<GameUpdateEvent>(delegate (GameUpdateEvent ev)
                {
                    binder.gameObject.SetActive(!this.MyPlayer.IsDead && MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted);
                }, new GameObjectLifespan(binder), 100);
            }
            TextMeshPro countText = UnityEngine.Object.Instantiate<TextMeshPro>(MeetingHud.Instance.TitleText, binder.transform);
            countText.gameObject.SetActive(true);
            countText.gameObject.GetComponent<TextTranslatorTMP>().enabled = false;
            countText.alignment = TextAlignmentOptions.Center;
            countText.transform.localPosition = new UnityEngine.Vector3(2.59f, 0f);
            countText.color = Palette.White;
            countText.transform.localScale *= 0.8f;
            countText.text = Language.Translate("role.doomsayer.winleftguess") + ":" + leftguess;
            GuesserSystem.DoomsayerOnMeetingStart(delegate
            {
                leftguess--;
                countText.text = Language.Translate("role.doomsayer.winleftguess") + ":" + Mathf.Max(leftguess, 0);
                if (leftguess <= 0)
                {
                    NebulaGameManager instance3 = NebulaGameManager.Instance;
                    if (instance3 == null)
                    {
                        return;
                    }
                    new StaticAchievementToken("doomsayer.common1");
                    instance3.RpcInvokeSpecialWin(DoomsayerTeamWin, 1 << (int)this.MyPlayer.PlayerId);
                }
            },guessCanKillPlayer);
        }
        int leftguess;
        [OnlyMyPlayer]
        [Local]
        void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == DoomsayerTeamWin);
        }
        GamePlayer oracleTarget;
        List<byte> useskilltargets = new List<byte>();
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                useskilltargets = new List<byte>();
                PoolablePlayer playericon = null;
                GuesserSystem.doomsayerGuessKillPlayers = new List<GamePlayer>();
                leftguess = guessCorrectNumToWin;
                if (CanUseOracle)
                {
                    var playerTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeStandardPredicate(p)&&!useskilltargets.Contains(p.RealPlayer.PlayerId));

                    var oracleButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                        OracleCooldown, "doomsayer.oracle", Oracle.Ability.buttonSprite,
                        _ => playerTracker.CurrentTarget != null, null);
                    oracleButton.OnClick = (button) =>
                    {
                        var player = playerTracker.CurrentTarget.RealPlayer;
                        var result = player.Role.Role;
                        if (BeforeMeetingStartNoSecondSkill)
                        {
                            oracleTarget = player;
                            if (playericon != null)
                            {
                                UnityEngine.Object.Destroy(playericon);
                            }
                            playericon = (button as ModAbilityButtonImpl)?.GeneratePlayerIcon(player);
                        }
                        else
                        {
                            List<DefinedRole> list = new List<DefinedRole>();
                            var rolelist = new List<DefinedRole>(Nebula.Roles.Roles.AllRoles);
                            rolelist.Remove(Doomsayer.MyRole);
                            rolelist.Remove(Nebula.Roles.Crewmate.Crewmate.MyRole);
                            rolelist.Remove(Nebula.Roles.Impostor.Impostor.MyRole);
                            rolelist.RemoveAll(r => !r.CanBeGuess);
                            for (int i = 0; list.Count< OracleGetRoleNum - 1; i++)
                            {
                                var r = rolelist.Random();
                                if (r.Id == player.Role.Role.Id)
                                {
                                    rolelist.Remove(r);
                                    continue;
                                }
                                if (!list.Contains(r))
                                {
                                    if (!GetRoleIsSpawnable || r.IsSpawnable)
                                    {
                                        list.Add(r);
                                        rolelist.Remove(r);
                                    }
                                }
                            }
                            list.Insert(UnityEngine.Random.Range(0, list.Count), player.Role.Role);
                            var text = Language.Translate("role.doomsayer.oracletext").Replace("%NAME%", player.PlayerName).Replace("%ROLE%", string.Join(",", list.Select(r => r.DisplayName)));
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, text);
                            useskilltargets.Add(player.PlayerId);
                        }
                        button.StartCoolDown();
                    };
                    oracleButton.Visibility = (button) => !MyPlayer.IsDead;
                    GameOperatorManager.Instance.Subscribe<MeetingPreStartEvent>(ev =>
                    {
                        if (playericon!=null)
                        {
                            UnityEngine.Object.Destroy(playericon);
                        }
                        if (CanUseOracle && BeforeMeetingStartNoSecondSkill && oracleTarget != null)
                        {
                            List<DefinedRole> list = new List<DefinedRole>();
                            var rolelist = new List<DefinedRole>(Nebula.Roles.Roles.AllRoles);
                            rolelist.Remove(Doomsayer.MyRole);
                            rolelist.Remove(Nebula.Roles.Crewmate.Crewmate.MyRole);
                            rolelist.Remove(Nebula.Roles.Impostor.Impostor.MyRole);
                            rolelist.RemoveAll(r => !r.CanBeGuess);
                            for (int i = 0; list.Count < OracleGetRoleNum - 1; i++)
                            {
                                var r = rolelist.Random();
                                if (r.Id == oracleTarget.Role.Role.Id)
                                {
                                    rolelist.Remove(r);
                                    continue;
                                }    
                                if (!list.Contains(r))
                                {
                                    if (!GetRoleIsSpawnable || r.IsSpawnable)
                                    {
                                        list.Add(r);
                                        rolelist.Remove(r);
                                    }
                                }
                            }
                            list.Insert(UnityEngine.Random.Range(0, list.Count), oracleTarget.Role.Role);
                            var text = Language.Translate("role.doomsayer.oracletext").Replace("%NAME%", oracleTarget.PlayerName).Replace("%ROLE%", string.Join(",", list.Select(r => r.DisplayName)));
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, text);
                            useskilltargets.Add(oracleTarget.PlayerId);
                            oracleTarget = null;
                        }
                    }, this);
                }
            }
        }
    }
}
