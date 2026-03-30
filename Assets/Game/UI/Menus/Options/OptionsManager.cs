using UnityEngine;

public class OptionsManager : MonoBehaviour
{

    [SerializeField]
    private GameObject soundOptions;
    [SerializeField]
    private GameObject displayOptions;


    public void EnableDisplayOptions()
    {
        displayOptions.SetActive(true);
        soundOptions.SetActive(false);
    }

    public void EnableSoundOptions()
    {
        soundOptions.SetActive(true);
        displayOptions.SetActive(false);
    }
}
