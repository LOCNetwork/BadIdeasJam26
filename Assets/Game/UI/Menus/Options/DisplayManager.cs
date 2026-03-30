using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class DisplayManager : MonoBehaviour 
{
    [SerializeField]
    private TMP_Dropdown resolutionsDropDown;
    [SerializeField]
    private TMP_Dropdown fullscreenDropDown;

    private Resolution[] resolutions;
    private FullScreenMode[] fullScreenModes;


    public void Start()
    {
        resolutionsDropDown.ClearOptions();
        fullscreenDropDown.ClearOptions();

        AddResolutions();
        AddScreenTypes();

        ChangeResolution(PlayerPrefs.GetInt("Resolution"));
    }


    private void AddResolutions()
    {
        resolutions = Screen.resolutions;

        List<string> options = new List<string>();

        int currentResolution = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            options.Add(resolutions[i].ToString());

            if (Screen.currentResolution.Equals(resolutions[i]))
            {
                currentResolution = i;
            }

        }

        resolutionsDropDown.AddOptions(options);

        if (!PlayerPrefs.HasKey("Resolution"))
        {
            PlayerPrefs.SetInt("Resolution", currentResolution);
            PlayerPrefs.Save();
        }

        resolutionsDropDown.SetValueWithoutNotify(PlayerPrefs.GetInt("Resolution", currentResolution));
        resolutionsDropDown.RefreshShownValue();
    }

    private void AddScreenTypes()
    {
        List<string> options = new List<string>();
    
        fullScreenModes = new FullScreenMode[3];

        options.Add("Full Screen");
        options.Add("Exclusive Full Screen");
        options.Add("Windowed");

        fullScreenModes[0] = FullScreenMode.FullScreenWindow;
        fullScreenModes[1] = FullScreenMode.ExclusiveFullScreen;
        fullScreenModes[2] = FullScreenMode.Windowed;

        if (!PlayerPrefs.HasKey("ScreenType"))
        {
            PlayerPrefs.SetInt("ScreenType", 0);
            PlayerPrefs.Save();
        }

        fullscreenDropDown.AddOptions(options);

        fullscreenDropDown.SetValueWithoutNotify(PlayerPrefs.GetInt("ScreenType", 0));
        fullscreenDropDown.RefreshShownValue();

    }



    public void ChangeResolution(int index)
    {
        PlayerPrefs.SetInt("Resolution", index);

        Resolution resolucion = resolutions[index];

        FullScreenMode fsm = fullScreenModes[PlayerPrefs.GetInt("ScreenType")];

        Screen.SetResolution(resolucion.width, resolucion.height, fsm);
    }

    public void ChangeScreenType(int index)
    {
        PlayerPrefs.SetInt("ScreenType", index);

        Resolution resolucion = resolutions[PlayerPrefs.GetInt("Resolution")];

        FullScreenMode fsm = fullScreenModes[index];

        Screen.SetResolution(resolucion.width, resolucion.height, fsm);
    }

}
