using Color = Virial.Color;

namespace Plana.Core;

public static class PCitations
{
    public static Citation TownOfUs { get; private set; } = new("TownOfUs", NebulaAPI.AddonAsset.GetResource("TownOfUs.png").AsImage(125f), new ColorTextComponent(Color.White.ToUnityColor(), new RawTextComponent("TownOfUs")),null);
    public static Citation TOHE { get; private set; } = new("Town of Host-Enhanced",null, new ColorTextComponent(new UnityEngine.Color32(242,219,227,255), new RawTextComponent("Town of Host-Enhanced")), null);
    public static Citation SuperInterestingRoles { get; private set; } = new("SuperInterestingRoles", NebulaAPI.AddonAsset.GetResource("SuperInterestingRoles.png").AsImage(125f), new ColorTextComponent(new(255f, 255f, 255f), new RawTextComponent("Super Interesting Roles")), "https://github.com/zhuo-yue-shi");
    public static Citation PlanaANDKC { get; private set; } = new("PlanaAndKc", NebulaAPI.AddonAsset.GetResource("PNC.png").AsImage(125f), new ColorTextComponent(Color.White.ToUnityColor(), new RawTextComponent("PlanaAndKC")), null);
    public static Citation PlanaANDKCANDYunDuan { get; private set; } = new("PlanaAndKcAndYunDuan", NebulaAPI.AddonAsset.GetResource("PNCNYD.png").AsImage(125f), new ColorTextComponent(Color.White.ToUnityColor(), new RawTextComponent("PlanaAndKCAndYunDuan")), null);
    public static Citation ExtremeRoles { get; private set; } = new("ExtremeRoles", NebulaAPI.AddonAsset.GetResource("ExtremeRoles.png").AsImage(125f), new ColorTextComponent(Color.White.ToUnityColor(), new RawTextComponent("Extreme Roles")), "https://github.com/yukieiji/ExtremeRoles");
    public static Citation LTS { get; private set; } = new("Nebula-R-LTS", null, new ColorTextComponent(new UnityEngine.Color32(25,20,198,255), new RawTextComponent("Nebula-R-LTS")), null);
}
