import * as msgpack from '@msgpack/msgpack';
import { PacketType } from './packetTypes';
import { broadcastToRoom, broadcastOriginalMessageToRoom } from './utils';
import { ServerState, Logger, RoomData } from './types';

const readyStates = new Map<string, Map<number, boolean>>();

export function handleHiddenGamePacket(clientId: number, binaryMessage: Buffer, state: ServerState, log: Logger) {
    broadcastOriginalMessageToRoom(clientId, binaryMessage, state, log);
}

export function handleExtraTurnMoves(clientId: number, binaryMessage: Buffer, state: ServerState, log: Logger) {
    broadcastOriginalMessageToRoom(clientId, binaryMessage, state, log);
}

/**
 * Handle ready state confirmation and start game when all players ready
 */
export function handleHiddenGameConfirmStart(clientId: number, data: any, state: ServerState, log: Logger) {
    const clientState = state.clientConnections.get(clientId);
    if (!clientState || !clientState.roomId) return;

    const roomId = clientState.roomId;
    const room = state.userRooms.get(roomId);
    if (!room) return;

    const isReady = data[2]; // Extract ready state from BooleanPacket

    if (!readyStates.has(roomId)) {
        readyStates.set(roomId, new Map());
    }

    const roomReadyStates = readyStates.get(roomId)!;
    roomReadyStates.set(clientId, isReady);

    log('Player ready state updated', { clientId, roomId, isReady });

    broadcastToRoom(clientId, Buffer.from(msgpack.encode(data)), state, room);

    if (checkAllPlayersReady(room, roomReadyStates)) {
        log('All players ready, starting game', { roomId });

        const firstPlayerId = determineFirstPlayer(roomId, state, log);
        if (firstPlayerId !== null) {
            const startGameMessage = createGameStartMessage(firstPlayerId);

            room.clients.forEach(cId => {
                const clientSocket = state.activeConnections.get(cId);
                if (clientSocket && clientSocket.readyState === 1) {
                    clientSocket.send(startGameMessage);
                }
            });
        }

        readyStates.delete(roomId);
    }
}

function checkAllPlayersReady(room: RoomData, roomReadyStates: Map<number, boolean>) {
    if (room.clients.size !== roomReadyStates.size) {
        return false;
    }

    for (const clientId of room.clients) {
        if (!roomReadyStates.get(clientId)) {
            return false;
        }
    }

    return true;
}

function createGameStartMessage(firstPlayerId: number) {
    const gameStartPacket = [
        0, // Server sender ID
        PacketType.GAME_START_INFO,
        firstPlayerId
    ];

    return Buffer.from(msgpack.encode(gameStartPacket));
}

function determineFirstPlayer(roomId: string, state: ServerState, log: Logger) {
    const room = state.userRooms.get(roomId);
    if (!room) return null;

    const playerArray = Array.from(room.clients);
    const firstPlayerIndex = Math.floor(Math.random() * playerArray.length);
    const firstPlayerId = playerArray[firstPlayerIndex];

    log('First player determined', { roomId, firstPlayerId });

    return firstPlayerId as number;
}

/**
 * Clean up ready states and notify opponents when player disconnects
 */
export function handleClientDisconnect(clientId: number, state: ServerState, log: Logger) {
    for (const [roomId, roomReadyStates] of readyStates.entries()) {
        if (roomReadyStates.has(clientId)) {
            roomReadyStates.delete(clientId);

            const room = state.userRooms.get(roomId);
            if (room) {
                room.clients.forEach(otherId => {
                    if (otherId !== clientId) {
                        const otherSocket = state.activeConnections.get(otherId);
                        if (otherSocket && otherSocket.readyState === 1) {
                            const disconnectPacket = [
                                0,
                                PacketType.OPPONENT_DISCONNECTED,
                                true
                            ];
                            otherSocket.send(msgpack.encode(disconnectPacket));
                        }
                    }
                });
            }

            if (!room || room.clients.size <= 1) {
                readyStates.delete(roomId);
            }

            log('Player disconnected during ready phase', { clientId, roomId });
            break;
        }
    }
}

export function handleHiddenGameImmune(clientId: number, binaryMessage: Buffer, state: ServerState, log: Logger) {
    log('Broadcasting immune pieces update', { clientId });
    broadcastOriginalMessageToRoom(clientId, binaryMessage, state, log);
}
