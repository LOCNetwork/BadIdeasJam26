using System.Collections;
using UnityEngine;

public class TriggerESCMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject ESCMenu;

    private bool isEnabled = false;
    private bool inAnimation = false;


    void Start()
    {

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {

         if (!inAnimation) 
         { 

            if (!isEnabled)
            {
                StartCoroutine(StartIntroAnimation());

            } else
            {

                StartCoroutine(StartOutroAnimation());

            }
         }



        }
    }


    public void StartOutroExternal()
    {
        StartCoroutine(StartOutroAnimation());
    }


    private IEnumerator StartIntroAnimation()
    {
        
        Time.timeScale = 0.0f;

        ESCMenu.SetActive(true);
        UIManager.Instance.TransitionForced(true);
        inAnimation = true;

        yield return new WaitForSecondsRealtime(0.7f);

        inAnimation = false;

        isEnabled = true;
    }

    private IEnumerator StartOutroAnimation()
    {
        UIManager.Instance.TransitionForced(false);
        inAnimation = true;

        yield return new WaitForSecondsRealtime(0.7f);

        inAnimation = false;
        isEnabled = false;
        ESCMenu.SetActive(false);

        Time.timeScale = 1.0f;
    }
}
