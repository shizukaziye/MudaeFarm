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

- General
    - `enabled`: Whether MudaeFarm is enabled or not. Setting `false` will completely disable all MduaeFarm features.
    - `fallback_status`: If you are running MudaeFarm continuously, when your primary client is logged out, the Discord user status to be used. Possible values: `Online`, `Invisible`, `Idle`, `DoNotDisturb`.
    - `state_update_command`: See [Miscellaneous](#miscellaneous).
- Claiming
    - `enabled`: Whether autoclaiming is enabled or not.
    - `delay_seconds`: When receiving a roll that can be claimed, number of seconds to wait before claiming it.
    - `kakera_delay_seconds`: Same as `delay_seconds` but for kakeras.
    - `kakera_targets`: Which type of kakera should be claimed. Purple kakera will always be claimed regardless of this configuration.
    - `enable_custom_emotes`: Enables compatibility with servers that use custom emotes instead of the default heart emojis. This is not suggested unless necessary as it will bypass some internal emote safety checks.
- Rolling
    - `enabled`: Whether autorolling is enabled or not.
    - `command`: Command to use when rolling.
    - `roll_with_no_claim`: Whether MudaeFarm should continue rolling even if it cannot claim any rolls or not.
    - `daily_kakera_enabled`: Whether autorolling of daily kakeras is enabled or not.
    - `daily_kakera_command`: Command to use when rolling daily kakeras.
    - `daily_kakera_then_state_update`: Whether state update should be performed after rolling daily kakeras.
    - `typing_delay_seconds`: Number of seconds to "type" the rolling command before sending it.
    - `interval_override_minutes`: See [Miscellaneous](#miscellaneous).
- Miscellaneous
    - `auto_update`: Set to `false` to disable MudaeFarm from automatically updating itself.

### Wishlists

To configure character/anime wishlists, you can simply send the name of the character/anime in the wishlist channel, separated by individual messages.

Names are *case insensitive* and support basic glob expressions like `?` and `*`.

To remove a character/anime from the wishlist, delete the message itself.

MudaeFarm wishlists are entirely separate from Mudae the wishlist and will not synchronize against each other. However, if your *Mudae* wishlist is public, you can use `#wishlist-users` where you can enter your own ID.

- If a server has custom emotes instead of hearts for claiming, change `enable_custom_emotes` to `true` in the claiming configuration.
- You can exclude specific characters for one anime. e.g. `is the order a rabbit? (excluding: kafuu chino)` will allow claiming every character from "Is the Order a Rabbit?" *except* "Kafuu Chino". Glob expressions will not work in this case.

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
- You can also change `state_update_command` to `$mu` or any other command that yields timer output. However, some parts of the bot may not work without required information from this command.
