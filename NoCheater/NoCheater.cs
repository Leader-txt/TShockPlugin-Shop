using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using System.IO;

namespace TestPlugin
{
    [ApiVersion(2, 1)]
    public class NoCheater : TerrariaPlugin
    {
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Leader";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "反作弊";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "NoCheater";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(1, 0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the TestPlugin class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public NoCheater(Main game) : base(game)
        {

        }

        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }
        private void OnGetData(GetDataEventArgs args)
        {
            var user = TShock.Players[args.Msg.whoAmI];
            TShock.Utils.Broadcast(user.Name + "发送了数据包:" + args.MsgID, Color.Red);
            if (args.MsgID == PacketTypes.PlayerUpdate)
            {
                using (BinaryReader data = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                {
                    byte plr = data.ReadByte();//第一个字节是玩家号
                    BitsByte control = data.ReadByte();//此乃用户的操作
                    BitsByte pulley = data.ReadByte();//此乃用户的操作
                    var item = data.ReadByte();//此乃用户的操作
                    var pos = new Vector2(data.ReadSingle(), data.ReadSingle());
                    if (control[5])
                    {
                        string itemName = user.TPlayer.inventory[item].Name;
                        TShock.Utils.Broadcast(user.Name + "使用了" + itemName + "[i:" + user.TPlayer.inventory[item].netID + "]", Color.Yellow);
                    }
                }
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