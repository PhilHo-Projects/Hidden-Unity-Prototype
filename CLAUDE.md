# HiddenGame Agent Quick Reference

## What This Repo Is
- `HiddenGame` contains the original Unity project plus a newer real web runtime.
- `Client/` is the Unity project. `Client/BuildWebGL/` is the built Unity WebGL artifact served in production at `/HiddenGame/`.
- `WebClient/` is the React/TypeScript/Vite/Tailwind web version of the game.
- `Server/` is the Node/TypeScript WebSocket backend used by the Unity/Web clients for rooms, matchmaking, ready flow, and relaying game packets.

## Run The Web Version Locally
Run both processes:

```powershell
Set-Location "D:\Unity Projects\HiddenGame\Server"
npm install
npm run dev
```

```powershell
Set-Location "D:\Unity Projects\HiddenGame\WebClient"
npm install
npm run dev
```

- Server default: `ws://localhost:8080` (`PORT` overrides it).
- Vite usually prints `http://localhost:5173/`.
- `WebClient` auto-connects to `ws://localhost:8080` on localhost.
- To force a remote socket in PowerShell: `$env:VITE_WS_URL='wss://philippeho.popnux.com/ws'; npm run dev`.

## WebClient Map
- Stack: React 19, TypeScript, Vite 8, Tailwind 4, Vitest, MessagePack.
- `WebClient/src/App.tsx`: main UI state machine, setup/matchmaking/ready/countdown/battle/results screens, `wsUrl()` selection.
- `WebClient/src/game/engine.ts`: local deterministic game rules, turn handling, scoring, AI practice opponent, powerup resolution.
- `WebClient/src/game/protocol.ts`: MessagePack packet encoder/decoder. Keep enum values in sync with `Server/src/packetTypes.ts`.
- `WebClient/src/game/networkClient.ts`: browser WebSocket wrapper and client events.
- `WebClient/src/components/BoardGrid.tsx`: 3x3 board UI.
- `WebClient/src/components/PowerupTray.tsx`: shield/reveal/extra-turn controls.
- Tests live in `WebClient/src/game/__tests__/`.

## Game Gist
- 3x3 hidden/blind board game using rock/paper/scissors colors.
- Colors: green = Rock, blue = Paper, red = Scissors.
- Powerups: green unlocks Shield, blue unlocks Reveal, red unlocks Extra Turn.
- Modes in web runtime: local practice bot and online quick match.
- Online flow: connect, set username, join `lobbyRoom`, start matchmaking, receive `match_*` room, ready up, server sends `GAME_START_INFO`.

## Server Map
- Stack: Node, TypeScript, `ws`, `@msgpack/msgpack`; built output goes to `Server/dist/`.
- `Server/src/server.ts`: WebSocket server, default port 8080, permanent rooms `pongRoom` and `lobbyRoom`.
- `Server/src/packetRouter.ts`: dispatches incoming MessagePack arrays by packet type.
- `Server/src/matchmakingHandler.ts`: queue/pairing logic and match room creation.
- `Server/src/roomHandlers.ts`: room create/join/leave/destroy.
- `Server/src/hiddenGameHandler.ts`: ready state, game start packet, hidden move/immune relay, disconnect cleanup.
- `Server/src/userInfoHandler.ts`: stores/broadcasts usernames.
- `Server/src/utils.ts`: room broadcast helpers and server response packets.

## Protocol Notes
- Packets are MessagePack arrays shaped like `[senderId, packetType, ...payload]`.
- Important packet IDs: `ID_ASSIGN=2`, `TIME_SYNC=3`, `ROOM_JOIN=5`, `SERVER_RESPONSE=8`, `USER_INFO=9`, `HIDDEN_GAME=10`, `HIDDEN_GAME_IMMUNE=11`, `HIDDEN_GAME_CONFIRM_START=12`, `MATCH_MAKING_REQUEST=13`, `MATCH_FOUND=14`, `GAME_START_INFO=15`, `OPPONENT_DISCONNECTED=17`, `EXTRA_TURN_MOVES=18`.
- Any protocol change must update both `WebClient/src/game/protocol.ts` and `Server/src/packetTypes.ts`.

## Build/Test Commands
```powershell
Set-Location "D:\Unity Projects\HiddenGame\WebClient"
npm run build
npm test
npm run lint
```

```powershell
Set-Location "D:\Unity Projects\HiddenGame\Server"
npm run build
npm run start
```

Known local status on 2026-04-28: `WebClient` build and tests pass; `npm run lint` fails on existing React hook/useEffectEvent rule violations in `WebClient/src/App.tsx`.

## Deployment/Ops Notes
- Primary server: `ssh phil@3.99.70.5` (Ubuntu AWS). User prefers connecting directly without host-confirm prompts.
- Server project path: `~/projects/HiddenGame/`.
- Production WebSocket proxy: nginx `/ws` -> `http://localhost:8080`.
- Production Unity build path in nginx: `/home/phil/projects/HiddenGame/client/` served at `/HiddenGame/`.
- Server deploy workflow copies `Server/*` to `~/projects/HiddenGame/server`, runs `npm install`, `npm run build`, then restarts/starts PM2 process `hiddengame`.
- Current local nginx references are `hiddengame.conf` and `philippeho.popnux.com.conf`.
- Nginx root on server: `/etc/nginx/`; reload with `sudo nginx -t && sudo systemctl reload nginx`.
- GitHub org target: `PhilHo-Projects`; org secret names: `SERVER_HOST`, `SERVER_SSH_KEY`, `SERVER_USER`.

## Working Rules
- Preserve user changes. The worktree may already be dirty.
- Keep `WebClient` and `Server` protocol changes paired.
- Prefer focused tests around `WebClient/src/game/__tests__/` for rule/protocol changes.
- Do not confuse `WebClient/` with the Unity WebGL artifact in `Client/BuildWebGL/`.
