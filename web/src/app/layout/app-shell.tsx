import { Outlet } from 'react-router'
import { BottomNav } from './bottom-nav'
import { Sidebar } from './sidebar'
import { TopBar } from './top-bar'

export function AppShell() {
  return (
    <div className="flex min-h-dvh">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar />
        <main className="mx-auto w-full max-w-6xl flex-1 px-4 pt-6 pb-28 md:px-8 md:pb-10">
          <Outlet />
        </main>
      </div>
      <BottomNav />
    </div>
  )
}
