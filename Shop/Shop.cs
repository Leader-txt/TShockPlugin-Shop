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
using Microsoft.Data.Sqlite;
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
        public override string Author => "Leader,Cai upgrade to .net core6";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "实现商店功能|Make the store can be truely used.";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "Shop";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(3, 2, 0, 0);
        //public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Initializes a new instance of the TestPlugin class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public Shop(Main game) : base(game)
        {
            if (!File.Exists(Config.GetConfig().SQLPath))
            {
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
                //player.SendErrorMessage("The death punish is enable，your money had been deleted:" + del + "，your rest money is" + bank.Money);
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
                        //player.SendSuccessMessage("Your item has been returned,please check.");
                        c.Delete();
                    }
                    if (c.Account == -1)
                    {
                        Bank ba = Data.GetBank(player.Name);
                        ba.Money += c.Prize;
                        ba.Save();
                        player.SendSuccessMessage($"您的商品:[i/s{c.Stack}:{c.NetID}]," +
                            $"价格:{c.Prize}已售出,货款已到账,请查收");
                        //player.SendSuccessMessage($"Your item:[i/s{c.Stack}:{c.NetID}]has been selled," + $"prize:{c.Prize},the prize has been add to your money,please check.");
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
            /*"/shop list,list all the items that the system shop sells",
            "/shop buy ItemID [i],buy item ,i:how much do you want to buy the item,if the parameter is empty,i=1.",
            "/shop check [PlayerName],check the target player's rest money,if empty,check yourself.",
            "/shop pay [i],recharge,mention:The coin must in your package,the Current exchange rate is"+Config.GetConfig().CoinToMoney+"one copper coin/money",
            "/shop change,玩家交易"*/
        };
        string[] HelpTextForAdmin = new string[]
        {
            "/shop add 商品id 商品单价 [商品数量],添加商品，商品数量不填默认为1",
            "/shop del 商品id",
            "/shop edit 金额 [玩家名字],玩家名字不填默认为自己",
            "/shop clear bank/item,清除所有玩家余额或所有商品"
            /*"/shop add ItemID prize [i],add item to system shop,i:how many items do you want to sell once,if empty,i=1",
            "/shop del ItemID,delete item from the system shop.",
            "/shop edit monsy [PlayerName],Edit the target player's rest money,if playername is empty,edit yourself",
            "/shop clear bank/item,clear all the players' rest money or clear the system shop"*/
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
                        //args.Player.SendInfoMessage("/shop life,buy life");         
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
                            /*args.Player.SendInfoMessage("/shop change list,list all the items and their sellID in exchanging.");
                            args.Player.SendInfoMessage("/shop change add ItemID2 prize,sell the item in your package(ItemID2:see /shop change find).");
                            args.Player.SendInfoMessage("/shop change find,check which item can you sell and the ItemID2 of it.");
                            args.Player.SendInfoMessage("/shop change buy sellID,buy item,");
                            args.Player.SendInfoMessage("/shop change cancel sellID/all ,cancle selling item,if all cancle all items you sell.");*/
                            if (args.Player.HasPermission(Permission))
                            {
                                args.Player.SendInfoMessage("/shop change del all/商品编号 [nr],删除商品,是否归还商品,若不归还,请填写nr,默认归还");
                                //args.Player.SendInfoMessage("/shop change del all/sellID [nr],delete selling item,nr:don't return the item,if empty,return.");
                            }
                            return;
                        }
                        switch (args.Parameters[1])
                        {
                            case "del":
                                if (!args.Player.HasPermission(Permission))
                                {
                                    args.Player.SendErrorMessage("您无权限使用此指令!");
                                    //args.Player.SendErrorMessage("You don't have the promission to use this command!");
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
                                                    //p.SendSuccessMessage("Your item has returned to your package,please check.");
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
                                                //p.SendSuccessMessage("Your item has returned to your package,please check.");
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
                                    //args.Player.SendSuccessMessage("All of your items have been returned,please check.");
                                    return;
                                }
                                int _account = int.Parse(args.Parameters[2]);
                                Change cha = Change.GetChange(_account, args.Player.Name);
                                args.Player.GiveItem(cha.NetID, cha.Stack, cha.Perfix);
                                cha.Delete();
                                args.Player.SendSuccessMessage("已归还商品,请查收");
                                //args.Player.SendSuccessMessage("Your item has been returned to your package,please check.");
                                break;
                            case "buy":
                                int account = int.Parse(args.Parameters[2]);
                                if (account < 0)
                                {
                                    args.Player.SendErrorMessage("对不起,此商品已售出,请浏览其他商品吧~");
                                    //args.Player.SendErrorMessage("Sorry,the item is selling out :(,please view the other items :)");
                                    return;
                                }
                                Change ch = Change.GetChange(account);
                                if (bank.Money < ch.Prize)
                                {
                                    args.Player.SendErrorMessage("余额不足!");
                                    //args.Player.SendErrorMessage("You don't have enough money! :(");
                                    return;
                                }
                                args.Player.GiveItem(ch.NetID, ch.Stack, ch.Perfix);
                                bank.Money -= ch.Prize;
                                bank.Save();
                                args.Player.SendSuccessMessage("您的商品已到账,请查收,您当前余额为:" + bank.Money);
                                //args.Player.SendSuccessMessage("The item you bought has reached,please check,your rest money:" + bank.Money);
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
                                        //p.SendSuccessMessage($"Your item:sellID:{account},[i/s{ch.Stack}:{ch.NetID}]," +$"prize:{ch.Prize}has been selled,buyer:{args.Player.Name},the money has reached,please check.");
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
                                    /*args.Player.SendInfoMessage($"ItemID2:{i},[i/s{args.Player.TPlayer.inventory[i].stack}" +
                                        $":{args.Player.TPlayer.inventory[i].netID}],stack:{args.Player.TPlayer.inventory[i].stack}" +
                                        $"prefix:{TShock.Utils.GetPrefixById(args.Player.TPlayer.inventory[i].prefix)}");*/
                                }
                                break;
                            case "add":
                                var item = args.Player.TPlayer.inventory[int.Parse(args.Parameters[2])];
                                if (item.stack == 0)
                                {
                                    args.Player.SendErrorMessage("抱歉,您无法出售空的物品");
                                    //args.Player.SendErrorMessage("Sorry,you can't sell empth item! :(");
                                    return;
                                }
                                (new Change(args.Player.Name, item.netID, item.stack, item.prefix, int.Parse(args.Parameters[3]), Change.GetChanges().Count())).Save();
                                TShock.Utils.Broadcast($"{args.Player.Name}正在出售{Lang.GetItemName(item.netID)}[i/s{item.stack}:{item.netID}]," +
                                    $"数量:{item.stack},商品编号:{Change.GetChanges().Count() - 1},价格:{int.Parse(args.Parameters[3])},欲购从速,快来抢购吧", Color.Green);
                                /*TShock.Utils.Broadcast($"{args.Player.Name}is selling{Lang.GetItemName(item.netID)}[i/s{item.stack}:{item.netID}]," +
                                    $"stack:{item.stack},sellID:{Change.GetChanges().Count() - 1},prize:{int.Parse(args.Parameters[3])},buy it now! :)", Color.Green);*/
                                item.stack = 0;
                                for (int i = 0; i < 58; i++)
                                    args.Player.SendData(PacketTypes.PlayerSlot, "", args.Player.Index, i, args.Player.TPlayer.inventory[i].prefix);
                                break;
                            case "list":
                                foreach (var c in Change.GetChanges())
                                {
                                    args.Player.SendInfoMessage($"商品编号:{c.Account},[i/s{c.Stack}:{c.NetID}],{Lang.GetItemName(c.NetID)}:{c.Stack},价格:{c.Prize},出售者:{c.Owner}");
                                    //args.Player.SendInfoMessage($"SellID:{c.Account},[i/s{c.Stack}:{c.NetID}],{Lang.GetItemName(c.NetID)}:{c.Stack},priz:{c.Prize},seller:{c.Owner}");
                                }
                                break;
                        }
                        break;
                    case "life":
                        if (!Config.GetConfig().LifeBuy)
                        {
                            args.Player.SendErrorMessage("血量购买未启用，请联系管理员开启");
                            //args.Player.SendErrorMessage("Life buying is disable，please communicate with the administrator to enable it.");
                            return;
                        }
                        if (args.Parameters.Count() == 1 || args.Parameters[1] == "help")
                        {
                            args.Player.SendInfoMessage("当前血量价格:"+Config.GetConfig().MoneyToLife+"余额/血量,最大血量为"+
                                Config.GetConfig().MaxLife);
                            args.Player.SendInfoMessage("/shop life buy 血量,购买血量");
                            /*args.Player.SendInfoMessage("Life prize now:"+Config.GetConfig().MoneyToLife+"money/life,the ,max life you can buy:"+
                                Config.GetConfig().MaxLife);
                            args.Player.SendInfoMessage("/shop life buy [i],buy life,i:how many life do you want to buy.");*/
                            if (args.Player.HasPermission(Permission))
                            {
                                args.Player.SendInfoMessage("/shop life edit 血量 [玩家名字],修改血量,不填玩家名字则为自己");
                                //args.Player.SendInfoMessage("/shop life edit life [PlayerName],edit the life,if PlayerName is empty,edit yourself.");
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
                                //args.Player.SendErrorMessage("You don't have enough money！You still need" + (money - bank.Money));
                                return;
                            }
                            bank.Money -= money;
                            bank.Save();
                            SetLife(life + args.Player.TPlayer.statLifeMax, args.Player.Name);
                            args.Player.SendInfoMessage("购买成功！您当前生命值为" + args.Player.TPlayer.statLifeMax+"您当前余额为"+bank.Money);
                            //args.Player.SendInfoMessage("Life buying successfully！:) You HP now:" + args.Player.TPlayer.statLifeMax+"Yoour rest money:"+bank.Money);
                            return;
                        }
                        if (args.Parameters[1] == "edit")
                        {
                            if (!args.Player.HasPermission(Permission))
                            {
                                args.Player.SendErrorMessage("抱歉，您无权限使用此命令！");
                                //args.Player.SendErrorMessage("Sorry,you don't have the promission to use the command! :(");
                                return;
                            }
                            string Name = args.Parameters.Count() == 3 ? args.Player.Name : args.Parameters[3];
                            SetLife(int.Parse(args.Parameters[2]), Name);
                            args.Player.SendSuccessMessage("修改成功！" + (args.Parameters.Count() == 3 ? "您" : args.Parameters[3]) + "的血量为" +
                                TSPlayer.FindByNameOrID(Name)[0].TPlayer.statLifeMax);
                            /*args.Player.SendSuccessMessage("Edit successfully！ :)" + (args.Parameters.Count() == 3 ? "Your" : args.Parameters[3]) + "HP now is" +
                                TSPlayer.FindByNameOrID(Name)[0].TPlayer.statLifeMax);*/
                            return;
                        }
                        break;
                    case "clear":
                        if (!args.Player.HasPermission(Permission))
                        {
                            args.Player.SendErrorMessage("抱歉，您无权限使用此命令！");
                            //args.Player.SendErrorMessage("Sorry,you don't have the promission to use the command! :(");
                            return;
                        }
                        if (args.Parameters[1] == "bank")
                        {
                            Data.Command("delete from bank");
                            args.Player.SendSuccessMessage("删除成功!");
                            //args.Player.SendSuccessMessage("Delete successfully! :)");
                        }
                        else if (args.Parameters[1] == "item")
                        {
                            Data.Command("delete from item");
                            args.Player.SendSuccessMessage("删除成功！");
                            //args.Player.SendSuccessMessage("Delete successfully! :)");
                        }
                        break;
                    case "edit":
                        if (!args.Player.HasPermission(Permission))
                        {
                            args.Player.SendErrorMessage("抱歉，您无权限使用此命令！");
                            //args.Player.SendErrorMessage("Sorry,you don't have the promission to use the command! :(");
                            return;
                        }
                        Bank b = args.Parameters.Count() == 3 ? Data.GetBank(args.Parameters[2]) : bank;
                        b.Money = int.Parse(args.Parameters[1]);
                        b.Save();
                        args.Player.SendSuccessMessage("修改成功！"+ (args.Parameters.Count() == 3 ?args.Parameters[2]:args.Player.Name)
                            +"的余额为"+ (args.Parameters.Count() == 3 ? Data.GetBank(args.Parameters[2]) : bank).Money);
                        /*args.Player.SendSuccessMessage("Edit successfully！ :) "+ (args.Parameters.Count() == 3 ?args.Parameters[2]:args.Player.Name)
                            +"'s HP now is"+ (args.Parameters.Count() == 3 ? Data.GetBank(args.Parameters[2]) : bank).Money);*/
                        break;
                    case "del":
                        if (!args.Player.HasPermission(Permission))
                        {
                            args.Player.SendErrorMessage("抱歉，您无权限使用此命令！");
                            //args.Player.SendErrorMessage("Sorry,you don't have the promission to use the command! :(");
                            return;
                        }
                        try
                        {
                            Item.GetItem(int.Parse(args.Parameters[1]));
                            Data.Command($"delete from item where NetID={args.Parameters[1]}");
                            args.Player.SendSuccessMessage("删除成功");
                            //args.Player.SendSuccessMessage("Delete successfully! :)");
                        }
                        catch
                        {
                            args.Player.SendErrorMessage("查无此项");
                            //args.Player.SendErrorMessage("Sorry, we can't find this item. :(");
                        }
                        break;
                    case "add":
                        if (!args.Player.HasPermission(Permission))
                        {
                            args.Player.SendErrorMessage("抱歉，您无权限使用此命令！");
                            //args.Player.SendErrorMessage("Sorry,you don't have the promission to use the command! :(");
                            return;
                        }
                        try
                        {
                            Item.GetItem(int.Parse(args.Parameters[1]));
                            args.Player.SendErrorMessage("无法添加相同的项");
                            //args.Player.SendErrorMessage("Sorry,you can't add the same item. :(");
                        }
                        catch
                        {
                            var item = new Item(int.Parse(args.Parameters[1]), int.Parse(args.Parameters[2]),
                                args.Parameters.Count() == 3 ? 1 : int.Parse(args.Parameters[3]));
                            item.Save();
                            args.Player.SendSuccessMessage("添加成功");
                            //args.Player.SendSuccessMessage("Adding successfully!");
                        }
                        break;
                    case "list":
                        var reader = Data.Command("select * from item");
                        string result = "";
                        while (reader.Read())
                        {
                            result += $"{Lang.GetItemName(reader.GetInt32(0))}[i/s{reader.GetInt32(1)}:{reader.GetInt32(0)}],id:{reader.GetInt32(0)},价格:" +
                                $"{reader.GetInt32(2)}/{reader.GetInt32(1)}个" + "\r\n";
                            /*result += $"{Lang.GetItemName(reader.GetInt32(0))}[i/s{reader.GetInt32(1)}:{reader.GetInt32(0)}],id:{reader.GetInt32(0)},prize:" +
                                $"{reader.GetInt32(2)}/{reader.GetInt32(1)}" + "\r\n";*/
                        }
                        result += "您的余额为" + bank.Money;
                        //result += "Your rest money is" + bank.Money;
                        //SendInfo(args.Player, Colorful(Color.Yellow, result));
                        args.Player.SendInfoMessage(result);
                        break;
                    case "buy":
                        int id = int.Parse(args.Parameters[1]);
                        int num = args.Parameters.Count() == 3 ? int.Parse(args.Parameters[2]) : 1;
                        if (num < 0)
                        {
                            args.Player.SendErrorMessage("数量不能为负！");
                            //args.Player.SendErrorMessage("The number can't less than zero!");
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
                            //args.Player.SendSuccessMessage("The item you bought has reached,please check.");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("余额不足，还差" + (ite.Prize * num - bank.Money));
                            //args.Player.SendErrorMessage("You don't have enough money！You still need" + (money - bank.Money));
                        }
                        break;
                    case "check":
                        if (args.Parameters.Count() == 1)
                            args.Player.SendSuccessMessage("您的余额为" + bank.Money);
                            //args.Player.SendSuccessMessage("Your rest money is" + bank.Money);
                        else
                            args.Player.SendSuccessMessage(args.Parameters[1] + "的余额为" + Data.GetBank(args.Parameters[1]).Money);
                            //args.Player.SendSuccessMessage(args.Parameters[1] + "'s rest money is" + Data.GetBank(args.Parameters[1]).Money);
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
                            //args.Player.SendErrorMessage("There isn't enough coins in your package.");
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
                            //args.Player.SendSuccessMessage("The recharge has reached，please check，your rest money is" + bank.Money);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                args.Player.SendErrorMessage("命令执行错误，您输入的似乎不正确");
                Console.WriteLine(args.Player.Name + "执行的命令报错如下");
                //args.Player.SendErrorMessage("The command excuted returned an error,the command you just typed may uncurrectly.");
                //Console.WriteLine(args.Player.Name + " excuted command returned an error:");
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
