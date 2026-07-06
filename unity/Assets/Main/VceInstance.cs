using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
//using CsvHelper;

public class VceInstance : MonoBehaviour
{
	private GameObject VCESettings;

	public GameObject PlayerVehicle;
	public Vector3 PlayerStartPoint { get; private set; } // supposed to be a 3x4 matrix
	public GameObject SUMONetworkGenerator { get; private set; }
	public NetworkGenerator SUMOScript { get; private set; }
	// public TrafficLightsManager TrafficLightsManager {get; private set;}
	public EviConnector eviConnector;
	//public TrafficLightsManager trafficLightManager;

	public void ConnectToEVI(string EgoVehicleName, string IP, int Port, string VehicleType)
	{
		eviConnector.Init(IP, Port, EgoVehicleName, VehicleType);
	}

	void Start()
	{
		this.transform.position = Vector3.zero;
		GameStatics.GameInstance = this;
		Debug.Log("VCEInstance started");
		// Things I am not sure about are commented out, starting with !!!
		// !!! GameStatics.SetGameInstance(this);
		SUMONetworkGenerator = GameObject.Find("NetworkGenerator");
		SUMOScript = SUMONetworkGenerator.GetComponent<NetworkGenerator>();

		// !!! TrafficLightsManager = new TrafficLightsManager();
		// TODO: already use the settings from VCESettings here
		// !!! GameObject settings = GameObject.Find("LevelAndConnectionSettings");
		eviConnector.gameObject.transform.position = Vector3.zero;
		SUMOScript.LoadNetwork();
		ConnectToEVI(
			"ego-vehicle",
			"127.0.0.1",
			12346,
			"bicycle"
		);
	}
}