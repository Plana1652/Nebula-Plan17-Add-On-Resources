using AmongUs.Data.Player;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Nebula;
using Nebula.Game.Statistics;
using Nebula.Map;
using Nebula.Modules;
using Nebula.Patches;
using Nebula.Roles;
using Nebula.Roles.Complex;
using Nebula.Roles.Neutral;
using Nebula.Utilities;
using Plana.Roles.Modifier;
using System.Diagnostics.SymbolStore;
using Virial;
using Virial.Assignable;
using Virial.Attributes;
using Virial.Events.Player;
using Virial.Game;
using Vector2 = UnityEngine.Vector2;

namespace Plana.Role.Complex;
public class Splicer : DefinedSingleAbilityRoleTemplate<IUsurpableAbility>, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder
{
    private Splicer(bool isEvil) : base((isEvil ? "evil" : "nice") + "splicer", isEvil ? Palette.ImpostorRed.ToNebulaColor() : new(58, 127, 190),
        isEvil ? RoleCategory.ImpostorRole : RoleCategory.CrewmateRole, isEvil ? NebulaTeams.ImpostorTeam : NebulaTeams.CrewmateTeam,
        [isEvil ? EvilwarpCoolDownOption : NicewarpCoolDownOption, warpMaxDistanceOption,PreWarpTimeOption,WarpUseTimeOption, isEvil ? EvilwarpFailStartCooldown : NicewarpFailStartCooldown, warpStartvanish, useWarpLeftMark])
    {
        if (IsEvil)
        {
            ConfigurationHolder?.AppendConfigurations([KillFailWarp, HasNormalKill,CanKillImpostor]);
        }
        else
        {
            ConfigurationHolder?.AppendConfiguration(WarpingIgnoreBlackout);
        }
        ConfigurationHolder?.ScheduleAddRelated(() => [isEvil ? MyNiceRole.ConfigurationHolder! : MyEvilRole.ConfigurationHolder!]);
    }
    bool AssignableFilterHolder.CanLoadDefault(DefinedAssignable assignable)
    {
        return base.CanLoadDefaultTemplate(assignable) && !(assignable is chameleon);
    }
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => !IsEvil ? AbilityAssignmentStatus.CanLoadToMadmate : AbilityAssignmentStatus.Killers;
    public bool IsEvil => Category == RoleCategory.ImpostorRole;
    public override IUsurpableAbility CreateAbility(Player player, int[] arguments)
    {
        if (!IsEvil)
        {
            return new NiceAbility(player, arguments.GetAsBool(0));
        }
        return new EvilAbility(player, arguments.GetAsBool(0));
    }
    public static void LoadPatch(Harmony harmony)
    {
        PDebug.Log("Splicer Patch");
        harmony.PatchAll(typeof(Splicer.WarpSystem));
        PDebug.Log("Done");
    }
    static private FloatConfiguration NicewarpCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.splicer.NicewarpCoolDown", (0f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration EvilwarpCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.splicer.EvilwarpCoolDown", (0f, 60f, 2.5f), 25f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration warpMaxDistanceOption = NebulaAPI.Configurations.Configuration("options.role.splicer.warpMaxDistance", (1f, 20f, 0.25f), 4f, FloatConfigurationDecorator.Ratio);
    static private FloatConfiguration PreWarpTimeOption = NebulaAPI.Configurations.Configuration("options.role.splicer.PreWarpTime", (0f, 20f, 0.1f), 0.6f, (val)=>val.ToString("F1")+ Language.Translate("options.sec"));
    static private FloatConfiguration WarpUseTimeOption = NebulaAPI.Configurations.Configuration("options.role.splicer.WarpUseTime", (0f, 20f, 0.1f), 2.4f, (val) => val.ToString("F1") + Language.Translate("options.sec"));
    static private BoolConfiguration NicewarpFailStartCooldown = NebulaAPI.Configurations.Configuration("options.role.splicer.NicewarpFailStartCooldown", false);
    static private BoolConfiguration EvilwarpFailStartCooldown = NebulaAPI.Configurations.Configuration("options.role.splicer.EvilwarpFailStartCooldown", false);
    static private BoolConfiguration warpStartvanish = NebulaAPI.Configurations.Configuration("options.role.splicer.warpStartvanish", false);
    static private BoolConfiguration useWarpLeftMark = NebulaAPI.Configurations.Configuration("options.role.splicer.usewarpLeftMark", false);
    static private BoolConfiguration KillFailWarp = NebulaAPI.Configurations.Configuration("options.role.splicer.killfailwarp", false);
    static private BoolConfiguration HasNormalKill = NebulaAPI.Configurations.Configuration("options.role.splicer.hasnormalkill", false);
    static private BoolConfiguration CanKillImpostor = NebulaAPI.Configurations.Configuration("options.role.splicer.CanKillImpostor", false);
    static private BoolConfiguration WarpingIgnoreBlackout = NebulaAPI.Configurations.Configuration("options.role.splicer.warpingIgnoreBlackout", false);
    static public Splicer MyNiceRole = new Splicer(false);
    static public Splicer MyEvilRole = new Splicer(true);
    public class EvilAbility : AbstractPlayerUsurpableAbility, IPlayerAbility, IBindPlayer, IGameOperator, ILifespan
    {

        private ModAbilityButton? warpButton = null;
        static private Virial.Media.Image warpImage = NebulaAPI.AddonAsset.GetResource("WarpButton_impostorVer.png")!.AsImage(100f)!;
        bool IPlayerAbility.HideKillButton
        {
            get
            {
                if (HasNormalKill)
                {
                    return false;
                }
                ModAbilityButton modAbilityButton = warpButton!;
                return modAbilityButton == null || !modAbilityButton.IsBroken;
            }
        }
        bool isuseskilling;
        List<TeleportEvidence> marks = new List<TeleportEvidence>();
        int killnum;
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public EvilAbility(Virial.Game.Player player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                killnum = 0;
                iswarping = false;
                isuseskilling = false;
                marks = new List<TeleportEvidence>();
                warpButton = NebulaAPI.Modules.AbilityButton(this,MyPlayer,!HasNormalKill,false,!HasNormalKill?Virial.Compat.VirtualKeyInput.Kill:Virial.Compat.VirtualKeyInput.Ability,null, EvilwarpCoolDownOption, "warp2", (HasNormalKill ? warpImage : null)!, (button) => MyPlayer.CanMove&&!button.IsInEffect, (button) => !MyPlayer.IsDead, false).SetAsUsurpableButton(this);
                warpButton.OnClick = delegate (ModAbilityButton button)
                {
                    isuseskilling = true;
                    iswarping = false;
                    button.StartEffect();
                    PlayerControl.LocalPlayer.lightSource.StartCoroutine(WarpSystem.CoOrient(PlayerControl.LocalPlayer.lightSource, PreWarpTimeOption,WarpUseTimeOption, delegate (PlayerControl p)
                    {
                        if (p == null)
                        {
                            return;
                        }
                        var player = p.ToNebulaPlayer();
                        if (player==null)
                        {
                            return;
                        }
                        HighlightHelpers.SetHighlight(player, NebulaTeams.ImpostorTeam.Color);
                    }
                    !, () => { iswarping = true; }, () => { iswarping = false; }, delegate (PlayerControl p)
                    {
                        if (p != null&&(CanKillImpostor||!p.ToNebulaPlayer().IsImpostor))
                        {
                            if (MeetingHud.Instance)
                            {
                                return;
                            }
                            if (useWarpLeftMark)
                            {
                                TeleportEvidence mark = (NebulaSyncObject.RpcInstantiate(TeleportEvidence.MyGlobalTag, [PlayerControl.LocalPlayer.transform.localPosition.x, PlayerControl.LocalPlayer.transform.localPosition.y - 0.25f])?.SyncObject as TeleportEvidence)!;
                                marks.Add(mark!);
                            }
                            MyPlayer.MurderPlayer(p.ToNebulaPlayer(), PlayerStates.Dead, EventDetails.Kill, KillParameter.NormalKill);
                            NebulaAPI.CurrentGame?.KillButtonLikeHandler.StartCooldown();
                            new StaticAchievementToken("evilsplicer.common1");
                            killnum++;
                            if (killnum>=3)
                            {
                                new StaticAchievementToken("evilsplicer.challenge1");
                            }
                        }
                        else if (KillFailWarp)
                        {
                            TryWarp(() =>
                            {
                                if (!EvilwarpFailStartCooldown)
                                {
                                    button.StartCoolDown();
                                }
                            });
                        }
                    }
                    !).WrapToIl2Cpp());
                    /*PlayerControl.LocalPlayer.lightSource.StartCoroutine(WarpSystem.CoOrient(PlayerControl.LocalPlayer.lightSource, 0.6f, 2.4f,
                    (p) =>
                    {
                    }, () => { iswarping = true; }, () => { iswarping = false; }, () =>
                    {
                        TryWarp(() =>
                        {
                            if (!EvilwarpFailStartCooldown)
                            {
                                button.StartCoolDown();
                            }
                        });
                    }).WrapToIl2Cpp());*/
                };
                warpButton.OnEffectStart = (button) =>
                {
                    SoundManager.instance.PlaySound(PatchManager.GetSound("Executioner"), false, 1f);
                    MyPlayer.GainSpeedAttribute(0f, (float)(PreWarpTimeOption+WarpUseTimeOption), false, 100);
                    if (warpStartvanish)
                    {
                        MyPlayer.GainAttribute(PlayerAttributes.Invisible, PreWarpTimeOption + WarpUseTimeOption+2f, false, 0, "splicer:vanish");
                    }
                };
                warpButton.OnEffectEnd = (button) =>
                {
                    isuseskilling = false;
                    if (EvilwarpFailStartCooldown)
                    {
                        button.StartCoolDown();
                    }
                };
                warpButton.OnBroken = delegate (ModAbilityButton button)
                {
                    iswarping = false;
                    Snatcher.RewindKillCooldown();
                };
                warpButton.EffectTimer = NebulaAPI.Modules.Timer(this, (float)(PreWarpTimeOption + WarpUseTimeOption)).Start();
                warpButton.StartCoolDown();
                warpButton.SetLabel("warp");
                warpButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                new GuideLineAbility(MyPlayer, () => !warpButton.IsInCooldown && MyPlayer.CanMove && !MyPlayer.IsDead).Register(new FunctionalLifespan(() => !IsDeadObject && !warpButton.IsBroken));
                if (useWarpLeftMark)
                {
                    GameOperatorManager.Instance!.Subscribe<MeetingPreStartEvent>(ev =>
                    {
                        foreach (var mark in marks)
                        {
                            NebulaSyncObject.RpcDestroy(mark.ObjectId);
                        }
                    }, this);
                }
                GameOperatorManager.Instance!.Subscribe<MeetingStartEvent>(ev =>
                {
                    killnum = 0;
                }, this);
                NebulaAPI.CurrentGame?.KillButtonLikeHandler.Register(warpButton.GetKillButtonLike());
            }
        }
        [OnlyMyPlayer]
        void CheckKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (isuseskilling)
            {
                ev.SetAsCannotKillForcedly();
            }
        }
        private void TryWarp(Action Succescall)
        {
            float angle = PlayerControl.LocalPlayer.FlashlightAngle;
            Vector2 vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 truePos = PlayerControl.LocalPlayer.GetTruePosition();

            bool result = false;
            float maxDistance = warpMaxDistanceOption;
            float minDistance = maxDistance;
            int num = Physics2D.RaycastNonAlloc(truePos, vector, PhysicsHelpers.castHits, minDistance, Constants.ShipAndAllObjectsMask);
            for (int i = 0; i < num; i++)
            {
                if (PhysicsHelpers.castHits[i].collider.isTrigger) continue;

                result = true;
                float temp = (PhysicsHelpers.castHits[i].point - truePos).magnitude;
                if (temp < minDistance) minDistance = temp;
            }

            if (!result)
            {
                return;
            }

            float d = minDistance;
            var data = MapData.GetCurrentMapData();
            Vector2 tempVec;
            while (true)
            {
                d += 0.1f;
                if (d > maxDistance) break;

                tempVec = truePos + (vector * d);
                if (data.CheckMapArea(tempVec, 0.23f))
                {
                    //RPCEventInvoker.ObjectInstantiate(CustomObject.Type.TeleportEvidence, PlayerControl.LocalPlayer.GetTruePosition());
                    //PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(tempVec);
                    if (useWarpLeftMark)
                    {
                        TeleportEvidence mark = (NebulaSyncObject.RpcInstantiate(TeleportEvidence.MyGlobalTag, [PlayerControl.LocalPlayer.transform.localPosition.x, PlayerControl.LocalPlayer.transform.localPosition.y - 0.25f])?.SyncObject as TeleportEvidence)!;
                        marks.Add(mark);
                    }
                    Succescall();
                    TeleportationSystem.RpcTeleport.Invoke((MyPlayer, tempVec));
                    break;
                }
            }
        }
        bool iswarping;
        bool IPlayerAbility.EyesightIgnoreWalls => iswarping;
    }
    public class NiceAbility : AbstractPlayerUsurpableAbility, IPlayerAbility, IBindPlayer, IGameOperator, ILifespan
    {

        private ModAbilityButton? warpButton = null;
        static private Virial.Media.Image warpImage = NebulaAPI.AddonAsset.GetResource("WarpButton.png")!.AsImage(100f)!;
        int warpnum;
        AchievementToken<ValueTuple<bool, bool>>? acTokenAnother=null;
        [Local]
        [OnlyMyPlayer]
        private void OnDead(PlayerDieEvent ev)
        {
            if (this.acTokenAnother != null && base.MyPlayer.PlayerState == PlayerState.Exiled)
            {
                AchievementToken<ValueTuple<bool, bool>> achievementToken = this.acTokenAnother;
                achievementToken.Value.Item1 = achievementToken.Value.Item1 | this.acTokenAnother.Value.Item2;
            }
        }
        private void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (this.acTokenAnother != null)
            {
                this.acTokenAnother.Value.Item2 = false;
            }
        }
        bool warpafter;
        [Local]
        private void OnPlayerMurdered(PlayerMurderedEvent ev)
        {
            Vector2 vec;
            if (warpafter && !Helpers.AnyNonTriggersBetween(base.MyPlayer.ToAUPlayer().GetTruePosition(), ev.Dead.ToAUPlayer().GetTruePosition(), out vec, null) && vec.magnitude < 1.25f)
            {
                new StaticAchievementToken("nicesplicer.challenge1");
            }
        }
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt()];
        public NiceAbility(Virial.Game.Player player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                acTokenAnother=AbstractAchievement.GenerateSimpleTriggerToken("nicesplicer.another1");
                warpnum = 0;
                iswarping = false;
                isuseskilling = false;
                marks = new List<TeleportEvidence>();
                warpButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer,Virial.Compat.VirtualKeyInput.Ability, NicewarpCoolDownOption, "warp", warpImage, (button) => MyPlayer.CanMove&&!button.IsInEffect, (button) => !MyPlayer.IsDead, false).SetAsUsurpableButton(this);
                warpButton.OnClick = delegate (ModAbilityButton button)
                {
                    isuseskilling = true;
                    iswarping = false;
                    button.StartEffect();
                    PlayerControl.LocalPlayer.lightSource.StartCoroutine(WarpSystem.CoOrient(PlayerControl.LocalPlayer.lightSource, PreWarpTimeOption, WarpUseTimeOption,
                    (p) =>
                    {
                    }, () => { iswarping = true; }, () => { iswarping = false; }, () =>
                    {
                        TryWarp(() =>
                        {
                            new StaticAchievementToken("nicesplicer.common1");
                            warpnum++;
                            if (warpnum>=10)
                            {
                                new StaticAchievementToken("nicesplicer.common2");
                            }
                            if (acTokenAnother==null)
                            {
                                AchievementToken<ValueTuple<bool, bool>> achievementToken = this.acTokenAnother!;
                                achievementToken.Value.Item2 = true;
                            }
                            warpafter = true;
                            NebulaManager.Instance.StartDelayAction(5f, () =>
                            {
                                warpafter = false;
                            });
                            if (!NicewarpFailStartCooldown)
                            {
                                button.StartCoolDown();
                            }
                        });
                    }).WrapToIl2Cpp());
                };
                warpButton.OnEffectStart = (button) =>
                {
                    MyPlayer.GainSpeedAttribute(0f,(float)(PreWarpTimeOption+WarpUseTimeOption), false, 100);
                    if (warpStartvanish)
                    {
                        MyPlayer.GainAttribute(PlayerAttributes.Invisible, PreWarpTimeOption + WarpUseTimeOption + 2f, false, 0, "splicer:vanish");
                    }
                };
                warpButton.OnEffectEnd = (button) =>
                {
                    isuseskilling = false;
                    if (NicewarpFailStartCooldown)
                    {
                        button.StartCoolDown();
                    }
                };
                warpButton.OnBroken = delegate (ModAbilityButton button)
                {
                    iswarping = false;
                    Snatcher.RewindKillCooldown();
                };
                warpButton.EffectTimer = NebulaAPI.Modules.Timer(this, (float)(PreWarpTimeOption + WarpUseTimeOption)).Start();
                warpButton.StartCoolDown();
                warpButton.SetLabel("warp");
                warpButton.SetLabelType(ModAbilityButton.LabelType.Crewmate);
                new GuideLineAbility(MyPlayer, () => !warpButton.IsInCooldown && MyPlayer.CanMove && !MyPlayer.IsDead).Register(new FunctionalLifespan(() => !IsDeadObject && !warpButton.IsBroken));
                if (useWarpLeftMark)
                {
                    GameOperatorManager.Instance!.Subscribe<MeetingPreStartEvent>(ev =>
                    {
                        foreach (var mark in marks)
                        {
                            NebulaSyncObject.RpcDestroy(mark.ObjectId);
                        }
                    }, this);
                }
            }
        }
        bool isuseskilling;
        [OnlyMyPlayer]
        void CheckKill(PlayerCheckCanKillLocalEvent ev)
        {
            if (isuseskilling)
            {
                ev.SetAsCannotKillForcedly();
            }
        }
        private void TryWarp(Action Succescall)
        {
            float angle = PlayerControl.LocalPlayer.FlashlightAngle;
            Vector2 vector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 truePos = PlayerControl.LocalPlayer.GetTruePosition();

            bool result = false;
            float maxDistance = warpMaxDistanceOption;
            float minDistance = maxDistance;
            int num = Physics2D.RaycastNonAlloc(truePos, vector, PhysicsHelpers.castHits, minDistance, Constants.ShipAndAllObjectsMask);
            for (int i = 0; i < num; i++)
            {
                if (PhysicsHelpers.castHits[i].collider.isTrigger) continue;

                result = true;
                float temp = (PhysicsHelpers.castHits[i].point - truePos).magnitude;
                if (temp < minDistance) minDistance = temp;
            }

            if (!result)
            {
                return;
            }

            float d = minDistance;
            var data = MapData.GetCurrentMapData();
            Vector2 tempVec;
            while (true)
            {
                d += 0.1f;
                if (d > maxDistance) break;

                tempVec = truePos + (vector * d);
                if (data.CheckMapArea(tempVec, 0.23f))
                {
                    //RPCEventInvoker.ObjectInstantiate(CustomObject.Type.TeleportEvidence, PlayerControl.LocalPlayer.GetTruePosition());
                    //PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(tempVec);
                    if (useWarpLeftMark)
                    {
                        TeleportEvidence mark = (NebulaSyncObject.RpcInstantiate(TeleportEvidence.MyGlobalTag, [PlayerControl.LocalPlayer.transform.localPosition.x, PlayerControl.LocalPlayer.transform.localPosition.y - 0.25f])?.SyncObject as TeleportEvidence)!;
                        marks.Add(mark!);
                    }
                    Succescall();
                    TeleportationSystem.RpcTeleport.Invoke((MyPlayer, tempVec));
                    break;
                }
            }
        }
        List<TeleportEvidence> marks = new List<TeleportEvidence>();
        bool iswarping;
        bool IPlayerAbility.EyesightIgnoreWalls => iswarping;
        bool IPlayerAbility.IgnoreBlackout
        {
            get
            {
                if (WarpingIgnoreBlackout)
                {
                    return iswarping;
                }
                return false;
            }
        }
    }
    [HarmonyPatch]
    static public class WarpSystem
    {
        [HarmonyPatch(typeof(WideCamera), "DrawShadow", MethodType.Getter), HarmonyPostfix]
        public static void DrawShadow(ref bool __result)
        {
            if (GamePlayer.LocalPlayer == null || NebulaGameManager.Instance == null) return;
            if (GamePlayer.LocalPlayer.TryGetAbility<NiceAbility>(out var n) || GamePlayer.LocalPlayer.TryGetAbility<EvilAbility>(out var e))
            {
                if (NebulaGameManager.Instance!.IgnoreWalls)
                {
                    __result = true;
                }
            }
        }
        /*[HarmonyPatch(typeof(PlayerControl), "AdjustLighting"), HarmonyPrefix]
        public static bool AdjustLightingPatch(PlayerControl __instance)
        {
            if (PlayerControl.LocalPlayer != __instance) return false;

            float num = 0f;
            bool flashFlag = false;
            if (FlashlightEnabled) flashFlag = FlashlightEnabled;
            else if (__instance.IsFlashlightEnabled()) flashFlag = true;
            else if (__instance.lightSource.useFlashlight) flashFlag = true;

            if (__instance.IsFlashlightEnabled())
            {
                if (__instance.Data.Role.IsImpostor)
                    GameOptionsManager.Instance.CurrentGameOptions.TryGetFloat(FloatOptionNames.ImpostorFlashlightSize, out num);
                else
                    GameOptionsManager.Instance.CurrentGameOptions.TryGetFloat(FloatOptionNames.CrewmateFlashlightSize, out num);
            }
            else if (__instance.lightSource.useFlashlight)
            {
                num = __instance.lightSource.flashlightSize;
            }

            __instance.SetFlashlightInputMethod();
            __instance.lightSource.SetupLightingForGameplay(flashFlag, num, __instance.TargetFlashlight.transform);

            return false;
        }*/
        static private PlayerControl? SearchPlayer(float distance, float angle)
        {

            UnityEngine.Vector3 myPos = PlayerControl.LocalPlayer.transform.position;
            float lightAngle = PlayerControl.LocalPlayer.lightSource.GetFlashlightAngle();

            PlayerControl? result = null;
            float resultNum = 0f;

            foreach (var p in PlayerControl.AllPlayerControls.GetFastEnumerator())
            {
                if (p.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (p.Data.IsDead) continue;

                UnityEngine.Vector3 dis = (p.transform.position - myPos);
                float mag = dis.magnitude;
                float ang = Mathf.Abs(Mathf.Atan2(dis.y, dis.x) - lightAngle);

                while (ang > Mathf.PI * 2f) ang -= Mathf.PI * 2f;

                if (mag < distance && ang < angle && (result == null || ang + Mathf.Abs(mag - distance) < resultNum))
                {
                    result = p;
                    resultNum = ang + Mathf.Abs(mag - distance);
                }
            }
            return result;
        }

        static public System.Collections.IEnumerator CoOrient(LightSource light, float preDuration, float duration, Action<float> inSerchFunc,
            Action FlashEnable, Action FlashDisable, Action finalFunc)
        {
            if (ShipStatus.Instance)
            {
                ShipStatus.Instance.MaxLightRadius = 5f;
            }
            float t;

            t = 0f;
            while (t < preDuration)
            {
                float p = t / preDuration;
                if (ShipStatus.Instance) ShipStatus.Instance.MaxLightRadius = Mathf.Max(light.LightCutawayMaterial.GetFloat("_PlayerRadius"), 5f - p * p * 5f);

                t += Time.deltaTime;

                yield return null;
            }
            PDebug.Log("Enable FlashLight");
            light.SetFlashlightEnabled(true);
            PatchManager.SetFlashLight(true);
            FlashEnable();
            if (ShipStatus.Instance) ShipStatus.Instance.MaxLightRadius = 5f;
            t = 0f;
            float dis = light.viewDistance;
            PDebug.Log("SetLight");
            //Game.GameData.data.myData.Vision.Register(new Game.VisionFactor(duration, 1.15f));
            GamePlayer.LocalPlayer!.GainAttribute(PlayerAttributes.Eyesight, duration, 1.15f, false, 100);
            while (t < duration)
            {
                float p = t / duration;
                float invp = 1f - p;
                //light.LightCutawayMaterial.SetFloat("_PlayerRadius", dis * ((1f - invp * invp) * 1.2f - 0.3f));
                /*light.LightCutawayMaterial.SetFloat("_LightRadius", light.ViewDistance);
                light.LightCutawayMaterial.SetVector("_LightOffset", light.LightOffset);
                light.LightCutawayMaterial.SetFloat("_FlashlightSize", light.FlashlightSize);
                light.LightCutawayMaterial.SetFloat("_FlashlightAngle", PlayerControl.LocalPlayer.FlashlightAngle);
                UnityEngine.Vector3 position = light.transform.position;
                position.z -= 7f;
                light.lightChild.transform.position = position;
                light.renderer.Render(position);*/
                if (ShipStatus.Instance) ShipStatus.Instance.MaxLightRadius = 1f + p * 12f;
                light.flashlightSize = p * p * 0.15f;
                t += Time.deltaTime;
                inSerchFunc(p);
                yield return null;
            }
            finalFunc();
            PDebug.Log("Disable FlashLight");
            yield return new WaitForSeconds(0.5f);
            t = 0f;
            if (ShipStatus.Instance)
            {
                ShipStatus.Instance.MaxLightRadius = 5f;
            }
            FlashDisable();
            light.SetFlashlightEnabled(false);
            PatchManager.SetFlashLight(false);
            while (t < 0.15f)
            {
                float p = t / 0.15f;
                if (ShipStatus.Instance) ShipStatus.Instance.MaxLightRadius = 2f + (1f - (1 - p) * (1 - p) * (1 - p)) * 3f;

                t += Time.deltaTime;

                yield return null;
            }
            if (ShipStatus.Instance)
            {
                ShipStatus.Instance.MaxLightRadius = 5f;
            }
        }

        static public System.Collections.IEnumerator CoOrient(LightSource light, float preDuration, float duration, Action<PlayerControl?> nearbyPlayerFunc, Action FlashEnable, Action FlashDisable, Action<PlayerControl?> finalPlayerFunc)
        => CoOrient(light, preDuration, duration,
            (p) => nearbyPlayerFunc(SearchPlayer(3f + p * 2.5f, 0.1f + p * 0.2f)), FlashEnable, FlashDisable,
            () => finalPlayerFunc(SearchPlayer(4f, 0.3f)));
    }
    public class TeleportEvidence : NebulaSyncStandardObject, IGameOperator
    {
        public TeleportEvidence(Vector2 pos)
            : base(pos, NebulaSyncStandardObject.ZOption.Back, true,Evidence.GetSprite(), false)
        {
        }
        static TeleportEvidence()
        {
            NebulaSyncObject.RegisterInstantiater(MyGlobalTag, (float[] args) => new TeleportEvidence(new Vector2(args[0], args[1])));
        }
        public const string MyGlobalTag = "WarpMarkGlobal";
        public static Virial.Media.Image Evidence = NebulaAPI.AddonAsset.GetResource("TeleportEvidence.png")!.AsImage()!;
    }
}
