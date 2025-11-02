using Plana.Core;

namespace Plana.Roles.Impostor;

public class Crow : DefinedRoleTemplate, DefinedRole,HasCitation
{
    private Crow() : base("crow", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [CoolDownOption,skilltimeOption,MarkCooldownOption,teleportCooldownOption,OtherImpostorVisionBlockOption,ActiveExileMessageBlockOption,OtherImpostorCanSeeExileMessageOption,UseSkillOtherImpostorCanSeeOption]) { }
    Citation HasCitation.Citation => PCitations.PlanaANDKC;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    
    static public readonly FloatConfiguration CoolDownOption = NebulaAPI.Configurations.Configuration("options.role.crow.skillCoolDown", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static public readonly FloatConfiguration skilltimeOption = NebulaAPI.Configurations.Configuration("options.role.crow.skilltime", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static public readonly FloatConfiguration MarkCooldownOption = NebulaAPI.Configurations.Configuration("options.role.crow.markCoolDown", (0f, 60f, 2.5f), 5f, FloatConfigurationDecorator.Second);
    static public readonly FloatConfiguration teleportCooldownOption = NebulaAPI.Configurations.Configuration("options.role.crow.teleportCoolDown", (0f, 60f, 2.5f), 15f, FloatConfigurationDecorator.Second);
    static public readonly BoolConfiguration OtherImpostorVisionBlockOption = NebulaAPI.Configurations.Configuration("options.role.crow.otherimpblock",false);
    static public readonly BoolConfiguration UseSkillOtherImpostorCanSeeOption = NebulaAPI.Configurations.Configuration("options.role.crow.useskillotherimpcansee", false);
    static public readonly BoolConfiguration ActiveExileMessageBlockOption = NebulaAPI.Configurations.Configuration("options.role.crow.activeexilemessage",true);
    static public readonly BoolConfiguration OtherImpostorCanSeeExileMessageOption = NebulaAPI.Configurations.Configuration("options.role.crow.otherimpcanseeexile", true,()=>ActiveExileMessageBlockOption);

    static public Crow MyRole = new Crow();
    public static void LoadPatch(Harmony harmony)
    {
        PDebug.Log("Crow Patch");
        harmony.Patch(typeof(ExileController).GetMethod("Begin"), null, new HarmonyMethod(typeof(Crow.Instance).GetMethod("ExileControllerBegin")));
        PDebug.Log("Done");
    }
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;

        private ModAbilityButton? skillButton = null;
        static private Virial.Media.Image bimage = NebulaAPI.AddonAsset.GetResource("crowbutton.png")!.AsImage(100f)!,markImage=NebulaAPI.AddonAsset.GetResource("crowMark.png")!.AsImage(100f)!,tpImage=NebulaAPI.AddonAsset.GetResource("crowTP.png")!.AsImage(100f)!;
        static private Virial.Media.Image Skillimage = NebulaAPI.AddonAsset.GetResource("croweffect.png")!.AsImage(100f)!;
        public static string GenerateSecureRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";
            var result = new StringBuilder(length);
            var bytes = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            for (int i = 0; i < length; i++)
            {
                result.Append(chars[bytes[i] % chars.Length]);
            }

            return result.ToString();
        }
        public static void ExileControllerBegin(ExileController __instance, ref ExileController.InitProperties init)
        {
            if (!ActiveExileMessageBlockOption)
            {
                return;
            }
            bool hascrow = false;
            foreach (GamePlayer player in NebulaGameManager.Instance!.AllPlayerInfo)
            {
                if (player==null||player.IsDead||player.IsDisconnected)
                {
                    continue;
                }
                if (player.Role is Crow.Instance)
                {
                    hascrow = true;
                    break;
                }
            }
            if (hascrow)
            {
                if (PlayerControl.LocalPlayer!=null)
                {
                    if (PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        return;
                    }
                    var p = PlayerControl.LocalPlayer.ToNebulaPlayer();
                    if (p!=null&&p.IsImpostor&&OtherImpostorCanSeeExileMessageOption)
                    {
                        return;
                    }
                }
                string text = $"{GenerateSecureRandomString(UnityEngine.Random.Range(5, 16))} {GenerateSecureRandomString(UnityEngine.Random.Range(5, 11))}";
                if (init.networkedPlayer != null)
                {
                    Player player = NebulaGameManager.Instance.GetPlayer(init.networkedPlayer.PlayerId)!;
                    RuntimeRole role = ((player != null) ? player.Role : null)!;
                    if (role != null)
                    {
                        __instance.completeString = text + " " + Language.Translate("role.crow." + role.Role.Category.ToString());
                    }
                    else
                    {
                        __instance.completeString = text;
                    }
                }
                else
                {
                    __instance.completeString = text;
                }
                __instance.ImpostorText.text = GenerateSecureRandomString(UnityEngine.Random.Range(7, 17));
            }
        }
        static SpriteRenderer? renderer;
        static bool OutAlpha;
        RemoteProcess<bool> SkillRpc = new RemoteProcess<bool>("CrowSkill", delegate (bool message,bool _)
        {
            if (GamePlayer.LocalPlayer == null||GamePlayer.LocalPlayer.IsDead)
            {
                return;
            }
            if (GamePlayer.LocalPlayer.Role is Crow.Instance)
            {
                return;
            }
            if (UseSkillOtherImpostorCanSeeOption&&GamePlayer.LocalPlayer.IsImpostor&&message)
            {
                Game currentGame = NebulaAPI.CurrentGame!;
                if (currentGame != null)
                {
                    TitleShower module = currentGame.GetModule<TitleShower>()!;
                    if (module != null)
                    {
                        module.SetText(Language.Translate("role.crow.useskilltext"),MyRole.RoleColor.ToUnityColor(),1f);
                        var t=module.GetPrivateField<Transform>("textHolder");
                        t.localScale = UnityEngine.Vector2.one / 2;
                    }
                    AmongUsUtil.PlayCustomFlash(MyRole.RoleColor.ToUnityColor(), 0f, 0.25f, 0.4f, 0f);
                }
            }
            if (!OtherImpostorVisionBlockOption&&GamePlayer.LocalPlayer.IsImpostor)
            {
                return;
            }
            if (renderer==null)
            {
                renderer= UnityHelper.CreateObject<SpriteRenderer>("cblockvision", HudManager.Instance.transform, UnityEngine.Vector3.zero, new int?(LayerExpansion.GetUILayer()));
                renderer.sprite = (Skillimage as SpriteLoader)!.GetSprite();
                renderer.transform.localPosition = new UnityEngine.Vector3(0f, 0f, 0.1f);
                renderer.transform.localScale = UnityEngine.Vector3.one * 0.419f;
            }
            /*if (message)
            {
                renderer.color = new UnityEngine.Color(1f, 1f, 1f, 1f);
            }
            else
            {
                OutAlpha = true;
                progressing = 0f;
            }*/
            if (!message)
            {
                SoundManager.instance.PlaySound(PatchManager.GetSound("crowEnvelopingOver"), false, 1f);
            }
            renderer.gameObject.SetActive(message);
            /*NebulaGameManager instance = NebulaGameManager.Instance;
            if (instance != null)
            {
                foreach (List<Il2CppArgument<HudContent>> hclist in instance.HudGrid.Contents)
                {
                    if (hclist==null||hclist.Count<=0)
                    {
                        continue;
                    }
                    foreach (Il2CppArgument<HudContent> hc in hclist)
                    {
                        if (hc.Value != null)
                        {
                            hc.Value.gameObject.GetComponent<ActionButton>().graphic.color = new UnityEngine.Color(1f, 1f, 1f, 0.4f);
                        }
                    }
                }
            }*/
        });
        static float progressing;
        void Update(GameUpdateEvent ev)
        {
            if (renderer!=null&&OutAlpha)
            {
                if (progressing>=2f)
                {
                    progressing = 2f;
                    renderer.color = new UnityEngine.Color(1f, 1f, 1f, 0f);
                    renderer.gameObject.SetActive(false);
                    OutAlpha = false;
                    return;
                }
                progressing += Time.deltaTime;
                renderer.color = new UnityEngine.Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, progressing / 2f));
            }
        }
        [Local,OnlyMyPlayer]
        void OnKill(PlayerKillPlayerEvent ev)
        {
            if (aftertp)
            {
                new StaticAchievementToken("crow.common2");
            }
        }
        public Instance(GamePlayer player) : base(player)
        {
        }
        NebulaSyncStandardObject? mark;
        bool aftertp;
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                aftertp = false;
                skillButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability);
                skillButton.Availability = (button) => MyPlayer.CanMove;
                skillButton.Visibility = (button) => !MyPlayer.IsDead;
                skillButton.SetLabel("crow");
                skillButton.SetImage(bimage);
                skillButton.OnClick = (button) =>
                {
                    button.StartEffect();
                };
                skillButton.OnEffectStart = (button) =>
                {
                    SkillRpc.Invoke(true);
                    new StaticAchievementToken("crow.common1");
                };
                skillButton.OnEffectEnd = (button) =>
                {
                    SkillRpc.Invoke(false);
                    button.StartCoolDown();
                };
                GameOperatorManager.Instance?.Subscribe<MeetingPreStartEvent>(p =>
                {
                    SkillRpc.Invoke(false);
                }, this, 100);
                GameOperatorManager.Instance?.Subscribe<PlayerDieEvent>(ev =>
                {
                    if (ev.Player.AmOwner)
                    {
                        SkillRpc.Invoke(false);
                    }
                }, this);
                GameOperatorManager.Instance?.RegisterOnReleased(() =>
                {
                    if (skillButton.IsInEffect)
                    {
                        SkillRpc.Invoke(false);
                    }
                }, skillButton);
                skillButton.EffectTimer = NebulaAPI.Modules.Timer(this, skilltimeOption);
                skillButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, CoolDownOption).SetAsAbilityTimer().Start();
                skillButton.StartCoolDown();
                skillButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                mark = null!;
                ModAbilityButton markButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, VirtualKeyInput.SidekickAction, null, MarkCooldownOption, "mark", markImage, (ModAbilityButton _) =>true, (button)=>!MyPlayer.IsDead, false);
                markButton.OnClick = delegate (ModAbilityButton button)
                {
                    if (mark!=null)
                    {
                        NebulaSyncStandardObject.LocalDestroy(mark.ObjectId);
                    }
                    mark = (NebulaSyncObject.LocalInstantiate("CannonMark", new float[]
                    {
                    PlayerControl.LocalPlayer.transform.localPosition.x,
                    PlayerControl.LocalPlayer.transform.localPosition.y - 0.25f
                    }).SyncObject as NebulaSyncStandardObject)!;
                    button.StartCoolDown();
                };
                markButton.SetLabelType(ModAbilityButton.LabelType.Utility);
                ModAbilityButton tpButton = NebulaAPI.Modules.AbilityButton(this, base.MyPlayer, VirtualKeyInput.SecondaryAbility, null, teleportCooldownOption, "cteleport", tpImage, (ModAbilityButton _) => mark!=null, (button) => !MyPlayer.IsDead, false);
                tpButton.OnClick = delegate (ModAbilityButton button)
                {
                    var myplayer = MyPlayer.ToAUPlayer();
                        myplayer.MyPhysics.ResetMoveState(true);
                    UnityEngine.Vector2 TempPosition = mark!.Position;
                        myplayer.NetTransform.SnapTo(new UnityEngine.Vector2(TempPosition.x, TempPosition.y + 0.3636f));
                    button.StartCoolDown();
                    aftertp = true;
                    NebulaManager.Instance.StartDelayAction(10f, () => { aftertp = false; });
                };
                tpButton.SetLabelType(ModAbilityButton.LabelType.Utility);
                markButton.StartCoolDown();
                tpButton.StartCoolDown();
            }
        }
    }
}
