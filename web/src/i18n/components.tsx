import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { I18nContext, useI18n } from './context'
import {
  DICTIONARIES,
  LANGUAGES,
  LANGUAGE_STORAGE_KEY,
  createTranslator,
  initialLanguage,
  type Language,
} from './translator'

export function I18nProvider({ children }: { children: ReactNode }) {
  const [language, setLanguageState] = useState<Language>(initialLanguage)

  const setLanguage = useCallback((next: Language) => {
    window.localStorage.setItem(LANGUAGE_STORAGE_KEY, next)
    setLanguageState(next)
  }, [])

  useEffect(() => {
    document.documentElement.lang = language
    document.title = DICTIONARIES[language].ui.brand
  }, [language])

  const value = useMemo(() => ({ ...createTranslator(language), setLanguage }), [language, setLanguage])
  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}

export function LanguageSwitcher() {
  const { language, setLanguage, t } = useI18n()
  return (
    <select
      className="language-switcher"
      aria-label={t('language')}
      value={language}
      onChange={(event) => setLanguage(event.target.value as Language)}
    >
      {LANGUAGES.map((option) => (
        <option key={option.code} value={option.code}>
          {option.label}
        </option>
      ))}
    </select>
  )
}
