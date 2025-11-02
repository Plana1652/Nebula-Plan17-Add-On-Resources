using Plana.Core;

namespace Plana.Roles.Modifier;

public class VIP : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
private VIP(): base("vip", "vipp", new(255,159,76), [CrewmateCanSeeDead,NeutralCanSeeDead,ImpostorCanSeeDead]) {
    }
    Citation? HasCitation.Citation { get { return Citations.TheOtherRoles; } }
    public static BoolConfiguration CrewmateCanSeeDead = NebulaAPI.Configurations.Configuration("options.roles.vip.crewmatecanseedead", true);
    public static BoolConfiguration NeutralCanSeeDead = NebulaAPI.Configurations.Configuration("options.roles.vip.neutralcanseedead",false);
    public static BoolConfiguration ImpostorCanSeeDead = NebulaAPI.Configurations.Configuration("options.roles.vip.impostorcanseedead",false);
    static public VIP MyRole = new VIP();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated() {
        }
        public static RemoteProcess<GamePlayer> TellAllVIPDeadRpc = new RemoteProcess<GamePlayer>("TAVIPDRpc", delegate (GamePlayer
message, bool _)
        {
            if (GamePlayer.LocalPlayer == null)
            {
                return;
            }
            if (GamePlayer.LocalPlayer.Role.Role.Category == RoleCategory.CrewmateRole && !CrewmateCanSeeDead)
            {
                return;
            }
            if (GamePlayer.LocalPlayer.Role.Role.Category == RoleCategory.NeutralRole && !NeutralCanSeeDead)
            {
                return;
            }
            if (GamePlayer.LocalPlayer.Role.Role.Category == RoleCategory.CrewmateRole && !CrewmateCanSeeDead)
            {
                return;
            }
            UnityEngine.Object.Instantiate(GameManagerCreator.Instance.HideAndSeekManagerPrefab.DeathPopupPrefab, HudManager.Instance.transform.parent).Show(message.ToAUPlayer(), 0);
        });
        [OnlyMyPlayer]
        [Local]
        void OnDead(PlayerDieEvent ev)
        {
            if (PatchManager.CanSeeAllPlayerDead)
            {
                return;
            }
            TellAllVIPDeadRpc.Invoke(MyPlayer);
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " VIP".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}
