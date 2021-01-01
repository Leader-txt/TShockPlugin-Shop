using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria.ID;
using OTAPI;
using System.Diagnostics;
using System.Net;

namespace PUBG
{
    [ApiVersion(2, 1)]
    public class PUBG : TerrariaPlugin
    {
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Leader";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "PUBG plugin";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "PUBG";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(1, 0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the PUBG class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public PUBG(Main game) : base(game)
        {

        }

        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.NetSendData.Register(this, OnNetSendData);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            Commands.ChatCommands.Add(new Command("pubg.admin", pubg, "pubg"));
            Commands.ChatCommands.Add(new Command("", list, "pubgrandoms"));
        }

        private void OnNetSendData(SendDataEventArgs args)
        {

        }

        private void OnGetData(GetDataEventArgs args)
        {

        }

        private void list(CommandArgs args)
        {
            Config config = Config.GetConfig();
            foreach (var i in config.Random.Items)
            {
                args.Player.SendInfoMessage(Lang.GetItemName(i.NetID) + "[i:" + i.NetID + "]:数量:0-" + i.Stack + ",生成概率:" + i.Select + "%");
            }
        }

        private void pubg(CommandArgs args)
        {
            if(args.Parameters.Count == 0 || args.Parameters[0] == "help")
            {
                args.Player.SendInfoMessage("/pubg start,启动游戏");
                args.Player.SendInfoMessage("/pubg random,获取随机生成指令列表");
                return;
            }
            Config config = Config.GetConfig();
            switch (args.Parameters[0])
            {
                case "start":
                    break;
                case "random":
                    if(args.Parameters.Count == 1 || args.Parameters[1] == "help")
                    {
                        args.Player.SendInfoMessage("/pubg random clear,清空所有箱子物品");
                        args.Player.SendInfoMessage("/pubg random del 物品id/all,清除所有/指定生成物品");
                        args.Player.SendInfoMessage("/pubg random add 物品id 概率(小于等于100,大于0的整数,单位:%) [数量],添加随机生成物品,数量为最多生成的数量");
                        args.Player.SendInfoMessage("/pubg random start [最少数量] [最多数量],开始生成,数量为生成物品的数量,默认为1");
                        return;
                    }
                    switch (args.Parameters[1])
                    {
                        case "start":
                            int min = args.Parameters.Count > 2 ? int.Parse(args.Parameters[2]) : 1;
                            int max = args.Parameters.Count > 3 ? int.Parse(args.Parameters[3]) : 1;
                            if (min <= 0 || max <= 0)
                            {
                                args.Player.SendErrorMessage("最大,最小数量必须>0");
                                return;
                            }
                            System.Random random = new System.Random(); 
                            foreach (var chest in Main.chest)
                            {
                                if (chest != null)
                                {
                                    int num = random.Next(min, max);
                                    List<Item> items1 = new List<Item>();
                                    while (items1.Count < num)
                                    {
                                        List<Item> items2 = config.Random.Items.ToList().FindAll((Item i) => i.Select>= random.Next(1, 100));
                                        if (items2.Count != 0)
                                        {
                                            items1.Add(items2[random.Next(0, items2.Count - 1)]);
                                        }
                                    }
                                    foreach(Terraria.Item item in chest.item)
                                    {
                                        if (item.stack == 0)
                                        {
                                            item.netID = items1[0].NetID;
                                            item.stack = random.Next(1,items1[0].Stack);
                                            items1.RemoveAt(0);
                                            if (items1.Count == 0)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            args.Player.SendSuccessMessage("生成完毕");
                            break;
                        case "add":
                            int netId = int.Parse(args.Parameters[2]);
                            int percent = int.Parse(args.Parameters[3]);
                            if(percent >100||percent <= 0)
                            {
                                args.Player.SendErrorMessage("概率必须<=100且>0");
                                return;
                            }
                            int stack = args.Parameters.Count == 4 ? 1 : int.Parse(args.Parameters[4]);
                            if (stack <= 0)
                            {
                                args.Player.SendErrorMessage("数量必须>=0");
                                return;
                            }
                            List<Item> ites = config.Random.Items.ToList();
                            ites.Add(new Item(netId, stack,percent));
                            config = new Config(new Random(ites.ToArray()));
                            config.Save();
                            args.Player.SendInfoMessage("添加成功");
                            break;
                        case "del":
                            if (args.Parameters[2] == "all")
                            {
                                config.Random = new Random();
                                config.Save();
                                args.Player.SendSuccessMessage("删除成功");
                            }
                            int id = int.Parse(args.Parameters[2]);
                            List<Item> items = config.Random.Items.ToList();
                            var ite = items.FindAll((Item i) => i.NetID == id);
                            if (ite.Count > 0)
                            {
                                items.Remove(ite[0]);
                                config.Random = new Random(items.ToArray());
                                config.Save();
                                args.Player.SendInfoMessage("删除成功");
                            }
                            else
                            {
                                args.Player.SendErrorMessage("未找到匹配项");
                            }
                            break;
                        case "clear":
                            for(int i = 0; i < Main.chest.Count(); i++)
                            {
                                if (Main.chest[i] != null)
                                {
                                    for(int item = 0; item < Main.chest[i].item.Count(); item++)
                                    {
                                        Main.chest[i].item[item].stack = 0;
                                    }
                                }
                            }
                            args.Player.SendSuccessMessage("删除成功");
                            break;
                    }
                    break;
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