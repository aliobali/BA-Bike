using UnityEngine;
using System.Collections.Generic;
using Env3d.SumoImporter.NetFileComponents;

public class TrafficLight : MonoBehaviour
{
    public List<GameObject> panels { get; private set; } = new List<GameObject>();
    private GameObject lightPanelObj;
    private GameObject lightExtenderObj;

    const float trafficLightExtendLength = 2.5f;
    void Start()
	{
	}

    public void AddPanel(string panelId, float offset, int currentPanelIndex, int totalPanels)
	{
        GameObject lightPanelPrefab = Resources.Load<GameObject>(GameStatics.trafficPanelPath);
        lightPanelObj = GameObject.Instantiate(lightPanelPrefab);
        lightPanelObj.name = $"tlP_{currentPanelIndex}";
		//lightPanelObj.transform.position = new Vector3(trafficLightExtendLength * offset + 0.6f * (currentPanelIndex - totalPanels/2.0f) + 0.4f, 0, 0);
        lightPanelObj.transform.position = gameObject.transform.position;
        lightPanelObj.transform.rotation = gameObject.transform.rotation;
        lightPanelObj.transform.parent = gameObject.transform;
        panels.Add(lightPanelObj);
	}

    public void AddPoolExtension(int extentionId, GameObject parent)
	{
		GameObject poleExtentionPrefab = Resources.Load<GameObject>(GameStatics.trafficLightExtenderPath);
        GameObject poleExtentionObj = GameObject.Instantiate(poleExtentionPrefab);
        poleExtentionObj.name = $"tlE_{extentionId}";
        //poleExtentionObj.transform.position = new Vector3(trafficLightExtendLength * extentionId, 0, 0);
        poleExtentionObj.transform.position = parent.transform.position;
        poleExtentionObj.transform.rotation = parent.transform.rotation;
        poleExtentionObj.transform.parent = parent.transform;
	}
}
