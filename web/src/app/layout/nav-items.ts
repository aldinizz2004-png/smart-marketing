import { Binoculars, ChartNoAxesCombined, FileText, House, Megaphone, MessageCircleHeart, MessagesSquare, Package, Settings, Sparkles, type LucideIcon } from 'lucide-react'

export type NavItem = {
  to: string
  labelKey: string
  icon: LucideIcon
  // Shown in the phone's bottom bar; the rest go under "More".
  bottom?: boolean
}

export const navItems: NavItem[] = [
  { to: '/', labelKey: 'nav.home', icon: House, bottom: true },
  { to: '/content', labelKey: 'nav.content', icon: Sparkles, bottom: true },
  { to: '/inbox', labelKey: 'nav.inbox', icon: MessagesSquare, bottom: true },
  { to: '/comments', labelKey: 'nav.comments', icon: MessageCircleHeart, bottom: true },
  { to: '/campaigns', labelKey: 'nav.campaigns', icon: Megaphone },
  { to: '/catalog', labelKey: 'nav.catalog', icon: Package },
  { to: '/insights', labelKey: 'nav.insights', icon: ChartNoAxesCombined },
  { to: '/competitors', labelKey: 'nav.competitors', icon: Binoculars },
  { to: '/reports', labelKey: 'nav.reports', icon: FileText },
  { to: '/settings', labelKey: 'nav.settings', icon: Settings },
]
