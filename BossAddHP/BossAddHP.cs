using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.CompilerServices;
using System.ComponentModel.Design;
using System.Threading;

namespace BossAddHP
{
    [ApiVersion(2, 1)]
    public class BossAddHP : TerrariaPlugin
    {
        public static string ConfigPath = "tshock\\BossAddHP.json";
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Leader";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "增加boss血量";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "BossAddHP";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(1, 0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the TestPlugin class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public BossAddHP(Main game) : base(game)
        {

        }

        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.NpcSpawn.Register(this, OnNpcSpwan);
            ServerApi.Hooks.NpcStrike.Register(this, OnNpc);
        }

        private void OnNpc(NpcStrikeEventArgs args)
        {
            List<int> a = AddHP;
            foreach(var i in a)
            {
                if (i == args.Npc.netID)
                {
                    AddHP.Remove(i);
                    var npc = args.Npc;
                    int life = Config.GetConfig().Num;
                    npc.lifeMax *= life;
                    npc.life *= life;
                    for (int _i = 0; _i < Main.npc.Count(); _i++)
                        if (Main.npc[i] == args.Npc)
                            npc.UpdateNPCDirect(i);
                    npc.StrikeNPC(1, 0, 0);
                }
            }
        }
        List<int> AddHP = new List<int>();
        private void OnNpcSpwan(NpcSpawnEventArgs args)
        {
            var npc = Main.npc[args.NpcId];
            ///TShock.Utils.Broadcast(Lang.GetNPCName(npc.netID) + "已苏醒,ID:" + args.NpcId, Color.Red);
            if (!Config.GetConfig().Open)
                return;
            int life = Config.GetConfig().Num;
            if (!npc.boss)
                return;
            foreach(var i in Config.GetConfig().SpecialIDs)
            {
                if (i == npc.netID)
                {
                    AddHP.Add(i);
                    return;
                }
            }
            npc.lifeMax *= life;
            npc.life *= life;
            npc.UpdateNPCDirect(args.NpcId);
            npc.StrikeNPC(1, 0, 0);
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