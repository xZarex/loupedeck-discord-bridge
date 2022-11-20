namespace Loupedeck.DiscordBridgePlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Timers;

    public delegate void DiscordTickEvent(object source, DiscordTickEventArgs e);

    public class DiscordTickEventArgs : EventArgs
    {
        private Boolean _isMuted = false;
        private Boolean _isOutMuted = false;
        private Boolean _discordDetected = false;

        public DiscordTickEventArgs(Boolean isMuted, Boolean isOutMuted, Boolean discordDetected)
        {
            this._isMuted = isMuted;
            this._isOutMuted = isOutMuted;
            this._discordDetected = discordDetected;
        }
        public Boolean IsMuted() => this._isMuted;
        public Boolean IsOutMuted() => this._isOutMuted;
        public Boolean IsDiscordDetected() => this._discordDetected;
    }

    sealed class Discord
    {
        private static readonly Discord _instance = new Discord();
        private System.Timers.Timer _timer;
        private Boolean _isMuted = false;
        private Boolean _isOutMuted = false;
        private Boolean _discordDetected = false;
        private Plugin _plugin = null;

        private String _access_token = "";
        private String _refresh_token = "";
        private String _client_id = "";
        private String _client_secret = "";

        const String accessTokenSetting = "DISCORDaccess_token";
        const String refreshTokenSetting = "DISCORDrefresh_token";
        const String clientIdSetting = "DISCORDclient_id";
        const String clientSecretSetting = "DISCORDclient_secret";


        public event DiscordTickEvent OnTick;
        static Discord()
        {
        }

        private Discord()
        {
            this._timer = new System.Timers.Timer();
            this._timer.Interval = 500;
            this._timer.Elapsed += this.Tick;
            this._timer.Start();
        }

        public static Discord Instance
        {
            get
            {
                return _instance;
            }
        }

        public void SetPlugin(Plugin plugin)
        {
            this._plugin = plugin;

            

            if (plugin.TryGetPluginSetting(accessTokenSetting, out var accessToken))
            {
                this._access_token = accessToken;
            }
            if (plugin.TryGetPluginSetting(refreshTokenSetting, out var refreshToken))
            {
                this._refresh_token = refreshToken;
            }
            if (plugin.TryGetPluginSetting(clientIdSetting, out var clientId))
            {
                this._client_id = clientId;
            }
            if (plugin.TryGetPluginSetting(clientSecretSetting, out var clientSecret))
            {
                this._client_secret = clientSecret;
            }

            this.Tick();
        }

        public Boolean NeedsPlugin() => this._plugin == null;

        public void Tick() => this.Tick(null, null);

        private void Tick(object sender, ElapsedEventArgs e)
        {
            if (this._plugin == null)
            {
                this.OnTick?.Invoke(this, new DiscordTickEventArgs(this._isMuted, this._isOutMuted, this._discordDetected));
                return;
            }

            try
            {
                Process[] pname = Process.GetProcessesByName("Discord");
                if (pname.Length == 0)
                {
                    this._plugin.OnPluginStatusChanged(PluginStatus.Error, "Discord not running", "");
                    this._discordDetected = false;
                }
                else
                {
                    this._plugin.OnPluginStatusChanged(PluginStatus.Normal, "Discord running", "");
                    

                    if (String.IsNullOrEmpty(this._refresh_token) == false)
                    {
                        if (!DiscordIPC.HasConnection())
                        {
                            DiscordIPC.Setup(this._client_id, this._client_secret, this._access_token, this._refresh_token);
                            DiscordIPC.Start();
                            if (DiscordIPC.DoFullAuth())
                            {
                                
                                this._access_token = DiscordIPC.GetAccessToken();
                                this._refresh_token = DiscordIPC.GetRefreshToken();

                                this._plugin.SetPluginSetting(accessTokenSetting, this._access_token);
                                this._plugin.SetPluginSetting(refreshTokenSetting, this._refresh_token);
                            }
                            else
                            {
                                this._plugin.SetPluginSetting(accessTokenSetting, "");
                                this._plugin.SetPluginSetting(refreshTokenSetting, "");

                                this._refresh_token = "";
                                this._access_token = "";
                            }
                        }

                        if (DiscordIPC.HasConnection())
                        {
                            this._discordDetected = true;
                            // TODO: Get Current States
                            var voiceSettings = DiscordIPC.GetVoiceSettings();

                            this._isOutMuted = voiceSettings.Contains("\"deaf\":true");
                            this._isMuted = voiceSettings.Contains("\"mute\":true") || voiceSettings.Contains("\"deaf\":true");
                        }
                    }
                    else
                    {
                        this._plugin.OnPluginStatusChanged(PluginStatus.Error, "Discord Setup needed! Please execute the Setup Action once!", "");
                        this._discordDetected = false;
                    }
                }
            }
            catch { }

            this.OnTick?.Invoke(this, new DiscordTickEventArgs(this._isMuted, this._isOutMuted, this._discordDetected));

        }

        public Boolean Setup(String actionParameter)
        {
            try
            {
                String[] parameterStrings = actionParameter.Split(':');
                this._client_id = parameterStrings[0];
                this._client_secret = parameterStrings[1];

                this._plugin.SetPluginSetting(clientIdSetting, this._client_id);
                this._plugin.SetPluginSetting(clientSecretSetting, this._client_secret);

                DiscordIPC.Setup(this._client_id, this._client_secret, this._access_token, this._refresh_token);
                DiscordIPC.Start();
                if (DiscordIPC.DoFullAuth())
                {
                    this._access_token = DiscordIPC.GetAccessToken();
                    this._refresh_token = DiscordIPC.GetRefreshToken();

                    this._plugin.SetPluginSetting(accessTokenSetting, this._access_token);
                    this._plugin.SetPluginSetting(refreshTokenSetting, this._refresh_token);
                    return true;
                }

            }
            catch
            {
                //ignored
            }

            return false;
        }

        public void Mute() => DiscordIPC.Mute();
        public void UnMute() => DiscordIPC.UnMute();
        public void Deaf() => DiscordIPC.Deaf();
        public void UnDeaf() => DiscordIPC.UnDeaf();
    }
}
