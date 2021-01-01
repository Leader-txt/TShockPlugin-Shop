using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using System.Threading;
using System.Reflection;
using System.Timers;
using System.IO;
using Microsoft.Xna.Framework;
using MySql.Data;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using TShockAPI.DB;
using System.Data;
using Mono.Data.Sqlite;
using System.Windows.Forms;
using Terraria.ID;
using Terraria.GameContent.Achievements;
using System.Diagnostics.Contracts;
using System.ComponentModel.Design;

namespace Shop
{
    [ApiVersion(2, 1)]   
    public class Shop : TerrariaPlugin
    {
        public static string ConfigPath = "tshock\\shop.json";
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Leader";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "实现商店功能";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "Shop";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(3, 1, 0, 0);
        //public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Initializes a new instance of the TestPlugin class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public Shop(Main game) : base(game)
        {
            if (!File.Exists(Config.GetConfig().SQLPath))
            {
                SqliteConnection.CreateFile(Config.GetConfig().SQLPath);
                Data.connection.Open();
                Data.Command($"create table {Tables.Bank.TableName}({Tables.Bank.Columns.Account.Name} text,{Tables.Bank.Columns.Money.Name} int(64))");
                Data.Command("create table item(NetID int(32),Num int(32),Prize int(32))");
                Data.Command("create table change(Owner text,NetID int(32),Stack int(32),Perfix int(32),Prize int(32),Account int(32))");
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
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
            ServerApi.Hooks.NetGetData.Register(this, OnNetGetData);
            Commands.ChatCommands.Add(new Command("", shop, "shop"));
        }

        private void OnNetGetData(GetDataEventArgs args)
        {
            if (!Config.GetConfig().deathPunish.Open)
                return;
            var player = TShock.Players[args.Msg.whoAmI];
            if(player!=null&&args.MsgID==PacketTypes.DeadPlayer)
            {
                Bank bank = Data.GetBank(player.Name);
                Random random = new Random();
                long p = random.Next((int)Config.GetConfig().deathPunish.DropMinValue,
                    (int)Config.GetConfig().deathPunish.DropMaxValue);
                long del = bank.Money * p / 100;
                bank.Money -= del;
                bank.Save();
                player.SendErrorMessage("死亡惩罚已开启，您已被扣除" + del + "余额，您当前余额为" + bank.Money);
                if (Config.GetConfig().deathPunish.Spwan)
                    player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
            }
        }

        private void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            try
            {
                var player = TShock.Players[args.Who];
                foreach (var c in Change.GetChanges(player.Name))
                {
                    if (c.Account == -2)
                    {
                        player.GiveItem(c.NetID, c.Stack, c.Perfix);
                        player.SendSuccessMessage("您的商品已退回,请查收");
                        c.Delete();
                    }
                    if (c.Account == -1)
                    {
                        Bank ba = Data.GetBank(player.Name);
                        ba.Money += c.Prize;
                        ba.Save();
                        player.SendSuccessMessage($"您的商品:[i/s{c.Stack}:{c.NetID}]," +
                            $"价格:{c.Prize}已售出,货款已到账,请查收");
                        c.Delete();
                    }
                }
            }
            catch { }
        }

        string[] HelpText = new string[]
        {
            "/shop list,列出所有商品",
            "/shop buy 物品id [数量],购买物品,数量不填默认为1",
            "/shop check [玩家名字],查询余额，玩家名字不填默认为自己",
            "/shop pay 金额,充值,注意金币必须要在背包中,当前汇率为"+Config.GetConfig().CoinToMoney+"铜币/余额",
            "/shop change,玩家交易"
        };
        string[] HelpTextForAdmin = new string[]
        {
            "/shop add 商品id 商品单价 [商品数量],添加商品，商品数量不填默认为1",
            "/shop del 商品id",
            "/shop edit 金额 [玩家名字],玩家名字不填默认为自己",
            "/shop clear bank/item,清除所有玩家余额或所有商品"
        };
        string Permission = "shop.admin";
        private void shop(CommandArgs args)
        { 
            try
            {
                if (args.Parameters.Count() == 0 || args.Parameters[0] == "help")
                {
                    foreach (string s in HelpText)
                    {
                        args.Player.SendInfoMessage(s);
                    }
                    if (Config.GetConfig().LifeBuy)
                    {
                        args.Player.SendInfoMessage("/shop life,血量购买");
                    }
                    if (args.Player.HasPermission(Permission))
                    {
                        foreach (string s in HelpTextForAdmin)
                        {
                            args.Player.SendInfoMessage(s);
                        }                        
                    }
                    return;
                }
                Bank bank = Data.GetBank(args.Player.Name);
                switch (args.Parameters[0])
                {
                    case "change":
                        if (args.Parameters.Count() == 1 || args.Parameters[1] == "help")
                        {
                            args.Player.SendInfoMessage("/shop change list,列出所有交易商品");
                            args.Player.SendInfoMessage("/shop change add 物品编号 价格,添加交易商品");
                            args.Player.SendInfoMessage("/shop change find,查看可交易物品列表以及物品编号");
                            args.Player.SendInfoMessage("/shop change buy 商品编号,购买商品");
                            args.Player.SendInfoMessage("/shop change cancel 商品编号/all ,取消售卖物品,若为all则取消售卖所有物品");
                            if (args.Player.HasPermission(Permission))
                            {
                                args.Player.SendInfoMessage("/shop change del all/商品编号 [nr],删除商品,是否归还商品,若不归还,请填写nr,默认归还");
                            }
                            return;
                        }
                        switch (args.Parameters[1])
                        {
                            case "del":
                                if (!args.Player.HasPermission(Permission))
                                {
                                    args.Player.SendErrorMessage("您无权限使用此指令");
                                    return;
                                }
                                if (args.Parameters.Count() == 3 || args.Parameters[3] != "nr")
                                {
                                    if (args.Parameters[2] == "all")
                                    {
                                        Change[] changes = Change.GetChanges();
                                        foreach (var c in changes)
                                        {
                                            c.Delete();
                                            c.Account = -2;
                                            c.Save();
                                            foreach (var p in TShock.Players)
                                            {
                                                if (p.Name == c.Owner)
                                                {
                                                    p.GiveItem(c.NetID, c.Stack, c.Perfix);
                                                    p.SendSuccessMessage("您的商品已退回,请查收");
                                                    c.Delete();
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Change c = Change.GetChange(int.Parse(args.Parameters[2]));
                                        c.Delete();
                                        c.Account = -2;
                                        c.Save();
                                        foreach (var p in TShock.Players)
                                        {
                                            if (p.Name == c.Owner)
                                            {
                                                p.GiveItem(c.NetID, c.Stack, c.Perfix);
                                                p.SendSuccessMessage("您的商品已退回,请查收");
                                                c.Delete();
                                            }
                                        }
                                    }
                                }
                                else if (args.Parameters[3] == "nr")
                                {
                                    if (args.Parameters[2] == "all")
                                    {
                                        Change[] changes = Change.GetChanges();
                                        foreach (var c in changes)
                                        {
                                            c.Delete();
                                        }
                                    }
                                    else { 
                                    }
                                }
                                break;
                            case "cancel":
                                if (args.Parameters[2] == "all")
                                {
                                    Change[] changes = Change.GetChanges(args.Player.Name);
                                    foreach(var c in changes)
                                    {
                                        args.Player.GiveItem(c.NetID, c.Stack, c.Perfix);
                                        c.Delete();
                                    }
                                    args.Player.SendSuccessMessage("已归还全部商品,请查收");
                                    return;
                                }
                                int _account = int.Parse(args.Parameters[2]);
                                Change cha = Change.GetChange(_account, args.Player.Name);
                                args.Player.GiveItem(cha.NetID, cha.Stack, cha.Perfix);
                                cha.Delete();
                                args.Player.SendSuccessMessage("已归还商品,请查收");
                                break;
                            case "buy":
                                int account = int.Parse(args.Parameters[2]);
                                if (account < 0)
                                {
                                    args.Player.SendErrorMessage("对不起,此商品已售出,请浏览其他商品吧~");
                                    return;
                                }
                                Change ch = Change.GetChange(account);
                                if (bank.Money < ch.Prize)
                                {
                                    args.Player.SendErrorMessage("余额不足!");
                                    return;
                                }
                                args.Player.GiveItem(ch.NetID, ch.Stack, ch.Perfix);
                                bank.Money -= ch.Prize;
                                bank.Save();
                                args.Player.SendSuccessMessage("您的商品已到账,请查收,您当前余额为:" + bank.Money);
                                ch.Delete();
                                ch.Account = -1;
                                ch.Save();
                                foreach (var p in TShock.Players)
                                {
                                    if(p!=null)
                                    if(p.Name == ch.Owner)
                                    {
                                        Bank ba = Data.GetBank(p.Name);
                                        ba.Money += ch.Prize;
                                        ba.Save();
                                        p.SendSuccessMessage($"您的商品:商品编号:{account},[i/s{ch.Stack}:{ch.NetID}]," +
                                            $"价格:{ch.Prize}已售出,购买者:{args.Player.Name},货款已到账,请查收");
                                        ch.Delete();
                                        break;
                                    }
                                }
                                break;
                            case "find":
                                for(int i=0; i< args.Player.TPlayer.inventory.Count(); i++)
                                {
                                    if (args.Player.TPlayer.inventory[i].stack == 0)
                                        continue;
                                    args.Player.SendInfoMessage($"商品编号:{i},[i/s{args.Player.TPlayer.inventory[i].stack}" +
                                        $":{args.Player.TPlayer.inventory[i].netID}],数量{args.Player.TPlayer.inventory[i].stack}" +
                                        $"前缀:{TShock.Utils.GetPrefixById(args.Player.TPlayer.inventory[i].prefix)}");
                                }
                                break;
                            case "add":
                                var item = args.Player.TPlayer.inventory[int.Parse(args.Parameters[2])];
                                if (item.stack == 0)
                                {
                                    args.Player.SendErrorMessage("抱歉,您无法出售空的物品");
                                    return;
                                }
                                (new Change(args.Player.Name, item.netID, item.stack, item.prefix, int.Parse(args.Parameters[3]), Change.GetChanges().Count())).Save();
                                TShock.Utils.Broadcast($"{args.Player.Name}正在出售{Lang.GetItemName(item.netID)}[i/s{item.stack}:{item.netID}]," +
                                    $"数量:{item.stack},商品编号:{Change.GetChanges().Count() - 1},价格:{int.Parse(args.Parameters[3])},欲购从速,快来抢购吧", Color.Green);
                                item.stack = 0;
                                for (int i = 0; i < 58; i++)
                                    args.Player.SendData(PacketTypes.PlayerSlot, "", args.Player.Index, i, args.Player.TPlayer.inventory[i].prefix);
                                break;
                            case "list":
                                foreach (var c in Change.GetChanges())
                                {
                                    args.Player.SendInfoMessage($"商品编号:{c.Account},[i/s{c.Stack}:{c.NetID}],{Lang.GetItemName(c.NetID)}:{c.Stack},价格:{c.Prize},出售者:{c.Owner}");
                                }
                                break;
                        }
                        break;
                    case "life":
                        if (!Config.GetConfig().LifeBuy)
                        {
                            args.Player.SendErrorMessage("血量购买未启用，请联系管理员开启");
                            return;
                        }
                        if (args.Parameters.Count() == 1 || args.Parameters[1] == "help")
                        {
                            args.Player.SendInfoMessage("当前血量价格:"+Config.GetConfig().MoneyToLife+"余额/血量,最大血量为"+
                                Config.GetConfig().MaxLife);
                            args.Player.SendInfoMessage("/shop life buy 血量,购买血量");
                            if (args.Player.HasPermission(Permission))
                            {
                                args.Player.SendInfoMessage("/shop life edit 血量 [玩家名字],修改血量,不填玩家名字则为自己");
                            }
                            return;
                        }
                        if (args.Parameters[1] == "buy")
                        {
                            int life = Math.Min(int.Parse(args.Parameters[2]), Config.GetConfig().MaxLife- args.Player.TPlayer.statLifeMax);
                            int money = life * Config.GetConfig().MoneyToLife;
                            if (bank.Money < money)
                            {
                                args.Player.SendErrorMessage("余额不足！还差" + (money - bank.Money));
                                return;
                            }
                            bank.Money -= money;
                            bank.Save();
                            SetLife(life + args.Player.TPlayer.statLifeMax, args.Player.Name);
                            args.Player.SendInfoMessage("购买成功！您当前生命值为" + args.Player.TPlayer.statLifeMax+"您当前余额为"+bank.Money);
                            return;
                        }
                        if (args.Parameters[1] == "edit")
                        {
                            if (!args.Player.HasPermission(Permission))
                            {
                                args.Player.SendErrorMessage("抱歉，您无权限使用此命令！");
                                return;
                            }
                            string Name = args.Parameters.Count() == 3 ? args.Player.Name : args.Parameters[3];
                            SetLife(int.Parse(args.Parameters[2]), Name);
                            args.Player.SendSuccessMessage("修改成功！" + (args.Parameters.Count() == 3 ? "您" : args.Parameters[3]) + "的血量为" +
                                TSPlayer.FindByNameOrID(Name)[0].TPlayer.statLifeMax);
                            return;
                        }
                        break;
                    case "clear":
                        if (!args.Player.HasPermission(Permission))
                        {
                            args.Player.SendErrorMessage("抱歉，您无权限使用此命令！");
                            return;
                        }
                        if (args.Parameters[1] == "bank")
                        {
                            Data.Command("delete from bank");
                            args.Player.SendSuccessMessage("删除成功!");
                        }
                        else if (args.Parameters[1] == "item")
                        {
                            Data.Command("delete from item");
                            args.Player.SendSuccessMessage("删除成功！");
                        }
                        break;
                    case "edit":
                        if (!args.Player.HasPermission(Permission))
                        {
                            args.Player.SendErrorMessage("抱歉，您无权限使用此命令！");
                            return;
                        }
                        Bank b = args.Parameters.Count() == 3 ? Data.GetBank(args.Parameters[2]) : bank;
                        b.Money = int.Parse(args.Parameters[1]);
                        b.Save();
                        args.Player.SendSuccessMessage("修改成功！"+ (args.Parameters.Count() == 3 ?args.Parameters[2]:args.Player.Name)
                            +"的余额为"+ (args.Parameters.Count() == 3 ? Data.GetBank(args.Parameters[2]) : bank).Money);
                        break;
                    case "del":
                        if (!args.Player.HasPermission(Permission))
                        {
                            args.Player.SendErrorMessage("抱歉，您无权限使用此命令！");
                            return;
                        }
                        try
                        {
                            Item.GetItem(int.Parse(args.Parameters[1]));
                            Data.Command($"delete from item where NetID={args.Parameters[1]}");
                            args.Player.SendSuccessMessage("删除成功");
                        }
                        catch
                        {
                            args.Player.SendErrorMessage("查无此项");
                        }
                        break;
                    case "add":
                        if (!args.Player.HasPermission(Permission))
                        {
                            args.Player.SendErrorMessage("抱歉，您无权限使用此命令！");
                            return;
                        }
                        try
                        {
                            Item.GetItem(int.Parse(args.Parameters[1]));
                            args.Player.SendErrorMessage("无法添加相同的项");
                        }
                        catch
                        {
                            var item = new Item(int.Parse(args.Parameters[1]), int.Parse(args.Parameters[2]),
                                args.Parameters.Count() == 3 ? 1 : int.Parse(args.Parameters[3]));
                            item.Save();
                            args.Player.SendSuccessMessage("添加成功");
                        }
                        break;
                    case "list":
                        var reader = Data.Command("select * from item");
                        string result = "";
                        while (reader.Read())
                        {
                            result += $"{Lang.GetItemName(reader.GetInt32(0))}[i/s{reader.GetInt32(1)}:{reader.GetInt32(0)}],id:{reader.GetInt32(0)},价格:" +
                                $"{reader.GetInt32(2)}/{reader.GetInt32(1)}个" + "\r\n";
                        }
                        result += "您的余额为" + bank.Money;
                        //SendInfo(args.Player, Colorful(Color.Yellow, result));
                        args.Player.SendInfoMessage(result);
                        break;
                    case "buy":
                        int id = int.Parse(args.Parameters[1]);
                        int num = args.Parameters.Count() == 3 ? int.Parse(args.Parameters[2]) : 1;
                        if (num < 0)
                        {
                            args.Player.SendErrorMessage("数量不能为负！");
                            return;
                        }
                        var ite = Item.GetItem(id);
                        if (ite.Prize * num < bank.Money)
                        {
                            bank.Money -= ite.Prize * num;
                            bank.Save();
                            num *= ite.Num;
                            var BuyedItem = TShock.Utils.GetItemById(ite.NetID);
                            while (num>BuyedItem.maxStack)
                            {
                                args.Player.GiveItem(ite.NetID, BuyedItem.maxStack);
                                num -= BuyedItem.maxStack;
                            }
                            args.Player.GiveItem(ite.NetID, num);
                            args.Player.SendSuccessMessage("您购买的商品已到账，请查收");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("余额不足，还差" + (ite.Prize * num - bank.Money));
                        }
                        break;
                    case "check":
                        if (args.Parameters.Count() == 1)
                            args.Player.SendSuccessMessage("您的余额为" + bank.Money);
                        else
                            args.Player.SendSuccessMessage(args.Parameters[1] + "的余额为" + Data.GetBank(args.Parameters[1]).Money);
                        break;
                    case "pay":
                        int prize = int.Parse(args.Parameters[1]);
                        int pack = 0;
                        foreach (var i in args.Player.TPlayer.inventory)
                        {
                            if(i.netID == ItemID.CopperCoin)
                            {
                                pack += i.stack;
                                i.stack = 0;
                            }
                            if (i.netID == ItemID.SilverCoin)
                            {
                                pack += i.stack*100;
                                i.stack = 0;
                            }
                            else if (i.netID == ItemID.GoldCoin)
                            {
                                pack += 100 * i.stack*100;
                                i.stack = 0;
                            }
                            else if (i.netID == ItemID.PlatinumCoin)
                            {
                                pack += 100 * 100 * i.stack*100;
                                i.stack = 0;
                            }
                        }
                        //pack /= Config.GetConfig().CoinToMoney;
                        if (prize > (pack/ Config.GetConfig().CoinToMoney))
                        {
                            args.Player.SendErrorMessage("背包中钱币不足");
                            return;
                        }
                        else
                        {
                            pack -= prize* Config.GetConfig().CoinToMoney;
                            for (int i = 0; i < 58; i++)
                                args.Player.SendData(PacketTypes.PlayerSlot, "", args.Player.Index, i, args.Player.TPlayer.inventory[i].prefix);
                            bank.Money += prize;
                            bank.Save();
                            //for (int i = 0; i < pack; i++)
                            if (pack > 100 * 100 * 100)
                            {
                                int numb = pack / (100 * 100 * 100);
                                args.Player.GiveItem(ItemID.PlatinumCoin, numb);
                                pack -= numb * 100 * 100 * 100;
                            }
                            if (pack > 100 * 100 )
                            {
                                int numb = pack / (100 * 100 );
                                args.Player.GiveItem(ItemID.GoldCoin, numb);
                                pack -= numb * 100 * 100 ;
                            }
                            if (pack > 100 )
                            {
                                int numb = pack / 100;
                                args.Player.GiveItem(ItemID.SilverCoin, numb);
                                pack -= numb * 100;
                            }
                            args.Player.GiveItem(ItemID.CopperCoin, pack);
                            args.Player.SendSuccessMessage("充值已到账，请查收，您的余额为" + bank.Money);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                args.Player.SendErrorMessage("命令执行错误，您输入的似乎不正确");
                Console.WriteLine(args.Player.Name + "执行的命令报错如下");
                Console.WriteLine(ex);
            }
        }
        void SetLife(int life,string Name)
        {
            var ply = TSPlayer.FindByNameOrID(Name)[0].TPlayer;
            life = Math.Min(life, Config.GetConfig().MaxLife);
            ply.statLifeMax = life;
            NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1,null, ply.whoAmI);
        }

        private void OnNpcStrike(NpcStrikeEventArgs args)
        {
            foreach(int i in Config.GetConfig().IgnoreNPCID)
            {
                if (i == args.Npc.netID)
                    return;
            }
            Bank bank = Data.GetBank(args.Player.name);
            bank.Money++;
            bank.Save();
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