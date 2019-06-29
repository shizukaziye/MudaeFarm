# MudaeFarm

**WARNING**: Selfbots are officially banned. It is considered an API abuse and is [no longer tolerated](https://support.discordapp.com/hc/en-us/articles/115002192352-Automated-user-accounts-self-bots-). By using this bot, *you are running a risk*.

This is a simple bot that automatically rolls and claims Mudae waifus/husbandoes.

## Setup

1. Install [.NET Core SDK](https://dotnet.microsoft.com/download).

2. Download this repository [as a zip](https://github.com/chiyadev/MudaeFarm/archive/master.zip).

3. Open terminal or CMD in the folder and run these commands: `dotnet build`, `dotnet run`

4. Read the printed text carefully and proceed by pressing `y`.

### Finding Your User Token

Press `Ctrl+Shift+I` or `Command+Option+I` on Discord which opens Chrome inspector, then click on the tab named `Network`. Click on `XHR`.

![xhr](images/xhr.png)

Open any channel and type something on Discord, which should create an entry named `science`.

![science](images/science.png)

Under `Request Headers` find `authorization`.

![headers](images/headers.png)

DO NOT EVER SHARE THIS TOKEN WITH ANYONE. TREAT THIS TOKEN LIKE YOU TREAT YOUR PASSWORD.

## Commands

Autorolling:

- `/rollinterval {minutes}` — Sets the roll interval in minutes. Setting this to `-1` disables autorolling. Autorolling is disabled by default.
- `/setchannel` — Sets the channel in which you use this command as a bot channel, where MudaeFarm will automatically issue roll commands. You can do this in as many channels as you like.
- `/unsetchannel` — Stops autorolling in the channel where you send this command.
- `/marry waifu` — Sets the marry command to waifus (`$w`). This is the default.
- `/marry husbando` — Sets the marry command to husbandoes (`$h`).

Autoclaiming:

- `/wish {character}` — Adds a character to your wishlist.
- `/unwish {character}` — Removes a character from your wishlist.
- `/wishani {anime}` — Adds an anime to your wishlist. This is akin to wishing every character from that anime.
- `/unwishani {anime}` — Removes an anime from your wishlist.
- `/wishlist` — Shows the list of your wished characters and anime.
- `/claimdelay {seconds}` — Sets the number of seconds to wait before automatically claiming a character. This can be used to give a *human-like* feeling at the expense of time spent waiting. The default is `0`.
