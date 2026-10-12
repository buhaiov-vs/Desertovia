import type { UiKey } from '../i18n/en'

/** Village is shared with everyone, character and tasks are the player's own, settings are not part of the game. */
export type MenuTab = 'village' | 'character' | 'tasks' | 'build' | 'technologies' | 'settings'

export const TABS: Record<MenuTab, UiKey> = {
  village: 'tab.village',
  character: 'tab.character',
  tasks: 'tab.tasks',
  build: 'tab.build',
  technologies: 'tab.technologies',
  settings: 'tab.settings',
}

/** Citizens work and research discoveries; the Governor (village owner) builds and calls votes on discoveries. */
export const tabsFor = (governor: boolean): MenuTab[] =>
  governor
    ? ['village', 'build', 'technologies', 'settings']
    : ['village', 'character', 'tasks', 'technologies', 'settings']
