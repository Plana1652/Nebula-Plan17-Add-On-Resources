using Plana.Core;
using Nebula.Roles.Assignment;
using Nebula.Roles.Impostor;
using Plana.Roles.Modifier;
using Plana.Roles.Crewmate;
using Nebula.Roles.Complex;

namespace Plana.Roles.Impostor;

public class Dreamweaver : DefinedRoleTemplate, DefinedRole, HasCitation
{
    private Dreamweaver() : base("dreamweaver", NebulaTeams.ImpostorTeam.Color,RoleCategory.ImpostorRole,PatchManager.DreamweaverAndInsomniacsTeam,[CanSelectRole,CanSelectRoleNum,SelectRoleImmediately], true, true, null, null, () =>
    {
        return [new AllocationParameters.ExtraAssignmentInfo((DefinedRole _, int playerId) => new ValueTuple<DefinedRole, int[]>(Insomniacs.MyRole, new int[] { 0, playerId
}), RoleCategory.CrewmateRole)];
    })
    {
        IConfigurationHolder configurationHolder = base.ConfigurationHolder;
        if (configurationHolder == null)
        {
            return;
        }
        configurationHolder.ScheduleAddRelated(() => new IConfigurationHolder[] { Insomniacs.MyRole.ConfigurationHolder });
    }
    static BoolConfiguration CanSelectRole = NebulaAPI.Configurations.Configuration("options.role.dreamweaver.canselectrole", true);
    static IntegerConfiguration CanSelectRoleNum = NebulaAPI.Configurations.Configuration("options.role.insomniacs.selectrolenum", (1,20),2,()=>CanSelectRole);
    static BoolConfiguration SelectRoleImmediately = NebulaAPI.Configurations.Configuration("options.role.dreamweaver.selectroleImmediately", false);
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public Dreamweaver MyRole = new Dreamweaver();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    DefinedRole[] DefinedRole.AdditionalRoles => [Insomniacs.MyRole];
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        static TextAttributeOld ButtonAttribute = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new(1.3f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(2f, 1f, 2f);
        MetaScreen OpenRoleSelectWindow(bool alpha100,List<DefinedRole> rolelist,Func<DefinedRole, bool> predicate, string underText, Action<DefinedRole> onSelected)
        {
            var window = MetaScreen.GenerateWindow(new UnityEngine.Vector2(7.6f, 4.2f), HudManager.Instance.transform, new UnityEngine.Vector3(0, 0, -50f), alpha100, false);
            var t = window.transform.parent.Find("CloseButton");
            if (t != null)
            {
                UnityEngine.Object.Destroy(t.gameObject);
            }
            if (!alpha100)
            {
                window.combinedObject.GetComponentsInChildren<SpriteRenderer>().Do(r =>
                {
                    var c = r.color;
                    c.a = 0.6f;
                    r.color = c;
                });
            }
            MetaWidgetOld widget = new();
            MetaWidgetOld inner = new();
            var roles = rolelist.Where(predicate).ToList();
            var list = new List<DefinedRole>();
            while (list.Count<CanSelectRoleNum)
            {
                var role = roles.Random();
                if (list.Contains(role))
                {
                    roles.Remove(role);
                    continue;
                }
                list.Add(role);
                roles.Remove(role);
            }
            inner.Append(list, r => new MetaWidgetOld.Button(() => onSelected.Invoke(r), ButtonAttribute) { RawText = r.DisplayColoredName, PostBuilder = (_, renderer, _) =>
            {
                renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }
            }, 4, -1, 0, 0.59f);
            MetaWidgetOld.ScrollView scroller = new(new(6.9f, 3.8f), inner, true) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
            widget.Append(scroller);
            widget.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttr) { MyText = new RawTextComponent(underText), Alignment = IMetaWidgetOld.AlignmentOption.Center });
            window.SetWidget(widget);
            System.Collections.IEnumerator CoCloseOnResult()
            {
                if (!MeetingHud.Instance) yield break;
                while (MeetingHud.Instance.state == MeetingHud.VoteStates.Discussion) yield return null;
                if (SelectRoleScreen != null)
                {
                    MyPlayer.SetRole(roles[UnityEngine.Random.Range(0, list.Count)]);
                    MyPlayer.AddModifier(DreamweaverModifier.MyRole);
                    if (MyPlayer.Role.Role.LocalizedName.Contains("speechEater") && MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var guesser))
                    {
                        MyPlayer.RemoveModifier(GuesserModifier.MyRole);
                    }
                    window.CloseScreen();
                }
            }
            window.StartCoroutine(CoCloseOnResult().WrapToIl2Cpp());
            return window;
        }
        MetaScreen SelectRoleScreen;
        List<TrackingArrowAbility> tracking = new List<TrackingArrowAbility>();
        [Local]
        void OnSetRole(PlayerRoleSetEvent ev)
        {
            var p = tracking.FirstOrDefault(r => r.MyPlayer.PlayerId == ev.Player.PlayerId);
            if (ev.Role is Insomniacs.Instance)
            {
                if (p == null)
                {
                    tracking.Add(new TrackingArrowAbility(ev.Player, 0f, Insomniacs.MyRole.UnityColor, false).Register(this));
                }
            }
            else
            {
                if (p!=null)
                {
                    p.Release();
                    tracking.Remove(p);
                }
            }
        }
        [Local]
        void OnSetModifier(PlayerModifierSetEvent ev)
        {
            var p = tracking.FirstOrDefault(r => r.MyPlayer.PlayerId == ev.Player.PlayerId);
            if (ev.Modifier is InsomniacsModifier.Instance)
            {
                if (p == null)
                {
                    tracking.Add(new TrackingArrowAbility(ev.Player, 0f, Insomniacs.MyRole.UnityColor, false).Register(this));
                }
            }
        }
        [Local]
        void OnRemoveModifier(PlayerModifierRemoveEvent ev)
        {
            var p = tracking.FirstOrDefault(r => r.MyPlayer.PlayerId == ev.Player.PlayerId);
            if (p != null&&ev.Modifier is InsomniacsModifier.Instance)
            {
                p.Release();
                tracking.Remove(p);
            }
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            if (SelectRoleImmediately)
            {
                return;
            }
            List<DefinedRole> CanSpawnImpostorRoles = new List<DefinedRole>(Nebula.Roles.Roles.AllRoles.Where(p => p.IsSpawnable&&p.Category == RoleCategory.ImpostorRole));
            if (CanSpawnImpostorRoles.Contains(Destroyer.MyRole))
            {
                CanSpawnImpostorRoles.Remove(Destroyer.MyRole);
            }
            if (CanSpawnImpostorRoles.Contains(Nebula.Roles.Impostor.Impostor.MyRole))
            {
                CanSpawnImpostorRoles.Remove(Nebula.Roles.Impostor.Impostor.MyRole);
            }
            foreach (GamePlayer player in NebulaGameManager.Instance.AllPlayerInfo ?? [])
            {
                if (player.IsImpostor)
                {
                    if (CanSpawnImpostorRoles.Contains(player.Role.Role))
                    {
                        CanSpawnImpostorRoles.Remove(player.Role.Role);
                    }
                }
            }
            if (CanSelectRole)
            {
                if (SelectRoleScreen == null)
                {
                    SelectRoleScreen = OpenRoleSelectWindow(true,CanSpawnImpostorRoles, (DefinedRole r) => r.IsSpawnable && r.Category == RoleCategory.ImpostorRole, Language.Translate("role.infected.selectImpRoleText"), (DefinedRole r) =>
                    {
                        MyPlayer.SetRole(r);
                        MyPlayer.AddModifier(DreamweaverModifier.MyRole);
                        if (MyPlayer.Role.Role.LocalizedName.Contains("speechEater") && MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var guesser))
                        {
                            MyPlayer.RemoveModifier(GuesserModifier.MyRole);
                        }
                        SelectRoleScreen?.CloseScreen();
                        SelectRoleScreen = null;
                    });
                }
            }
            else
            {
                MyPlayer.SetRole(CanSpawnImpostorRoles[UnityEngine.Random.Range(0, CanSpawnImpostorRoles.Count)]);
                MyPlayer.AddModifier(DreamweaverModifier.MyRole);
            }
        }
        [Local]
        void OnGameStarted(GameStartEvent ev)
        {
            if (!SelectRoleImmediately)
            {
                return;
            }
            List<DefinedRole> CanSpawnImpostorRoles = new List<DefinedRole>(Nebula.Roles.Roles.AllRoles.Where(p => p.IsSpawnable && p.Category == RoleCategory.ImpostorRole));
            if (CanSpawnImpostorRoles.Contains(Destroyer.MyRole))
            {
                CanSpawnImpostorRoles.Remove(Destroyer.MyRole);
            }
            if (CanSpawnImpostorRoles.Contains(Nebula.Roles.Impostor.Impostor.MyRole))
            {
                CanSpawnImpostorRoles.Remove(Nebula.Roles.Impostor.Impostor.MyRole);
            }
            foreach (GamePlayer player in NebulaGameManager.Instance.AllPlayerInfo ?? [])
            {
                if (player.IsImpostor)
                {
                    if (CanSpawnImpostorRoles.Contains(player.Role.Role))
                    {
                        CanSpawnImpostorRoles.Remove(player.Role.Role);
                    }
                }
            }
            if (CanSelectRole)
            {
                if (SelectRoleScreen == null)
                {
                    SelectRoleScreen = OpenRoleSelectWindow(false, CanSpawnImpostorRoles, (DefinedRole r) => r.IsSpawnable && r.Category == RoleCategory.ImpostorRole, Language.Translate("role.infected.selectImpRoleText"), (DefinedRole r) =>
                    {
                        MyPlayer.SetRole(r);
                        MyPlayer.AddModifier(DreamweaverModifier.MyRole);
                        if (MyPlayer.Role.Role.LocalizedName.Contains("speechEater") && MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var guesser))
                        {
                            MyPlayer.RemoveModifier(GuesserModifier.MyRole);
                        }
                        SelectRoleScreen?.CloseScreen();
                        SelectRoleScreen = null;
                    });
                }
            }
            else
            {
                MyPlayer.SetRole(CanSpawnImpostorRoles[UnityEngine.Random.Range(0, CanSpawnImpostorRoles.Count)]);
                MyPlayer.AddModifier(DreamweaverModifier.MyRole);
            }
        }
        [Local]
        private void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (!ev.Player.AmOwner && (ev.Player.Role is Insomniacs.Instance || ev.Player.TryGetModifier<InsomniacsModifier.Instance>(out var m)))
            {
                ev.Color = new(216, 164, 246);
            }
        }
       [Local]
        void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.EndCondition==NebulaGameEnd.ImpostorWin)
            {
                var player = NebulaGameManager.Instance.AllPlayerInfo.FirstOrDefault(p => p.Role is Insomniacs.Instance || p.TryGetModifier<InsomniacsModifier.Instance>(out var im));
                if (player!=null&&ev.EndState.Winners.Test(MyPlayer)&&ev.EndState.Winners.Test(player)&&NebulaGameManager.Instance.AllPlayerInfo.Count(p=>!p.IsDead&&p.IsImpostor)==1)
                {
                    new StaticAchievementToken("dreamweaver.challenge1");
                }
            }
        }
        [Local,OnlyMyPlayer]
        void OnKilled(PlayerKillPlayerEvent ev)
        {
            if (ev.Dead.Role is Insomniacs.Instance)
            {
                new StaticAchievementToken("dreamweaver.another1");
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                List<GamePlayer> list = NebulaGameManager.Instance.AllPlayerInfo.Where((GamePlayer p) => !p.IsDead && (p.Role is Insomniacs.Instance || p.Modifiers.Any(m => m is InsomniacsModifier.Instance))).ToList();
                if (list.Count == 0)
                {
                    return;
                }
                foreach (var p in list)
                {
                    tracking.Add(new TrackingArrowAbility(p, 0f, Insomniacs.MyRole.UnityColor, false).Register(this));
                }
            }
        }
    }
}
public class DreamweaverModifier : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    private DreamweaverModifier() : base("dreamweaverM", new Virial.Color(255,43,133), null,true,()=>false)
    {
    }
    bool DefinedAssignable.ShowOnHelpScreen
    {
        get
        {
            return false;
        }
    }
    bool DefinedAssignable.ShowOnFreeplayScreen
    {
        get
        {
            return false;
        }
    }
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }
    static public DreamweaverModifier MyRole = new DreamweaverModifier();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        List<TrackingArrowAbility> tracking = new List<TrackingArrowAbility>();
        [Local]
        void OnSetRole(PlayerRoleSetEvent ev)
        {
            var p = tracking.FirstOrDefault(r => r.MyPlayer.PlayerId == ev.Player.PlayerId);
            if (ev.Role is Insomniacs.Instance)
            {
                if (p == null)
                {
                    tracking.Add(new TrackingArrowAbility(ev.Player, 0f, Insomniacs.MyRole.UnityColor, false).Register(this));
                }
            }
            else
            {
                if (p != null)
                {
                    p.Release();
                    tracking.Remove(p);
                }
            }
        }
        [Local]
        void OnSetModifier(PlayerModifierSetEvent ev)
        {
            var p = tracking.FirstOrDefault(r => r.MyPlayer.PlayerId == ev.Player.PlayerId);
            if (ev.Modifier is InsomniacsModifier.Instance)
            {
                if (p == null)
                {
                    tracking.Add(new TrackingArrowAbility(ev.Player, 0f, Insomniacs.MyRole.UnityColor, false).Register(this));
                }
            }
        }
        [Local]
        void OnRemoveModifier(PlayerModifierRemoveEvent ev)
        {
            var p = tracking.FirstOrDefault(r => r.MyPlayer.PlayerId == ev.Player.PlayerId);
            if (p != null&&ev.Modifier is InsomniacsModifier.Instance)
            {
                p.Release();
                tracking.Remove(p);
            }
        }
        [Local]
        private void DecorateNameColor(PlayerDecorateNameEvent ev)
        {
            if (!ev.Player.AmOwner && (ev.Player.Role is Insomniacs.Instance||ev.Player.TryGetModifier<InsomniacsModifier.Instance>(out var m)))
            {
                ev.Color = new(216, 164, 246);
            }
        }
        [OnlyHost]
        [OnlyMyPlayer]
        private void OnDead(PlayerDieOrDisconnectEvent ev)
        {
            if (MeetingHud.Instance)
            {
                return;
            }
            foreach (GamePlayer player in NebulaGameManager.Instance.AllPlayerInfo)
            {
                if (!player.IsDead && player.TryGetModifier<InsomniacsModifier.Instance>(out var m))
                {
                    using (RPCRouter.CreateSection("RemoveInsomniacsModifier"))
                    {
                        player.RemoveModifier(InsomniacsModifier.MyRole);
                    }
                }
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                List<GamePlayer> list = NebulaGameManager.Instance.AllPlayerInfo.Where((GamePlayer p) => !p.IsDead && (p.Role is Insomniacs.Instance || p.Modifiers.Any(m => m is InsomniacsModifier.Instance))).ToList();
                if (list.Count == 0)
                {
                    return;
                }
                foreach (var p in list)
                {
                    tracking.Add(new TrackingArrowAbility(p, 0f, Insomniacs.MyRole.UnityColor, false).Register(this));
                }
            }
        }
        string RuntimeAssignable.OverrideRoleName(string lastRoleName, bool isShort)
        {
             return Language.Translate("role.dreamweaver.prefix").Color(MyRole.RoleColor.ToUnityColor()) + lastRoleName;
        }
    }
}

