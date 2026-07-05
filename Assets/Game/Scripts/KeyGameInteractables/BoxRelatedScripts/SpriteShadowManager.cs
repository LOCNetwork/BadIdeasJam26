using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoxShadowSpritePair
{
    [Header("Box Sprite")]
    public Sprite sourceSprite;

    [Header("Shadow Sprite")]
    public Sprite shadowSprite;
}

public class SpriteShadowManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;

    [Header("Shadow Prefabs")]
    [SerializeField] private GameObject boxShadowPrefab;
    [SerializeField] private GameObject itemShadowPrefab;

    [Header("Shadow Local Offset")]
    [SerializeField] private Vector3 boxShadowLocalOffset = new Vector3(0f, -0.12f, 0f);
    [SerializeField] private Vector3 itemShadowLocalOffset = new Vector3(0f, -0.08f, 0f);

    [Header("Scan")]
    [SerializeField] private float scanInterval = 0.35f;

    [Header("Box Shadow Sprite Mapping")]
    [SerializeField] private List<BoxShadowSpritePair> boxShadowSpritePairs = new List<BoxShadowSpritePair>();

    private readonly Dictionary<Transform, ShadowRuntimeData> trackedObjects = new Dictionary<Transform, ShadowRuntimeData>();
    private float scanTimer = 0f;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();
    }

    private void Start()
    {
        ScanSceneAndRegister();
    }

    private void Update()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            ScanSceneAndRegister();
            CleanupMissingTargets();
        }

        UpdateTrackedShadows();
    }

    private void ScanSceneAndRegister()
    {
        Box[] boxes = FindObjectsByType<Box>(FindObjectsSortMode.None);
        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] == null)
                continue;

            RegisterBox(boxes[i]);
        }

        WorldItemComponent[] items = FindObjectsByType<WorldItemComponent>(FindObjectsSortMode.None);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                continue;

            RegisterItem(items[i]);
        }
    }

    private void RegisterBox(Box box)
    {
        if (box == null)
            return;

        Transform target = box.transform;
        if (trackedObjects.ContainsKey(target))
            return;

        if (boxShadowPrefab == null)
            return;

        GameObject shadowInstance = Instantiate(boxShadowPrefab, target);
        shadowInstance.name = $"{target.name}_Shadow";

        shadowInstance.transform.localPosition = boxShadowLocalOffset;
        shadowInstance.transform.localRotation = Quaternion.identity;
        shadowInstance.transform.localScale = Vector3.one;

        SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
        SpriteRenderer shadowRenderer = shadowInstance.GetComponent<SpriteRenderer>();

        ShadowRuntimeData data = new ShadowRuntimeData
        {
            target = target,
            box = box,
            item = null,
            shadowObject = shadowInstance,
            targetRenderer = targetRenderer,
            shadowRenderer = shadowRenderer,
            isBox = true,
            lastBoxSprite = null
        };

        trackedObjects.Add(target, data);
        UpdateSingleShadow(data);
    }

    private void RegisterItem(WorldItemComponent item)
    {
        if (item == null)
            return;

        Transform target = item.transform;
        if (trackedObjects.ContainsKey(target))
            return;

        if (itemShadowPrefab == null)
            return;

        GameObject shadowInstance = Instantiate(itemShadowPrefab, target);
        shadowInstance.name = $"{target.name}_Shadow";

        shadowInstance.transform.localPosition = itemShadowLocalOffset;
        shadowInstance.transform.localRotation = Quaternion.identity;
        shadowInstance.transform.localScale = Vector3.one;

        SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
        SpriteRenderer shadowRenderer = shadowInstance.GetComponent<SpriteRenderer>();

        ShadowRuntimeData data = new ShadowRuntimeData
        {
            target = target,
            box = null,
            item = item,
            shadowObject = shadowInstance,
            targetRenderer = targetRenderer,
            shadowRenderer = shadowRenderer,
            isBox = false,
            lastBoxSprite = null
        };

        trackedObjects.Add(target, data);
        UpdateSingleShadow(data);
    }

    private void CleanupMissingTargets()
    {
        List<Transform> toRemove = new List<Transform>();

        foreach (KeyValuePair<Transform, ShadowRuntimeData> kvp in trackedObjects)
        {
            if (kvp.Key == null || kvp.Value == null || kvp.Value.target == null)
                toRemove.Add(kvp.Key);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            trackedObjects.Remove(toRemove[i]);
        }
    }

    private void UpdateTrackedShadows()
    {
        foreach (KeyValuePair<Transform, ShadowRuntimeData> kvp in trackedObjects)
        {
            ShadowRuntimeData data = kvp.Value;
            if (data == null || data.target == null)
                continue;

            UpdateSingleShadow(data);
        }
    }

    private void UpdateSingleShadow(ShadowRuntimeData data)
    {
        if (data.shadowObject == null || data.target == null)
            return;

        bool targetActive = data.target.gameObject.activeInHierarchy;
        bool heldByPlayer = IsHeldByPlayer(data.target);

        bool shouldShadowBeVisible = targetActive && !heldByPlayer;
        if (data.shadowObject.activeSelf != shouldShadowBeVisible)
            data.shadowObject.SetActive(shouldShadowBeVisible);

        if (!shouldShadowBeVisible)
            return;

        if (data.isBox)
        {
            UpdateBoxShadowSprite(data);

            if (data.shadowObject.transform.localPosition != boxShadowLocalOffset)
                data.shadowObject.transform.localPosition = boxShadowLocalOffset;
        }
        else
        {
            if (data.shadowObject.transform.localPosition != itemShadowLocalOffset)
                data.shadowObject.transform.localPosition = itemShadowLocalOffset;
        }
    }

    private void UpdateBoxShadowSprite(ShadowRuntimeData data)
    {
        if (data.targetRenderer == null || data.shadowRenderer == null)
            return;

        Sprite currentBoxSprite = data.targetRenderer.sprite;
        if (currentBoxSprite == null)
            return;

        if (data.lastBoxSprite == currentBoxSprite)
            return;

        data.lastBoxSprite = currentBoxSprite;

        Sprite shadowSprite = GetShadowSpriteForBoxSprite(currentBoxSprite);
        if (shadowSprite != null)
            data.shadowRenderer.sprite = shadowSprite;
    }

    private Sprite GetShadowSpriteForBoxSprite(Sprite source)
    {
        if (source == null)
            return null;

        for (int i = 0; i < boxShadowSpritePairs.Count; i++)
        {
            if (boxShadowSpritePairs[i] == null)
                continue;

            if (boxShadowSpritePairs[i].sourceSprite == source)
                return boxShadowSpritePairs[i].shadowSprite;
        }

        return null;
    }

    private bool IsHeldByPlayer(Transform target)
    {
        if (target == null || player == null)
            return false;

        Transform holdTarget = player.HoldTarget;
        if (holdTarget == null)
            return false;

        return target.parent == holdTarget || target.IsChildOf(holdTarget);
    }

    private class ShadowRuntimeData
    {
        public Transform target;
        public Box box;
        public WorldItemComponent item;

        public GameObject shadowObject;

        public SpriteRenderer targetRenderer;
        public SpriteRenderer shadowRenderer;

        public bool isBox;
        public Sprite lastBoxSprite;
    }
}