using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject soundOptions;
    [SerializeField] private GameObject displayOptions;

    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private GameObject mainMenu;

    [SerializeField] private RectTransform UpMainMenu;
    [SerializeField] private RectTransform BottomMainMenu;

    [SerializeField] private RectTransform RightOptionsMenu;
    [SerializeField] private RectTransform LeftOptionsMenu;


    [SerializeField] private RectTransform TargetRightOptionsMenu;
    [SerializeField] private RectTransform TargetLeftOptionsMenu;

    [SerializeField] private RectTransform TargetRightOptionsMenu2;
    [SerializeField] private RectTransform TargetLeftOptionsMenu2;

    [SerializeField] private RectTransform TargetUpMainMenu;
    [SerializeField] private RectTransform TargetBottomMainMenu;

    [SerializeField] private Image fadeImage;


    private string currentPosition;

    void Start()
    {
        Instance = this;

        if (SceneManager.GetActiveScene() != null && SceneManager.GetActiveScene().name.Equals("Main Menu"))
        {
            StartCoroutine(FadeInCoroutine(fadeImage, 40f));
        }

        currentPosition = "MainMenu";
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void TransitionToSceneAnimation(string sceneName)
    {

        StartCoroutine(SlidePanelsFromCenterMenuAndLoad(sceneName, 0.5f));
        StartCoroutine(FadeOutCoroutine(fadeImage, 40f));

    }

    public void TransitionToSceneEndingAnimation(string sceneName, Image fadeImg)
    {
        StartCoroutine(TransitionToEndScene(sceneName, fadeImg));
    }

    public void TransitionToSceneEndingAnimation(string sceneName)
    {
        StartCoroutine(TransitionToEndScene(sceneName, fadeImage));
    }

    private IEnumerator TransitionToEndScene(string sceneName, Image image)
    {
        StartCoroutine(FadeOutCoroutine(image, 40f));

        yield return new WaitForSecondsRealtime(1.9f);

        SceneManager.LoadScene(sceneName);
    }



    public void TransitionForced(bool inout)
    {

        if (!inout)
        {
            StartCoroutine(SlidePanelsToCenterOptionsOutroAndLoad(0.5f));
        } else
        {
            StartCoroutine(SlidePanelsToCenterOptionsIntroAndLoad(0.5f));
        }


    }

    public void TransitionToMicroSceneAnimation(string sceneName)
    {

        if (currentPosition.Equals("OptionsMenu") && sceneName.Equals("MainMenu"))
        {
            StartCoroutine(SlidePanelsToCenterOptionsOutroAndLoad(0.5f));
        } else
        {
            StartCoroutine(SlidePanelsToCenterOptionsIntroAndLoad(0.5f));
        }


    }


    public void TransitionToSceneClean(string sceneName)    
    {
        SceneManager.LoadSceneAsync(sceneName);
    }



    // Y MOVEMENT
    private IEnumerator SlidePanelsFromCenterMenuAndLoad(string sceneName, float duration)
    {
        mainMenu.SetActive(false);
        float elapsed = 0f;

        Vector2 upInitial = UpMainMenu.localPosition;
        Vector2 downInitial = BottomMainMenu.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            UpMainMenu.localPosition = Vector2.Lerp(upInitial, TargetUpMainMenu.localPosition, smoothT);
            BottomMainMenu.localPosition = Vector2.Lerp(downInitial, TargetBottomMainMenu.localPosition, smoothT);

            yield return null;
        }


        SceneManager.LoadScene(sceneName);
    }


    // X MOVEMENT
    public IEnumerator SlidePanelsToCenterOptionsIntroAndLoad(float duration)
    {
        float elapsed = 0f;

        Vector2 rightInitial = RightOptionsMenu.localPosition;
        Vector2 leftInitial = LeftOptionsMenu.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            RightOptionsMenu.localPosition = Vector2.Lerp(rightInitial, TargetRightOptionsMenu.localPosition, smoothT);
            LeftOptionsMenu.localPosition = Vector2.Lerp(leftInitial, TargetLeftOptionsMenu.localPosition, smoothT);

            yield return null;
        }

        if (mainMenu != null) mainMenu.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(true);

        currentPosition = "OptionsMenu";
   
    }

    public IEnumerator SlidePanelsToCenterOptionsOutroAndLoad(float duration)
    {
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);

        displayOptions.SetActive(false);
        soundOptions.SetActive(false);

        float elapsed = 0f;

        Vector2 rightInitial = RightOptionsMenu.localPosition;
        Vector2 leftInitial = LeftOptionsMenu.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            RightOptionsMenu.localPosition = Vector2.Lerp(rightInitial, TargetRightOptionsMenu2.localPosition, smoothT);
            LeftOptionsMenu.localPosition = Vector2.Lerp(leftInitial, TargetLeftOptionsMenu2.localPosition, smoothT);

            yield return null;
        }

      

        currentPosition = "MainMenu";

    }

    // FADES

    public static IEnumerator FadeInCoroutine(Image image, float duration)
    {
        Color startColor = new Color(image.color.r, image.color.g, image.color.b, 1);
        Color targetColor = new Color(image.color.r, image.color.g, image.color.b, 0);

        yield return FadeCoroutine(image, startColor, targetColor, duration);
    }

    public static IEnumerator FadeOutCoroutine(Image image, float duration)
    {
        Color startColor = new Color(image.color.r, image.color.g, image.color.b, 0);
        Color targetColor = new Color(image.color.r, image.color.g, image.color.b, 1);

        yield return FadeCoroutine(image, startColor, targetColor, duration);
    }

    private static IEnumerator FadeCoroutine(Image image, Color startColor, Color targetColor, float duration) 
    {
        float elapsedTime = 0;
        float elapsedPercentage = 0;

        while (elapsedPercentage < 1)
        {
            elapsedPercentage += elapsedTime / duration;

            image.color = Color.Lerp(startColor, targetColor, elapsedPercentage);

            yield return null;
            elapsedTime += Time.unscaledDeltaTime;
        }

    }

}
