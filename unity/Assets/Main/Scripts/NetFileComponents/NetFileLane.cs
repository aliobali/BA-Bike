using UnityEngine;
using Env3d.SumoImporter;

namespace Env3d.SumoImporter.NetFileComponents
{
    public class NetFileLane
    {
        public string Id {get; }
        public string Allow {get; private set;}
        public string Disallow {get; private set;}
        public int Index {get; private set;}
        public float Speed {get; private set;}
        public float Length {get; private set;}
        public float Width {get; private set;}
        public Vector3[] Shape {get; private set;}
        
        public NetFileLane(string id)
        {
            Id = id;
        }

        public NetFileLane(laneType lane)
        {
            Id = lane.id;
            Index = int.Parse(lane.index, GameStatics.provider);
            Speed = lane.speed;
            Length = lane.length;
            Width = lane.width > .1f ? lane.width : 3.2f;
            Allow = lane.allow;
            Disallow = lane.disallow;
            Shape = ImportHelper.ConvertShapeString(lane.shape);
        }

        // Sometimes we only get the lane id as a string and have to update later
        public void Update(laneType lane)
        {
            Index = int.Parse(lane.index, GameStatics.provider);
            Speed = lane.speed;
            Length = lane.length;
            Width = lane.width > .1f ? lane.width : 3.2f;
            Allow = lane.allow;
            Disallow = lane.disallow;
            Shape = ImportHelper.ConvertShapeString(lane.shape);
        }
    }
}