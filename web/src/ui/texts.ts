/** First-age goods: always in the top bar and the inventory; later goods appear once the village has some. */
export const HUD_RESOURCES = ['Wood', 'Stone', 'Food', 'Reeds', 'Leather', 'Feathers']

export function seconds(milliseconds: number) {
  return Math.max(0, Math.ceil(milliseconds / 1000))
}
