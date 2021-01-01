using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.IO;

namespace NPCStronger
{
    class Config
    {
        public float StrongerPercent { get; set; }
        public bool AllNPC { get; set; }
        public bool OnlyBoss { get; set; }
        public int[] IgnoreNPCs { get; set; }
        public Config(float percent = 2,bool allNPC=false,bool onlyBoss=true,int[] ignoreNPCs = null)
        {
            StrongerPercent = percent;
            AllNPC = allNPC;
            OnlyBoss = onlyBoss;
            IgnoreNPCs = ignoreNPCs == null ? new int[0] : ignoreNPCs;
        }
        public void Save()
        {
            using (StreamWriter wr=new StreamWriter("tshock\\StrongerNPC.json"))
            {
                wr.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
            }
        }
        public static Config GetConfig()
        {
            Config result;
            using (StreamReader re=new StreamReader("tshock\\StrongerNPC.json"))
            {
                result = JsonConvert.DeserializeObject<Config>(re.ReadToEnd());
            }
            return result;
        }
    }
    [ApiVersion(2, 1)]
    public class NPCStronger : TerrariaPlugin
    {
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Leader";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "增强NPC";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "NPC增强";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(1, 0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the NPCStronger class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public NPCStronger(Main game) : base(game)
        {

        }
        void ColorfulWrite(string text,ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = ConsoleColor.White;
        }
        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>
        public override void Initialize()
        {
            try
            {
                Config.GetConfig();
            }
            catch
            {
                (new Config()).Save();
                ColorfulWrite("感谢使用NPC增强插件,", ConsoleColor.Green);
                ColorfulWrite("作者:", ConsoleColor.Green);
                ColorfulWrite("Leader\r", ConsoleColor.Yellow);
                ColorfulWrite("使用方法:", ConsoleColor.DarkBlue);
                ColorfulWrite("在您的tshock根目录下生成了NPCStronger.json，使用记事本打开，第一个参数是增强百分比，例如2即为200%，0.2则为原来血量的20%，第二个参数是是否应用到所有NPC，第三个参数是是否仅应用到Boss，第二个优先级高于第三个，第四个则为list类型，输入胡罗加强的npcid，以逗号隔开\r", ConsoleColor.Green);
                ColorfulWrite("本插件无命令", ConsoleColor.Red);
                Console.ReadLine();
            }
            ServerApi.Hooks.NpcStrike.Register(this, OnNPCStrike);
        }

        private void OnNPCStrike(NpcStrikeEventArgs args)
        {
            Config config = Config.GetConfig();
            foreach (int i in config.IgnoreNPCs)
            {
                if (i == args.Npc.netID)
                    return;
            }
            if (config.AllNPC)
            {
                args.Damage = (int)((float)args.Damage * config.StrongerPercent);
            }
            else if(config .OnlyBoss && args.Npc.boss)
            {
                args.Damage = (int)((float)args.Damage * config.StrongerPercent);
            }
        }

        /// <summary>
        /// Handles plugin disposal logic.
        /// *Supposed* to fire when the server shuts down.
        /// You should deregister hooks and free all resources here.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Deregister hooks here
            }
            base.Dispose(disposing);
        }
    }
}