import { COLOR_GREEN, COLOR_RED } from '../constants'
import {
  decodePacket,
  encodeExtraTurnPacket,
  encodeHiddenMovePacket,
  encodeImmunePacket,
  PacketType,
} from '../protocol'

describe('protocol', () => {
  it('encodes and decodes a hidden move packet', () => {
    const packet = decodePacket(encodeHiddenMovePacket(7, 4, COLOR_GREEN))

    expect(packet.type).toBe(PacketType.HIDDEN_GAME)
    if (packet.type !== PacketType.HIDDEN_GAME) {
      throw new Error('Expected hidden move packet')
    }

    expect(packet.senderId).toBe(7)
    expect(packet.index).toBe(4)
    expect(packet.color).toBe(COLOR_GREEN)
  })

  it('encodes and decodes extra turn packets', () => {
    const packet = decodePacket(
      encodeExtraTurnPacket(3, [
        { index: 0, color: COLOR_RED },
        { index: 8, color: COLOR_GREEN },
      ]),
    )

    expect(packet.type).toBe(PacketType.EXTRA_TURN_MOVES)
    if (packet.type !== PacketType.EXTRA_TURN_MOVES) {
      throw new Error('Expected extra turn packet')
    }

    expect(packet.moves).toHaveLength(2)
    expect(packet.moves[1]?.index).toBe(8)
    expect(packet.moves[1]?.color).toBe(COLOR_GREEN)
  })

  it('encodes and decodes immune packets', () => {
    const packet = decodePacket(encodeImmunePacket(5, [2, 6]))

    expect(packet.type).toBe(PacketType.HIDDEN_GAME_IMMUNE)
    if (packet.type !== PacketType.HIDDEN_GAME_IMMUNE) {
      throw new Error('Expected immune packet')
    }

    expect(packet.indices).toEqual([2, 6])
  })
})
