using System;
using System.Collections.Generic;
using System.Linq;
using FLServer.DataWorkers;
using FLServer.Solar;
using FLServer.Physics;
namespace FLServer.Object.Solar
{

    public class StarSystem
    {
        private readonly Dictionary<uint, PathfindingNode> _pathfinding = new Dictionary<uint, PathfindingNode>();
        private readonly Dictionary<uint, uint> _tradelanes = new Dictionary<uint, uint>();
        public List<Solar> Gates = new List<Solar>();
        public string Nickname;
        public Dictionary<uint, Solar> Solars = new Dictionary<uint, Solar>();
        public uint SystemID;

        public List<Zone> Zones = new List<Zone>();

        public Solar FindGateTo(uint systemid)
        {
            //foreach (Solar s in Gates)
            //    if (s.DestinationSystemid == systemid && s.Arch.Type == Archetype.ObjectType.JUMP_GATE)
            //        return s;
            return Gates.FirstOrDefault(s => s.DestinationSystemid == systemid && s.Arch.Type == Archetype.ObjectType.JUMP_GATE);
        }

        public Solar FindHoleTo(uint systemid)
        {
            //foreach (Solar s in Gates)
            //    if (s.DestinationSystemid == systemid && s.Arch.Type == Archetype.ObjectType.JUMP_HOLE)
            //        return s;
            return Gates.FirstOrDefault(s => s.DestinationSystemid == systemid && s.Arch.Type == Archetype.ObjectType.JUMP_HOLE);
        }

        public void CalculatePathfinding()
        {
            foreach (var s in Solars)
            {
                if (s.Value.Arch.Type == Archetype.ObjectType.TRADELANE_RING &&
                    (s.Value.PrevRing == 0 || s.Value.NextRing == 0))
                    _pathfinding.Add(s.Key, new PathfindingNode(s.Key));
            }

            foreach (var node1 in _pathfinding)
            {
                foreach (var node2 in _pathfinding)
                {
                    Solar s1 = Solars[node1.Key], s2 = Solars[node2.Key];

                    var d = s1.Position.DistanceTo(s2.Position);

                    if (s1.Arch.Type == Archetype.ObjectType.TRADELANE_RING &&
                        s2.Arch.Type == Archetype.ObjectType.TRADELANE_RING)
                    {
                        var temp = s1;
                        if (temp.PrevRing != 0)
                        {
                            while (temp.PrevRing != 0)
                                temp = Solars[temp.PrevRing];
                        }
                        else
                        {
                            while (temp.NextRing != 0)
                                temp = Solars[temp.NextRing];
                        }

                        if (temp.Objid == s2.Objid)
                        {
                            _tradelanes[node1.Key] = node2.Key;
                            _tradelanes[node2.Key] = node1.Key;

                            d /= UniverseDB.TradelaneSpeed;
                        }
                        else
                            d /= UniverseDB.CruiseSpeed;
                    }
                    else
                        d /= UniverseDB.CruiseSpeed;

                    node1.Value.Connections.Add(new PathfindingConnection(node1.Value, node2.Value, d));
                    node2.Value.Connections.Add(new PathfindingConnection(node2.Value, node1.Value, d));
                }
            }
        }

        public List<uint> FindBestPath(Vector origin, Vector destination)
        {
            if (_tradelanes.Count == 0)
                return null;

            var tldist = new Dictionary<uint, double>();
            var tlprev = new Dictionary<uint, uint>();

            foreach (var tl in _tradelanes)
            {
                if (tldist.ContainsKey(tl.Key))
                    continue;

                Vector x1 = Solars[tl.Key].Position, x2 = Solars[tl.Value].Position;

                double t = -(x1 - origin).Dot(x2 - x1) / (x2 - x1).LengthSq();

                double cruiseTravel = 0, tlTravel = (x2 - x1).Length() / UniverseDB.TradelaneSpeed;
                if (t >= 0 && t <= 1)
                {
                    Vector newintersect = x1 + (x2 - x1) * t;
                    cruiseTravel = (newintersect - origin).Length();
                }
                else if (t < 0)
                {
                    t = 0;
                    cruiseTravel = x1.DistanceTo(origin);
                }
                else if (t > 1)
                {
                    t = 1;
                    cruiseTravel = x2.DistanceTo(origin);
                }

                tlprev.Add(tl.Key, 0);
                tlprev.Add(tl.Value, 0);
                tldist.Add(tl.Key, cruiseTravel / UniverseDB.CruiseSpeed + tlTravel * t);
                tldist.Add(tl.Value, cruiseTravel / UniverseDB.CruiseSpeed + tlTravel * (1 - t));
            }

            /*double dist = origin.DistanceTo(destination);
            Solar closest = null;
            foreach (KeyValuePair<uint, uint> tl in tradelanes)
            {
                Solar s = solars[tl.Key];
                double newdist = s.position.DistanceTo(destination);
                if (newdist < dist)
                {
                    dist = newdist;
                    closest = s;
                }
            }
            if (closest == null)
                return null; // Cruise directly to destination*/

            var nodes = new HashSet<PathfindingNode>();

            foreach (var ns in _pathfinding)
                nodes.Add(ns.Value);

            while (nodes.Count > 0)
            {
                KeyValuePair<uint, double> u =
                    tldist.Where(x => nodes.Contains(_pathfinding[x.Key]))
                        .Aggregate((p1, p2) => (p1.Value < p2.Value) ? p1 : p2);

                nodes.Remove(_pathfinding[u.Key]);

                if (u.Value == Double.MaxValue)
                    return null; // derp?

                foreach (PathfindingConnection c in _pathfinding[u.Key].Connections)
                {
                    if (nodes.Contains(c.To))
                    {
                        double alt = tldist[c.From.SolarID] + c.Cost;
                        if (alt < tldist[c.To.SolarID])
                        {
                            tldist[c.To.SolarID] = alt;
                            tlprev[c.To.SolarID] = c.From.SolarID;
                        }
                    }
                }
            }

            var objects = new List<uint>();
            {
                uint u = 0;
                double mindist = Double.MaxValue;

                foreach (var tl in tldist)
                {
                    var s = Solars[tl.Key];
                    double dist = s.Position.DistanceTo(destination) / UniverseDB.CruiseSpeed + tl.Value;
                    if (dist < mindist)
                    {
                        u = tl.Key;
                        mindist = dist;
                    }
                }

                while (tlprev[u] != 0)
                {
                    objects.Add(u);
                    u = tlprev[u];
                }

                objects.Add(u);

                uint lastadded = u;

                uint minu = u;
                mindist = Solars[u].Position.DistanceTo(origin);
                if (Solars[u].PrevRing != 0)
                {
                    while (Solars[u].PrevRing != 0)
                    {
                        u = Solars[u].PrevRing;

                        double d = Solars[u].Position.DistanceTo(origin);
                        if (d < mindist)
                        {
                            minu = u;
                            mindist = d;
                        }
                    }
                }
                else
                {
                    while (Solars[u].NextRing != 0)
                    {
                        u = Solars[u].NextRing;

                        double d = Solars[u].Position.DistanceTo(origin);
                        if (d < mindist)
                        {
                            minu = u;
                            mindist = d;
                        }
                    }
                }

                if (minu == lastadded)
                    objects.Remove(lastadded);
                else
                    objects.Add(minu);
            }

            objects.Reverse();

            return objects;
        }

        protected class PathfindingConnection
        {
            public double Cost;
            public PathfindingNode From = null, To = null;

            public PathfindingConnection(PathfindingNode a, PathfindingNode b, double c)
            {
                From = a;
                To = b;
                Cost = c;
            }
        }

        protected class PathfindingNode
        {
            public List<PathfindingConnection> Connections = new List<PathfindingConnection>();
            public uint SolarID;

            public PathfindingNode(uint id)
            {
                SolarID = id;
            }
        }
    }

}
