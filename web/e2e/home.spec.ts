import { DEMO_NAME, DEMO_SLUG, developmentLogin, expect, test, uniqueName } from './fixtures.ts'

test.describe('home page', () => {
  test('shows the login and the open villages to a visitor', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { name: 'Desertod', level: 1 })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Sign in to start' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Open villages' })).toBeVisible()
    await expect(page.getByRole('link', { name: DEMO_NAME })).toHaveAttribute('href', `/v/${DEMO_SLUG}`)
  })

  test('switches the language and remembers it', async ({ page }) => {
    await page.goto('/')

    await page.getByRole('combobox', { name: 'Language' }).selectOption('ru')
    await expect(page.getByRole('heading', { name: 'Открытые города' })).toBeVisible()

    await page.reload()
    await expect(page.getByRole('heading', { name: 'Войдите, чтобы начать' })).toBeVisible()
  })

  test('signs in, lists villages and signs out', async ({ page }) => {
    const name = uniqueName('home')
    await page.goto('/')

    await developmentLogin(page, name)
    await expect(page.getByText('Signed in as')).toContainText(name)
    await expect(page.getByRole('heading', { name: 'Your villages' })).toBeVisible()
    await expect(page.getByText('You do not live anywhere yet.')).toBeVisible()

    await page.getByRole('button', { name: 'Sign out' }).click()
    await expect(page.getByRole('heading', { name: 'Sign in to start' })).toBeVisible()
  })

  test('visiting an unknown village address shows an error', async ({ page }) => {
    await page.goto('/')
    await developmentLogin(page, uniqueName('visit'))

    await page.getByPlaceholder('village-address').fill('no-such-village')
    await page.getByRole('button', { name: 'Go' }).click()

    await expect(page.getByText('Village "no-such-village" was not found.')).toBeVisible()
  })

  test('opens a village from the open villages list', async ({ page }) => {
    await page.goto('/')

    await page.getByRole('link', { name: DEMO_NAME }).click()

    await expect(page).toHaveURL(`/v/${DEMO_SLUG}`)
    await expect(page.locator('canvas').first()).toBeVisible()
  })
})
