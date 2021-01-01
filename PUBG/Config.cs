using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PUBG
{
    class Item
    {
        public int NetID { get; set; }
        public int Stack { get; set; }
        public int Select { get; set; }
        public Item(int netID=96,int stack=1,int select = 10)
        {
            NetID = netID;
            Stack = stack;
            Select = select;
        }
    }
    class Random
    {
        public Item[] Items { get; set; }
        public Random (Item[] items = null)
        {
            Items = items == null ? new Item[1] { new Item() } : items;
        }
    }
    class Config
    {
        public const string ConfigPath= "tshock\\RandomItem.json";
        public Random Random { get; set; }
        public Config(Random random = null)
        {
            Random = random == null ? new Random() : random;
        }
        public void Save()
        {
            using (StreamWriter wr=new StreamWriter (ConfigPath))
            {
                wr.WriteLine(JsonConvert.SerializeObject(this, Formatting.Indented));
            }
        }
        public static Config GetConfig()
        {
            Config result;
            if (!File.Exists(ConfigPath))
            {
                (new Config()).Save();
            }
            using (StreamReader re=new StreamReader(ConfigPath))
            {
                result = JsonConvert.DeserializeObject<Config>(re.ReadToEnd());
            }
            return result;
        }
    }
}
