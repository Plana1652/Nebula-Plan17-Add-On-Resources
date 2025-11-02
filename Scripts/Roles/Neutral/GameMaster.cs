using Nebula.Roles.MapLayer;
using Nebula.Roles.Neutral;
using Plana.Core;
using Plana.Roles.Impostor;
using Virial.Events.Game.Minimap;
using GUI = Nebula.Modules.GUIWidget.NebulaGUIWidgetEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Plana.Roles.Neutral;

public class GameMaster : DefinedRoleTemplate, DefinedRole, HasCitation
{
    private GameMaster() : base("gamemaster", new(255, 91, 112), RoleCategory.NeutralRole, PatchManager.GameMasterTeam, null, false,true,()=>false)
    {
    }
    bool DefinedAssignable.ShowOnHelpScreen
    {
        get
        {
            return false;
        }
    }
    bool IGuessed.CanBeGuess => false;
    Citation? HasCitation.Citation { get { return PCitations.PlanaANDKC; } }

    static public GameMaster MyRole = new GameMaster();
    static RemoteProcess<(string, GamePlayer)> SetPerk = new RemoteProcess<(string, GamePlayer)>("SetPerkRPCGM", (message, _) =>
    {
        if (message.Item2.AmOwner)
        {
            ModSingleton<ItemSupplierManager>.Instance?.SetPerk(Nebula.Roles.Roles.AllPerks.FirstOrDefault(p => p.Id == message.Item1));
        }
    });
    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);
    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, ILifespan, IBindPlayer, IGameOperator, IReleasable
    {
        DefinedRole RuntimeRole.Role => MyRole;
        public Instance(GamePlayer player) : base(player)
        {
        }
        void RuntimeAssignable.OnActivated()
        {
            if (AmOwner)
            {
                if (GameOperatorManager.Instance.AllOperators.Any(p=>p is GameMasterAbility))
                {
                    return;
                }
                new GameMasterAbility().Register(NebulaGameManager.Instance);
            }
        }
        CrawlerEngineer.ShowPlayersMapLayer mapCountLayer;
        [Local]
        void OnGameStarted(GameStartEvent ev)
        {
            MyPlayer.Suicide(PlayerState.Suicide, null, 0, null);
        }
        [Local]
        void OnOpenMap(AbstractMapOpenEvent ev)
        {
            try
            {
                if (!(ev is MapOpenAdminEvent) && !MeetingHud.Instance)
                {
                    if (!this.mapCountLayer)
                    {
                        this.mapCountLayer = UnityHelper.CreateObject<CrawlerEngineer.ShowPlayersMapLayer>("CountLayer", MapBehaviour.Instance.transform, new UnityEngine.Vector3(0f, 0f, -1f), null);
                        this.BindGameObject(this.mapCountLayer.gameObject);
                        this.mapCountLayer.SetUp(p => true, null, true, null);
                    }
                    this.mapCountLayer.gameObject.SetActive(true);
                    return;
                }
                if (this.mapCountLayer)
                {
                    this.mapCountLayer.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        [OnlyMyPlayer]
        void EditGuessable(PlayerCanGuessPlayerLocalEvent ev)
        {
           ev.CanGuess = false;
        }
        [Local]
        void ShowAllRole(PlayerCheckRoleInfoVisibilityLocalEvent ev)
        {
            ev.CanSeeRole = true;
        }
        [Local]
        void ShowNameColor(PlayerDecorateNameEvent ev) 
        {
            if (ev.Player.Modifiers.Any(m=>m is SidekickModifier.Instance))
            {
                ev.Color = Jackal.MyRole.RoleColor;
            }
            else
            {
                ev.Color = ev.Player.Role.Role.Color;
            }
        }
        [OnlyMyPlayer]
        void ShowMyRole(PlayerCheckRoleInfoVisibilityLocalEvent ev)
        {
            ev.CanSeeRole = true;
        }
        [OnlyMyPlayer]
        void ShowMyNameColor(PlayerDecorateNameEvent ev)
        {
            ev.Color = MyRole.RoleColor;
        }
        [Local]
        public void EditLightRange(LightRangeUpdateEvent ev)
        {
            ev.LightRange *= 100;
        }
        bool RuntimeRole.HasImpostorVision
        {
            get
            {
                return true;
            }
        }
        bool RuntimeRole.IgnoreBlackout
        {
            get
            {
                return true;
            }
        }
        bool RuntimeRole.EyesightIgnoreWalls
        {
            get
            {
                return true;
            }
        }

    }
    public class GameMasterAbility : DependentLifespan, IGameOperator
    {
        public GameMasterAbility()
        {
            NebulaGameManager.Instance?.ChangeToSpectator(false);
            var roleButton = NebulaAPI.Modules.AbilityButton(this, true, false, 100)
                .BindKey(Virial.Compat.VirtualKeyInput.FreeplayAction)
                .SetImage(MetaAbility.buttonSprite).SetLabel("operate");
            roleButton.Availability = (button) => true;
            roleButton.Visibility = (button) => true;
            roleButton.OnClick = (button) => OpenWindow();

            var reviveButton = NebulaAPI.Modules.AbilityButton(this, true, false, 98)
                .SetImage(MetaAbility.reviveSprite).SetLabel("revive");
            reviveButton.Availability = (button) => true;
            reviveButton.Visibility = (button) => PlayerControl.LocalPlayer.Data.IsDead;
            reviveButton.OnClick = (button) => NebulaManager.Instance.ScheduleDelayAction(() => GamePlayer.LocalPlayer!.Revive(null, new Virial.Compat.Vector2(PlayerControl.LocalPlayer.transform.position), true, false));


            var suicideButton = NebulaAPI.Modules.AbilityButton(this, true, false, 98)
                .SetLabel("suicide");
            suicideButton.Availability = (button) => true;
            suicideButton.Visibility = (button) => !PlayerControl.LocalPlayer.Data.IsDead;
            suicideButton.OnClick = (button) => NebulaManager.Instance.ScheduleDelayAction(() => GamePlayer.LocalPlayer!.Suicide(PlayerState.Suicide, null, 0));


            var circleButton = NebulaAPI.Modules.AbilityButton(this, true, false, 99)
                .SetImage(MetaAbility.circleButtonSprite).SetLabel("show");
            circleButton.Availability = (button) => true;
            circleButton.Visibility = (button) => true;
            circleButton.OnClick = (button) => OpenCircleWindow();

            if (DebugTools.DebugMode) new DebugAbility().Register(this);
        }

        static EffectCircle circle = null!;
        private void OpenCircleWindow()
        {
            var window = MetaScreen.GenerateWindow(new Vector2(4f, 3.8f), HudManager.Instance.transform, new Vector3(0, 0, -400f), true, false);
            var maskedTittleAttr = new TextAttribute(GUI.API.GetAttribute(Virial.Text.AttributeAsset.MetaRoleButton)) { Size = new(3f, 0.26f) };

            window.SetWidget(GUI.API.ScrollView(Virial.Media.GUIAlignment.Center, new(3.8f, 3.8f), "circleMenu", GUI.API.VerticalHolder(Virial.Media.GUIAlignment.Center,
                MetaAbility.allEffectCircleInfo.Select(info => GUI.API.Button(Virial.Media.GUIAlignment.Center, maskedTittleAttr, GUI.API.ColorTextComponent(new(info.color), GUI.API.LocalizedTextComponent(info.translationKey)), button =>
                {
                    if (circle) circle.Disappear();
                    circle = EffectCircle.SpawnEffectCircle(null, GamePlayer.LocalPlayer.Position.ToUnityVector(), info.color, info.outerRadious.Invoke(), info.innerRadious.Invoke(), true);
                    window.CloseScreen();
                }))
                ), out _), out _);
        }
        void OpenWindow()
        {
            try
            {
                var window = MetaScreen.GenerateWindow(new Vector2(7.5f, 4.5f), HudManager.Instance.transform, new Vector3(0, 0, -400f), true, false);

                //widget.Append(new MetaWidgetOld.Text(TextAttributeOld.TitleAttr) { RawText = Language.Translate("role.metaRole.ui.roles") });

                var roleMaskedTittleAttr = GUI.API.GetAttribute(Virial.Text.AttributeAsset.MetaRoleButton);
                var roleTitleAttr = new Virial.Text.TextAttribute(roleMaskedTittleAttr) { Font = GUI.API.GetFont(Virial.Text.FontAsset.Gothic), Size = new(1.1f, 0.22f) };
                void SetPlayerWidget(int tab, MetaScreen window, GamePlayer p, Action callback)
                {

                    var holder = GUI.API.VerticalHolder(GUIAlignment.Center,
                        GUI.API.HorizontalHolder(Virial.Media.GUIAlignment.Center,
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "game.metaAbility.tabs.roles", (button) => SetPlayerWidget(0, window, p, callback), color: tab == 0 ? Virial.Color.Yellow : null),
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "game.metaAbility.tabs.modifiers", (button) => SetPlayerWidget(1, window, p, callback), color: tab == 1 ? Virial.Color.Yellow : null),
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "game.metaAbility.tabs.ghostRoles", (button) => SetPlayerWidget(2, window, p, callback), color: tab == 2 ? Virial.Color.Yellow : null),
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "game.metaAbility.tabs.perks", (button) => SetPlayerWidget(3, window, p, callback), color: tab == 3 ? Virial.Color.Yellow : null)),
                        GUI.API.HorizontalHolder(Virial.Media.GUIAlignment.Center,
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "button.label.kill", (button) => GamePlayer.LocalPlayer?.MurderPlayer(p, PlayerStates.Dead, EventDetails.Kill, KillParameter.RemoteKill,KillCondition.NoCondition), color: null),
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "button.label.revive", (button) => p.Revive(GamePlayer.LocalPlayer, p.TruePosition, true), color: null),
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "game.metaAbiliry.tabs.traps", (button) =>SetPlayerWidget(4,window,p,callback), color:tab == 4 ? Virial.Color.Yellow : null))
                        );

                    GUIWidget inner = GUIEmptyWidget.Default;
                    if (tab == 0)
                    {
                        inner = GUI.API.Arrange(Virial.Media.GUIAlignment.Center, Nebula.Roles.Roles.AllRoles.Where(r => r.ShowOnFreeplayScreen).Select(r => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, r.DisplayColoredName, button =>
                        {
                            if (r is Jackal)
                            {
                                p?.SetRole(r, Jackal.GenerateArgument(0, r));
                            }
                            else
                            {
                                p?.SetRole(r);
                            }
                            window.CloseScreen();
                            callback();
                        })), 4);
                    }
                    else if (tab == 1)
                    {
                        inner = GUI.API.VerticalHolder(Virial.Media.GUIAlignment.Center,
                            GUI.API.LocalizedText(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, "game.metaAbility.equipped"),
                            GUI.API.Arrange(Virial.Media.GUIAlignment.Center, Nebula.Roles.Roles.AllModifiers.Where(r => p!.Modifiers.Any(m => m.Modifier == r)).Select(r => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, r.DisplayColoredName, button =>
                            {
                                p?.RemoveModifier(r);
                                SetPlayerWidget(1,window,p,callback);
                            })), 4),
                            GUI.API.LocalizedText(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, "game.metaAbility.unequipped"),
                            GUI.API.Arrange(Virial.Media.GUIAlignment.Center, Nebula.Roles.Roles.AllModifiers.Where(r => r.ShowOnFreeplayScreen && !p!.Modifiers.Any(m => m.Modifier == r)).Select(r => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, r.DisplayColoredName, button =>
                            {
                                p?.AddModifier(r);
                                SetPlayerWidget(1,window,p,callback);
                            })), 4)
                            );
                    }
                    else if (tab == 2)
                    {
                        inner = GUI.API.Arrange(Virial.Media.GUIAlignment.Center, Nebula.Roles.Roles.AllGhostRoles.Where(r => r.ShowOnFreeplayScreen).Select(r => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, r.DisplayColoredName, button =>
                        {
                            p?.SetGhostRole(r);
                            window.CloseScreen();
                            callback();
                        })), 4);
                    }
                    else if (tab == 3)
                    {
                        Virial.Media.GUIWidget GetPerksWidget(IEnumerable<PerkFunctionalDefinition> perks) => GUI.API.Arrange(Virial.Media.GUIAlignment.Center, perks.Select(pk => pk.PerkDefinition.GetPerkImageWidget(true,
                            () =>
                            {
                                SetPerk.Invoke((pk.Id, p));
                                window.CloseScreen();
                                callback();
                            },
                            () => pk.PerkDefinition.GetPerkWidget())), 8);

                        inner = GUI.API.VerticalHolder(Virial.Media.GUIAlignment.Center,
                            GUI.API.HorizontalMargin(7.4f),
                            GUI.API.LocalizedText(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, "game.metaAbility.perks.standard"),
                            GetPerksWidget(Nebula.Roles.Roles.AllPerks.Where(pk => pk.PerkCategory == PerkFunctionalDefinition.Category.Standard)),
                            GUI.API.LocalizedText(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, "game.metaAbility.perks.noncrewmateOnly"),
                            GetPerksWidget(Nebula.Roles.Roles.AllPerks.Where(pk => pk.PerkCategory == PerkFunctionalDefinition.Category.NoncrewmateOnly))
                            );
                    }
                    else if (tab==4)
                    {
                        inner = GUI.API.Arrange(Virial.Media.GUIAlignment.Center, NebulaGameManager.Instance.AllPlayerInfo.Where(p2 => !p2.IsDisconnected).Select(p2 => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, p2.Unbox().ColoredDefaultName, button =>
                        {
                            p.ToAUPlayer().NetTransform.RpcSnapTo(p2.Position);
                            window.CloseScreen();
                            callback();
                        })), 4);
                    }
                        window.SetWidget(GUI.API.VerticalHolder(Virial.Media.GUIAlignment.Center, holder, GUI.API.VerticalMargin(0.15f), GUI.API.ScrollView(Virial.Media.GUIAlignment.Center, new(7.4f, 3f), null, inner, out _)), out _);
                }
                void SetWidget(int tab)
                {
                    var holder = GUI.API.VerticalHolder(GUIAlignment.Center,GUI.API.HorizontalHolder(Virial.Media.GUIAlignment.Center,
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "game.metaAbility.tabs.players", (button) => SetWidget(0), color: tab == 0 ? Virial.Color.Yellow : null),
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "game.metaAbility.tabs.roles", (button) => SetWidget(1), color: tab == 1 ? Virial.Color.Yellow : null),
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "game.metaAbility.tabs.modifiers", (button) => SetWidget(2), color: tab == 2 ? Virial.Color.Yellow : null),
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "game.metaAbility.tabs.ghostRoles", (button) => SetWidget(3), color: tab == 3 ? Virial.Color.Yellow : null)),
                        GUI.API.HorizontalHolder(Virial.Media.GUIAlignment.Center,
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "game.metaAbility.tabs.perks", (button) => SetWidget(4), color: tab == 4 ? Virial.Color.Yellow : null),
                        GUI.API.LocalizedButton(Virial.Media.GUIAlignment.Center, roleTitleAttr, "game.metaAbility.tabs.gameends", (button) => SetWidget(5), color: tab == 5 ? Virial.Color.Yellow : null))
                        );

                    GUIWidget inner = GUIEmptyWidget.Default;
                    if (tab == 0)
                    {
                        inner = GUI.API.Arrange(Virial.Media.GUIAlignment.Center, NebulaGameManager.Instance.AllPlayerInfo.Where(p => !p.IsDisconnected).Select(p => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, p.Unbox().ColoredDefaultName, button =>
                        {
                            var pwindow = MetaScreen.GenerateWindow(new Vector2(7.5f, 4.5f), HudManager.Instance.transform, new Vector3(0, 0, -400f), true, false);
                            SetPlayerWidget(0, pwindow, p, () => window.CloseScreen());
                        })), 4);
                    }
                    else if (tab == 1)
                    {
                        inner = GUI.API.Arrange(Virial.Media.GUIAlignment.Center, Nebula.Roles.Roles.AllRoles.Where(r => r.ShowOnFreeplayScreen).Select(r => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, r.DisplayColoredName, button =>
                        {
                            if (r is Jackal)
                            {
                                GamePlayer.LocalPlayer?.SetRole(r, Jackal.GenerateArgument(0, null));
                            }
                            else
                            {
                                GamePlayer.LocalPlayer?.SetRole(r);
                            }
                            window.CloseScreen();
                        })), 4);
                    }
                    else if (tab == 2)
                    {
                        inner = GUI.API.VerticalHolder(Virial.Media.GUIAlignment.Center,
                            GUI.API.LocalizedText(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, "game.metaAbility.equipped"),
                            GUI.API.Arrange(Virial.Media.GUIAlignment.Center, Nebula.Roles.Roles.AllModifiers.Where(r => GamePlayer.LocalPlayer!.Modifiers.Any(m => m.Modifier == r)).Select(r => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, r.DisplayColoredName, button =>
                            {
                                GamePlayer.LocalPlayer?.RemoveModifier(r);
                                SetWidget(2);
                            })), 4),
                            GUI.API.LocalizedText(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, "game.metaAbility.unequipped"),
                            GUI.API.Arrange(Virial.Media.GUIAlignment.Center, Nebula.Roles.Roles.AllModifiers.Where(r => r.ShowOnFreeplayScreen && !GamePlayer.LocalPlayer!.Modifiers.Any(m => m.Modifier == r)).Select(r => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, r.DisplayColoredName, button =>
                            {
                                GamePlayer.LocalPlayer?.AddModifier(r);
                                SetWidget(2);
                            })), 4)
                            );
                    }
                    else if (tab == 3)
                    {
                        inner = GUI.API.Arrange(Virial.Media.GUIAlignment.Center, Nebula.Roles.Roles.AllGhostRoles.Where(r => r.ShowOnFreeplayScreen).Select(r => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, r.DisplayColoredName, button =>
                        {
                            GamePlayer.LocalPlayer?.SetGhostRole(r);
                            window.CloseScreen();
                        })), 4);
                    }
                    else if (tab == 4)
                    {
                        Virial.Media.GUIWidget GetPerksWidget(IEnumerable<PerkFunctionalDefinition> perks) => GUI.API.Arrange(Virial.Media.GUIAlignment.Center, perks.Select(p => p.PerkDefinition.GetPerkImageWidget(true,
                            () =>
                            {
                                ModSingleton<ItemSupplierManager>.Instance?.SetPerk(p);
                                window.CloseScreen();
                            },
                            () => p.PerkDefinition.GetPerkWidget())), 8);

                        inner = GUI.API.VerticalHolder(Virial.Media.GUIAlignment.Center,
                            GUI.API.HorizontalMargin(7.4f),
                            GUI.API.LocalizedText(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, "game.metaAbility.perks.standard"),
                            GetPerksWidget(Nebula.Roles.Roles.AllPerks.Where(p => p.PerkCategory == PerkFunctionalDefinition.Category.Standard)),
                            GUI.API.LocalizedText(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, "game.metaAbility.perks.noncrewmateOnly"),
                            GetPerksWidget(Nebula.Roles.Roles.AllPerks.Where(p => p.PerkCategory == PerkFunctionalDefinition.Category.NoncrewmateOnly))
                            );
                    }
                    else if (tab == 5)
                    {
                        inner = GUI.API.Arrange(Virial.Media.GUIAlignment.Center, GameEnd.AllEndConditions.Select(r => GUI.API.RawButton(Virial.Media.GUIAlignment.Center, roleMaskedTittleAttr, r.DisplayText.GetString().Replace("%EXTRA%","").Color(r.Color), button =>
                        {
                            NebulaAPI.CurrentGame?.TriggerGameEnd(r, GameEndReason.Special);
                            window.CloseScreen();
                        })), 4);
                    }
                    window.SetWidget(GUI.API.VerticalHolder(Virial.Media.GUIAlignment.Center, holder, GUI.API.VerticalMargin(0.15f), GUI.API.ScrollView(Virial.Media.GUIAlignment.Center, new(7.4f, 3f), null, inner, out _)), out _);
                }

                SetWidget(0);
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
    }
}
