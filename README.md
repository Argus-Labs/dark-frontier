<div align="center"><img src="./client/Assets/DF/Images/UI_LogoMain.png" alt="Gameplay Screenshot" width="512"><br/><br/><br/></div>

# Welcome
<p>
This repo, dark-frontier-full-stack-example, consists of two projects: a backend, and a frontend client created with Unity 2022 LTS. We hope these projects will provide a real-world example of what you can achieve with World Engine. It's quite likely that the current version of World Engine will easily surpass what was available when this example was created both in terms of features and ease of use.
</p>

# Getting Started

<p>
We recommend setting up a local backend first and then the client project. Instructions are provided below for setting up both projects. If you have any questions, please reach out to our friendly community on <a href="https://discord.com/invite/XyfDPHDmWU.">Discord</a>!
</p>

# Backend

## Getting Started
- Be sure to read the [World Engine Getting Started](https://world.dev/introduction) page first.

- Follow the [Quickstart](https://world.dev/quickstart) guide to install the prerequisites for this project. There's no need to create your own World Engine project (part of the guide), but it may be a valuable experience and will only take a few minutes if you have the time.

- Optional: Install or use [Visual Studio Code](https://code.visualstudio.com) to navigate the project on any platform. On Windows this will provide an integrated WSL CLI while working with the project.  WSL must be pre-installed on Windows.  More information is available in the [Quickstart](https://world.dev/quickstart) guide.  Please be sure to install your choice of Go Lang extensions for VSCode.  Please note: When modifying Cardinal code via VSCode, for full Go Lang capabilities, please open the <b><i>cardinal</i></b> folder as your root folder.  When running World Engine CLI commands, run them from the <b><i>backend</i></b> folder.

- You can also find backend (Cardinal) specific documentation [here](https://world.dev/cardinal/introduction).

## Running the Server

### Setup

(1) Get the circuit artifacts:

```bash
make getCircuitArtifacts
```

(2) Run vendor so the artifacts are passed into the cardinal shard:

```bash
cd cardinal && go mod vendor && cd ../
```

(3) To start nakama and the game shard, ensure you're in the project root directory, then:

```bash
world cardinal start
```

(4) To restart JUST the game shard:

```bash
world cardinal restart
```

(5) To stop both the Nakama and game shards:

```bash
world cardinal stop
```

Alternatively, killing the docker processes will also stop Nakama and the game shard.

Note, if any server endpoints have been added or removed Nakama must be relaunched (via `world cardinal stop` and `world cardinal start`).

## Verify the Nakama Server is Running

Visit `localhost:7351` in a web browser to access Nakama. For local development, use `admin:password` as your login
credentials.

The Account tab on the left will give you access to a valid account ID.

The API Explorer tab on the left will allow you to make requests to the game shard.

## Allowlisting / Beta Keys

Allowlisting can be enabled to provide a mechanism similar to beta keyed entry to the game. To enable this feature, set the following environment variable:

```
ENABLE_ALLOWLIST=true
```

This will enable two new RPC endpoints to support beta keyed access to the game:

### generate-beta-keys

#### Description
An admin only endpoint that generates N amount of beta keys.

#### Payload

```json
{
  "amount": 5
}
```

#### Return Payload

```json
{
  "keys": [
    "ABCD-EFGH-1234-5678",
    "ABCD-EFGH-1234-5678",
    "ABCD-EFGH-1234-5678",
    "ABCD-EFGH-1234-5678",
    "ABCD-EFGH-1234-5678"
  ]
}
```

### claim-key

#### Description

A user endpoint which validates the user's `device_id`.

⚠️ NOTE: the `create-persona` endpoint will be 🛑 BLOCKED until a user calls `claim-key` with a valid key. ⚠️

#### Payload

```json
{
  "key": "ABCD-EFGH-1234-5678"
}
```

#### Return Payload

```json
{
  "success": true
}
``````
<br/><br/>
# Client

![Gameplay Screenshot](./client/screenshot.png)
Dark Frontier is a Unity based MMORTS created by Argus Labs to demonstrate the capabilities of World Engine.

## Getting Started
- Be sure to read the [World Engine Getting Started](https://world.dev/introduction) page first.
- You can find client specific documentation [here](https://world.dev/client/introduction).

## Setup Guide
- Clone the latest version of this repo.
- When asked if you want to initialize [GitLFS](https://docs.github.com/en/repositories/working-with-files/managing-large-files/installing-git-large-file-storage), choose YES.
- [Install Unity Hub.](https://unity.com/download)
- Add the project to Unity Hub.
- At the top of the Unity Hub window, click the drop-down next to "Open".
- Select "Add project from disk".
- Navigate to the location the repo was cloned to in Step 1 and select it.  
- Attempt to open the project.
  - Note: Unity may prompt you with a request to update.
  - Update Unity if needed.
- Open the project in Unity 2022 LTS. At the time of this writing, 2023.2.x contains rendering issues.
- Open the Bootstrap scene: Assets/Scenes/Bootstrap.unity
- Locate and select the GameObject named Bootstrap within the Bootstrap scene.
- Notice that the Bootstrap GameObject contains a Bootstrap.cs script as seen below.
- Adjust the communications settings to suit your needs.
    - <img src="./client/bootstrap.png" alt="Gameplay Screenshot" width="400"/>
     - Need help with custom Nakama settings? Start here: [https://world.dev/client/introduction](https://world.dev/client/introduction)
     - The client project contains valid defaults for running a backend locally.
     - Please feel free to use our cloud prover for both local and remote DF dev setups:
    - [https://lambda.argus-dev.com/generate-proof](https://lambda.argus-dev.com/generate-proof)
- Check <b>Will Start In Clean Test Mode</b> to force creation of a new player.
- Check <b>Will Require Beta Key</b> only if you've previously setup support for beta keys within the backend.
- Check <b>Will Respect Countdown</b> only if you want a time-limited game.
- Ignore the overrides. These simply override game settings and have no value outside of testing.
- Press Play to start the game.

### Start with Bootstrap
[Bootstrap.cs](./client/Packages/gg.argus.df-client/Runtime/Core/Bootstrap.cs) is the main entry point to start looking at code. It is repsonsible for instantiating all dependencies as well as setting up the major game states and their transitions so that everything is ready to go when the initial game state is started.

Each major game state receives dependencies on a need-to-know basis. An EventManager allows for communication between game states, UI, and a CommunicationsManager.

The CommunicationsManager is responsible for communicating with the backend. Non-game-specific communications are handled by the [gg.argus.world-engine-client-communications-unity](https://github.com/Argus-Labs/world-engine-client-communications-unity) UPM package.

Gameplay logic is handled in [Gameplay.cs](./client/Packages/gg.argus.df-client/Runtime/Core/States/Gameplay.cs).

## Gameplay
Here are 3 tutorials from the game. They may not be visible from GitHub, but they should be visible from Visual Studio Code or other supported viewers. Alternatively, click any of the links below and then select GitHub's View Raw link to download and watch.

### [Tutorial 1](./client/Assets/StreamingAssets/tutorial-1.mp4)
<video width="640" height="360" controls>
  <source src="./client/Assets/StreamingAssets/tutorial-1.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

### [Tutorial 2](./client/Assets/StreamingAssets/tutorial-2.mp4)
<video width="640" height="360" controls>
  <source src="./client/Assets/StreamingAssets/tutorial-2.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

### [Tutorial 3](./client/Assets/StreamingAssets/tutorial-3.mp4)
<video width="640" height="360" controls>
  <source src="./client/Assets/StreamingAssets/tutorial-3.mp4" type="video/mp4">
  Your browser does not support the video tag.
</video>

### Input Bindings
| Controls | Functionality                                          
| -------- | ---------------------------------------------- |
|     WASD | Pan space view camera.
|    Wheel | Zoom camera in and out.
|      LMB | Select a planet.
|LMB + Drag| Create an energy transfer line between planets.
|        F | Focus the camera on the selected planet.
|        H | Focus the camera on your home planet.
|      TAB | Cycle through the various debug modes.

## UPM Dependencies
- [gg.argus.world-engine-client-communications-unity](https://github.com/Argus-Labs/)
- [com.heroiclabs.nakama-unity](https://github.com/heroiclabs/nakama-unity.git?path=/Packages/Nakama#v3.6.0)
- [com.olegknyazev.softmask](https://github.com/olegknyazev/SoftMask.git?path=/Packages/com.olegknyazev.softmask#1.7.0)
- [com.smonch.cyclopsframework](https://github.com/darkmavis/com.smonch.cyclopsframework.git)