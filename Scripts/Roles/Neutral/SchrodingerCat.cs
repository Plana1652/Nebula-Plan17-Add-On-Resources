using Nebula;
using Nebula.Game.Statistics;
using Nebula.Modules;
using Nebula.Roles;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Plana.Roles.Modifier;
using Plana.Roles.Neutral;
using Rewired.Demos.CustomPlatform;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Game;

public class SchrodingerCat()
    : DefinedRoleTemplate("SchrodingerCat", RoleTeam.Color, RoleCategory.NeutralRole, RoleTeam, [CanSeeKiller,CanNotKillKiller]), DefinedRole,HasCitation
{
    private static readonly Team RoleTeam = new("teams.SchrodingerCat", new Virial.Color(115, 115, 115), TeamRevealType.OnlyMe);
    public static readonly BoolConfiguration CanSeeKiller = NebulaAPI.Configurations.Configuration("options.role.schrodingerCat.canseekiller", false);
    public static readonly BoolConfiguration CanNotKillKiller = NebulaAPI.Configurations.Configuration("options.role.schrodingerCat.cannotkillkiller", false);
    Citation? HasCitation.Citation => Citations.TheOtherRolesGMH;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(Player player, int[] arguments) => new Instance(player);

    public static SchrodingerCat MyRole = new();
    public class Instance(Player player) : RuntimeAssignableTemplate(player), RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;

        void RuntimeAssignable.OnActivated() { }

        public bool hasGuard = true;
        [OnlyMyPlayer]
        void CheckKill(PlayerCheckKilledEvent ev)
        {
            //Damnedが反射するように発動することは無い
            if (ev.IsMeetingKill || ev.EventDetail == EventDetail.Curse) return;
            //自殺は考慮に入れない
            if (ev.Killer.PlayerId == MyPlayer.PlayerId) return;

            //Avengerのキルは呪いを貫通する(このあと、Avengerに呪いを起こす)
            if (ev.Killer.Role.Role == Avenger.MyRole && (ev.Killer.Role as Avenger.Instance)?.AvengerTarget == ev.Player) return;

            ev.Result = hasGuard ? KillResult.ObviousGuard : KillResult.Kill;
        }

        [OnlyMyPlayer]
        void OnGuard(PlayerGuardEvent ev)
        {
            hasGuard = false;
            if (ev.Player.AmOwner)
            {
                var nextRole = ev.Murderer.Role.Role;
                var nextArgs = ev.Murderer.Role.RoleArguments;
                using (RPCRouter.CreateSection("SchrodingerCatAction"))
                {
                    if (MyPlayer.TryGetModifier<SkinnerDog.Instance>(out var d))
                    {
                        if (nextRole is Jackal)
                        {
                            MyPlayer.SetRole(nextRole, nextArgs);
                            return;
                        }
                        var p = NebulaGameManager.Instance?.AllPlayerInfo.FirstOrDefault(p => p.Role is Jackal.Instance);
                        int[] jackalargs = Jackal.GenerateArgument(0, null);
                        if (Jackal.JackalizedImpostorOption)
                        {
                            var jackalType=AssignmentType.AllTypes.FirstOrDefault(p=>p.RelatedRole==Jackal.MyRole);
                            var jackalizedRoles = Nebula.Roles.Roles.AllRoles.Where(r => r.IsSpawnable&&(r.GetCustomAllocationParameters(jackalType)?.RoleCountSum??0)>0).ToList();
                            jackalargs = Jackal.GenerateArgument(0, jackalizedRoles.Random());
                        }
                        if (p != null)
                        {
                            jackalargs = p.Role.RoleArguments;
                        }
                        MyPlayer.SetRole(Jackal.MyRole, jackalargs);
                        return;
                    }
                    MyPlayer.AddModifier(SchrodingerCatModifier.MyRole, [ev.Murderer.PlayerId]);
                    MyPlayer.SetRole(nextRole, nextArgs);
                }
            }
        }
    }
}
public class SchrodingerCatModifier : DefinedModifierTemplate, HasCitation, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>
{
    bool DefinedAssignable.ShowOnHelpScreen => false;
    bool DefinedAssignable.ShowOnFreeplayScreen => false;
    string DefinedAssignable.InternalName => "SchrodingerCat.modifier";
    private SchrodingerCatModifier() : base("SchrodingerCat", SchrodingerCat.MyRole.RoleColor, null, false)
    {
    }
    Citation? HasCitation.Citation => Citations.TheOtherRolesGMH;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Player player, int[] arguments)
    {
        return new Instance(player,GamePlayer.GetPlayer(Convert.ToByte(arguments.Get(0,player.PlayerId))));
    }

    static public SchrodingerCatModifier MyRole = new SchrodingerCatModifier();
    IEnumerable<DefinedAssignable> DefinedAssignable.AchievementGroups => [SchrodingerCat.MyRole, SchrodingerCat.MyRole, MyRole];

    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedModifier RuntimeModifier.Modifier
        {
            get
            {
                return MyRole;
            }
        }
        GamePlayer killer;
        public Instance(GamePlayer player,GamePlayer killer) : base(player)
        {
            this.killer = killer;
        }
        bool IsSameTeam(GamePlayer p)
        {
            if (p.Role.Role.Id==MyPlayer.Role.Role.Id)
            {
                return true;
            }
            return false;
        }
        void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (SchrodingerCat.CanSeeKiller)
            {
                if (MyPlayer.AmOwner||killer.AmOwner)
                {
                    if (ev.Player.PlayerId==MyPlayer.PlayerId)
                    {
                        ev.Color = MyPlayer.Role.Role.Color;
                    }
                    else if (ev.Player.PlayerId == killer.PlayerId)
                    {
                        ev.Color = MyPlayer.Role.Role.Color;
                    }
                }
            }
        }
        private void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (SchrodingerCat.CanNotKillKiller)
            {
                if (MyPlayer.AmOwner || killer.AmOwner)
                {
                    if (ev.Target.PlayerId == MyPlayer.PlayerId || ev.Target.PlayerId == killer.PlayerId)
                    {
                        ev.SetAsCannotKillBasically();
                    }
                }
            }
        }
        void RuntimeAssignable.OnActivated()
        {
        }
}
}
