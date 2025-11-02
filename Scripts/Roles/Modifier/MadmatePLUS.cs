using Nebula.Patches;
using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Neutral;
using Virial.Configuration;
using Virial.Events.Game.Minimap;

namespace Plana.Roles.Modifier;

public class MadmatePLUS : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, ICodeName, HasRoleFilter, HasAssignmentRoutine, ISpawnable, IAssignToCategorizedRole
{
    Citation HasCitation.Citation => PCitations.PlanaANDKC;
    private MadmatePLUS()
        : base("madmateplus", "MDMP", Palette.ImpostorRed.ToNebulaColor(), [ModifierFilterOptionEditor,CanFixLightOption,CanFixCommsOption,CanSuicideOption,SuicideCoolDownOption,HasImpostorVisionOption,CanInvokeSabotageOption,CanUseVentOption,CanPromoteImpostorOption,
            new GroupConfiguration("options.role.madmate.group.embroil",
                    [EmbroilVotersOnExileOption,
                    LimitEmbroiledPlayersToVotersOption,
                    EmbroilDelayOption], GroupConfigurationColor.ImpostorRed, null),
            new GroupConfiguration("options.role.madmate.group.identification",[CanSeeAllImpostorOption,CanIdentifyImpostorsOptionEditor], GroupConfigurationColor.ImpostorRed)], true, false, false)
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Madmate.MyRole.ConfigurationHolder]);
        Madmate.MyRole.ConfigurationHolder?.ScheduleAddRelated(() => [ConfigurationHolder]);
    }
    public static ModifierFilter modifierFilter = NebulaAPI.Configurations.ModifierFilter("role.madmateplus.modifierFilter");
    int HasAssignmentRoutine.AssignPriority => 101;
    void HasAssignmentRoutine.TryAssign(Virial.Assignable.IRoleTable roleTable)
    {
        // 0を下回らないように、最大値の中に納まるよう値を減少させる。戻り値は余剰。
        int LessenRandomly(int[] num, int max)
        {
            //最大値が0を下回るものは無視する。
            if (max < 0) return max;

            int left = num.Sum() - max;
            if (left <= 0) return -left;

            List<int> moreThanZeroIndex = new();
            for (int i = 0; i < num.Length; i++) if (num[i] > 0) moreThanZeroIndex.Add(i);

            while (true)
            {
                if (left <= 0) return 0;

                int targetIndex = moreThanZeroIndex[System.Random.Shared.Next(moreThanZeroIndex.Count)];
                num[targetIndex]--;
                if (num[targetIndex] == 0) moreThanZeroIndex.Remove(targetIndex);

                left--;
            }
        }
        var rou = this as DefinedAllocatableModifierTemplate;
        RoleCategory[] categories = [RoleCategory.CrewmateRole, RoleCategory.ImpostorRole, RoleCategory.NeutralRole];
        var players = categories.Select(c =>
        {
            var leftplayers = roleTable.GetPlayers(c).Where(tuple => tuple.role.CanLoad(this)).Select(r => r.playerId).ToList();
            return roleTable.GetPlayersModifier().Where(tuple => leftplayers.Contains(tuple.playerId) && tuple.role.All(r => modifierFilter.Test(r))).OrderBy(_ => Guid.NewGuid()).ToArray();
        }).ToArray();
        int[] assignables = players.Select(p => p.Count()).ToArray();
        int[] num = categories.Select(c => rou.allocatorOptions.GetOptions(c)?.Assignment.Value ?? 0).ToArray();
        for (int i = 0; i < categories.Length; i++) if (num[i] > assignables[i]) num[i] = assignables[i];
        int randomMax = LessenRandomly(num, rou.allocatorOptions.MaxCount);

        if (randomMax != 0)
        {
            int[] randomNum = categories.Select(c => rou.allocatorOptions.GetOptions(c)?.CalcedRandomAssignment ?? 0).ToArray();
            for (int i = 0; i < categories.Length; i++) if (num[i] + randomNum[i] > assignables[i]) randomNum[i] = assignables[i] - num[i];

            LessenRandomly(randomNum, randomMax);
            for (int i = 0; i < num.Length; i++) num[i] += randomNum[i];
        }

        for (int i = 0; i < categories.Length; i++) for (int p = 0; p < num[i]; p++) SetModifier(roleTable, players[i][p].playerId);
    }
    private static IConfiguration ModifierFilterOptionEditor = NebulaAPI.Configurations.Configuration(()=>
    { 
        List<DefinedAllocatableModifier> assignable = new(),nonAssignable = new();
        foreach (var r in Nebula.Roles.Roles.AllAllocatableModifiers().Where(r => r != MyRole))
        {
            //ヘルプ画面に出現しない役職はスルー
            if (!r.ShowOnHelpScreen) continue;
            (modifierFilter.Test(r) ? assignable : nonAssignable).Add(r);
        }

        int allAssignableSum = assignable.Count;
        int allNonAssignableSum = nonAssignable.Count;

        if (allAssignableSum == 0)
        {
            //出現なし
            return Language.Translate("document.canAssignTo")+":"+Language.Translate("roleFilter.none");
        }
        if (allNonAssignableSum == 0)
        {
            //割り当て不可能なし
            return Language.Translate("document.canAssignTo")+":"+Language.Translate("roleFilter.allPattern.all");
        }
        if (allAssignableSum <= 8)
        {
            //割当先が比較的少ない場合
            return Language.Translate("document.canAssignTo")+":"+string.Join(Language.Translate("roleFilter.separator"), assignable.Select(r => r.DisplayColoredName));
        }
        if (allNonAssignableSum <= 8)
        {
            //割当不可能な対象が比較的少ない場合
            string roles = string.Join(Language.Translate("roleFilter.separator"), nonAssignable.Select(r => r.DisplayColoredName));
            return Language.Translate("document.canAssignTo")+":"+Language.Translate("roleFilter.exceptPattern.all").Replace("%ROLES%", roles);
        }
        //どうしようもない場合は略称で表示
        if (allAssignableSum <= allNonAssignableSum)
        {
            return Language.Translate("document.canAssignTo")+":"+string.Join(Language.Translate("roleFilter.separator"), assignable.Select(r => r.DisplayColoredName));
        }
        else
        {
            return Language.Translate("document.canAssignTo")+":"+string.Join(Language.Translate("roleFilter.separator"), assignable.Select(r => r.DisplayColoredName));
        }
    }, ()=>
    {
        return NebulaAPI.GUI.RawButton(GUIAlignment.Center, new TextAttribute(Virial.Text.TextAlignment.Center, NebulaAPI.GUI.GetFont(FontAsset.GothicMasked), Virial.Text.FontStyle.Bold, new FontSize(1.8f, 1f, 2f, true), new Size(1.8f, 0.4f), new global::Virial.Color(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), false, null), Language.Translate("options.role.modifierFilter").Color(Palette.ImpostorRed), delegate (GUIClickable _)
        {
            Nebula.Roles.RoleOptionHelper.OpenFilterScreen<DefinedAllocatableModifier>("ModifierFilter", Nebula.Roles.Roles.AllAllocatableModifiers().Where(r => r != MyRole),
    r => modifierFilter.Test(r), (r, val) => modifierFilter.SetAndShare(r, val), r => modifierFilter.ToggleAndShare(r));
        });
    },null);
    internal static BoolConfiguration CanSeeAllImpostorOption = NebulaAPI.Configurations.Configuration("options.role.madmateplus.canSeeAllImpostor",true);
    internal static BoolConfiguration HasImpostorVisionOption = NebulaAPI.Configurations.Configuration("options.role.madmateplus.hasimpostorvision", true);
    internal static BoolConfiguration CanUseVentOption = NebulaAPI.Configurations.Configuration("options.role.madmateplus.canusevent", true);
    internal static BoolConfiguration CanInvokeSabotageOption = NebulaAPI.Configurations.Configuration("options.role.madmateplus.cansabotage", false);
    internal static BoolConfiguration CanPromoteImpostorOption = NebulaAPI.Configurations.Configuration("options.role.madmateplus.canpromote", false);
    private static readonly BoolConfiguration CanFixLightOption = NebulaAPI.Configurations.Configuration("options.role.madmate.canFixLight", false, null, null);
    private static readonly BoolConfiguration CanFixCommsOption = NebulaAPI.Configurations.Configuration("options.role.madmate.canFixComms", false, null, null);
    internal static readonly IntegerConfiguration CanIdentifyImpostorsOption = NebulaAPI.Configurations.Configuration("options.role.madmate.canIdentifyImpostors", new ValueTuple<int, int>(0, 6), 0, null, null, null);
    private static readonly BoolConfiguration EmbroilVotersOnExileOption = NebulaAPI.Configurations.Configuration("options.role.madmate.embroilPlayersOnExile", false, null, null);
    private static readonly BoolConfiguration LimitEmbroiledPlayersToVotersOption = NebulaAPI.Configurations.Configuration("options.role.madmate.limitEmbroiledPlayersToVoters", true, () => EmbroilVotersOnExileOption, null);
    private static readonly FloatConfiguration EmbroilDelayOption = NebulaAPI.Configurations.Configuration("options.role.madmate.embroilDelay", new ValueTuple<float, float, float>(0f, 5f, 1f), 0f, FloatConfigurationDecorator.TaskPhase, () => EmbroilVotersOnExileOption, null);
    private static readonly BoolConfiguration CanSuicideOption = NebulaAPI.Configurations.Configuration("options.role.madmate.canSuicide", false, null, null);
    private static readonly IRelativeCoolDownConfiguration SuicideCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.madmate.suicideCooldown", CoolDownType.Relative, new ValueTuple<float, float, float>(0f, 60f, 2.5f), 30f, new ValueTuple<float, float, float>(-40f, 40f, 2.5f), 0f, new ValueTuple<float, float, float>(0.125f, 2f, 0.125f), 1f, () => CanSuicideOption, null);
    private static readonly IOrderedSharableVariable<int>[] NumOfTasksToIdentifyImpostorsOptions = [
            NebulaAPI.Configurations.SharableVariable("numOfTasksToIdentifyImpostors0", new ValueTuple<int, int>(0, 10), 2),
            NebulaAPI.Configurations.SharableVariable("numOfTasksToIdentifyImpostors1", new ValueTuple<int, int>(0, 10), 4),
            NebulaAPI.Configurations.SharableVariable("numOfTasksToIdentifyImpostors2", new ValueTuple<int, int>(0, 10), 6),
            NebulaAPI.Configurations.SharableVariable("numOfTasksToIdentifyImpostors3", new ValueTuple<int, int>(0, 10), 7),
            NebulaAPI.Configurations.SharableVariable("numOfTasksToIdentifyImpostors4", new ValueTuple<int, int>(0, 10), 8),
            NebulaAPI.Configurations.SharableVariable("numOfTasksToIdentifyImpostors5", new ValueTuple<int, int>(0, 10), 9)];

    private static IConfiguration CanIdentifyImpostorsOptionEditor = NebulaAPI.Configurations.Configuration(() => CanIdentifyImpostorsOption.GetDisplayText() + ("(" + NumOfTasksToIdentifyImpostorsOptions.Take(CanIdentifyImpostorsOption).Join((IOrderedSharableVariable<int> option) => option.Value.ToString(), ", ") + ")").Color(UnityEngine.Color.gray), delegate
    {
        List<GUIWidget> widgets = new List<GUIWidget>([CanIdentifyImpostorsOption.GetEditor()()]);
        if (CanIdentifyImpostorsOption > 0)
        {
            int length = CanIdentifyImpostorsOption;
            for (int i = 0; i < length; i++)
            {
                IOrderedSharableVariable<int> option = NumOfTasksToIdentifyImpostorsOptions[i];
                widgets.Add(new HorizontalWidgetsHolder(GUIAlignment.Left, new GUIWidget[]
                {
                        NebulaGUIWidgetEngine.API.LocalizedText(GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.OptionsTitle), "options.role.madmate.requiredTasksForIdentifying" + i.ToString()),
                        NebulaGUIWidgetEngine.API.RawText(GUIAlignment.Left, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.OptionsFlexible), ":"),
                        NebulaGUIWidgetEngine.API.RawText(GUIAlignment.Center, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.OptionsValueShorter), option.CurrentValue.ToString()),
                        NebulaGUIWidgetEngine.API.SpinButton(GUIAlignment.Center, delegate(bool v)
                        {
                            option.ChangeValue(v, true);
                            NebulaAPI.Configurations.RequireUpdateSettingScreen();
                        })
                }));
            }
        }
        return new VerticalWidgetsHolder(GUIAlignment.Left, widgets);
    },()=>!CanSeeAllImpostorOption);
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Player player, int[] arguments)
    {
        return new MadmatePLUS.Instance(player);
    }
    public static MadmatePLUS MyRole = new MadmatePLUS();
    IEnumerable<DefinedAssignable> DefinedAssignable.AchievementGroups => [Madmate.MyRole,Madmate.MyRole, MyRole];
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedModifier RuntimeModifier.Modifier
        {
            get
            {
                return MadmatePLUS.MyRole;
            }
        }
        public Instance(Player player)
            : base(player)
        {
        }
        [Local]
        void OnSetRole(PlayerTaskTextLocalEvent ev)
        {
            var text = Language.Translate("role.madmate.Target");
            if (MyPlayer.Role.Role is Insomniacs)
            {
                text = Language.Translate("role.madmate.insomniacs");
            }
            else if (MyPlayer.Role.Role is Mayor)
            {
                text = Language.Translate("role.madmate.mayor");
            }
            else if (MyPlayer.Role.Role is LoveMesseger)
            {
                text = Language.Translate("role.madmate.lovemesseger");
            }
            else if (MyPlayer.Role.Role is Swapper)
            {
                text = Language.Translate("role.madmate.swapper");
            }
            ev.AppendText(text.Color(MyRole.RoleColor.ToUnityColor()));
        }
        [Local,OnlyMyPlayer]
        void OnExiled(PlayerExiledEvent ev)
        {
            if (NebulaGameManager.Instance.AllPlayerInfo.Any((Player p) => !p.IsDead && p.Role.Role.Category == RoleCategory.ImpostorRole))
            {
                new StaticAchievementToken("madmate.common1");
            }
            if (!EmbroilVotersOnExileOption)
            {
                return;
            }
            GamePlayer[] voters = MeetingHudExtension.LastVotedForMap
                .Where(entry => entry.Value == MyPlayer.PlayerId && entry.Key != MyPlayer.PlayerId)
                .Select(entry => NebulaGameManager.Instance!.GetPlayer(entry.Key)).ToArray()!;

            void Embroil()
            {
                NebulaAPI.IncrementStatsEntry("stats.madmate.embroil");
                ExtraExileRoleSystem.MarkExtraVictim(MyPlayer.Unbox(), false, true, LimitEmbroiledPlayersToVotersOption ? voters : []);

            }

            if (EmbroilDelayOption == 0)
                Embroil();
            else
            {
                int left = (int)(float)EmbroilDelayOption;
                GameOperatorManager.Instance?.Subscribe<MeetingPreSyncEvent>(ev => 
                {
                    left--;
                    if (left == 0) Embroil();
                }, new FunctionalLifespan(() => left > 0));
            }
        }
        [Local]
        [OnlyMyPlayer]
        private void OnMurdered(PlayerMurderedEvent ev)
        {
            Player murderer = ev.Murderer;
            if (murderer != null && murderer.Role.Role.Category == RoleCategory.ImpostorRole)
            {
                new StaticAchievementToken("madmate.another1");
                if (ev.Murderer != null && this.impostors.Contains(ev.Murderer.PlayerId))
                {
                    new StaticAchievementToken("madmate.another2");
                }
                if (ev.Murderer != null && !this.impostors.Contains(ev.Murderer.PlayerId) && this.impostors.Count > 0)
                {
                    new StaticAchievementToken("madmate.another3");
                }
            }
        }
        [Local]
        private void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.EndCondition == NebulaGameEnd.ImpostorWin)
            {
                if (NebulaGameManager.Instance.AllPlayerInfo.All((Player p) => p.Role.Role.Category != RoleCategory.ImpostorRole || !p.IsDead))
                {
                    new StaticAchievementToken("madmate.challenge");
                }
            }
        }
        void MeetingStart(MeetingStartEvent ev)
        {
            if (CanPromoteImpostorOption&&!(MyPlayer.Role is Nebula.Roles.Impostor.Impostor.Instance))
            {
                if (NebulaGameManager.Instance?.AllPlayerInfo.Count(p => p.IsImpostor && !p.IsDead && !p.IsDisconnected) <= 0)
                {
                    MyPlayer.SetRole(Nebula.Roles.Impostor.Impostor.MyRole);
                }
            }
        }
        [OnlyMyPlayer]
        private void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == NebulaGameEnd.ImpostorWin);
        }
        [OnlyMyPlayer]
        private void BlockWins(PlayerBlockWinEvent ev)
        {
            ev.IsBlocked |= (!MyPlayer.Modifiers.Any((RuntimeModifier x)=> x is HasLove) &&ev.GameEnd == NebulaGameEnd.CrewmateWin);
        }
        [Local]
        public void EditLightRange(LightRangeUpdateEvent ev)
        {
            if (HasImpostorVisionOption)
            {
                ev.LightRange *= 10;
            }
        }
        private void SetMadmateTask()
        {
            if (CanSeeAllImpostorOption||CanIdentifyImpostorsOption<=0)
            {
                MyPlayer.Tasks.Unbox().WaiveAllTasksAsOutsider();
                MyPlayer.Tasks.Unbox().ReplaceTasksAndRecompute(0, 0, 0);
                MyPlayer.Tasks.Unbox().BecomeToOutsider();
                return;
            }
            if (base.AmOwner && CanIdentifyImpostorsOption > 0)
            {
                int max = NumOfTasksToIdentifyImpostorsOptions.Take(CanIdentifyImpostorsOption).Max((IOrderedSharableVariable<int> option) => option.Value);
                using (RPCRouter.CreateSection("MadmateTask"))
                {
                    MyPlayer.Tasks.Unbox().ReplaceTasksAndRecompute(max, 0, 0);
                    MyPlayer.Tasks.Unbox().BecomeToOutsider();
                }
            }
        }
        private List<byte> impostors = new List<byte>();
        private void IdentifyImpostors()
        {
            if (CanSeeAllImpostorOption)
            {
                return;
            }
            while (CanIdentifyImpostorsOption > impostors.Count && base.MyPlayer.Tasks.CurrentCompleted >= NumOfTasksToIdentifyImpostorsOptions[this.impostors.Count].Value)
            {
                Player[] pool = NebulaGameManager.Instance.AllPlayerInfo.Where((Player p) => p.Role.Role.Category == RoleCategory.ImpostorRole && !this.impostors.Contains(p.PlayerId)).ToArray<Player>();
                if (pool.Length == 0)
                {
                    return;
                }
                if (pool.Any((Player p) => !p.IsDead))
                {
                    pool = pool.Where((Player p) => !p.IsDead).ToArray<Player>();
                }
                this.impostors.Add(pool[System.Random.Shared.Next(pool.Length)].PlayerId);
                NebulaAPI.IncrementStatsEntry("stats.madmate.foundImpostors");
                if (base.MyPlayer.Tasks.CurrentCompleted > 0)
                {
                    new StaticAchievementToken("madmate.common2");
                }
            }
        }
        public void OnGameStart(GameStartEvent ev)
        {
            SetMadmateTask();
            if (AmOwner)
            {
                IdentifyImpostors();
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            SetMadmateTask();
            if (AmOwner)
            {
                IdentifyImpostors();
                if (base.AmOwner && CanSuicideOption)
                {
                    ModAbilityButton modAbilityButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, (MyPlayer.Role.Role is Sheriff || MyPlayer.Role.Role is Knight || MyPlayer.Role.Role is Jackal||MyPlayer.Role.Role is Avenger) ? VirtualKeyInput.SecondaryAbility : VirtualKeyInput.Kill, SuicideCoolDownOption.CoolDown, "madmate.suicide", Madmate.Instance.suicideButtonSprite, null, null, false);
                    modAbilityButton.OnClick = delegate (ModAbilityButton button)
                    {
                        base.MyPlayer.Suicide(PlayerState.Suicide, null, KillParameter.RemoteKill, null);
                    };
                    modAbilityButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                }
            }
        }
        [OnlyMyPlayer]
        public void OnTaskCompleteLocal(PlayerTaskCompleteLocalEvent ev)
        {
            this.IdentifyImpostors();
        }
        [Local]
        private void DecorateOtherPlayerName(PlayerDecorateNameEvent ev)
        {
            if (CanSeeAllImpostorOption&&ev.Player.IsImpostor)
            {
                ev.Color =Palette.ImpostorRed.ToNebulaColor();
            }
            else if (impostors.Contains(ev.Player.PlayerId) && ev.Player.IsImpostor)
            {
                ev.Color = Palette.ImpostorRed.ToNebulaColor();
            }
            if (impostors.Count>=GameOptionsManager.Instance.CurrentGameOptions.GetAdjustedNumImpostorsModded(PlayerControl.AllPlayerControls.Count))
            {
                if (ev.Player.IsImpostor)
                {
                    ev.Color = Palette.ImpostorRed.ToNebulaColor();
                }
            }
        }
        string RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort)
        {
            return Language.Translate("role.madmateplus.prefix").Color(MyRole.RoleColor.ToUnityColor()) + lastRoleName;
        }
        bool RuntimeModifier.InvalidateCrewmateTask
        {
            get
            {
                return CanSeeAllImpostorOption;
            }
        }
        bool RuntimeModifier.MyCrewmateTaskIsIgnored
        {
            get
            {
                return CanSeeAllImpostorOption;
            }
        }
        void OnSheriffKill(SheriffCheckKillEvent ev)
        {
            if (ev.Player.Role.Role.LocalizedName.Contains("sheriff")&&ev.Player.IsModMadmate())
            {
                ev.CanKill = false;
            }
            else if (ev.Target.PlayerId == MyPlayer.PlayerId)
            {
                ev.CanKill = true;
            }
        }
        void OnCollactorCheckTeam(CollatorCheckTeamEvent ev)
        {
            if (ev.Target.PlayerId == MyPlayer.PlayerId)
            {
                var value= NebulaAPI.Configurations.Configuration("options.role.collator.madmateIsClassifiedAs", new string[] { "options.role.collator.madmateIsClassifiedAs.impostor", "options.role.collator.madmateIsClassifiedAs.crewmate" }, 0, null, null).GetValue();
                ev.Team = value == 0 ? NebulaTeams.ImpostorTeam : NebulaTeams.CrewmateTeam;
            }
        }
        //string? RuntimeModifier.DisplayIntroBlurb => Language.Translate("role.madmateplus.blurb");
        bool RuntimeAssignable.CanFixComm => CanFixCommsOption;
        bool RuntimeAssignable.CanFixLight => CanFixLightOption;
    }
}