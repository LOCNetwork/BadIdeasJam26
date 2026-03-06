using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{

    public static UIManager Instance { get; private set; }

    public SpriteRenderer SpriteRenderer;

    void Start()
    {
        Instance = this;
    }

    public void TransitionToSceneAnimation(string sceneName, double initialTime, double finalTime)
    {
        if (SpriteRenderer == null) return;


        // TODO

    }


    public void TransitionToSceneClean(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName);
    }




}
