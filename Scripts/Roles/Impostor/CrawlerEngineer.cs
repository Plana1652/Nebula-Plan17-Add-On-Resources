using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Injection;
using Nebula.Modules.Cosmetics;
using Nebula.Roles.Complex;
using Plana.Core;
using Virial.Events.Game.Minimap;

namespace Plana.Roles.Impostor;

public class CrawlerEngineer : DefinedSingleAbilityRoleTemplate<CrawlerEngineer.Ability>, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder, HasCitation
{
    private CrawlerEngineer() : base("crawlerengineer", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [CrawlerHasWatching,CrawlerNoCamouflager,PatchManager.ImpostorMeetingChat])
    {
    }
    static public readonly BoolConfiguration CrawlerHasWatching = NebulaAPI.Configurations.Configuration("role.crawlerengineer.haswatching", true);
    static public readonly BoolConfiguration CrawlerNoCamouflager= NebulaAPI.Configurations.Configuration("role.crawlerengineer.nocamouflager", false);
    Citation HasCitation.Citation => PCitations.PlanaANDKC;
    public override CrawlerEngineer.Ability CreateAbility(Player player, int[] arguments)
    {
        return new CrawlerEngineer.Ability(player, arguments.GetAsBool(0));
    }
    public static void LoadPatch(Harmony harmony)
    {
        PDebug.Log("CrawlerEngineer Patch");
        harmony.Patch(typeof(PlayerModInfo).GetMethod("AddOutfit"), new HarmonyMethod(typeof(CrawlerEngineer.Ability).GetMethod("BlockAddOutfit")));
        PDebug.Log("Done");
    }
        static public CrawlerEngineer MyRole = new CrawlerEngineer();
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility, IBindPlayer, IGameOperator, ILifespan
    {
        private ModAbilityButton? skillButton = null;
        Virial.Media.Image MeetingSkill = NebulaAPI.AddonAsset.GetResource("CrawlerMeetingSkill.png")!.AsImage(100f)!;
        Virial.Media.Image skillImage = NebulaAPI.AddonAsset.GetResource("CrawlerUsePadSkill.png")!.AsImage(100f)!;
        ShowPlayersMapLayer? mapCountLayer;
        void OnGameStarted(GameStartEvent ev)
        {
            MarkedPlayer = new Dictionary<byte, int>();
        }
        public static bool BlockAddOutfit(GamePlayer __instance,OutfitCandidate outfit)
        {
            if (CrawlerNoCamouflager)
            {
                if (GamePlayer.LocalPlayer!.Role.GetAbility<CrawlerEngineer.Ability>()==null)
                {
                    return true;
                }
                if (outfit.Tag.ToLower().Contains("camo") || outfit.Tag.ToLower().Contains("commscfeffect"))
                {
                    if (__instance.Role.GetAbility<CrawlerEngineer.Ability>()!=null)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return true;
        }
        void UpdateNameText(PlayerDecorateNameEvent ev)
        {
            if (GamePlayer.LocalPlayer!.IsImpostor&&!GamePlayer.LocalPlayer.IsDead)
            {
                if (ev.Player.IsImpostor)
                {
                    if (ev.Player.AmOwner)
                    {
                        return;
                    }
                    string text = ev.Name.Replace(ev.Player.ToAUPlayer().name, "");
                    foreach (var modifier in ev.Player.Modifiers)
                    {
                        if (modifier.Modifier.LocalizedName.Contains("guesser"))
                        {
                            modifier.DecorateNameConstantly(ref text, true);
                        }
                        else if (modifier.Modifier.LocalizedName.Contains("watching"))
                        {
                            modifier.DecorateNameConstantly(ref text, true);
                        }
                        else if (modifier.Modifier.LocalizedName.Contains("tieBreaker"))
                        {
                            modifier.DecorateNameConstantly(ref text, true);
                        }
                    }
                    ev.Name += text;
                }
            }
        }
        void ReflectRoleName(PlayerSetFakeRoleNameEvent ev)
        {
            if (GamePlayer.LocalPlayer!.IsImpostor)
            {
                if (ev.Player.IsImpostor)
                {
                    string text = ev.Player.Role.DisplayColoredName;
                    foreach (var modifier in ev.Player.Modifiers)
                    {
                        if (modifier.Modifier.LocalizedName.Contains("jailer"))
                        {
                            var newtext = modifier.OverrideRoleName(text, false);
                            if (newtext != null)
                            {
                                text = newtext;
                            }
                            break;
                        }
                    }
                    if (MarkedPlayer.TryGetValue(ev.Player.PlayerId, out int rid))
                    {
                        text+=" "+Language.Translate("role.crawlerMarkPlayerText").Replace("%ROLE%", Nebula.Roles.Roles.AllRoles[rid].DisplayColoredName);
                    }
                    ev.Set(text);
                    return;
                }
                if (AmOwner&&ev.Player.IsModMadmate())
                {
                    ev.Append(Language.Translate("role.madmate.name").Color(Palette.ImpostorRed));
                }
                if (MarkedPlayer.TryGetValue(ev.Player.PlayerId,out int id))
                {
                    ev.Set(" "+Language.Translate("role.crawlerMarkPlayerText").Replace("%ROLE%", Nebula.Roles.Roles.AllRoles[id].DisplayColoredName));
                }
            }
        }
        MetaScreen? LastWindow;
        static TextAttributeOld ButtonAttribute = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new(1.3f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(2f, 1f, 2f);
        static public MetaScreen OpenRoleSelectWindow(Func<DefinedRole, bool> predicate, string underText, Action<DefinedRole> onSelected)
        {
            var window = MetaScreen.GenerateWindow(new UnityEngine.Vector2(7.6f, 4.2f), HudManager.Instance.transform, new UnityEngine.Vector3(0, 0, -50f), true, false);
            MetaWidgetOld widget = new();
            MetaWidgetOld inner = new();
            inner.Append(Nebula.Roles.Roles.AllRoles.Where(predicate), r => new MetaWidgetOld.Button(() => onSelected.Invoke(r), ButtonAttribute) { RawText = r.DisplayColoredName, PostBuilder = (_, renderer, _) => renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask }, 4, -1, 0, 0.59f);
            MetaWidgetOld.ScrollView scroller = new(new(6.9f, 3.8f), inner, true) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
            widget.Append(scroller);
            widget.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttr) { MyText = new RawTextComponent(underText), Alignment = IMetaWidgetOld.AlignmentOption.Center });
            window.SetWidget(widget);
            System.Collections.IEnumerator CoCloseOnResult()
            {
                while (MeetingHud.Instance.state != MeetingHud.VoteStates.Results) yield return null;
                window.CloseScreen();
            }
            window.StartCoroutine(CoCloseOnResult().WrapToIl2Cpp());
            return window;
        }
        static public MetaScreen OpenWindow(Action<DefinedRole> onSelected)
        {
            return OpenRoleSelectWindow(r =>true,Language.Translate("role.crawlerEngineer.Marktarget"), onSelected);
        }
        static Dictionary<byte, int> MarkedPlayer = new Dictionary<byte, int>();
        RemoteProcess<ValueTuple<byte,int>> AddMarkPlayerRpc = new RemoteProcess<ValueTuple<byte,int>>("markPlayer",delegate(ValueTuple<byte,int> message,bool _)
        {
            MarkedPlayer.Add(message.Item1, message.Item2);
        },true);
        void OnMeetingStart(MeetingStartEvent ev)
        {
                MarkedPlayer = new Dictionary<byte, int>();
            if (AmOwner)
            {
                NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(MeetingSkill,
                state =>
                {
                    var p = state.MyPlayer;
                    LastWindow = OpenWindow((r) =>
                    {
                        try
                        {
                            if (PlayerControl.LocalPlayer.Data.IsDead) return;
                            if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;
                            AddMarkPlayerRpc.Invoke(new ValueTuple<byte, int>(p.PlayerId, r.Id));
                            if (LastWindow) LastWindow!.CloseScreen();
                            LastWindow = null!;
                        }
                        catch (Exception e)
                        {
                            PDebug.Log(e);
                        }
                    }
                    );
                },
                p => !p.MyPlayer.IsDead && !p.MyPlayer.AmOwner&&!MarkedPlayer.TryGetValue(p.MyPlayer.PlayerId,out int roleid) && !PlayerControl.LocalPlayer.Data.IsDead
                ));
            }
        }

        [Local]
        private void OnOpenMap(AbstractMapOpenEvent ev)
        {
            try
            {
                if (MyPad != null && !(ev is MapOpenAdminEvent) && !base.IsUsurped && !MeetingHud.Instance && !base.MyPlayer.IsDead)
                {
                    if (!this.mapCountLayer)
                    {
                        this.mapCountLayer = UnityHelper.CreateObject<ShowPlayersMapLayer>("CountLayer", MapBehaviour.Instance.transform, new UnityEngine.Vector3(0f, 0f, -1f), null);
                        this.BindGameObject(this.mapCountLayer.gameObject);
                        this.mapCountLayer.SetUp(p => true, null!, true,this);
                    }
                    this.mapCountLayer!.gameObject.SetActive(true);
                    MapBehaviour.Instance?.ColorControl?.SetColor(new UnityEngine.Color32(171,0,126, 255));
                    return;
                }
                if (this.mapCountLayer)
                {
                    this.mapCountLayer!.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        bool useMap;
        bool kill;
        [Local]
        void OnKill(PlayerMurderedEvent ev)
        {
            if (ev.Murderer.AmOwner)
            {
                kill = true;
            }
        }
        [Local]
        void OnExiled(PlayerExiledEvent ev)
        {
            if (kill &&ev.Player.AmOwner)
            {
                new StaticAchievementToken("crawlerengineer.another1");
            }
        }
        [Local]
        void OnTaskRestart(TaskPhaseRestartEvent ev)
        {
            kill = false;
        }
        public Ability(Player player, bool isUsurped) : base(player, isUsurped)
        {
            if (AmOwner)
            {
                useMap = false;
                kill = false;
                skillButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability);
                skillButton.Availability = (button) => MyPlayer.CanMove;
                skillButton.Visibility = (button) => !MyPlayer.IsDead;
                skillButton.SetLabel("equip");
                skillButton.SetImage(skillImage!);
                skillButton.OnClick = (button) =>
                {
                    useMap = true;
                    if (MyPad == null)
                    {
                        new StaticAchievementToken("crawlerengineer.common1");
                    }
                    button.SetLabel(MyPad == null ? "unequip" : "equip");
                    RpcEquip.Invoke(new ValueTuple<byte, bool>(MyPlayer.PlayerId, MyPad== null));
                };
                skillButton.OnBroken = delegate (ModAbilityButton button)
                {
                    if (MyPad != null)
                    {
                        button.SetLabel("equip");
                        RpcEquip.Invoke(new ValueTuple<byte, bool>(base.MyPlayer.PlayerId, false));
                    }
                };
                skillButton.SetLabelType(ModAbilityButton.LabelType.Impostor);
                GameOperatorManager instance = GameOperatorManager.Instance!;
                if (instance != null)
                {
                    instance.Subscribe<MeetingStartEvent>(delegate (MeetingStartEvent ev)
                    {
                        skillButton.SetLabel("equip");
                        if (MyPad != null)
                        {
                            RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                        }
                    }, this, 100);
                    instance.Subscribe<PlayerMurderedEvent>(ev =>
                    {
                        if (ev.Murderer.AmOwner && ev.Player.PlayerState == PlayerState.Guessed&&useMap)
                        {
                            new StaticAchievementToken("crawlerengineer.challenge1");
                        }
                    }, this);
                    instance.Subscribe<MeetingEndEvent>(ev =>
                    {
                        useMap = false;
                    }, this);
                    instance.Subscribe<PlayerDieEvent>(ev =>
                    {
                        if (ev.Player.AmOwner)
                        {
                            if (MyPad != null)
                            {
                                RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                            }
                        }
                    }, this);
                    instance.RegisterOnReleased(() =>
                    {
                        if (MyPad != null)
                        {
                            RpcEquip.Invoke(new ValueTuple<byte, bool>(this.MyPlayer.PlayerId, false));
                        }
                    }, skillButton);
                }
            }
        }
        private void EquipBubblegun()
        {
            MyPad = new CrawlerPad(base.MyPlayer).Register(this);
        }

        private void UnequipBubblegun()
        {
            if (MyPad!=null)
            {
                MyPad.UnUsePad();
            }
            MyPad = null!;
        }
        public CrawlerPad MyPad
        {
            get; private set;
        } = null!;
        static readonly RemoteProcess<(byte playerId, bool equip)> RpcEquip = new(
        "EquipPad",
        (message, _) =>
        {
            var role = GamePlayer.GetPlayer(message.playerId)?.Role;
            var ability = role!.GetAbility<CrawlerEngineer.Ability>();
            if (ability != null)
            {
                if (message.equip)
                    ability.EquipBubblegun();
                else
                    ability.UnequipBubblegun();
            }
        }
        );
    }
    public class CrawlerPad : EquipableAbility
    {
        Virial.Media.MultiImage PadSprite = NebulaAPI.AddonAsset.GetResource("CrawlerPad.png")!.AsMultiImage(4,2, 100f)!;
        protected override float Size => 0.68f;
        protected override float Distance => 0.7f;
        public CrawlerPad(GamePlayer owner) : base(owner, false, "CrawlerPad")
        {
            animing = false;
            spriteIndex = 4;
            NebulaManager.Instance.StartCoroutine(CoAppear().WrapToIl2Cpp());
        }
        protected override void HudUpdate(GameHudUpdateEvent ev)
        {
            return;
        }
        int spriteIndex;
        bool animing;
        float updateSpriteTime;
        void OnUpdate(GameUpdateEvent ev)
        {
            if (animing)
            {
                return;
            }
            updateSpriteTime += Time.deltaTime;
            if (updateSpriteTime >= 0.15f)
            {
                Renderer.sprite = PadSprite.AsLoader(spriteIndex).GetSprite();
                spriteIndex++;
                if (spriteIndex >= 8)
                {
                    spriteIndex = 4;
                }
                updateSpriteTime = 0f;
            }
        }
        System.Collections.IEnumerator CoAppear()
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
        }
    }
    public class ShowPlayersMapLayer : MonoBehaviour
    {
        static ShowPlayersMapLayer()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ShowPlayersMapLayer>();
        }
        bool showDead;
        CrawlerEngineer.Ability? role;
        public void SetUp(Predicate<IPlayerlike> showPredicate, Action<int> postAction, bool showdead,CrawlerEngineer.Ability role)
        {
            this.showPredicate = showPredicate;
            this.postShownAction = postAction;
            showDead = showdead;
            this.role = role;
        }
        Dictionary<byte, SpriteRenderer>? mapIcons;
        void OnEnable()
        {
            if (mapIcons!=null)
            {
                foreach (var m in mapIcons.Values)
                {
                    m.gameObject.SetActive(true);
                }
            }
        }
        void OnDisable()
        {
            if (mapIcons != null)
            {
                foreach (var m in mapIcons.Values)
                {
                    m.gameObject.SetActive(false);
                }
            }
        }
        void initializeIcons(MapBehaviour __instance, PlayerControl pc = null!)
        {
            List<PlayerControl> list = new List<PlayerControl>();
            if (pc == null)
            {
                mapIcons = new Dictionary<byte, SpriteRenderer>();
                foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls)
                {
                    list.Add(playerControl);
                }
            }
            else
            {
                list.Add(pc);
            }
            foreach (PlayerControl playerControl2 in list)
            {
                byte playerId = playerControl2.PlayerId;
                mapIcons![playerId] = UnityEngine.Object.Instantiate<SpriteRenderer>(__instance.HerePoint, __instance.HerePoint.transform.parent);
                PlayerMaterial.SetColors(DynamicPalette.PlayerColors[playerControl2.ToNebulaPlayer().CurrentOutfit.outfit.ColorId], mapIcons[playerId]);
            }
        }
        public void Start()
        {
            if (mapIcons == null)
            {
                initializeIcons(MapBehaviour.Instance, null!);
            }
            foreach (byte b in mapIcons!.Keys)
            {
                NetworkedPlayerInfo playerById = GameData.Instance.GetPlayerById(b);
                PlayerMaterial.SetColors(DynamicPalette.PlayerColors[playerById.Object.ToNebulaPlayer().CurrentOutfit.outfit.ColorId], mapIcons[b]);
                mapIcons[b].enabled = !playerById.IsDead;
            }
            IconPool = new Nebula.Utilities.ObjectPool<SpriteRenderer>(ShipStatus.Instance.MapPrefab.HerePoint, base.transform);
            DeadIconPool = new Nebula.Utilities.ObjectPool<SpriteRenderer>(ShipStatus.Instance.MapPrefab.HerePoint, base.transform);
            DeadIconPool.OnInstantiated = delegate (SpriteRenderer icon)
            {
                PlayerMaterial.SetColors(UnityEngine.Color.gray, icon);
            };
        }

        public void FixedUpdate()
        {
            try
            {
                if (!ShipStatus.Instance)
                {
                    return;
                }
                if (role!=null)
                {
                    if (role.MyPad == null)
                    {
                        OnDisable();
                        return;
                    }
                    else
                    {
                        OnEnable();
                    }
                }
                foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls)
                {
                    if (playerControl != null)
                    {
                        byte playerId = playerControl.PlayerId;
                        if (mapIcons!.ContainsKey(playerId))
                        {
                            if (!playerControl.Data.IsDead)
                            {
                                UnityEngine.Vector3 vector = playerControl.transform.position;
                                vector /= ShipStatus.Instance.MapScale;
                                vector.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
                                vector.z = -1f;
                                mapIcons[playerId].transform.localPosition = vector;
                                mapIcons[playerId].enabled = true;
                            }
                            else
                            {
                                if (playerControl.Data.Disconnected)
                                {
                                    mapIcons[playerId].gameObject.SetActive(false);
                                }
                                mapIcons[playerId].gameObject.SetActive(false);
                                foreach (var deadbody in GameObject.FindObjectsByType<DeadBody>(FindObjectsSortMode.None))
                                {
                                    UnityEngine.Vector3 vector = deadbody.transform.position;
                                    vector /= ShipStatus.Instance.MapScale;
                                    vector.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
                                    vector.z = -1f;
                                    mapIcons[playerId].transform.localPosition = vector;
                                    PlayerMaterial.SetColors(UnityEngine.Color.gray, mapIcons[playerId]);
                                    mapIcons[playerId].enabled = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }

        public ShowPlayersMapLayer()
        {
        }

        private Nebula.Utilities.ObjectPool<SpriteRenderer>? IconPool;
        private Nebula.Utilities.ObjectPool<SpriteRenderer>? DeadIconPool;
        private Predicate<IPlayerlike>? showPredicate;
        private Action<int>? postShownAction;
    }
}
