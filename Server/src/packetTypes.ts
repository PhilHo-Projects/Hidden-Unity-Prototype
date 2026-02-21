// packetTypes.js

export enum PacketType {
    CHAT = 0,
    POSITION = 1,
    ID_ASSIGN = 2,
    TIME_SYNC = 3,

    // For rooms:
    ROOM_CREATE = 4,
    ROOM_JOIN = 5,
    ROOM_LEAVE = 6,
    ROOM_DESTROY = 7,
    SERVER_RESPONSE = 8,
    USER_INFO = 9,

    // Hidden game
    HIDDEN_GAME = 10,
    HIDDEN_GAME_IMMUNE = 11,
    HIDDEN_GAME_CONFIRM_START = 12,
    MATCH_MAKING_REQUEST = 13,
    MATCH_FOUND = 14,
    
    // New packet types for the ready system:
    GAME_START_INFO = 15,
    OPPONENT_DISCONNECTED = 17,
    EXTRA_TURN_MOVES = 18
}

// Explicit packet structures replacing array indexing

export interface BasePacket {
    0: number; // usually 0
    1: PacketType;
}

export interface ChatPacket extends BasePacket {
    1: PacketType.CHAT;
    2: string; // The chat message
}

export interface PositionPacket extends BasePacket {
    1: PacketType.POSITION;
    // position specific fields based on existing client expectations (e.g. x, y, z)
}

export interface TimeSyncPacket extends BasePacket {
    1: PacketType.TIME_SYNC;
    2: number; // global sequence number
    3: number; // current time
}

export interface RoomCreatePacket extends BasePacket {
    1: PacketType.ROOM_CREATE;
    2: string; // target room name/id
}

export interface RoomJoinPacket extends BasePacket {
    1: PacketType.ROOM_JOIN;
    2: string; // target room name/id
}

export interface RoomLeavePacket extends BasePacket {
    1: PacketType.ROOM_LEAVE;
    2: string; // target room name/id
}

export interface RoomDestroyPacket extends BasePacket {
    1: PacketType.ROOM_DESTROY;
    2: string; // target room name/id
}

export interface UserInfoPacket extends BasePacket {
    1: PacketType.USER_INFO;
    2: string; // user name
}

export interface MatchmakingRequestPacket extends BasePacket {
    1: PacketType.MATCH_MAKING_REQUEST;
    2: number; // something else
    3: boolean; // boolean flag
}

export interface HiddenGameConfirmStartPacket extends BasePacket {
    1: PacketType.HIDDEN_GAME_CONFIRM_START;
}

export type AnyPacket = 
    | ChatPacket 
    | PositionPacket 
    | TimeSyncPacket 
    | RoomCreatePacket 
    | RoomJoinPacket 
    | RoomLeavePacket 
    | RoomDestroyPacket 
    | UserInfoPacket 
    | MatchmakingRequestPacket 
    | HiddenGameConfirmStartPacket 
    | BasePacket;
