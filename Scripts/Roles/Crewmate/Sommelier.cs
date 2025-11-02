using Nebula.Patches;
using Nebula.Roles.Scripts;
using Plana.Core;
using Plana.Roles.Modifier;

namespace Plana.Roles.Crewmate;

public class Sommelier : DefinedSingleAbilityRoleTemplate<Sommelier.Ability>, HasCitation, DefinedRole, DefinedSingleAssignable, DefinedCategorizedAssignable, DefinedAssignable, IRoleID, ISpawnable, RuntimeAssignableGenerator<RuntimeRole>, IGuessed, AssignableFilterHolder
{

    private Sommelier() : base("sommelier", new(194,60,60), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [MarkCoolDownOption,MarkDurationOption,UseSkillNum]) { }
    Citation? HasCitation.Citation => PCitations.PlanaANDKC;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;
    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0),arguments.Get(1,0));
    private static FloatConfiguration MarkCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.sommelier.markedCooldown", new ValueTuple<float, float, float>(0f, 30f, 2.5f), 7.5f, FloatConfigurationDecorator.Second, null, null);
    private static FloatConfiguration MarkDurationOption = NebulaAPI.Configurations.Configuration("options.role.sommelier.markedDuration", new ValueTuple<float, float, float>(1f, 10f, 0.5f), 2f, FloatConfigurationDecorator.Second, null, null);
    static IntegerConfiguration UseSkillNum = NebulaAPI.Configurations.Configuration("options.role.sommelier.useskillnum", (0, 30), 2);

    static public Sommelier MyRole = new Sommelier();
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int initialMarkedMask;
        public Ability(GamePlayer player, bool isUsurped,int mask) : base(player,isUsurped)
        {
            if (base.AmOwner)
            {
                initialMarkedMask = mask;
                meetingnum = 0;
                leftuse = UseSkillNum + 1;
                useafterskill = false;
                HudContent IconsHolder = HudContent.InstantiateContent("SommelierIcons", true, true, false, true);
                this.BindGameObject(IconsHolder.gameObject);
                ScriptBehaviour ajust = UnityHelper.CreateObject<ScriptBehaviour>("Sjust", IconsHolder.transform, UnityEngine.Vector3.zero, null);
                ajust.UpdateHandler += delegate
                {
                    if (MeetingHud.Instance)
                    {
                        ajust.transform.localScale = new UnityEngine.Vector3(0.65f, 0.65f, 1f);
                        ajust.transform.localPosition = new UnityEngine.Vector3(-0.45f, -0.37f, 0f);
                        return;
                    }
                    ajust.transform.localScale = UnityEngine.Vector3.one;
                    ajust.transform.localPosition = UnityEngine.Vector3.zero;
                };
                foreach (Player p2 in NebulaGameManager.Instance.AllPlayerInfo)
                {
                    if (!p2.AmOwner)
                    {
                        PoolablePlayer icon = AmongUsUtil.GetPlayerIcon(p2.Unbox().DefaultOutfit, ajust.transform, UnityEngine.Vector3.zero, UnityEngine.Vector3.one * 0.31f, false, true);
                        icon.ToggleName(false);
                        icon.SetAlpha(0.35f);
                        playerIcons.Add(new ValueTuple<byte, PoolablePlayer>(p2.PlayerId, icon));
                        UpdateIcons();
                    }
                }
                if (initialMarkedMask != 0)
                {
                    foreach (ValueTuple<byte, PoolablePlayer> icon2 in playerIcons)
                    {
                        if (((1 << (int)icon2.Item1) & initialMarkedMask) != 0)
                        {
                            icon2.Item2.SetAlpha(1f);
                        }
                    }
                }
                ObjectTracker<IPlayerlike> myTracker = ObjectTrackers.ForPlayerlike(this, null, base.MyPlayer, (IPlayerlike p) => ObjectTrackers.PlayerlikeStandardPredicate(p) && this.playerIcons.Any((ValueTuple<byte, PoolablePlayer> tuple) => tuple.Item1 == p.RealPlayer.PlayerId && tuple.Item2.GetAlpha() < 0.8f), null, false, false);
                ModAbilityButton markedButton = NebulaAPI.Modules.EffectButton(this, base.MyPlayer, VirtualKeyInput.Ability, MarkCoolDownOption, MarkDurationOption, "SLmark", markButton!, (ModAbilityButton _) => myTracker.CurrentTarget != null, (ModAbilityButton _) => !MyPlayer.IsDead && !playerIcons.All((ValueTuple<byte, PoolablePlayer> tuple) => tuple.Item2.GetAlpha() > 0.8f) && leftuse > 0, false, false);
                markedButton.OnEffectEnd = delegate (ModAbilityButton button)
                {
                    if (myTracker.CurrentTarget == null)
                    {
                        return;
                    }
                    if (!button.EffectTimer.IsProgressing)
                    {
                        GameOperatorManager instance = GameOperatorManager.Instance;
                        if (instance == null || !instance.Run<PlayerInteractPlayerLocalEvent>(new PlayerInteractPlayerLocalEvent(this.MyPlayer, myTracker.CurrentTarget, new PlayerInteractParameter(true, false, true)), false).IsCanceled)
                        {
                            foreach (ValueTuple<byte, PoolablePlayer> icon3 in this.playerIcons)
                            {
                                if (icon3.Item1 == myTracker.CurrentTarget.RealPlayer.PlayerId)
                                {
                                    icon3.Item2.SetAlpha(1f);
                                }
                            }
                        }
                    }
                    new StaticAchievementToken("sommelier.common1");
                    markedButton.StartCoolDown();
                };
                markedButton.OnUpdate = delegate (ModAbilityButton button)
                {
                    if (!button.IsInEffect)
                    {
                        return;
                    }
                    if (myTracker.CurrentTarget == null)
                    {
                        button.InterruptEffect();
                    }
                };
                markedButton.StartCoolDown();
            }
        }
        private List<ValueTuple<byte, PoolablePlayer>> playerIcons = new List<ValueTuple<byte, PoolablePlayer>>();
        int[] IPlayerAbility.AbilityArguments
        {
            get
            {
                int mask = 0;
                foreach (ValueTuple<byte, PoolablePlayer> icon2 in playerIcons.Where((ValueTuple<byte, PoolablePlayer> icon) => icon.Item2.GetAlpha() > 0.8f))
                {
                    mask |= 1 << (int)icon2.Item1;
                }
                return [mask];
            }
        }
        bool CheckMarked(ValueTuple<byte, PoolablePlayer> p)
        {
            return p.Item2.GetAlpha() > 0.8f;
        }

        bool CheckUseSkill()
        {
            return playerIcons.Count(CheckMarked) >= Mathf.FloorToInt(playerIcons.Count * 2 / 3)&&leftuse>0;
        }

        void UpdateIcons()
        {
            for (int i = 0; i < playerIcons.Count; i++)
            {
                playerIcons[i].Item2.transform.localPosition = new UnityEngine.Vector3((float)i * 0.29f - 0.3f, -0.1f, (float)(-(float)i) * 0.01f);
            }
        }
        [Local]
        void LocalUpdate(GameUpdateEvent ev)
        {
            UpdateIcons();
        }
        List<byte> deadPlayers = new List<byte>();
        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            playerIcons.RemoveAll(tuple =>
            {
                if (NebulaGameManager.Instance?.GetPlayer(tuple.Item1)?.IsDead ?? true)
                {
                    GameObject.Destroy(tuple.Item2.gameObject);
                    return true;
                }
                return false;
            });
            deadPlayers = new List<byte>();
            PlayerControl.AllPlayerControls.GetFastEnumerator().Where(p => p.Data.IsDead).Do(pid => deadPlayers.Add(pid.PlayerId));
        }
        Virial.Media.Image? meetingButton = NebulaAPI.AddonAsset.GetResource("SommelierMeetingSkill.png")!.AsImage(115f);
        Virial.Media.Image? markButton = NebulaAPI.AddonAsset.GetResource("SommelierMark.png")!.AsImage(115f);
        int leftuse;
        bool useafterskill;
        int meetingnum;
        [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (meetingnum>=2)
            {
                if (ev.EndState.EndCondition != NebulaGameEnd.CrewmateWin)
                {
                    return;
                }
                if (!ev.EndState.Winners.Test(base.MyPlayer))
                {
                    return;
                }
                new StaticAchievementToken("sommelier.challenge1");
            }
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (useafterskill)
            {
                meetingnum++;
            }
            if (CheckUseSkill())
            {
                GameObject binder = UnityHelper.CreateObject("SommelierSkill", MeetingHud.Instance.SkipVoteButton.transform.parent, MeetingHud.Instance.SkipVoteButton.transform.localPosition, null);
                GameOperatorManager instance = GameOperatorManager.Instance;
                if (instance != null)
                {
                    instance.Subscribe<GameUpdateEvent>(delegate (GameUpdateEvent ev)
                    {
                        binder.gameObject.SetActive(!this.MyPlayer.IsDead && MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted);
                    }, new GameObjectLifespan(binder), 100);
                }
                TextMeshPro countText = UnityEngine.Object.Instantiate<TextMeshPro>(MeetingHud.Instance.TitleText, binder.transform);
                countText.gameObject.SetActive(useafterskill);
                if (useafterskill)
                {
                    leftuse--;
                }
                countText.gameObject.GetComponent<TextTranslatorTMP>().enabled = false;
                countText.alignment = TextAlignmentOptions.Center;
                countText.transform.localPosition = new UnityEngine.Vector3(2.4f, 0f);
                countText.color = Palette.White;
                countText.transform.localScale *= 0.65f;
                List<GamePlayer> list = new List<GamePlayer>();
                playerIcons.Where(CheckMarked).Do(p => list.Add(GamePlayer.GetPlayer(p.Item1)!));
                countText.text = Language.Translate("role.sommelier.nelandimpnum").Replace("%IMPNUM%",list.Count(p=>!p.IsDead&&p.IsImpostor).ToString()).Replace("%NEUNUM%",list.Count(p=>!p.IsDead&&!p.IsImpostor&&!p.IsCrewmate).ToString());
                if (!useafterskill)
                {
                    SpriteRenderer leftRenderer = UnityHelper.CreateObject<SpriteRenderer>("SommelierActivateSkill", binder.transform, new UnityEngine.Vector3(2.1f, 0.15f), null);
                    leftRenderer.sprite = meetingButton!.GetSprite();
                    PassiveButton passiveButton = leftRenderer.gameObject.SetUpButton(true, new SpriteRenderer[0], null, null);
                    passiveButton.OnMouseOver.AddListener(delegate
                    {
                        leftRenderer.color = UnityEngine.Color.green;
                    });
                    passiveButton.OnMouseOut.AddListener(delegate
                    {
                        leftRenderer.color = UnityEngine.Color.white;
                    });
                    passiveButton.OnClick.AddListener(delegate
                    {
                        new StaticAchievementToken("sommelier.common2");
                        leftuse--;
                        useafterskill = true;
                        leftRenderer.gameObject.SetActive(false);
                        countText.gameObject.SetActive(true);
                    });
                    leftRenderer.gameObject.AddComponent<BoxCollider2D>().size = new UnityEngine.Vector2(0.6f, 0.6f);
                }
            }
        }
        [OnlyMyPlayer]
        [Local]
        void OnExiled(PlayerExiledEvent ev)
        {
            List<GamePlayer> list = new List<GamePlayer>();
            playerIcons.Where(CheckMarked).Do(p => list.Add(GamePlayer.GetPlayer(p.Item1)!));
            if (list.Any(p => p.TryGetModifier<Mini.Instance>(out var m)))
            {
                new StaticAchievementToken("sommelier.another1");
            }
        }
    }
}
