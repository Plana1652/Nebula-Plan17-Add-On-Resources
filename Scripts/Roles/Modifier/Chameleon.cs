using Plana.Core;

namespace Plana.Roles.Modifier;

public class chameleon : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    private chameleon() : base("chameleon", "cha", new(130, 193, 111), [quiescentTime])
    {
    }

    Citation? HasCitation.Citation => Citations.TheOtherRoles;

    static private FloatConfiguration quiescentTime = NebulaAPI.Configurations.Configuration("options.role.chameleon.quiescentTime", (0.25f, 2f, 0.25f), 1f, FloatConfigurationDecorator.Second);

    static public chameleon MyRole = new chameleon();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        private UnityEngine.Vector2 lastPosition;
        private float lastMoveTime;

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                lastPosition = MyPlayer.TruePosition;
                lastMoveTime = Time.time;
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner && lastPosition != MyPlayer.TruePosition)
            {
                lastMoveTime = Time.time;
                lastPosition = MyPlayer.TruePosition;
            }
            if (AmOwner || canSeeAllInfo) name += " Þ".Color(MyRole.RoleColor.ToUnityColor());
            if (AmOwner && Time.time - lastMoveTime > quiescentTime)
            {
                MyPlayer.GainAttribute(PlayerAttributes.Invisible, 0.25f, false, 0, "chameleon:vanish");
            }
        }
    }
}


