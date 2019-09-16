# MudaeFarm

**WARNING**: Selfbots are officially banned. It is considered an API abuse and is [no longer tolerated](https://support.discordapp.com/hc/en-us/articles/115002192352-Automated-user-accounts-self-bots-). By using this bot, *you are running a risk*.

This is a simple bot that automatically rolls and claims Mudae waifus/husbandoes.

## Setup

1. Install [.NET Core SDK](https://dotnet.microsoft.com/download).

2. Download this repository [as a zip](https://github.com/chiyadev/MudaeFarm/archive/master.zip).

3. Open terminal or CMD in the folder and run these commands: `dotnet build`, `dotnet run`

4. Enter your user token ([how?](User tokens.md).

## Commands

Autorolling:

- `/rollinterval {minutes}` — Sets the roll interval in minutes. Setting this to `0` disables autorolling. Autorolling is disabled by default.
- `/roll` — Sets the channel in which you use this command as a bot channel, where MudaeFarm will automatically issue roll commands. You can do this in as many channels as you like.
- `/roll disable` — Stops autorolling in the channel where you send this command.
- `/marry waifu` — Sets the marry command to waifus (`$w`). This is the default.
- `/marry husbando` — Sets the marry command to husbandoes (`$h`).

Autoclaiming:

- `/wish {character}` — Adds a character to your wishlist.
- `/unwish {character}` — Removes a character from your wishlist.
- `/wishani {anime}` — Adds an anime to your wishlist. This is akin to wishing every character from that anime.
- `/unwishani {anime}` — Removes an anime from your wishlist.
- `/wishlist` — Shows the list of your wished characters and anime.
- `/wishclear` — Clears the wishlist entirely.
- `/claimdelay {seconds}` — Sets the number of seconds to wait before automatically claiming a character. This can be used to give a *human-like* feeling at the expense of time spent waiting. The default is `0`.
