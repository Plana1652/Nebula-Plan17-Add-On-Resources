using Nebula.Roles;
using Nebula.Roles.Complex;
using Plana.Core;
using Plana.Roles.Crewmate;
using Plana.Roles.Impostor;
using Plana.Roles.Modifier;
using Plana.Roles.Neutral;
using Virial.Assignable;

namespace Plana.Core;

/*static public class MeetingRoleSelectWindow
{
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
}*/

[NebulaRPCHolder]
public static class GuesserSystem
{
    public static void GuessedLockTargets(GamePlayer __instance, TextMeshPro roleText, bool inMeeting)
    {
        if (inMeeting)
        {
            if (playerGuess.TryGetValue(__instance.PlayerId, out var roleid))
            {
                roleText.text += "(".Color(UnityEngine.Color.white) + Language.Translate("guessLockedText").Replace("%LOCKEDTEXT2%", Language.Translate("guessLockedText2")).Color(NebulaTeams.ImpostorTeam.Color.ToUnityColor()).Replace("%ROLE%", Nebula.Roles.Roles.AllRoles[roleid].DisplayColoredName) + ")".Color(UnityEngine.Color.white);
            }
        }
    }
    private record MisguessedExtraDeadInfo(GamePlayer To, DefinedRole Role) : GamePlayer.ExtraDeadInfo(PlayerStates.Misguessed)
    {
        public override string ToStateText() => To.PlayerName + " as " + Role.DisplayColoredName;
    }
   public static RemoteProcess<(GamePlayer guesser, GamePlayer to, DefinedRole role)> RpcShareExtraInfo = new("ShareExInfoGuessOfGuesserEX",
        (message, _) => {
            message.guesser.PlayerStateExtraInfo = new MisguessedExtraDeadInfo(message.to, message.role);
        }
    );

    public static MetaScreen LastGuesserWindow = null!;

    static public MetaScreen OpenGuessWindow(int leftGuessPerMeeting, int leftGuess, Action<DefinedRole> onSelected)
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)125)==1)
        {
            SoundManager.instance.PlaySound(PatchManager.GetSound("Gunload"), false, 1f);
        }
        string leftStr;
        if (leftGuessPerMeeting < leftGuess)
            leftStr = $"{leftGuessPerMeeting.ToString()} ({leftGuess.ToString()})";
        else
            leftStr = leftGuess.ToString();
        return MeetingRoleSelectWindow.OpenRoleSelectWindow(r =>
        {
            var rSpawnable = r.IsSpawnable;
            if (PatchManager.GuesserAbilityGuessMode && !rSpawnable)
            {
                rSpawnable = AssignmentType.AllTypes.Any(t =>
                {
                    if (t.IsActive && t.Predicate(r.AssignmentStatus, r))
                    {
                        AllocationParameters customAllocationParameters = r.GetCustomAllocationParameters(t);
                        return ((customAllocationParameters != null) ? customAllocationParameters.RoleCountSum : 0) > 0;
                    }
                    return false;
                });
            }
            return r.CanBeGuess && (!PatchManager.GuesserCanGuessNoSpawnableRole || rSpawnable);
        }, Language.Translate("role.guesser.leftGuess") + " : " + leftStr, onSelected);
    }
    static public MetaScreen DoomsayerOpenGuessWindow(Action<DefinedRole> onSelected)
    {
        if (ClientOption.GetValue((ClientOption.ClientOptionType)125) == 1)
        {
            SoundManager.instance.PlaySound(PatchManager.GetSound("Gunload"), false, 1f);
        }
        return MeetingRoleSelectWindow.OpenRoleSelectWindow(r =>
        {
            var rSpawnable = r.IsSpawnable;
            if (PatchManager.GuesserAbilityGuessMode && !rSpawnable)
            {
                rSpawnable = AssignmentType.AllTypes.Any(t =>
                {
                    if (t.IsActive && t.Predicate(r.AssignmentStatus, r))
                    {
                        AllocationParameters customAllocationParameters = r.GetCustomAllocationParameters(t);
                        return ((customAllocationParameters != null) ? customAllocationParameters.RoleCountSum : 0) > 0;
                    }
                    return false;
                });
            }
            return r.CanBeGuess && (!PatchManager.GuesserCanGuessNoSpawnableRole || rSpawnable);
        }, "", onSelected);
    }
    internal static DividedSpriteLoader Icons = DividedSpriteLoader.FromResource("Nebula.Resources.MeetingButtons.png", 115f, 100, 110, true);
    internal static Image DoubleGuessIcon = NebulaAPI.AddonAsset.GetResource("DoubleGuess.png").AsImage(115f);
    public static RemoteProcess<ValueTuple<GamePlayer, GamePlayer, int,bool>> GuessMessageRPC = new RemoteProcess<ValueTuple<GamePlayer, GamePlayer, int,bool>>("GuessMessage", delegate
    (ValueTuple<GamePlayer, GamePlayer, int,bool> message, bool _)
    {
        if (PlayerControl.LocalPlayer.Data.IsDead)
        {
            HudManager.Instance.Chat.AddChat(message.Item1.ToAUPlayer(), Language.Translate("role.guesser.guessText").Replace("%NAME%", message.Item2.ToAUPlayer().name).Replace("%ROLE%", Nebula.Roles.Roles.AllRoles[message.Item3].DisplayColoredName));
        }
        if (message.Item4)
        {
            if (ClientOption.GetValue((ClientOption.ClientOptionType)125) == 1)
            {
                SoundManager.instance.PlaySound(PatchManager.GetSound("Gunfire"), false, 1f);
            }
            else
            {
                SoundManager.instance.PlaySound(message.Item2.ToAUPlayer().KillSfx, false, 0.8f);
            }
        }
        else
        {
            if (message.Item1.AmOwner || PlayerControl.LocalPlayer.Data.IsDead)
            {
                if (ClientOption.GetValue((ClientOption.ClientOptionType)125) == 1)
                {
                    SoundManager.instance.PlaySound(PatchManager.GetSound("Gunfire"), false, 1f);
                }
                else
                {
                    SoundManager.instance.PlaySound(message.Item2.ToAUPlayer().KillSfx, false, 0.8f);
                }
            }
        }
    });
    public static RemoteProcess<ValueTuple<GamePlayer, GamePlayer, int,bool>> DoomsayerGuessMessageRPC = new RemoteProcess<ValueTuple<GamePlayer, GamePlayer, int,bool>>("DoomsayerGuessMessage", delegate
(ValueTuple<GamePlayer, GamePlayer, int,bool> message, bool _)
    {
        if (PlayerControl.LocalPlayer.Data.IsDead)
        {
            HudManager.Instance.Chat.AddChat(message.Item1.ToAUPlayer(), Language.Translate("role.doomsayer.guessText").Replace("%NAME%", message.Item2.ToAUPlayer().name).Replace("%ROLE%", Nebula.Roles.Roles.AllRoles[message.Item3].DisplayColoredName));
        }
        if (message.Item4||message.Item1.AmOwner||PlayerControl.LocalPlayer.Data.IsDead)
        {
            if (ClientOption.GetValue((ClientOption.ClientOptionType)125) == 1)
            {
                SoundManager.instance.PlaySound(PatchManager.GetSound("Gunfire"), false, 1f);
            }
            else
            {
                SoundManager.instance.PlaySound(message.Item2.ToAUPlayer().KillSfx, false, 0.8f);
            }
        }
    });
    public static bool CheckGuess(GamePlayer target,DefinedRole r)
    {
        if (target==null)
        {
            return false;
        }
        if (target.Role.ExternalRecognitionRole==r)
        {
            return true;
        }
        if (PatchManager.GuesserAbilityGuessMode)
        {
            IPlayerAbility ability = r.GetAbilityOnRole(target, AbilityAssignmentStatus.KillersSide | AbilityAssignmentStatus.CanLoadToNeutral, Array.Empty<int>());
            try
            {
                if (ability != null && target.AllAbilities.Any(r => ability.GetType() == r.GetType()))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                PDebug.Log(e);
            }
        }
        return false;
    }
    public static Dictionary<byte, int> playerGuess = new Dictionary<byte, int>();
    static public void OnMeetingStart(int leftGuess, Action guessDecrementer, bool isDoubleGuess = false, Func<bool>? guessIf = null,bool isLI=false)
    {
        bool awareOfUsurpation = false;
        playerGuess = new Dictionary<byte, int>();
        int leftGuessPerMeeting = isLI?99:Nebula.Roles.Complex.Guesser.NumOfGuessPerMeetingOption;
        PDebug.Log("RegisterGuesserUI");
        NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(isDoubleGuess ? DoubleGuessIcon : Icons.AsLoader(0),
            state =>
            {
                var p = state.MyPlayer;
                try
                {
                    PDebug.Log("StartGenGuessUI");
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        if (PlayerControl.LocalPlayer.Data.IsDead) return;
                        if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;
                        if (!MeetingHudExtension.CanUseAbilityFor(p, true))
                        {
                            return;
                        }
                        if (playerGuess.TryGetValue(p.PlayerId, out int roleid))
                        {
                            if (CheckGuess(p,Nebula.Roles.Roles.GetRole(roleid)))
                            {
                                if (!p.IsDead)
                                {
                                    GamePlayer.LocalPlayer.MurderPlayer(p!, PlayerState.Guessed, EventDetail.Guess, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, (result) =>
                                    {
                                        GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, roleid, result == KillResult.Kill));
                                    });
                                    leftGuess--;
                                    leftGuessPerMeeting--;
                                    if (GamePlayer.LocalPlayer.TryGetAbility<SpeechEater.Ability>(out var ability))
                                    {
                                        if (ability.SelectPlayer != null && ability.SelectPlayer.PlayerId == p.PlayerId)
                                        {
                                            new StaticAchievementToken("SpeechEater.challenge1");
                                        }
                                    }
                                    return;
                                }
                            }
                            else if (isDoubleGuess)
                            {
                                var ability = GamePlayer.LocalPlayer.Role.GetAbility<SpeechEater.Ability>();
                                if (ability == null)
                                {
                                    PDebug.Log("No Found SpeechEater Ability");
                                    return;
                                }
                                if (!ability.GuessNoDead)
                                {
                                    GamePlayer.LocalPlayer.MurderPlayer(GamePlayer.LocalPlayer, PlayerState.Misguessed, EventDetail.Missed, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole, (result) =>
                                    {
                                        GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, roleid, result == KillResult.Kill));
                                    });
                                    RpcShareExtraInfo.Invoke((GamePlayer.LocalPlayer, p!, Nebula.Roles.Roles.AllRoles[roleid]));
                                    new StaticAchievementToken("SpeechEater.another1");
                                    leftGuess--;
                                    leftGuessPerMeeting--;
                                    return;
                                }
                                AmongUsUtil.PlayCustomFlash(UnityEngine.Color.red, 0.4f, 1f, 0.75f);
                                GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, roleid, false));
                                ability.GuessNoDead = false;
                                leftGuess--;
                                leftGuessPerMeeting--;
                                return;
                            }
                            else
                            {
                                GamePlayer.LocalPlayer.MurderPlayer(GamePlayer.LocalPlayer, PlayerState.Misguessed, EventDetail.Missed, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, (result) =>
                                {
                                    GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, roleid, result == KillResult.Kill));
                                });
                                RpcShareExtraInfo.Invoke((GamePlayer.LocalPlayer, p!, Nebula.Roles.Roles.AllRoles[roleid]));
                                leftGuess--;
                                leftGuessPerMeeting--;
                                return;
                            }
                            PDebug.Log("TryGuessed");
                        }
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
                LastGuesserWindow = OpenGuessWindow(leftGuessPerMeeting, leftGuess, (r) =>
                {
                    try
                    {
                        if (PlayerControl.LocalPlayer.Data.IsDead) return;
                        if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;
                        if (!MeetingHudExtension.CanUseAbilityFor(p, true))
                        {
                            return;
                        }

                        PDebug.Log("CheckGuessCorrect");
                        if (guessIf?.Invoke() ?? true)
                        {
                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                PDebug.Log("addTryGuessed");
                                if (!playerGuess.TryGetValue(p.PlayerId, out int roleid))
                                {
                                    playerGuess.Add(p.PlayerId, r.Id);
                                }
                                goto il_2;
                            }
                            else if (CheckGuess(p,r))
                            {
                                if (!p.IsDead)
                                {
                                    try
                                    {
                                        PDebug.Log("GuessCorrect");
                                        GamePlayer.LocalPlayer.MurderPlayer(p!, PlayerState.Guessed, EventDetail.Guess, KillParameter.WithOverlay|KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, (result) =>
                                        {
                                            GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, r.Id, result == KillResult.Kill));
                                        });
                                        if (GamePlayer.LocalPlayer.TryGetAbility<SpeechEater.Ability>(out var ability))
                                        {
                                            if (ability.SelectPlayer != null && ability.SelectPlayer.PlayerId == p.PlayerId)
                                            {
                                                new StaticAchievementToken("SpeechEater.challenge1");
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        PDebug.Log(e);
                                    }
                                }
                            }
                            else if (isDoubleGuess)
                            {
                                var ability = GamePlayer.LocalPlayer.Role.GetAbility<SpeechEater.Ability>();
                                if (ability == null)
                                {
                                    PDebug.Log("No Found SpeechEater Ability");
                                    return;
                                }
                                if (!ability.GuessNoDead)
                                {
                                    PDebug.Log("DoubleGuessWrong2");
                                    GamePlayer.LocalPlayer.MurderPlayer(GamePlayer.LocalPlayer, PlayerState.Misguessed, EventDetail.Missed, KillParameter.WithOverlay|KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, (result) =>
                                    {
                                        GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, r.Id, result == KillResult.Kill));
                                    });
                                    RpcShareExtraInfo.Invoke((GamePlayer.LocalPlayer, p!, r));
                                    new StaticAchievementToken("SpeechEater.another1");
                                    goto il_1;
                                }
                                PDebug.Log("DoubleGuessWrong1");
                                AmongUsUtil.PlayCustomFlash(UnityEngine.Color.red, 0.4f, 1f, 0.75f);
                                GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, r.Id, false));
                                ability.GuessNoDead = false;
                            }
                            else
                            {
                                PDebug.Log("GuessWrong");
                                GamePlayer.LocalPlayer.MurderPlayer(GamePlayer.LocalPlayer, PlayerState.Misguessed, EventDetail.Missed, KillParameter.WithOverlay|KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, (result) =>
                                {
                                    GuessMessageRPC.Invoke((GamePlayer.LocalPlayer, p, r.Id, result == KillResult.Kill));
                                });
                                RpcShareExtraInfo.Invoke((GamePlayer.LocalPlayer, p!, r));
                            }
                        }
                        else
                        {
                            NebulaAsset.PlaySE(NebulaAudioClip.ButtonBreaking, false, 1f, 1f);
                            awareOfUsurpation = true;
                        }
                    il_1:;
                        PDebug.Log("GuessEnd");
                        guessDecrementer.Invoke();
                        leftGuess--;
                        leftGuessPerMeeting--;
                    il_2:;
                        if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
                        LastGuesserWindow = null!;
                    }
                    catch (Exception e)
                    {
                        PDebug.Log(e);
                    }
                }
                );
            },
            p => !awareOfUsurpation&&!p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && leftGuess > 0 && leftGuessPerMeeting > 0 && !PlayerControl.LocalPlayer.Data.IsDead && GameOperatorManager.Instance!.Run(new PlayerCanGuessPlayerLocalEvent(NebulaAPI.CurrentGame!.LocalPlayer, p.MyPlayer, true)).CanGuess
            ));

        /*
        List<GameObject> guessIcons = new();

        foreach (var playerVoteArea in MeetingHud.Instance.playerStates)
        {
            if (playerVoteArea.AmDead || playerVoteArea.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

            GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
            GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
            guessIcons.Add(targetBox);
            targetBox.name = "ShootButton";
            targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1f);
            SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
            renderer.sprite = targetSprite.GetSprite();
            PassiveButton button = targetBox.GetComponent<PassiveButton>();
            button.OnClick.RemoveAllListeners();


            var player = NebulaGameManager.Instance?.GetModPlayerInfo(playerVoteArea.TargetPlayerId);
            button.OnClick.AddListener(() =>
            {
                if (PlayerControl.LocalPlayer.Data.IsDead) return;
                if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;

                LastGuesserWindow = OpenGuessWindow(leftGuessPerMeeting, leftGuess, (r) =>
                {
                    if (PlayerControl.LocalPlayer.Data.IsDead) return;
                    if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;

                    if (player?.Role.Role == r)
                        PlayerControl.LocalPlayer.ModMeetingKill(player!.MyControl, true, PlayerState.Guessed, EventDetail.Guess);
                    else
                        PlayerControl.LocalPlayer.ModMeetingKill(PlayerControl.LocalPlayer, true, PlayerState.Misguessed, EventDetail.Missed);

                    //のこり推察数を減らす
                    guessDecrementer.Invoke();
                    leftGuess--;
                    leftGuessPerMeeting--;

                    if (leftGuess <= 0 || leftGuessPerMeeting <= 0) foreach (var obj in guessIcons) GameObject.Destroy(obj);
                    

                    if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
                    LastGuesserWindow = null!;
                });
            });
        }
        */
    }
    static bool DGuessWrong;
    static RemoteProcess<(GamePlayer,GamePlayer)> KillShowAnimRpc = new RemoteProcess<(GamePlayer, GamePlayer)>("KillShowAnimRpc", delegate ((GamePlayer, GamePlayer)
        message, bool _)
    {
        if (!message.Item1.AmOwner&&!message.Item2.AmOwner)
        {
            HudManager.Instance.KillOverlay.ShowKillAnimation(message.Item2.ToAUPlayer().Data, message.Item2.ToAUPlayer().Data);
        }
    });
    public static List<GamePlayer> doomsayerGuessKillPlayers = new List<GamePlayer>();
    static GamePlayer lastguesswrong;
    static public void DoomsayerOnMeetingStart(Action guessCorrectAction,bool cankillPlayer,bool showanim=true)
    {
        DGuessWrong = false;
        playerGuess = new Dictionary<byte, int>();
        NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(Icons.AsLoader(0),
            state =>
            {
                var p = state.MyPlayer;
                try
                {
                    PDebug.Log("StartGenDGuessUI");
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        if (PlayerControl.LocalPlayer.Data.IsDead) return;
                        if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;
                        if (!MeetingHudExtension.CanUseAbilityFor(p, true))
                        {
                            return;
                        }
                        if (playerGuess.TryGetValue(p.PlayerId, out int roleid))
                        {
                            if (CheckGuess(p,Nebula.Roles.Roles.GetRole(roleid)))
                            {
                                if (!p.IsDead)
                                {
                                    doomsayerGuessKillPlayers.Add(p);
                                    if (doomsayerGuessKillPlayers.Count((GamePlayer player) => player.IsImpostor) >= 3)
                                    {
                                        new StaticAchievementToken("doomsayer.challenge1");
                                    }
                                    guessCorrectAction();
                                    if (cankillPlayer)
                                    {
                                        GamePlayer.LocalPlayer.MurderPlayer(p!, PlayerState.Guessed, EventDetail.Guess, KillParameter.WithOverlay | KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, null);
                                    }
                                    if (Doomsayer.GuessShowKillAnim && showanim)
                                    {
                                        KillShowAnimRpc.Invoke((GamePlayer.LocalPlayer, p));
                                    }
                                    DoomsayerGuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int, bool>(GamePlayer.LocalPlayer, p, roleid, !showanim));
                                    return;
                                }
                            }
                            else
                            {
                                if (lastguesswrong != null && lastguesswrong == p)
                                {
                                    new StaticAchievementToken("doomsayer.another1");
                                }
                                DoomsayerGuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int, bool>(GamePlayer.LocalPlayer, p, roleid, false));
                                AmongUsUtil.PlayCustomFlash(UnityEngine.Color.red, 0.4f, 1f, 0.75f);
                                lastguesswrong = p;
                                DGuessWrong = true;
                                return;
                            }
                            PDebug.Log("DTryGuessed");
                        }
                    }
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
                LastGuesserWindow = DoomsayerOpenGuessWindow((r) =>
                {
                    try
                    {
                        if (PlayerControl.LocalPlayer.Data.IsDead) return;
                        if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;
                        if (!MeetingHudExtension.CanUseAbilityFor(p, true))
                        {
                            return;
                        }
                        PDebug.Log("CheckDGuessCorrect");
                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        {
                            PDebug.Log("DaddTryGuessed");
                            if (!playerGuess.TryGetValue(p.PlayerId, out int roleid))
                            {
                                playerGuess.Add(p.PlayerId, r.Id);
                            }
                        }
                        else if (CheckGuess(p,r))
                        {
                            if (!p.IsDead)
                            {
                                doomsayerGuessKillPlayers.Add(p);
                                if (doomsayerGuessKillPlayers.Count((GamePlayer player) => player.IsImpostor) >= 3)
                                {
                                    new StaticAchievementToken("doomsayer.challenge1");
                                }
                                guessCorrectAction();
                                if (cankillPlayer)
                                {
                                          GamePlayer.LocalPlayer.MurderPlayer(p!, PlayerState.Guessed, EventDetail.Guess, KillParameter.WithOverlay|KillParameter.WithAssigningGhostRole, KillCondition.BothAlive, null);
                                }
                                if (Doomsayer.GuessShowKillAnim && showanim)
                                {
                                    KillShowAnimRpc.Invoke((GamePlayer.LocalPlayer, p));
                                }
                                DoomsayerGuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int,bool>(GamePlayer.LocalPlayer, p, r.Id,!showanim));
                            }
                        }
                        else
                        {
                            if (lastguesswrong!=null&&lastguesswrong==p)
                            {
                                new StaticAchievementToken("doomsayer.another1");
                            }
                            DoomsayerGuessMessageRPC.Invoke(new ValueTuple<GamePlayer, GamePlayer, int,bool>(GamePlayer.LocalPlayer, p, r.Id,false));
                            AmongUsUtil.PlayCustomFlash(UnityEngine.Color.red, 0.4f, 1f, 0.75f);
                            DGuessWrong = true;
                            lastguesswrong = p;
                        }
                        if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
                        LastGuesserWindow = null!;

                    }
                    catch (Exception e)
                    {
                        PDebug.Log(e);
                    }
                }
                );
            },
            p => !doomsayerGuessKillPlayers.Contains(p.MyPlayer) &&!p.MyPlayer.IsDead && !p.MyPlayer.AmOwner && !DGuessWrong && !PlayerControl.LocalPlayer.Data.IsDead && GameOperatorManager.Instance!.Run(new PlayerCanGuessPlayerLocalEvent(NebulaAPI.CurrentGame!.LocalPlayer, p.MyPlayer, true)).CanGuess
            ));
    }

    static public void OnDead()
    {
        if (LastGuesserWindow) LastGuesserWindow.CloseScreen();
        LastGuesserWindow = null!;
    }
}
internal static class RoleOptionHelper
{
    private static readonly TextAttribute RelatedOutsideButtonAttr = new(NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.CenteredBoldFixed)) { Size = new(1.2f, 0.29f) };
    private static readonly TextAttribute RelatedInsideButtonAttr = new(NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.CenteredBoldFixed)) { Size = new(1.14f, 0.26f), Font = NebulaGUIWidgetEngine.API.GetFont(FontAsset.GothicMasked) };

    static internal void OpenFilterScreen<R>(string scrollerTag, IEnumerable<R> allRoles, Func<R, AssignableFilter<R>> filter, MetaScreen? screen = null) where R : DefinedAssignable
        => OpenFilterScreen(scrollerTag, allRoles, r => filter.Invoke(r).Test(r), (r, val) => filter.Invoke(r).SetAndShare(r, val), r => filter.Invoke(r).ToggleAndShare(r), screen);
    static internal void OpenFilterScreen<R>(string scrollerTag, IEnumerable<R> allRoles, Func<R, bool> test, Action<R, bool>? setAndShare, Action<R> toggleAndShare, MetaScreen? screen = null) where R : DefinedAssignable
    {
        if (!screen) screen = MetaScreen.GenerateWindow(new UnityEngine.Vector2(6.7f, setAndShare != null ? 4.5f : 3.7f), HudManager.Instance.transform, UnityEngine.Vector3.zero, true, true);

        bool showOnlySpawnable = ClientOption.AllOptions[ClientOption.ClientOptionType.ShowOnlySpawnableAssignableOnFilter].Value == 1;

        IEnumerable<R> allRolesFiltered = showOnlySpawnable ? allRoles.Where(r => (r as ISpawnable)?.IsSpawnable ?? true) : allRoles;

        List<GUIWidget> shortcutButtons = [];
        if (setAndShare != null)
        {
            void Append(string translationKey, Func<bool> isInvalid, Action<bool> onClicked)
            {
                var invalid = isInvalid.Invoke();
                shortcutButtons.Add(new GUIButton(Virial.Media.GUIAlignment.Center, RelatedOutsideButtonAttr, NebulaGUIWidgetEngine.API.LocalizedTextComponent(translationKey))
                {
                    Color = invalid ? UnityEngine.Color.gray : UnityEngine.Color.white,
                    OnClick = _ =>
                    {
                        {
                            //データのセーブと共有を一括で行う
                            using var segment = new DataSaveSegment();
                            onClicked.Invoke(invalid);
                        }
                        OpenFilterScreen(scrollerTag, allRoles, test, setAndShare, toggleAndShare, screen);
                    },
                    AsMaskedButton = false
                });
            }

            Append("roleFilter.shortcut.all", () => allRolesFiltered.Any(r => !test.Invoke(r)), val => allRolesFiltered.Do(r => setAndShare.Invoke(r, val)));

            if (typeof(R).IsAssignableTo(typeof(DefinedRole)))
            {
                if (allRolesFiltered.Any(r => (r as DefinedRole)!.Category == RoleCategory.ImpostorRole))
                {
                    var impostors = allRolesFiltered.Where(r => (r as DefinedRole)!.Category == RoleCategory.ImpostorRole);
                    Append("roleFilter.shortcut.allImpostor", () => impostors.Any(r => !test.Invoke(r)), val => impostors.Do(r => setAndShare.Invoke(r, val)));
                }
                if (allRolesFiltered.Any(r => (r as DefinedRole)!.Category == RoleCategory.NeutralRole))
                {
                    var neutrals = allRolesFiltered.Where(r => (r as DefinedRole)!.Category == RoleCategory.NeutralRole);
                    Append("roleFilter.shortcut.allNeutral", () => neutrals.Any(r => !test.Invoke(r)), val => neutrals.Do(r => setAndShare.Invoke(r, val)));
                }
                if (allRolesFiltered.Any(r => (r as DefinedRole)!.Category == RoleCategory.CrewmateRole))
                {
                    var crewmates = allRolesFiltered.Where(r => (r as DefinedRole)!.Category == RoleCategory.CrewmateRole);
                    Append("roleFilter.shortcut.allCrewmate", () => crewmates.Any(r => !test.Invoke(r)), val => crewmates.Do(r => setAndShare.Invoke(r, val)));
                }
            }
        }

        screen!.SetWidget(new VerticalWidgetsHolder(Virial.Media.GUIAlignment.Center,
            new HorizontalWidgetsHolder(Virial.Media.GUIAlignment.Center, shortcutButtons),
            new HorizontalWidgetsHolder(Virial.Media.GUIAlignment.Center, new NoSGUICheckbox(Virial.Media.GUIAlignment.Center, showOnlySpawnable)
            {
                OnValueChanged = val =>
                {
                    ClientOption.AllOptions[ClientOption.ClientOptionType.ShowOnlySpawnableAssignableOnFilter].Increment();
                    OpenFilterScreen(scrollerTag, allRoles, test, setAndShare, toggleAndShare, screen);
                }
            }, NebulaGUIWidgetEngine.API.HorizontalMargin(0.2f), NebulaGUIWidgetEngine.API.LocalizedText(Virial.Media.GUIAlignment.Center, NebulaGUIWidgetEngine.API.GetAttribute(AttributeAsset.OverlayContent), "roleFilter.showOnlySpawnable")),
            new GUIScrollView(Virial.Media.GUIAlignment.Center, new(6.5f, 3.1f), NebulaGUIWidgetEngine.API.Arrange(Virial.Media.GUIAlignment.Center,
            allRolesFiltered.Select(r => new GUIButton(Virial.Media.GUIAlignment.Center, RelatedInsideButtonAttr, NebulaGUIWidgetEngine.API.RawTextComponent(r.DisplayColoredName))
            {
                OnClick = _ => { toggleAndShare(r); OpenFilterScreen(scrollerTag, allRoles, test, setAndShare, toggleAndShare, screen); },
                Color = test(r) ? UnityEngine.Color.white : new UnityEngine.Color(0.14f, 0.14f, 0.14f),
                AsMaskedButton = true,
            })
            , 4))
            { ScrollerTag = scrollerTag, WithMask = true }), out _);
    }
}