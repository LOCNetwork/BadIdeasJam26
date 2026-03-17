using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class SellManager
{

    public Dictionary<Guid, KeyValuePair<Box, float>> boxesOnSale;
    public Queue<GameObject> boxesQueue;

    public float[] sellSpeedArray;

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

        sellSpeedArray = new float[4];

        sellSpeedArray[0] = 250f; // VERY SLOW
        sellSpeedArray[1] = 150f; // SLOW
        sellSpeedArray[2] = 90f; // FAST
        sellSpeedArray[3] = 20f; // VERY FAST

        this.container = container;
        this.fontAsset = fontAsset;

        this.offset = 0;
        this.truckModuleOffset = 0;

        this.mono = mono;

        this.truckBodyModules = new Stack<GameObject>();
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

            if (item.passives == null) continue;

            foreach (Passive passive in item.passives)
            {
                if (passive.MeetsCondition(box, item.passivesInfo))
                {
                   DisplayItemPassive(passive.Display(box, item.passivesInfo));
                   yield return new WaitForSeconds(2f);
                }
                    
            }

        }


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



    public void DisplayItemPassive(string displayText)
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

                if (lastObject != null && lastObject.name.Contains("Text"))
                {
                    float width = lastObject.GetComponent<TextMeshProUGUI>().GetPreferredValues(lastObject.GetComponent<TextMeshProUGUI>().text).x;
                    lastObject.transform.localPosition = lastObject.transform.localPosition + new Vector3(width / 2f, 0, 0);
                }


                GameObject go = new GameObject("Icon");
                go.transform.SetParent(container);

                string sanitizedWord = word.Replace("[", "").Replace("]", "");
                string[] info = sanitizedWord.Split(':');

                string separation1 = info[0];
                string itemID = info[1];
                string separation2 = info[2];


                Sprite itemSprite = GameManager.instance.GetItemByID(itemID).displaySprite;

                if (lastObject != null)
                {
                    go.transform.localPosition = lastObject.transform.localPosition + new Vector3(float.Parse(separation1) + spacing, 0, 0);
                } else
                {
                    go.transform.localPosition = new Vector3(-container.rect.width / 2.1f, 0, 0);
                }

                if (lastObject != null && lastObject.name.Contains("Text"))
                {
                    float width = lastObject.GetComponent<TextMeshProUGUI>().GetPreferredValues(lastObject.GetComponent<TextMeshProUGUI>().text).x;
                    go.transform.localPosition = go.transform.localPosition + new Vector3(width / 2f, 0, 0);
                }


                lastObject = go;
                    
                Canvas canvas = go.AddComponent<Canvas>();

                Image image = canvas.AddComponent<Image>();
                image.transform.SetParent(canvas.transform);

                image.sprite = itemSprite;
                image.preserveAspect = true;
                RectTransform rt = image.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(64, 64);

                spacing = float.Parse(separation2);

               
                continue;
            }

            GameObject textObject;


            if (lastObject != null && lastObject.name.Contains("Icon"))
            {
               textObject = new GameObject("Text");
               textObject.transform.SetParent(container);
               textObject.transform.localPosition = lastObject.transform.localPosition + new Vector3(spacing, 0, 0);
               lastObject = textObject;
               spacing = 0f;
               textObject.AddComponent<TextMeshProUGUI>();
            } else if (lastObject != null && lastObject.name.Contains("Text"))
            {
                textObject = lastObject;
            } else 
            {
                textObject = new GameObject("Text");
                textObject.transform.SetParent(container);
                textObject.transform.localPosition = new Vector3(-container.rect.width / 2.1f, 0, 0);
                lastObject = textObject;
                spacing = 0f;
                textObject.AddComponent<TextMeshProUGUI>();
            }


            TextMeshProUGUI textMesh = textObject.GetComponent<TextMeshProUGUI>();
            textMesh.autoSizeTextContainer = true;
            textMesh.alignment = TextAlignmentOptions.Left;
            textMesh.text = textMesh.text + " " + word;
            textMesh.font = fontAsset;
            textMesh.fontSize = 75;
            textMesh.color = Color.white;
        }


        if (lastObject != null && lastObject.name.Contains("Text"))
        {
            float width = lastObject.GetComponent<TextMeshProUGUI>().GetPreferredValues(lastObject.GetComponent<TextMeshProUGUI>().text).x;
            
            lastObject.transform.localPosition = lastObject.transform.localPosition + new Vector3(width / 2f, 0, 0);
        }


    }
    
}
