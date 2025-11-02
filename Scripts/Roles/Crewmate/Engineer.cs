global using Plana.Core;
namespace Plana.Roles.Crewmate;

public class Engineer : DefinedRoleTemplate, DefinedRole, HasCitation
{
    private Engineer() : base("engineer", new(53,73,255), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [CooldownOption])
    {
    }
    Citation? HasCitation.Citation { get { return Citations.AmongUs; } }
    internal static IVentConfiguration CooldownOption = NebulaAPI.Configurations.VentConfiguration("options.role.engineer.cooldown", false, null, -1, (0f, 60f, 2.5f), 10f, (0f,60f,2.5f), 0f);
    static public Engineer MyRole = new Engineer();
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeVentRoleTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        public override DefinedRole Role
        {
            get
            {
                return Engineer.MyRole;
            }
        }

        public Instance(GamePlayer player) : base(player,CooldownOption)
        {
        }
        bool RuntimeRole.CanMoveInVent => true;
        bool RuntimeRole.CanUseVent => true;
        Sprite ImpventButton;
        public override void OnActivated()
        {
            if (AmOwner)
            {
                ImpventButton = HudManager.Instance.ImpostorVentButton.graphic.sprite;
                var engineer = AmongUsUtil.GetRolePrefab<EngineerRole>();
                HudManager.Instance.ImpostorVentButton.graphic.sprite=engineer?.Ability.Image;
                HudManager.Instance.ImpostorVentButton.buttonLabelText.fontSharedMaterial = engineer?.Ability.FontMaterial;
            }
        }
        void RuntimeAssignable.OnInactivated()
        {
            if (AmOwner)
            {
                HudManager.Instance.ImpostorVentButton.graphic.sprite = ImpventButton;
                HudManager.Instance.ImpostorVentButton.buttonLabelText.fontSharedMaterial= AmongUsUtil.GetRolePrefab<ShapeshifterRole>()?.Ability.FontMaterial;
            }
        }
        public static void EngineerVentTextPatch()
        {
            if (!PlayerControl.LocalPlayer)
            {
                return;
            }
            var localModInfo = GamePlayer.LocalPlayer?.Unbox();
            if (localModInfo != null)
            {
                if (!(localModInfo.Role is Engineer.Instance))
                {
                    return;
                }
                var ventTimer = PlayerControl.LocalPlayer.inVent ? localModInfo.Role?.VentDuration : localModInfo.Role?.VentCoolDown;
                string ventText = "";
                float ventPercentage = 0f;
                if (ventTimer != null && ventTimer.IsProgressing)
                {
                    ventText = Mathf.CeilToInt(ventTimer.CurrentTime).ToString();
                    ventPercentage = ventTimer.Percentage;
                }
                if ((localModInfo.Role?.VentDuration?.Max ??-2.5f)< 0f&&PlayerControl.LocalPlayer.inVent)
                {
                    ventText = "∞";
                }    
                if (ventTimer != null && !ventTimer.IsProgressing && PlayerControl.LocalPlayer.inVent)
                {
                    Vent.currentVent.SetButtons(false);
                    var exitVent = Vent.currentVent.GetValidVent();
                    if (exitVent != null) PlayerControl.LocalPlayer.MyPhysics.RpcExitVent(exitVent.Id);
                }

                var ventButton = HudManager.Instance.ImpostorVentButton;
                ventButton.SetCooldownFill(ventPercentage);
                CooldownHelpers.SetCooldownNormalizedUvs(ventButton.graphic);
                ventButton.cooldownTimerText.text = ventText;
                ventButton.cooldownTimerText.color = PlayerControl.LocalPlayer.inVent ? Engineer.MyRole.UnityColor : UnityEngine.Color.white;
            }
        }
    }
}
