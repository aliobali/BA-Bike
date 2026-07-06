using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;

public class LevelAndConnectionSettings : MonoBehaviour
{
    public GameObject SumoNetworkFileDialog;
    public GameObject BSelectSumoScenario;
    public GameObject BRecordPerformance;
    public GameObject SelectedVehicleTypeBtn;
    public GameObject EgoVehicleNameEdit;
    public GameObject EVIIPEdit;
    public GameObject EVIPortEdit;
    public GameObject BConnectToEVI;
    public GameObject SeedEdit;
    public GameObject StreetLightsCheckbox;
    private string SelectedSumoNetFile = @"Extern/Scenarios/berlin/intersection1_bigger_no_priority.net";
    private bool ConnectEVIOnLaunch = false;
    private bool SkipMenu = false;

    // Methods
    public string GetSelectedVehicleType()
    {
        return GameStatics.VehicleTypes.Keys[
            //SelectedVehicleTypeBtn.GetSelectedId()
            0
        ];
    }

    public string GetSelectedSumoNetFile()
    {
        return SelectedSumoNetFile;
    }

    public int GetSeed()
    {
        string seedText = SeedEdit.GetComponent<UnityEngine.UI.InputField>().text;
        return Int32.Parse(seedText);
    }

    public bool GetStreetLightsChecked()
    {
        return StreetLightsCheckbox.GetComponent<UnityEngine.UI.Toggle>().isOn;
    }

    public bool GetConnectToEVIOnLaunch()
    {
        return true;
    }

    public bool GetSkipMenu()
    {
        return SkipMenu;
    }

    public string GetEVIAddress()
    {
        string EVIIp = EVIIPEdit.GetComponent<UnityEngine.UI.InputField>().text;
        return EVIIp;
    }

    public int GetEVIPort()
    {
        string EVIPortText = EVIPortEdit.GetComponent<UnityEngine.UI.InputField>().text;
        return Int32.Parse(EVIPortText);
    }

    public string GetEgoVehicleName()
    {
        return EgoVehicleNameEdit.GetComponent<UnityEngine.UI.InputField>().text;
    }

    public void SetEVIConnected()
    {
        BConnectToEVI.GetComponent<UnityEngine.UI.Button>().interactable = false;
        EVIIPEdit.GetComponent<UnityEngine.UI.InputField>().interactable = false;
        EVIPortEdit.GetComponent<UnityEngine.UI.InputField>().interactable = false;
    }

    // Called when the node enters the scene tree for the first time.
    void Start()
    {
        TextAsset selectedSumoNetFileXml = Resources.Load<TextAsset>(SelectedSumoNetFile);

        var cmdLineArgs = ParseCmdLineArgs();

        // Update file selector button text accordingly:
        _on_SumoNetworkSelector_file_selected(SelectedSumoNetFile);
    }

    public Dictionary<string, string> ParseCmdLineArgs()
    {
        Dictionary<string, string> args = new Dictionary<string, string>();
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            if (!arg.Contains("="))
            {
                args[arg] = "";
            }
            else
            {
                string[] kv = arg.Split('=');
                args[kv[0].TrimStart('-')] = kv[1];
            }
        }

        // print parsed args:
        Debug.Log("The following command line args have been parsed:");
        foreach (KeyValuePair<string, string> entry in args)
        {
            Debug.Log($"{entry.Key}: {entry.Value}");
        }

        if (
            args.ContainsKey("help")
            || args.ContainsKey("h")
            || args.ContainsKey("vce-help")
        )
        {
            // This doesn't work with --help or -h,
            // since Godot will show its own
            // help before running any of our code.
            Debug.Log(
                @"
                3D Environment of the Virtual Cycling Environment

                Usage: 3denv [Unity options] -- [3DEnv options]

                3DEnv options:
                --scenario=<scenario>        A SUMO .net.xml scenario to load on startup
                --scenario-seed=<seed>       An integer seed for the procedural generation
                --vehicle-type=<vtype>       {""BICYCLE"", ""BICYCLE_WITH_MINIMAP""}
                --evi-address=<address>      Address of the Ego Vehicle Interface
                --evi-port=<port>            EVI Port
                --evi-connect-on-launch=True Connect to EVI immediately
                --skip-menu=True             Don't show menu on launch
                "
            );
            if (Application.isEditor)
            {
                // UnityEditor.EditorApplication.isPlaying = false;
            }
            else
            {
                Application.Quit();
            }
        }
        SkipMenu = args.ContainsKey("skip-menu")
            && args["skip-menu"].ToLower().Equals("true");
        ConnectEVIOnLaunch = args.ContainsKey("evi-connect-on-launch")
            && args["evi-connect-on-launch"].ToLower().Equals("true");
        if (args.ContainsKey("scenario")) SelectedSumoNetFile = args["scenario"];
        return args;
    }

    public void _on_VehicleTypeSelection_item_selected()
    {
        Debug.Log("Selected Vehicle Type: " + SelectedVehicleTypeBtn.GetComponent<Dropdown>().value);
        // Update selected vehicle type based on dropdown selection:
        switch (SelectedVehicleTypeBtn.GetComponent<Dropdown>().value)
        {
            case 0:
                SelectedVehicleTypeBtn.GetComponentInChildren<Text>().text = "Bicycle";
                break;
            case 1:
                SelectedVehicleTypeBtn.GetComponentInChildren<Text>().text = "Bicycle with Minimap";
                break;
            case 2:
                SelectedVehicleTypeBtn.GetComponentInChildren<Text>().text = "Bicycle Interface";
                break;
            case 3:
                SelectedVehicleTypeBtn.GetComponentInChildren<Text>().text = "Car";
                break;
            default:
                SelectedVehicleTypeBtn.GetComponentInChildren<Text>().text = "Bicycle";
                break;
        }
    }


    public void _on_BSelectSumoScenario_pressed()
    {

    }

    public void _on_SumoNetworkSelector_confirmed()
    {
        // SelectedSumoNetFile = SumoNetworkFileDialog.CurrentPath + SumoNetworkFileDialog.Filename;
        // BSelectSumoScenario.Text = SumoNetworkFileDialog.CurrentFile.Replace(".net.xml", "");
    }

    public void _on_SumoNetworkSelector_file_selected(string path)
    {
        SelectedSumoNetFile = "Resources/" + path;
        Debug.Log("Selected SUMO Network File: " + SelectedSumoNetFile);
        BSelectSumoScenario.GetComponentInChildren<TMP_Text>().text = path.Replace(".net.xml", "");
    }

    public void _on_BGenerateNetwork_pressed()
    {

    }

    public void _on_BCloseMenu_pressed()
    {
        gameObject.SetActive(false);
    }

    public void _on_BExit_pressed()
    {
        if (Application.isEditor)
        {
            // UnityEditor.EditorApplication.isPlaying = false;
        }
        else
        {
            Application.Quit();
        }
    }

    public void _on_BConnectToEVI_pressed()
    {
        /*
        GameStatics.GameInstance.ConnectToEVI(
            EgoVehicleNameEdit.Text,
            EVIIPEdit.Text,
            GetEVIPort(),
            GetSelectedVehicleType()
        );
        SetEVIConnected();
        */
    }

    public void _on_BRecordPerformance_pressed()
    {
        Debug.Log("Toggled performance recording");
    }

}
