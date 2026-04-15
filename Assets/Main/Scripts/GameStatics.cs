using UnityEngine;
using System;
using System.Globalization;
using System.Collections.Generic;

public static class GameStatics
{
	public static NumberFormatInfo provider = new NumberFormatInfo() { NumberDecimalSeparator = "." };
	public static string trafficLightPath = @"Environment/TrafficLight/TrafficLight";
	public static string trafficPanelPath = @"Environment/TrafficLight/TrafficLightPanel";
	public static string trafficLightExtenderPath = @"Environment/TrafficLight/TrafficLightExtender";
	public static string streetLightPath = @"Environment/StreetLights/StreetLight";
	public static string buildingPath = @"Environment/Buildings";

	public static float extentionLength = 2.5f;
	public const float Rad90 = Mathf.PI * 0.5f;
	public const float Rad180 = Mathf.PI;
	public const int DefaultSeed = 4358234;
	public static VceInstance GameInstance;
	public static Vector3 SumoOffset;
	public static SortedList<string, Tuple<string, string>> VehicleTypes = new SortedList<string, Tuple<string, string>>();

	static GameStatics()
	{
		// The keys have to match with those used in MakeRegisterCommand in VehicleManager.
		VehicleTypes.Add(
			"BICYCLE",
			new Tuple<string, string>(
				"Bicycle",
				"Environment/Vehicles/Bike"
			)
		);
		VehicleTypes.Add(
			"BICYCLE_WITH_MINIMAP",
			new Tuple<string, string>(
				"Bicycle with Minimap",
				"Environment/Vehicles/BicycleWithMinimap"
			)
		);
		VehicleTypes.Add(
			"BICYCLE_INTERFACE",
			new Tuple<string, string>(
				"Bicycle Interface",
				"Environment/Vehicles/InterfaceBicycle"
			)
		);
		VehicleTypes.Add(
			"CAR",
			new Tuple<string, string>(
				"Car",
				"Environment/Vehicles/Car"
			)
		);
	}
}
