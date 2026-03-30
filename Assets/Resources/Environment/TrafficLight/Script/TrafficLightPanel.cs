using UnityEngine;

public class TrafficLightPanel : MonoBehaviour
{
    private string _id;
    private GameObject redLight;
    private GameObject yellowLight;
    private GameObject greenLight;
    private TrafficLightState _lightState = TrafficLightState.TLState_BlinkYellow;
    public TrafficLightState LightState
	{
		get { return _lightState; }
        set
		{
			_lightState = value;
            switch (_lightState)
            {
                case TrafficLightState.TLState_Green:
                    redLight.SetActive(false);
                    yellowLight.SetActive(false);
                    greenLight.SetActive(true);
                    break;
                case TrafficLightState.TLState_Yellow:
                    redLight.SetActive(false);
                    yellowLight.SetActive(true);
                    greenLight.SetActive(false);
                    break;
                case TrafficLightState.TLState_Red:
                    redLight.SetActive(true);
                    yellowLight.SetActive(false);
                    greenLight.SetActive(false);
                    break;
            }
		}
	}

    public string id
	{
		get
		{
			return _id;
		}
        set
		{
			_id = value;
		}
	}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        redLight = gameObject.transform.Find("Light_Red").gameObject;
        yellowLight = gameObject.transform.Find("Light_Yellow").gameObject;
        greenLight = gameObject.transform.Find("Light_Green").gameObject;

        redLight.SetActive(false);
        yellowLight.SetActive(false);
        greenLight.SetActive(false);
    }

/*
    public void SetDirection(connectionTypeDir direction)
	{
        MeshRenderer panelRenderer = gameObject.GetComponent<MeshRenderer>();
        switch(direction)
		{
			case connectionTypeDir.l:
                panelRenderer.material = 
		}
	}
*/

    private float passedDeltaTime = 0.0f;
    void Update()
	{
		if(LightState == TrafficLightState.TLState_BlinkYellow)
		{
			if(passedDeltaTime > 0.5f)
			{
				yellowLight.SetActive(!yellowLight.activeSelf);
                passedDeltaTime = 0.0f;
			}
            else
			{
				passedDeltaTime += Time.deltaTime;
			}
		}
	}

}

public enum TrafficLightState
{
	TLState_Green = 0,
    TLState_Yellow = 1,
    TLState_Red = 2,
    TLState_RedYellow = 3,
    TLState_BlinkYellow = 4,
}
