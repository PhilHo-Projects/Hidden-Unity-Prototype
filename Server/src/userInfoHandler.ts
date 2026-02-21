import WebSocket from 'ws';
import * as msgpack from '@msgpack/msgpack';
import { PacketType } from './packetTypes';
import { ServerState, Logger } from './types';

export function storeUserName(clientId: number, userName: string, state: ServerState, log: Logger) {
    log('Received username data', {
        clientId,
        userName,
        typeOfUserName: typeof userName,
        userNameValue: userName
    });

    if (!userName || typeof userName !== 'string') {
        log('Invalid username received', {
            clientId,
            userName,
            typeOfUserName: typeof userName
        });
        return;
    }

    const previousName = state.userNames.get(clientId);
    state.userNames.set(clientId, userName);

    log('Username stored', {
        clientId,
        userName,
        previousName: previousName || 'none'
    });

    updateUserNamesToClients(state, log);
}

export function updateUserNamesToClients(state: ServerState, log: Logger) {
    const userList = Array.from(state.userNames.entries()).map(([id, name]) =>
        [id, name]
    );

    // Prepare the packet
    const packet = msgpack.encode([
        0, // sequence number (not used for system messages)
        PacketType.USER_INFO,
        userList
    ]);

    // Send to all connected clients
    state.activeConnections.forEach((socket, clientId) => {
        if (socket.readyState === WebSocket.OPEN) {
            socket.send(packet);
            log('Sent user list update', {
                recipientId: clientId,
                userCount: userList.length
            });
        }
    });

    log('Broadcast user list update', {
        connectedUsers: userList.length,
        users: userList
    });
}
