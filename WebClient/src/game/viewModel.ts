import type { HiddenGameState } from './types'
import type { UserEntry } from './protocol'

export type Screen = 'intro' | 'online-name' | 'offline-setup' | 'matchmaking' | 'ready' | 'countdown' | 'battle' | 'results' | 'disconnected'

export function getScreenLabel(screen: Screen) {
  return {
    intro: 'Workshop',
    'online-name': 'Online',
    'offline-setup': 'Offline',
    matchmaking: 'Searching',
    ready: 'Ready',
    countdown: 'Launch',
    battle: 'Battle',
    results: 'Results',
    disconnected: 'Disconnected',
  }[screen]
}

export function normalizeUsername(value: string) {
  return value.trim()
}

export function getOpponentName(users: UserEntry[], clientId: number | null | undefined, match: HiddenGameState | null) {
  return (
    users.find((entry) => entry.userId !== clientId)?.userName ??
    (match?.config.hasAI ? 'Practice Bot' : 'Opponent')
  )
}

export function shouldShowOpponentBoard(match: HiddenGameState | null, screen: Screen) {
  if (!match) return false
  return !match.config.blindMode || match.playerPowerups.revealActive || screen === 'results'
}
