﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using FLServer.Old;
using FLServer.Physics;


namespace FLServer.DataWorkers
{
	/// <summary>
	///     Freelancer uses short-ids in some messages to reduce the bandwidth used when sending
	///     references to various archetype objects. On receipt of a short-id the corresponding
	///     nickname hash is produced. This class builds the map of short to nickname hashes and
	///     provides methods to convert in both directions. The loading order of archetype objects
	///     from the ini files is very important.
	///     I note that Freelancer is retarded as this mechanism introduce a lot of complexity with
	///     significant reduction in the bandwidth used.
	/// </summary>
	public class ArchetypeDB
	{
		/// <summary>
		///     Archetypes referenced by their nickname hash
		/// </summary>
		private static readonly Dictionary<uint, Archetype> ArchetypesByLargeID = new Dictionary<uint, Archetype>();

		/// <summary>
		///     Archetypes referenced by their small_id (index)
		/// </summary>
		private static readonly List<Archetype> ArchetypesBySmallID = new List<Archetype>();

		/// <summary>
		///     The weapon types database to adjust shield hit factors.
		/// </summary>
		private static readonly Dictionary<uint, WeaponType> WeaponTypes = new Dictionary<uint, WeaponType>();


		/// <summary>
		///     Load the architecture database.
		/// </summary>
		/// <param name="flPath"></param>
		/// <param name="log"></param>
		public static void Load(string flPath, ILogController log)
		{
			log.AddLog(LogType.GENERAL, "Loading archetypes");

			// Load the universe and systems and all other static data
			string flIniPath = flPath + Path.DirectorySeparatorChar + "EXE" + Path.DirectorySeparatorChar +
							   "Freelancer.ini";
			try
			{
				var flIni = new FLDataFile(flIniPath, false);

				string dataPath =
					Path.GetFullPath(Path.Combine(flPath + Path.DirectorySeparatorChar + "EXE",
						flIni.GetSetting("Freelancer", "data path").Str(0)));
				foreach (FLDataFile.Setting entry in flIni.GetSettings("Data", "WeaponModDB"))
					LoadWeaponModDB(dataPath, dataPath + Path.DirectorySeparatorChar + entry.Str(0), log);
				foreach (FLDataFile.Setting entry in flIni.GetSettings("Data", "solar"))
					Load(dataPath, dataPath + Path.DirectorySeparatorChar + entry.Str(0), log);
				foreach (FLDataFile.Setting entry in flIni.GetSettings("Data", "debris"))
					Load(dataPath, dataPath + Path.DirectorySeparatorChar + entry.Str(0), log);
				foreach (FLDataFile.Setting entry in flIni.GetSettings("Data", "asteroids"))
					Load(dataPath, dataPath + Path.DirectorySeparatorChar + entry.Str(0), log);
				foreach (FLDataFile.Setting entry in flIni.GetSettings("Data", "equipment"))
					Load(dataPath, dataPath + Path.DirectorySeparatorChar + entry.Str(0), log);
				foreach (FLDataFile.Setting entry in flIni.GetSettings("Data", "ships"))
					Load(dataPath, dataPath + Path.DirectorySeparatorChar + entry.Str(0), log);
			}
			catch (Exception e)
			{
				log.AddLog(LogType.ERROR, "Error '" + e.Message + "' when parsing '" + flIniPath);
			}

			//Compare our archetype db to flserver's db generated by adoxa's packetdump.
			//List<uint> validids = new List<uint>();
			//byte[] x = File.ReadAllBytes(Properties.Settings.Default.FLPath + "\\EXE\\largeid.dat");
			//for (int i=0; i<x.Length; i+=4)
			//{
			//    uint id = BitConverter.ToUInt32(x, i);
			//    validids.Add(id);
			//}
			//for (int i = 0; i < validids.Count; i++)
			//{
			//    if (validids[i] != archetypes_by_small_id[i].archetypeid)
			//    {
			//        Archetype a1 = Find(validids[i]);
			//        Archetype a2 = archetypes_by_small_id[i];
			//        log.AddLog(String.Format("short id mismatch index={0} validid={1} {2} ourid={3} {4}", i, validids[i], a1.nickname,
			//            a2.archetypeid, a2.nickname));
			//    }
			//}
			//log.AddLog(String.Format("Archetypes loaded, large_count={0} small_count={1} valid_ids={2}", archetypes_by_large_id.Count, archetypes_by_small_id.Count, validids.Count));
		}

		private static void LoadWeaponModDB(string datapath, string path, ILogController log)
		{
			var ini = new FLDataFile(path, true);
			foreach (var sec in ini.Sections)
			{
				string sectionName = sec.SectionName.ToLowerInvariant();
				if (sectionName == "weapontype")
				{
					var wt = new WeaponType {Nickname = sec.GetSetting("nickname").Str(0)};
					foreach (FLDataFile.Setting set in sec.Settings)
					{
						if (set.SettingName.ToLowerInvariant() == "shield_mod")
						{
							uint shieldTypeID = FLUtility.CreateID(set.Str(0));
							float shieldFactor = set.Float(1);
							wt.ShieldMods.Add(shieldTypeID, shieldFactor);
						}
					}
					WeaponTypes[FLUtility.CreateID(wt.Nickname)] = wt;
				}
			}
		}

		private static void AddArchetype(Archetype arch)
		{
			if (!ArchetypesByLargeID.ContainsKey(arch.ArchetypeID))
			{
				arch.SmallID = (UInt16) (ArchetypesByLargeID.Count + 1);
				ArchetypesByLargeID.Add(arch.ArchetypeID, arch);
				ArchetypesBySmallID.Add(arch);
			}
		}

		private static void Load(string datapath, string path, ILogController log)
		{
			var ini = new FLDataFile(path, true);

			// Load elements by dependency order; slower, but avoids issues with file ordering

			// Second order dependencies: motors
			foreach (FLDataFile.Section sec in ini.Sections)
			{
				try
		{
				string sectionName = sec.SectionName.ToLowerInvariant();
				if (sectionName == "motor")
				{
					AddArchetype(new MotorArchetype(datapath, sec, log));
				}
		}
		catch (Exception ex)
		{
			log.AddLog(LogType.ERROR, "ArchetypeDB load error: \"" + ex.Message + "\", item skipped");
		}
			}

			// First order dependencies: munitions
			foreach (var sec in ini.Sections)
			{
				try
{
	var sectionName = sec.SectionName.ToLowerInvariant();
	switch (sectionName)
	{
		case "munition":
			AddArchetype(new MunitionArchetype(datapath, sec, log));
			break;
		case "mine":
			AddArchetype(new MineArchetype(datapath, sec, log));
			break;
		case "countermeasure":
			AddArchetype(new CounterMeasureArchetype(datapath, sec, log));
			break;
	}
}
catch (Exception ex)
{
	log.AddLog(LogType.ERROR, "ArchetypeDB load error: \"" + ex.Message + "\", item skipped");
}
			}

			// Zeroth order dependencies: everything else
			foreach (var sec in ini.Sections)
			{
try
{
	var sectionName = sec.SectionName.ToLowerInvariant();


				switch (sectionName)
				{
					case "ship":
						AddArchetype(new ShipArchetype(datapath, sec, log));
						break;
					case "gun":
						AddArchetype(new GunArchetype(datapath, sec, log));
						break;
					case "countermeasuredropper":
						AddArchetype(new CounterMeasureDropperArchetype(datapath, sec, log));
						break;
					case "minedropper":
						AddArchetype(new MineDropperArchetype(datapath, sec, log));
						break;
					case "shieldgenerator":
						AddArchetype(new ShieldGeneratorArchetype(datapath, sec, log));
						break;
					case "power":
						AddArchetype(new PowerArchetype(datapath, sec, log));
						break;
					case "armor":
						AddArchetype(new ArmorArchetype(datapath, sec, log));
						break;
					case "thruster":
						AddArchetype(new ThrusterArchetype(datapath, sec, log));
						break;
					case "engine":
						AddArchetype(new EngineArchetype(datapath, sec, log));
						break;
					case "repairkit":
						AddArchetype(new RepairKitArchetype(datapath, sec, log));
						break;
					case "shieldbattery":
						AddArchetype(new ShieldBatteryArchetype(datapath, sec, log));
						break;
					case "scanner":
						AddArchetype(new ScannerArchetype(datapath, sec, log));
						break;
					case "simple":
					case "cloakingdevice":
					case "tractor":
					case "shield":
					case "attachedfx":
					case "internalfx":
					case "tradelane":
					case "commodity":
					case "lootcrate":
					case "cargopod":
					case "light":
					case "dynamicasteroid":
					case "asteroidmine":
					case "asteroid":
					case "solar":
						AddArchetype(new Archetype(datapath, sec, log));
						break;
				}
}
catch (Exception ex)
{
	log.AddLog(LogType.ERROR, "ArchetypeDB load error: \"" + ex.Message + "\", item skipped");
}
			}
		}

		public static Archetype Find(uint id)
		{
			if (!ArchetypesByLargeID.ContainsKey(id))
				return null;
			return ArchetypesByLargeID[id];
		}

		public static Archetype FindBySmallID(uint id)
		{
			return ArchetypesBySmallID[(int) id - 1];
		}

		public static WeaponType FindWeaponType(string nickname)
		{
			uint weaponTypeID = FLUtility.CreateID(nickname);
			if (WeaponTypes.ContainsKey(weaponTypeID))
				return WeaponTypes[weaponTypeID];
			return null;
		}
	}

	/// <summary>
	///     Weapon type modifiers from WeaponModDB.ini
	/// </summary>
	public class WeaponType
	{
		/// <summary>
		///     The nickname of the weapon type.
		/// </summary>
		public string Nickname;

		/// <summary>
		///     Map of shield_typeid to shield factor used to determine amount of damage
		///     an energy hit makes to ship.
		/// </summary>
		public Dictionary<uint, float> ShieldMods = new Dictionary<uint, float>();
	}

	public class DockingPoint
	{
		public enum DockingSphere
		{
			// ReSharper disable once InconsistentNaming
			AIRLOCK = 0, // Nothing can dock with this
			// ReSharper disable once InconsistentNaming
			BERTH = 1,
			// ReSharper disable once InconsistentNaming
			RING = 2,
			// ReSharper disable once InconsistentNaming
			MOOR_SMALL = 4,
			// ReSharper disable once InconsistentNaming
			MOOR_MEDIUM = 8,
			// ReSharper disable once InconsistentNaming
			MOOR_LARGE = 16,
			// ReSharper disable once InconsistentNaming
			TRADELANE_RING = 32,
			// ReSharper disable once InconsistentNaming
			JUMP = 64 // Everything can dock with this
		}

		public float DockingRadius;

		public string HpName;
		public Vector Position;
		public Matrix Rotation;
		public DockingSphere Type;
	}

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
		public OrientedBoundingBox Obb; // fixme: get from SUR
		public double Radius = 10; // fixme: get from SUR
		public UInt16 SmallID;
		public List<OrientedBoundingBox> Subobbs = new List<OrientedBoundingBox>(); // fixme: add all SUR subobject OBBs
		public ObjectType Type;

		public string Loadout;

		public Archetype(string datapath, FLDataFile.Section sec, ILogController log)
		{
			Nickname = sec.GetSetting("nickname").Str(0);
			ArchetypeID = FLUtility.CreateID(Nickname);

			// Load the hardpoints and animation data for the model
			Hardpoints = new Dictionary<string, HardpointData>();

			if (sec.SettingExists("loot_appearance"))
			{
				LootAppearance = sec.GetSetting("loot_appearance").Str(0);
			}
			if (sec.SettingExists("pod_appearance"))
			{
				PodAppearance = sec.GetSetting("loot_appearance").Str(0);
			}

			if (sec.SettingExists("DA_archetype"))
			{
				ModelPath = datapath + Path.DirectorySeparatorChar + sec.GetSetting("DA_archetype").Str(0);
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

							if (cons.Nodes.ContainsKey("Pris"))
							{
								TreeNode prisNode = cons.Nodes["Pris"];
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
						(ObjectType) Enum.Parse(typeof (ObjectType), sec.GetSetting("type").Str(0).ToUpperInvariant());
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
						Enum.Parse(typeof (DockingPoint.DockingSphere), type.ToUpperInvariant()),
				HpName = hpname,
				DockingRadius = dockingRadius,
				Position = hardpoints[hpname].Position,
				Rotation = hardpoints[hpname].Rotation
			};
			DockingPoints.Add(dp);
		}
	}

	public class ShipArchetype : Archetype
	{
		public enum MissionProperty
		{
			// ReSharper disable InconsistentNaming
			// file-specific
			CAN_USE_BERTHS = 1,
			CAN_USE_SMALL_MOORS = 4,
			CAN_USE_MED_MOORS = 8,
			CAN_USE_LARGE_MOORS = 16
			// ReSharper restore InconsistentNaming
		}

		/// <summary>
		///     Angular drag is the resistance to the steering torque.
		///     max_rotational_velocity (radian/s) = steering_torque / angular_drag
		/// </summary>
		public Vector AngularDrag;

		public float HoldSize;

		/// <summary>
		///     The force applied to slow the ship.
		/// </summary>
		public float LinearDrag;

		/// <summary>
		///     Mission properties define docking parameters.
		/// </summary>
		public MissionProperty mission_property;

		public uint NanobotLimit;

		/// <summary>
		///     The force applied to move the ship side to side when avoiding rocks.
		/// </summary>
		public float NudgeForce;

		/// <summary>
		///     Rotational inertia is the amount of initial resistance to moving the centerline on both the start and end of a
		///     turn.
		///     Kind of like why a car plows down at the nose when you brake hard. The Inertia controls how snappy or mushy the
		///     craft handles in flight.
		///     rotational_acceleration (radian/s*s) = steering_torque / rotation_inertia;
		/// </summary>
		public Vector RotationInertia;

		public uint ShieldBatteryLimit;

		/// <summary>
		///     Steering torque is the amount of force applied to the centerline of the craft to make it turn;
		/// </summary>
		public Vector SteeringTorque;

		/// <summary>
		///     The force applied to move the ship side to side when strafing keys are pressed.
		/// </summary>
		public float StrafeForce;

		public ShipArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("steering_torque"))
				SteeringTorque = sec.GetSetting("steering_torque").Vector();
			if (sec.SettingExists("angular_drag"))
				AngularDrag = sec.GetSetting("angular_drag").Vector();
			if (sec.SettingExists("rotation_inertia"))
				RotationInertia = sec.GetSetting("rotation_inertia").Vector();
			if (sec.SettingExists("nudge_force"))
				NudgeForce = sec.GetSetting("nudge_force").Float(0);
			if (sec.SettingExists("linear_drag"))
				LinearDrag = sec.GetSetting("linear_drag").Float(0);
			if (sec.SettingExists("hold_size"))
				HoldSize = sec.GetSetting("hold_size").Float(0);
			if (sec.SettingExists("strafe_force"))
				StrafeForce = sec.GetSetting("strafe_force").Float(0);
			if (sec.SettingExists("nanobot_limit"))
				NanobotLimit = sec.GetSetting("nanobot_limit").UInt(0);
			if (sec.SettingExists("shield_battery_limit"))
				ShieldBatteryLimit = sec.GetSetting("shield_battery_limit").UInt(0);
			if (sec.SettingExists("mission_property"))
				mission_property =
					(MissionProperty)
						Enum.Parse(typeof (MissionProperty),
							sec.GetSetting("mission_property").Str(0).ToUpperInvariant());
		}
	}

	public class EquipmentArchetype : Archetype
	{
		public bool Lootable;
		public float UnitsPerContainer;
		public float Volume;

		public EquipmentArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("volume"))
				Volume = sec.GetSetting("volume").Float(0);
			if (sec.SettingExists("units_per_container"))
				UnitsPerContainer = sec.GetSetting("units_per_container").Float(0);
			if (sec.SettingExists("lootable"))
				Lootable = sec.GetSetting("lootable").Str(0) == "true";
		}
	}

	public class ExternalEquipmentArechetype : EquipmentArchetype
	{
		public ExternalEquipmentArechetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
		}
	}

	public class LauncherArchetype : ExternalEquipmentArechetype
	{
		public float DamagePerFire;
		public Vector MuzzleVelocity = new Vector();
		public float PowerUsage;
		public ProjectileArchetype ProjectileArch;
		public float RefireDelay;

		public LauncherArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("damage_per_fire"))
				DamagePerFire = sec.GetSetting("damage_per_fire").Float(0);
			if (sec.SettingExists("power_usage"))
				PowerUsage = sec.GetSetting("power_usage").Float(0);
			if (sec.SettingExists("refire_delay"))
				RefireDelay = sec.GetSetting("refire_delay").Float(0);

			if (sec.SettingExists("muzzle_velocity"))
				MuzzleVelocity = new Vector(0, 0, -sec.GetSetting("muzzle_velocity").Float(0));

			if (sec.SettingExists("projectile_archetype"))
			{
				uint projectileArchID = FLUtility.CreateID(sec.GetSetting("projectile_archetype").Str(0));
				ProjectileArch = ArchetypeDB.Find(projectileArchID) as ProjectileArchetype;
				if (ProjectileArch == null)
					log.AddLog(LogType.ERROR, "error: projectile not found: " + sec.Desc);
			}
		}
	}

	public class GunArchetype : LauncherArchetype
	{
		public float DispersionAngle;
		public float TurnRate;

		public GunArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("dispersion_angle"))
				DispersionAngle = sec.GetSetting("dispersion_angle").Float(0);
			if (sec.SettingExists("turn_rate"))
				TurnRate = sec.GetSetting("turn_rate").Float(0);
		}
	}

	public class MineDropperArchetype : LauncherArchetype
	{
		public MineDropperArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
		}
	}

	public class MineArchetype : ProjectileArchetype
	{
		public float Acceleration;
		public float DetonationDist;
		public float LinearDrag;
		public float SeekerDist;
		public float TopSpeed;

		public MineArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("linear_drag"))
				LinearDrag = sec.GetSetting("linear_drag").Float(0);
			if (sec.SettingExists("detonation_dist"))
				DetonationDist = sec.GetSetting("detonation_dist").Float(0);
			if (sec.SettingExists("seeker_dist"))
				SeekerDist = sec.GetSetting("seeker_dist").Float(0);
			if (sec.SettingExists("acceleration"))
				Acceleration = sec.GetSetting("acceleration").Float(0);
			if (sec.SettingExists("top_speed"))
				TopSpeed = sec.GetSetting("top_speed").Float(0);
		}
	}

	public class ProjectileArchetype : EquipmentArchetype
	{
		public bool ForceGunOri;
		public float Lifetime;
		public float OwnerSafeTime;
		public bool RequiresAmmo;

		public ProjectileArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("lifetime"))
				Lifetime = sec.GetSetting("lifetime").Float(0);
			if (sec.SettingExists("owner_safe_time"))
				OwnerSafeTime = sec.GetSetting("owner_safe_time").Float(0);
			if (sec.SettingExists("requires_ammo"))
				RequiresAmmo = sec.GetSetting("requires_ammo").Str(0) == "true";
			if (sec.SettingExists("force_gun_ori"))
				ForceGunOri = sec.GetSetting("force_gun_ori").Str(0) == "true";
		}
	}

	public class CounterMeasureArchetype : ProjectileArchetype
	{
		public float DiversionPctg;
		public float LinearDrag;
		public float Range;

		public CounterMeasureArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("linear_drag"))
				LinearDrag = sec.GetSetting("linear_drag").Float(0);
			if (sec.SettingExists("range"))
				Range = sec.GetSetting("range").Float(0);
			if (sec.SettingExists("diversion_pctg"))
				DiversionPctg = sec.GetSetting("diversion_pctg").Float(0);
		}
	}

	public class CounterMeasureDropperArchetype : LauncherArchetype
	{
		public float AiRange;

		public CounterMeasureDropperArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("ai_range"))
				AiRange = sec.GetSetting("ai_range").Float(0);
		}
	}

	public class MunitionArchetype : ProjectileArchetype
	{
		public bool CruiseDisruptor;
		public float DetonationDist;
		public float EnergyDamage;
		public float HullDamage;
		public float MaxAngularVelocity;
		public MotorArchetype MotorArch;
		public string Seeker;
		public float SeekerFovDeg;
		public float SeekerRange;
		public float TimeToLock;
		public WeaponType WeaponType;

		public MunitionArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("hull_damage"))
				HullDamage = sec.GetSetting("hull_damage").Float(0);
			if (sec.SettingExists("energy_damage"))
				EnergyDamage = sec.GetSetting("energy_damage").Float(0);
			if (sec.SettingExists("weapon_type"))
			{
				WeaponType = ArchetypeDB.FindWeaponType(sec.GetSetting("weapon_type").Str(0));
				if (WeaponType == null)
					log.AddLog(LogType.ERROR, "error: weapon_type not found " + sec.Desc);
			}
			if (sec.SettingExists("seeker"))
				Seeker = sec.GetSetting("seeker").Str(0);
			if (sec.SettingExists("time_to_lock"))
				TimeToLock = sec.GetSetting("time_to_lock").Float(0);
			if (sec.SettingExists("seeker_range"))
				SeekerRange = sec.GetSetting("seeker_range").Float(0);
			if (sec.SettingExists("seeker_fov_deg"))
				SeekerFovDeg = sec.GetSetting("seeker_fov_deg").Float(0);
			if (sec.SettingExists("detonation_dist"))
				DetonationDist = sec.GetSetting("detonation_dist").Float(0);
			if (sec.SettingExists("cruise_disruptor"))
				CruiseDisruptor = sec.GetSetting("cruise_disruptor").Str(0) == "true";
			if (sec.SettingExists("max_angular_velocity"))
				MaxAngularVelocity = sec.GetSetting("max_angular_velocity").Float(0);

			if (sec.SettingExists("motor"))
			{
				uint motorID = FLUtility.CreateID(sec.GetSetting("motor").Str(0));
				MotorArch = ArchetypeDB.Find(motorID) as MotorArchetype;
				if (MotorArch == null)
					log.AddLog(LogType.ERROR, "error: motor not found " + sec.Desc);
			}
		}
	}

	public class MotorArchetype : LauncherArchetype
	{
		public float Accel;
		public float AiRange;
		public float Delay;
		public float Lifetime;

		public MotorArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("ai_range"))
				AiRange = sec.GetSetting("ai_range").Float(0);
			if (sec.SettingExists("lifetime"))
				Lifetime = sec.GetSetting("lifetime").Float(0);
			if (sec.SettingExists("accel"))
				Accel = sec.GetSetting("accel").Float(0);
			if (sec.SettingExists("delay"))
				Delay = sec.GetSetting("delay").Float(0);
		}
	}

	public class ArmorArchetype : EquipmentArchetype
	{
		public float HitPtsScale;

		public ArmorArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("hit_pts_scale"))
				HitPtsScale = sec.GetSetting("hit_pts_scale").Float(0);
		}
	}

	public class PowerArchetype : EquipmentArchetype
	{
		public float Capacity;
		public float ChargeRate;
		public float ThrustCapacity;
		public float ThrustChargeRate;

		public PowerArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("capacity"))
				Capacity = sec.GetSetting("capacity").Float(0);
			if (sec.SettingExists("charge_rate"))
				ChargeRate = sec.GetSetting("charge_rate").Float(0);
			if (sec.SettingExists("thrust_capacity"))
				ThrustCapacity = sec.GetSetting("thrust_capacity").Float(0);
			if (sec.SettingExists("thrust_charge_rate"))
				ThrustChargeRate = sec.GetSetting("thrust_charge_rate").Float(0);
		}
	}

	public class ShieldGeneratorArchetype : ExternalEquipmentArechetype
	{
		public float ConstantPowerDraw;
		public float MaxCapacity;
		public float OfflineRebuildTime;
		public float OfflineThreshold;
		public float RebuildPowerDraw;
		public float RegenerationRate;

		public ShieldGeneratorArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("regeneration_rate"))
				RegenerationRate = sec.GetSetting("regeneration_rate").Float(0);
			if (sec.SettingExists("max_capacity"))
				MaxCapacity = sec.GetSetting("max_capacity").Float(0);
			if (sec.SettingExists("offline_rebuild_time"))
				OfflineRebuildTime = sec.GetSetting("offline_rebuild_time").Float(0);
			if (sec.SettingExists("offline_threshold"))
				OfflineThreshold = sec.GetSetting("offline_threshold").Float(0);
			if (sec.SettingExists("constant_power_draw"))
				ConstantPowerDraw = sec.GetSetting("constant_power_draw").Float(0);
			if (sec.SettingExists("rebuild_power_draw"))
				RebuildPowerDraw = sec.GetSetting("rebuild_power_draw").Float(0);
		}
	}

	public class ThrusterArchetype : ExternalEquipmentArechetype
	{
		public float MaxForce;
		public float PowerUsage;

		public ThrusterArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("max_force"))
				MaxForce = sec.GetSetting("max_force").Float(0);
			if (sec.SettingExists("power_usage"))
				PowerUsage = sec.GetSetting("power_usage").Float(0);
		}
	}

	public class EngineArchetype : EquipmentArchetype
	{
		public float CruiseChargeTime;
		public float CruisePowerUsage;
		public float LinearDrag;
		public float MaxForce;
		public float PowerUsage;
		public float ReverseFraction;

		public EngineArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
			if (sec.SettingExists("max_force"))
				MaxForce = sec.GetSetting("max_force").Float(0);

			if (sec.SettingExists("linear_drag"))
				LinearDrag = sec.GetSetting("linear_drag").Float(0);

			if (sec.SettingExists("power_usage"))
				PowerUsage = sec.GetSetting("power_usage").Float(0);

			if (sec.SettingExists("reverse_fraction"))
				ReverseFraction = sec.GetSetting("reverse_fraction").Float(0);

			if (sec.SettingExists("cruise_charge_time"))
				CruiseChargeTime = sec.GetSetting("cruise_charge_time").Float(0);

			if (sec.SettingExists("cruise_power_usage"))
				CruisePowerUsage = sec.GetSetting("cruise_power_usage").Float(0);
		}
	}

	public class RepairKitArchetype : Archetype
	{
		public RepairKitArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
		}
	}

	public class ScannerArchetype : Archetype
	{

		public uint ScanRange;
		public uint CargoRange;

		 public ScannerArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{

			if (sec.SettingExists("range"))
				ScanRange = sec.GetSetting("range").UInt(0);

			if (sec.SettingExists("cargo_scan_range"))
				CargoRange = sec.GetSetting("cargo_scan_range").UInt(0);

		}
	}

	public class ShieldBatteryArchetype : Archetype
	{
		public ShieldBatteryArchetype(string datapath, FLDataFile.Section sec, ILogController log)
			: base(datapath, sec, log)
		{
		}
	}
}