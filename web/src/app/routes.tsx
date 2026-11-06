import { MessageCircleHeart, Settings, Sparkles } from 'lucide-react'
import { Route, Routes } from 'react-router'
import { ComingSoonPage } from '@/pages/coming-soon-page'
import { HomePage } from '@/pages/home-page'
import { NotFoundPage } from '@/pages/not-found-page'
import { AppShell } from './layout/app-shell'

export function AppRoutes() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<HomePage />} />
        <Route path="content" element={<ComingSoonPage page="content" icon={Sparkles} />} />
        <Route path="comments" element={<ComingSoonPage page="comments" icon={MessageCircleHeart} />} />
        <Route path="settings" element={<ComingSoonPage page="settings" icon={Settings} />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
