using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace BossAddHP
{
    class Config
    {
        public bool Open { get; set; }
        public int Num { get; set; }
        public int[] SpecialIDs { get; set; }        
        public Config(bool open = true, int num = 5, int[] specialids =null)
        {
            Open = open;
            Num = num;
            SpecialIDs = specialids == null ? new int[3] { NPCID.MoonLordCore,NPCID.MoonLordHand ,NPCID.MoonLordHead } : specialids; 
        }
        public void Save()
        {
            using (StreamWriter wr = new StreamWriter(BossAddHP.ConfigPath))
            {
                wr.Write(JsonConvert.SerializeObject(this,Formatting.Indented));
            }
        }
        public static Config GetConfig()
        {
            if (!File.Exists(BossAddHP.ConfigPath))
            {
                Config config = new Config();
                config.Save();
            }
            string json;
            using (StreamReader re = new StreamReader(BossAddHP.ConfigPath))
            {
                json = re.ReadToEnd();
            }
            return JsonConvert.DeserializeObject<Config>(json);
        }
    }
}
