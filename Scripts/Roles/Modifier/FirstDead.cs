namespace Plana.Roles.Modifier;

public class firstdead : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier
{
private firstdead(): base("firstdead", "fd", Virial.Color.White) {
    }

    static public firstdead MyRole = new firstdead();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player, arguments.Get<int>(0, 1));
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable,ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;
        int meetingnum;
        int inactivemeetingnum;
        public Instance(GamePlayer player,int inactivemeetings) : base(player)
        {
            meetingnum = 0;
            inactivemeetingnum = inactivemeetings;
        }
            void RuntimeAssignable.OnActivated() {
        }
        private void OnPreMeetingStart(MeetingPreStartEvent ev)
        {
            meetingnum++;
            if (meetingnum >= inactivemeetingnum)
            {
                MyPlayer.RemoveModifier(MyRole);
            }
        }
        [OnlyMyPlayer]
        private void CheckKill(PlayerCheckKilledEvent ev)
        {
            if (ev.Killer.PlayerId == base.MyPlayer.PlayerId)
            {
                return;
            }
            if (ev.EventDetail == EventDetail.Bubbled)
            {
                NebulaManager.Instance.StartDelayAction(2f, () =>
                {
                    PatchManager.SetVisible.Invoke(MyPlayer);
                });
            }
            ev.Result =KillResult.Rejected;
        }
        void OnKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (ev.Target.RealPlayer.PlayerId == MyPlayer.PlayerId&& GamePlayer.LocalPlayer!.Role.GetAbility<Destroyer.Ability>() != null)
            {
                ev.SetAsCannotKillForcedly();
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            name += " "+Language.Translate("role.firstdead.name").Color(MyRole.RoleColor.ToUnityColor());
        }
    }
}

