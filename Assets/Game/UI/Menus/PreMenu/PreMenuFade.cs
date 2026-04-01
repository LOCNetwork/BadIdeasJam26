using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class PreMenuFade : MonoBehaviour
{

    [SerializeField] private VideoPlayer player;
    [SerializeField] private Image fadeImg;


    public void Start()
    {
        player.Stop();
        RunAnimationCoroutine();
    }

    public void RunAnimationCoroutine()
    {
        StartCoroutine(Run());
    }


    private IEnumerator Run()
    {
        StartCoroutine(FadeInCoroutine(fadeImg, 40f));

        yield return new WaitForSeconds(2f);

        player.Play();

        yield return new WaitForSeconds(5f);

        StartCoroutine(FadeOutCoroutine(fadeImg, 40f));

        yield return new WaitForSeconds(2.1f);

        SceneManager.LoadScene("Main Menu");
    }


    private IEnumerator FadeOutCoroutine(Image image, float duration)
    {
        Color startColor = new Color(image.color.r, image.color.g, image.color.b, 0);
        Color targetColor = new Color(image.color.r, image.color.g, image.color.b, 1);

        yield return FadeCoroutine(image, startColor, targetColor, duration);
    }

    private IEnumerator FadeInCoroutine(Image image, float duration)
    {
        Color startColor = new Color(image.color.r, image.color.g, image.color.b, 1);
        Color targetColor = new Color(image.color.r, image.color.g, image.color.b, 0);

        yield return FadeCoroutine(image, startColor, targetColor, duration);
    }

    private IEnumerator FadeCoroutine(Image image, Color startColor, Color targetColor, float duration)
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
