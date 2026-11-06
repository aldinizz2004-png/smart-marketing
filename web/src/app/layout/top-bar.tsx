import { Brand } from './brand'
import { LanguageToggle } from './language-toggle'
import { ThemeToggle } from './theme-toggle'
import { UserMenu } from './user-menu'

export function TopBar() {
  return (
    <header className="sticky top-0 z-20 flex items-center justify-between gap-3 border-b border-line bg-bg/85 px-4 py-2.5 backdrop-blur md:justify-end md:px-8">
      <div className="md:hidden">
        <Brand compact />
      </div>
      <div className="flex items-center gap-1">
        <LanguageToggle />
        <ThemeToggle />
        <UserMenu />
      </div>
    </header>
  )
}
