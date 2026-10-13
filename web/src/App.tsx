import { lazy, Suspense } from 'react'
import { BrowserRouter, Route, Routes } from 'react-router'
import { useI18n } from './i18n'
import { CreateVillagePage } from './pages/CreateVillagePage'
import { HomePage } from './pages/HomePage'

// The map page carries the PixiJS renderer; the home page does not wait for it.
const GamePage = lazy(() => import('./pages/GamePage').then((module) => ({ default: module.GamePage })))

function Loading() {
  const i18n = useI18n()
  return <div className="splash">{i18n.t('loading')}</div>
}

export function App() {
  return (
    <BrowserRouter>
      <Suspense fallback={<Loading />}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/new" element={<CreateVillagePage />} />
          <Route path="/v/:slug" element={<GamePage />} />        </Routes>
      </Suspense>
    </BrowserRouter>
  )
}
