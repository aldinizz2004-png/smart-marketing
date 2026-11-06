import { MessageCircleHeart, Settings, Sparkles } from 'lucide-react'
import { Route, Routes } from 'react-router'
import { PublicOnly, RequireAuth } from '@/auth/guards'
import { LoginPage } from '@/pages/auth/login-page'
import { RegisterPage } from '@/pages/auth/register-page'
import { ComingSoonPage } from '@/pages/coming-soon-page'
import { HomePage } from '@/pages/home-page'
import { NotFoundPage } from '@/pages/not-found-page'
import { AppShell } from './layout/app-shell'

export function AppRoutes() {
  return (
    <Routes>
      <Route path="login" element={<PublicOnly><LoginPage /></PublicOnly>} />
      <Route path="register" element={<PublicOnly><RegisterPage /></PublicOnly>} />
      <Route element={<RequireAuth><AppShell /></RequireAuth>}>
        <Route index element={<HomePage />} />
        <Route path="content" element={<ComingSoonPage page="content" icon={Sparkles} />} />
        <Route path="comments" element={<ComingSoonPage page="comments" icon={MessageCircleHeart} />} />
        <Route path="settings/*" element={<ComingSoonPage page="settings" icon={Settings} />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
