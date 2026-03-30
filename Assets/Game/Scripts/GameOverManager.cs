using System.Collections;
using System.Reflection;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private MonoBehaviour quotaManager;

    [Header("Scene Objects")]
    [SerializeField] private SpriteRenderer spriteRendererToDisable;
    [SerializeField] private SpriteRenderer spriteRendererToDisable2;
    [SerializeField] private GameObject firstObjectToActivate;
    [SerializeField] private GameObject secondObjectToActivate;
    [SerializeField] private GameObject thirdObjectToFadeIn;
    [SerializeField] private GameObject fourthObjectToFadeIn;

    [Header("Timings")]
    [SerializeField] private float firstDelay = 1f;
    [SerializeField] private float secondDelay = 1f;
    [SerializeField] private float finalFadeDuration = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource managerAudioSource;
    [SerializeField] private AudioClip firstLoopClip;
    [SerializeField] private AudioClip secondLoopClip;
    [SerializeField][Range(0f, 1f)] private float firstLoopVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float secondLoopVolume = 1f;

    [Header("Optional Audio Stop")]
    [SerializeField] private bool stopAllOtherAudioSources = true;
    [SerializeField] private bool muteAllOtherAudioListeners = false;

    [Header("Debug")]
    [SerializeField] private bool detectedByLog = false;

    private bool gameOverTriggered = false;
    private bool gameOverDetectedFromLog = false;

    private MethodInfo isGameOverMethod;
    private FieldInfo gameOverField;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();

        if (managerAudioSource == null)
            managerAudioSource = GetComponent<AudioSource>();

        CacheQuotaReflection();
        SetInitialInactive(firstObjectToActivate);
        SetInitialInactive(secondObjectToActivate);
        SetInitialInactive(thirdObjectToFadeIn);
        SetInitialInactive(fourthObjectToFadeIn);
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLogMessageReceived;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLogMessageReceived;
    }

    private void Update()
    {
        if (gameOverTriggered)
            return;

        if (IsQuotaGameOver())
            StartCoroutine(GameOverRoutine());
    }

    private void CacheQuotaReflection()
    {
        if (quotaManager == null)
            return;

        System.Type quotaType = quotaManager.GetType();
        isGameOverMethod = quotaType.GetMethod("IsGameOver", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        gameOverField = quotaType.GetField("gameOver", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private void HandleLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (gameOverTriggered)
            return;

        if (string.IsNullOrEmpty(condition))
            return;

        if (condition.Contains("Game Over"))
        {
            gameOverDetectedFromLog = true;
            detectedByLog = true;
        }
    }

    private bool IsQuotaGameOver()
    {
        if (gameOverDetectedFromLog)
            return true;

        if (quotaManager == null)
            return false;

        if (isGameOverMethod != null && isGameOverMethod.ReturnType == typeof(bool))
        {
            object result = isGameOverMethod.Invoke(quotaManager, null);
            if (result is bool methodValue && methodValue)
                return true;
        }

        if (gameOverField != null && gameOverField.FieldType == typeof(bool))
        {
            bool fieldValue = (bool)gameOverField.GetValue(quotaManager);
            if (fieldValue)
                return true;
        }

        return false;
    }

    private IEnumerator GameOverRoutine()
    {
        gameOverTriggered = true;

        if (player != null)
            player.SetMovementLocked(true);

        if (spriteRendererToDisable != null)
            spriteRendererToDisable.enabled = false;
        if (spriteRendererToDisable2 != null)
            spriteRendererToDisable2.enabled = false;

        StopAllOtherGameAudio();
        PlayManagerLoop(firstLoopClip, firstLoopVolume);

        if (firstObjectToActivate != null)
            firstObjectToActivate.SetActive(true);

        if (firstDelay > 0f)
            yield return new WaitForSecondsRealtime(firstDelay);

        if (secondObjectToActivate != null)
            secondObjectToActivate.SetActive(true);

        if (secondDelay > 0f)
            yield return new WaitForSecondsRealtime(secondDelay);

        PrepareFadeObject(thirdObjectToFadeIn);
        PrepareFadeObject(fourthObjectToFadeIn);

        PlayManagerLoop(secondLoopClip, secondLoopVolume);

        yield return StartCoroutine(FadeInObjectsRoutine(finalFadeDuration, thirdObjectToFadeIn, fourthObjectToFadeIn));
    }

    private void StopAllOtherGameAudio()
    {
        if (muteAllOtherAudioListeners)
            AudioListener.pause = true;

        if (!stopAllOtherAudioSources)
            return;

        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        for (int i = 0; i < allSources.Length; i++)
        {
            AudioSource source = allSources[i];
            if (source == null)
                continue;

            if (source == managerAudioSource)
                continue;

            source.Stop();
            source.enabled = false;
        }
    }

    private void PlayManagerLoop(AudioClip clip, float volume)
    {
        if (managerAudioSource == null || clip == null)
            return;

        if (muteAllOtherAudioListeners)
            AudioListener.pause = false;

        managerAudioSource.Stop();
        managerAudioSource.clip = clip;
        managerAudioSource.loop = true;
        managerAudioSource.volume = volume;
        managerAudioSource.Play();
    }

    private void SetInitialInactive(GameObject go)
    {
        if (go != null)
            go.SetActive(false);
    }

    private void PrepareFadeObject(GameObject go)
    {
        if (go == null)
            return;

        go.SetActive(true);

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = go.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
    }

    private IEnumerator FadeInObjectsRoutine(float duration, params GameObject[] objectsToFade)
    {
        if (duration <= 0f)
        {
            for (int i = 0; i < objectsToFade.Length; i++)
                SetCanvasGroupAlpha(objectsToFade[i], 1f);

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < objectsToFade.Length; i++)
                SetCanvasGroupAlpha(objectsToFade[i], t);

            yield return null;
        }

        for (int i = 0; i < objectsToFade.Length; i++)
            SetCanvasGroupAlpha(objectsToFade[i], 1f);
    }

    private void SetCanvasGroupAlpha(GameObject go, float alpha)
    {
        if (go == null)
            return;

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = go.AddComponent<CanvasGroup>();

        cg.alpha = alpha;
    }
}