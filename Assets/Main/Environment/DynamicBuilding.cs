using Env3d.SumoImporter;
using UnityEngine;
using Env3d.SumoImporter.NetFileComponents;
using System;
using System.Collections.Generic;

public class DynamicBuilding: MonoBehaviour
{
    private Material wallMaterial1;
    private Material wallMaterial2;
    private Material wallMaterial3;

    private System.Random random;

    // Assets
    private GameObject windowPrefab;

    // Building set 1
    private Material wallMaterialBT2;
	private Material wallMaterialBT3;
	private Material wallMaterialBT4;
	private Material wallMaterialBT5;
    private Material wallWindowMaterialBT2;
	private Material wallWindowMaterialBT3;
	private Material wallWindowMaterialBT4;
	private Material wallWindowMaterialBT5;
    private GameObject WindowSet1Large;
    private GameObject WindowSet1Two;

    public void CreateBuilding(polygonType p, int Seed, Vector3 sumoOffset)
    {
        random = new System.Random(Seed);
        CreateMeshFromPolygonType(p, sumoOffset);
    }

	private void CreateMeshFromPolygonType(polygonType p, Vector3 sumoOffset)
	{
		PolygonBaseShape shape;
		Material pMaterial = GetRandomWallMaterial();
		Vector3[] shapePoints = ImportHelper.ConvertShapeString(p.shape);
		Vector3 buildingCenter = Vector3.zero;

		for(var i = 0; i < shapePoints.Length; i++)
		{
			shapePoints[i] -= sumoOffset;
		}

		foreach(Vector3 point in shapePoints)
		{
			buildingCenter += point;
		}

		buildingCenter /= shapePoints.Length;
		gameObject.transform.position = buildingCenter;

		// Center the shape points
		for(int i = 0; i < shapePoints.Length; i++)
		{
			shapePoints[i] -= buildingCenter;
		}

		// polygon not a fill and includes lines
		/*
		if(p.fill == boolType.Item0 && p.lineWidth != null)
		{
			Vector3[] orderedVs = new Vector3[shapePoints.Length * 2 + 1];

			for(int i = 0; i < shapePoints.Length; i++)
			{
				Vector3 nextPoint = shapePoints[(i + 1) % shapePoints.Length];
				Vector3 currentPoint = shapePoints[i];
				Vector3 lastPoint = shapePoints[(i - 1 + shapePoints.Length) % shapePoints.Length];

				Vector3 direction = nextPoint - lastPoint;
				Vector3 nOrthogonal = Vector3.Cross(Vector3.up, direction).normalized;

				orderedVs[i] = currentPoint - nOrthogonal * float.Parse(p.lineWidth, GameStatics.provider);
				orderedVs[2 * shapePoints.Length - 1 -i] = currentPoint + nOrthogonal * float.Parse(p.lineWidth, GameStatics.provider);
			}
			orderedVs[orderedVs.Length - 1] = orderedVs[0];
			shape = new PolygonBaseShape(p.id, p.type, new List<Vector3>(orderedVs));
		}
		*/

		if(p.fill != boolType.Item0)
		{
			shape = new PolygonBaseShape(p.id, p.type, new List<Vector3>(shapePoints));
			shape.FixOrder();
		}
		else
		{
			Debug.LogError($"Polygon with ID {p.id} has neither fill nor lineWidth");
			return;
		}

		List<Vector3> verticesList = new List<Vector3>();
		List<Vector2> uvsList = new List<Vector2>();
		List<int> trianglesList = new List<int>();

        float wallHeight = random.Next(5, 10) * 2.5f;

        List<List<Vector3>> fillWalls = new List<List<Vector3>>();

		for (int i = 0; i < shape.Count; i++)
		{
			Vector3 startPos = shape[i];
			Vector3 endPos = shape[(i + 1) % shape.Count];

			Vector3 direction = endPos - startPos;
			float wallLength = direction.magnitude;

			if (wallLength >= 4.0f)
			{
				int numberOfWindows = Mathf.FloorToInt(wallLength / 4.0f);
				float offset = (wallLength - numberOfWindows * 4) / 2.0f + 2.0f;
				bool wasLast = false;

				for (int j = 0; j < numberOfWindows; j++)
				{
					if (!wasLast && random.NextDouble() < 0.33)
					{
						Vector3 start = startPos + direction / wallLength * ((offset-2.0f) + j * 4);
						Vector3 end = startPos + direction / wallLength * ((offset+2.0f) + j * 4);
						List<Vector3> fillWall = new List<Vector3>();
						fillWall.Add(start);
						fillWall.Add(end);
						fillWalls.Add(fillWall);
						wasLast = true;
					}
					else
					{
						wasLast = false;
						Vector3 orign = startPos + direction / wallLength * (offset + j * 4);   
						Quaternion rotation;

						for (float k = 0; k < wallHeight; k = k + 2.5f)
						{
							WindowSet1Large = Resources.Load<GameObject>(GameStatics.buildingPath + "/Set1/building_assets_set1_WallOneWindowLarge");
							WindowSet1Two = Resources.Load<GameObject>(GameStatics.buildingPath + "/Set1/building_assets_set1_WallTwoWindows");
							GameObject windowWall = random.Next(2) == 0 ? GameObject.Instantiate(WindowSet1Two) : GameObject.Instantiate(WindowSet1Large);
							rotation = Quaternion.FromToRotation(windowWall.transform.right, direction);
							windowWall.transform.position = gameObject.transform.position + orign + Vector3.up * k;
							windowWall.transform.rotation = rotation;

							Renderer renderer = windowWall.GetComponent<Renderer>();
							Material[] mats = renderer.materials;
							for(var l = 0; l < mats.Length; l++)
							{
								if(mats[l].name.Contains("Wall"))
								{
									mats[l] = pMaterial;
									renderer.materials = mats;
								}
							}
						}
					}
				}
				
				List<Vector3> fillWallStart = new List<Vector3>();
				fillWallStart.Add(startPos);
				fillWallStart.Add(startPos + direction / wallLength * (offset - 2.0f));
				fillWalls.Add(fillWallStart);

				List<Vector3> fillWallEnd = new List<Vector3>();
				fillWallEnd.Add(endPos - direction / wallLength * (offset - 2.0f));
				fillWallEnd.Add(endPos);
				fillWalls.Add(fillWallEnd);
			}
			else
			{
				List<Vector3> fillWall = new List<Vector3>();
				fillWall.Add(startPos);
				fillWall.Add(endPos);
				fillWalls.Add(fillWall);
			}
		}

		foreach(List<Vector3> fillWall in fillWalls)
		{
			BuildWall(fillWall.ToArray(), wallHeight, ref verticesList, ref trianglesList, ref uvsList);
		}

        Vector3[] vertices = verticesList.ToArray();
		Vector2[] uvs = uvsList.ToArray();
		int[] triangles = trianglesList.ToArray();

		Vector3[] normals;
		float[] tangents;
		ImportHelper.CalculateNormals(vertices, triangles, uvs, out normals, out tangents);
        Mesh mesh = new Mesh();
		ImportHelper.AddMesh(gameObject, mesh, vertices, uvs, triangles, null, pMaterial);
	}

    private void BuildWall(Vector3[] shape, float Height, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uvs)
	{
		float totalWallLength = 0;

		int triangleOffset = vertices.Count;

		for (int i = 0; i < shape.Length; i++)
		{
			Vector3 startPos = shape[i];
			Vector3 endPos = shape[(i + 1) % shape.Length];

			Vector3 direction = endPos - startPos;
			float wallLength = direction.magnitude;

			Vector3 wallTopPoint = startPos + new Vector3(0, Height, 0);
			vertices.Add(startPos);
			vertices.Add(wallTopPoint);
		
			uvs.Add(new Vector2(totalWallLength, 0));
			uvs.Add(new Vector2(totalWallLength, Height / 2.0f));

			totalWallLength += wallLength / 2.0f;

			if (i < shape.Length - 1)
			{
				triangles.Add(triangleOffset + i * 2 + 0);
				triangles.Add(triangleOffset + i * 2 + 2);
				triangles.Add(triangleOffset + i * 2 + 1);

				
				triangles.Add(triangleOffset + i * 2 + 1);
				triangles.Add(triangleOffset + i * 2 + 2);
				triangles.Add(triangleOffset + i * 2 + 3);
				
			}
		}
	}

    private Material GetRandomWallMaterial()
	{
		switch (random.Next(4))
		{
			case 0:
				return wallMaterialBT2;
			case 1:
				return wallMaterialBT3;
			case 2:
				return wallMaterialBT4;
			case 3:
				return wallMaterialBT5;
		}
		switch (random.Next(3))
		{
			case 0:
				return wallMaterial1;
			case 1:
				return wallMaterial2;
			case 2:
				return wallMaterial3;
			default:
				return wallMaterial1;
		}
	}

    void Awake()
    {
        wallMaterial1 = Resources.Load<Material>(@"Environment/Materials/M_Wall1");
        wallMaterial2 = Resources.Load<Material>(@"Environment/Materials/M_Wall2");
        wallMaterial3 = Resources.Load<Material>(@"Environment/Materials/M_Wall3");

        // Assets
        windowPrefab = Resources.Load<GameObject>(GameStatics.buildingPath + "/Window");

        // Building set 1
        wallMaterialBT2 = Resources.Load<Material>(GameStatics.buildingPath + "/Set1/Wall_Yellow");
        wallMaterialBT3 = Resources.Load<Material>(GameStatics.buildingPath + "/Set1/Wall_Grey");
        wallMaterialBT4 = Resources.Load<Material>(GameStatics.buildingPath + "/Set1/Wall_Brown");
        wallMaterialBT5 = Resources.Load<Material>(GameStatics.buildingPath + "/Set1/Wall_DarkYellow");
        wallWindowMaterialBT2 = Resources.Load<Material>(GameStatics.buildingPath + "/Set1/WallWindow_Yellow.material");
        wallWindowMaterialBT3 = Resources.Load<Material>(GameStatics.buildingPath + "/Set1/WallWindow_Grey.material");
        wallWindowMaterialBT4 = Resources.Load<Material>(GameStatics.buildingPath + "/Set1/WallWindow_Brown.material");
        wallWindowMaterialBT5 = Resources.Load<Material>(GameStatics.buildingPath + "/Set1/WallWindow_DarkYellow.material");
    }

}