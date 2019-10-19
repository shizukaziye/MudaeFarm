# MudaeFarm

**WARNING**: Selfbots are officially banned. It is considered an API abuse and is [no longer tolerated](https://support.discordapp.com/hc/en-us/articles/115002192352-Automated-user-accounts-self-bots-). By using this bot, *you are running a risk*.

This is a simple bot that automatically rolls and claims Mudae waifus/husbandoes.

## Setup

1. Download and extract the [latest release](https://github.com/chiyadev/MudaeFarm/releases/latest/download/MudaeFarm.zip).

2. Run `MudaeFarm.exe`.

You can bypass the "Windows protected your PC" popup by clicking "More info". Alternatively, you may build this project yourself using the .NET Framework SDK. (I cannot afford a code signing certificate.)

3. Enter your user token. [How?](https://github.com/chiyadev/MudaeFarm/blob/master/User%20tokens.md)

## Usage

### Initialization

On initial run, MudaeFarm will create a dedicated server named `MudaeFarm` for bot configuration. You can edit your wishlists, claiming, rolling and other miscellaneous settings there.

It may take a while for this server to be created.

**MudaeFarm is disabled on all servers by default.** You must copy the *ID of the channel* in which you want to enable MudaeFarm (usually the bot/spam channel of that server), and send that ID in `#bot-channels`.

**MudaeFarm requires you to have some kakera beforehand.** If you don't, get some using `$dk`.

### Configuration

You can edit JSON configuration messages and the bot will reload the changes automatically.

### Wishlists

To configure character/anime wishlists, you can simply send the name of the character/anime in the wishlist channel, separated by individual messages.

Names are *case insensitive* and support basic glob expressions like `?` and `*`.

To remove a character/anime from the wishlist, delete the message itself.

MudaeFarm wishlists are entirely separate from Mudae the wishlist and will not synchronize against each other.

- If a server has custom emotes instead of hearts for claiming, change `enable_custom_emotes` to `true` in the claiming configuration.

### Autoreply

MudaeFarm can optionally send a reply message when a character is claimed. Selection is random.

- `.` represents *not* sending a reply.
- `\n` splits one selected reply into multiple messages.
- `*Character*` is replaced by the character's first name. `*Character_full*` is replaced by the character's full name. Lowercase `character` makes the template lowercase.
- `*Anime*` is replaced by the character's anime. Lowercase `anime` makes the template lowercase.

e.g. `I love *character_full* in *Anime*` produces `I love chino kafuu in Is the Order a Rabbit?`.

### Miscellaneous

- MudaeFarm will periodically send `$tu` command to determine the claiming cooldown and rolling interval. To disable this behavior, change `state_update_command` to `""`. All subsequent matching rolls will be claimed regardless of cooldown and adaptive autorolling will be disabled.
- Autorolling is adaptive to the reset time determined by `$tu`. Change `interval_override_minutes` to override the interval in *minutes*.
- You can also change `state_update_command` to `$mu`.
