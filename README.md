# escrollstest-server
- Use user filhit
- Clone the repo to home.
- Extract TShock to /home/filhit/terraria
- Download world backup to terraria/Worlds
- copy TelegramBot.dll to /home/filhit/terraria/ServerPlugins/
- copy NewtonsoftJson.dll (version 11) to /home/filhit/terraria/
- build TelegramRelay.dll using build-plugin.sh
- copy it to /home/filhit/terraria/ServerPlugins/
- Run server and auth
- Create REST token and put it to secrets.sh
- Fill telegram-relay.json in /home/filhit/terraria/ with secrets
- run install.sh

