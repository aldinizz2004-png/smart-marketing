import { House, MessageCircleHeart, Settings, Sparkles, type LucideIcon } from 'lucide-react'

export type NavItem = {
  to: string
  labelKey: string
  icon: LucideIcon
}

export const navItems: NavItem[] = [
  { to: '/', labelKey: 'nav.home', icon: House },
  { to: '/content', labelKey: 'nav.content', icon: Sparkles },
  { to: '/comments', labelKey: 'nav.comments', icon: MessageCircleHeart },
  { to: '/settings', labelKey: 'nav.settings', icon: Settings },
]
