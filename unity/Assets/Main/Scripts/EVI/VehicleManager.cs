using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Asmp.Vehicle;


public class VehicleManager : MonoBehaviour
{
	private Vector3 _sumoOffset = GameStatics.SumoOffset;
	private bool _egoVehicleRegistered;
	private Dictionary<uint, ForeignVehicleController> _vehicles3D;
	private uint _egoVehicleHashId;
	private string _vehicleType;
	private Rigidbody _egoInterfaceVehicle;
	private Rigidbody _egoSimpleVehicle;
	public GameObject playerVehicle;
	public GameObject setting;
	public void Init(uint egoVehicleHashId, string vehicleType)
	{
		_egoVehicleHashId = egoVehicleHashId;
		_vehicleType = vehicleType;

		// Change between different bicycle controllers
		if (_vehicleType == "BICYCLE_INTERFACE")
		{
			_egoInterfaceVehicle = playerVehicle.GetComponent<Rigidbody>();
		}
		else
		{
			_egoSimpleVehicle = playerVehicle.GetComponent<Rigidbody>();
		}


		_vehicles3D = new Dictionary<uint, ForeignVehicleController>();
	}

	//Seems to work
	public void ConvergeFellowVehicles(Dictionary<uint, VehicleData> vehicleData)
	{
		// For each vehicle that is not the bike
		foreach (VehicleData veh in vehicleData.Values.ToList().Where(veh => veh.vehicleId != _egoVehicleHashId))
		{
			ForeignVehicleController vehicle = _vehicles3D[veh.vehicleId];
			if (vehicle == null)
				continue;
			vehicle.targetTranslation = new Vector3((float)-veh.xCoord, 0f, (float)veh.yCoord);
			vehicle.targetRotation = Quaternion.Euler(0f, (float)veh.angleDeg, 0f).normalized;
			vehicle.vehicleSpeed = veh.speed;
			vehicle.updateSignalsBool(

				veh.turnSignalLeftOn,
				veh.turnSignalRightOn

			);

		}
	}

	public void UpdateFellowVehicles(Dictionary<uint, VehicleData> vehicleData)
	{
		foreach (VehicleData veh in vehicleData.Values.ToList().Where(veh => veh.vehicleId != _egoVehicleHashId))
		{
			if (veh.stateOfExistence == Command.CommandOneofOneofCase.UnregisterVehicleCommand &&
				_vehicles3D.ContainsKey(veh.vehicleId))
			{
				// Remove 3D Vehicle
				Debug.Log($"Removing vehicle {veh.vehicleId}");
				Destroy(_vehicles3D[veh.vehicleId].gameObject);
				// _vehicles3D.Remove(veh.vehicleId); Uncommenting this breaks the removal of vehicles from Godot
				continue;
			}

			// Create new 3D Vehicle
			if (!_vehicles3D.ContainsKey(veh.vehicleId))
			{
				ForeignVehicleController vehicle;
				GameObject carPrefab = Resources.Load<GameObject>("Environment/Vehicles/Car");
				GameObject bikePrefab = Resources.Load<GameObject>("Environment/Vehicles/Bike");
				Debug.Log($"Spawning a vehicle of type {veh.vehicleType}");
				if (veh.vehicleType == RegisterVehicleCommand.Types.VehicleType.Bicycle)
				{
					GameObject bike = GameObject.Instantiate(bikePrefab);
					vehicle = bike.GetComponent<ForeignVehicleController>();
					bike.transform.parent = this.transform;
				}
				else
				{
					if (veh.vehicleType != RegisterVehicleCommand.Types.VehicleType.PassengerCar)
					{
						Debug.Log($"Vehicle type {veh.vehicleType} is not supported yet. Spawning a passenger car.");
					}
					GameObject car = GameObject.Instantiate(carPrefab);
					vehicle = car.GetComponent<ForeignVehicleController>();
					car.transform.parent = this.transform;
				}

				vehicle.vehicleId = veh.vehicleId;
				vehicle.vehicleType = veh.vehicleType;
				vehicle.lane = veh.lane;
				// vehicle = vehicleScene.Instance<ForeignVehicleController>();
				vehicle.gameObject.transform.position = new Vector3((float)-veh.xCoord, 0f, (float)veh.yCoord);
				vehicle.gameObject.transform.rotation = Quaternion.Euler(0f, (float)veh.angleDeg, 0f).normalized; // normalized or not need to check
				vehicle.gameObject.name = "Vehicle: " + veh.vehicleId.ToString();
				vehicle.angleDeg = veh.angleDeg;

				_vehicles3D.Add(veh.vehicleId, vehicle);
			}
			// Vehicle Exists, update position and signals
			else
			{
				ForeignVehicleController vehicle = _vehicles3D[veh.vehicleId];
				if (vehicle == null)
					continue;
				vehicle.targetTranslation = new Vector3((float)-veh.xCoord, 0f, (float)veh.yCoord);
				vehicle.targetRotation = Quaternion.Euler(0f, (float)veh.angleDeg, 0f).normalized;
				vehicle.vehicleSpeed = veh.speed;
				vehicle.lane = veh.lane;
				vehicle.angleDeg = veh.angleDeg;

				vehicle.updateSignalsBool(

					veh.turnSignalLeftOn,
					veh.turnSignalRightOn

				);

			}
		}
	}

	private Asmp.Vehicle.Command MakeRegisterCommand(VehicleData vData)
	{
		var cmd = new Asmp.Vehicle.Command
		{
			RegisterVehicleCommand = new Asmp.Vehicle.RegisterVehicleCommand
			{
				State = vData.toVehicleStateMsg(),
				VehicleId = vData.vehicleId,
				IsEgoVehicle = true
			}
		};

		LevelAndConnectionSettings settings = setting.GetComponent<LevelAndConnectionSettings>();
		string vehicleType = settings.GetSelectedVehicleType();

		Debug.Log($"Registering ego vehicle with hash id {cmd.RegisterVehicleCommand.VehicleId}");

		//TODO: Add other vehicle types; make minimap an option?
		cmd.RegisterVehicleCommand.VehType = vehicleType switch
		{
			"CAR" => RegisterVehicleCommand.Types.VehicleType.PassengerCar,
			"BICYCLE" => RegisterVehicleCommand.Types.VehicleType.Bicycle,
			"BICYCLE_INTERFACE" => RegisterVehicleCommand.Types.VehicleType.Bicycle,
			"BICYCLE_WITH_MINIMAP" => RegisterVehicleCommand.Types.VehicleType.Bicycle,
			_ => throw new ArgumentException(
				$"Vehicle type {vehicleType} is not defined for "
				+ "MakeRegisterCommand()."
			)
		};

		return cmd;
	}

	private static Asmp.Vehicle.Command MakeUpdateCommand(VehicleData vData)
	{
		var cmd = new Asmp.Vehicle.Command();
		cmd.UpdateVehicleCommand = new Asmp.Vehicle.UpdateVehicleCommand();
		cmd.UpdateVehicleCommand.State = vData.toVehicleStateMsg();
		cmd.UpdateVehicleCommand.VehicleId = vData.vehicleId;
		return cmd;
	}

	uint _messageId = 0;
	public Asmp.Message SyncEgoVehicle()
	{
		VehicleData vData = GetEgoVehicleState();
		Asmp.Message msg = new Asmp.Message();
		msg.Id = _messageId++;
		msg.Vehicle = new Asmp.Vehicle.Message();
		//msg.Vehicle.TimeS = (stepLengthMilliseconds * vehicleUpdateStep) / 1000.0;
		if (_egoVehicleRegistered)
		{
			msg.Vehicle.Commands.Add(MakeUpdateCommand(vData));
		}
		else
		{
			msg.Vehicle.Commands.Add(MakeRegisterCommand(vData));
			_egoVehicleRegistered = true;
		}

		return msg;
	}
	private VehicleData GetEgoVehicleState()
	{
		// Get the current position of the ego vehicle in Godot, flip the X coordinate to match the Sumo coordinate system

		if (_vehicleType == "BICYCLE_INTERFACE")
		{
			return new VehicleData(
				_egoVehicleHashId,
				-(_egoInterfaceVehicle.position.x + _sumoOffset.x),
				_egoInterfaceVehicle.position.z + _sumoOffset.z,
				0,
				0,
				null,
				null,
				_egoInterfaceVehicle.linearVelocity.magnitude * 3.6, // convert from m/s to km/h
				-_egoInterfaceVehicle.rotation.eulerAngles.y - 90.0f,
				Command.CommandOneofOneofCase.UpdateVehicleCommand,
				false,
				false,
				false,
				false,
				false,
				false,
				false);
		}
		else
		{
			return new VehicleData(
			_egoVehicleHashId,
			-(_egoSimpleVehicle.position.x + _sumoOffset.x),
			_egoSimpleVehicle.position.z + _sumoOffset.z,
			0,
			0,
			null,
			null,
			_egoSimpleVehicle.linearVelocity.magnitude * 3.6, // convert from m/s to km/h
			-_egoSimpleVehicle.rotation.eulerAngles.y - 90.0f,
			Command.CommandOneofOneofCase.UpdateVehicleCommand,
			false,
			false,
			false,
			false,
			false,
			false,
			false);
		}
	}


}
