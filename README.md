<div align="center">
  <img src="./client/Assets/DF/Images/UI_LogoMain.png" alt="Dark Frontier Logo" width="512">
  <br/>
  <p>Fully onchain, ZK space conquest MMORTS Dark Forest-inspired game, <br/>
      powered by Argus Labs' ｢ World Engine ｣</p>
  <p>
    <a href="https://t.me/worldengine_dev" target="_blank">
      <img alt="Telegram Chat" src="https://img.shields.io/endpoint?color=neon&logo=telegram&label=chat&url=https%3A%2F%2Ftg.sumanjay.workers.dev%2Fworldengine_dev">
    </a>
    <a href="https://x.com/DarkFrontierGG" target="_blank">
      <img alt="Twitter Follow" src="https://img.shields.io/twitter/follow/DarkFrontierGG">
    </a>
  </p>
</div>

<br/>

![Gameplay Screenshot](./client/screenshot.png)

## Table of Content

- [Overview](#overview)
- [Getting Started](#getting-started)
- [Running Dark Frontier's World Engine backend](#running-dark-frontiers-world-engine-backend)
- [Running Dark Frontier's client](#running-dark-frontiers-client)
- [Gameplay Tutorial](#gameplay-tutorial)

<br/>

## Overview

This repository contains both the Dark Frontier's Unity game client (`/client`) and World Engine's Cardinal game 
shard  (`/backend`). 

Dark Frontier provides a real-world example of what you can achieve with World Engine and have processed live 
production workload with 700K+ transactions.

To learn more about World Engine, visit [https://world.dev](https://world.dev).

## Getting Started

We recommend reading the [World Engine quickstart guide](https://world.dev/quickstart) before proceeding.

The quickstart guide will guide you in installing the prerequisites for running a World Engine project that is 
needed to run a Dark Frontier instance on your machine.

If you have any questions, please reach out to our friendly community on [Telegram](https://t.me/worldengine_dev)!

<br/>

## Running Dark Frontier's World Engine backend

### Prerequisites
- Go
- Docker
- World CLI

To install the World CLI, run the following command:
```bash
curl https://install.world.dev/cli! | bash
```

### Setup

1. Download ZK circuit artifacts

```bash
make getCircuitArtifacts
```

<br/>

2. Run vendor to statically link the artifacts into Cardinal

```bash
cd cardinal && go mod vendor && cd ../
```

<br/>

3. Start Nakama and Cardinal. Ensure you're in `/backend` directory, then:

```bash
world cardinal start
```

<br/>

4. To stop both the Nakama and Cardinal

```bash
world cardinal stop
```

<br/>

### Configuring Allowlist/Game Keys

World Engine's Nakama Allowlist feature allows you to restrict access to the game using game keys.

To enable this feature, set the following environment variable:

```
ENABLE_ALLOWLIST=true
```

Enabling the allowlist feature will enable two new RPC endpoints to manage and use game keys.

You can interact with these RPC endpoints easily through the Nakama API Explorer, accessible via browser at 
`localhost:7351`.
For local development, use `admin:password` as your login credentials.

#### RPC generate-beta-keys

An admin-only endpoint that generates N number of game keys.

##### Request Payload

```json
{
  "amount": 5
}
```

##### Return Payload

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

#### RPC claim-key

A user-facing endpoint which allows a user to redeem a game key.

⚠️ NOTE: the `create-persona` endpoint will be 🛑 BLOCKED until a user calls `claim-key` with a valid key. ⚠️

##### Request Payload

```json
{
  "key": "ABCD-EFGH-1234-5678"
}
```

##### Return Payload

```json
{
  "success": true
}
``````

<br/>

## Running Dark Frontier's client

### Prerequisites

- Unity Hub
- Unity 2022 LTS

### Setup
1. [Install Unity Hub.](https://unity.com/download) and load `/client` project in unity.
2. Open the Bootstrap scene: Assets/Scenes/Bootstrap.unity
3. Locate and select the GameObject named Bootstrap within the Bootstrap scene.
4. Configure the communication setting
    - The default configuration connects to your local World Engine backend instance.
    - For testing, you can use Argus Labs' cloud prover to generate ZK proofs required for game moves: [https://lambda.argus-dev.com/generate-proof](https://lambda.argus-dev.com/generate-proof)

<div align="center">
   <img src="./client/bootstrap.png" alt="Bootstrap Screenshot" width="500"/>
</div>

5. Check `Will Start In Clean Test Mode` to force creation of a new player.
6. Check `Will Require Beta Key` only if you enabled the allowlist feature on the World Engine backend.
7. Check <b>Will Respect Countdown</b> only if you want a time-limited game.
8. Press Play to start the game.

### Development Guide

#### Start with Bootstrap
[Bootstrap.cs](./client/Packages/gg.argus.df-client/Runtime/Core/Bootstrap.cs) is the main entry point to start looking at code. It is repsonsible for instantiating all dependencies as well as setting up the major game states and their transitions so that everything is ready to go when the initial game state is started.

Each major game state receives dependencies on a need-to-know basis. An EventManager allows for communication between game states, UI, and a CommunicationsManager.

The CommunicationsManager is responsible for communicating with the backend. Non-game-specific communications are handled by the [gg.argus.world-engine-client-communications-unity](https://github.com/Argus-Labs/world-engine-client-communications-unity) UPM package.

Gameplay logic is handled in [Gameplay.cs](./client/Packages/gg.argus.df-client/Runtime/Core/States/Gameplay.cs).

#### UPM Dependencies
- [gg.argus.world-engine-client-communications-unity](https://github.com/Argus-Labs/)
- [com.heroiclabs.nakama-unity](https://github.com/heroiclabs/nakama-unity.git?path=/Packages/Nakama#v3.6.0)
- [com.olegknyazev.softmask](https://github.com/olegknyazev/SoftMask.git?path=/Packages/com.olegknyazev.softmask#1.7.0)
- [com.smonch.cyclopsframework](https://github.com/darkmavis/com.smonch.cyclopsframework.git)

<br/>

## Gameplay Tutorial

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

