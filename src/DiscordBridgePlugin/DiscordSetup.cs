namespace Loupedeck.DiscordBridgePlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class DiscordSetup : PluginDynamicCommand
    {
        public DiscordSetup()
            : base(displayName: "Setup", description: "Setup Discord, this plugin needs a clientid:secret, check github for more info", groupName: "Setup")
        {
            Discord.Instance.OnTick += this.Tick;
            this.AddParameter("settings", $"clientId:secret", "settings");
            this.MakeProfileAction("text;clientId:secret | More info https://bit.ly/3AvYUnD");

        }

        private void Tick(object source, DiscordTickEventArgs e)
        {
            if (Discord.Instance.NeedsPlugin() && this.Plugin != null)
                Discord.Instance.SetPlugin(this.Plugin);
        }

        protected override void RunCommand(String actionParameter)
        {
            this.Plugin.TryGetPluginSetting("DISCORD2refresh_token", out var refresh_token);
            if (String.IsNullOrEmpty(refresh_token))
            {
                if (Discord.Instance.Setup(actionParameter))
                {
                    this.DisplayName = "Setup successful";
                }
                else
                {
                    this.DisplayName = "Setup failed";
                }
                this.ActionImageChanged();
                return;
            }
            this.DisplayName = "Discord already connected!";
            this.ActionImageChanged();
        }
    }
}
