using Plana.Core;

namespace Plana.Roles.Neutral;

public class Tunny : DefinedRoleTemplate, HasCitation, DefinedRole
{
    private static Team RoleTeam = new Team("teams.tunny", Virial.Color.Yellow, TeamRevealType.OnlyMe);
    private Tunny() : base("tunny", RoleTeam.Color, RoleCategory.NeutralRole, RoleTeam, [stoptime,AfterMeetingstoptime]) { }

    Citation? HasCitation.Citation => Citations.SuperNewRoles;

    static private FloatConfiguration stoptime = NebulaAPI.Configurations.Configuration(
        "options.role.tunny.stoptime",
        (0.1f, 5f, 0.25f),
        0.1f,
        (val) => val.ToString("F1") + Language.Translate("options.sec")
    );
    static private FloatConfiguration AfterMeetingstoptime = NebulaAPI.Configurations.Configuration(
    "options.role.tunny.aftermeetingstoptime",
    (1f, 10f, 1f),
    2f,
    FloatConfigurationDecorator.Second
);


    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public Tunny MyRole = new Tunny();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        private UnityEngine.Vector2 lastPosition;
        private int kill = 0;

        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        bool meetingstart;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                lastPosition = MyPlayer.TruePosition;
                stopmovetime = -10f;
            }
        }
        float stopmovetime;
        void OnMeetingPreEnd(MeetingEndEvent ev)
        {
            stopmovetime = -AfterMeetingstoptime;
            meetingstart = true;
        }
        [Local,OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            if (ev.Player.PlayerState!=PlayerState.Suicide)
            {
                new StaticAchievementToken("tunny.common2");
            }
            if (ev.Player.PlayerState==PlayerState.Bubbled)
            {
                new StaticAchievementToken("tunny.another1");
            }
        }
        void Update(GameUpdateEvent ev)
        {
            if (MyPlayer != null && !MyPlayer.IsDead && MyPlayer.CanMove && AmOwner && !MeetingHud.Instance)
            {
                if (lastPosition == MyPlayer.TruePosition || stopmovetime < 0f)
                {
                    stopmovetime += Time.deltaTime;
                }
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            try
            {
                if (MyPlayer.ToAUPlayer() == null)
                {
                    return;
                }
                if (MyPlayer != null && !MyPlayer.IsDead && MyPlayer.CanMove && AmOwner && !MeetingHud.Instance)
                {
                    float lefttime = stoptime - stopmovetime;
                    if (lastPosition == MyPlayer.TruePosition)
                    {
                        if (stopmovetime < 0f)
                        {
                            name += (" " + Language.Translate("role.tunny.stop")).Color("yellow");
                            name += lefttime.ToString("F1").Color("red");
                            return;
                        }
                        if (stopmovetime > stoptime)
                        {
                            kill = 1;
                            MyPlayer.Suicide(PlayerState.Suicide, null, KillParameter.NormalKill, null);
                            if (!meetingstart)
                            {
                                new StaticAchievementToken("tunny.common1");
                            }
                            return;
                        }
                        name += (" " + Language.Translate("role.tunny.stop")).Color("red");
                        name += lefttime.ToString("F1").Color("red");
                    }
                    else
                    {
                        lastPosition = MyPlayer.TruePosition;
                        if (stopmovetime > 0)
                        {
                            stopmovetime = 0f;
                        }
                        if (AmOwner)
                        {
                            name += (" " + Language.Translate("role.tunny.run")).Color("green");
                        }
                    }
                }
                else
                {
                    lastPosition = MyPlayer.TruePosition;
                    if (stopmovetime > 0)
                    {
                        stopmovetime = 0f;
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }

        [OnlyMyPlayer]
        private void CheckExtraWin(PlayerCheckExtraWinEvent ev)
        {
            if (!MyPlayer.IsDead)
            {
                ev.SetWin(true);
                ev.ExtraWinMask.Add(PatchManager.tunnyExtra);
            }
        }
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.ExtraWins.Test(PatchManager.tunnyExtra))
            {
                new StaticAchievementToken("tunny.challenge1");
            }
        }
    }
}
