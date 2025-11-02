using Plana.Core;
using Plana.Roles.Impostor;
using Plana.Roles.Neutral;
using static Plana.Roles.Impostor.CrawlerEngineer;

namespace Plana.Roles.Crewmate;

public class Celestialdog : DefinedRoleTemplate, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder
{
    private Celestialdog() : base("celestialdog", new(255,228,174), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam,[SkillCooldownOption,SkillDurationOption,KillCooldownOption,KillNumOption,CanKillStateOption,SkillEndCanUseTeleport]) 
    {
    }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player,arguments.Get(0,0),arguments.GetAsBool(1));
    public static readonly FloatConfiguration SkillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.celestialdog.skillCoolDown", (2.5f,60f,2.5f),20f,FloatConfigurationDecorator.Second);
    public static readonly FloatConfiguration SkillDurationOption = NebulaAPI.Configurations.Configuration("options.role.celestialdog.skillDuration", (2.5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    public static readonly FloatConfiguration KillCooldownOption = NebulaAPI.Configurations.Configuration("options.role.celestialdog.killCooldown", (2.5f, 60f, 2.5f), 22.5f, FloatConfigurationDecorator.Second);
    public static readonly IntegerConfiguration KillNumOption = NebulaAPI.Configurations.Configuration("options.role.celestialdog.killnum", (1,16), 2);
    public static readonly ValueConfiguration<int> CanKillStateOption = NebulaAPI.Configurations.Configuration("options.role.celestialdog.cankillstate", ["options.role.celestialdog.state.InEffect", "options.role.celestialdog.state.none"], 0);
    public static readonly BoolConfiguration SkillEndCanUseTeleport = NebulaAPI.Configurations.Configuration("options.role.celestialdog.endskillcanusetp", true);

    static public Celestialdog MyRole = new Celestialdog();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IReleasable, IBindPlayer, IGameOperator
    {
        DefinedRole RuntimeRole.Role => MyRole;
        int[]? RuntimeAssignable.RoleArguments => [leftKill, addedKill.AsInt()];
        bool addedKill;
        public Instance(GamePlayer player, int leftkill, bool addedkill) : base(player)
        {
            leftKill = leftkill;
            addedKill = addedkill;
        }
        Virial.Media.Image skillImage = NebulaAPI.AddonAsset.GetResource("DogSkill.png")!.AsImage(115f)!;
        Virial.Media.Image KillImage = NebulaAPI.AddonAsset.GetResource("DogKill.png")!.AsImage(100f)!;
        Virial.Media.Image TeleportImage = NebulaAPI.AddonAsset.GetResource("DogTeleport.png")!.AsImage(115f)!;
        public ModAbilityButton skillButton, killButton, teleportButton;
        RoleTaskType RuntimeRole.TaskType
        {
            get
            {
                if (skillButton == null)
                {
                    return RoleTaskType.CrewmateTask;
                }
                return skillButton.IsInEffect ? RoleTaskType.NoTask : RoleTaskType.CrewmateTask;
            }
        }
        int leftKill;
        void AddKill()
        {
            if (killButton == null)
            {
                ObjectTracker<GamePlayer> killTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.KillablePredicate(base.MyPlayer), null, false, false);
                killButton = NebulaAPI.Modules.AbilityButton(this, false, true, 0, false).BindKey(base.MyPlayer.IsCrewmate ? Virial.Compat.VirtualKeyInput.Kill : Virial.Compat.VirtualKeyInput.SecondaryAbility, null);
                killButton.Availability = (button) => killTracker.CurrentTarget != null && this.MyPlayer.CanMove&&(CanKillStateOption.GetValue()==1||skillButton.IsInEffect);
                killButton.Visibility = (button) => !base.MyPlayer.IsDead && leftKill > 0;
                killButton.SetImage(KillImage);
                killButton.ShowUsesIcon(3, leftKill.ToString());
                this.killButton.OnClick = delegate (ModAbilityButton button)
                {
                    leftKill--;
                    MyPlayer.MurderPlayer(killTracker.CurrentTarget, PlayerState.Dead, EventDetail.Kill, KillParameter.NormalKill, (result) =>
                    {
                        if (result == KillResult.Kill)
                        {
                            killButton.UpdateUsesIcon(leftKill.ToString());
                        }
                        else
                        {
                            leftKill++;
                        }
                    });
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                };
                killButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, KillCooldownOption).SetAsKillCoolTimer().Start(null);
                killButton.SetLabel("kill");
                killButton.StartCoolDown();
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(killButton.GetKillButtonLike());
            }
            killButton.UpdateUsesIcon(leftKill.ToString());
        }
        bool usedTeleport;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                skillButton = NebulaAPI.Modules.EffectButton(this, base.MyPlayer, VirtualKeyInput.Ability, null, SkillCooldownOption, SkillDurationOption, "celestialdog.skill", skillImage, (ModAbilityButton _) => MyPlayer.CanMove, (ModAbilityButton _) => !MyPlayer.IsDead, false, false).SetLabelType(ModAbilityButton.LabelType.Crewmate);
                skillButton.OnEffectStart = (button) =>
                {
                    usedTeleport = false;
                    MyPlayer.ToAUPlayer().Visible = false;
                    MyPlayer.GainAttribute(PlayerAttributes.Invisible, SkillDurationOption - 0.8f, false, 100);
                    RpcEquip.Invoke(new ValueTuple<byte, bool>(MyPlayer.PlayerId, true));
                };
                skillButton.OnEffectEnd = (button) =>
                {
                    MyPlayer.ToAUPlayer().Visible = true;
                    RpcEquip.Invoke(new ValueTuple<byte, bool>(MyPlayer.PlayerId, false));
                    button.StartCoolDown();
                };
                if (leftKill > 0)
                {
                    AddKill();
                }
                if (SkillEndCanUseTeleport)
                {
                    teleportButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, VirtualKeyInput.SecondaryAbility, 0f, "celestialdog.teleport", TeleportImage);
                    teleportButton.Visibility = (button) => skillButton.IsInEffect && skillButton.EffectTimer.Percentage <= 0.25f && !usedTeleport;
                    teleportButton.PlayFlashWhile = (button) => true;
                    teleportButton.OnClick = (button) =>
                    {
                        usedTeleport = true;
                        byte mapId = AmongUsUtil.CurrentMapId;
                        NebulaPreSpawnLocation[] cand = NebulaPreSpawnLocation.Locations[(int)mapId];
                        if (cand.Length == 0)
                        {
                            cand = NebulaPreSpawnLocation.Locations[(int)mapId].Where((NebulaPreSpawnLocation l) => l.VanillaIndex != null).ToArray<NebulaPreSpawnLocation>();
                        }
                        var list = new List<UnityEngine.Vector2>();
                        cand.Do(p => list.Add(p.Position!.Value));
                        MyPlayer.ToAUPlayer().NetTransform.RpcSnapTo(list[UnityEngine.Random.Range(0,list.Count)]);
                    };
                }
                GameOperatorManager instance = GameOperatorManager.Instance!;
                if (instance != null)
                {
                    instance.Subscribe<GameUpdateEvent>(op =>
                    {
                        if (addedKill)
                        {
                            return;
                        }
                        if (!skillButton.IsInEffect)
                        {
                            return;
                        }
                        foreach (DeadBody dead in Helpers.AllDeadBodies())
                        {
                            var ma = (dead.TruePosition - MyPlayer.TruePosition.ToUnityVector()).magnitude;
                            if (ma <= 3.5f)
                            {
                                addedKill = true;
                                leftKill = KillNumOption;
                                AddKill();
                            }
                        }
                    }, this, 100);
                    instance.Subscribe<MeetingStartEvent>(delegate (MeetingStartEvent ev)
                    {
                        if (MyState != null)
                        {
                            RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                        }
                    }, this, 100);
                    instance.Subscribe<PlayerDieEvent>(ev =>
                    {
                        if (ev.Player.AmOwner)
                        {
                            if (MyState != null)
                            {
                                RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                            }
                        }
                    }, this);
                    instance.RegisterOnReleased(() =>
                    {
                        if (MyState != null)
                        {
                            RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                        }
                    }, skillButton);
                }

            }
        }
        public void EnterDogState()
        {
            MyState = new DogState(base.MyPlayer).Register(this);
        }

        public void ExitDogState()
        {
            if (MyState != null)
            {
                MyState.Release();
            }
            MyState = null!;
        }
        public DogState MyState
        {
            get; private set;
        } = null!;
        static readonly RemoteProcess<(byte playerId, bool enter)> RpcEquip = new(
        "CelestialDogStateRPC",
        (message, _) =>
        {
            var player = GamePlayer.GetPlayer(message.playerId);
            var role = player.Role;
            if (role is Celestialdog.Instance instance)
            {
                if (message.enter)
                    instance.EnterDogState();
                else
                    instance.ExitDogState();
            }
        }
        );
        public class DogState : EquipableAbility,IGameOperator
        {
            Virial.Media.MultiImage DogNormal = NebulaAPI.AddonAsset.GetResource("DogNormal.png")!.AsMultiImage(4, 2, 100f)!;
            Virial.Media.MultiImage DogRun = NebulaAPI.AddonAsset.GetResource("DogRun.png")!.AsMultiImage(4, 2, 100f)!;
            protected override float Size => 0.6f;
            protected override float Distance => 0.3f;
            public DogState(GamePlayer owner) : base(owner, false, "Dogstate")
            {
                spriteIndex = 0;
                //NebulaManager.Instance.StartCoroutine(CoAppear().WrapToIl2Cpp());
                owner.ToAUPlayer().Visible = false;
            }
            void IGameOperator.OnReleased()
            {
                if (Renderer) GameObject.Destroy(Renderer.gameObject);
                Renderer = null!;
                Owner.ToAUPlayer().Visible = true;
            }
            protected override void HudUpdate(GameHudUpdateEvent ev)
            {
                return;
            }
            int spriteIndex;
            bool isrunning;
            float updateSpriteTime;
            void OnUpdate(GameUpdateEvent ev)
            {
                var control = Owner.ToAUPlayer();
                control.Visible = false;
                if (control.MyPhysics.body.velocity.sqrMagnitude >= 0.05f)
                {
                    isrunning = true;
                }
                else
                {
                    isrunning = false;
                }
                Renderer.flipX = Owner.VanillaCosmetics.FlipX;
                updateSpriteTime += Time.deltaTime;
                if (updateSpriteTime >= 0.1f)
                {
                    Renderer.sprite = (isrunning ? DogRun : DogNormal).AsLoader(spriteIndex).GetSprite();
                    spriteIndex++;
                    if (spriteIndex >= 8)
                    {
                        spriteIndex = 0;
                    }
                    updateSpriteTime = 0f;
                }
            }
            /*System.Collections.IEnumerator CoAppear()
            {
                animing = true;
                for (int i = 0; i <= 1; i++)
                {
                    Renderer.sprite = PadSprite.AsLoader(i).GetSprite();
                    yield return Effects.Wait(0.08f);
                }
                animing = false;
            }
            System.Collections.IEnumerator CoDisappear()
            {
                animing = true;
                for (int i = 2; i <= 3; i++)
                {
                    Renderer.sprite = PadSprite.AsLoader(i).GetSprite();
                    yield return Effects.Wait(0.08f);
                }
                Release();
            }
            public void UnUsePad()
            {
                NebulaManager.Instance.StartCoroutine(CoDisappear().WrapToIl2Cpp());
            }*/
        }
    }
}