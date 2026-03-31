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
        Resolution[] allResolutions = Screen.resolutions;

        Resolution[] resolutionsArray;

        List<string> options = new List<string>();
        
        int currentResolution = 0;

        int size = 0;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            if (Screen.currentResolution.Equals(allResolutions[i]))
            {
                currentResolution = i;
            }

            if (((double) allResolutions[i].width / allResolutions[i].height) == (1920.0 / 1080.0))
            {
                size++;
            }

        }

        resolutionsArray = new Resolution[size];

        int index = 0;
        for (int i = 0; i < allResolutions.Length; i++)
        {

            if (((double) allResolutions[i].width / allResolutions[i].height) == (1920.0 / 1080.0))
            {
                options.Add(allResolutions[i].ToString());

                resolutionsArray[index++] = allResolutions[i];
            }

        }

        resolutions = resolutionsArray;

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

        if (index >= resolutions.Length) return;


        PlayerPrefs.SetInt("Resolution", index);

        Resolution resolucion = resolutions[index];

        FullScreenMode fsm = fullScreenModes[PlayerPrefs.GetInt("ScreenType")];

        Screen.SetResolution(resolucion.width, resolucion.height, fsm);
    }

    public void ChangeScreenType(int index)
    {
        if (index >= fullScreenModes.Length) return;

        PlayerPrefs.SetInt("ScreenType", index);

        Resolution resolucion = resolutions[PlayerPrefs.GetInt("Resolution")];

        FullScreenMode fsm = fullScreenModes[index];

        Screen.SetResolution(resolucion.width, resolucion.height, fsm);
    }

}
