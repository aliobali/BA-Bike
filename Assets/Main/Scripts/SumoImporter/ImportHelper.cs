using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using Env3d.SumoImporter.NetFileComponents;

namespace Env3d.SumoImporter
{
    public class ImportHelper
    {
        public static Vector3[] ConvertShapeString(string Shape)
		{
			string[] sPoint = Shape.Split(' ');
			return Array.ConvertAll(sPoint, s => VStringXYToVector(s));
		}

/*
        public static T LoadXMLFile<T>(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (FileStream stream = new FileStream(filePath, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
        }
*/

		public static T LoadXMLFile<T>(string filePath)
        {
            TextAsset xmlAsset = Resources.Load<TextAsset>(filePath);
			if (xmlAsset == null)
			{
				Debug.LogError("Failed to load XML from Resources: " + filePath);
				return default;
			}

			XmlSerializer serializer = new XmlSerializer(typeof(T));
			using (StringReader reader = new StringReader(xmlAsset.text))
			{
				return (T)serializer.Deserialize(reader);
			}
        }

        public static Vector3 VStringXYToVector(string VectorString)
		{
			string[] xzString = VectorString.Split(',');
			Vector3 outVector = new Vector3(
				float.Parse(xzString[0], GameStatics.provider),
				0,
				float.Parse(xzString[1], GameStatics.provider)
			);

			return outVector;
		}

        public static void AddMesh(GameObject mainBody, Mesh meshBody, Vector3[] vertices, Vector2[] uvs, int[] triangles, Texture2D texture, Material material = null)
		{
			meshBody.vertices = vertices;
            meshBody.uv = uvs;
            meshBody.triangles = triangles;

            meshBody.RecalculateNormals();
            meshBody.RecalculateBounds();

            MeshFilter meshFilter = mainBody.GetComponent<MeshFilter>(); // Mesh Holder
            meshFilter.mesh = meshBody; 
            MeshRenderer meshRender = mainBody.GetComponent<MeshRenderer>(); // Texture Renderer
			Shader shader = Shader.Find("Unlit/Texture");

			if(material == null)
			{
				if(shader == null)
				{
					Debug.Log("Shader not found! Using default material instead.");
					meshRender.material = new Material(Shader.Find("Standard"));
				}
				else
				{
					meshRender.material = new Material(shader);
				}
				meshRender.material.mainTexture = texture;	
			}
			else
			{
				meshRender.material = material;
			}
		}

        public static void CalculateNormals(Vector3[] vertices, int[] triangles, Vector2[] uvs, out Vector3[] OutNormals, out float[] tangents)
		{
			OutNormals = new Vector3[vertices.Length];
			tangents = new float[vertices.Length * 4];

			Vector3[] tan1 = new Vector3[vertices.Length];
			Vector3[] tan2 = new Vector3[vertices.Length];

			// Calculate Normals and Tangents for all Sections
			for (int i = 0; i < triangles.Length; i += 3)
			{
				int ia = triangles[i];
				int ib = triangles[i + 1];
				int ic = triangles[i + 2];

				Vector3 e1 = vertices[ia] - vertices[ib];
				Vector3 e2 = vertices[ic] - vertices[ib];

				// Sum up normals
				Vector3 crossP = Vector3.Cross(e1, e2);
				OutNormals[ia] += crossP;
				OutNormals[ib] += crossP;
				OutNormals[ic] += crossP;

				// Sum up tangents
				Vector2 w1 = uvs[ia];
				Vector2 w2 = uvs[ib];
				Vector2 w3 = uvs[ic];

				float s1 = w2.x - w1.x;
				float t1 = w2.y - w1.y;

				float s2 = w3.x - w1.x;
				float t2 = w3.y - w1.y;

				float r = 1.0f / (s1 * t2 - s2 * t1);

				Vector3 sdir = (t2 * e1 - t1 * e2) * r;
				Vector3 tdir = (s1 * e2 - s2 * e1) * r;

				tan1[ia] += sdir;
				tan1[ib] += sdir;
				tan1[ic] += sdir;

				tan2[ia] += tdir;
				tan2[ib] += tdir;
				tan2[ic] += tdir;
			}

			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 n = OutNormals[i].normalized;
				OutNormals[i] = n;


				Vector3 t = tan1[i];
				// Gram-Schmidt orthogonalize
				Vector3 tangent = (t - n * Vector3.Dot(n, t)).normalized;
				tangents[i * 4] = tangent.x;
				tangents[i * 4 + 1] = tangent.y;
				tangents[i * 4 + 2] = tangent.z;
				// Calculate handedness
				tangents[i * 4 + 3] = (Vector3.Dot(Vector3.Cross(n, t), tan2[i]) < 0.0f) ? -1.0f : 1.0f;
			}
		}

		public static bool ClosestPointOnSegment(Vector3 Point, Vector3 StartPoint, Vector3 EndPoint, out Vector3 hitPoint)
		{
			Vector3 Segment = EndPoint - StartPoint;
			Vector3 VectToPoint = Point - StartPoint;

			// Point is before the start point
			float Dot1 = Vector3.Dot(VectToPoint, Segment);
			if(Dot1 <= 0 )
			{
				hitPoint = StartPoint;
				return false;
			}

			// Point is after the end point
			float Dot2 = Vector3.Dot(Segment, Segment);
			if(Dot2 <= Dot1 )
			{
				hitPoint = EndPoint;
				return false;
			}

			// Closest point is within the start and end point
			hitPoint = StartPoint + Segment * (Dot1 / Dot2);
			return true;
		}

		public static Vector3 Abs(Vector3 vec)
		{
			return new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
		}

		public static Vector3 Sign(Vector3 vec)
		{
			return new Vector3(Mathf.Sign(vec.x), Mathf.Sign(vec.y), Mathf.Sign(vec.z));
		}

		public static Vector3 LineIntersection2D(Vector3 StartA, Vector3 DirectionA, Vector3 StartB, Vector3 DirectionB)
		{
			float factor = (DirectionB.x * (StartA.z - StartB.z) - DirectionB.z * (StartA.x - StartB.x)) / (-DirectionB.x * DirectionA.z + DirectionA.x * DirectionB.z);
			return StartA + DirectionA * factor;
		}
    }

    public static class Triangulator
	{
		public static int[] Triangulate(Vector3[] points)
		{
			List<int> indices = new List<int>();

			int n = points.Length;
			if (n < 3)
				return indices.ToArray();

			int[] V = new int[n];
			if (Area(points) > 0)
			{
				for (int v = 0; v < n; v++)
					V[v] = v;
			}
			else
			{
				for (int v = 0; v < n; v++)
					V[v] = (n - 1) - v;
			}

			int nv = n;
			int count = 2 * nv;
			for (int m = 0, v = nv - 1; nv > 2;)
			{
				if ((count--) <= 0)
					return indices.ToArray();

				int u = v;
				if (nv <= u)
					u = 0;
				v = u + 1;
				if (nv <= v)
					v = 0;
				int w = v + 1;
				if (nv <= w)
					w = 0;

				if (Snip(points, u, v, w, nv, V))
				{
					int a, b, c, s, t;
					a = V[u];
					b = V[v];
					c = V[w];
					indices.Add(b);
					indices.Add(a);
					indices.Add(c);
					m++;
					for (s = v, t = v + 1; t < nv; s++, t++)
						V[s] = V[t];
					nv--;
					count = 2 * nv;
				}
			}

			// Unity has a mirrored coordinate system then Godot
			// uncomment when used in godot
			//indices.Reverse();
			return indices.ToArray();
		}

		private static float Area(Vector3[] points)
		{
			int n = points.Length;
			float A = 0.0f;
			for (int p = n - 1, q = 0; q < n; p = q++)
			{
				Vector3 pval = points[p];
				Vector3 qval = points[q];
				A += pval.x * qval.z - qval.x * pval.z;
			}
			return (A * 0.5f);
		}

		private static bool Snip(Vector3[] points, int u, int v, int w, int n, int[] V)
		{
			int p;
			Vector3 A = points[V[u]];
			Vector3 B = points[V[v]];
			Vector3 C = points[V[w]];
			if (Mathf.Epsilon > (((B.x - A.x) * (C.z - A.z)) - ((B.z - A.z) * (C.x - A.x))))
				return false;
			for (p = 0; p < n; p++)
			{
				if ((p == u) || (p == v) || (p == w))
					continue;
				Vector3 P = points[V[p]];
				if (InsideTriangle(A, B, C, P))
					return false;
			}
			return true;
		}

		private static bool InsideTriangle(Vector3 A, Vector3 B, Vector3 C, Vector3 P)
		{
			float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
			float cCROSSap, bCROSScp, aCROSSbp;

			ax = C.x - B.x; ay = C.z - B.z;
			bx = A.x - C.x; by = A.z - C.z;
			cx = B.x - A.x; cy = B.z - A.z;
			apx = P.x - A.x; apy = P.z - A.z;
			bpx = P.x - B.x; bpy = P.z - B.z;
			cpx = P.x - C.x; cpy = P.z - C.z;

			aCROSSbp = ax * bpy - ay * bpx;
			cCROSSap = cx * apy - cy * apx;
			bCROSScp = bx * cpy - by * cpx;

			return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
		}
	}

}
