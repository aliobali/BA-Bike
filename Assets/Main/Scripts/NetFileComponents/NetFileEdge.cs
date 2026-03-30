using UnityEngine;
using System.Collections.Generic;

namespace Env3d.SumoImporter.NetFileComponents
{
    public class NetFileEdge
    {
        public readonly string id;
        public NetFileJunction from { get; }
        public NetFileJunction to { get; }
        public int priority { get; }
        public List<NetFileLane> lanes { get; }        

        public NetFileEdge(edgeType edge, ref Dictionary<string, NetFileJunction> existingJunctions)
        {
            this.id = edge.id;
            this.priority = int.Parse(edge.priority, GameStatics.provider);

            this.lanes = new List<NetFileLane>();

            this.from = existingJunctions[edge.from];
            this.to = existingJunctions[edge.to];
        }

        public void AddLane(laneType lane, ref Dictionary<string, NetFileLane> existingLanes)
        {
            this.lanes.Add(new NetFileLane(lane));
            // existingLanes should already contain all lanes,
            // but they were previously only created from their respective
            // lane index.
            existingLanes[lane.id].Update(lane);
        }
    }
}