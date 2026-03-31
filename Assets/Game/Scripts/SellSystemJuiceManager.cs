using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SellSystemJuiceManager : MonoBehaviour
{
    [Header("Watched Root")]
    [SerializeField] private GameObject watchedObject;

    [Header("Scan")]
    [SerializeField] private float scanInterval = 0.05f;

    [Header("Image Juice")]
    [SerializeField] private bool animateImages = true;
    [SerializeField] private float imagePunchDuration = 0.22f;
    [SerializeField] private Vector3 imageStartScaleMultiplier = new Vector3(0.92f, 0.92f, 1f);
    [SerializeField] private Vector3 imageOvershootScaleMultiplier = new Vector3(1.08f, 1.08f, 1f);
    [SerializeField] private float imageRotationStartZ = 195f;
    [SerializeField] private float imageRotationEndZ = 15f;

    [Header("Image Spawn Audio")]
    [SerializeField] private AudioSource imageAudioSource;
    [SerializeField] private AudioClip imageSpawnClip;
    [SerializeField][Range(0f, 1f)] private float imageSpawnVolume = 1f;
    [SerializeField] private float baseImagePitch = 1f;
    [SerializeField] private float pitchIncreasePerImage = 0.035f;
    [SerializeField] private float maxImagePitch = 1.5f;

    [Header("TMP Typewriter")]
    [SerializeField] private bool animateTexts = true;
    [SerializeField] private float characterInterval = 0.015f;

    [Header("Numeric Character Juice")]
    [SerializeField] private bool animateNumericCharacters = true;
    [SerializeField] private float numericWaveAmplitude = 3f;
    [SerializeField] private float numericWaveSpeed = 5f;
    [SerializeField] private float numericShakeStrength = 0.65f;
    [SerializeField] private float numericShakeSpeed = 22f;

    [Header("Activation Juice")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float cameraShakeDuration = 0.18f;
    [SerializeField] private float cameraShakeStrength = 0.08f;

    [Header("Deactivate Juice")]
    [SerializeField] private ParticleSystem particlesOnDeactivate;
    [SerializeField] private Transform particlesSpawnPoint;

    private readonly Dictionary<int, ChildState> trackedChildren = new Dictionary<int, ChildState>();

    private bool lastWatchedActiveState = false;
    private float currentImagePitch;
    private Coroutine cameraShakeRoutine;

    private void Awake()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main;

        currentImagePitch = baseImagePitch;
    }

    private void Start()
    {
        lastWatchedActiveState = watchedObject != null && watchedObject.activeInHierarchy;
        StartCoroutine(WatchRoutine());
    }

    private IEnumerator WatchRoutine()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(scanInterval);

        while (true)
        {
            UpdateWatchedObjectState();
            ScanChildren();
            UpdateNumericTMPJuice();
            yield return wait;
        }
    }

    private void UpdateWatchedObjectState()
    {
        bool isActive = watchedObject != null && watchedObject.activeInHierarchy;

        if (isActive != lastWatchedActiveState)
        {
            if (isActive)
            {
                OnWatchedObjectActivated();
            }
            else
            {
                OnWatchedObjectDeactivated();
            }

            lastWatchedActiveState = isActive;
        }
    }

    private void OnWatchedObjectActivated()
    {
        if (cameraShakeRoutine != null)
            StopCoroutine(cameraShakeRoutine);

        if (targetCamera != null)
            cameraShakeRoutine = StartCoroutine(CameraShakeRoutine());
    }

    private void OnWatchedObjectDeactivated()
    {
        currentImagePitch = baseImagePitch;

        foreach (var kv in trackedChildren)
        {
            if (kv.Value != null && kv.Value.textAnimator != null)
                kv.Value.textAnimator.ResetMeshImmediate();
        }

        trackedChildren.Clear();

        if (particlesOnDeactivate != null)
        {
            Transform spawn = particlesSpawnPoint != null ? particlesSpawnPoint : transform;
            particlesOnDeactivate.transform.position = spawn.position;
            particlesOnDeactivate.transform.rotation = spawn.rotation;
            particlesOnDeactivate.gameObject.SetActive(true);
            particlesOnDeactivate.Play(true);
        }
    }

    private void ScanChildren()
    {
        if (watchedObject == null || !watchedObject.activeInHierarchy)
            return;

        HashSet<int> currentIds = new HashSet<int>();
        Transform[] transforms = watchedObject.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == null || t == watchedObject.transform)
                continue;

            int id = t.gameObject.GetInstanceID();
            currentIds.Add(id);

            if (!trackedChildren.ContainsKey(id))
            {
                ChildState state = BuildStateForTransform(t);
                trackedChildren.Add(id, state);
                ProcessNewChild(state);
            }
            else
            {
                ChildState state = trackedChildren[id];
                RefreshExistingState(state);
            }
        }

        List<int> toRemove = new List<int>();
        foreach (var kv in trackedChildren)
        {
            if (!currentIds.Contains(kv.Key))
            {
                if (kv.Value != null && kv.Value.textAnimator != null)
                    kv.Value.textAnimator.ResetMeshImmediate();

                toRemove.Add(kv.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
            trackedChildren.Remove(toRemove[i]);
    }

    private ChildState BuildStateForTransform(Transform t)
    {
        ChildState state = new ChildState();
        state.target = t;
        state.rectTransform = t as RectTransform;
        state.baseLocalScale = t.localScale;
        state.baseLocalRotation = t.localRotation;

        state.image = t.GetComponent<Image>();
        state.tmp = t.GetComponent<TMP_Text>();

        if (state.tmp != null)
        {
            state.originalText = state.tmp.text;
            state.lastObservedText = state.originalText;
            state.textAnimator = t.GetComponent<TMPNumberWaveAnimator>();
            if (state.textAnimator == null)
                state.textAnimator = t.gameObject.AddComponent<TMPNumberWaveAnimator>();
        }

        return state;
    }

    private void ProcessNewChild(ChildState state)
    {
        if (state == null || state.target == null)
            return;

        if (animateImages && state.image != null)
        {
            StartCoroutine(ImageSpawnJuiceRoutine(state));
            PlayImageSpawnSound();
        }

        if (animateTexts && state.tmp != null)
        {
            StartCoroutine(TypeTextRoutine(state));
        }
    }

    private void RefreshExistingState(ChildState state)
    {
        if (state == null || state.tmp == null)
            return;

        string current = state.tmp.text;
        if (current != state.lastObservedText && !state.isTyping)
        {
            state.originalText = current;
            state.lastObservedText = current;
            StartCoroutine(TypeTextRoutine(state));
        }
    }

    private IEnumerator ImageSpawnJuiceRoutine(ChildState state)
    {
        if (state == null || state.target == null)
            yield break;

        Transform t = state.target;
        Vector3 baseScale = state.baseLocalScale;
        Quaternion baseRot = state.baseLocalRotation;

        Quaternion fromRot = Quaternion.Euler(0f, 0f, imageRotationStartZ);
        Quaternion toRot = Quaternion.Euler(0f, 0f, imageRotationEndZ) * baseRot;

        Vector3 startScale = new Vector3(
            baseScale.x * imageStartScaleMultiplier.x,
            baseScale.y * imageStartScaleMultiplier.y,
            baseScale.z * imageStartScaleMultiplier.z
        );

        Vector3 overshootScale = new Vector3(
            baseScale.x * imageOvershootScaleMultiplier.x,
            baseScale.y * imageOvershootScaleMultiplier.y,
            baseScale.z * imageOvershootScaleMultiplier.z
        );

        t.localScale = startScale;
        t.localRotation = fromRot;

        float half = imagePunchDuration * 0.5f;
        float timer = 0f;

        while (timer < half)
        {
            if (t == null)
                yield break;

            timer += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(timer / Mathf.Max(0.001f, half));

            t.localScale = Vector3.LerpUnclamped(startScale, overshootScale, EaseOutBack(n));
            t.localRotation = Quaternion.LerpUnclamped(fromRot, toRot, n);
            yield return null;
        }

        timer = 0f;
        while (timer < half)
        {
            if (t == null)
                yield break;

            timer += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(timer / Mathf.Max(0.001f, half));

            t.localScale = Vector3.LerpUnclamped(overshootScale, baseScale, n);
            t.localRotation = Quaternion.LerpUnclamped(toRot, baseRot, n);
            yield return null;
        }

        if (t != null)
        {
            t.localScale = baseScale;
            t.localRotation = baseRot;
        }
    }

    private void PlayImageSpawnSound()
    {
        if (imageAudioSource == null || imageSpawnClip == null)
            return;

        imageAudioSource.pitch = currentImagePitch;
        imageAudioSource.PlayOneShot(imageSpawnClip, imageSpawnVolume);
        currentImagePitch = Mathf.Min(maxImagePitch, currentImagePitch + pitchIncreasePerImage);
    }

    private IEnumerator TypeTextRoutine(ChildState state)
    {
        if (state == null || state.tmp == null)
            yield break;

        state.isTyping = true;

        TMP_Text tmp = state.tmp;
        string fullText = state.originalText ?? string.Empty;

        // Lay out the full text once so TMP uses the correct width/scale.
        tmp.text = fullText;
        tmp.ForceMeshUpdate();

        int totalVisibleCharacters = tmp.textInfo.characterCount;
        tmp.maxVisibleCharacters = 0;

        if (animateNumericCharacters && state.textAnimator != null)
        {
            state.textAnimator.SetAnimationEnabled(true);
            state.textAnimator.SetParameters(numericWaveAmplitude, numericWaveSpeed, numericShakeStrength, numericShakeSpeed);
        }

        for (int i = 0; i <= totalVisibleCharacters; i++)
        {
            if (tmp == null)
                yield break;

            tmp.maxVisibleCharacters = i;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.001f, characterInterval));
        }

        tmp.maxVisibleCharacters = totalVisibleCharacters;
        state.lastObservedText = fullText;

        if (animateNumericCharacters && state.textAnimator != null)
        {
            state.textAnimator.SetAnimationEnabled(true);
            state.textAnimator.SetParameters(numericWaveAmplitude, numericWaveSpeed, numericShakeStrength, numericShakeSpeed);
        }

        state.isTyping = false;
    }

    private void UpdateNumericTMPJuice()
    {
        if (!animateNumericCharacters)
            return;

        foreach (var kv in trackedChildren)
        {
            ChildState state = kv.Value;
            if (state == null || state.textAnimator == null || state.tmp == null)
                continue;

            if (!state.tmp.gameObject.activeInHierarchy)
                continue;

            state.textAnimator.SetAnimationEnabled(true);
            state.textAnimator.SetParameters(numericWaveAmplitude, numericWaveSpeed, numericShakeStrength, numericShakeSpeed);
        }
    }

    private IEnumerator CameraShakeRoutine()
    {
        if (targetCamera == null)
            yield break;

        Transform camTransform = targetCamera.transform;
        Vector3 basePos = camTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < cameraShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 offset = Random.insideUnitCircle * cameraShakeStrength;
            camTransform.localPosition = basePos + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        camTransform.localPosition = basePos;
        cameraShakeRoutine = null;
    }

    private float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    [Serializable]
    private class ChildState
    {
        public Transform target;
        public RectTransform rectTransform;
        public Vector3 baseLocalScale;
        public Quaternion baseLocalRotation;

        public Image image;
        public TMP_Text tmp;

        public string originalText;
        public string lastObservedText;
        public bool isTyping;

        public TMPNumberWaveAnimator textAnimator;
    }
}

[RequireComponent(typeof(TMP_Text))]
public class TMPNumberWaveAnimator : MonoBehaviour
{
    private TMP_Text tmp;
    private Mesh mesh;
    private Vector3[][] cachedVertices;

    private bool animationEnabled = true;
    private float waveAmplitude = 3f;
    private float waveSpeed = 5f;
    private float shakeStrength = 0.65f;
    private float shakeSpeed = 22f;

    private void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    public void SetAnimationEnabled(bool enabled)
    {
        animationEnabled = enabled;
    }

    public void SetParameters(float amplitude, float wave, float shake, float shakeSpd)
    {
        waveAmplitude = amplitude;
        waveSpeed = wave;
        shakeStrength = shake;
        shakeSpeed = shakeSpd;
    }

    private void LateUpdate()
    {
        if (!animationEnabled || tmp == null || !tmp.gameObject.activeInHierarchy)
            return;

        AnimateNumericCharacters();
    }

    private void AnimateNumericCharacters()
    {
        tmp.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmp.textInfo;

        if (textInfo.characterCount == 0)
            return;

        if (cachedVertices == null || cachedVertices.Length != textInfo.meshInfo.Length)
        {
            cachedVertices = new Vector3[textInfo.meshInfo.Length][];
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Vector3[] src = textInfo.meshInfo[i].vertices;
            cachedVertices[i] = new Vector3[src.Length];
            src.CopyTo(cachedVertices[i], 0);
        }

        float time = Time.unscaledTime;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible)
                continue;

            char c = charInfo.character;
            if (!char.IsDigit(c))
                continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;
            Vector3[] sourceVertices = cachedVertices[materialIndex];

            Vector3 charMid = (sourceVertices[vertexIndex] + sourceVertices[vertexIndex + 2]) * 0.5f;

            float wave = Mathf.Sin((time * waveSpeed) + (i * 0.35f)) * waveAmplitude;
            float noiseX = Mathf.Sin((time * shakeSpeed) + (i * 1.13f)) * shakeStrength;
            float noiseY = Mathf.Cos((time * shakeSpeed * 0.87f) + (i * 0.91f)) * shakeStrength;

            Vector3 offset = new Vector3(noiseX, wave + noiseY, 0f);

            for (int j = 0; j < 4; j++)
            {
                destinationVertices[vertexIndex + j] = sourceVertices[vertexIndex + j] + offset;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            tmp.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    public void ResetMeshImmediate()
    {
        if (tmp == null)
            return;

        tmp.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmp.textInfo;

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Mesh m = textInfo.meshInfo[i].mesh;
            m.vertices = textInfo.meshInfo[i].vertices;
            tmp.UpdateGeometry(m, i);
        }
    }
}