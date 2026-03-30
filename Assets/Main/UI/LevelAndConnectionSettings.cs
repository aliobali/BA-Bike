using UnityEngine;
using System;
using System.Collections.Generic;

public class LevelAndConnectionSettings : MonoBehaviour
{
    private string SelectedSumoNetFile = @"Scenarios/example-intersection/network.net.xml";
    private string SeedEdit = "";
    public string GetSelectedSumoNetFile()
	{
		return SelectedSumoNetFile;
	}

    public int GetSeed()
	{
		return int.Parse(SeedEdit);
	}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
