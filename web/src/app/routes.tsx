import { Settings } from 'lucide-react'
import { Route, Routes } from 'react-router'
import { PublicOnly, RequireAuth } from '@/auth/guards'
import { RequireWorkspace } from '@/features/workspace/require-workspace'
import { LoginPage } from '@/pages/auth/login-page'
import { RegisterPage } from '@/pages/auth/register-page'
import { CampaignDetailPage } from '@/pages/campaigns/campaign-detail-page'
import { CampaignsPage } from '@/pages/campaigns/campaigns-page'
import { CommentsPage } from '@/pages/comments/comments-page'
import { ComingSoonPage } from '@/pages/coming-soon-page'
import { ContentDetailPage } from '@/pages/content/content-detail-page'
import { ContentListPage } from '@/pages/content/content-list-page'
import { CreatePostPage } from '@/pages/content/create-post-page'
import { HomePage } from '@/pages/home-page'
import { InboxPage } from '@/pages/inbox/inbox-page'
import { NewBusinessPage } from '@/pages/new-business-page'
import { NotFoundPage } from '@/pages/not-found-page'
import { OnboardingPage } from '@/pages/onboarding/onboarding-page'
import { AppShell } from './layout/app-shell'

export function AppRoutes() {
  return (
    <Routes>
      <Route path="login" element={<PublicOnly><LoginPage /></PublicOnly>} />
      <Route path="register" element={<PublicOnly><RegisterPage /></PublicOnly>} />
      <Route path="onboarding" element={<RequireAuth><OnboardingPage /></RequireAuth>} />
      <Route element={<RequireAuth><RequireWorkspace><AppShell /></RequireWorkspace></RequireAuth>}>
        <Route index element={<HomePage />} />
        <Route path="businesses/new" element={<NewBusinessPage />} />
        <Route path="content" element={<ContentListPage />} />
        <Route path="content/new" element={<CreatePostPage />} />
        <Route path="content/:id" element={<ContentDetailPage />} />
        <Route path="campaigns" element={<CampaignsPage />} />
        <Route path="campaigns/:id" element={<CampaignDetailPage />} />
        <Route path="comments" element={<CommentsPage />} />
        <Route path="inbox" element={<InboxPage />} />
        <Route path="inbox/:id" element={<InboxPage />} />
        <Route path="settings/*" element={<ComingSoonPage page="settings" icon={Settings} />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
