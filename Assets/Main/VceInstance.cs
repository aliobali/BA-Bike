using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
//using CsvHelper;

public class VceInstance: MonoBehaviour
{
	private GameObject VCESettings;

	public GameObject PlayerVehicle {get; private set;}
	public Vector3 PlayerStartPoint {get; private set;} // supposed to be a 3x4 matrix
	public GameObject SUMONetworkGenerator {get; private set;}
	public NetworkGenerator SUMOScript {get; private set;}
	// public TrafficLightsManager TrafficLightsManager {get; private set;}

	void Start()
	{
		Debug.Log("VCEInstance started");
		// Things I am not sure about are commented out, starting with !!!
		// !!! GameStatics.SetGameInstance(this);
		SUMONetworkGenerator = GameObject.Find("NetworkGenerator");
		SUMOScript = SUMONetworkGenerator.GetComponent<NetworkGenerator>();

		// !!! TrafficLightsManager = new TrafficLightsManager();
		// TODO: already use the settings from VCESettings here
		// !!! GameObject settings = GameObject.Find("LevelAndConnectionSettings");

		SUMOScript.LoadNetwork();
        
	}

	void Update()
	{
		
	}
}