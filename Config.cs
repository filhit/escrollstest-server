using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace Escrollstest.Server
{
    public class Config
    {
        public static string ConfigPath = Path.Combine(TShock.SavePath, "telegram-relay.json");

        public string TelegramBotToken { get; set; }

        public long TelegramChatId { get; set; }

        /// <summary>
        /// Writes the current internal server config to the external .json file
        /// </summary>
        /// <param Name="path"></param>
        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        /// <summary>
        /// Reads the .json file and stores its contents into the plugin
        /// </summary>
        public static Config Read(string path)
        {
            if (!File.Exists(path))
                return new Config();
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
        }
    }
}
