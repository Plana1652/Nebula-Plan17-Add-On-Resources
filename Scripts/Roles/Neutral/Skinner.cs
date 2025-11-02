using Nebula.Roles.Ghost.Neutral;
using Plana.Core;
using Plana.Roles.Crewmate;
using UnityEngine.UI;

namespace Plana.Roles.Neutral;

public class Skinner : DefinedRoleTemplate, HasCitation, DefinedRole
{
    public static Team RoleTeam = new Team("teams.skinner", new(121,138,190), TeamRevealType.OnlyMe);
    private Skinner() : base("skinner", RoleTeam.Color, RoleCategory.NeutralRole, RoleTeam) { }

    Citation? HasCitation.Citation => PCitations.PlanaANDKC;

    RuntimeRole RuntimeAssignableGenerator<RuntimeRole>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public Skinner MyRole = new Skinner();
    public class Instance : RuntimeAssignableTemplate, RuntimeRole
    {
        public static readonly GameEnd SkinnerWin = NebulaAPI.Preprocessor!.CreateEnd("skinner", MyRole.RoleColor, 254);
        public static readonly ExtraWin ScarletLoverExtraWin = NebulaAPI.Preprocessor!.CreateExtraWin("scarletex", ((DefinedAssignable)Scarlet.MyRole).Color);
        public static readonly ExtraWin JackalExtraWin = NebulaAPI.Preprocessor!.CreateExtraWin("jackalex", ((DefinedAssignable)Jackal.MyRole).Color);
        public static readonly ExtraWin KnightedExtraWin = NebulaAPI.Preprocessor!.CreateExtraWin("knightedTargetEx", ((DefinedAssignable)Knighted.MyRole).Color);
        public static readonly ExtraWin GhostExtraWin = NebulaAPI.Preprocessor!.CreateExtraWin("ghostextrawin",Virial.Color.White);
        DefinedRole RuntimeRole.Role => MyRole;
        [EventPriority(90)]
        void OnCheckGameEnd(EndCriteriaMetEvent ev)
        {
            if (MyPlayer.TryGetModifier<HasLove.Instance>(out var m))
            {
                return;
            }
            if (MyPlayer.IsDead)
            {
                return;
            }
            if (ev.EndReason==GameEndReason.Task||ev.EndReason==GameEndReason.Sabotage)
            {
                return;
            }
            if (NebulaGameManager.Instance!.AllPlayerInfo.Any(p=>p.TryGetModifier<SkinnerDog.Instance>(out var d)&&(ev.Winners.Test(p)||ev.OverwrittenGameEnd==NebulaGameEnd.SpectreWin&&(ev.Winners.Test(p)||ev.LastWinners.Test(p)))))
            {
                try
                {
                    var mask = BitMasks.AsPlayer();
                    try
                    {
                        var scarlet = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => p.Role is Scarlet.Instance && p.TryGetModifier<SkinnerDog.Instance>(out var g));
                        if (scarlet != null)
                        {
                            var r = scarlet.Role as Scarlet.Instance;
                            var p = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => p.GetModifiers<ScarletLover.Instance>().Any((ScarletLover.Instance f) => f.FlirtatiousId == r.FlirtatiousId&&f.AmFavorite));
                            if (p != null)
                            {
                                mask.Add(p);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        PDebug.Log("ScarletAddWinnerFail Error:" + e.ToString());
                    }
                    try
                    {
                        NebulaGameManager.Instance.AllPlayerInfo.Where(p => p.Role is Jackal.Instance && p.TryGetModifier<SkinnerDog.Instance>(out var d)).Do(jackal =>
                        {
                            var j = jackal.Role as Jackal.Instance;
                            NebulaGameManager.Instance.AllPlayerInfo.Where(player => j.IsMySidekick(player)).Do(p =>
                            {
                                mask.Add(p);
                            });
                        });
                    }
                    catch (Exception e)
                    {
                        PDebug.Log("JackalAddWinnerFail Error:" + e.ToString());
                    }
                    try
                    {
                        if (NebulaGameManager.Instance.AllPlayerInfo.Any(p => p.Role is Knighted.Instance&&p.TryGetModifier<SkinnerDog.Instance>(out var d)))
                        {
                            NebulaGameManager.Instance.AllPlayerInfo.Where(p => p.TryGetModifier<KnightedTarget.Instance>(out var m)).Do(k =>
                            {
                                mask.Add(k);
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        PDebug.Log("KnightedAddWinnerFail Error:" + e.ToString());
                    }
                    try
                    {
                        if (NebulaGameManager.Instance.AllPlayerInfo.Any(p => p.Role is Yandere.Instance && p.TryGetModifier<SkinnerDog.Instance>(out var d)))
                        {
                            NebulaGameManager.Instance.AllPlayerInfo.Where(p => p.TryGetModifier<YandereLover.Instance>(out var m)).Do(k =>
                            {
                                mask.Add(k);
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        PDebug.Log("YandereAddWinnerFail Error:" + e.ToString());
                    }
                    try
                    {
                        var dancer = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => p.Role is Dancer.Instance && p.TryGetModifier<SkinnerDog.Instance>(out var g));
                        if (dancer != null)
                        {
                            var r = dancer.Role as Dancer.Instance;
                            IEnumerable<Player> ps = NebulaGameManager.Instance!.AllPlayerInfo.Where(p =>
                            {
                                if (r.completedDanceLooked.Contains(p) 
                                || (r.activeDanceLooked.Contains(p) && p.IsDead)
                                || (ev.Winners.Test(p.MyKiller) && p.PlayerState == PlayerState.Frenzied))
                                {
                                    return true;
                                }
                                return false;
                            });
                            if (ps != null)
                            {
                                ps.Do(p=>mask.Add(p));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        PDebug.Log("DancerAddWinnerFail Error:" + e.ToString());
                    }
                    int winners = NebulaGameManager.Instance.AllPlayerInfo.Where((Player p) => mask.Test(p)).Aggregate(0, (int v, Player p) => v | (1 << (int)p.PlayerId));
                    ev.TryOverwriteEnd(SkinnerWin, GameEndReason.Special, winners);
                  
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }
        }
        void OnGameEnd(GameEndEvent ev)
        {
            if (ev.EndState.EndCondition==SkinnerWin)
            {
                try
                {
                    var scarlet = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => p.Role is Scarlet.Instance && p.TryGetModifier<SkinnerDog.Instance>(out var g));
                    if (scarlet != null)
                    {
                        var r = scarlet.Role as Scarlet.Instance;
                        var p = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => p.GetModifiers<ScarletLover.Instance>().Any((ScarletLover.Instance f) => f.FlirtatiousId == r.FlirtatiousId&&f.AmFavorite));
                        if (p != null&&ev.EndState.Winners.Test(p))
                        {
                            (ev.EndState.ExtraWins as EditableBitMask<ExtraWin>).Add(ScarletLoverExtraWin);
                        }
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log("ScarletAddWinnerFail Error:" + e.ToString());
                }
                try
                {
                    if (NebulaGameManager.Instance.AllPlayerInfo.Any(p => p.Role is Jackal.Instance && p.TryGetModifier<SkinnerDog.Instance>(out var d)))
                    {
                        (ev.EndState.ExtraWins as EditableBitMask<ExtraWin>).Add(JackalExtraWin);
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log("JackalAddWinnerFail Error:" + e.ToString());
                }
                try
                {
                    if (NebulaGameManager.Instance.AllPlayerInfo.Any(p => p.Role is Knighted.Instance&&p.TryGetModifier<SkinnerDog.Instance>(out var do2)))
                    {
                        if (NebulaGameManager.Instance.AllPlayerInfo.Any(p => p.TryGetModifier<KnightedTarget.Instance>(out var m)))
                        {
                            (ev.EndState.ExtraWins as EditableBitMask<ExtraWin>).Add(KnightedExtraWin);
                        }
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log("KnightedAddWinnerFail Error:" + e.ToString());
                }
                try
                {
                    var dancer = NebulaGameManager.Instance!.AllPlayerInfo.FirstOrDefault(p => p.Role is Dancer.Instance && p.TryGetModifier<SkinnerDog.Instance>(out var g));
                    if (dancer != null)
                    {
                        (ev.EndState.ExtraWins as EditableBitMask<ExtraWin>).Add(GhostExtraWin);
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log("DancerAddWinnerFail Error:" + e.ToString());
                }
            }
        }
        [Local]
        private void OnSetRole(PlayerRoleSetEvent ev)
        {
            if (ev.Role.Role.Category !=RoleCategory.NeutralRole&&ev.Player.TryGetModifier<SkinnerDog.Instance>(out var d))
            {
                ev.Player.RemoveModifier(SkinnerDog.MyRole);
            }
            if (MyPlayer.IsDead)
            {
                return;
            }
            if (ev.Role is Sidekick.Instance)
            {
                return;
            }
            if (ev.Role is Tunny.Instance) return;
            if (ev.Role is Lawyer.Instance) return;
            if (ev.Role is Pursuer.Instance) return;
            if (ev.Role is GameMaster.Instance) return;
            if (ev.Player.TryGetModifier<HasLove.Instance>(out var d2))
            {
                return;
            }
            if (ev.Role.Role.Category==RoleCategory.NeutralRole&&!ev.Player.TryGetModifier<SkinnerDog.Instance>(out var dog))
            {
                ev.Player.AddModifier(SkinnerDog.MyRole);
            }
        }
        /*[Local]
        private void OnSetModifier(PlayerModifierSetEvent ev)
        {
            if (ev.Modifier is SidekickModifier.Instance && !ev.Player.TryGetModifier<SkinnerDog.Instance>(out var dog))
            {
                ev.Player.AddModifier(SkinnerDog.MyRole);
            }
        }*/
        [OnlyMyPlayer]
        void GuessEdited(PlayerCanGuessPlayerLocalEvent ev)
        {
            if (ev.Guesser.TryGetModifier<SkinnerDog.Instance>(out var d))
            {
                ev.CanGuess = false;
            }
        }

        [Local]
        void DecorateName(PlayerDecorateNameEvent ev)
        {
            if (ev.Player.TryGetModifier<SkinnerDog.Instance>(out var d))
            {
                ev.Color = MyRole.RoleColor;
                if (!MyPlayer.IsDead)
                {
                    ev.Name += " ◆";
                }
            }
            if (ev.Player.Role is Skinner.Instance)
            {
                ev.Color = MyRole.RoleColor;
            }
        }
        bool isSameTeam(GamePlayer player)
        {
            if (player.Role is Skinner.Instance)
            {
                return true;
            }
            if (player.TryGetModifier<SkinnerDog.Instance>(out var d))
            {
                return true;
            }
            return false;
        }
        void CheckWins(PlayerCheckWinEvent ev)
        {
            ev.SetWinIf(ev.GameEnd == SkinnerWin&&isSameTeam(ev.Player));
        }

        public Instance(GamePlayer player) : base(player)
        {
        }
        [Local,OnlyMyPlayer]
        void OnDead(PlayerDieOrDisconnectEvent ev)
        {
            NebulaGameManager.Instance?.AllPlayerInfo.Where(p => p.TryGetModifier<SkinnerDog.Instance>(out var m)).Do(p =>
            {
                p.RemoveModifier(SkinnerDog.MyRole);
            });
        }
        void OnMeetingStart(MeetingStartEvent ev)
        {
            ShowMarkArrow.Invoke((MyPlayer, null));
            arrow?.Release();
            alwaysArrow?.Release();
            arrow = null;
            alwaysArrow = null;
            select = null;
        }
        Virial.Media.Image markImage = NebulaAPI.AddonAsset.GetResource("markbutton.png")!.AsImage(115f)!,showmarkImage=NebulaAPI.AddonAsset.GetResource("ShowMarkbutton.png")!.AsImage(115f)!;
        GamePlayer select;
        static TrackingArrowAbility arrow, alwaysArrow;
        static RemoteProcess<(GamePlayer,GamePlayer)> ShowMarkArrow = new("SkinnerShowMarkArrow", (p, _) =>
        {
            if (!(GamePlayer.LocalPlayer.Role is Skinner.Instance) && !GamePlayer.LocalPlayer.TryGetModifier<SkinnerDog.Instance>(out var d))
            {
                return;
            }
            if (p.Item2==null)
            {
                if (arrow != null)
                {
                    arrow.Release();
                    arrow = null;
                }
                return;
            }
            arrow = new TrackingArrowAbility(p.Item2, 0f, MyRole.RoleColor.ToUnityColor(), false).Register(p.Item1.Role);
        });
        ModAbilityButton markButton, showMarkButton;
        void RuntimeAssignable.OnActivated()
        {
            arrow = null;
            if (AmOwner)
            {
                select = null;
                NebulaGameManager.Instance?.AllPlayerInfo.Where(p => !p.IsCrewmate && !p.IsImpostor&&!(p.Role is Skinner.Instance)).Do(p =>
                {
                    if (p.Role is Tunny.Instance) return;
                    if (p.Role is Lawyer.Instance) return;
                    if (p.Role is Pursuer.Instance) return;
                    if (p.Role is GameMaster.Instance) return;
                    p.AddModifier(SkinnerDog.MyRole);
                });
                ObjectTracker<GamePlayer> myTracker = ObjectTrackers.ForPlayer(this, null, base.MyPlayer, ObjectTrackers.StandardPredicate, null, false, false);
                markButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.Ability, null);
                markButton.Availability = (button) => myTracker.CurrentTarget != null && this.MyPlayer.CanMove;
                markButton.Visibility = (button) => !base.MyPlayer.IsDead && select == null;
                markButton.SetImage(markImage);
                markButton.OnClick = delegate (ModAbilityButton button)
                {
                    var p = myTracker.CurrentTarget;
                    select = p!;
                    alwaysArrow=new TrackingArrowAbility(p, 0f, MyRole.RoleColor.ToUnityColor(), false).Register(this);
                };
                markButton.SetLabel("mark");
                showMarkButton = NebulaAPI.Modules.AbilityButton(this, false, false, 0, false).BindKey(Virial.Compat.VirtualKeyInput.SecondaryAbility, null);
                showMarkButton.Availability = (button) => this.MyPlayer.CanMove&&select!=null;
                showMarkButton.Visibility = (button) => !base.MyPlayer.IsDead;
                showMarkButton.SetImage(showmarkImage);
                showMarkButton.OnClick = delegate (ModAbilityButton button)
                {
                    button.StartEffect();
                };
                showMarkButton.OnEffectStart = (button) =>
                {
                    ShowMarkArrow.Invoke((MyPlayer, select));
                };
                showMarkButton.OnEffectEnd = (button) =>
                {
                    ShowMarkArrow.Invoke((MyPlayer, null));
                    showMarkButton.StartCoolDown();
                };
                showMarkButton.SetLabel("showmark");
                showMarkButton.CoolDownTimer = NebulaAPI.Modules.Timer(this, 10f).SetAsAbilityTimer().Start(null);
                showMarkButton.EffectTimer = NebulaAPI.Modules.Timer(this, 5f).Start(null);
                showMarkButton.StartCoolDown();
            }
        }
        void RuntimeAssignable.OnInactivated()
        {
            NebulaGameManager.Instance?.AllPlayerInfo.Where(p => p.TryGetModifier<SkinnerDog.Instance>(out var m)).Do(p =>
            {
                p.RemoveModifier(SkinnerDog.MyRole);
            });
        }
    }
}
public class SkinnerDog : DefinedModifierTemplate, DefinedModifier, DefinedAssignable, IRoleID, RuntimeAssignableGenerator<RuntimeModifier>, HasCitation
{
    private SkinnerDog() : base("skinnerdog", new(121,138,190),null,true,()=>false) { }

    Citation? HasCitation.Citation => PCitations.PlanaANDKC;

    RuntimeModifier RuntimeAssignableGenerator<RuntimeModifier>.CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    static public SkinnerDog MyRole = new SkinnerDog();
    public class Instance : RuntimeAssignableTemplate, RuntimeModifier
    {
        DefinedModifier RuntimeModifier.Modifier => MyRole;

        public Instance(GamePlayer player) : base(player)
        {
        }
        [Local]
        void DecorateName(PlayerDecorateNameEvent ev)
        {
            if (ev.Player.Role is Skinner.Instance)
            {
                ev.Color = MyRole.RoleColor;
            }
        }
        [OnlyMyPlayer]
        void OnCheckCanKill(PlayerCheckCanKillLocalEvent ev)
        {
            Skinner.Instance s = ev.Target.Role as Skinner.Instance;
            if (s!=null)
            {
                ev.SetAsCannotKillBasically();
            }
        }
        [Local, OnlyMyPlayer]
        void OnDead(PlayerDieEvent ev)
        {
            if (MyPlayer.Role is Knighted.Instance||MyPlayer.Role is Lawyer.Instance)
            {
                MyPlayer.RemoveModifier(SkinnerDog.MyRole);
            }
        }
        void RuntimeAssignable.OnActivated()
        {
        }
        void RuntimeAssignable.DecorateNameConstantly(ref string name, bool canSeeAllInfo)
        {
            if (AmOwner||canSeeAllInfo)
            {
                name += " ◆".Color(MyRole.RoleColor.ToUnityColor());
            }
        }
    }
}
