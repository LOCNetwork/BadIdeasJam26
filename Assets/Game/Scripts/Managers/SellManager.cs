using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;


[System.Serializable]
public class SellManager
{

    public Dictionary<Guid, KeyValuePair<Box, float>> boxesOnSale;
    public Queue<GameObject> boxesQueue;

    public int[] sellSpeedArray;

    private RectTransform container;

    // UI Font
    private TMP_FontAsset fontAsset;

    private float offset;
    private float truckModuleOffset;

    private Stack<GameObject> truckBodyModules;

    private MonoBehaviour mono;

    public SellManager(RectTransform container, TMP_FontAsset fontAsset, MonoBehaviour mono)
    {
        boxesOnSale = new Dictionary<Guid, KeyValuePair<Box, float>>();
        boxesQueue = new Queue<GameObject>();

        sellSpeedArray = new int[4];


        sellSpeedArray[0] = 8; // VERY SLOW
        sellSpeedArray[1] = 5; // SLOW
        sellSpeedArray[2] = 3; // FAST
        sellSpeedArray[3] = 1; // VERY FAST

        this.container = container;
        this.fontAsset = fontAsset;

        this.offset = 0;
        this.truckModuleOffset = 0;

        this.mono = mono;

        this.truckBodyModules = new Stack<GameObject>();
    }

    public int GetSellTimeByIndex(int index)
    {
        if (index < 0 || index >= sellSpeedArray.Length) 
        {
            return 1;
        }

        return sellSpeedArray[index];
    }



    public bool PutBoxToSell(GameObject go, Box box)
    {
        if (box.Type == BoxType.Delivery) return false;

        boxesOnSale.Add(box.guid, new KeyValuePair<Box, float>(box, GameManager.instance.timer));

        // Remove physics components
        UnityEngine.Object.Destroy(go.GetComponent<Rigidbody2D>());
        UnityEngine.Object.Destroy(go.GetComponent<BoxCollider2D>());

        go.transform.SetParent(GameManager.instance.truckObject.transform);

        go.transform.localPosition = new Vector3(0, offset - 1);

        go.GetComponent<Box>().guid = box.guid;

        boxesQueue.Enqueue(go);

        UpdateTruck();

        offset -= 1.3f;
        
        return true;
    }

    public void CompleteBoxSale(Box box)
    {
        Debug.Log("Selling box with value: " + box.value);
        boxesOnSale.Remove(box.guid);
        
        Debug.Log("Performing sell animation");
        mono.StartCoroutine(PerformBoxSellAnimation(box));


      
    }
    
    
    IEnumerator PerformBoxSellAnimation(Box box)
    {

        foreach (WorldItem item in box.playerItemPool)
        {

            // Add to items sold stats
            GameManager.instance.gameStats.itemsSold.TryGetValue(item.itemID, out int sells);
            GameManager.instance.gameStats.itemsSold[item.itemID] = sells + 1;
            //

            bool activatesPassives = false;

            if (item.passives != null)
            {

                foreach (Passive passive in item.passives)
                {
                    if (passive.MeetsCondition(item, box, item.passivesInfo))
                    {
                        activatesPassives = true;
                        container.gameObject.SetActive(true);

                        string passiveDisplay = passive.Display(item, box, item.passivesInfo);

                        if (passiveDisplay.Equals(string.Empty)) continue;

                        DisplayInfo(passiveDisplay);
                        passive.ExecutePassive(item, box, item.passivesInfo);

                        yield return new WaitForSeconds(2f);
                    }

                }
            }

            if (!activatesPassives)
            {
                container.gameObject.SetActive(true);

                string display = $"[20:{item.itemID}:40] <color=green>+{item.value} $</color>";

                DisplayInfo(display);

                yield return new WaitForSeconds(2f);
            }

        }


        for (int i = container.childCount - 1; i >= 0; --i)
        {
            UnityEngine.Object.Destroy(container.GetChild(i).gameObject);
        }

        container.gameObject.SetActive(false);

        UnityEngine.Object.Destroy(boxesQueue.Dequeue());

        offset += 1.3f;

        UpdateBoxPositions();
        UpdateTruck();


        Debug.Log("Adding money: " + box.value);
        GameManager.instance.gameStats.money += box.value;
    }

    private void UpdateBoxPositions()
    { 
        float currentOffset = 0;

        foreach (GameObject box in boxesQueue)
        {
            Vector3 targetPosition = new Vector3(0, currentOffset - 1, 0);
            box.transform.localPosition = targetPosition;
            currentOffset -= 1.3f;
        }
    }


    private void UpdateTruck()
    {
        if (boxesQueue.Count < truckBodyModules.Count)
        {
            truckModuleOffset += 1.31f;
            GameObject module = truckBodyModules.Pop();
            UnityEngine.Object.Destroy(module);
        } else
        {
            truckModuleOffset -= 1.31f;

            GameObject module = new GameObject("TruckBodyModule");
            module.transform.SetParent(GameManager.instance.truckObject.transform);

            module.AddComponent<SpriteRenderer>().sprite = GameManager.instance.truckBodySprite;

            module.transform.localPosition = new Vector3(0, truckModuleOffset, 0);

            truckBodyModules.Push(module);
        }

        // Update truck back position
        if (truckBodyModules.Count > 0)
        {
            GameObject lastModule = truckBodyModules.Peek();

            GameManager.instance.truckBack.transform.localPosition = new Vector3(0, lastModule.transform.localPosition.y - 1.3f, 0);
        } else
        {
            GameManager.instance.truckBack.transform.localPosition = new Vector3(0, -1.3f, 0);
        }


    }



    public void DisplayInfo(string displayText)
    {

        for (int i = container.childCount - 1; i >= 0; --i)
        {
            UnityEngine.Object.Destroy(container.GetChild(i).gameObject);
        }


        string[] words = displayText.Split(' ');

        GameObject lastObject = null;
        float spacing = 0f;
        
        foreach(string word in words)
        {

            if (word.Contains("["))
            {


                GameObject go = new GameObject("Icon");
                go.transform.SetParent(container);

                string sanitizedWord = word.Replace("[", "").Replace("]", "");
                string[] info = sanitizedWord.Split(':');

                string separation1 = info[0];
                string itemID = info[1];
                string separation2 = info[2];

               

                Sprite itemSprite = GameManager.instance.GetItemByID(itemID).displaySprite;


                    
                Canvas canvas = go.AddComponent<Canvas>();

                UnityEngine.UI.Image image = canvas.AddComponent<UnityEngine.UI.Image>();
                image.transform.SetParent(canvas.transform);

                image.sprite = itemSprite;
                image.preserveAspect = true;
                RectTransform rt = image.GetComponent<RectTransform>();
                rt.pivot = new Vector2(0f, 0f);
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);

                rt.sizeDelta = new Vector2(64, 64);

                

                if (lastObject != null && lastObject.name.Contains("Icon"))
                {
                    go.transform.localPosition = new Vector3(lastObject.transform.localPosition.x + float.Parse(separation1) + spacing, 41, 0);
                } else if (lastObject != null && lastObject.name.Contains("Text"))
                {
                    RectTransform rtText = lastObject.GetComponent<TextMeshProUGUI>().rectTransform;
 
                    float width = lastObject.GetComponent<TextMeshProUGUI>().GetPreferredValues(lastObject.GetComponent<TextMeshProUGUI>().text).x;

                    go.transform.localPosition = new Vector3(rtText.localScale.x * width + lastObject.transform.localPosition.x + float.Parse(separation1), 41, 0);
                } else
                {
                    go.transform.localPosition = new Vector3(float.Parse(separation1), 41, 0);
                }



                lastObject = go;

                spacing = float.Parse(separation2);

               
                continue;
            }

            GameObject textObject;


            if (lastObject != null && lastObject.name.Contains("Icon"))
            {
                textObject = new GameObject("Text");
                textObject.transform.SetParent(container);

                textObject.AddComponent<TextMeshProUGUI>();

                RectTransform rt = textObject.GetComponent<TextMeshProUGUI>().rectTransform;
                rt.pivot = new Vector2(0f, 0f);
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0.6f);

                rt.localPosition = new Vector3(lastObject.transform.localPosition.x + spacing, 0, 0);

                lastObject = textObject;
                spacing = 0f;
            } else if (lastObject != null && lastObject.name.Contains("Text"))
            {
                textObject = lastObject;
            } else 
            {
                textObject = new GameObject("Text");
                textObject.transform.SetParent(container);

                textObject.AddComponent<TextMeshProUGUI>();

                RectTransform rt = textObject.GetComponent<RectTransform>();
                rt.pivot = new Vector2(0f, 0f);
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0.6f);

                rt.localPosition = new Vector3(0, 0, 0);

                Debug.Log(rt.localPosition);

                lastObject = textObject;
                spacing = 0f;
            }


            TextMeshProUGUI textMesh = textObject.GetComponent<TextMeshProUGUI>();
            textMesh.autoSizeTextContainer = true;
            textMesh.alignment = TextAlignmentOptions.Left;
            textMesh.text = textMesh.text + " " + word;
            textMesh.font = fontAsset;
            textMesh.fontSize = 75;
            textMesh.color = Color.white;
        }



    }
    
}
