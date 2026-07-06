using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Env3d.SumoImporter;

namespace Env3d.SumoImporter.NetFileComponents
{
	public class NetFileJunction
	{
		public string Id { get; }
		public junctionTypeType JunctionType { get; }
		public Vector3 Location { get; }
		public List<NetFileLane> IncomingLanes { get; }
		public List<NetFileLane> InternalLanes { get; }
		public Vector3[] Shape { get; }

		public NetFileJunction(junctionType junction, ref Dictionary<string, NetFileLane> existingLanes)
		{
			this.Id = junction.id;
			this.JunctionType = junction.type;
			this.Location = new Vector3(
				float.Parse(junction.x, GameStatics.provider),
				float.Parse(junction.y, GameStatics.provider),
				float.Parse(junction.z ?? "0", GameStatics.provider)
			);

			// Get incoming Lanes
			this.IncomingLanes= new List<NetFileLane>();
			foreach(string stringPiece in junction.incLanes.Split(' '))
			{
				NetFileLane l = new NetFileLane(stringPiece);
				this.IncomingLanes.Add(l);

				// Add to global list
				if(!existingLanes.ContainsKey(l.Id))
					existingLanes.Add(l.Id, l);
			}

			// Get internal Lanes
			this.InternalLanes = new List<NetFileLane>();
			foreach (string stringPiece in junction.intLanes.Split(' '))
			{
				NetFileLane l = new NetFileLane(stringPiece);
				this.InternalLanes.Add(l);

				// Add to global list
				if (!existingLanes.ContainsKey(l.Id))
					existingLanes.Add(l.Id, l);
			}

			// necessary when the os seperates decimals with , instead of . 
			NumberFormatInfo provider = new NumberFormatInfo();
			provider.NumberDecimalSeparator = ".";

			// Get shape coordinates as List of tuple-arrays
			this.Shape = ImportHelper.ConvertShapeString(junction.shape);
		}
		
	}
}
