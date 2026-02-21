import WebSocket from 'ws';
import * as msgpack from '@msgpack/msgpack';
import { PacketType } from './packetTypes';
import { sendServerResponseToClient } from './utils';
import { ServerState, Logger } from './types';

// Matchmaking queue and state
const matchmakingState = {
    queue: new Set<number>(),
    activeMatches: new Map<string, { players: number[], startTime: number }>()
};

// Process matchmaking requests
export function handleMatchmakingRequest(clientId: number, data: any, state: ServerState, log: Logger) {
    const isSearching = data[2]; // Boolean value from BooleanPacket

    if (isSearching) {
        // Add client to matchmaking queue
        addClientToQueue(clientId, state, log);
    } else {
        // Remove client from matchmaking queue
        removeClientFromQueue(clientId, log);
    }
}

// Add client to matchmaking queue
function addClientToQueue(clientId: number, state: ServerState, log: Logger) {
    log('Client added to matchmaking queue', { clientId });

    // Add to queue
    matchmakingState.queue.add(clientId);

    // Try to match immediately
    processMatchmakingQueue(state, log);
}

// Remove client from matchmaking queue
function removeClientFromQueue(clientId: number, log: Logger) {
    log('Client removed from matchmaking queue', { clientId });
    matchmakingState.queue.delete(clientId);
}

// Process the queue and match players
function processMatchmakingQueue(state: ServerState, log: Logger) {
    // Convert Set to Array for easier manipulation
    const queueArray = Array.from(matchmakingState.queue);

    // Need at least 2 players to make a match
    if (queueArray.length < 2) {
        return;
    }

    // Take the first two players from the queue
    const player1 = queueArray[0]!;
    const player2 = queueArray[1]!;

    // Create a new room ID for the match
    const matchRoomId = `match_${Date.now()}_${Math.floor(Math.random() * 1000)}`;

    // Create the match room
    state.userRooms.set(matchRoomId, {
        clients: new Set([player1, player2]),
        // @ts-ignore (we just add the extra data here dynamically for matches)
        matchData: {
            startTime: Date.now(),
            players: [player1, player2]
        }
    });

    // Update client room information
    if (state.clientConnections.has(player1)) {
        state.clientConnections.get(player1)!.roomId = matchRoomId;
    }
    if (state.clientConnections.has(player2)) {
        state.clientConnections.get(player2)!.roomId = matchRoomId;
    }

    // Remove players from queue
    matchmakingState.queue.delete(player1);
    matchmakingState.queue.delete(player2);

    // Add to active matches
    matchmakingState.activeMatches.set(matchRoomId, {
        players: [player1, player2],
        startTime: Date.now()
    });

    log('Match created', { matchRoomId, player1, player2 });

    // Notify both players that a match was found
    notifyMatchFound(player1, player2, matchRoomId, state, log);
}

// Send match found notification to both players
function notifyMatchFound(player1: number, player2: number, roomId: string, state: ServerState, log: Logger) {
    const players = [player1, player2];

    players.forEach(playerId => {
        const playerSocket = state.activeConnections.get(playerId);
        if (playerSocket && playerSocket.readyState === WebSocket.OPEN) {
            // Create MatchFound packet (RoomAction type)
            const matchFoundPacket = [
                0, // SenderId (server is 0)
                PacketType.MATCH_FOUND,
                roomId // RoomId field
            ];

            const encodedPacket = msgpack.encode(matchFoundPacket);
            playerSocket.send(encodedPacket);

            log('Sent match found notification', { playerId, roomId });
        }
    });
}

// Cleanup function to handle disconnects during matchmaking
export function handleClientDisconnectMatchmaking(clientId: number, state: ServerState, log: Logger) {
    // Remove from matchmaking queue if present
    if (matchmakingState.queue.has(clientId)) {
        removeClientFromQueue(clientId, log);
    }

    // Check if client is in an active match
    for (const [roomId, matchData] of matchmakingState.activeMatches.entries()) {
        if (matchData.players.includes(clientId)) {
            // Notify other player that opponent disconnected
            const otherPlayer = matchData.players.find(id => id !== clientId);
            if (otherPlayer) {
                // To avoid sending response without undefined, type coercion needed:
                sendServerResponseToClient(otherPlayer, false, PacketType.OPPONENT_DISCONNECTED, state, log);
            }

            // Clean up the match
            matchmakingState.activeMatches.delete(roomId);

            // Room cleanup should happen in handleClientDisconnection in server.js
        }
    }
}
