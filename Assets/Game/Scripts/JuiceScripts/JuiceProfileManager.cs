using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class JuiceTargetEntry
{
    [Header("Animated Target")]
    public Transform target;

    [Header("Hover / Click Source")]
    [Tooltip("Image del botón que detecta hover. El juice se aplicará al target de arriba.")]
    public Image hoverSourceImage;

    [Header("Flags")]
    public bool juiceUpDown = false;
    public bool juiceOnHover = false;
    public bool juiceOnClick = false;

    [Header("Up / Down")]
    public float upDownAmplitude = 6f;
    public float upDownSpeed = 4f;
    public bool useUnscaledTimeForUpDown = true;

    [Header("Hover Juice")]
    public float hoverScaleMultiplier = 1.08f;
    public float hoverLerpSpeed = 10f;

    [Header("Click Juice")]
    public float clickScaleMultiplier = 0.88f;
    public float clickLerpSpeed = 14f;
    public float clickRecoverLerpSpeed = 10f;
    public Color clickColor = Color.gray;
    public float clickHoldDuration = 0.06f;

    [Header("Audio")]
    public AudioClip hoverEnterClip;
    public AudioClip clickClip;
    [Range(0f, 1f)] public float hoverEnterVolume = 1f;
    [Range(0f, 1f)] public float clickVolume = 1f;

    [HideInInspector] public bool initialized;
    [HideInInspector] public bool isRectTransform;
    [HideInInspector] public RectTransform rectTarget;
    [HideInInspector] public Vector3 baseLocalPosition;
    [HideInInspector] public Vector3 baseAnchoredPosition3D;
    [HideInInspector] public Vector3 baseLocalScale;

    [HideInInspector] public bool isHovered;
    [HideInInspector] public bool isClickAnimating;
    [HideInInspector] public Coroutine clickRoutine;

    [HideInInspector] public Image targetImage;
    [HideInInspector] public Color baseColor = Color.white;

    [HideInInspector] public JuiceUIHoverRelay uiRelay;
}

public class JuiceProfileManager : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private List<JuiceTargetEntry> targets = new List<JuiceTargetEntry>();

    [Header("Shared Audio Source")]
    [SerializeField] private AudioSource sharedAudioSource;

    private float sharedAudioSourceBasePitch = 1f;
    private bool sharedAudioSourceBaseLoop = false;
    private AudioClip sharedAudioSourceBaseClip;

    private void Awake()
    {
        CacheAudioSourceBaseState();
        InitializeAllTargets();
    }

    private void OnEnable()
    {
        CacheAudioSourceBaseState();
        InitializeAllTargets();
        RestoreAllToBaseImmediate();
    }

    private void Update()
    {
        float scaledDt = Time.deltaTime;
        float unscaledDt = Time.unscaledDeltaTime;
        float scaledTime = Time.time;
        float unscaledTime = Time.unscaledTime;

        for (int i = 0; i < targets.Count; i++)
        {
            JuiceTargetEntry entry = targets[i];

            if (!IsEntryValid(entry))
                continue;

            if (!entry.target.gameObject.activeInHierarchy)
                continue;

            UpdateUpDown(entry, scaledDt, unscaledDt, scaledTime, unscaledTime);
            UpdateHover(entry, scaledDt, unscaledDt);
        }
    }

    private void CacheAudioSourceBaseState()
    {
        if (sharedAudioSource == null)
            return;

        sharedAudioSourceBasePitch = sharedAudioSource.pitch;
        sharedAudioSourceBaseLoop = sharedAudioSource.loop;
        sharedAudioSourceBaseClip = sharedAudioSource.clip;
    }

    private void InitializeAllTargets()
    {
        for (int i = 0; i < targets.Count; i++)
            InitializeEntry(targets[i], i);
    }

    private void InitializeEntry(JuiceTargetEntry entry, int index)
    {
        if (entry == null || entry.target == null)
            return;

        entry.rectTarget = entry.target as RectTransform;
        entry.isRectTransform = entry.rectTarget != null;

        entry.baseLocalPosition = entry.target.localPosition;
        entry.baseLocalScale = entry.target.localScale;

        if (entry.isRectTransform)
            entry.baseAnchoredPosition3D = entry.rectTarget.anchoredPosition3D;

        entry.targetImage = entry.target.GetComponent<Image>();
        if (entry.targetImage != null)
            entry.baseColor = entry.targetImage.color;

        entry.initialized = true;

        if (entry.juiceOnHover && entry.hoverSourceImage != null)
            EnsureUIHoverRelay(entry, index);
    }

    private void EnsureUIHoverRelay(JuiceTargetEntry entry, int index)
    {
        if (entry.hoverSourceImage == null)
            return;

        GameObject sourceObject = entry.hoverSourceImage.gameObject;

        JuiceUIHoverRelay relay = sourceObject.GetComponent<JuiceUIHoverRelay>();
        if (relay == null)
            relay = sourceObject.AddComponent<JuiceUIHoverRelay>();

        relay.Configure(this, index);
        entry.uiRelay = relay;
    }

    private bool IsEntryValid(JuiceTargetEntry entry)
    {
        return entry != null && entry.target != null && entry.initialized;
    }

    private void UpdateUpDown(
        JuiceTargetEntry entry,
        float scaledDt,
        float unscaledDt,
        float scaledTime,
        float unscaledTime)
    {
        if (!entry.juiceUpDown)
            return;

        float usedTime = entry.useUnscaledTimeForUpDown ? unscaledTime : scaledTime;
        float usedDt = entry.useUnscaledTimeForUpDown ? unscaledDt : scaledDt;

        float offsetY = Mathf.Sin(usedTime * entry.upDownSpeed) * entry.upDownAmplitude;

        if (entry.isRectTransform)
        {
            Vector3 targetPos = entry.baseAnchoredPosition3D + new Vector3(0f, offsetY, 0f);
            entry.rectTarget.anchoredPosition3D = Vector3.Lerp(
                entry.rectTarget.anchoredPosition3D,
                targetPos,
                usedDt * entry.upDownSpeed
            );
        }
        else
        {
            Vector3 targetPos = entry.baseLocalPosition + new Vector3(0f, offsetY, 0f);
            entry.target.localPosition = Vector3.Lerp(
                entry.target.localPosition,
                targetPos,
                usedDt * entry.upDownSpeed
            );
        }
    }

    private void UpdateHover(JuiceTargetEntry entry, float scaledDt, float unscaledDt)
    {
        if (!entry.juiceOnHover)
            return;

        if (entry.isClickAnimating)
            return;

        float dt = unscaledDt;
        float targetMultiplier = entry.isHovered ? entry.hoverScaleMultiplier : 1f;
        Vector3 desiredScale = entry.baseLocalScale * targetMultiplier;

        entry.target.localScale = Vector3.Lerp(
            entry.target.localScale,
            desiredScale,
            dt * entry.hoverLerpSpeed
        );

        if (entry.targetImage != null)
        {
            Color desiredColor = entry.baseColor;
            entry.targetImage.color = Color.Lerp(
                entry.targetImage.color,
                desiredColor,
                dt * entry.hoverLerpSpeed
            );
        }
    }

    private void RestoreAllToBaseImmediate()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            JuiceTargetEntry entry = targets[i];
            if (!IsEntryValid(entry))
                continue;

            if (entry.isRectTransform)
                entry.rectTarget.anchoredPosition3D = entry.baseAnchoredPosition3D;
            else
                entry.target.localPosition = entry.baseLocalPosition;

            entry.target.localScale = entry.baseLocalScale;
            entry.isHovered = false;
            entry.isClickAnimating = false;

            if (entry.targetImage != null)
                entry.targetImage.color = entry.baseColor;
        }
    }

    public void NotifyPointerEnter(int index)
    {
        if (!IsValidIndex(index))
            return;

        JuiceTargetEntry entry = targets[index];
        if (!entry.juiceOnHover)
            return;

        entry.isHovered = true;
        PlayEntryClip(entry.hoverEnterClip, entry.hoverEnterVolume);
    }

    public void NotifyPointerExit(int index)
    {
        if (!IsValidIndex(index))
            return;

        JuiceTargetEntry entry = targets[index];
        if (!entry.juiceOnHover)
            return;

        entry.isHovered = false;
    }

    public void PlayClickJuice(int index)
    {
        if (!IsValidIndex(index))
            return;

        JuiceTargetEntry entry = targets[index];
        if (!entry.juiceOnClick || entry.target == null)
            return;

        PlayEntryClip(entry.clickClip, entry.clickVolume);

        if (entry.clickRoutine != null)
            StopCoroutine(entry.clickRoutine);

        entry.clickRoutine = StartCoroutine(ClickJuiceRoutine(entry));
    }

    private void PlayEntryClip(AudioClip clip, float volume)
    {
        if (sharedAudioSource == null || clip == null)
            return;

        // Asegura que un clip en loop o configuraciones previas no interfieran
        bool wasPlaying = sharedAudioSource.isPlaying;
        AudioClip previousClip = sharedAudioSource.clip;
        bool previousLoop = sharedAudioSource.loop;
        float previousPitch = sharedAudioSource.pitch;

        sharedAudioSource.loop = false;
        sharedAudioSource.clip = null;
        sharedAudioSource.pitch = 1f;
        sharedAudioSource.PlayOneShot(clip, volume);

        // Restauramos el estado base para no dejar la source contaminada
        sharedAudioSource.loop = sharedAudioSourceBaseLoop;
        sharedAudioSource.pitch = sharedAudioSourceBasePitch;
        sharedAudioSource.clip = sharedAudioSourceBaseClip != null ? sharedAudioSourceBaseClip : previousClip;
    }

    private IEnumerator ClickJuiceRoutine(JuiceTargetEntry entry)
    {
        entry.isClickAnimating = true;

        Vector3 startScale = entry.target.localScale;
        Vector3 pressedScale = entry.baseLocalScale * entry.clickScaleMultiplier;

        Color startColor = entry.targetImage != null ? entry.targetImage.color : Color.white;
        Color targetClickColor = entry.clickColor;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * entry.clickLerpSpeed;
            float n = Mathf.Clamp01(t);

            entry.target.localScale = Vector3.Lerp(startScale, pressedScale, n);

            if (entry.targetImage != null)
                entry.targetImage.color = Color.Lerp(startColor, targetClickColor, n);

            yield return null;
        }

        if (entry.clickHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(entry.clickHoldDuration);

        Vector3 recoverTargetScale = entry.baseLocalScale * (entry.isHovered ? entry.hoverScaleMultiplier : 1f);
        Color recoverTargetColor = entry.baseColor;

        t = 0f;
        Vector3 currentScale = entry.target.localScale;
        Color currentColor = entry.targetImage != null ? entry.targetImage.color : Color.white;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * entry.clickRecoverLerpSpeed;
            float n = Mathf.Clamp01(t);

            entry.target.localScale = Vector3.Lerp(currentScale, recoverTargetScale, n);

            if (entry.targetImage != null)
                entry.targetImage.color = Color.Lerp(currentColor, recoverTargetColor, n);

            yield return null;
        }

        entry.target.localScale = recoverTargetScale;

        if (entry.targetImage != null)
            entry.targetImage.color = recoverTargetColor;

        entry.isClickAnimating = false;
        entry.clickRoutine = null;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < targets.Count;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            JuiceTargetEntry entry = targets[i];
            if (entry == null)
                continue;

            if (entry.hoverScaleMultiplier < 1f)
                entry.hoverScaleMultiplier = 1f;

            if (entry.clickScaleMultiplier <= 0f)
                entry.clickScaleMultiplier = 0.88f;

            if (entry.hoverLerpSpeed < 0f)
                entry.hoverLerpSpeed = 0f;

            if (entry.clickLerpSpeed < 0f)
                entry.clickLerpSpeed = 0f;

            if (entry.clickRecoverLerpSpeed < 0f)
                entry.clickRecoverLerpSpeed = 0f;

            if (entry.upDownSpeed < 0f)
                entry.upDownSpeed = 0f;
        }
    }
#endif
}

public class JuiceUIHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private JuiceProfileManager manager;
    private int index = -1;

    public void Configure(JuiceProfileManager newManager, int newIndex)
    {
        manager = newManager;
        index = newIndex;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager == null)
            return;

        manager.NotifyPointerEnter(index);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (manager == null)
            return;

        manager.NotifyPointerExit(index);
    }
}