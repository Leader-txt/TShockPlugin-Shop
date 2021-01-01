using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using Mono.Data.Sqlite;
using Terraria.Achievements;
using System.Windows.Forms;
using TShockAPI.DB;
using System.IO;
using Newtonsoft.Json;

namespace Shop
{
    class Change
    {
        public string Owner { get; set; }
        public int NetID { get; set; }
        public int Stack { get; set; }
        public int Perfix { get; set; }
        public int Prize { get; set; }
        public int Account { get; set; }
        public Change(string owner, int netid, int stack, int perfix, int prize, int account=0)
        {
            Owner = owner;
            NetID = netid;
            Stack = stack;
            Perfix = perfix;
            Prize = prize;
            Account = account;
        }
        public void Save()
        {
            try
            {
                //Data.Command($"select * from change where Account={Account}");
                GetChange(Account);
                Data.Command($"update change set Owner='{Owner}',NetID={NetID},Stack={Stack},Perfix ={Perfix},Prize={Prize} where Account={Account}");
            }
            catch
            {
                Data.Command($"insert into change(Owner,NetID,Stack,Perfix,Prize,Account)values('{Owner}',{NetID},{Stack},{Perfix},{Prize},{Account})");
            }
        }
        public void Delete()
        {
            Data.Command($"delete from change where Account={Account} and Owner='{Owner}'");
        }
        public static Change GetChange(int account,string owner = null)
        {
            var reader = Data.Command($"select Owner,NetID,Stack,Perfix,Prize from change where Account={account}" + (owner == null ? "" : $" and Owner='{owner}'"));
            reader.Read();
            return new Change(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3),reader.GetInt32(4) ,account);
        }
        public static Change[] GetChanges(string owner=null)
        {
            var reader = Data.Command($"select Owner,NetID,Stack,Perfix,Prize,Account from change"+(owner==null?"":$" where Owner='{owner}'"));
            List<Change> changes = new List<Change>();
            while (reader.Read())
            {
                changes.Add(new Change(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4),reader.GetInt32(5)));
            }
            return changes.ToArray();
        }
    }
    class Config
    {
        public string SQLPath { get; set; }
        public int CoinToMoney { get; set; }
        public bool LifeBuy { get; set; }
        public int MoneyToLife { get; set; }
        public int MaxLife { get; set; }
        public DeathPunish deathPunish { get; set; }
        public int[] IgnoreNPCID { get; set; }
        public class DeathPunish
        {
            public bool Open { get; set; }
            public long DropMaxValue { get; set; }
            public long DropMinValue { get; set; }
            public bool Spwan { get; set; }
            public DeathPunish(bool open=false,long dropMaxValue=50,long dropMinValue = 0,bool spwan=false)
            {
                Open = open;
                DropMaxValue = dropMaxValue;
                DropMinValue = dropMinValue;
                Spwan = spwan;
            }
        }
        public Config(string sqlpath="tshock\\shop.sqlite",int coinToMoney=100,
            int moneyToLife=20,int maxlife=500,DeathPunish punish =null,int[] ignore=null,bool lifebuy=false)
        {
            SQLPath = sqlpath;
            CoinToMoney = coinToMoney;
            MoneyToLife = moneyToLife;
            MaxLife = maxlife;
            LifeBuy = lifebuy;
            deathPunish = punish == null ? new DeathPunish() : punish;
            IgnoreNPCID = ignore == null ? new int[0] : ignore;
        }
        public static Config GetConfig()
        {
            if (!File.Exists(Shop.ConfigPath))
            {
                Config config = new Config();
                config.Save();
            }
            string text;
            using (StreamReader re=new StreamReader(Shop.ConfigPath))
            {
                text = re.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<Config>(text);
        }
        public void Save()
        {
            using (StreamWriter wr=new StreamWriter(Shop.ConfigPath))
            {
                wr.Write(JsonConvert.SerializeObject(this,Formatting.Indented));
            }
        }
    }
    class Item
    {
        public int NetID { get; set; }
        public int Prize { get; set; }
        public int Num { get; set; }
        public Item(int netid,int prize,int num)
        {
            NetID = netid;
            Prize = prize;
            Num = num;
        }
        public void Save()
        {
            try
            {
                GetItem(NetID);
                Data.Command($"update item set NetID={NetID},Prize={Prize},Num={Num}");
            }
            catch
            {
                Data.Command($"insert into item(NetID,Num,Prize)values({NetID},{Num},{Prize})");
            }
        }
        public static Item GetItem(int netid)
        {
            var reader = Data.Command($"select Prize,Num from item where NetID={netid}");
            reader.Read();
            return new Item(netid, reader.GetInt32(0), reader.GetInt32(1));
        }
    }
    public class Bank
    {
        public string Name { get; set; }
        public long Money { get; set; }
        public Bank(string name,long money)
        {
            Name = name;
            Money = money;
        }
        public void Save()
        {
            Data.Command($"update bank set Money={Money} where Name='{Name}'");
        }
    }
    public class Data
    {
        public static string SqlStr = "data source = " + Config.GetConfig().SQLPath;
        public static SqliteConnection connection = new SqliteConnection(SqlStr);
        public static SqliteDataReader Command(string cmd)
        {
            SqliteCommand command = new SqliteCommand(cmd, connection);
            return command.ExecuteReader();
        }
        public static Bank GetBank(string Name)
        {
            SqliteDataReader reader = Command($"select Money,Name from bank where Name='{Name}'");
            try 
            {
                reader.Read();
                return new Bank(Name, reader.GetInt64(0));
            }
            catch
            {
                Command($"insert into bank (Name,Money)values('{Name}',0)");
                return new Bank(Name, 0);
            }
        }
    }
    class Tables
    {
        public class Change
        {
            public const string TableName = "change";
            public static class Columns
            {
                public static class Owner
                {
                    public const string Name = "Owner";
                    public const string Type = "text";
                }
                public static class NetID
                {
                    public const string Name = "NetID";
                    public const string Type = "int(32)";
                }
                public static class Num
                {
                    public const string Name = "Num";
                    public const string Type = "int(32)";
                }
                public static class Prize
                {
                    public const string Name = "Prize";
                    public const string Type = "int(32)";
                }
                public static class Prefix
                {
                    public const string Name = "Prefix";
                    public const string Type = "int(32)";
                }
            }
        }
        public static class Item
        {
            public const string TableName = "item";
            public static class Columns
            {
                public static class NetID
                {
                    public const string Name = "NetID";
                    public const string Type = "int(32)";
                }
                public static class Num
                {
                    public const string Name = "Num";
                    public const string Type = "int(32)";
                }
                public static class Prize
                {
                    public const string Name = "Prize";
                    public const string Type = "int(32)";
                }
            }
        }
        public static class Bank
        {
            public const string TableName = "bank";
            public static class Columns
            {
                public static class Account
                {
                    public const string Name = "Name";
                    public const string Type = "text";
                }
                public static class Money
                {
                    public const string Name = "Money";
                    public const string Type = "int64";
                }
            }
        }
    }
}
