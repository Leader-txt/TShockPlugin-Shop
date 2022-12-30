using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaRPG
{
    public class Data
    {
        public static long GetNextLevelPrize(long level)
        {
            Config config = Config.GetConfig();
            long prize = config.Player.DefaultLevelUp + config.Player.LevelUp * level;
            return prize;
        }
        public static SqliteConnection connection = new SqliteConnection("data source = "+Config.GetConfig().SQLPath );
        public static SqliteDataReader Command(string cmd)
        {
            SqliteCommand command = new SqliteCommand(cmd, connection);
            return command.ExecuteReader();
        }
        public class NPC
        {
            public bool Stronger { get; set; }
            public int Level { get; set; }
            public int LevelUP { get; set; }
            public int[] IgnoreNpc { get; set; }
            public NPC(bool stronger = true,int level=2,int levelUP = 2,int[] ignoreNPC=null)
            {
                Stronger = stronger;
                Level = level;
                LevelUP = levelUP;
                IgnoreNpc = ignoreNPC == null ? new int[0] : ignoreNPC;
            }
        }
        public class Player
        {
            public int LevelUp { get; set; }
            public int DamageAdd { get; set; }
            public int DefaultLevelUp { get; set; }
            public Player(int levelUp=10,int damageAdd=10,int defaultLevelUp=100)
            {
                LevelUp = levelUp;
                DamageAdd = damageAdd;
                DefaultLevelUp = defaultLevelUp;
            }
        }
        public class Config
        {
            public string SQLPath { get; set; }
            public Player Player { get; set; }
            public NPC NPC { get; set; }
            public Config(string sqlpath="tshock\\RPG.sqlite",Player player =null,NPC npc=null)
            {
                SQLPath = sqlpath;
                Player = player == null ? new Player() : player;
                NPC = npc == null ? new NPC() : npc;
            }
            public void Save()
            {
                using (StreamWriter wr=new StreamWriter(TerrariaRPG.ConfigPath))
                {
                    wr.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
                }
            }
            public static Config GetConfig()
            {
                if (!File.Exists(TerrariaRPG.ConfigPath))
                {
                    Config config = new Config();
                    config.Save();
                }
                string json;
                using(StreamReader re=new StreamReader(TerrariaRPG.ConfigPath))
                {
                    json = re.ReadToEnd();
                }
                return JsonConvert.DeserializeObject<Config>(json);
            }
        }
        public class Job
        {
            //sql columns
            //JobType int(32);Level int(64);Name text;Table:job
            public enum JobType
            {
                射手,
                战士,
                法师,
                白丁
            }
            public JobType jobType { get; set; }
            public long Level { get; set; }
            public string Name { get; set; }
            public Job(JobType type,long level,string name)
            {
                jobType = type;
                Level = level;
                Name = name;
            }
            public void Save()
            {
                try
                {
                    GetJob(Name);
                    Command($"update job set JobType={(int)jobType},Level={Level} where Name='{Name}'");
                }
                catch
                {
                    Command($"insert into job(JobType,Level,Name)values({(int)jobType},{Level},'{Name}')");
                }
            }
            public static Job GetJob(string name)
            {
                var reader = Command($"select JobType,Level from job where Name='{name}'");
                reader.Read();
                return new Job((JobType)reader.GetInt32(0), reader.GetInt64(1), name);
            }
        }
    }
}
