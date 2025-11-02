using Plana.Roles.Modifier;

namespace Plana.Roles.Impostor;

public class NinjaFix : DefinedRoleTemplate, DefinedRole,HasCitation
{
    private NinjaFix() : base("ninja", NebulaTeams.ImpostorTeam.Color, RoleCategory.ImpostorRole, NebulaTeams.ImpostorTeam, [VanishCoolDownOption, VanishDurationOption,SpeedRatioOption,noreportBaitOption,CannotUseVentOption]) { }

    Citation? HasCitation.Citation => Citations.TheOtherRolesGM;
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    
    static private FloatConfiguration VanishCoolDownOption = NebulaAPI.Configurations.Configuration("options.role.ninja.vanishCoolDown", (5f, 60f, 2.5f), 20f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration VanishDurationOption = NebulaAPI.Configurations.Configuration("options.role.ninja.vanishDuration", (5f, 60f, 2.5f), 10f, FloatConfigurationDecorator.Second);
    static private FloatConfiguration SpeedRatioOption = NebulaAPI.Configurations.Configuration("options.role.ninja.speedRatio", (0f, 5f, 0.25f), 1.5f, FloatConfigurationDecorator.Ratio);
    static private BoolConfiguration noreportBaitOption = NebulaAPI.Configurations.Configuration("options.role.ninja.noreportBait",false);
    static private BoolConfiguration CannotUseVentOption = NebulaAPI.Configurations.Configuration("options.role.ninja.cannotusevent", true);

    static public NinjaFix MyRole = new NinjaFix();
    public static void LoadPatch(Harmony harmony)
    {
        PDebug.Log("Ninja Patch");
        harmony.Patch(typeof(PlayerControl).GetMethod("CmdReportDeadBody"), new HarmonyMethod(typeof(NinjaFix).GetMethod("OnReportDeadBody")));
        PDebug.Log("Done");
    }
    public static bool OnReportDeadBody(PlayerControl __instance,NetworkedPlayerInfo target)
    {
        try
        {
            if (!noreportBaitOption)
            {
                return true;
            }
            if (target == null)
            {
                return true;
            }
            var reporter = __instance.ToNebulaPlayer();
            var reported = GamePlayer.GetPlayer(target.PlayerId);
            if (reporter != null && reporter.Role is NinjaFix.Instance && reported != null)
            {
                if (reported.TryGetAbility<Bait.Ability>(out var ability) || reported.Modifiers.Any(r => r is BaitM.Instance))
                {
                    if (NebulaGameManager.Instance!.HavePassed(reported.DeathTime ?? NebulaGameManager.Instance.CurrentTime, Mathf.Min(0.5f, (NebulaAPI.Configurations.GetSharableVariable<int>("options.role.bait.reportDelay")?.Value ?? 0f) + NebulaAPI.Configurations.GetSharableVariable<int>("options.role.bait.reportDelayDispersion")?.Value ?? 0f) + 1f))
                    {
                        return true;
                    }
                    return false;
                }
            }
        }
        catch (Exception e)
        {

        }
        return true;
    }
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        DefinedRole RuntimeRole.Role => MyRole;
        bool RuntimeRole.CanUseVent => !CannotUseVentOption;
        bool RuntimeRole.CanMoveInVent => !CannotUseVentOption;
        private ModAbilityButton? vanishButton = null;
        static private Virial.Media.Image vanishImage = NebulaAPI.AddonAsset.GetResource("NinjaButton.png")!.AsImage(115f)!;
        
        public Instance(GamePlayer player) : base(player)
        {
        }

        void RuntimeAssignable.OnActivated() { 
            if (AmOwner)
            {
                vanishButton = NebulaAPI.Modules.AbilityButton(this, MyPlayer, Virial.Compat.VirtualKeyInput.Ability, VanishCoolDownOption, "vanish", vanishImage, (button) => MyPlayer.CanMove, (button) => !MyPlayer.IsDead, false);
				vanishButton.OnClick = (button) => {
				    button.StartEffect();
				};
				vanishButton.OnEffectStart = (button) =>
				{
                    MyPlayer.GainAttribute(PlayerAttributes.InvisibleElseImpostor, VanishDurationOption, false, 0, "ninja::vanish");
                    MyPlayer.GainSpeedAttribute(SpeedRatioOption, VanishDurationOption, false, 1, "ninja::speed");
				};
				vanishButton.OnEffectEnd = (button) =>
				{
				    button.StartCoolDown();
				};
				vanishButton.StartCoolDown();
				vanishButton.EffectTimer = NebulaAPI.Modules.Timer(this, VanishDurationOption);
                vanishButton.SetLabel("vanish");
            }
        }
    }
}
