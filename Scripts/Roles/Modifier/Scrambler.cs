using Plana.Core;
using Plana.Roles.Crewmate;

namespace Plana.Roles.Modifier;

public class Scrambler : DefinedAllocatableModifierTemplate, DefinedAllocatableModifier, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, ICodeName, HasRoleFilter, HasAssignmentRoutine, ISpawnable, IAssignToCategorizedRole,HasCitation
{
    private Scrambler() : base("scrambler", "sc", new(183,31,29), [BlockSkillNum,NoActiveMeeting],false,true,false)
    {
    }
    Citation HasCitation.Citation => PCitations.PlanaANDKC;
    static IntegerConfiguration BlockSkillNum = NebulaAPI.Configurations.Configuration("options.role.scrambler.blockskillnum", (1, 15), 2);
    static IntegerConfiguration NoActiveMeeting = NebulaAPI.Configurations.Configuration("options.role.scrambler.meeting", (1, 15), 2);
    static public Scrambler MyRole = new Scrambler();
    public static void LoadPatch(Harmony harmony)
    {
        PDebug.Log("Scrambler Patch");
        harmony.Patch(typeof(ModAbilityButtonImpl).GetMethod("get_OnClick"), new HarmonyMethod(typeof(Scrambler.Instance).GetMethod("ClickPatch")));
        PDebug.Log("Done");
    }
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        public static int UseSkillnum;
        public static int ActiveMeeting;
        static RemoteProcess<ValueTuple<int, int>> SetRpc = new RemoteProcess<ValueTuple<int, int>>("ScramblerSetNumRPC", delegate (
            ValueTuple<int, int> message, bool _)
        {
            UseSkillnum = message.Item1;
            if (message.Item2 != 999)
            {
                ActiveMeeting = message.Item2;
            }
        });
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                UseSkillnum = 0;
                ActiveMeeting = 0;
                SetRpc.Invoke(new ValueTuple<int, int>(UseSkillnum, ActiveMeeting));
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " Φ".Color(MyRole.RoleColor.ToUnityColor());
        }
        void OnMeetingPreEnd(MeetingEndEvent ev)
        {
            if (AmOwner)
            {
                UseSkillnum = 0;
                ActiveMeeting++;
                SetRpc.Invoke(new ValueTuple<int, int>(UseSkillnum, ActiveMeeting));
            }
        }
        public static bool ClickPatch(ModAbilityButtonImpl __instance)
        {
            try
            {
                /* (__instance.VanillaButton.buttonLabelText.text.Contains(Language.Translate("button.label.agent")))
                {
                    return true;
                }*/
                if (!NebulaGameManager.Instance!.AllPlayerInfo.Any(r=>r.Modifiers.Any(r=>r is Scrambler.Instance)))
                {
                    return true;
                }
                var pl = PlayerControl.LocalPlayer;
                if (pl==null)
                {
                    return true;
                }
                var p = pl.ToNebulaPlayer();
                if (p==null)
                {
                    return true;
                }
                if (p.IsCrewmate&&!(p.Role.Role is Sheriff||p.Role.Role is Knight|p.Role is Agent.Instance))
                {
                    if (UseSkillnum < BlockSkillNum && ActiveMeeting < NoActiveMeeting)
                    {
                        UseSkillnum++;
                        SetRpc.Invoke(new ValueTuple<int, int>(UseSkillnum, 999));
                        __instance.StartCoolDown();
                        return false;
                    }
                }
            }
            catch(Exception ex)
            {
                PDebug.Log(ex);
            }
            return true;
        }
    }
}
