import { LandingFooter } from './landing-footer'
import { LandingHeader, type LandingSection } from './landing-header'
import { HeroSection } from './hero-section'

const sections: LandingSection[] = []

export function LandingPage() {
  return (
    <div className="min-h-dvh">
      <LandingHeader sections={sections} />
      <main>
        <HeroSection />
      </main>
      <LandingFooter />
    </div>
  )
}
