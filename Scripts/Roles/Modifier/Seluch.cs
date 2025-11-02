using Plana.Core;

namespace Plana.Roles.Modifier;

public class Seluch : DefinedAllocatableModifierTemplate, HasCitation, DefinedAllocatableModifier
{
private Seluch(): base("seluch", "sel", new(117, 72, 78), [ShowModifier]) {
    }
    Citation? HasCitation.Citation { get { return PCitations.TownOfUs; } }
    public static BoolConfiguration ShowModifier = NebulaAPI.Configurations.Configuration("options.roles.seluch.showmodifier", true);
    static public Seluch MyRole = new Seluch();
    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated() {
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner || canSeeAllInfo) name += " ▼".Color(MyRole.RoleColor.ToUnityColor());
        }
        [Local]
        void OnReported(ReportDeadBodyEvent ev)
        {
            try
            {
                if (ev.Reporter.AmOwner&&ev.Reported!=null)
                {
                    string text = ev.Reported.Role.DisplayColoredName;
                    if (ShowModifier)
                    {
                        ev.Reported.Modifiers?.Do(runtimeModifier =>
                        {
                            var newtext = runtimeModifier.OverrideRoleName(text, false);
                            if (!string.IsNullOrEmpty(newtext))
                            {
                                text = newtext;
                            }
                            runtimeModifier.DecorateNameConstantly(ref text, true);
                        });
                    }
                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, Language.Translate("role.seluch.reportText") + text);
                    if (ev.Reported.Role.Role.LocalizedName.Contains("busker"))
                    {
                        new StaticAchievementToken("seluch.another1");
                    }
                    if (ev.Reported.Role.Role.LocalizedName.Contains("bait") || ev.Reported.Modifiers.Any((RuntimeModifier m) => m.Modifier.LocalizedName.Contains("baitM")))
                    {
                        new StaticAchievementToken("seluch.another2");
                    }
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        string? RuntimeModifier.DisplayIntroBlurb => Language.Translate("role.seluch.blurb");
    }
}
