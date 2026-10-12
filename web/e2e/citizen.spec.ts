import {
  closeModal,
  DEMO_SLUG,
  developmentLogin,
  expect,
  expectVillageView,
  openInventory,
  openMenu,
  test,
  uniqueName,
  visitEveryTab,
} from './fixtures.ts'

test.describe('citizen', () => {
  test('a visitor watches the demo village without signing in', async ({ page }) => {
    await page.goto(`/v/${DEMO_SLUG}`)
    await expectVillageView(page)

    const menu = await openMenu(page)
    await expect(menu.getByRole('heading', { name: 'Sign in to become a citizen' })).toBeVisible()
    await closeModal(page, 'Menu')

    const inventory = await openInventory(page)
    await expect(inventory.getByRole('heading', { name: 'Village storage' })).toBeVisible()
    await closeModal(page, 'Inventory')
  })

  test('signs in, creates a character, opens every panel and starts a task', async ({ page }) => {
    test.slow()
    await page.goto(`/v/${DEMO_SLUG}`)
    await expectVillageView(page)

    await page.locator('.hud-status').getByRole('button', { name: 'Become a citizen' }).click()
    const menu = page.getByRole('dialog', { name: 'Menu' })
    await expect(menu.locator('nav.tabs button.active')).toHaveText('Character')
    await developmentLogin(page, uniqueName('citizen'))

    const form = menu.locator('form.character-creation')
    await expect(form.getByRole('heading', { name: 'Join the tribe' })).toBeVisible()
    await form.getByRole('button', { name: 'Random', exact: true }).click()
    await form.getByLabel('Name', { exact: true }).fill('Smoky')
    await form.getByRole('button', { name: 'Join the camp' }).click()

    await expect(menu.locator('nav.tabs button.active')).toHaveText('Tasks')
    await visitEveryTab(page, ['Village', 'Character', 'Tasks', 'Discoveries', 'Settings'])

    await menu.locator('nav.tabs button', { hasText: 'Character' }).click()
    await expect(menu.getByText('Energy').first()).toBeVisible()

    await menu.locator('nav.tabs button', { hasText: 'Tasks' }).click()
    await menu.locator('button.task-card', { hasText: 'Gather wood' }).click()
    await expect(menu).toBeHidden()
    await expect(page.locator('.hud-status')).toContainText('next action in')

    const inventory = await openInventory(page)
    await expect(inventory.getByRole('heading', { name: /Smoky/ })).toBeVisible()
    await closeModal(page, 'Inventory')
  })

  test('an unknown village shows an error and a way back', async ({ page }) => {
    await page.goto('/v/no-such-village')

    await expect(page.getByText('Village "no-such-village" was not found.')).toBeVisible()
    await page.getByRole('link', { name: 'Back to home' }).click()
    await expect(page).toHaveURL('/')
  })
})

test.describe('stream overlay', () => {
  test('renders the village without a login', async ({ page }) => {
    await page.goto(`/v/${DEMO_SLUG}/overlay?lang=ru&night=1`)

    await expect(page.locator('canvas').first()).toBeVisible()
    await expect(page.locator('.overlay-join')).toContainText(`/v/${DEMO_SLUG}`)
  })
})
