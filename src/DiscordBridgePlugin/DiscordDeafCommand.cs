namespace Loupedeck.DiscordBridgePlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class DiscordDeafCommand : PluginDynamicCommand
    {
        private bool _isDeaf = false;
        private bool _discordDetected = false;


        private readonly String _imageOutOnResourcePath;
        private readonly String _imageOutOffResourcePath;
        private readonly String _imageDiscordOffResourcePath;

        public DiscordDeafCommand()
            : base(displayName: "Toggle Discord Output", description: "Toggles discord deaf state", groupName: "Audio")
        {

            this._imageOutOnResourcePath = EmbeddedResources.FindFile("OutOn.png");
            this._imageOutOffResourcePath = EmbeddedResources.FindFile("OutOff.png");
            this._imageDiscordOffResourcePath = EmbeddedResources.FindFile("DCOff.png");


            Discord.Instance.OnTick += this.Tick;
        }


        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (!this._discordDetected)
            {
                var img = EmbeddedResources.ReadImage(this._imageDiscordOffResourcePath);
                this.DisplayName = img == null ? "null" : "Discord not running";
                return img;
            }

            if (!this._isDeaf)
            {
                var img = EmbeddedResources.ReadImage(this._imageOutOnResourcePath);
                this.DisplayName = img == null ? "null" : "Not deaf";
                return img;
            }

            var img2 = EmbeddedResources.ReadImage(this._imageOutOffResourcePath);
            this.DisplayName = img2 == null ? "null" : "Deaf";
            return img2;
        }


        public void Tick(Object source, DiscordTickEventArgs e)
        {
            if (Discord.Instance.NeedsPlugin() && this.Plugin != null)
            {
                Discord.Instance.SetPlugin(this.Plugin);
                return;
            }


            var discordDetected = e.IsDiscordDetected();
            if (discordDetected != this._discordDetected)
            {
                this._discordDetected = discordDetected;
                this.ActionImageChanged();
            }

            var isMuted = e.IsOutMuted();
            if (isMuted != this._isDeaf)
            {
                this._isDeaf = isMuted;
                this.ActionImageChanged();
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (!this._isDeaf)
            {
                Discord.Instance.Deaf();
            }
            else
            {
                Discord.Instance.UnDeaf();
            }
            
            this._isDeaf = !this._isDeaf;
            this.ActionImageChanged();
        }
    }
}
