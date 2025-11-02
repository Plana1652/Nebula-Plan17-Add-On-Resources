namespace Plana.Roles.Modifier;

public class BaitM : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
    private BaitM()
        : base("baitM", "biM", new Virial.Color(0, 247, byte.MaxValue, byte.MaxValue), [
                ShowKillFlashOption,ReportDelayOption,ReportDelayDispersionOption,
                CanSeeVentFlashOption], true, false,false)
        {
    }
    Citation HasCitation.Citation
    {
        get
        {
            return Citations.TheOtherRoles;
        }
    }
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments)
    {
        return new BaitM.Instance(player);
    }
    IEnumerable<DefinedAssignable> DefinedAssignable.AchievementGroups => [Bait.MyRole,Bait.MyRole, MyRole];
    static BaitM()
    {
    }
    private static BoolConfiguration ShowKillFlashOption = NebulaAPI.Configurations.Configuration("options.role.bait.showKillFlash", false);
    private static FloatConfiguration ReportDelayOption = NebulaAPI.Configurations.Configuration("options.role.bait.reportDelay", new ValueTuple<float, float, float>(0f, 5f, 0.25f), 0f, FloatConfigurationDecorator.Second, null, null);
    private static FloatConfiguration ReportDelayDispersionOption = NebulaAPI.Configurations.Configuration("options.role.bait.reportDelayDispersion", new ValueTuple<float, float, float>(0f, 10f, 0.25f), 0f, FloatConfigurationDecorator.Second, null, null);
    private static BoolConfiguration CanSeeVentFlashOption = NebulaAPI.Configurations.Configuration("options.role.bait.canSeeVentFlash", false);

    public static BaitM MyRole = new BaitM();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedModifier RuntimeModifier.Modifier
        {
            get
            {
                return BaitM.MyRole;
            }
        }

        public Instance(GamePlayer player)
            : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
        }
        [Local]
        [OnlyMyPlayer]
        private void OnExiled(PlayerExiledEvent ev)
        {
            new StaticAchievementToken("bait.another1");
        }
        AchievementToken<ValueTuple<bool, bool>> acTokenChallenge;
        [Local]
        private void OnReported(ReportDeadBodyEvent ev)
        {
            Player reported = ev.Reported;
            if (reported != null && reported.AmOwner && ev.Reporter == base.MyPlayer.MyKiller && !ev.Reporter.AmOwner)
            {
                NebulaAPI.IncrementStatsEntry("stats.bait.bait", 1);
                new StaticAchievementToken("bait.common1");
                if (this.acTokenChallenge == null)
                {
                    this.acTokenChallenge = new AchievementToken<ValueTuple<bool, bool>>("bait.challenge", new ValueTuple<bool, bool>(false, true),(ValueTuple<bool, bool> val, ProgressRecord _) => val.Item1);
                }
            }
        }
        private void OnMeetingEnd(TaskPhaseRestartEvent ev)
        {
            if (acTokenChallenge == null)
            {
                return;
            }
            if (MyPlayer.MyKiller!=null&&MyPlayer.MyKiller.IsDead&&acTokenChallenge.Value.Item2)
            {
                acTokenChallenge.Value.Item1 = true;
                return;
            }
            if (this.acTokenChallenge != null)
            {
                this.acTokenChallenge.Value.Item2 = false;
            }
        }

        [Local]
        private void OnPlayerExiled(PlayerExiledEvent ev)
        {
            AchievementToken<ValueTuple<bool, bool>> achievementToken = this.acTokenChallenge;
            if (achievementToken != null && achievementToken.Value.Item2)
            {
                byte playerId = ev.Player.PlayerId;
                Player myKiller = base.MyPlayer.MyKiller;
                if (playerId == ((myKiller != null) ? myKiller.PlayerId : 255))
                {
                    this.acTokenChallenge.Value.Item1 = true;
                }
            }
        }
        /*private void CoReport(PlayerControl murderer)
        {
            if (BaitM.ShowKillFlashOption)
            {
                AmongUsUtil.PlayQuickFlash(BaitM.MyRole.RoleColor.ToUnityColor());
            }
            if (MeetingHud.Instance)
            {
                return;
            }
            murderer.CmdReportDeadBody(new List<PlayerControl>(PlayerControl.AllPlayerControls.ToArray()).FirstOrDefault((PlayerControl x) => x.PlayerId == MyPlayer.PlayerId).Data);
        }*/
        System.Collections.IEnumerator CoReport(PlayerControl murderer)
        {
            if (ShowKillFlashOption) AmongUsUtil.PlayQuickFlash(MyRole.RoleColor.ToUnityColor());
            float t = ReportDelayOption + (ReportDelayDispersionOption * (float)System.Random.Shared.NextDouble());
            yield return new WaitForSeconds(t);
            if (MeetingHud.Instance) yield break;
            murderer.CmdReportDeadBody(MyPlayer.ToAUPlayer().Data);
            NebulaAPI.IncrementStatsEntry("stats.bait.killer", 1);
        }
        [OnlyMyPlayer]
        private void BaitReportOnMurdered(PlayerMurderedEvent ev)
        {
            if (ev.Murderer.AmOwner && !base.MyPlayer.AmOwner)
            {
                NebulaManager.Instance.StartCoroutine(this.CoReport(ev.Murderer.ToAUPlayer()).WrapToIl2Cpp());
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " @".Color(MyRole.RoleColor.ToUnityColor());
        }
        [Local]
        private void OnEnterVent(PlayerVentEnterEvent ev)
        {
            if (CanSeeVentFlashOption)
            {
                UnityEngine.Color color = MyRole.RoleColor.ToUnityColor();
                color.a *= 0.3f;
                AmongUsUtil.PlayQuickFlash(color);
            }
        }

        string? RuntimeModifier.DisplayIntroBlurb => Language.Translate("role.baitModifier.blurb");
    }
}

