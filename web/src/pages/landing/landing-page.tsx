import { DemoRequestSection } from './demo-request-section'
import { FeaturesSection } from './features-section'
import { LandingFooter } from './landing-footer'
import { LandingHeader, type LandingSection } from './landing-header'
import { HeroSection } from './hero-section'
import { TechStackSection } from './tech-stack-section'

const sections: LandingSection[] = [
  { id: 'features', labelKey: 'landing.nav.features' },
  { id: 'stack', labelKey: 'landing.nav.stack' },
  { id: 'demo', labelKey: 'landing.nav.demo' },
]

export function LandingPage() {
  return (
    <div className="min-h-dvh">
      <LandingHeader sections={sections} />
      <main>
        <HeroSection />
        <FeaturesSection />
        <TechStackSection />
        <DemoRequestSection />
      </main>
      <LandingFooter />
    </div>
  )
}
