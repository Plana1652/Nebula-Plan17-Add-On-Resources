using Il2CppInterop.Runtime.Injection;
using Nebula.Patches;
using UnityEngine.Rendering;
using UnityEngine.UI;
using IEnumerator = System.Collections.IEnumerator;
using Color = UnityEngine.Color;
using Vector3 = UnityEngine.Vector3;
using Vector2 = UnityEngine.Vector2;
using Nebula.Modules.Cosmetics;
using Plana.Core;
namespace Plana.Roles.Crewmate;

public interface IMultibandMater
{
    float[] Band { get; }
    int Length { get; }

    void Update();
}
public class RandomMultibandMeter : IMultibandMater{
    private float[] secretBand;
    public float[] Band { get; private set; }
    public int Length { get; private init; }
    public RandomMultibandMeter(int num) {
        Length = num;
        Band = new float[num];
        secretBand = new float[num];
        for (int i = 0; i < num; i++) Band[i] = 0f;
    }

    public void Update() {

        void SpawnWave()
        {
            float center = System.Random.Shared.NextSingle();
            float height = 0.3f * System.Random.Shared.NextSingle() * 2f;
            height *= height; //大きい山の生まれる頻度を下げる
            float width = 0.2f * System.Random.Shared.NextSingle() * 0.45f;

            for(int i = 0;i < Length; i++)
            {
                float pos = (float)i / Length;
                float x = (pos - center) / width;
                float y = height / (float)Math.Sqrt((x * x) + Math.Cos(x));
                secretBand[i] = Mathf.Max(y, secretBand[i]);
            }
        }

        //毎ティック新たな波を生成する
        for (int i = 0; i < 3; i++) SpawnWave();

        for(int i = 0;i < Length; i++)
        {
            //見た目上の値を上限値にシームレスに寄せる
            if (Band[i] < secretBand[i])
                Band[i] += (secretBand[i] - Band[i]).Delta(8f, 0.1f);
            else
                Band[i] = secretBand[i];

            //上限値を0に近づける
            secretBand[i] -= Time.deltaTime * 0.5f;
            if (secretBand[i] < 0f) secretBand[i] = 0f;
        }  
    }
}

public class JusticePlusMeetingHud : MonoBehaviour
{
   static JusticePlusMeetingHud() => ClassInjector.RegisterTypeInIl2Cpp<JusticePlusMeetingHud>();

    static private readonly SpriteLoader meetingBackMaskSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.MeetingUIMask.png", 100f);
    static private readonly SpriteLoader meetingBackSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.MeetingBack.png", 100f);
    static private readonly SpriteLoader meetingBackAlphaSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.MeetingBackAlpha.png", 100f);
    static private readonly SpriteLoader meetingReticleSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeMeetingReticle.png", 100f);
    static private readonly Virial.Media.Image meetingUpViewSprite = NebulaAPI.AddonAsset.GetResource("JusticeMeetingUP.png").AsImage();
    static private readonly SpriteLoader meetingViewSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeMeetingView.png", 100f);
    static private readonly SpriteLoader votingHolderLeftSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeHolderLeft.png", 120f);
    static private readonly SpriteLoader votingHolderRightSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeHolderRight.png", 120f);
    static private readonly SpriteLoader votingHolderMaskSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeHolderMask.png", 120f);
    static private readonly SpriteLoader votingHolderFlashSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeHolderFlash.png", 120f);
    static private readonly SpriteLoader votingHolderBlurSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeHolderFlashBlur.png", 120f);

    static private readonly SpriteLoader circleGraphSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeCircleGraph.png", 120f);
    static private readonly SpriteLoader circleGraphBackSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeCircleBackGraph.png", 120f);
    static private readonly SpriteLoader bandGraphSprite = NebulaResourceTextureLoader.FromResource("Nebula.Resources.JusticeBandGraph.png", 120f);

    SpriteRenderer Background1, Background2, BackView,UpView;
    
    public GameObject InstantiateCircleGraph(Transform parent, Vector3 localPos, Color color)
    {
        var circleBack = UnityHelper.CreateSpriteRenderer("CircleGraph", parent, localPos);
        circleBack.gameObject.AddComponent<SortingGroup>();
        circleBack.sprite = circleGraphBackSprite.GetSprite();
        circleBack.color = color;
        circleBack.sortingGroupOrder = 35;
        var circleFront = UnityHelper.CreateSpriteRenderer("Front", circleBack.transform, new(0f, 0f, -0.05f));
        circleFront.sprite = circleGraphSprite.GetSprite();
        var material = new Material(NebulaAsset.GuageShader);
        material.SetFloat("_Guage", 0.5f);
        material.SetColor("_Color", Color.Lerp(color, Color.white, 0.5f));
        circleFront.material = material;
        circleFront.transform.localScale = new(-1f, 1f, 1f);
        circleFront.sortingGroupOrder = 36;


        IEnumerator CoAnimCircle()
        {
            float goal = System.Random.Shared.NextSingle();
            material.SetFloat("_Guage", goal);

            float current = 0f;
            float t = 1f;
            float slightT = 0.2f;
            while (circleFront)
            {
                current -= (current - goal).Delta(0.8f, 0.001f);
                t -= Time.deltaTime;
                slightT -= Time.deltaTime;
                if(t < 0f)
                {
                    goal = Math.Clamp(goal + (System.Random.Shared.NextSingle() - 0.5f) * 0.35f, 0f, 1f);
                    t = 1.4f + System.Random.Shared.NextSingle() * 6f;
                }
                if(slightT < 0f)
                {
                    goal = Math.Clamp(goal + (System.Random.Shared.NextSingle() - 0.5f) * 0.15f, 0f, 1f);
                    slightT = 0.2f;
                }

                material.SetFloat("_Guage", current);

                yield return null;
            }
        }
        StartCoroutine(CoAnimCircle().WrapToIl2Cpp());

        return circleBack.gameObject;
    }
    void SetActive(ValueTuple<GameObject, GameObject> objects, bool active)
    {
        objects.Item1.SetActive(active);
        objects.Item2.SetActive(active);
    }
    public void InstantiateBandGraph(int num, Transform parent, Vector3 center, Color color)
    {
        var bandHolder = UnityHelper.CreateObject<SortingGroup>("BandHolder", parent, center);

        SpriteRenderer[] renderers = new SpriteRenderer[num];
        float[] filter = new float[num];
        for (int i = 0; i < num; i++)
        {
            renderers[i] = UnityHelper.CreateSpriteRenderer("Graph", bandHolder.transform, new Vector3((i / (float)(num - 1) - 0.5f) * 1.8f, 0f, 0f));
            renderers[i].transform.localScale = new(1f, 0f, 1f);
            renderers[i].color = color;
            renderers[i].sprite = bandGraphSprite.GetSprite();
            filter[i] = 0.5f + Math.Clamp(0.2f * Math.Min(i, num - 1 - i), 0f, 0.5f);
        }

        IMultibandMater multibandMater = new RandomMultibandMeter(num);

        IEnumerator CoUpdate()
        {
            while (true)
            {
                multibandMater.Update();
                for (int i = 0; i < num; i++)
                    renderers[i].transform.localScale = new Vector3(1f, multibandMater.Band[i] * filter[i] * 4.4f, 1f);
                if (!isTransitioning)
                {
                    if (pagemax >1)
                    {
                        float scroll = Input.GetAxis("Mouse ScrollWheel");
                        if (scroll < 0)
                        {
                            PDebug.Log("Page " + page + " To " + (page + 2));
                            yield return StartCoroutine(PageTransition(true).WrapToIl2Cpp());
                            isTransitioning = false;
                        }
                        else if (scroll > 0)
                        {
                            PDebug.Log("Page " + page + " To " + (page - 2));
                            yield return StartCoroutine(PageTransition(false).WrapToIl2Cpp());
                            isTransitioning = false;
                        }
                    }
                }

                yield return null;
            }
        }

        StartCoroutine(CoUpdate().WrapToIl2Cpp());
    }
    // 位置参数
    Vector3 leftPos = new Vector3(-2f, 0f, 0f);
    Vector3 rightPos = new Vector3(2f, 0f, 0f);
    Vector3 CenterPos = new Vector3(0f, 0f, 0f);
    Vector3 leftOutPos = new Vector3(-10f, 0f, 0f);
    Vector3 rightOutPos = new Vector3(10f, 0f, 0f);

    IEnumerator SlidePage((GameObject, GameObject) page, Vector3 startPos, Vector3 endPos, float duration)
    {
        Coroutine slide1 = StartCoroutine(Effects.Slide3D(page.Item1.transform, startPos, endPos, duration));
        Coroutine slide2 = StartCoroutine(Effects.Slide3D(page.Item2.transform, startPos+ new Vector3(0.11f, -1.08f, -0.9f), endPos+new Vector3(0.11f,-1.08f,-0.9f), duration));
        yield return slide1;
        yield return slide2;
    }

    IEnumerator PageTransition(bool nextPage)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        int leftIdx = page;
        int rightIdx = leftIdx + 1;
        int targetPage = nextPage ?
            Mathf.Min(pagemax, leftIdx + 2) :
            Mathf.Max(0, leftIdx - 2);

        if (targetPage == leftIdx)
        {
            isTransitioning = false;
            yield break;
        }

        if (!pageDic.ContainsKey(rightIdx))
        {
            yield return StartCoroutine(nextPage ?
            SlidePage(pageDic[leftIdx], CenterPos, leftOutPos, 0.25f).WrapToIl2Cpp() :
            SlidePage(pageDic[leftIdx], CenterPos, rightOutPos, 0.25f).WrapToIl2Cpp());
            SetActive(pageDic[leftIdx], false);
        }
        else
        {
            var currentLeftSlide = nextPage ?
                SlidePage(pageDic[leftIdx], leftPos, leftOutPos, 0.25f).WrapToIl2Cpp() :
                SlidePage(pageDic[leftIdx], leftPos, rightOutPos, 0.25f).WrapToIl2Cpp();

            var currentRightSlide = nextPage ?
                SlidePage(pageDic[rightIdx], rightPos, leftOutPos, 0.25f).WrapToIl2Cpp() :
                SlidePage(pageDic[rightIdx], rightPos, rightOutPos, 0.25f).WrapToIl2Cpp();
            yield return StartCoroutine(currentLeftSlide);
            yield return StartCoroutine(currentRightSlide);
            SetActive(pageDic[leftIdx], false);
            SetActive(pageDic[rightIdx], false);
        }
        page = targetPage;
        int newLeftIdx = page;
        int newRightIdx = newLeftIdx + 1;
        Vector3 enterPos;
        if (!pageDic.ContainsKey(newRightIdx))
        {
            enterPos = nextPage ? rightOutPos : leftOutPos;
            pageDic[newLeftIdx].Item1.transform.position = enterPos;
            pageDic[newLeftIdx].Item2.transform.position = enterPos;
            SetActive(pageDic[newLeftIdx], true);
            yield return StartCoroutine(nextPage ?
    SlidePage(pageDic[newLeftIdx], rightOutPos, CenterPos, 0.25f).WrapToIl2Cpp() :
    SlidePage(pageDic[newLeftIdx], leftOutPos, CenterPos, 0.25f).WrapToIl2Cpp());
        }
        else
        {
            enterPos = nextPage ? rightOutPos : leftOutPos;
            pageDic[newLeftIdx].Item1.transform.position = enterPos;
            pageDic[newLeftIdx].Item2.transform.position = enterPos;
            pageDic[newRightIdx].Item1.transform.position = enterPos;
            pageDic[newRightIdx].Item2.transform.position = enterPos;
            SetActive(pageDic[newLeftIdx], true);
            SetActive(pageDic[newRightIdx], true);
            var newLeftSlide = nextPage ?
                SlidePage(pageDic[newLeftIdx], rightOutPos, leftPos, 0.25f).WrapToIl2Cpp() :
                SlidePage(pageDic[newLeftIdx], leftOutPos, leftPos, 0.25f).WrapToIl2Cpp();
            var newRightSlide = nextPage ?
                SlidePage(pageDic[newRightIdx], rightOutPos, rightPos, 0.25f).WrapToIl2Cpp() :
                SlidePage(pageDic[newRightIdx], leftOutPos, rightPos, 0.25f).WrapToIl2Cpp();
            yield return StartCoroutine(newLeftSlide);
            yield return StartCoroutine(newRightSlide);
        }
        isTransitioning = false;
    }
    bool isTransitioning;

    private IEnumerator CoAnimColor(SpriteRenderer renderer, Color color1, Color color2, float duration)
    {
        float t = 0f;
        while(t <  duration)
        {
            t += Time.deltaTime;
            renderer.color = Color.Lerp(color1, color2, t / duration);
            yield return null;
        }
        renderer.color = color2;
    }

    private IEnumerator CoAnimColorRepeat(SpriteRenderer renderer, Color color1, Color color2, float duration)
    {
        while (true)
        {
            yield return CoAnimColor(renderer, color1, color2, duration);
            yield return CoAnimColor(renderer, color2, color1, duration);
        }
    }
    int page = 0, pagemin = 0, pagemax;
    public void Begin(List<GamePlayer> players, Action onMeetingStart)
    {
        pageDic = new Dictionary<int, (GameObject, GameObject)>();
        List<int> indexs = new List<int>();
        pagemax = players.Count-2;
        if (pagemax%2==1)
        {
            pagemax++;
        }
        for (int i = 0; i < players.Count; i++)
        {
            if (i % 2 == 0)
            {
                indexs.Add(players[i].PlayerId % PhotoData.Length);
            }
            else
            {
                indexs.Add((indexs[i-1] + ((players[i].PlayerId + 1) % (PhotoData.Length - 2))) % PhotoData.Length);
            }
        }
        StartCoroutine(SetUpJusticeMeeting(MeetingHud.Instance, players, onMeetingStart, indexs).WrapToIl2Cpp());
    }

    private static string[] RandomTexts = ["(despired)", "terminus", "<revolt>", "solitary", "bona vacantia", "despotism", "pizza", "elitism", "suspicion", "justice", "outsider", "discrepancy", "purge", "uniformity", "conviction", "tribunal", "triumph", "heroism", "u - majority"];
    private static string[] RandomAltTexts = ["HERO", "victor", "supreme", "the one", "genius", "prodigy", "detective", "clairvoyant"];
    IEnumerator CoDisappearVotingArea(MeetingHud meetingHud, float duration)
    {
        var states = meetingHud.playerStates.OrderBy(i => Guid.NewGuid()).ToArray();
        var interval = duration / states.Length;
        foreach(var state in states)
        {
            state.gameObject.SetActive(false);
            yield return Effects.Wait(interval);
        }
    }

    IEnumerator CoAnimBackLine(GameObject parent)
    {
        var lineRenderer = UnityHelper.CreateObject<SpriteRenderer>("Line", parent.transform, new(0.01f, 0f, -18f));
        lineRenderer.sprite = VanillaAsset.FullScreenSprite;
        lineRenderer.color = Color.black.AlphaMultiplied(0.85f);
        lineRenderer.transform.localScale = new(20f, 0f, 1f);
        var lineRenderer2 = UnityHelper.CreateObject<SpriteRenderer>("Line2", parent.transform, new(0.01f, 1f, -18f));
        lineRenderer2.sprite = VanillaAsset.FullScreenSprite;
        lineRenderer2.color = Color.black.AlphaMultiplied(0.85f);
        lineRenderer2.transform.localScale = new(20f, 0f, 1f);
        var lineRenderer3 = UnityHelper.CreateObject<SpriteRenderer>("Line3", parent.transform, new(0.01f, -1f, -18f));
        lineRenderer3.sprite = VanillaAsset.FullScreenSprite;
        lineRenderer3.color = Color.black.AlphaMultiplied(0.85f);
        lineRenderer3.transform.localScale = new(20f, 0f, 1f);

        float t = 0f;
        float p = 0f;
        while(t < 2f && parent)
        {
            p += (1 - p).Delta(3.5f, 0.01f);
            lineRenderer.transform.localScale = new(8.79f, p * 0.76f, 1f);
            lineRenderer2.transform.localScale = new(8.79f, p * 0.76f, 1f);
            lineRenderer3.transform.localScale = new(8.79f, p * 0.76f, 1f);
            t += Time.deltaTime;
            yield return null;
        }

        yield return Effects.Wait(1.25f);
        t = 0f;
        while (t < 1f && parent)
        {
            p -= p.Delta(6.9f, 0.01f);
            lineRenderer.transform.localScale = new(8.79f, p * 0.76f, 1f);
            lineRenderer2.transform.localScale = new(8.79f, p * 0.76f, 1f);
            lineRenderer3.transform.localScale = new(8.79f, p * 0.76f, 1f);
            t += Time.deltaTime;
            yield return null;
        }

    }

    IEnumerator CoAnimIntroText(GameObject parent)
    {
        var introText = UnityHelper.CreateObject<TextMeshNoS>("IntroText", parent.transform, new(0f, 0f, -18.5f));
        introText.Font = NebulaAsset.JusticeFont;
        introText.FontSize = 0.48f;
        introText.TextAlignment = Virial.Text.TextAlignment.Center;
        introText.Pivot = new(0.5f, 0.5f);
        introText.Text = "";
        introText.Material = UnityHelper.GetMeshRendererMaterial();
        introText.Color = JusticePlus.MyRole.UnityColor;
        var introText2 = UnityHelper.CreateObject<TextMeshNoS>("IntroText2", parent.transform, new(0f, 1f, -18.5f));
        introText2.Font = NebulaAsset.JusticeFont;
        introText2.FontSize = 0.48f;
        introText2.TextAlignment = Virial.Text.TextAlignment.Center;
        introText2.Pivot = new(0.5f, 0.5f);
        introText2.Text = "";
        introText2.Material = UnityHelper.GetMeshRendererMaterial();
        introText2.Color = JusticePlus.MyRole.UnityColor;
        var introText3 = UnityHelper.CreateObject<TextMeshNoS>("IntroText3", parent.transform, new(0f, -1f, -18.5f));
        introText3.Font = NebulaAsset.JusticeFont;
        introText3.FontSize = 0.48f;
        introText3.TextAlignment = Virial.Text.TextAlignment.Center;
        introText3.Pivot = new(0.5f, 0.5f);
        introText3.Text = "";
        introText3.Material = UnityHelper.GetMeshRendererMaterial();
        introText3.Color = JusticePlus.MyRole.UnityColor;
        IEnumerator CoShowWarningText(TextMeshNoS tm)
        {
            string completedText = "!WARNING!";
            for (int i = 0; i < completedText.Length; i++)
            {
                tm.Text = completedText.Substring(0, i + 1);
                yield return Effects.Wait(0.06f);
            }
            yield break;
        }
        IEnumerator CoAnimTextColor(TextMeshNoS tm, Color color1, Color color2, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                tm.Color = Color.Lerp(color1, color2, t / duration);
                yield return null;
            }
            tm.Color = color2;
        }
        StartCoroutine(CoShowWarningText(introText2).WrapToIl2Cpp());
        StartCoroutine(CoShowWarningText(introText3).WrapToIl2Cpp());
        string completedText = System.Random.Shared.NextSingle() < 0.1f ? "The balance is at will!" : "Justice meeting begins...";
        for(int i = 0;i<completedText.Length;i++)
        {
            if (i==8)
            {
                StartCoroutine(CoAnimTextColor(introText, introText.Color, new Color32(212, 24, 24, 255), 0.5f).WrapToIl2Cpp());
                StartCoroutine(CoAnimTextColor(introText2, introText2.Color, new Color32(212, 24, 24, 255), 0.5f).WrapToIl2Cpp());
                StartCoroutine(CoAnimTextColor(introText3, introText3.Color, new Color32(212, 24, 24, 255), 0.5f).WrapToIl2Cpp());
            }
            introText.Text = completedText.Substring(0,i + 1);
            yield return Effects.Wait(0.078f);
        }
        for(int i = 0; i < 3; i++)
        {
            introText.gameObject.SetActive(false);
            yield return Effects.Wait(0.04f);
            introText.gameObject.SetActive(true);
            yield return Effects.Wait(0.04f);
        }
    }

    IEnumerator CoPlayAlertFlash()
    {
        yield return Effects.Wait(0.15f);
        for(int i = 0; i < 3; i++)
        {
            AmongUsUtil.PlayCustomFlash(Color.red, 0.2f, 0.2f, 0.3f, 0.6f);
            yield return Effects.Wait(1.0f + 0.3f);
        }
    }
    Dictionary<int, ValueTuple<GameObject, GameObject>> pageDic = new Dictionary<int, ValueTuple<GameObject, GameObject>>();
    static (int photoIndex, Vector2 localPos, float scale)[] PhotoData = [/*(0, new(-1.04f, 0.62f), 0.52f),*/ (1, new(-0.845f, 0.816f), 0.76f), (2, new(-1.04f, 0.77f), 0.7f), (3, new(-1.06f, 0.805f), 0.73f), (4, new(-1.06f, 0.89f), 0.9f), (5, new(-1.07f, 0.81f), 0.75f), (6, new(-0.93f, 0.73f), 0.65f), (7, new(-1.06f, 0.77f), 0.7f)];
    IEnumerator SetUpJusticeMeeting(MeetingHud meetingHud, List<GamePlayer> players, Action onMeetingStart,List<int> indexs)
    {
        meetingHud.TimerText.gameObject.SetActive(false);
        NebulaAsset.PlaySE(NebulaAudioClip.Justice1);
        StartCoroutine(CoDisappearVotingArea(meetingHud, 2.3f).WrapToIl2Cpp());
        StartCoroutine(CoPlayAlertFlash().WrapToIl2Cpp());

        yield return Effects.Wait(0.1f);
        StartCoroutine(CoDisappearVotingArea(meetingHud, 2.3f).WrapToIl2Cpp());

        var introObj = UnityHelper.CreateObject("IntroObj", transform,Vector3.zeroVector);
        StartCoroutine(CoAnimBackLine(introObj).WrapToIl2Cpp());
        StartCoroutine(CoAnimIntroText(introObj).WrapToIl2Cpp());

        var black = UnityHelper.CreateSpriteRenderer("Black", transform, new(0f, 0f, -19f));
        black.transform.localScale = new(1.2f, 1f, 1f);
        black.sprite = meetingBackMaskSprite.GetSprite();
        black.color = new(0f, 0f, 0f, 0f);

        yield return Effects.Wait(2.5f);
        NebulaManager.Instance.StartDelayAction(0.2f, () => NebulaAsset.PlaySE(NebulaAudioClip.Justice2));
        yield return CoAnimColor(black, new(0f, 0f, 0f, 0f), Color.black, 1.2f);
        StartCoroutine(ManagedEffects.Sequence(Effects.Wait(1f).WrapToManaged(), CoAnimColor(black, Color.black, new(0f, 0f, 0f, 0f), 1f), ManagedEffects.Action(()=>GameObject.Destroy(black.gameObject))).WrapToIl2Cpp());
        GameObject.Destroy(introObj);
        meetingHud.TimerText.gameObject.SetActive(true);
        PDebug.Log("JusticeMeetingStart");
            onMeetingStart.Invoke();

        //タイトルテキストが少し右にずれているので修正
        meetingHud.TitleText.transform.localPosition = new(-0.25f, 2.2f, -1f);
        meetingHud.TitleText.text = Language.Translate("game.meeting.justiceMeeting");

        //背景を作る
        var backObj = UnityHelper.CreateObject<SortingGroup>("JusticeBackground", transform, new(0f, 0f, 7f));

        //背景マスク
        var mask = UnityHelper.CreateObject<SpriteMask>("JusticeMask", backObj.transform, Vector3.zero);
        mask.transform.localScale = new(1.2f, 1f, 1f);
        mask.sprite = meetingBackMaskSprite.GetSprite();

        Background1 = UnityHelper.CreateSpriteRenderer("JusticeBack", backObj.transform, new(0f, 0f, 0f));
        Background2 = UnityHelper.CreateSpriteRenderer("JusticeBackAlpha", backObj.transform, new(0f, 0f, -0.1f));
        Background1.sprite = meetingBackSprite.GetSprite();
        Background2.sprite = meetingBackAlphaSprite.GetSprite();
        Background1.transform.localScale = new(4.5f, 2.6f, 1f);
        Background2.transform.localScale = new(4.5f, 2.6f, 1f);
        Background1.color = new(0.002f, 0.03f, 0.16f);
        Background2.color = Justice.MyRole.UnityColor.RGBMultiplied(0.21f);
        Background1.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        Background2.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        StartCoroutine(CoAnimColorRepeat(Background1, new(0.002f, 0.03f, 0.16f), new(0.002f, 0.1f, 0.1f), 5f).WrapToIl2Cpp());
        PDebug.Log("ShowAnimEnd");
        var backReticle = UnityHelper.CreateSpriteRenderer("JusticeBackReticle", backObj.transform, new(0f, 0f, -0.2f));
        backReticle.transform.localScale = new(0.69f, 0.69f, 1f);
        backReticle.sprite = meetingReticleSprite.GetSprite();
        backReticle.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        var reticleText = NebulaAsset.InstantiateText("ReticleText", backObj.transform, new(0f, -1.9f, -0.2f), NebulaAsset.JusticeFont, 0.42f, Virial.Text.TextAlignment.Center, new(0.5f,0.5f), "", new(0.7f, 0.7f, 0.7f, 0.4f));
        
        IEnumerator CoAnimText(string text)
        {
            reticleText.Text = "";
            reticleText.gameObject.SetActive(true);
            yield return null;
            string targetText = text;
            for(int i = 1;i<=targetText.Length; i++)
            {
                reticleText.Text = targetText.Substring(0, i);
                yield return Effects.Wait(0.08f);
            }
            for (int i = 0; i < 3; i++)
            {
                reticleText.gameObject.SetActive(false);
                yield return Effects.Wait(0.05f);
                reticleText.gameObject.SetActive(true);
                yield return Effects.Wait(0.05f);
            }

            yield return Effects.Wait(5f + System.Random.Shared.NextSingle() * 8f);

            for (int i = 0; i < 3; i++)
            {
                reticleText.gameObject.SetActive(false);
                yield return Effects.Wait(0.05f);
                reticleText.gameObject.SetActive(true);
                yield return Effects.Wait(0.05f);
            }
            reticleText.gameObject.SetActive(false);
            yield return Effects.Wait(0.6f + System.Random.Shared.NextSingle() * 0.6f);
        }
        IEnumerator CoRepeatAnimText()
        {
            string[] texts = [];
            int index = 0;
            while (true)
            {
                if (index == texts.Length) {
                    texts = RandomTexts.OrderBy(_ => Guid.NewGuid()).ToArray();
                    index = 0;
                }

                yield return CoAnimText(texts[index++]);
            }
        }
        StartCoroutine(CoRepeatAnimText().WrapToIl2Cpp());
        PDebug.Log("GenBackView");

        BackView = UnityHelper.CreateSpriteRenderer("JusticeBackView", backObj.transform, new(0f, 0f, -0.2f));
        BackView.transform.localScale = new(0.69f, 0.69f, 1f);
        BackView.sprite = meetingViewSprite.GetSprite();
        BackView.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        IEnumerator CoAnimView()
        {
            BackView.color = new(0.5f, 0.5f, 0.5f, 0.4f);

            while (true)
            {
                yield return Effects.Wait(5.5f + System.Random.Shared.NextSingle() * 3f);

                switch (System.Random.Shared.Next(3))
                {
                    case 0:
                        yield return CoAnimColor(BackView, new(1f, 0.3f, 0.3f, 0.4f), new(0.5f, 0.5f, 0.5f, 0.4f), 0.8f);
                        break;
                    case 1:
                        yield return CoAnimColor(BackView, new(0.9f, 0.3f, 0.3f, 0.4f), new(0.5f, 0.5f, 0.5f, 0.4f), 0.3f);
                        yield return Effects.Wait(0.2f);
                        yield return CoAnimColor(BackView, new(1f, 0.3f, 0.3f, 0.4f), new(0.5f, 0.5f, 0.5f, 0.4f), 1.4f);
                        break;
                    case 2:
                        yield return CoAnimColor(BackView, new(0.5f, 0.5f, 0.5f, 0.4f), new(1f, 1f, 1f, 0.6f), 0.2f);
                        yield return CoAnimColor(BackView, new(1f, 1f, 1f, 0.6f), new(0.5f, 0.5f, 0.5f, 0.4f), 0.8f);
                        break;
                }
            }
        }
        IEnumerator CoUpAnimView()
        {
            UpView.color = new(1f,1f,1f,1f);

            while (true)
            {
                yield return Effects.Wait(7f + System.Random.Shared.NextSingle() * 3f);
                yield return CoAnimColor(UpView, new(1f, 1f, 1f, 1f), new(1f, 1f, 1f, 0f), 2f);
                yield return Effects.Wait(0.2f);
                yield return CoAnimColor(UpView, new(1f, 1f, 1f, 0f), new(1f, 1f, 1f, 1f), 1f);
            }
        }
        PDebug.Log("CoAnimView");
        StartCoroutine(CoAnimView().WrapToIl2Cpp());
        PDebug.Log("AnimViewEnd");
        UpView = UnityHelper.CreateSpriteRenderer("JusticeUpView", backObj.transform, new(0f, 0.95f, -0.2f));
        UpView.transform.localScale = new(0.69f, 0.69f, 1f);
        UpView.sprite = meetingUpViewSprite.GetSprite();
        UpView.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        StartCoroutine(CoUpAnimView().WrapToIl2Cpp());
        meetingHud.playerStates.Do(p => p.gameObject.SetActive(false));

        var boardPassGame = VanillaAsset.MapAsset[2].CommonTasks.FirstOrDefault(p => p.MinigamePrefab.name == "BoardingPassGame")?.MinigamePrefab.TryCast<BoardPassGame>();

        int lastNumberIndex = 0;
        int lastAltIndex = 0;
        void SpawnVotingArea(GamePlayer player, Vector3 localPos, Virial.Media.Image holder, int photoIndex,int page)
        {
                var flashHolder = UnityHelper.CreateObject("FlashHolder", transform, Vector3.zero);
                var flash = UnityHelper.CreateSpriteRenderer("Flash", flashHolder.transform, localPos + new Vector3(0f, 0f, -19.5f));
                flash.sprite = votingHolderFlashSprite.GetSprite();
                var flashBlur = UnityHelper.CreateSpriteRenderer("FlashBlur", flashHolder.transform, localPos + new Vector3(0f, 0f, -19.5f));
                flashBlur.sprite = votingHolderBlurSprite.GetSprite();
            IEnumerator CoAnimFlash()
            {
                yield return ManagedEffects.Lerp(1.4f, p => {
                    flash.color = new(1f, 1f, 1f, p);
                    flashBlur.color = new(1f, 1f, 1f, p * 0.5f);
                    flashBlur.transform.localScale = new(0.6f + p * 0.1f, 0.6f + p * 0.1f, 1f);
                });
                yield return Effects.Wait(0.1f);
                yield return ManagedEffects.Lerp(0.5f, p => {
                    flash.color = new(1f, 1f, 1f, 1 - p);
                    flashBlur.color = new(1f, 1f, 1f, 0.5f - p * 0.5f);
                    flashBlur.transform.localScale = new(0.7f + p * 0.2f, 0.7f + p * 0.2f, 1f);
                });
                GameObject.Destroy(flashHolder.gameObject);
            }

            IEnumerator CoShake(float duration)
            {
                float t = duration;
                while (t > 0f)
                {
                    float p = (t / duration);
                    flashHolder.transform.localPosition = Vector3.right.RotateZ(System.Random.Shared.NextSingle() * 360f) * p * 0.16f * (System.Random.Shared.NextSingle() * 0.6f + 0.4f);

                    float ipip = (1 - p) * (1 - p);
                    float wait = 0.001f + ipip * 0.2f;
                    float lastTime = Time.time;
                    yield return Effects.Wait(wait);
                    t -= Time.time - lastTime;
                }
                flashHolder.transform.localPosition = Vector3.zero;
                yield break;
            }

            if (page<=1)
            {
                StartCoroutine(CoAnimFlash().WrapToIl2Cpp());
                StartCoroutine(CoShake(1.3f).WrapToIl2Cpp());
            }
            else
            {
                GameObject.Destroy(flashHolder.gameObject);
            }
            IEnumerator CoSpawnPlayerArea(int photoIndex)
            {
                yield return Effects.Wait(1.3f);
                try
                {
                    PDebug.Log("GenJusticePlayerArea");
                    var back = UnityHelper.CreateSpriteRenderer("JusticePlayerArea", transform, localPos + new Vector3(0f, 0f, 6f));
                    back.gameObject.AddComponent<SortingGroup>();
                    back.transform.localScale = new(1f, 1f, 1f);
                    back.sprite = holder.GetSprite();
                    back.material = HatManager.Instance.PlayerMaterial;
                    PlayerMaterial.SetColors(player.PlayerId, back.material);

                    var mask = UnityHelper.CreateObject<SortingGroup>("Masked", back.transform, new(0f, 0f, -0.5f));
                    var maskRenderer = UnityHelper.CreateObject<SpriteMask>("Mask", mask.transform, Vector3.zero);
                    maskRenderer.sprite = votingHolderMaskSprite.GetSprite();

                    var playerColor = DynamicPalette.PlayerColors[player.PlayerId];
                    var textColor = Color.Lerp(playerColor, DynamicPalette.IsLightColor(playerColor) ? new(0.18f, 0.18f, 0.18f, 1f) : new(1f, 1f, 1f, 1f), 0.4f);

                    var graphColor = Color.Lerp(Color.Lerp(DynamicPalette.PlayerColors[player.PlayerId], DynamicPalette.ShadowColors[player.PlayerId], 0.25f), Color.white, 0.28f);
                    for (int x = 0; x < 3; x++) InstantiateCircleGraph(back.transform, new(-0.6f + (0.28f * x), -0.5f, -0.2f), graphColor);
                    InstantiateBandGraph(24, back.transform, new(0.65f, -0.13f, -0.2f), graphColor);
                    PDebug.Log("SetAchievementTopText");
                    var topText = NebulaAsset.InstantiateText("AchievementTopText", back.transform, new(0.67f, 0.87f, -0.5f), NebulaAsset.JusticeFont, 0.14f, Virial.Text.TextAlignment.Center, new(0.5f, 0.5f), "a.k.a.", Color.white.AlphaMultiplied(0.8f));
                    if ((NebulaGameManager.Instance?.TryGetTitle(player.PlayerId, out var title) ?? false) && title != null)
                    {
                        var textComponent = new NoSGUIText(Virial.Media.GUIAlignment.Center, NebulaGUIWidgetEngine.API.GetAttribute(Virial.Text.AttributeAsset.MeetingTitle), NebulaGUIWidgetEngine.API.RawTextComponent(title.GetLocalizedText()))
                        {
                            OverlayWidget = title.GetDetailWidget(),
                            PostBuilder = text =>
                            {
                                text.outlineWidth = 0.1f;
                                text.color = Color.white.AlphaMultiplied(0.8f);
                                text.outlineColor = Color.black;
                            }
                        };
                        var obj = textComponent.Instantiate(new(5f, 5f), out _);
                        obj.AddComponent<SortingGroup>();
                        obj!.transform.SetParent(back.transform);
                        obj.transform.localPosition = new(0.67f, 0.65f, -0.5f);
                    }
                    else
                    {
                        //重複したテキストを回避する
                        lastAltIndex = (lastAltIndex + 1 + System.Random.Shared.Next(RandomAltTexts.Length - 1)) % RandomAltTexts.Length;
                        var achievementAltText = NebulaAsset.InstantiateText("AchievementAltText", back.transform, new(0.67f, 0.65f, -0.5f), NebulaAsset.JusticeFont, 0.27f, Virial.Text.TextAlignment.Center, new(0.5f, 0.5f), RandomAltTexts[lastAltIndex], Color.white.AlphaMultiplied(0.8f));
                    }
                    PDebug.Log("SetNumberText");
                    //重複した文字を回避する
                    lastNumberIndex = (lastNumberIndex + 1 + System.Random.Shared.Next(10 - 1)) % 10;
                    var numberText = UnityHelper.CreateObject<TextMeshNoS>("NumberText", mask.transform, new(-0.8f, 1.2f, -0.4f));
                    numberText.Font = NebulaAsset.JusticeFont;
                    numberText.FontSize = 1.2f;
                    numberText.TextAlignment = Virial.Text.TextAlignment.Center;
                    numberText.Pivot = new(0.5f, 0.5f);
                    numberText.Text = ((char)('0' + lastNumberIndex)).ToString();
                    numberText.Material = UnityHelper.GetMeshRendererMaskedMaterial();
                    numberText.Color = textColor;

                    PDebug.Log("SetPhoto");
                    //重複した画像を回避する
                    if (photoIndex > PhotoData.Length)
                    {
                        photoIndex = PhotoData.Length;
                    }
                    var photo = UnityHelper.CreateSpriteRenderer("JusticeHolderPhoto", back.transform, PhotoData[photoIndex].localPos.AsVector3(-0.5f));
                    photo.transform.localScale = Vector3.one * PhotoData[photoIndex].scale;
                    photo.sprite = boardPassGame!.Photos[PhotoData[photoIndex].photoIndex];
                    photo.material = HatManager.Instance.PlayerMaterial;
                    PlayerMaterial.SetColors(player.PlayerId, photo.material);
                    PDebug.Log("SetVoteAreaPos");
                    var playerState = meetingHud.playerStates.FirstOrDefault(p => p.TargetPlayerId == player.PlayerId);
                    if (playerState != null)
                    {
                        playerState.gameObject.SetActive(true);
                        playerState.gameObject.transform.localPosition = localPos + new Vector3(0.11f, -1.08f, -0.9f);
                        playerState.gameObject.transform.localScale = Vector3.one;
                    }
                    if (page>=2)
                    {
                        back.gameObject.SetActive(false);
                        playerState.gameObject.SetActive(false);
                    }
                    PDebug.Log("AddDic page:"+page);
                    pageDic.Add(page, new ValueTuple<GameObject, GameObject>(back.gameObject, playerState.gameObject));
                }
                catch (Exception E)
                {
                    PDebug.Log(E);
                }
            }

            StartCoroutine(CoSpawnPlayerArea(photoIndex).WrapToIl2Cpp());
        }
        PDebug.Log("CoStartGeneratePlayer");
        try
        {
            for (int i = 0; i < players.Count; i++)
            {
                PDebug.Log("GeneratePlayer" + i);
                if (i + 1 == players.Count && i % 2 == 0)
                {
                    SpawnVotingArea(players[i], new Vector3(0f, 0f, 0f), votingHolderLeftSprite, indexs[i],i);
                }
                else if (i % 2 == 0)
                {
                    SpawnVotingArea(players[i], new Vector3(-2f, 0f, 0f), votingHolderLeftSprite, indexs[i],i);
                }
                else
                {
                    SpawnVotingArea(players[i], new Vector3(2, 0f, 0f), votingHolderRightSprite, indexs[i],i);
                }
            }
        }
        catch (Exception e)
        {
            PDebug.Log(e);
        }


        yield break;
    }
}

[NebulaRPCHolder]
public class JusticePlus : DefinedSingleAbilityRoleTemplate<JusticePlus.Ability>, HasCitation, DefinedRole
{
    private JusticePlus():base("justicePlus", new(194,242,255), RoleCategory.CrewmateRole, NebulaTeams.CrewmateTeam, [JusticeMeetingTimeOption,JusticeMeetingMaxPlayerOption,JusticeMeetingMinPlayerOption])
    {
        ConfigurationHolder?.AddTags(ConfigurationTags.TagSNR);
        ConfigurationHolder!.Illustration = new NebulaSpriteLoader("Assets/NebulaAssets/Sprites/Configurations/Justice.png");
    }

    Citation? HasCitation.Citation => Citations.SuperNewRoles;
    AbilityAssignmentStatus DefinedRole.AssignmentStatus => AbilityAssignmentStatus.CanLoadToMadmate;

    public override Ability CreateAbility(GamePlayer player, int[] arguments) => new Ability(player, arguments.GetAsBool(0), arguments.GetAsBool(1));
    static public readonly FloatConfiguration JusticeMeetingTimeOption = NebulaAPI.Configurations.Configuration("options.role.JusticePlus.JusticePlusMeetingTime", (30f,300f,15f), 120f, FloatConfigurationDecorator.Second);
    static public readonly IntegerConfiguration JusticeMeetingMaxPlayerOption = NebulaAPI.Configurations.Configuration("options.role.JusticePlus.JusticePlusMaxPlayer", (1,15), 4);
    static public readonly IntegerConfiguration JusticeMeetingMinPlayerOption = NebulaAPI.Configurations.Configuration("options.role.JusticePlus.JusticePlusMinPlayer", (1, 3), 1);
    static public readonly JusticePlus MyRole = new();

    static private readonly GameStatsEntry StatsExiled = NebulaAPI.CreateStatsEntry("stats.JusticePlus.exiled", GameStatsCategory.Roles, MyRole);
    static private readonly GameStatsEntry StatsNonCrewmates = NebulaAPI.CreateStatsEntry("stats.JusticePlus.nonCrewmates", GameStatsCategory.Roles, MyRole);

    static readonly RemoteProcess<byte[]> RpcJusticePlusMeeting = new("JusticePlusMeeting",
        (message, _) => {
            var list = new List<GamePlayer>();
            for (int i=0;i<message.Length;i++)
            {
                list.Add(GamePlayer.GetPlayer(message[i]));
            }
            MeetingModRpc.RpcChangeVotingStyle.LocalInvoke((255, false, JusticeMeetingTimeOption, true, false));
            MeetingHudExtension.CanShowPhotos = false;
            foreach (var p in MeetingHud.Instance.playerStates) p.SetDisabled();
            var JusticePlusMeeting = UnityHelper.CreateObject<JusticePlusMeetingHud>("JusticePlusMeeting", MeetingHud.Instance.transform, Vector3.zero);
            JusticePlusMeeting.Begin(list, () =>
            {
                int votemask = 0;
                foreach (var player in list)
                {
                    votemask |= (1 << (int)player.PlayerId);
                }
                MeetingModRpc.RpcChangeVotingStyle.LocalInvoke((votemask,false, JusticePlus.JusticeMeetingTimeOption, true, false));
            });
        });
    static Virial.Media.Image StartJusticeButton = NebulaAPI.AddonAsset.GetResource("JusticeStartMeeting.png").AsImage(115f);
    [NebulaRPCHolder]
    public class Ability : AbstractPlayerUsurpableAbility, IPlayerAbility
    {
        int[] IPlayerAbility.AbilityArguments => [IsUsurped.AsInt(), usedBalance.AsInt()];
        public Ability(GamePlayer player, bool isUsurped, bool usedBalance) : base(player, isUsurped) {
            this.usedBalance = usedBalance;
        }
        List<GamePlayer> MeetingPlayers = new List<Virial.Game.Player>();
        static private readonly RoleRPC.Definition UpdateState = RoleRPC.Get<Ability>("Justice.heldMeeting", (ability, num, calledByMe) => ability.usedBalance = num == 1);

        bool usedBalance = false;
        bool isMyJusticePlusMeeting = false;
        void OnGameStarted(GameStartEvent ev)
        {
            MeetingPlayers = new List<Virial.Game.Player>();
        }
        int meetingnum;
        /*private void FixVote(PlayerFixVoteHostEvent ev)
        {
            if (isMyJusticePlusMeeting && JusticeMeetingOnlyOnePlayerCanSkip && MeetingHudExtension.CanSkip&&ev.VoteTo==null)
            {
                ev.Vote = (int)PlayerVoteArea.SkippedVote;
            }
        }*/
            [Local]
        void OnMeetingStart(MeetingStartEvent ev)
        {
            void StartJusticePlusMeeting()
            {
                try
                {
                    isMyJusticePlusMeeting = true;
                    List<byte> ids = new List<byte>();
                    for (int i = 0; i < MeetingPlayers.Count; i++)
                    {
                        if (!ids.Contains(MeetingPlayers[i].PlayerId))
                        {
                            ids.Add(MeetingPlayers[i].PlayerId);
                        }
                    }
                    RpcJusticePlusMeeting.Invoke(ids.ToArray());
                    new StaticAchievementToken("justicePlus.common1");
                    if (meetingnum == 1 && MeetingPlayers.Count == 1)
                    {
                        new StaticAchievementToken("justicePlus.another1");
                    }
                    UpdateState.RpcSync(MyPlayer, 1);
                }
                catch (Exception e)
                {
                    PDebug.Log(e);
                }
            }
            if (!usedBalance)
            {
                meetingnum++;
                MeetingPlayers = new List<GamePlayer>();
                GameObject binder = UnityHelper.CreateObject("JusticePlusButton", MeetingHud.Instance.SkipVoteButton.transform.parent, MeetingHud.Instance.SkipVoteButton.transform.localPosition, null);
                GameOperatorManager instance = GameOperatorManager.Instance;
                if (instance != null)
                {
                    instance.Subscribe<GameUpdateEvent>(delegate (GameUpdateEvent ev)
                    {
                        binder.gameObject.SetActive(!this.MyPlayer.IsDead && MeetingHud.Instance.CurrentState == MeetingHud.VoteStates.NotVoted&&!usedBalance);
                    }, new GameObjectLifespan(binder), 100);
                }
                SpriteRenderer leftRenderer = UnityHelper.CreateObject<SpriteRenderer>("JusticePlus-StartJustice", binder.transform, new Vector3(2.18f, 0.2f), null);
                leftRenderer.sprite = StartJusticeButton.GetSprite();
                PassiveButton passiveButton = leftRenderer.gameObject.SetUpButton(true, new SpriteRenderer[0], null, null);
                passiveButton.OnMouseOver.AddListener(delegate
                {
                    leftRenderer.color = UnityEngine.Color.green;
                });
                passiveButton.OnMouseOut.AddListener(delegate
                {
                    leftRenderer.color = UnityEngine.Color.white;
                });
                passiveButton.OnClick.AddListener(delegate
                {
                    if (MeetingPlayers.Count >= JusticeMeetingMinPlayerOption && MeetingPlayers.Count <= JusticeMeetingMaxPlayerOption)
                    {
                        StartJusticePlusMeeting();
                        usedBalance = true;
                    }
                });
                leftRenderer.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.6f);
                var buttonManager = NebulaAPI.CurrentGame?.GetModule<MeetingPlayerButtonManager>();
                buttonManager?.RegisterMeetingAction(new(MeetingPlayerButtonManager.Icons.AsLoader(2),
                   p =>
                   {
                       if (!(MeetingHud.Instance.state == MeetingHud.VoteStates.Voted || MeetingHud.Instance.state == MeetingHud.VoteStates.NotVoted)) return;
                       if (MeetingHudExtension.CanInvokeSomeAction)
                       {
                           if (MeetingPlayers != null && MeetingPlayers.Count <= JusticeMeetingMaxPlayerOption - 1)
                           {
                               if (IsUsurped) NebulaAsset.PlaySE(NebulaAudioClip.ButtonBreaking, volume: 1f);
                               else
                               {
                                   p.SetSelect(true);
                                   MeetingPlayers.Add(p.MyPlayer);
                               }
                           }
                       }
                   },
                   p => !usedBalance && !p.MyPlayer.IsDead && !MeetingHudExtension.ExileEvenIfTie && !MyPlayer.IsDead
                   ));
            }
        }

        [Local]
        void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (isMyJusticePlusMeeting)
            {
                if(ev.Exiled.Count() >=2)
                {
                    if (MeetingPlayers.Count((GamePlayer p)=>p.Role.Role.Category!=RoleCategory.CrewmateRole)>=4)
                    {
                        new StaticAchievementToken("justicePlus.challenge1");
                    }
                }
                else
                {
                    if (ev.Exiled != null && ev.Exiled[0] != null && ev.Exiled[0].IsCrewmate)
                    {
                        if (MeetingPlayers.Count((GamePlayer p) => p.IsImpostor) >= 3)
                        {
                            new StaticAchievementToken("justicePlus.another2");
                        }
                    }
                }
                StatsExiled.Progress(ev.Exiled.Count());
                StatsNonCrewmates.Progress(ev.Exiled.Count(p => !p.IsCrewmate));
                isMyJusticePlusMeeting = false;
            }
        }
    }
}