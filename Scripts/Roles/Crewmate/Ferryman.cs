using Nebula.Patches;
using Nebula.Roles.Scripts;
using Plana.Core;

namespace Plana.Roles.Crewmate;

public class FerryMan : DefinedSingleAbilityRoleTemplate<FerryMan.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder
{

    private FerryMan() : base("ferryman", new(158,172,193), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [CanSeeDeadFlash,CanSeeAllGhost,BeforeMeetingStartCanSeeAllGhost,CanDragDeadbody]) { }

    static private BoolConfiguration CanSeeDeadFlash = NebulaAPI.Configurations.Configuration("options.role.ferryman.canseedeadflash", true);
    static private BoolConfiguration CanSeeAllGhost= NebulaAPI.Configurations.Configuration("options.role.ferryman.canseeghost", false,()=>BeforeMeetingStartCanSeeAllGhost!=null&&!BeforeMeetingStartCanSeeAllGhost);
    static private BoolConfiguration BeforeMeetingStartCanSeeAllGhost = NebulaAPI.Configurations.Configuration("options.role.ferryman.beforemeetingstartcanseeghost", false,()=>!CanSeeAllGhost);
    static private BoolConfiguration CanDragDeadbody = NebulaAPI.Configurations.Configuration("options.role.ferryman.candragdeadbody", true);
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0));

    static public FerryMan MyRole = new FerryMan();
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {

        public Ability(GamePlayer player,bool isUsurped) : base(player,isUsurped)
        {
            if (CanDragDeadbody&&AmOwner)
            {
                Draggable draggable = new Draggable(base.MyPlayer).Register(new FunctionalLifespan(() => !base.IsDeadObject));
                draggable.OnHoldingDeadBody = d => { new StaticAchievementToken("ferryman.common1"); };
            }
        }
        List<byte> DeadPlayers = new List<byte>();
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            DeadPlayers = new List<byte>();
            PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => p.Data.IsDead).Do(pid => DeadPlayers.Add(pid.PlayerId));
        }
        [Local]
        void Update(GameHudUpdateEvent ev)
        {
            if (CanSeeAllGhost)
            {
                PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => p.Data.IsDead).Do(p =>
                {
                    p.Visible = true;
                });
            }
            else if (BeforeMeetingStartCanSeeAllGhost)
            {
                PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => !DeadPlayers.Contains(p.PlayerId)).Do(p =>
                {
                    p.Visible = true;
                });
            }
        }
        [Local]
        private void OnPlayerMurdered(PlayerMurderedEvent ev)
        {
            if (CanSeeDeadFlash)
            {
                if (MeetingHud.Instance || ExileController.Instance)
                {
                    return;
                }
                if (ev.Player.AmOwner)
                {
                    return;
                }
                if (!ev.Dead.HasAttribute(PlayerAttributes.BuskerEffect))
                {
                    AmongUsUtil.PlayFlash(MyRole.RoleColor.ToUnityColor());
                }
            }
        }
    }
}
