# MudaeFarm

**WARNING**: Selfbots are officially banned. It is considered an API abuse and is [no longer tolerated](https://support.discordapp.com/hc/en-us/articles/115002192352-Automated-user-accounts-self-bots-). By using this bot, *you are running a risk*.

This is a simple bot that automatically rolls and claims Mudae waifus/husbandoes.

## Setup

1. Download and extract the [latest release](https://github.com/chiyadev/MudaeFarm/releases/latest/download/MudaeFarm.zip).

2. Run `MudaeFarm.exe`.

You can bypass the "Windows protected your PC" popup by clicking "More info". Alternatively, you may build this project yourself using the .NET Framework SDK. (I cannot afford a code signing certificate.)

3. Enter your user token. [How?](https://github.com/chiyadev/MudaeFarm/blob/master/User%20tokens.md)

## Usage

On initial run, MudaeFarm will create a dedicated server named `MudaeFarm` for bot configuration. You can edit your wishlists, claiming, rolling and other miscellaneous settings there. It may take a while for this server to be created.

To configure character/anime wishlists, you can simply send the name of the character/anime in the wishlist channel, separated by individual messages. Names are *case insensitive* and support basic glob expressions like `?` and `*`.

MudaeFarm wishlists are entirely separate from Mudae the wishlist and will not synchronize against each other.

**Autoclaiming is disabled by default.** You must copy the ID of the server you want to enable autoclaiming in, and send that ID in `#claim-servers`.

**Autorolling is disabled by default.** You must copy the ID of the channel you want to enable autorolling in, and send that ID in `#bot-channels`.

For JSON-based configuration messages, you can simply edit the contents and the bot will reload the changes automatically.
