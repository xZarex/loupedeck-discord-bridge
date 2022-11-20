namespace Loupedeck.DiscordBridgePlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class DiscordMuteCommand : PluginDynamicCommand
    {
        private bool _isMuted = false;
        private bool _discordDetected = false;


        private readonly String _imageMicOnResourcePath;
        private readonly String _imageMicOffResourcePath;
        private readonly String _imageDiscordOffResourcePath;

        public DiscordMuteCommand()
            : base(displayName: "Toggle Discord Mic", description: "Toggles microphone mute state", groupName: "Audio")
        {

            this._imageMicOnResourcePath = EmbeddedResources.FindFile("MicOn.png");
            this._imageMicOffResourcePath = EmbeddedResources.FindFile("MicOff.png");
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

            if (!this._isMuted)
            {
                var img = EmbeddedResources.ReadImage(this._imageMicOnResourcePath);
                this.DisplayName = img == null ? "null" : "Unmuted";
                return img;
            }

            var img2 = EmbeddedResources.ReadImage(this._imageMicOffResourcePath);
            this.DisplayName = img2 == null ? "null" : "Muted";
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

            var isMuted = e.IsMuted();
            if (isMuted != this._isMuted)
            {
                this._isMuted = isMuted;
                this.ActionImageChanged();
            }
        }

        protected override void RunCommand(String actionParameter)
        {
            if (!this._isMuted)
            {
                Discord.Instance.Mute();
            }
            else
            {
                Discord.Instance.UnMute();
            }
            
            this._isMuted = !this._isMuted;
            this.ActionImageChanged();
        }
    }
}
