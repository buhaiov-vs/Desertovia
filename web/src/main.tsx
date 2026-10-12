import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router'
import { I18nProvider } from './i18n'
import './index.css'
import { CreateVillagePage } from './pages/CreateVillagePage'
import { GamePage } from './pages/GamePage'
import { HomePage } from './pages/HomePage'
import { OverlayPage } from './pages/OverlayPage'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <I18nProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/new" element={<CreateVillagePage />} />
          <Route path="/v/:slug" element={<GamePage />} />
          <Route path="/v/:slug/overlay" element={<OverlayPage />} />
        </Routes>
      </BrowserRouter>
    </I18nProvider>
  </StrictMode>,
)
