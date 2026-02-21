import WebSocket from 'ws';
import * as msgpack from '@msgpack/msgpack';
import { routePacket } from './packetRouter';
import { PacketType } from './packetTypes';
import { handleRoomLeavePacket } from './roomHandlers';
import { updateUserNamesToClients } from './userInfoHandler';
import { handleClientDisconnectMatchmaking } from './matchmakingHandler';
import { handleClientDisconnect as handleHiddenGameDisconnect } from './hiddenGameHandler';

import { ServerState, ClientConnectionInfo, RoomData } from './types';


// 1) Server Configuration
const SERVER_CONFIG = {
    port: process.env.PORT ? parseInt(process.env.PORT) : 8080,
    timeSync: {
        interval: 1000,
        enabled: false
    }
};

const PERMANENT_ROOMS = {
    PONG_ROOM: 'pongRoom',
    LOBBY_ROOM: 'lobbyRoom'
};

// 2) Server State
const ACTIVE_DATA: ServerState = {
    usedClientIds: new Set<number>(),
    userNames: new Map<number, string>(),
    activeConnections: new Map<number, WebSocket>(),
    clientConnections: new Map<number, ClientConnectionInfo>(),
    userRooms: new Map<string, RoomData>()
};

// 4) Simple Logger
function log(message: string, data: any = {}) {
    const d = new Date();
    const t = `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:${String(d.getSeconds()).padStart(2, '0')}.${String(d.getMilliseconds()).padStart(3, '0')}`;
    console.log(JSON.stringify({ t, message, ...data }));
}

// 4) Utility: Generate next client ID
let nextClientId = 1;
function getNextAvailableClientId() {
    const id = nextClientId++;  // ← That's it. Done. Atomic.
    ACTIVE_DATA.usedClientIds.add(id);  // Optional for tracking
    return id;
}

// 5) Utility: Create new client connection info
function createClientConnectionInfo(): ClientConnectionInfo {
    return {
        connectTime: Date.now(),
        roomId: null
    };
}

// 6) Utility: Encode / Decode
function encodePacket(packetArray: any[]) {
    return msgpack.encode(packetArray);
}

function decodeMsgPack(message: Buffer): any {
    try {
        const decoded = msgpack.decode(message, {
            // @ts-ignore
            useDefaults: true,
            // @ts-ignore
            ignoreUndefined: true,
            // @ts-ignore
            requireAllProperties: false,
            extensionCodec: new msgpack.ExtensionCodec(),
            context: undefined,
        });
        log('Decoded MessagePack data', {
            raw: Array.from(message).slice(0, 20),
            decoded
        });
        return decoded;
    } catch (error: any) {
        log('MessagePack decode failed', {
            error: error.message,
            messageLength: message.length
        });
        return null;
    }
}


// 7) Time Sync
let globalSequenceNumber = 0;
function getNextSequenceNumber() {
    return globalSequenceNumber++;
}

function sendTimeSync() {
    const currentTime = Date.now();
    const packet = [
        0,
        PacketType.TIME_SYNC,
        getNextSequenceNumber(),
        currentTime
    ];
    const encodedPacket = encodePacket(packet);

    ACTIVE_DATA.activeConnections.forEach((client, id) => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(encodedPacket);
            log('Sent TIME_SYNC packet', { clientId: id, serverTime: currentTime });
        }
    });
}

// 8) ID Assignment
function sendClientIdAssignment(socket: WebSocket, clientId: number) {
    const packet = [
        0,
        PacketType.ID_ASSIGN,
        clientId
    ];
    const encodedPacket = encodePacket(packet);
    socket.send(encodedPacket);
    log('Sent ID_ASSIGN packet', { clientId, encodedPacket: Array.from(encodedPacket) });
}

// 9) Handle Connections and Disconnections
function handleNewConnection(socket: WebSocket) {
    const clientId = getNextAvailableClientId();
    setupNewClient(clientId, socket);
    setupClientEventListeners(clientId, socket);
}

function setupNewClient(clientId: number, socket: WebSocket) {
    log('New client connected', { clientId });
    ACTIVE_DATA.activeConnections.set(clientId, socket);
    ACTIVE_DATA.clientConnections.set(clientId, createClientConnectionInfo());
    sendClientIdAssignment(socket, clientId);
}

function setupClientEventListeners(clientId: number, socket: WebSocket) {
    socket.on('message', (message: Buffer) => {
        routePacket(clientId, message, ACTIVE_DATA, log, decodeMsgPack);
    });

    socket.on('close', () => {
        handleClientDisconnection(clientId);
    });

    socket.on('error', (error: Error) => {
        log('Socket error', { clientId, error: error.message });
    });
}

function handleClientDisconnection(clientId: number) {
    try {
        const clientInfo = ACTIVE_DATA.clientConnections.get(clientId);

        log('Client disconnected', {
            clientId,
            roomId: clientInfo?.roomId,
            stats: clientInfo ? {
                connectTime: clientInfo.connectTime,
                disconnectTime: Date.now(),
                connectionDuration: Date.now() - clientInfo.connectTime
            } : null
        });

        // 1. Handle game-specific cleanup FIRST (while clientInfo still exists)
        if (clientInfo?.roomId) {
            // Game-specific disconnection handling
            handleClientDisconnectMatchmaking(clientId, ACTIVE_DATA, log);
            handleHiddenGameDisconnect(clientId, ACTIVE_DATA, log);

            // Room departure handling
            handleRoomLeavePacket(clientId, clientInfo.roomId, ACTIVE_DATA, log, PERMANENT_ROOMS);
        }

        // 2. Clean up client data AFTER game cleanup
        ACTIVE_DATA.activeConnections.delete(clientId);
        ACTIVE_DATA.clientConnections.delete(clientId);
        ACTIVE_DATA.usedClientIds.delete(clientId);
        ACTIVE_DATA.userNames.delete(clientId);

        // 3. Update other clients
        updateUserNamesToClients(ACTIVE_DATA, log);

    } catch (error: any) {
        log('Error during client disconnection cleanup', {
            clientId,
            error: error.message
        });
    }
}

// 10) Server Initialization
function initializePermanentRooms() {
    ACTIVE_DATA.userRooms.set(PERMANENT_ROOMS.PONG_ROOM, {
        clients: new Set<number>(),
    });
    ACTIVE_DATA.userRooms.set(PERMANENT_ROOMS.LOBBY_ROOM, {
        clients: new Set<number>(),
    });
}

const server = new WebSocket.Server({ port: SERVER_CONFIG.port });
initializePermanentRooms();
server.on('connection', handleNewConnection);
log('WebSocket server started', {});
