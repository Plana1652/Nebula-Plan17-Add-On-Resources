using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Impostor;

namespace Plana.Roles.Modifier;

public class Infected : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>,HasCitation
{
    private Infected() : base("infected",NebulaTeams.ImpostorTeam.Color,null,true,()=>false)
    {
    }
    bool DefinedAssignable.ShowOnHelpScreen
    {
        get
        {
            return false;
        }
    }
    Citation HasCitation.Citation => PCitations.PlanaANDKC;
    static public Infected MyRole = new Infected();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        static TextAttributeOld ButtonAttribute = new TextAttributeOld(TextAttributeOld.BoldAttr) { Size = new(1.3f, 0.3f), Alignment = TMPro.TextAlignmentOptions.Center, FontMaterial = VanillaAsset.StandardMaskedFontMaterial }.EditFontSize(2f, 1f, 2f);
        MetaScreen OpenRoleSelectWindow(bool alpha100,Func<DefinedRole, bool> predicate, string underText, Action<DefinedRole> onSelected)
        {
            var window = MetaScreen.GenerateWindow(new UnityEngine.Vector2(7.6f, 4.2f), HudManager.Instance.transform, new UnityEngine.Vector3(0, 0, -50f), alpha100, false);
            var t = window.transform.parent.Find("CloseButton");
            if (t!=null)
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
            inner.Append(Nebula.Roles.Roles.AllRoles.Where(predicate), r => new MetaWidgetOld.Button(() => onSelected.Invoke(r), ButtonAttribute) { RawText = r.DisplayColoredName, PostBuilder = (_, renderer, _) => {
                renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                } }, 4, -1, 0, 0.59f);
            MetaWidgetOld.ScrollView scroller = new(new(6.9f, 3.8f), inner, true) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
            widget.Append(scroller);
            widget.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttr) { MyText = new RawTextComponent(underText), Alignment = IMetaWidgetOld.AlignmentOption.Center });
            window.SetWidget(widget);
            System.Collections.IEnumerator CoCloseOnResult()
            {
                if (!MeetingHud.Instance) yield break;
                while (MeetingHud.Instance.state == MeetingHud.VoteStates.Discussion) yield return null;
                if (SelectRoleScreen!=null)
                {
                    var list = Nebula.Roles.Roles.AllRoles.Where(predicate).ToList();
                    MyPlayer.SetRole(list[UnityEngine.Random.Range(0, list.Count)]);
                    if (MyPlayer.Role.Role is SpeechEater && MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var guesser))
                    {
                        MyPlayer.RemoveModifier(GuesserModifier.MyRole);
                        var ability = MyPlayer.Role.GetAbility<SpeechEater.Ability>();
                        if (ability != null)
                        {
                            ability.LeftGuess = PatchManager.InfectedGuessNum;
                        }
                    }
                    window.CloseScreen();
                }
            }
            window.StartCoroutine(CoCloseOnResult().WrapToIl2Cpp());
            return window;
        }
        MetaScreen SelectRoleScreen;
        bool Selected;
        [Local]
        void OnMeetingUpdate(GameUpdateEvent ev)
        {
            if (!MeetingHud.Instance)
            {
                return;
            }
            if (Selected)
            {
                return;
            }
            if (PatchManager.InfectedSetRoleImmediately)
            {
                return;
            }    
            if (MeetingHud.Instance.state==MeetingHud.VoteStates.Discussion)
            {
                if (SelectRoleScreen == null)
                {
                    Selected = true;
                    SelectRoleScreen = OpenRoleSelectWindow(true,(DefinedRole r) => r.IsSpawnable && r.Category == RoleCategory.ImpostorRole, Language.Translate("role.infected.selectImpRoleText"), (DefinedRole r) =>
                        {
                            MyPlayer.SetRole(r);
                            if (r is SpeechEater&& MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var guesser))
                            {
                                MyPlayer.RemoveModifier(GuesserModifier.MyRole);
                                var ability = MyPlayer.Role.GetAbility<SpeechEater.Ability>();
                                if (ability != null)
                                {
                                    ability.LeftGuess = PatchManager.InfectedGuessNum;
                                }
                            }
                            SelectRoleScreen?.CloseScreen();
                            SelectRoleScreen = null;
                        });
                }
            }
        }
        bool showtext;
        [Local]
        private void OnMeetingEnd(TaskPhaseStartEvent ev)
        {
            if (!showtext)
            {
                showtext = true;
                Game currentGame = NebulaAPI.CurrentGame;
                if (currentGame != null)
                {
                    TitleShower module = currentGame.GetModule<TitleShower>();
                    if (module != null)
                    {
                        module.SetText(Language.Translate("role.infected.OnBecameInfected"), MyRole.RoleColor.ToUnityColor(), 6f);
                    }
                }
                AmongUsUtil.PlayCustomFlash(MyRole.RoleColor.ToUnityColor(), 0f, 0.8f, 0.7f, 0f);
                if (PatchManager.InfectedSetRoleImmediately)
                {
                    Selected = true;
                    SelectRoleScreen = OpenRoleSelectWindow(false, (DefinedRole r) => r.IsSpawnable && r.Category == RoleCategory.ImpostorRole, Language.Translate("role.infected.selectImpRoleText"), (DefinedRole r) =>
                    {
                        MyPlayer.SetRole(r);
                        if (r is SpeechEater && MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var guesser))
                        {
                            MyPlayer.RemoveModifier(GuesserModifier.MyRole);
                            var ability = MyPlayer.Role.GetAbility<SpeechEater.Ability>();
                            if (ability!=null)
                            {
                                ability.LeftGuess = PatchManager.InfectedGuessNum;
                            }
                        }
                        SelectRoleScreen?.CloseScreen();
                        SelectRoleScreen = null;
                    });
                }
            }
        }
        void OnSheriffKill(SheriffCheckKillEvent ev)
        {
            if (ev.Player.Role.Role.LocalizedName.Contains("sheriff") && ev.Player.IsModMadmate())
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
                var value = NebulaAPI.Configurations.Configuration("options.role.collator.madmateIsClassifiedAs", new string[] { "options.role.collator.madmateIsClassifiedAs.impostor", "options.role.collator.madmateIsClassifiedAs.crewmate" }, 0, null, null).GetValue();
                ev.Team = value == 0 ? NebulaTeams.ImpostorTeam : NebulaTeams.CrewmateTeam;
            }
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                Selected = false;
                showtext = false;
                if (MyPlayer.TryGetModifier<GuesserModifier.Instance>(out var modifier))
                {
                    modifier.LeftGuess = PatchManager.InfectedGuessNum;
                }
                else if (PatchManager.InfectedHasGuesser)
                {
                    MyPlayer.AddModifier(GuesserModifier.MyRole, new int[1] { PatchManager.InfectedGuessNum });
                }
            }
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " ∀".Color(MyRole.RoleColor.ToUnityColor());
        }
        bool RuntimeModifier.MyCrewmateTaskIsIgnored
        {
            get
            {
                return true;
            }
        }
    }
}
