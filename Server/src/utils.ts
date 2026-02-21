import WebSocket from 'ws';
import * as msgpack from '@msgpack/msgpack';
import { PacketType } from './packetTypes';
import { ServerState, Logger, RoomData } from './types';

export function broadcastToRoom(senderId: number, binaryMessage: Buffer, state: ServerState, roomData: RoomData) {
    if (!roomData?.clients) return;

    roomData.clients.forEach(clientId => {
        if (clientId !== senderId) {
            const clientSocket = state.activeConnections.get(clientId);
            if (clientSocket && clientSocket.readyState === WebSocket.OPEN) {
                clientSocket.send(binaryMessage);
            }
        }
    });
}

export function broadcastOriginalMessageToRoom(clientId: number, binaryMessage: Buffer, state: ServerState, log: Logger = console.log, targetRoomId: string | null = null) {
    let roomData: RoomData | undefined;
    let roomIdToLog: string | undefined;

    if (targetRoomId) {
        roomData = state.userRooms.get(targetRoomId);
        roomIdToLog = targetRoomId;
        if (!roomData) {
            log(`Target room ${targetRoomId} not found for client ${clientId}`);
            return;
        }
    } else {
        const clientState = state.clientConnections?.get(clientId);
        if (clientState?.roomId) {
            roomIdToLog = clientState.roomId;
            roomData = state.userRooms.get(roomIdToLog);
        } else {
            // Fallback: search all rooms for this client
            const roomEntry = Array.from(state.userRooms.entries())
                .find(([_, rData]) => rData.clients.has(clientId));
            if (roomEntry) {
                [roomIdToLog, roomData] = roomEntry;
            }
        }

        if (!roomData) {
            log(`Client ${clientId} not in any room`);
            return;
        }
    }

    log(`Broadcasting for client ${clientId} in room ${roomIdToLog}`);
    broadcastToRoom(clientId, binaryMessage, state, roomData);
}

export function sendServerResponseToClient(clientId: number, responseData: any, originalPacketType: PacketType, state: ServerState, log: Logger = console.log) {
    const clientSocket = state.activeConnections.get(clientId);
    if (clientSocket && clientSocket.readyState === WebSocket.OPEN) {
        try {
            const packet = [0, PacketType.SERVER_RESPONSE, responseData, originalPacketType];
            clientSocket.send(msgpack.encode(packet));
            log(`Sent server response to ${clientId}`, { responseData, originalPacketType });
        } catch (error: any) {
            log(`Error sending server response to ${clientId}: ${error.message}`);
        }
    } else {
        log(`Cannot send server response, client ${clientId} not connected`);
    }
}

