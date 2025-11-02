using Nebula.Patches;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using BepInEx.Unity.IL2CPP.Utils;
using Plana.Core;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using IEnumerator = System.Collections.IEnumerator;

namespace Plana.Roles.Crewmate;

public class Swapper : DefinedSingleAbilityRoleTemplate<Swapper.Ability>, DefinedRole, HasCitation {
    private Swapper() : base("swapper", new Virial.Color(134, 55, 86), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [NumOfSwapOption,OnlyCanSwapOtherOption]) {}
    
    Citation? HasCitation.Citation => Citations.TheOtherRoles;
    
    public override Swapper.Ability CreateAbility(GamePlayer player, int[] arguments) => new Swapper.Ability(player, arguments.GetAsBool(0), arguments.Get(1, NumOfSwapOption));
    
    public static IntegerConfiguration NumOfSwapOption = NebulaAPI.Configurations.Configuration("options.role.swapper.op1", (1, 15), 3);
    public static BoolConfiguration OnlyCanSwapOtherOption = NebulaAPI.Configurations.Configuration("options.role.swapper.op2", false);

    static public Swapper MyRole = new Swapper();
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToCrewmate;
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility, IBindPlayer, IGameOperator, ILifespan {
        public Ability(GamePlayer player, bool isUsurped, int shots) : base(player, isUsurped) 
        {
            leftSwap = shots;
            if (base.AmOwner)
            {
                string prefix = Language.Translate("role.swapper.leftswap");
                Helpers.TextHudContent("SwapperLeftText", this, delegate (TextMeshPro tmPro)
                {
                    tmPro.text = prefix + ": " + leftSwap.ToString();
                }, true);
            }
        }

        int leftSwap = 0;
        int[]? IPlayerAbility.AbilityArguments => [IsUsurped.AsInt(), leftSwap];
        [Local]
        void OnExiled(PlayerExiledEvent ev)
        {
            if (SwapSystem.playerId1 != SwapSystem.NonSelected && SwapSystem.playerId2!=SwapSystem.NonSelected)
            {
                var player1 = SwapSystem.playerId1;
                var player2 = SwapSystem.playerId2;
                GamePlayer p1 = GamePlayer.GetPlayer(player1), p2 = GamePlayer.GetPlayer(player2);
                if (ev.Player.PlayerId == player1 && p1.IsImpostor && p2.IsCrewmate)
                {
                    new StaticAchievementToken("swapper.common1");
                }
                else if (ev.Player.PlayerId == player2 && p2.IsImpostor && p1.IsCrewmate)
                {
                    new StaticAchievementToken("swapper.common1");
                }
                else if (ev.Player.PlayerId == MyPlayer.PlayerId)
                {
                    new StaticAchievementToken("swapper.another1");
                }
                else if (ev.Player.PlayerId == player1 && p1.Role is Jester.Instance && p2.IsImpostor)
                {
                    new StaticAchievementToken("swapper.another2");
                }
                else if (ev.Player.PlayerId == player2 && p2.Role is Jester.Instance && p1.IsImpostor)
                {
                    new StaticAchievementToken("swapper.another2");
                }
            }
        }
        [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            SwapSystem.OnMeetingStart(leftSwap,IsUsurped, () => { leftSwap--; });
        }
    }
}

[NebulaRPCHolder]
public static class SwapSystem {
    public const byte NonSelected = 114;
    public static byte playerId1 = NonSelected, playerId2 = NonSelected;
    public static bool willSwap => playerId1 != NonSelected && playerId2 != NonSelected;

    public static Virial.Media.Image SwapImage = NebulaAPI.AddonAsset.GetResource("swapper.png").AsImage(115f);

    public static void OnMeetingStart(int leftSwap,bool isusurped, Action afterSelection) {
        RpcSyncSwapTarget.Invoke((NonSelected, NonSelected));
        if (leftSwap <= 0) return;
        playerId1 = playerId2 = NonSelected;
        NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>()?.RegisterMeetingAction(new(SwapImage,
            state =>
            {
                if (isusurped)
                {
                    if (isusurped) NebulaAsset.PlaySE(NebulaAudioClip.ButtonBreaking, volume: 1f);
                }
                var p = state.MyPlayer;
                if (p.PlayerId == playerId1)
                {
                    state.SetSelect(false);
                    playerId1 = NonSelected;
                }
                else if (playerId1 == NonSelected)
                {
                    state.SetSelect(true);
                    playerId1 = p.PlayerId;
                }
                else
                {
                    playerId2 = p.PlayerId;
                    afterSelection.Invoke();
                    RpcSyncSwapTarget.Invoke((playerId1, playerId2));
                }
            },
            p => !p.MyPlayer.IsDead && leftSwap > 0 && !PlayerControl.LocalPlayer.Data.IsDead && !willSwap && (!Swapper.OnlyCanSwapOtherOption || !p.MyPlayer.AmOwner)
        ));
    }

    public static byte VoteIdFix(byte target) {
        if (!willSwap) return target;
        if (target == playerId1) target = playerId2;
        else if (target == playerId2) target = playerId1;
        return target;
    }

    public static bool ModCalculateVotesPrefix([HarmonyArgument(0)]MeetingHud __instance, ref Dictionary<byte, int> __result) {
        Dictionary<byte, int> dictionary = new();
        __instance = MeetingHud.Instance;

        for (int i = 0; i < __instance.playerStates.Length; i++) {
            PlayerVoteArea playerVoteArea = __instance.playerStates[i];
            var player = NebulaGameManager.Instance?.GetPlayer(playerVoteArea.TargetPlayerId);
            if (player?.IsDead ?? true) continue;

            bool didVote = playerVoteArea.VotedFor != 252 && playerVoteArea.VotedFor != 255 && playerVoteArea.VotedFor != 254;
            if (!MeetingHudExtension.WeightMap.TryGetValue((byte)playerVoteArea.TargetPlayerId, out var vote)) vote = 1;
            var ev = GameOperatorManager.Instance!.Run(new PlayerFixVoteHostEvent(player, didVote, NebulaGameManager.Instance?.GetPlayer(playerVoteArea.VotedFor), vote));

            if (ev.DidVote) {
                dictionary.AddValueV2(VoteIdFix(ev.VoteTo?.PlayerId ?? PlayerVoteArea.SkippedVote), ev.Vote);
                playerVoteArea.VotedFor = VoteIdFix(ev.VoteTo?.PlayerId ?? PlayerVoteArea.SkippedVote);
                MeetingHudExtension.WeightMap[player.PlayerId] = ev.Vote;
            }
            else {
                playerVoteArea.VotedFor = PlayerVoteArea.MissedVote;
            }
        }

        __result = dictionary;

        return false;
    }

    [HarmonyPriority(Priority.High)]
    public static void ShowSwapAnim(MeetingHud __instance)
    {
        if (!willSwap) return;
        PlayerVoteArea playerVoteArea = null, playerVoteArea2 = null;
        foreach (PlayerVoteArea temp in MeetingHud.Instance.playerStates)
        {
            if (temp.TargetPlayerId == playerId1) playerVoteArea = temp;
            else if (temp.TargetPlayerId == playerId2) playerVoteArea2 = temp;
        }

        if (playerVoteArea == null || playerVoteArea2 == null) return;

        Vector3 startPos1 = playerVoteArea.transform.localPosition;
        Vector3 startPos2 = playerVoteArea2.transform.localPosition;
        MeetingHud.Instance.StartCoroutine(CircularSwapAnimation(playerVoteArea.transform, startPos1, startPos2, 1.5f, true));
        MeetingHud.Instance.StartCoroutine(CircularSwapAnimation(playerVoteArea2.transform, startPos2, startPos1, 1.5f, false));
    }

    private static IEnumerator CircularSwapAnimation(Transform movingTransform, Vector3 startPos, Vector3 endPos, float duration, bool useUpperHalf)
{
    float elapsed = 0f;
    
    // 计算控制点来形成圆形轨迹
    Vector3 center = (startPos + endPos) * 0.5f;
    float distance = Vector3.Distance(startPos, endPos);
    
    // 根据上下半圆设置不同的控制点高度
    float controlHeight = useUpperHalf ? distance * 0.5f : -distance * 0.5f;
    Vector3 controlPoint = center + new Vector3(0, controlHeight, 0);
    
    while (elapsed < duration) {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        
        // 使用正弦缓动函数使动画更平滑
        float easedT = EaseInOutSine(t);
        
        // 使用二次贝塞尔曲线计算位置
        Vector3 position = CalculateQuadraticBezierPoint(easedT, startPos, controlPoint, endPos);
        
        // 应用新位置
        movingTransform.localPosition = position;
        
        yield return null;
    }
    
    // 确保最终位置准确
    movingTransform.localPosition = endPos;
}

// 计算二次贝塞尔曲线上的点
private static Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
{
    float u = 1 - t;
    float tt = t * t;
    float uu = u * u;
    
    Vector3 point = uu * p0; // (1-t)^2 * P0
    point += 2 * u * t * p1; // 2(1-t)t * P1
    point += tt * p2; // t^2 * P2
    
    return point;
}

// 正弦缓动函数
private static float EaseInOutSine(float x) {
    return -(Mathf.Cos(Mathf.PI * x) - 1) / 2;
}
    public static readonly RemoteProcess<(byte, byte)> RpcSyncSwapTarget = new("RpcSyncSwapTarget", 
        (message, _) => {
            playerId1 = message.Item1;
            playerId2 = message.Item2;
    });
}

/*


.Patch(typeof(CheckForEndVotingPatch).GetMethod("ModCalculateVotes"), new HarmonyMethod(typeof(Plana.Roles.Crewmate.SwapSystem).GetMethod("ModCalculateVotesPrefix")));
harmony.Patch(typeof(MeetingHud).GetMethod("PopulateResults"), new HarmonyMethod(typeof(Plana.Roles.Crewmate.SwapSystem).GetMethod("ShowSwapAnim")));
*/