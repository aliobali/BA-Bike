using UnityEngine;
using Env3d.SumoImporter;
using Asmp.Vehicle;
using Env3d.SumoImporter.NetFileComponents;

public class ForeignVehicleController : MonoBehaviour
{
	private GameObject _wheelFrontLeft;
	private GameObject _wheelFrontRight;
	private GameObject _wheelRearLeft;
	private GameObject _wheelRearRight;
	private GameObject[] _wheels;
	private MeshFilter[] _caseMeshes;
	public double wheelRadius = .33;
	public double vehicleSpeed = 0;
	public float steeringAngleApproximationFactor = 6f;
	public Rigidbody _egoInterfaceVehicle;
	public Vector3 targetTranslation = Vector3.zero;
	public Quaternion targetRotation;
	public Vector3 rotation;
	public Vector3 translation;
	public double angleDeg; // 0 is North

	public uint vehicleId;
	public string edgeId;
	public uint laneNum;
	public uint laneId;

	public NetFileLane lane;

	public bool isEgoVehicle;
	public RegisterVehicleCommand.Types.VehicleType vehicleType;

	public bool TurnLeftSignalOn;
	public bool TurnRightSignalOn;
	public float passedDeltaTime = 0.0f;

	void Start()
	{
		_egoInterfaceVehicle = this.gameObject.GetComponent<Rigidbody>();
	}
	void FixedUpdate()
	{
		if (targetTranslation == Vector3.zero) // Make sure we start interpolating after receiving the first update message from SUMO
		{
			return;
		}
		float interpolationWeight = 0.095f;
		_egoInterfaceVehicle.position = Vector3.Lerp(gameObject.transform.position, targetTranslation, interpolationWeight);
		translation = _egoInterfaceVehicle.position;
		Quaternion currentRotation = _egoInterfaceVehicle.rotation.normalized;
		Quaternion interpolatedRotation = Quaternion.Slerp(currentRotation, targetRotation, interpolationWeight).normalized;
		_egoInterfaceVehicle.rotation = interpolatedRotation;

		float deltaYaw = interpolatedRotation.eulerAngles.y - currentRotation.eulerAngles.y;
		UpdateWheels(Time.fixedDeltaTime, deltaYaw);
		updateSignals(Time.fixedDeltaTime);

	}

	public void updateSignalsBool(bool turnSignalLeftOn, bool turnSignalRightOn)
	{
		if (this.vehicleType == RegisterVehicleCommand.Types.VehicleType.PassengerCar)
		{
			this.TurnLeftSignalOn = turnSignalLeftOn;
			this.TurnRightSignalOn = turnSignalRightOn;
		}
	}

	public void updateIsEgoVehicle(bool egoVehicle)
	{
		this.isEgoVehicle = egoVehicle;
	}

	private void updateSignals(float delta)
	{
		if (this.vehicleType == RegisterVehicleCommand.Types.VehicleType.Bicycle)
		{
			// TODO
		}
		else
		{
			// Assuming PassengerCar or similar

			if (this.TurnLeftSignalOn == true)
			{

				if (this.passedDeltaTime > 0.5f)
				{
					this.passedDeltaTime = 0.0f;
				}
				else
				{
					this.passedDeltaTime += delta;
				}
			}
			else
			{

			}

			if (this.TurnRightSignalOn == true)
			{

				if (this.passedDeltaTime > 0.5f)
				{
					this.passedDeltaTime = 0.0f;
				}
				else
				{
					this.passedDeltaTime += delta;
				}
			}
			else
			{

			}
		}
	}


	public virtual void UpdateWheels(float delta, float angleRad = 0) { }

	public virtual void ChangeColor(Color color) { }

	public Vector3 GetForwardVector()
	{
		return GetRotationVector(gameObject.transform.localRotation.eulerAngles);
	}

	private static Vector3 GetRotationVector(Vector3 Rotator)
	{
		float Pitch = (Rotator.z);
		float Yaw = (-Rotator.y);

		float sinP = Mathf.Sin(Pitch);
		float sinY = Mathf.Sin(Yaw);
		float cosY = Mathf.Cos(Yaw);
		float cosP = Mathf.Cos(Pitch);

		return new Vector3(cosY * cosP, sinP, cosP * sinY);
	}
}
