import { broadcastOriginalMessageToRoom } from './utils';
import { ServerState, Logger } from './types';

export function handleChatPacket(clientId: number, decoded: any, binaryMessage: Buffer, state: ServerState, log: Logger) {
    const [senderId, packetType, text] = decoded;

    if (!text || typeof text !== 'string') {
        log('Invalid chat message', { clientId });
        return;
    }

    log('Chat message received, preparing to broadcast', { clientId, senderId, text });
    broadcastOriginalMessageToRoom(clientId, binaryMessage, state, log);
}
