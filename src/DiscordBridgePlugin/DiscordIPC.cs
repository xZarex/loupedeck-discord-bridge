namespace Loupedeck.DiscordBridgePlugin
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Pipes;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    class DiscordIPC
    {
        private static NamedPipeClientStream _client;
        private static BinaryReader _reader;
        private static BinaryWriter _writer;
        private static string _clientId = "";
        private static string _clientSecret = "";
        private static string _clientRedirect = "http://localhost/";
        private static readonly HttpClient webClient = new HttpClient();

        private static string access_token;
        private static string refresh_token;

        public static bool HasConnection() => _client != null && _client.IsConnected;

        public static void Setup(string clientId, string clientSecret, string accessToken, string refreshToken)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            access_token = accessToken;
            refresh_token = refreshToken;
        }

        private static void SetAccessToken(string token) => access_token = token;

        private static void SetRefreshToken(string token) => refresh_token = token;

        public static string GetAccessToken() => access_token;

        public static string GetRefreshToken() => refresh_token;

        private static void writeMessage(int opcode, string str)
        {
            UInt32 test = UInt32.Parse("" + opcode);
            string connect0 = str;
            byte[] connect0bytes = Encoding.ASCII.GetBytes(connect0);
            UInt32 connect0len = UInt32.Parse("" + connect0bytes.Length);

            _writer.Write(BitConverter.GetBytes(test));
            _writer.Write(BitConverter.GetBytes(connect0len));
            _writer.Write(connect0bytes);
            _writer.Flush();
        }

        private static string readMessage(int len = 8)
        {
            byte[] readBytes = _reader.ReadBytes(len);
            UInt32 code = BitConverter.ToUInt32(readBytes, 0);
            UInt32 len2 = BitConverter.ToUInt32(readBytes, 4);
            byte[] readBytes2 = _reader.ReadBytes(int.Parse("" + len2));
            return Encoding.UTF8.GetString(readBytes2);
        }

        public static string Start()
        {
            if (_reader == null)
            {
                _client = new NamedPipeClientStream("discord-ipc-0");
                _client.Connect();
                _reader = new BinaryReader(_client);
                _writer = new BinaryWriter(_client);
            }

            writeMessage(0, "{\"v\":1, \"client_id\":\"" + _clientId + "\"}");

            return readMessage();
        }

        public static string Authorize()
        {
            writeMessage(1, "{\"nonce\": \"" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\", \"args\": {\"client_id\": \"" + _clientId + "\", \"scopes\": [\"rpc\"]}, \"cmd\": \"AUTHORIZE\"}");
            return readMessage();
        }

        private static async Task<string> InternalOAuthRefresh()
        {
            var values = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "grant_type", "refresh_token" },
                { "refresh_token", refresh_token }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await webClient.PostAsync("https://discord.com/api/v10/oauth2/token", content);

            var responseString = await response.Content.ReadAsStringAsync();

            return responseString;
        }

        private static string OAuthRefresh()
        {
            var task = InternalOAuthRefresh();
            task.Wait();
            string result = task.Result;

            return result;
        }

        private static async Task<string> InternalOAuthExchange(string code)
        {
            var values = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", _clientRedirect },
            };

            var content = new FormUrlEncodedContent(values);

            var response = await webClient.PostAsync("https://discord.com/api/v10/oauth2/token", content);

            var responseString = await response.Content.ReadAsStringAsync();

            return responseString;
        }

        public static string OAuthExchange(string code)
        {
            var task = InternalOAuthExchange(code);
            task.Wait();
            string result = task.Result;

            return result;
        }

        public static string Authenticate()
        {
            writeMessage(1, "{\"nonce\": \"" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\", \"cmd\": \"AUTHENTICATE\", \"args\": { \"access_token\": \"" + access_token + "\" }}");
            return readMessage();
        }


        public static int DoFullAuth()
        {
            try
            {
                var webAuth = "";
                if (string.IsNullOrEmpty(refresh_token))
                {
                    var code = "";
                    try
                    {
                        var authorize = Authorize();
                        code =
                            authorize.Split(new string[] {"{\"code\":\""}, StringSplitOptions.None)[1].Split('"')[0];
                    }
                    catch
                    {
                        return 0;
                    }

                    if (string.IsNullOrEmpty(code))
                        return 0;
                    
                    webAuth = OAuthExchange(code);
                }
                else
                {
                    webAuth = OAuthRefresh();
                }

                
                SetAccessToken(webAuth.Split(new string[] { "\"access_token\": \"" }, StringSplitOptions.None)[1].Split('"')[0]);

                SetRefreshToken(webAuth.Split(new string[] { "\"refresh_token\": \"" }, StringSplitOptions.None)[1].Split('"')[0]);


                //expires_in = webAuth.Split(new string[] { "\"expires_in\": " }, StringSplitOptions.None)[1].Split(',')[0];
                //Console.WriteLine("2: expiresIn = " + expires_in);

                var authenticate = Authenticate();

                return 2;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }


            return 1;
        }

        public static string Mute()
        {
            writeMessage(1, "{\"nonce\": \""+ DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\", \"args\": { \"mute\": true }, \"cmd\": \"SET_VOICE_SETTINGS\"}");
            return readMessage();
        }

        public static string UnMute()
        {
            writeMessage(1, "{\"nonce\": \"" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\", \"args\": { \"mute\": false }, \"cmd\": \"SET_VOICE_SETTINGS\"}");
            return readMessage();
        }

        public static string Deaf()
        {
            writeMessage(1, "{\"nonce\": \""+ DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\", \"args\": { \"deaf\": true }, \"cmd\": \"SET_VOICE_SETTINGS\"}");
            return readMessage();
        }

        public static string UnDeaf()
        {
            writeMessage(1, "{\"nonce\": \"" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\", \"args\": { \"deaf\": false }, \"cmd\": \"SET_VOICE_SETTINGS\"}");
            return readMessage();
        }

        public static string GetVoiceSettings()
        {
            writeMessage(1, "{\"nonce\": \"" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\", \"args\": { }, \"cmd\": \"GET_VOICE_SETTINGS\"}");
            return readMessage();
        }

        public static void Stop()
        {
            try
            {

                _client?.Close();
                _writer?.Close();
                _reader?.Close();

                _client = null;
                _writer = null;
                _reader = null;
            }
            catch
            {
                //ignored
            }
            
        }
    }
}
