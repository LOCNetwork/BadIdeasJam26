using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour 
{
    [SerializeField]
    private AudioMixer Mixer;
    [SerializeField]
    private GameObject muteIcon;
    [SerializeField]
    private Slider slider;


    public void Start()
    {
        float volume = PlayerPrefs.GetFloat("Volume", 1);

        slider.value = volume;

        OnChangeSlider(volume);

        
    }

    public void OnChangeSlider(float value)
    {
        if (Mixer != null)
        {
            Mixer.SetFloat("Volume", Mathf.Log10(value) * 20);
        }
     

        if (value <= 0.0001)
        {
            muteIcon.SetActive(true);
        } else
        {
            muteIcon.SetActive(false);
        }

        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();
    }

}
