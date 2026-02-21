import { broadcastOriginalMessageToRoom } from './utils';
import { ServerState, Logger } from './types';

export function handlePositionPacket(clientId: number, binaryMessage: Buffer, state: ServerState, log: Logger) {

    const pongRoom = state.userRooms.get('pongRoom');
    if (!pongRoom) return;

    broadcastOriginalMessageToRoom(clientId, binaryMessage, state, log, 'pongRoom');
}
