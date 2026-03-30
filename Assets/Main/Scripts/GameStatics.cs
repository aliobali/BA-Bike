using UnityEngine;
using System.Globalization;

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
}
