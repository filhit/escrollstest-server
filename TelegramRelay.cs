using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Escrollstest.Server {
  [ApiVersion (2, 1)]
  public class TelegramRelay : TerrariaPlugin {
    public override string Author => "filhit";

    public override string Description => "Relays messages to a Telegram chat.";

    public override string Name => "Telegram Relay Plugin";

    public override Version Version => new Version (1, 0, 0, 0);

    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource ();

    private TelegramBotClient _telegramBotClient;

    private Config _config;

    public TelegramRelay (Main game) : base (game) { }

    public override void Initialize () {
      _config = Config.Read (Config.ConfigPath);
      if (!System.IO.File.Exists (Config.ConfigPath)) {
        _config.Write (Config.ConfigPath);
      }

      if (string.IsNullOrEmpty (_config.TelegramBotToken)) {
        Console.WriteLine ("No token provided, exiting");
        return;
      }

      _telegramBotClient = new TelegramBotClient (_config.TelegramBotToken);
      _telegramBotClient.StartReceiving (
        new DefaultUpdateHandler (HandleUpdateAsync, HandleErrorAsync),
        _cancellationTokenSource.Token
      );

      PlayerHooks.PlayerChat += OnPlayerChat;
      ServerApi.Hooks.ServerJoin.Register (this, OnJoin);
      ServerApi.Hooks.ServerLeave.Register (this, OnLeave);
      ServerApi.Hooks.ServerBroadcast.Register (this, OnBroadcast);
    }

    /// <summary>
    /// Called when a player leaves the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnLeave (LeaveEventArgs args) {
      if (args != null && TShock.Players[args.Who] != null) {
        SendTelegramMessage ($"{TShock.Players[args.Who].Name} left the game.");
      }
    }

    /// <summary>
    /// Called when a player joins the server
    /// </summary>
    /// <param name="args">event arguments passed by hook</param>
    private void OnJoin (JoinEventArgs args) {
      if (args != null && TShock.Players[args.Who] != null) {
        SendTelegramMessage ($"{TShock.Players[args.Who].Name} joined the game.");
      }
    }

    private void OnBroadcast (ServerBroadcastEventArgs args) {
      if (Regex.IsMatch (args.Message.ToString (), @"has defeated the \d+th")) {
        return;
      }

      if (Regex.IsMatch (args.Message.ToString (), @"has joined the \S+ party")) {
        return;
      }

      if (Regex.IsMatch (args.Message.ToString (), @"is no longer on a party")) {
        return;
      }

      if (Regex.IsMatch (args.Message.ToString (), @"Looks like .+ (is|are) throwing a party")) {
        return;
      }

      if (Regex.IsMatch (args.Message.ToString (), @"Party time's over!")) {
        return;
      }

      if (Regex.IsMatch (args.Message.ToString (), @"\S+ the Traveling Merchant has (arrived|departed)!")) {
        return;
      }

      SendTelegramMessage (args.Message.ToString ());
    }

    private void SendTelegramMessage (string message, long? chatId = null) {
      chatId = chatId ?? _config.TelegramChatId;
      if (chatId.GetValueOrDefault () == 0) {
        return;
      }

      Task.Factory.StartNew (async () => {
        try {
          await _telegramBotClient.SendTextMessageAsync (
            chatId: chatId,
            text: message
          );
        } catch (Exception e) {
          Console.WriteLine (e);
        }
      });
    }

    void OnPlayerChat (PlayerChatEventArgs args) {
      if (args.RawText.StartsWith ("/")) {
        return;
      }
      SendTelegramMessage ($"<{args.Player.Name}>: {args.RawText}");
    }

    public async Task HandleUpdateAsync (ITelegramBotClient botClient, Update update, CancellationToken cancellationToken) {
      var handler = update.Type
      switch {
        UpdateType.Message => BotOnMessageReceived (update.Message),
        // UpdateType.EditedMessage => BotOnMessageReceived(update.Message),
        // UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery),
        // UpdateType.InlineQuery => BotOnInlineQueryReceived(update.InlineQuery),
        // UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(update.ChosenInlineResult),
        // UpdateType.Unknown:
        // UpdateType.ChannelPost:
        // UpdateType.EditedChannelPost:
        // UpdateType.ShippingQuery:
        // UpdateType.PreCheckoutQuery:
        // UpdateType.Poll:
        _ => UnknownUpdateHandlerAsync (update)
      };

      try {
        await handler;
      } catch (Exception exception) {
        await HandleErrorAsync (botClient, exception, cancellationToken);
      }
    }

    private async Task BotOnMessageReceived (Message message) {
      switch (message.Type) {
        case MessageType.Sticker:
          TShock.Utils.Broadcast ($"<{message.From.FirstName}> sent a sticker.", 255, 255, 255);
          break;
        case MessageType.Document:
          TShock.Utils.Broadcast ($"<{message.From.FirstName}> sent a document.", 255, 255, 255);
          break;
        case MessageType.Text when message.Text.StartsWith ("/ping"):
          SendTelegramMessage ($"pong (chat id: {message.Chat.Id})", message.Chat.Id);
          break;
        case MessageType.Text when message.Text.StartsWith ("/players"):
          var activePlayers = TShock.Players.Where (x => x != null && x.Active);
          if (activePlayers.Any ()) {
            SendTelegramMessage ($"Active players: {string.Join(",", activePlayers.Select(x => x.Name))}.", message.Chat.Id);
          } else {
            SendTelegramMessage ("No active players detected.", message.Chat.Id);
          }
          break;
        case MessageType.Text when message.Text.StartsWith ("/"):
          break;
        case MessageType.Text:
          TShock.Utils.Broadcast ($"<{message.From.FirstName}>: {message.Text}", 255, 255, 255);
          break;
        default:
          Console.WriteLine ($"Receive message type: {message.Type}");
          break;
      }
    }

    private static async Task UnknownUpdateHandlerAsync (Update update) {
      Console.WriteLine ($"Unknown update type: {update.Type}");
    }

    public static async Task HandleErrorAsync (ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) {
      var ErrorMessage = exception
      switch {
        ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString ()
      };

      Console.WriteLine (ErrorMessage);
    }

    protected override void Dispose (bool disposing) {
      if (disposing) {
        _cancellationTokenSource.Cancel ();
        DeregisterHooks ();
      }

      base.Dispose (disposing);
    }

    private void DeregisterHooks () {
      PlayerHooks.PlayerChat -= OnPlayerChat;
      ServerApi.Hooks.ServerJoin.Deregister (this, OnJoin);
      ServerApi.Hooks.ServerLeave.Deregister (this, OnLeave);
      ServerApi.Hooks.ServerBroadcast.Deregister (this, OnBroadcast);
    }
  }
}
