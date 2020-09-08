using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Xml.Serialization;
using FLServer.AppDomain;
using FLServer.DataWorkers;

/// <summary>
/// SQLite routines.
/// 
/// WARNING: It may be slow with tons of reads and writes - maybe we should implement queued commands-to-transaction thingy executed by timer.
/// </summary>

public class Account
{
	/// <summary>
	/// Unique identifier for an account. For tracking purposes
	/// </summary>
	[XmlIgnore]
	public string UID;

	[XmlIgnore]
	public string ID;

	[XmlIgnore]
	public string CharName;

	[XmlIgnore]
	public string CharFileName;

	/// <summary>
	/// Minutes online count.
	/// </summary>
	[XmlIgnore]
	public UInt32 TimeOnline;

	public Int32 Money;
	
	/// <summary>
	/// ShipArch hashcode.
	/// </summary>
	public uint Ship;
	public string System;
	public string Location;

	/// <summary>
	/// Last time account was updated, UTC time.
	/// </summary>
	public DateTime LastOnline;
	public byte Rank;
	public bool IsBanned;

	/// <summary>
	/// Base64-encoded serialized string.
	/// </summary>
	public string Settings;

	/// <summary>
	/// Base64-encoded serialized string.
	/// </summary>
	public string ShipState;

	/// <summary>
	/// Base64-encoded serialized string.
	/// </summary>
	public string Appearance;

	/// <summary>
	/// Base64-encoded serialized string.
	/// </summary>
	public string Equipment;

	/// <summary>
	/// Base64-encoded serialized string.
	/// </summary>
	public string Cargo;

	/// <summary>
	/// Base64-encoded serialized string.
	/// </summary>
	public string RepList;

	/// <summary>
	/// Base64-encoded serialized string.
	/// </summary>
	public string Visits;

}

namespace FLServer.Old.CharacterDB
{
	static class Database
	{
		private static SQLiteConnection _conn;
		private static Logging.Logger _logiface;
		private static Account _defAcct;
		private static readonly string Path;
		static Database()
		{
			Path = ((new CustomContext()).CurrentContext.AccountDir) + @"\CharacterDB.db";
			if (!File.Exists(Path)) 
				SQLiteConnection.CreateFile(Path);
			if (!Connect("Characters"))
			{
				CreateDB();
			}
			
			ReadStdAcct();


		}

		public static void AddCallback(Logging.Logger log)
		{
			_logiface = log;
		}

		private static void ReadStdAcct()
		{

			if (File.Exists((new CustomContext()).CurrentContext.AccountDir + @"\default.fl"))
			{
				_defAcct = new Importer().LoadCharFile( (new CustomContext()).CurrentContext.AccountDir + @"\default.fl", null);
				WriteStdAcct(_defAcct);
				return;
			} 

			try
			{
				var serializer = new XmlSerializer(typeof(Account));
				var reader = new StreamReader(((new CustomContext()).CurrentContext.AccountDir) + @"\default.sq");
				_defAcct = (Account) serializer.Deserialize(reader);
				reader.Close();
			}
			catch
			{
				throw new Exception("Can't load default account!");
			}
			
		}

		public static void WriteStdAcct(Account acct)
		{
			try
			{
				var writer = new XmlSerializer(acct.GetType());
				var file = new StreamWriter(((new CustomContext()).CurrentContext.AccountDir) + @"\default.sq");
				writer.Serialize(file, acct);
				file.Close();
			}
			catch
			{
				_logiface.AddLog(LogType.ERROR, @"Can't write default account!");
			}
		}

		#region "SQLite methods"

		/// <summary>
		/// Establishes connection to database. Returns true if table exists, otherwise false.
		/// </summary>
		/// <param name="dbName">Database name.</param>
		private static bool Connect(string dbName)
		{
			_conn = new SQLiteConnection(@"Data Source=" + Path);
			_conn.Open();
			var cmd = new SQLiteCommand(@"SELECT count(*) FROM sqlite_master WHERE type='table' AND name='" + dbName + @"';", _conn);
			if ((long)cmd.ExecuteScalar() > 0) return true;

			//Table not found
			return false;
		}


		#region "GetAccount"
		/// <summary>
		/// Get accounts by account ID
		/// </summary>
		/// <param name="id"></param>
		/// <returns>List of Account if anything found; null otherwise.</returns>
		public static List<Account> GetAccount(string id)
		{
			var cmd = new SQLiteCommand(@"SELECT * FROM Characters Where ID = '" + id + @"'", _conn);
			var outlist = new List<Account>();
			using (var reader = cmd.ExecuteReader())
			{
				if (!reader.HasRows) return null;

				while (reader.Read())
				{
					var a = new Account
					{
						CharName = reader.GetString(0),
						ID = reader.GetString(1),
						CharFileName = reader.GetString(2),
						Money = reader.GetInt32(3),
						Ship = (uint)reader.GetInt64(4),
						System = reader.GetString(5),
						Location = reader.GetString(6),
						LastOnline = reader.GetDateTime(7),
						Rank = reader.GetByte(8),
						IsBanned = reader.GetBoolean(9),
						Settings = reader.GetString(10),
						ShipState = reader.GetString(11),
						Appearance = reader.GetString(12),
						Equipment = reader.GetString(13),
						Cargo = reader.GetString(14),
						RepList = reader.GetString(15),
						Visits = reader.GetString(16),
						TimeOnline = (uint)reader.GetInt32(17),
						UID = reader.GetString(18)
						
					};
					outlist.Add(a);
				}
			}
			return outlist;
		}

		public static Account GetOneAccount(string id,string charfilename)
		{
			var cmd = new SQLiteCommand(@"SELECT * FROM Characters Where ID = '" + id + @"' And CharCode = '" + charfilename + @"'", _conn);
			using (var reader = cmd.ExecuteReader())
			{
				reader.Read();
				var a = new Account
					{
						CharName = reader.GetString(0),
						ID = reader.GetString(1),
						CharFileName = reader.GetString(2),
						Money = reader.GetInt32(3),
						Ship = (uint)reader.GetInt64(4),
						System = reader.GetString(5),
						Location = reader.GetString(6),
						LastOnline = reader.GetDateTime(7),
						Rank = reader.GetByte(8),
						IsBanned = reader.GetBoolean(9),
						Settings = reader.GetString(10),
						ShipState = reader.GetString(11),
						Appearance = reader.GetString(12),
						Equipment = reader.GetString(13),
						Cargo = reader.GetString(14),
						RepList = reader.GetString(15),
						Visits = reader.GetString(16),
						TimeOnline = (uint)reader.GetInt32(17),
						UID = reader.GetString(18)
					};
				Archetype arch = ArchetypeDB.Find(a.Ship);
				if (!(arch is ShipArchetype))
				{
					_logiface.AddLog(LogType.ERROR, "Wrong ship archetype: " + a.CharName);
					return null;
				}

				return a;
			}

		}

		/// <summary>
		/// Get accounts matching custom criteria.
		/// </summary>
		/// <param name="field">Field name to search, case sensitive</param>
		/// <param name="value">Field value.</param>
		/// <returns>List of Account if anything found; null otherwise.</returns>
		public static List<Account> GetAccount(string field,string value)
		{
			var cmd = new SQLiteCommand(@"SELECT * FROM Characters Where " + field + @" = '" + value + @"'", _conn);
			var outlist = new List<Account>();
			using (var reader = cmd.ExecuteReader())
			{
				if (!reader.HasRows) return null;

				while (reader.Read())
				{
					var a = new Account
					{
						CharName = reader.GetString(0),
						ID = reader.GetString(1),
						CharFileName = reader.GetString(2),
						Money = reader.GetInt32(3),
						Ship = (uint)reader.GetInt64(4),
						System = reader.GetString(5),
						Location = reader.GetString(6),
						LastOnline = reader.GetDateTime(7),
						Rank = reader.GetByte(8),
						IsBanned = reader.GetBoolean(9),
						Settings = reader.GetString(10),
						ShipState = reader.GetString(11),
						Appearance = reader.GetString(12),
						Equipment = reader.GetString(13),
						Cargo = reader.GetString(14),
						RepList = reader.GetString(15),
						Visits = reader.GetString(16),
						TimeOnline = (uint)reader.GetInt32(17),
						UID = reader.GetString(18)
					};
					outlist.Add(a);
				}
			}
			return outlist;
		}
		#endregion

		private const string Sqladdquery = @"INSERT INTO Characters (CharName,ID,CharCode,Money,ShipArch,System,Location,LastOnline,Rank,IsBanned,Settings,ShipState,Appearance,Equipment,Cargo,RepList,Visits,MinutesOnline,UID) VALUES (@CharName,@ID,@CharCode,@Money,@ShipArch,@System,@Location,@LastOnline,@Rank,@IsBanned,@Settings,@ShipState,@Appearance,@Equipment,@Cargo,@RepList,@Visits,@MinutesOnline,@UID)";
		
		/// <summary>
		/// Inserts new account info into DB.
		/// </summary>
		/// <param name="id">Player ID</param>
		/// <param name="charname">Character name</param>
		/// <returns>True if insert suceeds; otherwise false.</returns>
		public static bool AddAccount(string id, string charname)
		{
			using (var cmd = new SQLiteCommand(Sqladdquery,_conn))
			{
				cmd.Parameters.AddWithValue("@CharName", charname);
				cmd.Parameters.AddWithValue("@ID", id);
				cmd.Parameters.AddWithValue("@CharCode", FLMsgType.FLNameToFile(charname));
				cmd.Parameters.AddWithValue("@Money", _defAcct.Money);
				cmd.Parameters.AddWithValue("@ShipArch", _defAcct.Ship);
				cmd.Parameters.AddWithValue("@System", _defAcct.System);
				cmd.Parameters.AddWithValue("@Location", UniverseDB.FindSystem(_defAcct.System).Nickname);
				cmd.Parameters.AddWithValue("@LastOnline", DateTime.UtcNow);
				cmd.Parameters.AddWithValue("@Rank", _defAcct.Rank);
				cmd.Parameters.AddWithValue("@IsBanned", _defAcct.IsBanned);
				cmd.Parameters.AddWithValue("@Settings", _defAcct.Settings);
				cmd.Parameters.AddWithValue("@ShipState", _defAcct.ShipState);
				cmd.Parameters.AddWithValue("@Appearance", _defAcct.Appearance);
				cmd.Parameters.AddWithValue("@Equipment", _defAcct.Equipment);
				cmd.Parameters.AddWithValue("@Cargo", _defAcct.Cargo);
				cmd.Parameters.AddWithValue("@RepList", _defAcct.RepList);
				cmd.Parameters.AddWithValue("@Visits", _defAcct.Visits);
				cmd.Parameters.AddWithValue("@MinutesOnline", 0u);
				cmd.Parameters.AddWithValue("@UID",Guid.NewGuid().ToString());
				return cmd.ExecuteNonQuery() == 1;
			}


		}


		private const string SQLModQuery = @"UPDATE Characters SET CharName = @CharName, Money = @Money,ShipArch = @ShipArch,System = @System,Location = @Location,LastOnline = @LastOnline,Rank = @Rank,IsBanned = @IsBanned,Settings = @Settings,ShipState = @ShipState,Appearance = @Appearance,Equipment = @Equipment,Cargo = @Cargo,RepList = @RepList,Visits = @Visits, MinutesOnline = @MinutesOnline Where UID = @UID";
		/// <summary>
		/// Modifies account in database.
		/// </summary>
		/// <param name="acc">Modified account</param>
		/// <returns>True if mod suceeds; otherwise false.</returns>
		public static bool ModifyAccount(Account acc)
		{
			using (var cmd = new SQLiteCommand(SQLModQuery, _conn))
			{
				cmd.Parameters.AddWithValue("@UID", acc.UID);
				cmd.Parameters.AddWithValue("@CharName", acc.CharName);
				cmd.Parameters.AddWithValue("@CharCode", FLMsgType.FLNameToFile(acc.CharName));
				cmd.Parameters.AddWithValue("@Money", acc.Money);
				cmd.Parameters.AddWithValue("@ShipArch", acc.Ship);
				cmd.Parameters.AddWithValue("@System", acc.System);
				cmd.Parameters.AddWithValue("@Location", UniverseDB.FindSystem(acc.System).Nickname);
				cmd.Parameters.AddWithValue("@LastOnline", DateTime.UtcNow);
				cmd.Parameters.AddWithValue("@Rank", acc.Rank);
				cmd.Parameters.AddWithValue("@IsBanned", acc.IsBanned);
				cmd.Parameters.AddWithValue("@Settings", acc.Settings);
				cmd.Parameters.AddWithValue("@ShipState", acc.ShipState);
				cmd.Parameters.AddWithValue("@Appearance", acc.Appearance);
				cmd.Parameters.AddWithValue("@Equipment", acc.Equipment);
				cmd.Parameters.AddWithValue("@Cargo", acc.Cargo);
				cmd.Parameters.AddWithValue("@RepList", acc.RepList);
				cmd.Parameters.AddWithValue("@Visits", acc.Visits);
				cmd.Parameters.AddWithValue("@MinutesOnline", acc.TimeOnline);
				return cmd.ExecuteNonQuery() == 1;
			}
		}


		private const string SQLOnlineTickQuery = @"UPDATE Characters SET LastOnline = @LastOnline Where UID = @UID";
		/// <summary>
		/// Updates LastOnline with current time.
		/// </summary>
		/// <param name="acc">Account to update</param>
		/// <returns>True if mod suceeds; otherwise false.</returns>
		public static bool TickOnLine(Account acc)
		{
			//TODO: use dis
			using (var cmd = new SQLiteCommand(SQLOnlineTickQuery, _conn))
			{
				cmd.Parameters.AddWithValue("@UID", acc.UID);
				cmd.Parameters.AddWithValue("@LastOnline", DateTime.UtcNow);
				return cmd.ExecuteNonQuery() == 1;
			}
		}

		public static void DelAccount(string id, string charfilename)
		{
			using (var cmd = new SQLiteCommand(@"DELETE from Characters Where ID = '" + id + @"' And CharCode = '" + charfilename + @"'",_conn)) 
				cmd.ExecuteNonQuery();
		}

		private static void CreateDB()
		{
			using (var command = new SQLiteCommand(_conn))
			{
				command.CommandText = @"CREATE TABLE [Characters] (
					[CharName] text PRIMARY KEY ON CONFLICT REPLACE,
					[ID] text NOT NULL,
					[CharCode] text,
					[Money] integer NOT NULL,
					[ShipArch] bigint,
					[System] text,
					[Location] text,
					[LastOnline] datetime,
					[Rank] byte,
					[IsBanned] boolean,
					[Settings] text,
					[ShipState] text,
					[Appearance] text,
					[Equipment] text,
					[Cargo] text,
					[RepList] text,
					[Visits] text,
					[MinutesOnline] bigint,
					[UID] text UNIQUE
					);";
				command.CommandType = System.Data.CommandType.Text;
				command.ExecuteNonQuery();
			}
		}


		#endregion

	   
	}
}
