using UnityEngine;
using Env3d.SumoImporter;
using Env3d.SumoImporter.NetFileComponents;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Numerics;
using Debug = UnityEngine.Debug;
using Vector3 = UnityEngine.Vector3;
using Vector2 = UnityEngine.Vector2;
using Quaternion = UnityEngine.Quaternion;
using System.Reflection;

public class NetworkGenerator : MonoBehaviour
{
    // Default Map XML Path
    // private string netDataRouter = @"Sumo/DefaultEnvironment/10Traffic100Buildings.net.xml";
    // Basic Crossroad Map
    // private string netDataRouter = @"Extern/Scenarios/example-intersection/network.net";
    // Large Map
    // private string netDataRouter = @"Extern/Scenarios/paderborn-north/paderborn-north.net";
    private string netDataRouter = @"Extern/Scenarios/berlin/intersection1_bigger_no_priority.net";
    private string buildingRouter = @"Extern/Scenarios/berlin/intersection1_bigger.poly";

    // Texture Materials
    private Texture2D grasMap;
    private Texture2D intersectionMap;
    private Texture2D streetMap;

    // Private Variables
    private List<GameObject> junctionNodes = new List<GameObject>();
    private int tlCounter = 0;
    int sLCount = 0;

    public UnityEngine.Vector3 sumoOffset;

    private System.Random random = new (GameStatics.DefaultSeed);

    public void LoadNetwork(bool GenerateStreetLights = true)
	{
        Debug.Log("Loading Network");
        //string netPath = Path.Combine(Application.streamingAssetsPath, netDataRouter);
        netType netData = ImportHelper.LoadXMLFile<netType>(netDataRouter);
        // Texture Maps
        grasMap = Resources.Load<Texture2D>(@"Environment\Materials\Textures\T_Gras");
        intersectionMap = Resources.Load<Texture2D>(@"Environment\Materials\Textures\Intersection");
        streetMap = Resources.Load<Texture2D>(@"Environment\Materials\Textures\Street");

        // Get and Set Map Boundaries
        string[] boundaries = netData.location.convBoundary.Split(',');
        float xMax = -float.Parse(boundaries[0], GameStatics.provider);
        float yMin = float.Parse(boundaries[1], GameStatics.provider);
		float xMin = -float.Parse(boundaries[2], GameStatics.provider);
		float yMax = float.Parse(boundaries[3], GameStatics.provider);

        sumoOffset = new UnityEngine.Vector3((xMax + xMin) / 2.0f, 0, (yMin + yMax) / 2.0f);

        // Meta data
        string projParameter = netData.location.projParameter;
        string netOffset = netData.location.netOffset;

        // Filter data
        Dictionary<string, List<connectionType>> connections;
        Dictionary<string, NetFileJunction> junctions;
        Dictionary<string, NetFileEdge> edges;
		Dictionary<string, NetFileLane> lanes;

        LoadNetData(netData, out connections, out junctions, out edges, out lanes);

        // Generate Components
        Debug.Log("Start generating landscape");
        AddLandscape(yMin, xMin, yMax, xMax);
        Debug.Log("Finished generating");
        Debug.Log("Start generating junctions");
        AddJunctions(junctions);
        Debug.Log("Finished generating");

        foreach(NetFileEdge netFileEdge in edges.Values)
        {
            NetFileJunction junctionTo = netFileEdge.to;
            NetFileJunction junctionFrom = netFileEdge.from;
            AddTrafficLights(netFileEdge, junctionTo, lanes, connections);
            
            foreach(NetFileLane lane in netFileEdge.lanes)
            {
                AddLane(lane, junctionTo, junctionFrom);
            }

            if(GenerateStreetLights)
            {
                NetFileLane netFileLane = netFileEdge.lanes[0];
                AddStreetLights(netFileLane);
            }
        }

        LoadAndGenerateEnvironment(buildingRouter);
	}

    // Filter function for conncection, junctions, edges, lanes
    private void LoadNetData(
        netType netData,
        out Dictionary<string, List<connectionType>> connections, 
		out Dictionary<string, NetFileJunction> junctions, 
		out Dictionary<string, NetFileEdge> edges, 
		out Dictionary<string, NetFileLane> lanes
    )
    {
        connections = new Dictionary<string, List<connectionType>>();
		junctions = new Dictionary<string, NetFileJunction>();
		edges = new Dictionary<string, NetFileEdge>();
		lanes = new Dictionary<string, NetFileLane>();

        // Get all junctions
        foreach (junctionType junction in netData.junction)
        {
            // only generate junctions which are connected to lanes and are not internal
            if (junction.type != junctionTypeType.@internal)
            {
                NetFileJunction newJunction = new NetFileJunction(junction, ref lanes);
                junctions.Add(junction.id, newJunction);   
            }
        }

        // Get all edges and lane objects
        foreach (edgeType edge in netData.edge)
        {
            // Only non-internal edges
            if (edge.functionSpecified)
            {
                continue;
            }

            NetFileEdge newEdge = new NetFileEdge(edge, ref junctions);

            foreach (var lane in edge.Items)
            {
                if(lane is laneType)
                {
                    // Add all lanes which belong to this edge
                    newEdge.AddLane((laneType)lane, ref lanes);
                }
            }
            edges.Add(newEdge.id, newEdge);
        }

        // Get all connections in order to visualize traffic lights correctly
        foreach (connectionType ct in netData.connection)
        {
            // ct.viaField is junctionId + internal lanes
            if (ct.via is null && ct.fromLane is null)
            {
                continue;
            }

            NetFileEdge fromEdge;
            if(!edges.TryGetValue(ct.from, out fromEdge))
            {
                continue;
            }

            string fromLane = fromEdge.lanes[int.Parse(ct.fromLane)].Id;
            if(connections.ContainsKey(fromLane))
            {
                connections[fromLane].Add(ct);
            }
            else
            {
                List<connectionType> connectionTypes = new List<connectionType>();
                connectionTypes.Add(ct);
                connections.Add(fromLane, connectionTypes);
            }
        }
    }

    // Generate GameObject Functions

    private void AddLandscape(float yMin, float xMin, float yMax, float xMax)
    {
        Mesh landscape = new Mesh();

        UnityEngine.Vector3[] vertices = new UnityEngine.Vector3[4];
		UnityEngine.Vector2[] uvs = new UnityEngine.Vector2[4];
		int[] triangles = new int[6];

		vertices[0] = new UnityEngine.Vector3(xMax, -0.01f, yMax) - sumoOffset + new Vector3(320, 0, 0);
		vertices[1] = new UnityEngine.Vector3(xMax, -0.01f, yMin) - sumoOffset + new Vector3(320, 0, 0);
		vertices[2] = new UnityEngine.Vector3(xMin, -0.01f, yMin) - sumoOffset + new Vector3(320, 0, 0);
		vertices[3] = new UnityEngine.Vector3(xMin, -0.01f, yMax) - sumoOffset + new Vector3(320, 0, 0);

		//make landscape a little bit large then sumo file defines
		vertices[0] += vertices[0].normalized * 100.0f;
		vertices[1] += vertices[1].normalized * 100.0f;
		vertices[2] += vertices[2].normalized * 100.0f;
		vertices[3] += vertices[3].normalized * 100.0f;


		uvs[0] = new Vector2(0, 0);
		uvs[1] = new Vector2(0, yMax - yMin) / 2.0f;
		uvs[2] = new Vector2(xMax - xMin, yMax - yMin) / 2.0f;
		uvs[3] = new Vector2(xMax - xMin, 0) / 2.0f;

		triangles[0] = 0;
		triangles[1] = 1;
		triangles[2] = 2;
		triangles[3] = 0;
		triangles[4] = 2;
		triangles[5] = 3;

        ImportHelper.AddMesh(gameObject, landscape, vertices, uvs, triangles, grasMap);
        MeshCollider landscapePhysic = gameObject.GetComponent<MeshCollider>();
        landscapePhysic.sharedMesh = landscape;
    }

    private void AddJunctions(in Dictionary<string, NetFileJunction> junctions)
    {
        foreach(NetFileJunction junction in junctions.Values)
        {
            GameObject junctionNode = new GameObject("Junction " + junction.Id);
            junctionNode.transform.SetParent(gameObject.transform);
            junctionNode.AddComponent<MeshFilter>();
            junctionNode.AddComponent<MeshRenderer>();

            Mesh meshJunction = new Mesh();
            Vector3[] vertices = junction.Shape;

            // Skip if junction has not enough vertices to create a triangle
            if(vertices.Length < 3)
            {
                continue;
            }

            int[] triangles;
            // Use the triangulator to get indices for creating triangles
            if(vertices.Length == 3)
            {
                triangles = new int[3];
                triangles[0] = 1;
                triangles[1] = 2;
                triangles[2] = 0;
            }
            else 
            {
                triangles = Triangulator.Triangulate(vertices);
            }

            Vector2[] uvs = new Vector2[vertices.Length];
            float textureScale = 1; // .35f; // How often to repeat the texture per meter.
            for(int i = 0; i < vertices.Length; i++)
            {
                uvs[i] = new Vector2(vertices[i].x * textureScale, vertices[i].z * textureScale);
                vertices[i] -= sumoOffset; // Added because of the Landscape offset
            }

            /*
            Vector3[] normals;
            float[] tangents;
            ImportHelper.CalculateNormals(vertices, triangles, uvs, out normals, out tangents);
            */

            ImportHelper.AddMesh(junctionNode, meshJunction, vertices, uvs, triangles, intersectionMap);
            junctionNodes.Add(junctionNode);
        }
    }

    private void AddTrafficLights(in NetFileEdge netFileEdge, in NetFileJunction junction, in Dictionary<string, NetFileLane> lanes, in Dictionary<string, List<connectionType>> connections)
    {
        bool edgeHasTrafficLight = junction.JunctionType == junctionTypeType.traffic_light;

		if (!edgeHasTrafficLight)
		{
			return;
		}

		int edgeLaneCount = netFileEdge.lanes.Count;

		Vector3 lanesCenter = Vector3.zero;
		float laneCenterWidth = 0;
		Vector3 laneCenterDirection = Vector3.zero;

		List<NetFileLane> edgeLanes = new List<NetFileLane>();

		//Spawn traffic lights
		for (int i = 0; i < edgeLaneCount; i++)
		{
			NetFileLane lane = netFileEdge.lanes[i];
			edgeLanes.Add(lane);

			// Calculate the position (in line with the lane) coordinates of last two street vertices
			Vector3 preLaneEndPoint = lane.Shape[lane.Shape.Length - 2] - sumoOffset;
			Vector3 laneEndPoint = lane.Shape[lane.Shape.Length - 1] - sumoOffset;

			lanesCenter += laneEndPoint;
			laneCenterWidth += lane.Width;
			laneCenterDirection += (laneEndPoint - preLaneEndPoint).normalized;
		}

        lanesCenter /= edgeLaneCount;
		laneCenterWidth = laneCenterWidth * 0.5f + 1.0f;
		laneCenterDirection /= edgeLaneCount;

        float angle = Mathf.Atan2(laneCenterDirection.x, laneCenterDirection.z) * Mathf.Rad2Deg;

        laneCenterDirection = Vector3.Cross(laneCenterDirection, Vector3.up);
		laneCenterDirection = laneCenterDirection.normalized;

        Vector3 trafficLightPosition = lanesCenter + laneCenterDirection * laneCenterWidth;

        // Generate a Traffic Light
        GameObject trafficLightPrefab = Resources.Load<GameObject>(GameStatics.trafficLightPath);
        GameObject trafficLightObj = GameObject.Instantiate(trafficLightPrefab);
        trafficLightObj.name = $"TrafficLight {tlCounter}";
        trafficLightObj.transform.position = trafficLightPosition;

        // Rotation was complicated so this solution is a temporary fix, considering that 
        // streets in Paderborn should be left traffic streets. The traffic pole should rotate to
        // driving direction (which it does not do yet)
        trafficLightObj.transform.rotation = Quaternion.Euler(-90f, angle + 90f, 0f);

        edgeLanes.Sort((x, y) => (x.Shape[x.Shape.Length - 1] - trafficLightPosition).sqrMagnitude.CompareTo((y.Shape[y.Shape.Length - 1] - trafficLightPosition).sqrMagnitude));
        for(int i = 0; i < edgeLaneCount; i++)
		{
			NetFileLane lane = edgeLanes[i];
            int laneIndexInJunction = 0;
            for(int laneCount = 0; laneCount < junction.IncomingLanes.Count; laneCount++)
			{
				NetFileLane incLane = junction.IncomingLanes[laneCount];
                if(lane.Id == incLane.Id)
				{
					break;
				}
                else
				{
					List<connectionType> incLanesConnections;
                    if(connections.TryGetValue(incLane.Id, out incLanesConnections))
					{
						laneIndexInJunction += incLanesConnections.Count;
					}
				}
			}

            TrafficLight light = trafficLightObj.GetComponent<TrafficLight>();
            //light.AddPoolExtension(i, trafficLightObj);
            light.AddPoolExtension(tlCounter, trafficLightObj);

            List<connectionType> lct;
            if(connections.TryGetValue(lane.Id, out lct))
			{
				int totalPanels = lct.Count;
                for(int currentPanelIndex = 0; currentPanelIndex < totalPanels; currentPanelIndex++)
				{
					connectionType ct = lct[currentPanelIndex];
                    if((ct.tl == null) || string.Compare((ct.@from + "_" + ct.fromLane), lane.Id, StringComparison.OrdinalIgnoreCase) != 0)
					{
						continue;
					}

                    string tlId = "tl_" + MD5Hasher.getMD5(junction.Id) + "_" + laneIndexInJunction++;
                    light.AddPanel(tlId, i, currentPanelIndex, totalPanels);
				}
			}
		}
        tlCounter++;
    }


    private void AddLane(in NetFileLane lane, in NetFileJunction junctionTo, in NetFileJunction junctionFrom)
    {
        GameObject laneNode = new GameObject();
        laneNode.name = $"Lane {lane.Id}";
        laneNode.transform.SetParent(gameObject.transform);
        laneNode.AddComponent<MeshFilter>();
        laneNode.AddComponent<MeshRenderer>();

        Mesh laneMesh = new Mesh();

        float laneWidthPadding = 0.0f;
		float laneWidth = lane.Width * 0.5f + laneWidthPadding;

        Vector3[] laneShape = lane.Shape;
        if (laneShape.Length < 2)
		{
			Debug.LogWarning($"Lane {lane.Id} has less than 2 vertices.");
			return;
		}

        for(int i = 0; i < laneShape.Length; i++)
        {
            laneShape[i] -= sumoOffset;
        }

        Vector3[] vertices = new Vector3[laneShape.Length * 2];
		Vector2[] uvs = new Vector2[vertices.Length];

        int[] triangles = new int[(laneShape.Length - 1) * 2 * 3];
        //add start points
		AddLaneEnds(junctionFrom, laneShape[0], laneShape[1], laneWidth, true, ref vertices);

        triangles[0] = 1;
		triangles[1] = 2;
		triangles[2] = 0;
		triangles[3] = 1;
		triangles[4] = 3;
		triangles[5] = 2;

        //Build road between start and end
		for (int i = 1; i < laneShape.Length - 1; i++)
		{
			triangles[i * 6] = i * 2 + 1;
			triangles[i * 6 + 1] = i * 2 + 2;
			triangles[i * 6 + 2] = i * 2;
			triangles[i * 6 + 3] = i * 2 + 1;
			triangles[i * 6 + 4] = i * 2 + 3;
			triangles[i * 6 + 5] = i * 2 + 2;

			Vector3 lastDirection = laneShape[i - 1] - laneShape[i];
			Vector3 nextDirection = laneShape[i + 1] - laneShape[i];

			float lastLength = lastDirection.magnitude;
			float nextLength = nextDirection.magnitude;

			float lastFactor = 1, nextFactor = 1;

			if (nextLength > lastLength)
			{
				nextFactor = lastLength / nextLength;
			}
			else
			{
				lastFactor = nextLength / lastLength;
			}

			Vector3 direction =  lastDirection * lastFactor - nextFactor * nextDirection;
			Vector3 rightVector = Vector3.Cross(direction, Vector3.up).normalized * laneWidth;

			vertices[i * 2] = ImportHelper.LineIntersection2D(vertices[(i - 1) * 2], lastDirection, laneShape[i], rightVector);
			vertices[i * 2 + 1] = ImportHelper.LineIntersection2D(vertices[(i - 1) * 2 + 1], lastDirection, laneShape[i], rightVector);
		}

        AddLaneEnds(junctionTo, laneShape[laneShape.Length - 1], laneShape[laneShape.Length - 2], laneWidth, false, ref vertices);

        float distanceLeft = 0;
        float distanceRight = 0;
        for(int i = 0; i < uvs.Length; i += 2)
        {
            if(i >= 2)
            {
                distanceLeft += (vertices[i] - vertices[i - 2]).magnitude / lane.Width;
				distanceRight += (vertices[i+1] - vertices[i - 1]).magnitude / lane.Width;
            }
            uvs[i] = new Vector2(0, distanceLeft);
			uvs[i+1] = new Vector2(1, distanceRight);
        }

        ImportHelper.AddMesh(laneNode, laneMesh, vertices, uvs, triangles, streetMap);
    }

    private void AddLaneEnds(in NetFileJunction junction, UnityEngine.Vector3 shapePoint1, UnityEngine.Vector3 shapePoint2, float laneWidth, bool isStart, ref UnityEngine.Vector3[] vertices)
    {
        int shapeLength = junction.Shape.Length;
		int bestIndex = 0;
		Vector3 junctionIntersection = Vector3.zero;

		float bestDistance = float.MaxValue;

		for (int i = 0; i < shapeLength; i++)
		{
			Vector3 testPoint;
			bool isInSegment = ImportHelper.ClosestPointOnSegment(shapePoint1, junction.Shape[i], junction.Shape[(i + 1) % shapeLength], out testPoint);

			float distanceSq = (testPoint - shapePoint1).sqrMagnitude;
			if (distanceSq < bestDistance)
			{
				bestIndex = i;
				bestDistance = distanceSq;

				
				junctionIntersection = isInSegment ? testPoint : shapePoint1;
			}
		}

		Vector3 rightVector = (junction.Shape[bestIndex] - junction.Shape[(bestIndex + 1) % shapeLength]).normalized * laneWidth;

		Vector3 fDirection = Vector3.Cross((shapePoint1 - shapePoint2), Vector3.up);

		rightVector = Vector3.Scale(ImportHelper.Abs(rightVector), ImportHelper.Sign(fDirection));
		

		vertices[isStart ? 0 : vertices.Length - 1] = junctionIntersection + rightVector;
		vertices[isStart ? 1 : vertices.Length - 2] = junctionIntersection - rightVector;
    }

    private void AddStreetLights(NetFileLane netFileLane)
    {
        if(netFileLane.Allow.Contains("pedestrian"))
        {
            return;
        }
        if(netFileLane.Length < 2.0f) return;
        
        float streetLightSpace = 25.0f;
        int numberOfLights = Mathf.FloorToInt(netFileLane.Length / streetLightSpace);
        
        float distanceToNext = (netFileLane.Length - (numberOfLights * streetLightSpace)) / 2.0f;
        numberOfLights++;

        for(int i = 1; i < netFileLane.Shape.Length; i++)
        {
            Vector3 startPoint = netFileLane.Shape[i-1];
            Vector3 endPoint = netFileLane.Shape[i];

            Vector3 direction = endPoint - startPoint;
            float length = direction.magnitude;
            direction /= length;
            
            float currentLength = 0;

            while(currentLength + distanceToNext <= length)
            {
                currentLength += distanceToNext;
				distanceToNext = streetLightSpace;

                Vector3 orthogonal = Vector3.Cross(direction, Vector3.up).normalized;
                // Vector3 origin = startPoint + direction * currentLength + orthogonal * 3.5f;
				Vector3 origin = startPoint + direction * currentLength + orthogonal * 5.5f;

                GameObject streetLightPrefab = Resources.Load<GameObject>(GameStatics.streetLightPath);
                GameObject streetLightObj = GameObject.Instantiate(streetLightPrefab);
                streetLightObj.name = $"StreetLight {sLCount++}";
                streetLightObj.name = $"StreetLight {netFileLane.Id}";
        
                streetLightObj.transform.position = origin;
                streetLightObj.transform.rotation = Quaternion.FromToRotation(streetLightObj.transform.right, -direction);
            }
            distanceToNext -= (length - currentLength);
        }
    }

    private void LoadAndGenerateEnvironment(string shapesFilePath)
    {
        additionalType additional = ImportHelper.LoadXMLFile<additionalType>(shapesFilePath);

        if (additional is null)
		{
			Debug.Log("poly.xml is invalid.");
			return;
		}

        int invalidTypesCount = 0;
        foreach (object item in additional.Items)
		{
			switch(item)
			{
				case polygonType p:
				    AddPolygonType(p);
					break;

				case poiType p:
					// AddPOI(p);
					break;

				case parkingAreaType p:
					break;

				default:
					invalidTypesCount++;
					break;
			}
		}
    }

    private void AddPolygonType(polygonType p)
    {
        bool building = p.type.ToLower().Contains("building");
        if(building)
        {
            GameObject newBuilding = new GameObject();
            newBuilding.AddComponent<MeshFilter>();
            newBuilding.AddComponent<MeshRenderer>();
            DynamicBuilding build = newBuilding.AddComponent<DynamicBuilding>();
            build.CreateBuilding(p, random.Next(int.MaxValue), sumoOffset);
        }
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
