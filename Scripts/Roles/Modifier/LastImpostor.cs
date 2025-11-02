using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Impostor;

namespace Plana.Roles.Modifier;

public class LastImpostor : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>,HasCitation
{
    private LastImpostor() : base("lastImpostor",NebulaTeams.ImpostorTeam.Color,null,true,()=>false)
    {
    }
    bool DefinedAssignable.ShowOnHelpScreen
    {
        get
        {
            return false;
        }
    }
    Citation HasCitation.Citation => PCitations.PlanaANDKC;
    static public LastImpostor MyRole = new LastImpostor();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            GuesserSystem.OnMeetingStart(99, () => { }, false, null, true);
        }
        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            if (NebulaGameManager.Instance!.AllPlayerInfo.Count(p=>!p.IsDead&&p.IsImpostor)>1)
            {
                MyPlayer.RemoveModifier(LastImpostor.MyRole);
            }
            MyPlayer.GainAttribute(PlayerAttributes.CooldownSpeed, 0.25f, PatchManager.LastImpostorClockRatioOption, false, 100, "LI::CooldownSpeed");
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                if (MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var modifier))
                {
                    MyPlayer.RemoveModifier(GuesserModifier.MyRole);
                }
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " LI".Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}
