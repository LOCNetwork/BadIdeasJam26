using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoneyJuiceManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TMP_Text mainMoneyText;
    [SerializeField] private TMP_Text deltaMoneyText;

    [Header("Auto Bind")]
    [SerializeField] private bool useGameManagerMoneyUIIfMainMissing = true;

    [Header("Colors")]
    [SerializeField] private Color gainColor = Color.green;
    [SerializeField] private Color lossColor = Color.red;
    [SerializeField] private Color mainMoneyColor = Color.white;

    [Header("Transfer Duration")]
    [SerializeField] private float totalGainTransferDuration = 3f;
    [SerializeField] private float totalLossTransferDuration = 3f;
    [SerializeField] private float minimumStepDelay = 0.005f;

    [Header("Loss Accumulation")]
    [SerializeField] private float lossAccumulationWindow = 0.12f;

    [Header("Delta TMP Juice")]
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeStrength = 8f;
    [SerializeField] private float fadeOutDuration = 0.20f;
    [SerializeField] private float postAnimationHold = 0.08f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Gain Tick Audio")]
    [SerializeField] private AudioClip gainTickClip;
    [SerializeField][Range(0f, 1f)] private float gainTickVolume = 1f;
    [SerializeField] private Vector2 gainPitchRange = new Vector2(0.95f, 1.08f);

    [Header("Loss Tick Audio")]
    [SerializeField] private AudioClip lossTickClip;
    [SerializeField][Range(0f, 1f)] private float lossTickVolume = 1f;
    [SerializeField] private Vector2 lossPitchRange = new Vector2(0.85f, 1.0f);

    private readonly Queue<int> pendingMoneyChanges = new Queue<int>();

    private int displayedMoney;
    private int lastObservedRealMoney;
    private bool isAnimating;

    private RectTransform deltaRect;
    private Vector2 deltaBaseAnchoredPos;
    private CanvasGroup deltaCanvasGroup;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = GameManager.instance;

        if (useGameManagerMoneyUIIfMainMissing && mainMoneyText == null && gameManager != null)
            mainMoneyText = gameManager.moneyUI;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (deltaMoneyText != null)
        {
            deltaRect = deltaMoneyText.GetComponent<RectTransform>();
            if (deltaRect != null)
                deltaBaseAnchoredPos = deltaRect.anchoredPosition;

            deltaCanvasGroup = deltaMoneyText.GetComponent<CanvasGroup>();
            if (deltaCanvasGroup == null)
                deltaCanvasGroup = deltaMoneyText.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.instance;

        int currentMoney = GetRealMoney();
        displayedMoney = currentMoney;
        lastObservedRealMoney = currentMoney;

        RefreshMainMoneyTextImmediate();
        HideDeltaImmediate();
    }

    private void Update()
    {
        int realMoney = GetRealMoney();

        if (realMoney != lastObservedRealMoney)
        {
            int delta = realMoney - lastObservedRealMoney;
            pendingMoneyChanges.Enqueue(delta);
            lastObservedRealMoney = realMoney;

            if (!isAnimating)
                StartCoroutine(ProcessMoneyQueueRoutine());
        }
    }

    private void LateUpdate()
    {
        RefreshMainMoneyTextImmediate();
    }

    private int GetRealMoney()
    {
        if (gameManager == null || gameManager.gameStats == null)
            return 0;

        return gameManager.gameStats.money;
    }

    private void RefreshMainMoneyTextImmediate()
    {
        if (mainMoneyText == null)
            return;

        mainMoneyText.color = mainMoneyColor;
        mainMoneyText.text = displayedMoney.ToString();
    }

    private void HideDeltaImmediate()
    {
        if (deltaMoneyText == null)
            return;

        deltaMoneyText.gameObject.SetActive(false);

        if (deltaCanvasGroup != null)
            deltaCanvasGroup.alpha = 1f;

        if (deltaRect != null)
            deltaRect.anchoredPosition = deltaBaseAnchoredPos;
    }

    private IEnumerator ProcessMoneyQueueRoutine()
    {
        isAnimating = true;

        while (pendingMoneyChanges.Count > 0)
        {
            int firstDelta = pendingMoneyChanges.Dequeue();

            if (firstDelta > 0)
            {
                int totalGain = firstDelta;

                while (pendingMoneyChanges.Count > 0 && pendingMoneyChanges.Peek() > 0)
                    totalGain += pendingMoneyChanges.Dequeue();

                yield return StartCoroutine(AnimateGainRoutine(totalGain));
            }
            else if (firstDelta < 0)
            {
                int totalLoss = -firstDelta;

                if (lossAccumulationWindow > 0f)
                    yield return new WaitForSecondsRealtime(lossAccumulationWindow);

                while (pendingMoneyChanges.Count > 0 && pendingMoneyChanges.Peek() < 0)
                    totalLoss += -pendingMoneyChanges.Dequeue();

                yield return StartCoroutine(AnimateLossRoutine(totalLoss));
            }
        }

        isAnimating = false;
    }

    private IEnumerator AnimateGainRoutine(int amount)
    {
        if (amount <= 0)
            yield break;

        if (deltaMoneyText == null)
        {
            displayedMoney += amount;
            yield break;
        }

        PrepareDeltaTextForGain(amount);

        int remainingGain = amount;
        float stepDelay = GetStepDelayForAmount(amount, totalGainTransferDuration);

        while (remainingGain > 0)
        {
            remainingGain--;
            displayedMoney++;

            deltaMoneyText.text = $"{remainingGain}";
            PlayTick(gainTickClip, gainTickVolume, gainPitchRange);

            if (shakeDuration > 0f && shakeStrength > 0f)
                yield return StartCoroutine(ShakeDeltaRoutine(shakeDuration, shakeStrength));
            else
                yield return null;

            float waitTime = Mathf.Max(0f, stepDelay - shakeDuration);
            if (waitTime > 0f)
                yield return new WaitForSecondsRealtime(waitTime);
        }

        if (postAnimationHold > 0f)
            yield return new WaitForSecondsRealtime(postAnimationHold);

        yield return StartCoroutine(FadeOutDeltaRoutine());
    }

    private IEnumerator AnimateLossRoutine(int amount)
    {
        if (amount <= 0)
            yield break;

        if (deltaMoneyText == null)
        {
            displayedMoney -= amount;
            yield break;
        }

        PrepareDeltaTextForLoss(amount);

        int progressedLoss = 0;
        float stepDelay = GetStepDelayForAmount(amount, totalLossTransferDuration);

        while (progressedLoss < amount)
        {
            progressedLoss++;
            displayedMoney--;

            int remainingToDisplay = amount - progressedLoss;
            deltaMoneyText.text = $"- {remainingToDisplay}";
            PlayTick(lossTickClip, lossTickVolume, lossPitchRange);

            if (shakeDuration > 0f && shakeStrength > 0f)
                yield return StartCoroutine(ShakeDeltaRoutine(shakeDuration, shakeStrength));
            else
                yield return null;

            float waitTime = Mathf.Max(0f, stepDelay - shakeDuration);
            if (waitTime > 0f)
                yield return new WaitForSecondsRealtime(waitTime);
        }

        if (postAnimationHold > 0f)
            yield return new WaitForSecondsRealtime(postAnimationHold);

        yield return StartCoroutine(FadeOutDeltaRoutine());
    }

    private float GetStepDelayForAmount(int amount, float totalDuration)
    {
        if (amount <= 0)
            return minimumStepDelay;

        if (totalDuration <= 0f)
            return minimumStepDelay;

        return Mathf.Max(minimumStepDelay, totalDuration / amount);
    }

    private void PrepareDeltaTextForGain(int amount)
    {
        deltaMoneyText.gameObject.SetActive(true);
        deltaMoneyText.color = gainColor;
        deltaMoneyText.text = $"+ {amount}";

        if (deltaCanvasGroup != null)
            deltaCanvasGroup.alpha = 1f;

        if (deltaRect != null)
            deltaRect.anchoredPosition = deltaBaseAnchoredPos;
    }

    private void PrepareDeltaTextForLoss(int amount)
    {
        deltaMoneyText.gameObject.SetActive(true);
        deltaMoneyText.color = lossColor;
        deltaMoneyText.text = $"- {amount}";

        if (deltaCanvasGroup != null)
            deltaCanvasGroup.alpha = 1f;

        if (deltaRect != null)
            deltaRect.anchoredPosition = deltaBaseAnchoredPos;
    }

    private void PlayTick(AudioClip clip, float volume, Vector2 pitchRange)
    {
        if (audioSource == null || clip == null)
            return;

        float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
        float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);
        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(clip, volume);
    }

    private IEnumerator ShakeDeltaRoutine(float duration, float strength)
    {
        if (deltaRect == null || duration <= 0f || strength <= 0f)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 offset = Random.insideUnitCircle * strength;
            deltaRect.anchoredPosition = deltaBaseAnchoredPos + offset;
            yield return null;
        }

        deltaRect.anchoredPosition = deltaBaseAnchoredPos;
    }

    private IEnumerator FadeOutDeltaRoutine()
    {
        if (deltaMoneyText == null)
            yield break;

        if (deltaCanvasGroup == null)
        {
            deltaMoneyText.gameObject.SetActive(false);
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = deltaCanvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(elapsed / fadeOutDuration);
            deltaCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, n);
            yield return null;
        }

        deltaCanvasGroup.alpha = 1f;
        deltaMoneyText.gameObject.SetActive(false);

        if (deltaRect != null)
            deltaRect.anchoredPosition = deltaBaseAnchoredPos;
    }
}