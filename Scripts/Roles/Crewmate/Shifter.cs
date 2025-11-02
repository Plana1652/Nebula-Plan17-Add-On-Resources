using Plana.Core;

namespace Plana.Roles.Crewmate;

public class Shifter : DefinedRoleTemplate, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder
{

    private Shifter() : base("shifter", ChainShifter.MyTeam.Color, RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [ShiftCoolDown, CanCallEmergencyMeetingOption]) { }

    Citation? HasCitation.Citation => Citations.TheOtherRolesGM;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    static private FloatConfiguration ShiftCoolDown = NebulaAPI.Configurations.Configuration("options.role.shifter.shiftCoolDown", (5f, 60f, 5f), 15f, FloatConfigurationDecorator.Second);
    static private BoolConfiguration CanCallEmergencyMeetingOption = NebulaAPI.Configurations.Configuration("options.role.shifter.canCallEmergencyMeeting", true);

    static public Shifter MyRole = new Shifter();

    static private GameStatsEntry StatsShift = NebulaAPI.CreateStatsEntry("stats.Shifter.shift", GameStatsCategory.Roles, MyRole);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        private ModAbilityButton? NormalChainShiftButton = null;

        static private Image buttonSprite = NebulaAPI.AddonAsset.GetResource("ChainshiftButton.png").AsImage(115f);

        public Instance(GamePlayer player) : base(player)
        {
        }

        private GamePlayer? shiftTarget = null;
        private bool canExecuteShift = false;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                try
                {
                    PoolablePlayer? shiftIcon = null;

                    var playerTracker = ObjectTrackers.ForPlayer(this, null, MyPlayer, ObjectTrackers.StandardPredicate);
                    NormalChainShiftButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(VirtualKeyInput.Ability);
                    (NormalChainShiftButton as Virial.Components.ModAbilityButton).SetImage(buttonSprite);
                    NormalChainShiftButton.Availability = (button) => playerTracker.CurrentTarget != null && shiftTarget == null && MyPlayer.CanMove;
                    NormalChainShiftButton.Visibility = (button) => !base.MyPlayer.IsDead;
                    NormalChainShiftButton.CoolDownTimer = NebulaAPI.Modules.Timer(this,ShiftCoolDown).Start(null);
                    NormalChainShiftButton.SetLabel("shift");
                    NormalChainShiftButton.OnClick = (button) =>
                    {
                        shiftTarget = playerTracker.CurrentTarget;
                        shiftIcon = (NormalChainShiftButton as ModAbilityButtonImpl).GeneratePlayerIcon(shiftTarget);
                        RpcCheckShift.Invoke(new ValueTuple<GamePlayer, GamePlayer>(MyPlayer, shiftTarget));
                    };
                    (NormalChainShiftButton as ModAbilityButtonImpl).OnMeeting = delegate (ModAbilityButtonImpl button)
                    {
                        if (shiftIcon)
                        {
                            UnityEngine.Object.Destroy(shiftIcon.gameObject);
                        }
                        shiftIcon = null;
                    };
                    NormalChainShiftButton.StartCoolDown();
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            canExecuteShift = !MyPlayer.IsDead && currentTargetList.Any(entry => entry.shifter.AmOwner) && shiftTarget?.Role.Role != Shifter.MyRole;
        }
        [Local]
        void OnMeetingPreEnd(MeetingPreSyncEvent ev)
        {
            try
            {
                if (!canExecuteShift) return;
                if (shiftTarget == null) return;
                if (!(shiftTarget.ToAUPlayer())) return;
                var player = shiftTarget;
                if (player == null || player.IsDead) return;
                if (player.Role.Role.Category != RoleCategory.CrewmateRole)
                {
                    //MyPlayer.Suicide(PlayerState.Guessed, null, KillParameter.MeetingKill, null);
                    MyPlayer.ToAUPlayer().ModMarkAsExtraVictim(null, PlayerStates.Suicide, PlayerStates.Suicide);
                    new StaticAchievementToken("shifter.another1");
                    return;
                }
                var targetRole = player.Role.Role;
                new StaticAchievementToken("shifter.common1");
                if (targetRole is Sheriff)
                {
                    GameOperatorManager.Instance.Subscribe<PlayerMurderedEvent>(ev =>
                    {
                        if (ev.Murderer.AmOwner)
                        {
                            new StaticAchievementToken("shifter.challenge1");
                        }
                    }, NebulaAPI.CurrentGame);
                }
                player.SetRole(Nebula.Roles.Crewmate.Crewmate.MyRole);
                MyPlayer.SetRole(targetRole);
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }

        void OnMeetingEnd(MeetingEndEvent ev)
        {
            shiftTarget = null;
            currentTargetList.Clear();
        }
        bool RuntimeAssignable.CanCallEmergencyMeeting => CanCallEmergencyMeetingOption;
    }

    static private List<(GamePlayer shifter, GamePlayer target)> currentTargetList = [];
    static private RemoteProcess<(GamePlayer shifter, GamePlayer target)> RpcFixShift = new("NFixShift", (message, _) => currentTargetList.Add(message));
    static private RemoteProcess<(GamePlayer shifter, GamePlayer target)> RpcCheckShift = new("NCheckShift", (message, _) =>
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (currentTargetList.Any(entry => entry.target == message.target)) return;

        RpcFixShift.Invoke(message);
    });
}
