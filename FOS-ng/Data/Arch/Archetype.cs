using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FOS_ng.Structures;

namespace FOS_ng.Data.Arch
{
    public class Archetype
    {
        public enum ObjectType
        {
            // ReSharper disable once InconsistentNaming
            NONE = 0,
            // ReSharper disable once InconsistentNaming
            MOON = 1 << 0,
            // ReSharper disable once InconsistentNaming
            PLANET = 1 << 1,
            // ReSharper disable once InconsistentNaming
            SUN = 1 << 2,
            // ReSharper disable once InconsistentNaming
            BLACKHOLE = 1 << 3,
            // ReSharper disable once InconsistentNaming
            SATELLITE = 1 << 4,
            // ReSharper disable once InconsistentNaming
            DOCKING_RING = 1 << 5,
            // ReSharper disable once InconsistentNaming
            JUMP_GATE = 1 << 6,
            // ReSharper disable once InconsistentNaming
            TRADELANE_RING = 1 << 7,
            // ReSharper disable once InconsistentNaming
            STATION = 1 << 8,
            // ReSharper disable once InconsistentNaming
            WAYPOINT = 1 << 9,
            // ReSharper disable once InconsistentNaming
            AIRLOCK_GATE = 1 << 10,
            // ReSharper disable once InconsistentNaming
            JUMP_HOLE = 1 << 11,
            // ReSharper disable once InconsistentNaming
            WEAPONS_PLATFORM = 1 << 12,
            // ReSharper disable once InconsistentNaming
            DESTROYABLE_DEPOT = 1 << 13,
            // ReSharper disable once InconsistentNaming
            NON_TARGETABLE = 1 << 14,
            // ReSharper disable once InconsistentNaming
            MISSION_SATELLITE = 1 << 15,
            // ReSharper disable once InconsistentNaming
            FIGHTER = 1 << 16,
            // ReSharper disable once InconsistentNaming
            FREIGHTER = 1 << 17,
            // ReSharper disable once InconsistentNaming
            GUNBOAT = 1 << 18,
            // ReSharper disable once InconsistentNaming
            CRUISER = 1 << 19,
            // ReSharper disable once InconsistentNaming
            TRANSPORT = 1 << 20,
            // ReSharper disable once InconsistentNaming
            CAPITAL = 1 << 21,
            // ReSharper disable once InconsistentNaming
            MINING = 1 << 22,
            // ReSharper disable once InconsistentNaming
            GUIDED = 1 << 24,
            // ReSharper disable once InconsistentNaming
            BULLET = 1 << 25,
            // ReSharper disable once InconsistentNaming
            MINE = 1 << 26,
            // ReSharper disable once InconsistentNaming
            LOOT = 1 << 28,
            // ReSharper disable once InconsistentNaming
            ASTEROID = 1 << 29
        }

        public uint ArchetypeID;
        public List<DockingPoint> DockingPoints = new List<DockingPoint>();
        public Dictionary<string, HardpointData> Hardpoints;
        public float HitPts;
        public HardpointData HpMount = null;
        public float Mass;
        public string ModelPath;
        public string Nickname;
        public string LootAppearance;
        public string PodAppearance;
        //public OrientedBoundingBox Obb; // fixme: get from SUR
        public double Radius = 10; // fixme: get from SUR
        public UInt16 SmallID;
        //public List<OrientedBoundingBox> Subobbs = new List<OrientedBoundingBox>(); // fixme: add all SUR subobject OBBs
        public ObjectType Type;

        public string Loadout;

        public Archetype(string datapath, FLDataFile.Section sec)
        {
            Nickname = sec.GetFirstOf("nickname")[0];
            ArchetypeID = Utilities.CreateID(Nickname);

            // Load the hardpoints and animation data for the model
            Hardpoints = new Dictionary<string, HardpointData>();

            if (sec.ContainsAnyOf("loot_appearance"))
            {
                LootAppearance = sec.GetFirstOf("loot_appearance")[0];
            }
            if (sec.ContainsAnyOf("pod_appearance"))
            {
                PodAppearance = sec.GetFirstOf("pod_appearance")[0];
            }

            if (sec.ContainsAnyOf("DA_archetype"))
            {
                ModelPath = datapath + Path.DirectorySeparatorChar + sec.GetFirstOf("DA_archetype")[0];
                var utf = new UTFFile();
                TreeNode root = utf.LoadUTFFile(ModelPath);

                CmpFixData fix;
                CmpRevData rev;
                CmpPrisData pris;
                CmpSphereData sphere;

                var file_to_object = new Dictionary<string, string>();
                var cons_parent = new Dictionary<string, string>();
                var cons_position = new Dictionary<string, Vector>();
                var cons_orientation = new Dictionary<string, Matrix>();

                if (root.Nodes.ContainsKey("Cmpnd"))
                {
                    TreeNode cmpnd = root.Nodes["Cmpnd"];

                    foreach (TreeNode n in cmpnd.Nodes)
                    {
                        if (n.Name == "Cons")
                        {
                            TreeNode cons = n;
                            if (cons.Nodes.ContainsKey("Fix"))
                            {
                                TreeNode fixNode = cons.Nodes["Fix"];
                                try
                                {
                                    fix = new CmpFixData(fixNode.Tag as byte[]);
                                    foreach (CmpFixData.Part p in fix.Parts)
                                    {
                                        cons_parent[p.ChildName] = p.ParentName;
                                        cons_position[p.ChildName] = new Vector(p.OriginX, p.OriginY, p.OriginZ);
                                        var m = new Matrix
                                        {
                                            M00 = p.RotMatXX,
                                            M01 = p.RotMatXY,
                                            M02 = p.RotMatXZ,
                                            M10 = p.RotMatYX,
                                            M11 = p.RotMatYY,
                                            M12 = p.RotMatYZ,
                                            M20 = p.RotMatZX,
                                            M21 = p.RotMatZY,
                                            M22 = p.RotMatZZ
                                        };

                                        cons_orientation[p.ChildName] = m;
                                    }
                                }
                                catch (Exception)
                                {
                                    fix = null;
                                }
                            }

                            if (cons.ContainsKey("Pris"))
                            {
                                TreeNode prisNode = cons["Pris"];
                                try
                                {
                                    pris = new CmpPrisData(prisNode.Tag as byte[]);
                                    foreach (CmpPrisData.Part p in pris.Parts)
                                    {
                                        cons_parent[p.ChildName] = p.ParentName;
                                        cons_position[p.ChildName] = new Vector(p.OriginX, p.OriginY, p.OriginZ);
                                        var m = new Matrix
                                        {
                                            M00 = p.RotMatXX,
                                            M01 = p.RotMatXY,
                                            M02 = p.RotMatXZ,
                                            M10 = p.RotMatYX,
                                            M11 = p.RotMatYY,
                                            M12 = p.RotMatYZ,
                                            M20 = p.RotMatZX,
                                            M21 = p.RotMatZY,
                                            M22 = p.RotMatZZ
                                        };

                                        cons_orientation[p.ChildName] = m;
                                    }
                                }
                                catch (Exception)
                                {
                                    pris = null;
                                }
                            }

                            if (cons.Nodes.ContainsKey("Rev"))
                            {
                                TreeNode revNode = cons.Nodes["Rev"];
                                try
                                {
                                    rev = new CmpRevData(revNode.Tag as byte[]);
                                    foreach (CmpRevData.Part p in rev.Parts)
                                    {
                                        cons_parent[p.ChildName] = p.ParentName;
                                        cons_position[p.ChildName] = new Vector(p.OriginX, p.OriginY, p.OriginZ);
                                        var m = new Matrix
                                        {
                                            M00 = p.RotMatXX,
                                            M01 = p.RotMatXY,
                                            M02 = p.RotMatXZ,
                                            M10 = p.RotMatYX,
                                            M11 = p.RotMatYY,
                                            M12 = p.RotMatYZ,
                                            M20 = p.RotMatZX,
                                            M21 = p.RotMatZY,
                                            M22 = p.RotMatZZ
                                        };

                                        cons_orientation[p.ChildName] = m;
                                    }
                                }
                                catch (Exception)
                                {
                                    rev = null;
                                }
                            }

                            if (cons.Nodes.ContainsKey("Sphere"))
                            {
                                TreeNode sphereNode = cons.Nodes["Sphere"];
                                try
                                {
                                    sphere = new CmpSphereData(sphereNode.Tag as byte[]);
                                    foreach (CmpSphereData.Part p in sphere.Parts)
                                    {
                                        cons_parent[p.ChildName] = p.ParentName;
                                        cons_position[p.ChildName] = new Vector(p.OriginX, p.OriginY, p.OriginZ);
                                        var m = new Matrix
                                        {
                                            M00 = p.RotMatXX,
                                            M01 = p.RotMatXY,
                                            M02 = p.RotMatXZ,
                                            M10 = p.RotMatYX,
                                            M11 = p.RotMatYY,
                                            M12 = p.RotMatYZ,
                                            M20 = p.RotMatZX,
                                            M21 = p.RotMatZY,
                                            M22 = p.RotMatZZ
                                        };

                                        cons_orientation[p.ChildName] = m;
                                    }
                                }
                                catch (Exception)
                                {
                                    sphere = null;
                                }
                            }
                        }
                        else
                        {
                            if (n.Nodes.ContainsKey("Object name") && n.Nodes.ContainsKey("File name"))
                                file_to_object.Add(Utilities.GetString(n.Nodes["File name"]),
                                    Utilities.GetString(n.Nodes["Object name"]));
                        }
                    }
                }

                foreach (TreeNode hp in utf.Hardpoints.Nodes)
                {
                    TreeNode hpnode = null, parentnode = null;
                    TreeNode[] matches = root.Nodes.Find(hp.Name, true);

                    foreach (TreeNode m in matches)
                    {
                        hpnode = m;
                        parentnode = hpnode.Parent.Parent.Parent;
                        if (file_to_object.ContainsKey(parentnode.Name))
                            break;
                    }

                    if (hpnode == null)
                        continue;

                    var hpd = new HardpointData(hpnode);

                    if (parentnode != root)
                    {
                        string consName = file_to_object[parentnode.Name];

                        var positionOffset = new Vector();
                        var rotationOffset = new Matrix();
                        while (consName.ToLowerInvariant() != "root")
                        {
                            positionOffset += cons_position[consName];
                            rotationOffset *= cons_orientation[consName];

                            consName = cons_parent[consName];
                        }

                        hpd.Position += positionOffset;
                        hpd.Rotation *= rotationOffset;
                    }

                    Hardpoints[hpd.Name.ToLowerInvariant()] = hpd;
                    if (hpd.Name.ToLowerInvariant() == "hpmount")
                        HpMount = hpd;
                }
            }

            if (ModelPath != null)
            {
                string surPath = Path.ChangeExtension(ModelPath, ".sur");
                if (File.Exists(surPath))
                {
                    try
                    {
                        new SurFile(surPath);
                    }
                    catch
                    {
                        log.AddLog(LogType.ERROR, "sur load failed for " + surPath);
                    }
                }
            }

            if (sec.SettingExists("mass"))
                Mass = sec.GetSetting("mass").Float(0);

            if (sec.SettingExists("hit_pts"))
                HitPts = sec.GetSetting("hit_pts").Float(0);

            if (sec.SettingExists("type"))
            {
                try
                {
                    Type =
                        (ObjectType)Enum.Parse(typeof(ObjectType), sec.GetSetting("type").Str(0).ToUpperInvariant());
                }
                catch (Exception)
                {
                    Type = ObjectType.NONE;
                }
            }

            // Load the docking points for the model
            foreach (FLDataFile.Setting set in sec.Settings)
            {
                if (set.SettingName == "docking_sphere")
                {
                    string moortype = set.Str(0).ToLowerInvariant();
                    string hpname = set.Str(1).ToLowerInvariant();
                    float dockingRadius = set.Float(2);
                    AddDockingPoint(moortype, hpname, dockingRadius, Hardpoints, log);
                }
            }

            if (sec.SettingExists("loadout")) Loadout = sec.GetSetting("loadout").Str(0);

            // If this is a tradelane ring, load the docking points for it
            if (Type == ObjectType.TRADELANE_RING)
            {
                AddDockingPoint("tradelane_ring", "hpleftlane", 0, Hardpoints, log);
                AddDockingPoint("tradelane_ring", "hprightlane", 0, Hardpoints, log);
            }

            // FIXME: Load or build and surface used for collision detection for this model.
        }

        private void AddDockingPoint(string type, string hpname, float dockingRadius,
            Dictionary<string, HardpointData> hardpoints, ILogController log)
        {
            if (!hardpoints.ContainsKey(hpname))
            {
                log.AddLog(LogType.ERROR, "error: hardpoint not found arch={0} hpname={1}", Nickname, hpname);
                return;
            }

            var dp = new DockingPoint
            {
                Type =
                    (DockingPoint.DockingSphere)
                        Enum.Parse(typeof(DockingPoint.DockingSphere), type.ToUpperInvariant()),
                HpName = hpname,
                DockingRadius = dockingRadius,
                Position = hardpoints[hpname].Position,
                Rotation = hardpoints[hpname].Rotation
            };
            DockingPoints.Add(dp);
        }
    }
}
