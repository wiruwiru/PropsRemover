# PropsRemover CS2

This is a fast-tracked project developed to allow administrators to remove specific props from maps in Counter-Strike 2. Due to the limited time spent on testing, the plugin may contain bugs or may not work as expected.

PropsRemover CS2 supports the removal of props of the following types:
- `prop_physics`
- `prop_dynamic`
- `prop_static`
- `prop_physics_multiplayer`

## Installation
1. Install [CounterStrike Sharp](https://github.com/roflmuffin/CounterStrikeSharp) and [Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master).
2. Download [PropsRemover.zip](https://github.com/wiruwiru/PropsRemover/releases) from the releases section.
3. Unzip the archive and upload it to the game server.
4. Start the server and wait for the configuration file to be generated.
5. Edit the configuration file with the parameters of your choice.

---

## Config
The configuration file will be automatically generated when the plugin is first loaded. Below are the parameters you can customize:

| Parameter        | Description                                                                 |
|------------------|-----------------------------------------------------------------------------|
| `RemoveBlood`    | Removes blood stains from surfaces when a player takes damage.              |

---

## Commands

| Command   | Description                                                                                         |
|-----------|-----------------------------------------------------------------------------------------------------|
| `rmp`     | Toggles the prop elimination mode. When active, shooting a prop will remove it and store its data in `props_data.json` for automatic removal at the start of each round. |
| `rmprops` | Forcefully removes all registered props for the current map without waiting for the round start event.|

---

## How It Works
1. **Prop Elimination Mode** (`rmp`): When enabled, props can be removed by shooting them. Removed props are recorded in a file (`props_data.json`). These props will then be automatically deleted at the start of every round to keep the map clean.
2. **Manual Prop Removal** (`rmprops`): This command allows administrators to remove all registered props manually without waiting for the round to start.
3. **Blood Stain Removal** (`RemoveBlood`): Instantly clears blood stains from surfaces, ensuring a cleaner playing environment.

---

## Known Issues
- Due to rapid development and minimal testing, some features may not work as intended. Please report any issues you encounter.

---

## Contributions
I sincerely appreciate all contributions that help improve this project.

You may submit pull requests directly to the `main` branch or create a new feature-specific branch.
Please ensure that your commits include clear and detailed explanations of the changes made to facilitate understanding and review.

---