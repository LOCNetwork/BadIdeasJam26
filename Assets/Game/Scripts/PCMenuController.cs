using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PCCatalogType
{
    Base = 0,
    Tech = 1,
    DarkWeb = 2,
    Monster = 3
}

[System.Serializable]
public class PCJuiceProfile
{
    [Header("General")]
    public float initialDelay = 0f;
    public float staggerBetweenChildren = 0.02f;

    [Header("Move (relative to canvas size)")]
    [Range(-2f, 2f)] public float startOffsetXNormalized = 0f;
    [Range(-2f, 2f)] public float startOffsetYNormalized = 0.20f;
    public float moveDuration = 0.18f;

    [Header("Scale (multipliers over final scale)")]
    public float startScaleXMultiplier = 0.7f;
    public float startScaleYMultiplier = 1.35f;
    public float overshootScaleXMultiplier = 1.08f;
    public float overshootScaleYMultiplier = 0.95f;
    public float scaleDuration = 0.15f;

    [Header("Bounce")]
    public float bounceDuration = 0.08f;
}

[System.Serializable]
public class PCExitJuiceProfile
{
    [Header("General")]
    public float initialDelay = 0f;
    public float staggerBetweenChildren = 0.01f;

    [Header("Drop (relative to canvas size)")]
    [Range(-2f, 2f)] public float endOffsetXNormalized = 0f;
    [Range(-2f, 2f)] public float endOffsetYNormalized = -0.35f;
    public float dropDuration = 0.18f;

    [Header("Pre-Drop Stretch (multipliers over final scale)")]
    public float stretchScaleXMultiplier = 0.82f;
    public float stretchScaleYMultiplier = 1.28f;
    public float stretchDuration = 0.08f;

    [Header("Bounce / Squash")]
    public float squashScaleXMultiplier = 1.12f;
    public float squashScaleYMultiplier = 0.88f;
    public float squashDuration = 0.06f;
    public float returnDuration = 0.05f;

    [Header("Fade Out")]
    public bool useFade = true;
    [Range(0f, 1f)] public float finalAlpha = 0f;
    public float fadeDuration = 0.18f;

    [Header("Final")]
    public float endDelay = 0f;
}

[System.Serializable]
public class PCCatalogView
{
    public PCCatalogType type;
    [Min(0)] public int unlockLevel = 0;

    [Header("Objects")]
    public GameObject searchBarObject;
    public GameObject pageBarObject;

    [Header("BG Search Bar Exact Position")]
    public Vector2 bgSearchBarAnchoredPosition;
}

public class PCMenuController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject pcRoot;

    [Header("Shell visible during intro/outro juice only")]
    [SerializeField] private GameObject pcBG;
    [SerializeField] private GameObject pcImageBackground;

    [Header("Canvas Reference")]
    [SerializeField] private RectTransform canvasRect;

    [Header("Main Views")]
    [SerializeField] private GameObject bgSearchBarHome;
    [SerializeField] private GameObject bgSearchBar;
    [SerializeField] private RectTransform bgSearchBarRect;

    [Header("Giggle Home Variants")]
    [Tooltip("Index 0 = Giggle base, 1 = Giggle1, 2 = Giggle2, etc.")]
    [SerializeField] private GameObject[] giggleVariants;

    [Header("Catalogs")]
    [SerializeField] private List<PCCatalogView> catalogs = new List<PCCatalogView>();

    [Header("Upgrade Progress")]
    [SerializeField] private bool debugMode = false;
    [SerializeField][Range(0, 4)] private int debugUpgradeLevel = 0;
    [SerializeField][Range(0, 4)] private int currentUpgradeLevel = 0;

    [Header("Juice Entry")]
    [SerializeField] private PCJuiceProfile openJuice = new PCJuiceProfile();

    [Header("Juice Exit")]
    [SerializeField] private PCExitJuiceProfile closeJuice = new PCExitJuiceProfile();

    private bool isOpen = false;
    private bool isBusy = false;
    private Player currentPlayer;
    private PCCatalogType currentCatalog = PCCatalogType.Base;

    private readonly Dictionary<Transform, Vector3> cachedAnchoredPos = new();
    private readonly Dictionary<Transform, Vector3> cachedLocalScale = new();
    private readonly Dictionary<Transform, CanvasGroup> cachedCanvasGroups = new();
    private readonly Dictionary<Transform, float> cachedAlpha = new();

    public bool IsOpen => isOpen;

    public void Toggle(Player player)
    {
        if (isBusy)
            return;

        if (!isOpen)
            Open(player);
        else
            Close();
    }

    public void Open(Player player)
    {
        if (isBusy || isOpen || pcRoot == null)
            return;

        currentPlayer = player;
        if (currentPlayer != null)
            currentPlayer.SetMovementLocked(true);

        pcRoot.SetActive(true);

        // Solo shell visible en intro
        SetOnlyShellActive();

        StartCoroutine(OpenRoutine());
    }

    public void Close()
    {
        if (isBusy || !isOpen || pcRoot == null)
            return;

        StartCoroutine(CloseRoutine());
    }

    public void OpenHome()
    {
        if (!isOpen || isBusy)
            return;

        ApplyHomeState();
    }

    public void OpenPaperBase()
    {
        if (!isOpen || isBusy)
            return;

        OpenCatalog(PCCatalogType.Base);
    }

    public void OpenPaperTech()
    {
        if (!isOpen || isBusy)
            return;

        OpenCatalog(PCCatalogType.Tech);
    }

    public void OpenPaperDarkWeb()
    {
        if (!isOpen || isBusy)
            return;

        OpenCatalog(PCCatalogType.DarkWeb);
    }

    public void OpenPaperMonster()
    {
        if (!isOpen || isBusy)
            return;

        OpenCatalog(PCCatalogType.Monster);
    }

    private int GetActiveUpgradeLevel()
    {
        return debugMode ? debugUpgradeLevel : currentUpgradeLevel;
    }

    private void SetOnlyShellActive()
    {
        if (pcRoot == null) return;

        Transform[] allChildren = pcRoot.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform t = allChildren[i];

            if (t == pcRoot.transform)
                continue;

            GameObject go = t.gameObject;
            bool keepActive = false;

            if (pcBG != null && go == pcBG)
                keepActive = true;

            if (pcImageBackground != null && go == pcImageBackground)
                keepActive = true;

            go.SetActive(keepActive);
        }

        if (pcBG != null)
            pcBG.SetActive(true);

        if (pcImageBackground != null)
            pcImageBackground.SetActive(true);
    }

    private void ActivateAllUIAfterIntro()
    {
        if (pcRoot == null) return;

        Transform[] allChildren = pcRoot.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform t = allChildren[i];

            if (t == pcRoot.transform)
                continue;

            t.gameObject.SetActive(true);
        }
    }

    private void ApplyHomeState()
    {
        int level = GetActiveUpgradeLevel();

        SetAllGiggles(false);
        SetAllCatalogPages(false);

        if (bgSearchBarHome != null)
            bgSearchBarHome.SetActive(true);

        if (bgSearchBar != null)
            bgSearchBar.SetActive(false);

        int homeIndex = Mathf.Clamp(level, 0, giggleVariants.Length - 1);
        if (giggleVariants != null && giggleVariants.Length > 0 && giggleVariants[homeIndex] != null)
            giggleVariants[homeIndex].SetActive(true);

        ApplyUnlockedCatalogButtons();
    }

    private void OpenCatalog(PCCatalogType catalogType)
    {
        int level = GetActiveUpgradeLevel();
        PCCatalogView view = GetCatalog(catalogType);

        if (view == null)
            return;

        if (level < view.unlockLevel)
            return;

        currentCatalog = catalogType;

        SetAllGiggles(false);
        SetAllCatalogPages(false);

        if (bgSearchBarHome != null)
            bgSearchBarHome.SetActive(false);

        if (bgSearchBar != null)
            bgSearchBar.SetActive(true);

        if (bgSearchBarRect != null)
            bgSearchBarRect.anchoredPosition = view.bgSearchBarAnchoredPosition;

        if (view.pageBarObject != null)
            view.pageBarObject.SetActive(true);

        ApplyUnlockedCatalogButtons();
    }

    private PCCatalogView GetCatalog(PCCatalogType type)
    {
        for (int i = 0; i < catalogs.Count; i++)
        {
            if (catalogs[i].type == type)
                return catalogs[i];
        }

        return null;
    }

    private void SetAllGiggles(bool active)
    {
        if (giggleVariants == null) return;

        for (int i = 0; i < giggleVariants.Length; i++)
        {
            if (giggleVariants[i] != null)
                giggleVariants[i].SetActive(active);
        }
    }

    private void SetAllCatalogPages(bool active)
    {
        if (catalogs == null) return;

        for (int i = 0; i < catalogs.Count; i++)
        {
            if (catalogs[i].pageBarObject != null)
                catalogs[i].pageBarObject.SetActive(active);

            if (catalogs[i].searchBarObject != null)
                catalogs[i].searchBarObject.SetActive(active);
        }
    }

    private void ApplyUnlockedCatalogButtons()
    {
        int level = GetActiveUpgradeLevel();

        for (int i = 0; i < catalogs.Count; i++)
        {
            bool unlocked = level >= catalogs[i].unlockLevel;

            if (catalogs[i].searchBarObject != null)
                catalogs[i].searchBarObject.SetActive(unlocked);
        }
    }

    private IEnumerator OpenRoutine()
    {
        isBusy = true;
        isOpen = true;

        yield return null;
        ForceCanvasRefresh();

        List<RectTransform> shellTargets = GetShellRects();

        for (int i = 0; i < shellTargets.Count; i++)
            StartCoroutine(AnimateOpen(shellTargets[i], openJuice, i * openJuice.staggerBetweenChildren));

        float shellDuration =
            openJuice.initialDelay +
            openJuice.moveDuration +
            openJuice.scaleDuration +
            openJuice.bounceDuration +
            (shellTargets.Count * openJuice.staggerBetweenChildren);

        yield return new WaitForSecondsRealtime(shellDuration);

        // Tras el juice de entrada, activar la UI real
        ActivateAllUIAfterIntro();
        ApplyHomeState();
        ForceCanvasRefresh();

        isBusy = false;
    }

    private IEnumerator CloseRoutine()
    {
        isBusy = true;

        // Antes del juice de salida: dejar solo shell
        SetOnlyShellActive();
        yield return null;
        ForceCanvasRefresh();

        List<RectTransform> shellTargets = GetShellRects();

        for (int i = 0; i < shellTargets.Count; i++)
            StartCoroutine(AnimateClose(shellTargets[i], closeJuice, i * closeJuice.staggerBetweenChildren));

        float totalDuration =
            closeJuice.initialDelay +
            closeJuice.stretchDuration +
            Mathf.Max(closeJuice.dropDuration, closeJuice.fadeDuration) +
            closeJuice.returnDuration +
            closeJuice.endDelay +
            (shellTargets.Count * closeJuice.staggerBetweenChildren);

        yield return new WaitForSecondsRealtime(totalDuration);

        if (pcRoot != null)
            pcRoot.SetActive(false);

        if (currentPlayer != null)
            currentPlayer.SetMovementLocked(false);

        currentPlayer = null;
        isBusy = false;
        isOpen = false;
    }

    private void ForceCanvasRefresh()
    {
        Canvas.ForceUpdateCanvases();

        if (pcRoot != null)
        {
            RectTransform[] rects = pcRoot.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rects[i]);
        }

        Canvas.ForceUpdateCanvases();
    }

    private List<RectTransform> GetShellRects()
    {
        List<RectTransform> result = new List<RectTransform>();

        if (pcBG != null)
        {
            RectTransform[] bgRects = pcBG.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < bgRects.Length; i++)
            {
                RectTransform rt = bgRects[i];
                if (rt == null) continue;
                if (!rt.gameObject.activeInHierarchy) continue;

                CacheIfNeeded(rt);
                if (!result.Contains(rt))
                    result.Add(rt);
            }
        }

        if (pcImageBackground != null && pcImageBackground != pcBG)
        {
            RectTransform[] imageRects = pcImageBackground.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < imageRects.Length; i++)
            {
                RectTransform rt = imageRects[i];
                if (rt == null) continue;
                if (!rt.gameObject.activeInHierarchy) continue;
                if (result.Contains(rt)) continue;

                CacheIfNeeded(rt);
                result.Add(rt);
            }
        }

        return result;
    }

    private void CacheIfNeeded(RectTransform rt)
    {
        if (!cachedAnchoredPos.ContainsKey(rt.transform))
            cachedAnchoredPos.Add(rt.transform, rt.anchoredPosition3D);

        if (!cachedLocalScale.ContainsKey(rt.transform))
            cachedLocalScale.Add(rt.transform, rt.localScale);

        if (!cachedCanvasGroups.ContainsKey(rt.transform))
        {
            CanvasGroup cg = rt.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = rt.gameObject.AddComponent<CanvasGroup>();

            cachedCanvasGroups.Add(rt.transform, cg);
        }

        if (!cachedAlpha.ContainsKey(rt.transform))
            cachedAlpha.Add(rt.transform, cachedCanvasGroups[rt.transform].alpha);
    }

    private Vector3 GetCanvasRelativeOffset(float normalizedX, float normalizedY)
    {
        if (canvasRect == null)
            return new Vector3(normalizedX * 1920f, normalizedY * 1080f, 0f);

        Rect r = canvasRect.rect;
        return new Vector3(r.width * normalizedX, r.height * normalizedY, 0f);
    }

    private IEnumerator AnimateOpen(RectTransform rt, PCJuiceProfile profile, float staggerDelay)
    {
        if (rt == null) yield break;

        yield return new WaitForSecondsRealtime(profile.initialDelay + staggerDelay);

        CacheIfNeeded(rt);

        Vector3 basePos = cachedAnchoredPos[rt.transform];
        Vector3 baseScale = cachedLocalScale[rt.transform];

        Vector3 startOffset = GetCanvasRelativeOffset(profile.startOffsetXNormalized, profile.startOffsetYNormalized);
        Vector3 startPos = basePos + startOffset;

        Vector3 startScale = new Vector3(
            baseScale.x * profile.startScaleXMultiplier,
            baseScale.y * profile.startScaleYMultiplier,
            baseScale.z
        );

        Vector3 overshootScale = new Vector3(
            baseScale.x * profile.overshootScaleXMultiplier,
            baseScale.y * profile.overshootScaleYMultiplier,
            baseScale.z
        );

        rt.anchoredPosition3D = startPos;
        rt.localScale = startScale;

        float t = 0f;
        while (t < profile.moveDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / profile.moveDuration);
            float e = EaseOutBack(n);

            rt.anchoredPosition3D = Vector3.LerpUnclamped(startPos, basePos, e);
            yield return null;
        }

        t = 0f;
        while (t < profile.scaleDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / profile.scaleDuration);
            rt.localScale = Vector3.LerpUnclamped(startScale, overshootScale, n);
            yield return null;
        }

        t = 0f;
        while (t < profile.bounceDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / profile.bounceDuration);
            rt.localScale = Vector3.LerpUnclamped(overshootScale, baseScale, n);
            yield return null;
        }

        rt.anchoredPosition3D = basePos;
        rt.localScale = baseScale;
    }

    private IEnumerator AnimateClose(RectTransform rt, PCExitJuiceProfile profile, float staggerDelay)
    {
        if (rt == null) yield break;

        yield return new WaitForSecondsRealtime(profile.initialDelay + staggerDelay);

        CacheIfNeeded(rt);

        Vector3 basePos = cachedAnchoredPos[rt.transform];
        Vector3 baseScale = cachedLocalScale[rt.transform];

        CanvasGroup cg = cachedCanvasGroups[rt.transform];
        float startAlpha = cachedAlpha[rt.transform];

        Vector3 stretchScale = new Vector3(
            baseScale.x * profile.stretchScaleXMultiplier,
            baseScale.y * profile.stretchScaleYMultiplier,
            baseScale.z
        );

        Vector3 squashScale = new Vector3(
            baseScale.x * profile.squashScaleXMultiplier,
            baseScale.y * profile.squashScaleYMultiplier,
            baseScale.z
        );

        Vector3 endOffset = GetCanvasRelativeOffset(profile.endOffsetXNormalized, profile.endOffsetYNormalized);
        Vector3 endPos = basePos + endOffset;

        // 1) Stretch hacia arriba
        float t = 0f;
        while (t < profile.stretchDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / profile.stretchDuration);
            float e = EaseOutBackSoft(n);

            rt.localScale = Vector3.LerpUnclamped(baseScale, stretchScale, e);
            yield return null;
        }

        // 2) Caída principal + fade
        t = 0f;
        while (t < profile.dropDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / profile.dropDuration);

            float moveCurve = EaseInBackHeavy(n);
            float scaleCurve = EaseInCubic(n);

            rt.anchoredPosition3D = Vector3.LerpUnclamped(basePos, endPos, moveCurve);
            rt.localScale = Vector3.LerpUnclamped(stretchScale, squashScale, scaleCurve);

            if (profile.useFade)
            {
                float fadeT = Mathf.Clamp01(t / Mathf.Max(0.0001f, profile.fadeDuration));
                cg.alpha = Mathf.Lerp(startAlpha, profile.finalAlpha, EaseInCubic(fadeT));
            }

            yield return null;
        }

        // 3) Pequeño rebote visual
        t = 0f;
        while (t < profile.returnDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / profile.returnDuration);

            rt.localScale = Vector3.LerpUnclamped(squashScale, baseScale, n);
            yield return null;
        }

        if (profile.endDelay > 0f)
            yield return new WaitForSecondsRealtime(profile.endDelay);

        rt.anchoredPosition3D = basePos;
        rt.localScale = baseScale;
        cg.alpha = startAlpha;
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private float EaseInCubic(float x)
    {
        return x * x * x;
    }

    private float EaseOutBackSoft(float x)
    {
        float c1 = 1.1f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private float EaseInBackHeavy(float x)
    {
        float c1 = 2.4f;
        float c3 = c1 + 1f;
        return c3 * x * x * x - c1 * x * x;
    }
}