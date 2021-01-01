using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using Terraria.UI;
using ClientApi.Networking;
using Shop;
using System.IO;
using System.Data;
using Newtonsoft.Json;
using System.Security.AccessControl;
using TShockAPI.DB;

namespace TerrariaRPG
{
    [ApiVersion(2, 1)]
    public class TerrariaRPG : TerrariaPlugin
    {
        public const string ConfigPath = "tshock\\RPG.json";
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Leader";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "强化了各大职业";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "TerrariaRPG";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(1, 0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the TestPlugin class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public TerrariaRPG(Main game) : base(game)
        {
            if (!File.Exists(Data.Config.GetConfig().SQLPath))
            {
                Data.connection.Open();
                Data.Command("create table job(JobType int(32),Level int(64),Name text)");
                Data.connection.Close();
            }
            Data.connection.Open();
        }

        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcStrike);
            ServerApi.Hooks.ServerChat.Register(this, OnServerChat);
            ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
            ServerApi.Hooks.NpcKilled.Register(this, OnNPCKilled);

            Commands.ChatCommands.Add(new Command("", RPG, "job"));
        }

        private void OnNPCSpwan(NpcSpawnEventArgs args)
        {
            var npc = Main.npc[args.NpcId];
            npc.lifeMax += npc.lifeMax * Data.Config.GetConfig().NPC.Level;
            Main.npcLifeBytes[args.NpcId] = 4;
            npc.UpdateNPCDirect(args.NpcId);
        }

        private void RPG(CommandArgs args)
        {
            Data.Job job;
            try
            {
                job = Data.Job.GetJob(args.Player.Name);
            }
            catch
            {
                job = new Data.Job(Data.Job.JobType.白丁, 0, args.Player.Name);
                job.Save();
            }
            var config = Data.Config.GetConfig();
            var bank = Shop.Data.GetBank(args.Player.Name);
            if (args.Parameters.Count() == 0 || args.Parameters[0] == "help")
            {
                args.Player.SendInfoMessage("您当前的职业为:" + job.jobType);
                args.Player.SendInfoMessage("您当前的余额为:" + bank.Money);
                if (job.jobType == Data.Job.JobType.白丁)
                {
                    args.Player.SendInfoMessage("输入/job choose 战士，并花费" + config.Player.DefaultLevelUp
                        +"余额可升级到[战士lv1]");
                    args.Player.SendInfoMessage("输入/job choose 法师，并花费" + config.Player.DefaultLevelUp
                        + "余额可升级到[法师lv1]");
                    args.Player.SendInfoMessage("输入/job choose 射手，并花费" + config.Player.DefaultLevelUp
                        + "余额可升级到[射手lv1]");
                }
                else
                {
                    args.Player.SendInfoMessage("您可花费" + Data.GetNextLevelPrize(job.Level) + $"升级到[{job.jobType}lv{job.Level + 1}]" +
                        $"您当前伤害加成为原伤害的{100 + job.Level * config.Player.DamageAdd}%,升级后可提升到" +
                        $"{100 + (job.Level + 1) * config.Player.DamageAdd}");
                    args.Player.SendInfoMessage("输入/job lvup [等级],升级,不填写等级默认为升一级");
                    args.Player.SendInfoMessage($"若想转职，则必须先遗忘此职业，需花费" + Data.GetNextLevelPrize(job.Level)
                        + "并输入/job 孟婆汤，" +
                        "遗忘职业后才能重新选职业");
                }
                if (args.Player.HasPermission("RPG.admin"))
                {
                    args.Player.SendInfoMessage("输入/job lv 等级 [玩家名字],更改玩家等级(不填为自己)");
                    args.Player.SendInfoMessage("输入/job type 战士/法师/射手/白丁 [玩家名字]，更改玩家职业");
                    args.Player.SendInfoMessage("输入/job clear 玩家名字/all，清空指定玩家/所有数据");
                }
                return;
            }
            switch (args.Parameters[0])
            {
                case "clear":
                    if (!args.Player.HasPermission("RPG.admin"))
                    {
                        args.Player.SendErrorMessage("抱歉，您无权限操作");
                        return;
                    }
                    if (args.Parameters[1] == "all")
                    {
                        Data.Command("delete from job");
                        foreach (var v in TShock.Players)
                        {
                            if (v == null)
                                continue;
                            Data.Job j = new Data.Job(Data.Job.JobType.白丁, 0, v.Name);
                            j.Save();
                        }
                        args.Player.SendSuccessMessage("删除成功，已清除所有数据");
                        return;
                    }
                    Data.Command($"delete from job where Name='{args.Parameters[1]}'");
                    Data.Job  b= new Data.Job(Data.Job.JobType.白丁, 0, args.Parameters[1]);
                    b.Save();
                    args.Player.SendSuccessMessage($"删除成功，已清除{args.Parameters[1]}的数据");
                    break;
                case "type":
                    if (!args.Player.HasPermission("RPG.admin"))
                    {
                        args.Player.SendErrorMessage("抱歉，您无权限操作");
                        return;
                    }
                    var __job = args.Parameters.Count() == 3 ? Data.Job.GetJob(args.Parameters[2]) : job;
                    switch (args.Parameters[1])
                    {
                        case "战士":
                            __job.jobType = Data.Job.JobType.战士;
                            __job.Save();
                            args.Player.SendSuccessMessage("修改成功," + (args.Parameters.Count() == 3 ?
                                args.Parameters[2] : "您") + "的职业已改为战士");
                            break;
                        case "法师":
                            __job.jobType = Data.Job.JobType.法师;
                            __job.Save();
                            args.Player.SendSuccessMessage("修改成功," + (args.Parameters.Count() == 3 ?
                                args.Parameters[2] : "您") + "的职业已改为法师");
                            break;
                        case "射手":
                            __job.jobType = Data.Job.JobType.射手;
                            __job.Save();
                            args.Player.SendSuccessMessage("修改成功," + (args.Parameters.Count() == 3 ?
                                args.Parameters[2] : "您") + "的职业已改为射手");
                            break;
                        case "白丁":
                            __job.jobType = Data.Job.JobType.白丁;
                            __job.Save();
                            args.Player.SendSuccessMessage("修改成功," + (args.Parameters.Count() == 3 ?
                                args.Parameters[2] : "您") + "已变为白丁");
                            break;
                    }
                    break;
                case "lv":
                    if (!args.Player.HasPermission("RPG.admin"))
                    {
                        args.Player.SendErrorMessage("抱歉，您无权限操作");
                        return;
                    }
                    var _job = args.Parameters.Count() == 3 ? Data.Job.GetJob(args.Parameters[2]) : job;
                    if(_job.jobType ==Data.Job.JobType.白丁)
                    {
                        args.Player.SendErrorMessage("抱歉，白丁的等级无法修改");
                        return;
                    }
                    int lv = int.Parse(args.Parameters[1]);
                    if (lv <= 0)
                    {
                        lv = 1;
                    }
                    _job.Level = lv;
                    _job.Save();
                    args.Player.SendSuccessMessage((args.Parameters.Count() == 3 ? args.Parameters[2] : "您") +
                        "的等级已改为" + _job.Level);
                    break;
                case "孟婆汤":
                    if (job.jobType == Data.Job.JobType.白丁)
                    {
                        args.Player.SendErrorMessage("抱歉，白丁无法遗忘职业，请先选择职业");
                        return;
                    }
                    var prize = Data.GetNextLevelPrize(job.Level);
                    if (bank.Money < prize)
                    {
                        args.Player.SendErrorMessage("抱歉，您没有足够的余额购买孟婆汤，遗忘职业失败");
                        return;
                    }
                    bank.Money -= prize;
                    bank.Save();
                    job.jobType = Data.Job.JobType.白丁;
                    job.Level = 0;
                    job.Save();
                    args.Player.SendSuccessMessage("购买成功！您现在为[白丁lv0]");
                    break;
                case "lvup":
                    if (job.jobType == Data.Job.JobType.白丁)
                    {
                        args.Player.SendErrorMessage("抱歉，白丁无法升级，请先选择职业");
                        return;
                    }
                    long level = args.Parameters.Count == 1 ?1:int.Parse(args.Parameters[1]);
                    if (level <= 0)
                    {
                        args.Player.SendErrorMessage("等级不能<=0");
                            return;
                    }
                    long money = 0;
                    for(int i = 0; i < level; i++)
                    {
                        money += Data.GetNextLevelPrize(job.Level + i);
                    }
                    if(bank.Money < money)
                    {
                        args.Player.SendErrorMessage($"抱歉，您没有足够的余额来升级，升级到[{job.jobType}lv{job.Level+level}]" +
                            $"需要花费{money}余额，您还差{money-bank.Money}余额");
                        return;
                    }
                    bank.Money -= money;
                    bank.Save();
                    job.Level += level;
                    job.Save();
                    args.Player.SendInfoMessage("升级成功，您现在为[" + job.jobType + "lv" + job.Level + "],您的余额为"+bank.Money);
                    break;
                case "choose":
                    if(job.jobType != Data.Job.JobType.白丁)
                    {
                        args.Player.SendInfoMessage("请先购买孟婆汤遗忘职业才能重新选择职业");
                        return;
                    }
                    if (bank.Money < config.Player.DefaultLevelUp)
                    {
                        args.Player.SendErrorMessage("您没有足够的余额选择职业");
                        return;
                    }
                    switch (args.Parameters[1])
                    {
                        case "战士":
                            job.jobType = Data.Job.JobType.战士;
                            job.Level = 1;
                            job.Save();
                            bank.Money -= config.Player.DefaultLevelUp;
                            bank.Save();
                            args.Player.SendSuccessMessage("职业选择成功！您现在是[战士lv1]");
                            break;
                        case "法师":
                            job.jobType = Data.Job.JobType.法师;
                            job.Level = 1;
                            job.Save();
                            bank.Money -= config.Player.DefaultLevelUp;
                            bank.Save();
                            args.Player.SendSuccessMessage("职业选择成功！您现在是[法师lv1]");
                            break;
                        case "射手":
                            job.jobType = Data.Job.JobType.射手;
                            job.Level = 1;
                            job.Save();
                            bank.Money -= config.Player.DefaultLevelUp;
                            bank.Save();
                            args.Player.SendSuccessMessage("职业选择成功！您现在是[射手lv1]");
                            break;
                    }
                    break;
            }
        }
        private void OnNPCKilled(NpcKilledEventArgs args)
        {
            if (args.npc.boss)
            {
                Data.Config config = Data.Config.GetConfig();
                config.NPC.Level += config.NPC.LevelUP;
                config.Save();
                TShock.Utils.Broadcast($"击杀boss，全体NPC等级升级:{config.NPC.LevelUP}级，当前" +
                    $"NPC等级为:{config.NPC.Level}，玩家受伤加成{config.NPC.Level}倍", Color.Red);
            }
        }

        private void OnServerJoin(JoinEventArgs args)
        {
            try
            {
                Data.Job.GetJob(TShock.Players[args.Who].Name);
            }catch
            {
                Data.Job job = new Data.Job(Data.Job.JobType.白丁, 0, TShock.Players[args.Who].Name);
                job.Save();
            }
        }
        Color RGB(long color)
        {
            long r = 0xFF & color;
            long g = 0xFF00 & color;
            g >>= 8;
            long b = 0xFF0000 & color;
            b >>= 16;
            return new Color(r, g, b);
        }
        string RGBs(long color)
        {
            long r = 0xFF & color;
            long g = 0xFF00 & color;
            g >>= 8;
            long b = 0xFF0000 & color;
            b >>= 16;
            return r.ToString("X") + g.ToString("X") + b.ToString("X");
        }
        string JobLevelRBG(Data.Job.JobType type,long level,long toplv)
        {
            int r=0, g=0, b=0;
            switch (type)
            {
                case Data.Job.JobType.法师:
                    b = (int)(level * 255 / toplv);
                    r = 255-(int)(level * 255 / toplv);
                    g = 255-(int)(level * 255 / toplv);
                    break;
                case Data.Job.JobType.战士:
                    r = (int)(level * 255 / toplv);
                    g = 255-(int)(level * 255 / toplv);
                    b = 255-(int)(level * 255 / toplv);
                    break;
                case Data.Job.JobType.射手:
                    g = (int)(level * 255 / toplv);
                    r = 255-(int)(level * 255 / toplv);
                    b = 255-(int)(level * 255 / toplv);
                    break;
            }
            return r.ToString("X") + g.ToString("X") + b.ToString("X");
        }
        private void OnServerChat(ServerChatEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (args.Text[0].ToString() == TShock.Config.CommandSpecifier)
            {
                return;
            }
            args.Handled = true;
            var group = player.Group;
            var job = Data.Job.GetJob(player.Name);
            long toplv = job.Level;
            if (job.jobType != Data.Job.JobType.白丁)
                foreach (var plr in TShock.Players)
                {
                    try
                    {
                        var _job = Data.Job.GetJob(plr.Name);
                        if (_job.jobType == job.jobType)
                        {
                            if (_job.Level > toplv)
                                toplv = _job.Level;
                        }
                    }
                    catch { }
                }
            TShock.Utils.Broadcast(group.Prefix + player.Name + group.Suffix + $"[c/{JobLevelRBG(job.jobType, job.Level, toplv)}:{job.jobType} lv{job.Level}]" + ":" + args.Text,
                byte.Parse(group.ChatColor.Split(',')[0]),
                byte.Parse(group.ChatColor.Split(',')[1]),
                byte.Parse(group.ChatColor.Split(',')[2]));
        }

        private void OnNpcStrike(NpcStrikeEventArgs args)
        {
            var ply = TSPlayer.FindByNameOrID(args.Player.name)[0];
            var item = ply.TPlayer.inventory[ply.TPlayer.selectedItem];
            var job = Data.Job.GetJob(ply.Name);
            NetMessage.SendData((int)PacketTypes.Status, -1, -1, Terraria.Localization.NetworkText.FromLiteral("Status 测试            "), 100, 2);
            if (item.magic && job.jobType == Data.Job.JobType.法师 ||
                item.melee && job.jobType == Data.Job.JobType.战士 ||
                item.ranged && job.jobType == Data.Job.JobType.射手)
            {
                args.Damage += args.Damage * (int)job.Level * Data.Config.GetConfig().Player.DamageAdd / 100;
            }
            args.Damage = args.Damage+args.Damage/(Data.Config.GetConfig().NPC.Level + 1)/10;
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
                ServerApi.Hooks.NpcStrike.Deregister(this, OnNpcStrike);
                ServerApi.Hooks.ServerChat.Deregister(this, OnServerChat);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNPCKilled);
            }
            base.Dispose(disposing);
        }
    }
}