import { FeaturesSection } from './features-section'
import { LandingFooter } from './landing-footer'
import { LandingHeader, type LandingSection } from './landing-header'
import { HeroSection } from './hero-section'

const sections: LandingSection[] = [{ id: 'features', labelKey: 'landing.nav.features' }]

export function LandingPage() {
  return (
    <div className="min-h-dvh">
      <LandingHeader sections={sections} />
      <main>
        <HeroSection />
        <FeaturesSection />
      </main>
      <LandingFooter />
    </div>
  )
}
