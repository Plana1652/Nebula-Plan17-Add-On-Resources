using Il2CppSystem.Net.NetworkInformation;
using Nebula.Roles.Impostor;
using Nebula.VoiceChat;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Modifier;
using Virial.Helpers;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Plana.Roles.Neutral;
[NebulaRPCHolder]
public class Moriarty : DefinedRoleTemplate, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder
{
    public static readonly Team MyTeam = new Team("teams.moriarty", new(106, 252, 45), TeamRevealType.OnlyMe);
    private Moriarty(): base("moriarty", new(106, 252, 45), RoleCategory.NeutralRole, MyTeam, [SidekickMaxNum,SidekickCooldown,CanSuicide,SuicideCooldown,CanWinByKillHolmes,MoriartizedRoleOption],true,true,null,null,()=>
    {
        if (CanWinByKillHolmes)
        {
            return [new AllocationParameters.ExtraAssignmentInfo((DefinedRole _, int playerId) => new ValueTuple<DefinedRole, int[]>(Holmes.MyRole, new int[] { 0, playerId
}), RoleCategory.CrewmateRole)];
        }
        return Array.Empty<AllocationParameters.ExtraAssignmentInfo>();
    }) 
    {
        ConfigurationHolder?.ScheduleAddRelated(() => [Moran.MyRole.ConfigurationHolder,Holmes.MyRole.ConfigurationHolder]);
    }
    DefinedRole[] DefinedRole.AdditionalRoles
    {
        get
        {
            if (CanWinByKillHolmes)
            {
                return [Holmes.MyRole];
            }
            return Array.Empty<DefinedRole>();
        }
    }
    bool DefinedRole.IsKiller => true;
    Citation? HasCitation.Citation { get { return PCitations.LTS; } }
    static IntegerConfiguration SidekickMaxNum = NebulaAPI.Configurations.Configuration("options.role.moriarty.sidekickmaxnum", (1, 15), 1);
    static FloatConfiguration SidekickCooldown = NebulaAPI.Configurations.Configuration("options.role.moriarty.sidekickCooldown", (0f,60f,2.5f), 15f,FloatConfigurationDecorator.Second);
    static FloatConfiguration SuicideCooldown = NebulaAPI.Configurations.Configuration("options.role.moriarty.suicideCooldown", (0f, 60f, 2.5f), 0f,FloatConfigurationDecorator.Second);
    static BoolConfiguration CanSuicide = NebulaAPI.Configurations.Configuration("options.role.moriarty.Cansuicide", true);
    internal static BoolConfiguration CanWinByKillHolmes = NebulaAPI.Configurations.Configuration("options.role.moriarty.canwinbyholmes", true);
    public static BoolConfiguration MoriartizedRoleOption = NebulaAPI.Configurations.Configuration("options.role.moriarty.moriartizedRole",false);
    static public Moriarty MyRole = new Moriarty();
    public static int[] GenerateArgument(DefinedRole role)
    {
        return new int[] { SidekickMaxNum,(role != null) ? role.Id : (-1) };
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments)
    {
        return new Instance(player, arguments.Get(0,SidekickMaxNum),Nebula.Roles.Roles.GetRole(arguments.Get(1, -1)), arguments.Skip(2).ToArray());
    }
    IUsurpableAbility? DefinedRole.GetUsurpedAbility(Virial.Game.Player player, int[] arguments)
    {
        var role = Nebula.Roles.Roles.GetRole(arguments.Get(1, -1));
        var ability = role?.GetUsurpedAbility(player, arguments.Skip(2).ToArray());
        if (ability != null) return new UsurpedMoriartyAbility(player, role!, ability);
        return null;
    }
    private class UsurpedMoriartyAbility : FlexibleLifespan, IUsurpableAbility
    {
        public DefinedRole Role => moriartyRole;
        private DefinedRole moriartyRole;
        private IUsurpableAbility moriartyAbility;
        public IUsurpableAbility Ability => moriartyAbility;
        bool IUsurpableAbility.IsUsurped => moriartyAbility.IsUsurped;
        bool IUsurpableAbility.Usurp() => moriartyAbility.Usurp();
        public GamePlayer MyPlayer { get; private init; }
        public bool AmOwner => MyPlayer.AmOwner;

        public UsurpedMoriartyAbility(GamePlayer player, DefinedRole role, IUsurpableAbility ability)
        {
            this.MyPlayer = player;
            moriartyRole = role;
            moriartyAbility = ability.Register(this);
        }

        IEnumerable<IPlayerAbility> IPlayerAbility.SubAbilities => [moriartyAbility];
        int[] IPlayerAbility.AbilityArguments => [moriartyRole.Id, .. moriartyAbility.AbilityArguments];
    }
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        int[] MoriartyArguments => [sidekicknum, MyMoriartized?.Id ?? -1];
        int[]? RuntimeAssignable.RoleArguments => MoriartyArguments.Concat(MoriartizedAbility?.AbilityArguments ?? []).ToArray();
        int[]? RuntimeRole.UsurpedAbilityArguments => (MoriartizedAbility?.AbilityArguments ?? []).Prepend(MyMoriartized?.Id ?? -1).ToArray();
        public DefinedRole? MyMoriartized { get; set; }
        public IPlayerAbility? MoriartizedAbility { get; private set; } = null;
        private int[] StoredMoriartizedArgument { get; set; }
        IEnumerable<DefinedAssignable> RuntimeAssignable.AssignableOnHelp => MyMoriartized != null ? [MyRole, MyMoriartized] : [MyRole];
        string RuntimeRole.DisplayIntroRoleName => (MyMoriartized ?? MyRole).DisplayName;
        string RuntimeAssignable.DisplayColoredName => (this as RuntimeAssignable).DisplayName.Color(MyTeam.UnityColor);
        string RuntimeRole.DisplayShort => MyMoriartized?.GetDisplayShort(MoriartizedAbility!) ?? (MyRole as DefinedRole).DisplayShort;
        string RuntimeAssignable.DisplayName
        {
            get
            {
                var name = MyMoriartized?.GetDisplayName(MoriartizedAbility!) ?? (MyRole as DefinedRole).DisplayName;
                return name;
            }
        }
        IEnumerable<IPlayerAbility?> RuntimeAssignable.MyAbilities => MoriartizedAbility != null ? [MoriartizedAbility, .. MoriartizedAbility.SubAbilities] : [];
        DefinedRole RuntimeRole.Role=>MyRole;
        ModAbilityButton killButton, sidekickButton;
        public Instance(GamePlayer player,int sidekicknum,DefinedRole role, int[] moriartizedArguments):base(player)
        {
            this.sidekicknum = sidekicknum;
            MyMoriartized = role;
            StoredMoriartizedArgument = moriartizedArguments;
        }
        void RuntimeRole.Usurp()
        {
            (MoriartizedAbility as IUsurpableAbility)?.Usurp();
        }
        int sidekicknum;
        static bool IsMySidekick(GamePlayer player)
        {
            return player.Role.GetAbility<Moran.Ability>()!=null||player.Modifiers.Any(m=>m is MoranModifier.Instance);
        }
        private void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (IsSameTeam(GamePlayer.LocalPlayer)&&IsSameTeam(ev.Player))
            {
                ev.Color = MyRole.RoleColor;
            }
        }

        public static bool IsSameTeam(GamePlayer player)
        {
            if (IsMySidekick(player))
            {
                return true;
            }
            return player.Role is Moriarty.Instance;
    }
        public static GameEnd MoriartyTeamWin = NebulaAPI.Preprocessor!.CreateEnd("moriarty", MyRole.RoleColor, 72);
        void CheckWin(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == MoriartyTeamWin && IsSameTeam(ev.Player));
        }
        [Local]
        void OnDead(PlayerMurderedEvent ev)
        {
            if (ev.Player.Role.Role is Holmes)
            {
                if (ev.Murderer.Role is Moriarty.Instance||ev.Murderer.Role.Role is Moran||ev.Murderer.TryGetModifier<MoranModifier.Instance>(out var m))
                {
                    new StaticAchievementToken("moriarty.common2");
                }
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                ObjectTracker<IPlayerlike> myTracker = ObjectTrackers.ForPlayerlike(this, null, base.MyPlayer, (IPlayerlike p) => ObjectTrackers.PlayerlikeLocalKillablePredicate(p) && !IsMySidekick(p.RealPlayer), MyRole.RoleColor.ToUnityColor(), false, false);
                sidekickButton = NebulaAPI.Modules.InteractButton<IPlayerlike>(this, base.MyPlayer, myTracker, new PlayerInteractParameter(true, false, true), false, true, VirtualKeyInput.SidekickAction, null, SidekickCooldown, "sidekick", Jackal.Instance.sidekickButtonSprite, delegate (IPlayerlike p, ModAbilityButton button)
                {
                    sidekicknum--;
                    button.UpdateUsesIcon(sidekicknum.ToString());
                    new StaticAchievementToken("moriarty.common1");
                    if (myTracker.CurrentTarget.RealPlayer.Role.Role is Holmes)
                    {
                        new StaticAchievementToken("moriarty.another1");
                    }
                    if (Moran.IsModifierOption)
                    {
                        IPlayerlike currentTarget = myTracker.CurrentTarget;
                        if (currentTarget != null)
                        {
                            currentTarget.RealPlayer.AddModifier(MoranModifier.MyRole);
                        }
                    }
                    else
                    {
                        IPlayerlike currentTarget2 = myTracker.CurrentTarget;
                        if (currentTarget2 != null)
                        {
                            currentTarget2.RealPlayer.SetRole(Moran.MyRole);
                        }
                    }
                    if (sidekicknum <= 0)
                    {
                        button.HideUsesIcon();
                    }
                }, (ModAbilityButton _) => true, (ModAbilityButton _) => sidekicknum > 0, false);
                sidekickButton.ShowUsesIcon(3, sidekicknum.ToString());
                if (CanSuicide)
                {
                    killButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, true, false, VirtualKeyInput.Kill, null, SuicideCooldown, "suicide", SpectreImmoralist.Instance.suicideButtonSprite, (ModAbilityButton _) => myTracker.CurrentTarget != null && !this.MyPlayer.IsDived, (ModAbilityButton _) => base.MyPlayer.AllowToShowKillButtonByAbilities, false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                    killButton.OnClick = delegate (ModAbilityButton button)
                    {
                        if (!NebulaGameManager.Instance.AllPlayerInfo.Any(p => !p.IsDead && p.Role.Role is Moran || p.TryGetModifier<MoranModifier.Instance>(out var m)))
                        {
                            new StaticAchievementToken("moriarty.challenge1");
                        }
                        MyPlayer.MurderPlayer(myTracker.CurrentTarget, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill,KillCondition.TargetAlive, null);
                        MyPlayer.Suicide(PlayerStates.Suicide, null, KillParameter.NormalKill);
                    };
                }
            }
            MoriartizedAbility = MyMoriartized?.GetAbilityOnRole(MyPlayer,AbilityAssignmentStatus.CanLoadToMadmate,StoredMoriartizedArgument)?.Register(this);
        }
    }
}
[NebulaRPCHolder]
public class Moran : DefinedSingleAbilityRoleTemplate<Moran.Ability>, HasCitation, DefinedRole
{
    private Moran() : base("moran", Moriarty.MyTeam.Color, RoleCategory.NeutralRole, Moriarty.MyTeam, [IsModifierOption,CanWinAsOriginalTeamOption,SnipeCoolDownOption, ShotSizeOption, ShotEffectiveRangeOption, ShotNoticeRangeOption, StoreRifleOnFireOption, StoreRifleOnUsingUtilityOption, CanSeeRifleInShadowOption, CanKillHidingPlayerOption, AimAssistOption, DelayInAimAssistOption,PatchManager.SniperCanLockMini],false,true,()=>((ISpawnable)Moriarty.MyRole).IsSpawnable)
    {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagFunny, ConfigurationTags.TagDifficult);
        ConfigurationHolder?.ScheduleAddRelated(() => [Moriarty.MyRole.ConfigurationHolder, Holmes.MyRole.ConfigurationHolder]);
    }
    bool DefinedRole.IsKiller => true;
    string DefinedAssignable.InternalName
    {
        get
        {
            return "moriarty.moran";
        }
    }
    Citation? HasCitation.Citation => PCitations.LTS;
    bool ISpawnable.IsSpawnable
    {
        get
        {
            return ((ISpawnable)Moriarty.MyRole).IsSpawnable && !IsModifierOption;
        }
    }


    static public IRelativeCoolDownConfiguration SnipeCoolDownOption = NebulaAPI.Configurations.KillConfiguration("options.role.moran.snipeCoolDown", CoolDownType.Immediate, (0f, 60f, 2.5f), 20f, (-40f, 40f, 2.5f), -10f, (0.125f, 2f, 0.125f), 1f);
    static public FloatConfiguration ShotSizeOption = NebulaAPI.Configurations.Configuration("options.role.moran.shotSize", (0.25f, 4f, 0.25f), 1f, FloatConfigurationDecorator.Ratio);
    static public FloatConfiguration ShotEffectiveRangeOption = NebulaAPI.Configurations.Configuration("options.role.moran.shotEffectiveRange", (2.5f, 50f, 2.5f), 25f, FloatConfigurationDecorator.Ratio);
    static public FloatConfiguration ShotNoticeRangeOption = NebulaAPI.Configurations.Configuration("options.role.moran.shotNoticeRange", (2.5f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Ratio);
    static public BoolConfiguration StoreRifleOnFireOption = NebulaAPI.Configurations.Configuration("options.role.moran.storeRifleOnFire", true);
    static public BoolConfiguration StoreRifleOnUsingUtilityOption = NebulaAPI.Configurations.Configuration("options.role.moran.storeRifleOnUsingUtility", false);
    static public BoolConfiguration CanSeeRifleInShadowOption = NebulaAPI.Configurations.Configuration("options.role.moran.canSeeRifleInShadow", false);
    static public BoolConfiguration CanKillHidingPlayerOption = NebulaAPI.Configurations.Configuration("options.role.moran.canKillHidingPlayer", false);
    static public BoolConfiguration AimAssistOption = NebulaAPI.Configurations.Configuration("options.role.moran.aimAssist", false);
    static public FloatConfiguration DelayInAimAssistOption = NebulaAPI.Configurations.Configuration("options.role.moran.delayInAimAssistActivation", (0f, 20f, 1f), 3f, FloatConfigurationDecorator.Second, () => AimAssistOption);
    internal static BoolConfiguration IsModifierOption = NebulaAPI.Configurations.Configuration("options.role.moran.isModifier", false);
    internal static BoolConfiguration CanWinAsOriginalTeamOption = NebulaAPI.Configurations.Configuration("options.role.moran.canWinAsOriginalTeam",false, () => IsModifierOption, null);

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, AmongUsUtil.VanillaKillCoolDown);
    static public Moran MyRole = new Moran();
    [NebulaRPCHolder]
    public class SniperRifle : EquipableAbility, IGameOperator
    {
        private static SpriteLoader rifleSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperRifle.png", 100f);
        public SniperRifle(GamePlayer owner) : base(owner, CanSeeRifleInShadowOption, "SniperRifle")
        {
            Renderer.sprite = rifleSprite.GetSprite();
        }

        public IPlayerlike? GetTarget(float width, float maxLength)
        {
            float minLength = maxLength;
            IPlayerlike? result = null;

            foreach (var p in GamePlayer.AllPlayerlikes)
            {
                if (p.IsDead || p.AmOwner || ((!CanKillHidingPlayerOption) && p.Logic.InVent || p.IsDived)) continue;

                if (!PatchManager.SniperCanLockMini&&p.RealPlayer.TryGetModifier<Mini.Instance>(out Mini.Instance mini) && mini.IsMini())
                {
                    continue;
                }

                //吹っ飛ばされているプレイヤーは無視しない

                //不可視なプレイヤーは無視
                if (p.IsInvisible || p.WillDie) continue;

                var pos = p.TruePosition.ToUnityVector();
                Vector2 diff = pos - (Vector2)Renderer.transform.position;

                //移動と回転を施したベクトル
                var vec = diff.Rotate(-Renderer.transform.eulerAngles.z);

                if (vec.x > 0 && vec.x < minLength && Mathf.Abs(vec.y) < width * 0.5f)
                {
                    result = p;
                    minLength = vec.x;
                }
            }

            return result;
        }
    }

    [NebulaRPCHolder]
    public class Ability : AbstractPlayerAbility, IPlayerAbility
    {


        static private Image buttonSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.Buttons.SnipeButton.png", 115f);
        static private Image aimAssistSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperGuide.png", 100f);

        public SniperRifle? MyRifle = null;
        bool IPlayerAbility.HideKillButton => !(equipButton?.IsBroken ?? false);

        void BlockTriggerEnd(EndCriteriaPreMetEvent ev)
        {
            if (ev.GameEnd != NebulaGameEnd.LoversWin &&ev.GameEnd!=Moriarty.Instance.MoriartyTeamWin&& !MyPlayer.IsDead && ev.EndReason == GameEndReason.Situation)
            {
                ev.Reject();
            }
        }
        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            if (MyRifle != null && StoreRifleOnUsingUtilityOption)
            {
                var p = MyPlayer.ToAUPlayer();
                if (p.onLadder || p.inMovingPlat || p.inVent) RpcEquip.Invoke((MyPlayer.PlayerId, false));
            }
        }
        ModAbilityButton equipButton = null!;
        public Ability(GamePlayer player, float defaultCooldown) : base(player)
        {
            if (AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.UnityColor, 0f, 0.25f, 0.4f, 0f);
                equipButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, "sniper.equip",
                    0f, "equip", buttonSprite);
                equipButton.OnClick = (button) =>
                {
                    if (MyRifle == null)
                    {
                        NebulaGameManager.Instance?.RpcDoGameAction(MyPlayer, MyPlayer.Position, GameActionTypes.SniperEquippingAction);
                        NebulaAsset.PlaySE(NebulaAudioClip.SniperEquip, true);
                        equipButton.SetLabel("unequip");
                    }
                    else
                        equipButton.SetLabel("equip");

                    RpcEquip.Invoke((MyPlayer.PlayerId, MyRifle == null));

                    if (MyRifle != null)
                    {
                        var circle = EffectCircle.SpawnEffectCircle(PlayerControl.LocalPlayer.transform, Vector3.zero, Palette.ImpostorRed, ShotNoticeRangeOption, null, true);
                        var script = circle.gameObject.AddComponent<ScriptBehaviour>();
                        script.UpdateHandler += () =>
                        {
                            if (MyRifle == null) circle.Disappear();
                        };
                        this.BindGameObject(circle.gameObject);
                    }
                };
                equipButton.OnBroken = (button) =>
                {
                    if (MyRifle != null)
                    {
                        equipButton.SetLabel("equip");
                        RpcEquip.Invoke((MyPlayer.PlayerId, false));
                    }
                    Snatcher.RewindKillCooldown();
                };
                GameOperatorManager.Instance?.Subscribe<MeetingStartEvent>(ev => equipButton.SetLabel("equip"), this);

                var killButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, true, false, VirtualKeyInput.Kill, "sniper.kill",
                    SnipeCoolDownOption.GetCoolDown(MyPlayer.TeamKillCooldown), "snipe", null,
                    _ => MyRifle != null, _ => !equipButton.IsBroken)
                    .SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor)
                    .SetAsMouseClickButton();
                killButton.OnClick = (button) =>
                {
                    NebulaAsset.PlaySE(NebulaAudioClip.SniperShot, true);
                    var target = MyRifle?.GetTarget(ShotSizeOption, ShotEffectiveRangeOption);
                    if (target != null && !(GameOperatorManager.Instance?.Run(new PlayerInteractPlayerLocalEvent(MyPlayer, target, new(IsKillInteraction: true))).IsCanceled ?? false))
                    {
                        bool isBlown = target.IsBlown;
                        MyPlayer.MurderPlayer(target, PlayerState.Sniped, EventDetail.Kill, KillParameter.RemoteKill,null);
                        new StaticAchievementToken("moran.common1");
                        if (target.RealPlayer.Role.Role is Holmes)
                        {
                            new StaticAchievementToken("moran.common2");
                            if (MyPlayer.IsInvisible)
                            {
                                new StaticAchievementToken("moran.another1");
                            }
                            var ability = target.RealPlayer.Role.GetAbility<Holmes.Ability>();
                            if (ability!=null)
                            {
                                if (ability.divideResults.ContainsKey(MyPlayer.PlayerId))
                                {
                                    new StaticAchievementToken("moran.challenge1");
                                }
                            }
                        }
                    }
                    else
                    {
                        NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Missed, MyPlayer.ToAUPlayer(), 0);
                    }
                    Sniper.RpcShowNotice.Invoke(MyPlayer.Position);

                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();

                    if (StoreRifleOnFireOption) RpcEquip.Invoke((MyPlayer.PlayerId, false));

                };
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }

        [Local]
        [OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            if (MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));
        }

        System.Collections.IEnumerator CoShowAimAssist()
        {
            System.Collections.IEnumerator CoUpdateAimAssistArrow(PlayerControl player)
            {
                DeadBody? deadBody = null;
                Vector2 pos = Vector2.zero;
                Vector2 dir = Vector2.zero;
                Vector2 tempDir = Vector2.zero;
                bool isFirst = true;

                UnityEngine.Color targetColor = new UnityEngine.Color(55f / 225f, 1f, 0f);
                float t = 0f;

                SpriteRenderer? renderer = null;

                while (true)
                {
                    if (MeetingHud.Instance || MyPlayer.IsDead || MyRifle == null || IsDeadObject) break;

                    if (player.Data.IsDead && !deadBody) deadBody = Helpers.GetDeadBody(player.PlayerId);

                    //死亡して、死体も存在しなければ追跡を終了
                    if (player.Data.IsDead && !deadBody) break;

                    if (renderer == null)
                    {
                        renderer = UnityHelper.CreateObject<SpriteRenderer>("AimAssist", HudManager.Instance.transform, Vector3.zero);
                        renderer.sprite = aimAssistSprite.GetSprite();
                    }

                    pos = player.Data.IsDead ? deadBody!.transform.position : player.transform.position;
                    tempDir = (pos - (Vector2)PlayerControl.LocalPlayer.transform.position).normalized;

                    NebulaGameManager.Instance!.WideCamera.CheckPlayerState(out var localScale, out var localRotateZ);
                    tempDir.x *= localScale.x;
                    tempDir.y *= localScale.y;

                    if (isFirst)
                    {
                        dir = tempDir;
                        isFirst = false;
                    }
                    else
                    {
                        dir = (tempDir + dir).normalized;
                    }

                    float angle = Mathf.Atan2(dir.y, dir.x) + localRotateZ.DegToRad();
                    renderer.transform.eulerAngles = new Vector3(0, 0, angle.RadToDeg());
                    renderer.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2f, Mathf.Sin(angle) * 2f, -30f);

                    t += Time.deltaTime / 0.8f;
                    if (t > 1f) t = 1f;
                    renderer.color = UnityEngine.Color.Lerp(UnityEngine.Color.white, targetColor, t).AlphaMultiplied(0.6f);

                    yield return null;
                }

                if (renderer == null) yield break;

                float a = 0.6f;
                while (a > 0f)
                {
                    a -= Time.deltaTime / 0.8f;
                    var color = renderer.color;
                    color.a = a;
                    renderer.color = color;
                    yield return null;
                }

                GameObject.Destroy(renderer.gameObject);
            }

            yield return new WaitForSeconds(DelayInAimAssistOption);

            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (!p.AmOwner) NebulaManager.Instance.StartCoroutine(CoUpdateAimAssistArrow(p).WrapToIl2Cpp());
            }
        }

        void EquipRifle()
        {
            MyRifle = new SniperRifle(MyPlayer).Register(this);

            if (AmOwner && AimAssistOption) NebulaManager.Instance.StartCoroutine(CoShowAimAssist().WrapToIl2Cpp());
        }

        void UnequipRifle()
        {
            if (MyRifle != null) MyRifle.Release();
            MyRifle = null;
        }

        static RemoteProcess<(byte playerId, bool equip)> RpcEquip = new(
        "EquipRifleMoran",
        (message, _) =>
        {
            var role = NebulaGameManager.Instance?.GetPlayer(message.playerId)?.Role;
            var sniper = role.GetAbility<Ability>();
            if (sniper != null)
            {
                if (message.equip)
                    sniper.EquipRifle();
                else
                    sniper.UnequipRifle();
            }
        }
        );
    }

    private static SpriteLoader snipeNoticeSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperRifleArrow.png", 200f);
    public static RemoteProcess<Vector2> RpcShowNotice = new(
        "ShowSnipeNoticeMoran",
        (message, _) =>
        {
            if ((message - (Vector2)PlayerControl.LocalPlayer.transform.position).magnitude < ShotNoticeRangeOption)
            {
                var arrow = new Arrow(snipeNoticeSprite.GetSprite(), false) { IsSmallenNearPlayer = false, IsAffectedByComms = false, FixedAngle = true, OnJustPoint = true };
                arrow.Register(arrow);
                arrow.TargetPos = message;
                NebulaManager.Instance.StartCoroutine(arrow.CoWaitAndDisappear(3f).WrapToIl2Cpp());
            }
        }
        );
}
[NebulaRPCHolder]
public class MoranModifier : DefinedModifierTemplate, HasCitation, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>
{
    private MoranModifier() : base("moran", Moriarty.MyTeam.Color, null, false)
    {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagFunny, ConfigurationTags.TagDifficult);
    }
    Citation? HasCitation.Citation => PCitations.LTS;
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(Player player, int[] arguments)
    {
        return new Instance(player);
    }

    static public MoranModifier MyRole = new MoranModifier();
    IEnumerable<DefinedAssignable> DefinedAssignable.AchievementGroups => [Moran.MyRole, Moran.MyRole, MyRole];
    [NebulaRPCHolder]
    public class SniperRifle : EquipableAbility, IGameOperator
    {
        private static SpriteLoader rifleSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperRifle.png", 100f);
        public SniperRifle(GamePlayer owner) : base(owner, Moran.CanSeeRifleInShadowOption, "SniperRifle")
        {
            Renderer.sprite = rifleSprite.GetSprite();
        }

        public IPlayerlike? GetTarget(float width, float maxLength)
        {
            float minLength = maxLength;
            IPlayerlike? result = null;

            foreach (var p in GamePlayer.AllPlayerlikes)
            {
                if (p.IsDead || p.AmOwner || ((!Moran.CanKillHidingPlayerOption) && p.Logic.InVent || p.IsDived)) continue;

                if (!PatchManager.SniperCanLockMini && p.RealPlayer.TryGetModifier<Mini.Instance>(out Mini.Instance mini) && mini.IsMini())
                {
                    continue;
                }

                //吹っ飛ばされているプレイヤーは無視しない

                //不可視なプレイヤーは無視
                if (p.IsInvisible || p.WillDie) continue;

                var pos = p.TruePosition.ToUnityVector();
                Vector2 diff = pos - (Vector2)Renderer.transform.position;

                //移動と回転を施したベクトル
                var vec = diff.Rotate(-Renderer.transform.eulerAngles.z);

                if (vec.x > 0 && vec.x < minLength && Mathf.Abs(vec.y) < width * 0.5f)
                {
                    result = p;
                    minLength = vec.x;
                }
            }

            return result;
        }
    }

    [NebulaRPCHolder]
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedModifier RuntimeModifier.Modifier
        {
            get
            {
                return MoranModifier.MyRole;
            }
        }


        static private Image buttonSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.Buttons.SnipeButton.png", 115f);
        static private Image aimAssistSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperGuide.png", 100f);
        public SniperRifle? MyRifle = null;
        void BlockTriggerEnd(EndCriteriaPreMetEvent ev)
        {
            if (ev.GameEnd!=NebulaGameEnd.LoversWin&&ev.GameEnd!=Moriarty.Instance.MoriartyTeamWin&&!MyPlayer.IsDead && ev.EndReason == GameEndReason.Situation)
            {
                ev.Reject();
            }
        }
        [OnlyMyPlayer]
        private void BlockWins(PlayerBlockWinEvent ev)
        {
            ev.IsBlocked |= !Moran.CanWinAsOriginalTeamOption && ev.GameEnd != Moriarty.Instance.MoriartyTeamWin;
        }
        [OnlyMyPlayer]
        private void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd==Moriarty.Instance.MoriartyTeamWin);
        }

        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            if (MyRifle != null && Moran.StoreRifleOnUsingUtilityOption)
            {
                var p = MyPlayer.ToAUPlayer();
                if (p.onLadder || p.inMovingPlat || p.inVent) RpcEquip.Invoke((MyPlayer.PlayerId, false));
            }
        }
        ModAbilityButton equipButton = null!;
        public Instance(GamePlayer player):base(player)
        {
        }

        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (base.AmOwner || canSeeAllInfo)
            {
                string append = " ✿";
                name += append.Color(MyRole.UnityColor);
            }
        }

        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                AmongUsUtil.PlayCustomFlash(MyRole.UnityColor, 0f, 0.25f, 0.4f, 0f);
                equipButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, "sniper.equip",
                    0f, "equip", buttonSprite);
                equipButton.OnClick = (button) =>
                {
                    if (MyRifle == null)
                    {
                        NebulaGameManager.Instance?.RpcDoGameAction(MyPlayer, MyPlayer.Position, GameActionTypes.SniperEquippingAction);
                        NebulaAsset.PlaySE(NebulaAudioClip.SniperEquip, true);
                        equipButton.SetLabel("unequip");
                    }
                    else
                        equipButton.SetLabel("equip");

                    RpcEquip.Invoke((MyPlayer.PlayerId, MyRifle == null));

                    if (MyRifle != null)
                    {
                        var circle = EffectCircle.SpawnEffectCircle(PlayerControl.LocalPlayer.transform, Vector3.zero, Palette.ImpostorRed, Moran.ShotNoticeRangeOption, null, true);
                        var script = circle.gameObject.AddComponent<ScriptBehaviour>();
                        script.UpdateHandler += () =>
                        {
                            if (MyRifle == null) circle.Disappear();
                        };
                        this.BindGameObject(circle.gameObject);
                    }
                };
                equipButton.OnBroken = (button) =>
                {
                    if (MyRifle != null)
                    {
                        equipButton.SetLabel("equip");
                        RpcEquip.Invoke((MyPlayer.PlayerId, false));
                    }
                    Snatcher.RewindKillCooldown();
                };
                GameOperatorManager.Instance?.Subscribe<MeetingStartEvent>(ev => equipButton.SetLabel("equip"), this);

                var killButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, true, false, Virial.Compat.VirtualKeyInput.Kill, "sniper.kill",
                    Moran.SnipeCoolDownOption.GetCoolDown(MyPlayer.TeamKillCooldown), "snipe", null,
                    _ => MyRifle != null, _ => !equipButton.IsBroken)
                    .SetLabelType(Virial.Components.ModAbilityButton.LabelType.Impostor)
                    .SetAsMouseClickButton();
                killButton.OnClick = (button) =>
                {
                    NebulaAsset.PlaySE(NebulaAudioClip.SniperShot, true);
                    var target = MyRifle?.GetTarget(Moran.ShotSizeOption, Moran.ShotEffectiveRangeOption);
                    if (target != null && !(GameOperatorManager.Instance?.Run(new PlayerInteractPlayerLocalEvent(MyPlayer, target, new(IsKillInteraction: true))).IsCanceled ?? false))
                    {
                        bool isBlown = target.IsBlown;
                        MyPlayer.MurderPlayer(target, PlayerState.Sniped, EventDetail.Kill, KillParameter.RemoteKill, null);
                        new StaticAchievementToken("moran.common1");
                        if (target.RealPlayer.Role.Role is Holmes)
                        {
                            new StaticAchievementToken("moran.common2");
                            if (MyPlayer.IsInvisible)
                            {
                                new StaticAchievementToken("moran.another1");
                            }
                            var ability = target.RealPlayer.Role.GetAbility<Holmes.Ability>();
                            if (ability != null)
                            {
                                if (ability.divideResults.ContainsKey(MyPlayer.PlayerId))
                                {
                                    new StaticAchievementToken("moran.challenge1");
                                }
                            }
                        }
                    }
                    else
                    {
                        NebulaGameManager.Instance?.GameStatistics.RpcRecordEvent(GameStatistics.EventVariation.Kill, EventDetail.Missed, MyPlayer.ToAUPlayer(), 0);
                    }
                    Sniper.RpcShowNotice.Invoke(MyPlayer.Position);

                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();

                    if (Moran.StoreRifleOnFireOption) RpcEquip.Invoke((MyPlayer.PlayerId, false));

                };
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
        }

        [Local]
        [OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            if (MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));
        }

        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (MyRifle != null) RpcEquip.Invoke((MyPlayer.PlayerId, false));
        }

        System.Collections.IEnumerator CoShowAimAssist()
        {
            System.Collections.IEnumerator CoUpdateAimAssistArrow(PlayerControl player)
            {
                DeadBody? deadBody = null;
                Vector2 pos = Vector2.zero;
                Vector2 dir = Vector2.zero;
                Vector2 tempDir = Vector2.zero;
                bool isFirst = true;

                UnityEngine.Color targetColor = new UnityEngine.Color(55f / 225f, 1f, 0f);
                float t = 0f;

                SpriteRenderer? renderer = null;

                while (true)
                {
                    if (MeetingHud.Instance || MyPlayer.IsDead || MyRifle == null || IsDeadObject) break;

                    if (player.Data.IsDead && !deadBody) deadBody = Helpers.GetDeadBody(player.PlayerId);

                    //死亡して、死体も存在しなければ追跡を終了
                    if (player.Data.IsDead && !deadBody) break;

                    if (renderer == null)
                    {
                        renderer = UnityHelper.CreateObject<SpriteRenderer>("AimAssist", HudManager.Instance.transform, Vector3.zero);
                        renderer.sprite = aimAssistSprite.GetSprite();
                    }

                    pos = player.Data.IsDead ? deadBody!.transform.position : player.transform.position;
                    tempDir = (pos - (Vector2)PlayerControl.LocalPlayer.transform.position).normalized;

                    NebulaGameManager.Instance!.WideCamera.CheckPlayerState(out var localScale, out var localRotateZ);
                    tempDir.x *= localScale.x;
                    tempDir.y *= localScale.y;

                    if (isFirst)
                    {
                        dir = tempDir;
                        isFirst = false;
                    }
                    else
                    {
                        dir = (tempDir + dir).normalized;
                    }

                    float angle = Mathf.Atan2(dir.y, dir.x) + localRotateZ.DegToRad();
                    renderer.transform.eulerAngles = new Vector3(0, 0, angle.RadToDeg());
                    renderer.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2f, Mathf.Sin(angle) * 2f, -30f);

                    t += Time.deltaTime / 0.8f;
                    if (t > 1f) t = 1f;
                    renderer.color = UnityEngine.Color.Lerp(UnityEngine.Color.white, targetColor, t).AlphaMultiplied(0.6f);

                    yield return null;
                }

                if (renderer == null) yield break;

                float a = 0.6f;
                while (a > 0f)
                {
                    a -= Time.deltaTime / 0.8f;
                    var color = renderer.color;
                    color.a = a;
                    renderer.color = color;
                    yield return null;
                }

                GameObject.Destroy(renderer.gameObject);
            }

            yield return new WaitForSeconds(Moran.DelayInAimAssistOption);

            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (!p.AmOwner) NebulaManager.Instance.StartCoroutine(CoUpdateAimAssistArrow(p).WrapToIl2Cpp());
            }
        }

        void EquipRifle()
        {
            MyRifle = new SniperRifle(MyPlayer).Register(this);

            if (AmOwner && Moran.AimAssistOption) NebulaManager.Instance.StartCoroutine(CoShowAimAssist().WrapToIl2Cpp());
        }

        void UnequipRifle()
        {
            if (MyRifle != null) MyRifle.Release();
            MyRifle = null;
        }

        static RemoteProcess<(byte playerId, bool equip)> RpcEquip = new(
        "EquipRifleMoranModifier",
        (message, _) =>
        {
            var player = NebulaGameManager.Instance?.GetPlayer(message.playerId);
            if (player==null)
            {
                return;
            }
            var sniper = player.Modifiers.FirstOrDefault(m => m is MoranModifier.Instance) as MoranModifier.Instance;
            if (sniper != null)
            {
                if (message.equip)
                    sniper.EquipRifle();
                else
                    sniper.UnequipRifle();
            }
        }
        );
    }

    private static SpriteLoader snipeNoticeSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.SniperRifleArrow.png", 200f);
    public static RemoteProcess<Vector2> RpcShowNotice = new(
        "ShowSnipeNoticeMoranModifier",
        (message, _) =>
        {
            if ((message - (Vector2)PlayerControl.LocalPlayer.transform.position).magnitude < Moran.ShotNoticeRangeOption)
            {
                var arrow = new Arrow(snipeNoticeSprite.GetSprite(), false) { IsSmallenNearPlayer = false, IsAffectedByComms = false, FixedAngle = true, OnJustPoint = true };
                arrow.Register(arrow);
                arrow.TargetPos = message;
                NebulaManager.Instance.StartCoroutine(arrow.CoWaitAndDisappear(3f).WrapToIl2Cpp());
            }
        }
        );
}
[NebulaRPCHolder]
public class Holmes : DefinedSingleAbilityRoleTemplate<Holmes.Ability>, DefinedRole,HasCitation
{
    private Holmes() : base("holmes", new(214, 156, 45), RoleCategory.CrewmateRole,NebulaTeams.CrewmateTeam, [SurveyCooldownOption, SurveyMaxUseNumOption],false,true,()=>((ISpawnable)Moriarty.MyRole).IsSpawnable&&Moriarty.CanWinByKillHolmes)
    {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagChaotic);
        ConfigurationHolder?.ScheduleAddRelated(() => [Moriarty.MyRole.ConfigurationHolder, Moran.MyRole.ConfigurationHolder]);
    }

    static private readonly FloatConfiguration SurveyCooldownOption = NebulaAPI.Configurations.Configuration("options.role.holmes.surveyCooldown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private readonly IntegerConfiguration SurveyMaxUseNumOption = NebulaAPI.Configurations.Configuration("options.role.holmes.surveymaxUseNum", (1,15),4);
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player);
    bool ISpawnable.IsSpawnable => ((ISpawnable)Moriarty.MyRole).IsSpawnable && Moriarty.CanWinByKillHolmes;
    Citation? HasCitation.Citation { get { return PCitations.LTS; } }
    static public readonly Holmes MyRole = new();
    public class Ability : AbstractPlayerAbility, IPlayerAbility
    {

        static private readonly Image buttonSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.Buttons.OracleButton.png", 115f);
        public Dictionary<byte, (string longName, string shortName)> divideResults = [];
        bool playerisevil;
        GamePlayer exilePlayer;
        [Local]
        void OnTaskRestart(TaskPhaseRestartEvent ev)
        {
            if (playerisevil&&exilePlayer!=null&&exilePlayer.IsDead)
            {
                new StaticAchievementToken("holmes.common2");
            }
            playerisevil = false;
            exilePlayer = null;
        }
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (useskill&&!MyPlayer.IsDead)
            {
                new StaticAchievementToken("holmes.challenge1");
            }
        }
        [Local,OnlyMyPlayer]
        void OnMeDead(PlayerMurderedEvent ev)
        {
            if (moriartyTeamCheck&&(ev.Murderer.Role.Role is Moran||ev.Murderer.TryGetModifier<MoranModifier.Instance>(out var m)))
            {
                new StaticAchievementToken("holmes.another1");
            }
        }
        bool useskill;
        bool moriartyTeamCheck;
        public Ability(GamePlayer player) : base(player)
        {
            if (AmOwner)
            {
                playerisevil = false;
                exilePlayer = null;
                moriartyTeamCheck = false;
                int leftuse = SurveyMaxUseNumOption;
                var playerTracker = ObjectTrackers.ForPlayerlike(this, null, MyPlayer, (p) => ObjectTrackers.PlayerlikeStandardPredicate(p) && !divideResults.ContainsKey(p.RealPlayer.PlayerId));

                var oracleButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability,
                    SurveyCooldownOption, "survey", buttonSprite,
                    _ => playerTracker.CurrentTarget != null, null);
                oracleButton.OnClick = (button) => 
                {
                    leftuse--;
                    var player = playerTracker.CurrentTarget.RealPlayer;
                    var result = player.Role.Role;
                    if (player.Role is Moriarty.Instance || player.Role.Role is Moran || player.TryGetModifier<MoranModifier.Instance>(out var m))
                    {
                        moriartyTeamCheck = true;
                        var allroles = Nebula.Roles.Roles.AllRoles.Where(r=>!divideResults.Values.Any(v=>r.DisplayColoredName==v.longName));
                        result=allroles.Where(r => r.Category == RoleCategory.CrewmateRole && r.IsSpawnable).ToList().Random();
                    }
                    divideResults[player.PlayerId]=(result.DisplayColoredName,result.DisplayColoredShort);
                    if (player.IsImpostor||!player.IsCrewmate)
                    {
                        playerisevil = true;
                        exilePlayer = player;
                    }
                    button.UpdateUsesIcon(leftuse.ToString());
                    if (leftuse<=0)
                    {
                        button.HideUsesIcon();
                    }
                    new StaticAchievementToken("holmes.common1");
                    button.StartCoolDown();
                    useskill = true;
                };
                oracleButton.ShowUsesIcon(3, leftuse.ToString());
                oracleButton.Visibility = (button) => !MyPlayer.IsDead && leftuse > 0;
                oracleButton.SetLabel("survey");
            }
        }

        [Local]
        void ReflectRoleName(PlayerSetFakeRoleNameEvent ev)
        {
            if (divideResults.TryGetValue(ev.Player.PlayerId, out var roleName)) ev.Set(ev.InMeeting ? roleName.longName : roleName.shortName);
        }
    }
}