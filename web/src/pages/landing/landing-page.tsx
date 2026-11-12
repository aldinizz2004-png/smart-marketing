import { Network } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { ArchitectureDialog } from './architecture-dialog'
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
  const { t } = useTranslation()
  const [architectureOpen, setArchitectureOpen] = useState(false)
  const architectureButton = (
    <Button variant="secondary" size="lg" onClick={() => setArchitectureOpen(true)}>
      <Network aria-hidden />
      {t('landing.architecture.open')}
    </Button>
  )

  return (
    <div className="min-h-dvh">
      <LandingHeader sections={sections} />
      <main>
        <HeroSection secondaryAction={architectureButton} />
        <FeaturesSection />
        <TechStackSection action={architectureButton} />
        <DemoRequestSection />
      </main>
      <LandingFooter />
      <ArchitectureDialog open={architectureOpen} onClose={() => setArchitectureOpen(false)} />
    </div>
  )
}
