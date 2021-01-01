using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoCheater
{
    class Period
    {
        public int NPCNetID { get; set; }
        public bool Enable { get; set; }
        public int[] ItemIDs { get; set; }
        public CheckItem[] Checks { get; set; }
        public Period(int npcNetid=50,bool enable=false,int[] itemIDs=null,CheckItem[] checkItems=null)
        {
            NPCNetID = npcNetid;
            Enable = enable;
            ItemIDs = itemIDs == null ? new int[0] : itemIDs;
            Checks = checkItems == null ? new CheckItem[1] { new CheckItem() } : checkItems;
        }
    }
    class CheckItem
    {
        int NetID { get; set; }
        int MinStack { get; set; }
        public CheckItem(int netID=0,int minStack=0)
        {
            NetID = netID;
            MinStack = minStack;
        }
    }
    class Punish
    {
        public bool EnableBan { get; set; }
        public bool EnableKick { get; set; }
        public bool EnableKill { get; set; }
        public int BuffType { get; set; }
        public int BuffTime { get; set; }
        public Punish(bool enableBan=false,bool enableKick=false,bool enableKill=false,int buffType=0,int buffTime = 0)
        {
            EnableBan = enableBan;
            EnableKick = enableKick;
            EnableKill = enableKill;
            BuffType = buffType;
            BuffTime = buffTime;
        }
    }
    class Config
    {
        public const string ConfigPath= "tshock//Config.NoCheater";    
        public Punish Punish { get; set; }
        public CheckItem[] MainChecks { get; set; }
        public Period[] Periods { get; set; }
        public Config(Punish punish = null,CheckItem[] checkItems =null,Period[] periods = null)
        {
            Punish = punish == null ? new Punish() : punish;
            MainChecks = checkItems == null ? new CheckItem[1] { new CheckItem() } : checkItems;
            Periods = periods == null ? new Period[1] { new Period() } : periods;
        }
        public void Save()
        {
            using (StreamWriter wr=new StreamWriter(ConfigPath))
            {
                wr.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
            }
        }
        public Config GetConfig()
        {
            if(!File.Exists(ConfigPath))
            {
                Config config = new Config();
                config.Save();
            }
            Config result;
            using (StreamReader re=new StreamReader(ConfigPath))
            {
                result = JsonConvert.DeserializeObject<Config>(re.ReadToEnd());
            }
            return result;
        }
    }
}
