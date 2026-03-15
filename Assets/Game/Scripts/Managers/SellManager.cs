using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class SellManager
{

    public Dictionary<Box, float> boxesOnSale;
    public Queue<Box> boxesQueue;

    public float[] sellSpeedArray;

    private RectTransform container;

    // UI Font
    private TMP_FontAsset fontAsset;

    public SellManager(RectTransform container, TMP_FontAsset fontAsset)
    {
        boxesOnSale = new Dictionary<Box, float>();
        boxesQueue = new Queue<Box>();

        sellSpeedArray = new float[4];

        sellSpeedArray[0] = 250f; // VERY SLOW
        sellSpeedArray[1] = 150f; // SLOW
        sellSpeedArray[2] = 90f; // FAST
        sellSpeedArray[3] = 20f; // VERY FAST

        this.container = container;
        this.fontAsset = fontAsset;
    }



    public bool PutBoxToSell(Box box)
    {
        if (box.Type == BoxType.Delivery) return false;

        boxesOnSale.Add(box, GameManager.instance.timer);
        boxesQueue.Enqueue(box);
        
        return true;
    }

    public void CompleteBoxSale(Box box)
    {
        boxesOnSale.Remove(box);
        
       
        PerformBoxSellAnimation(box);


        GameManager.instance.gameStats.money += box.value;
    }
    
    
    IEnumerator PerformBoxSellAnimation(Box box)
    {

        foreach (WorldItem item in box.playerItemPool)
        {
            foreach (Passive passive in item.passives)
            {
                DisplayItemPassive(passive.Display(box, item.passivesInfo));
                yield return new WaitForSeconds(2f);
            }

        }



    }
    


    public void DisplayItemPassive(string displayText)
    {

        for (int i = container.childCount - 1; i >= 0; --i)
        {
            Object.Destroy(container.GetChild(i).gameObject);
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
