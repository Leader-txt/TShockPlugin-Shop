using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace SaveWords
{
    [ApiVersion(2, 1)]
    public class SaveWords : TerrariaPlugin
    {
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        public override string Author => "Leader";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        public override string Description => "缓存聊天信息";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name => "SaveWords";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version => new Version(1, 0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the SaveWords class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        public SaveWords(Main game) : base(game)
        {

        }

        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>        
        public override void Initialize()
        {
            words = new List<Word>();
            Commands.ChatCommands.Add(new Command("words.admin", GetWords, "words"));
            ServerApi.Hooks.ServerChat.Register(this, OnServerChat);
        }

        private void OnServerChat(ServerChatEventArgs args)
        {
            var plr = TShock.Players[args.Who];
            words.Add(new Word(plr.Name, plr.Group.Name, plr.Group.Prefix,args.Text));
        }

        List<Word> words;
        class Word
        {
            public string PlayerName { get; set; }
            public string GroupName { get; set; }
            public string Prefix { get; set; }
            public string Text { get; set; }
            public Word(string player,string group,string prefix,string text)
            {
                PlayerName = player;
                GroupName = group;
                Prefix = prefix;
                Text = text;
            }
        }
        private void GetWords(CommandArgs args)
        {
            if(words.Count != 0)
            {
                args.Player.SendInfoMessage(JsonConvert.SerializeObject(words));
                words = new List<Word>();
            }
            else
            {
                args.Player.SendInfoMessage("null");
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