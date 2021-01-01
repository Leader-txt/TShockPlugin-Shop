using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;

namespace RainbowChat
{
    [ApiVersion(2, 1)]
    public class RainbowChat : TerrariaPlugin
    {
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Leader";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "Colorful chating!";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "RainbowChat";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(1, 0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the TestPlugin class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public RainbowChat(Main game) : base(game)
        {

        }

        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.ServerChat.Register(this, OnServerChat);
        }
        private void OnServerChat(ServerChatEventArgs args)
        {
            if (args.Text.Contains("/"))
                return;
            args.Handled = true;
            var player = TShock.Players[args.Who];
            Group group = player.Group;
            string ChatText = (group.Name != "default" ? "[" + group.Name + "]" + player.Name : player.Name) + ":";
            string console = ChatText + args.Text;
            Random random = new Random();
            foreach (char c in args.Text)
            {
                ChatText += "[c/" + random.Next(0, 16777215).ToString("x8") + ":" + c.ToString() + "]";
            }
            //player.SendDataFromPlayer(PacketTypes.ChatText, player.Index, ChatText);
            Console.WriteLine(console);
            TSPlayer.All.SendMessage(ChatText, Color.White);
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
