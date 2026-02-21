import WebSocket from 'ws';

export interface ClientConnectionInfo {
    connectTime: number;
    roomId: string | null;
}

export interface RoomData {
    clients: Set<number>;
}

export interface ServerState {
    usedClientIds: Set<number>;
    userNames: Map<number, string>;
    activeConnections: Map<number, WebSocket>;
    clientConnections: Map<number, ClientConnectionInfo>;
    userRooms: Map<string, RoomData>;
}

export type Logger = (message: string, data?: any) => void;
