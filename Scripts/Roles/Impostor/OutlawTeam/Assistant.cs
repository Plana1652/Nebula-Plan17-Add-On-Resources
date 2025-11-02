using AmongUs.GameOptions;
using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;

namespace Plana.Roles.Impostor;

public class AssistantShower : DefinedRoleTemplate, DefinedRole, HasCitation
{
    private AssistantShower() : base("assistant", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.CrewmateTeam, null, false, true, () => false)
    {
    }
    bool DefinedAssignable.ShowOnFreeplayScreen => false;
    bool DefinedAssignable.ShowOnHelpScreen => false;
    string DefinedAssignable.InternalName => "assistantShower";
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public AssistantShower MyRole = new AssistantShower();
    bool ISpawnable.IsSpawnable
    {
        get
        {
            return ((ISpawnable)OutlawLeaderShower.MyRole).IsSpawnable;
        }
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;
        RoleTaskType RuntimeRole.TaskType => RoleTaskType.NoTask;
        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated()
        {
            return;
            if (AmOwner)
            {
                MyPlayer.SetRole(Assistant.MyRole);
            }
        }
    }
}
public class Assistant : DefinedRoleTemplate, DefinedRole, HasCitation
{
    private Assistant() : base("assistant", NebulaTeams.ImpostorTeam.Color, RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam,null,false,true,()=>false)
    {
    }
    bool IGuessed.CanBeGuess => false;
    bool DefinedRole.IsMadmate => true;
    bool DefinedAssignable.ShowOnHelpScreen => false;
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public Assistant MyRole = new Assistant();
    RoleCategory DefinedCategorizedAssignable.Category
    {
        get
        {
            if (NebulaAPI.CurrentGame==null)
            {
                return RoleCategory.CrewmateRole;
            }
            bool cankill = false;
            var myplayer = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.Role is Assistant.Instance);
            if (myplayer != null && myplayer.Role is Assistant.Instance assistant)
            {
                cankill = assistant.CanKill;
            }
            return cankill ? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole;
        }
    }
    RoleTeam DefinedSingleAssignable.Team
    {
        get
        {
            if (NebulaAPI.CurrentGame==null)
            {
                return NebulaTeams.CrewmateTeam;
            }
            bool cankill = false;
            var myplayer = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.Role is Assistant.Instance);
            if (myplayer != null && myplayer.Role is Assistant.Instance assistant)
            {
                cankill = assistant.CanKill;
            }
            return cankill ? NebulaTeams.ImpostorTeam : NebulaTeams.CrewmateTeam;
        }
    }
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;
        DefinedRole RuntimeRole.ExternalRecognitionRole => AssistantShower.MyRole;
        RoleTaskType RuntimeRole.TaskType => RoleTaskType.NoTask;
        static RemoteProcess<bool> SetAssistantCanKillState = new("SetAssistantCanKillStateRPC", (m, _) =>
        {
            var myplayer = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.Role is Assistant.Instance);
            if (myplayer != null && myplayer.Role is Assistant.Instance assistant)
            {
                assistant.CanKill = m;
                if (assistant.CanKill)
                {
                    DestroyableSingleton<RoleManager>.Instance.SetRole(myplayer.ToAUPlayer(), RoleTypes.Impostor);
                }
                else
                {
                    DestroyableSingleton<RoleManager>.Instance.SetRole(myplayer.ToAUPlayer(), RoleTypes.Crewmate);
                }
            }
        });

        public Instance(GamePlayer player) : base(player)
        {
        }
        bool RuntimeRole.HasVanillaKillButton => CanKill;
        bool RuntimeRole.CanInvokeSabotage => true;
        bool RuntimeRole.CanMoveInVent => true;
        bool RuntimeRole.CanUseVent => true;
        bool RuntimeRole.HasImpostorVision => true;
        bool RuntimeRole.IgnoreBlackout => true;
        public bool CanKill = false;
        [OnlyMyPlayer]
        private void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.IsWin |= ev.GameEnd == NebulaGameEnd.ImpostorWin;
        }

        [OnlyMyPlayer]
        private void BlockWins(PlayerBlockWinEvent ev)
        {
            ev.IsBlocked |= ev.GameEnd == NebulaGameEnd.CrewmateWin;
        }
        [Local]
        void OnLeaderDead(PlayerDieEvent ev)
        {
            NebulaManager.Instance.StartDelayAction(1f, () =>
            {
                if (!MyPlayer.IsDead && ev.Player.Role is OutlawLeader.Instance)
                {
                    SetAssistantCanKillState.Invoke(true);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    if (ev.Player.Modifiers.Any(r => r is GuesserModifier))
                    {
                        MyPlayer.AddModifier(GuesserModifier.MyRole);
                    }
                }
            });
        }
        [Local]
        void OnLeaderChangeRole(PlayerTryToChangeRoleEvent ev)
        {
            if (!ev.Player.IsDead&&ev.CurrentRole is OutlawLeader.Instance)
            {
                if (!MyPlayer.IsDead)
                {
                    SetAssistantCanKillState.Invoke(true);
                    NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                    if (ev.Player.Modifiers.Any(r => r is GuesserModifier))
                    {
                        MyPlayer.AddModifier(GuesserModifier.MyRole);
                    }
                }
            }
        }
        [OnlyMyPlayer]
        private void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (OutlawLeader.IsOutlawTeam(ev.Target))
            {
                ev.SetAsCannotKillBasically();
            }
        }
        [Local]
        void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (OutlawLeader.IsOutlawTeam(ev.Player))
            {
                ev.Color = NebulaTeams.ImpostorTeam.Color;
            }
        }
        [Local]
        void ShowRoleText(PlayerSetFakeRoleNameEvent ev)
        {
            if (ev.Player.PlayerId != MyPlayer.PlayerId && OutlawLeader.IsOutlawTeam(ev.Player))
            {
                ev.Set(ev.Player.Role.Role.DisplayColoredName);
            }
        }
        ModAbilityButton ShapeshiftButton;
        static RemoteProcess<(GamePlayer, GamePlayer, bool)> ShapeshiftRpc = new("shapeShiftPlayerRPC", (message, _) =>
        {
        if (MeetingHud.Instance)
        {
            return;
        }
        var shapeshifter = message.Item1.ToAUPlayer();
        //shapeshifter.Shapeshift(message.Item2.ToAUPlayer(), message.Item3);
        shapeshifter.shapeshifting = true;
        shapeshifter.MyPhysics.SetNormalizedVelocity(UnityEngine.Vector2.zero);
        if (GamePlayer.LocalPlayer.AmOwner && !Minigame.Instance)
        {
            PlayerControl.HideCursorTemporarily();
        }
        RoleEffectAnimation roleEffectAnimation = UnityEngine.Object.Instantiate<RoleEffectAnimation>(DestroyableSingleton<RoleManager>.Instance.shapeshiftAnim, message.Item1.ToAUPlayer().gameObject.transform);
        roleEffectAnimation.SetMaskLayerBasedOnWhoShouldSee(GamePlayer.LocalPlayer.AmOwner);
        PlayerMaterial.SetColors(message.Item1.DefaultOutfit.outfit.ColorId, roleEffectAnimation.Renderer);
        if (message.Item1.ToAUPlayer().cosmetics.FlipX)
        {
            roleEffectAnimation.transform.position -= new UnityEngine.Vector3(0.14f, 0f, 0f);
        }
            Action midanimcb = () =>
            {
                if (message.Item1.PlayerId == message.Item2.PlayerId)
                {
                    message.Item1.Unbox().RemoveOutfit("Shapeshift");
                    message.Item1.ToAUPlayer().CurrentOutfitType = PlayerOutfitType.Default;
                }
                else
                {
                    message.Item1.Unbox().AddOutfit(new OutfitCandidate(message.Item2.GetOutfit(75), "Shapeshift", 50, true));
                    message.Item1.ToAUPlayer().CurrentOutfitType = PlayerOutfitType.Shapeshifted;
                }
                shapeshifter.cosmetics.SetScale(shapeshifter.MyPhysics.Animations.DefaultPlayerScale, shapeshifter.defaultCosmeticsScale);
                if (OutlawLeaderShower.AssistantShapeshiftLeaveEvidence)
                {
                    var shapeRole = AmongUsUtil.GetRolePrefab<ShapeshifterRole>();
                    Component component = UnityEngine.Object.Instantiate<ShapeshifterEvidence>(shapeRole!.EvidencePrefab);
                    UnityEngine.Vector3 vector = shapeshifter.transform.position + shapeRole.EvidenceOffset * 0.7f;
                    vector.z = vector.y / 1000f;
                    component.transform.position = vector;
                }
            };
            roleEffectAnimation.MidAnimCB = midanimcb;
            float shapeshiftScale = shapeshifter.MyPhysics.Animations.ShapeshiftScale;
            if (AprilFoolsMode.ShouldLongAround())
            {
                shapeshifter.cosmetics.ShowLongModeParts(false);
                shapeshifter.cosmetics.SetHatVisorVisible(false);
            }
            shapeshifter.StartCoroutine(shapeshifter.ScalePlayer(shapeshiftScale, 0.25f));
            Action OnClipEnd = () =>
            {
                shapeshifter.shapeshifting = false;
                if (AprilFoolsMode.ShouldLongAround())
                {
                    shapeshifter.cosmetics.ShowLongModeParts(true);
                    shapeshifter.cosmetics.SetHatVisorVisible(true);
                }
            };
            roleEffectAnimation.Play(shapeshifter,OnClipEnd, PlayerControl.LocalPlayer.cosmetics.FlipX, RoleEffectAnimation.SoundType.Local, 0f, true, 0f);
        });
        public void RevertPlayerToNormal(bool shouldAnimate = true)
        {
            if (MyPlayer.ToAUPlayer().CurrentOutfitType == PlayerOutfitType.Shapeshifted || MeetingHud.Instance)
            {
                ShapeshiftRpc.Invoke((MyPlayer, MyPlayer, shouldAnimate));
            }
        }
        public static bool OnShapeshiftPanelSelected(ShapeshifterMinigame __instance,PlayerControl target)
        {
            if (PlayerControl.LocalPlayer.inVent)
            {
                __instance.Close();
                return false;
            }
            if (target != null)
            {
                ShapeshiftRpc.Invoke((GamePlayer.LocalPlayer, target.ToNebulaPlayer(), true));
            }
            else
            {
                Logger.GlobalInstance.Warning(string.Format("Player attempted to shapeshift into a null player {0}. Maybe due to disconnect?", (target != null) ? new byte?(target.PlayerId) : null), null);
            }
            __instance.Close();
            return false;
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                ShapeshiftButton = NebulaAPI.Modules.EffectButton(this, base.MyPlayer, VirtualKeyInput.Ability, null, OutlawLeaderShower.AssistantShapeshiftCooldown, OutlawLeaderShower.AssistantShapeshiftDuration,"assistant.shapeshift", new WrapSpriteLoader(()=>AmongUsUtil.GetRolePrefab<ShapeshifterRole>()!.Ability.Image), (ModAbilityButton _) =>MyPlayer.CanMove, (ModAbilityButton _) => !MyPlayer.IsDead, false,false).SetLabelType(ModAbilityButton.LabelType.Impostor);
                ShapeshiftButton.OnClick = (button) =>
                {
                    button.ToggleEffect();
                };
                ShapeshiftButton.OnEffectStart = (button) =>
                {
                    var shapeshift = AmongUsUtil.GetRolePrefab<ShapeshifterRole>();
                    MyPlayer.ToAUPlayer().NetTransform.Halt();
                    ShapeshifterMinigame shapeshifterMinigame = UnityEngine.Object.Instantiate<ShapeshifterMinigame>(shapeshift.ShapeshifterMenu);
                    shapeshifterMinigame.transform.SetParent(Camera.main.transform, false);
                    shapeshifterMinigame.transform.localPosition = new UnityEngine.Vector3(0f, 0f, -50f);
                    shapeshifterMinigame.Begin(null);
                };
                ShapeshiftButton.OnEffectEnd = (button) =>
                {
                    if (MyPlayer.ToAUPlayer().CurrentOutfitType == PlayerOutfitType.Shapeshifted)
                    {
                        MyPlayer.ToAUPlayer().NetTransform.Halt();
                        RevertPlayerToNormal(!MyPlayer.Logic.InVent);
                        button.StartCoolDown();
                    }
                };
                (ShapeshiftButton.EffectTimer as TimerImpl).SetPredicate(() =>
                {
                    if (MyPlayer.ToAUPlayer().CurrentOutfitType == PlayerOutfitType.Shapeshifted && ShapeshiftButton.EffectTimer.IsProgressing)
                    {
                        return true;
                    }
                    return false;
                });
                (ShapeshiftButton.CoolDownTimer as TimerImpl).SetPredicate(() =>
                {
                    if (MyPlayer.ToAUPlayer().CurrentOutfitType == PlayerOutfitType.Default && ShapeshiftButton.CoolDownTimer.IsProgressing&&PlayerControl.LocalPlayer.IsKillTimerEnabled)
                    {
                        return true;
                    }
                    return false;
                });
            }
            GameOperatorManager.Instance!.Subscribe<MeetingPreStartEvent>(delegate (MeetingPreStartEvent ev)
            {
                MyPlayer.Unbox().RemoveOutfit("Shapeshift");
                MyPlayer.ToAUPlayer().CurrentOutfitType = PlayerOutfitType.Default;
            }, this, 100);
        }
        void RuntimeAssignable.OnInactivated()
        {
            if (AmOwner)
            {
                RevertPlayerToNormal(!MyPlayer.Logic.InVent);
            }
        }
    }
}