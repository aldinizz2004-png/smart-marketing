import { ArrowDown, Bot, Cog, Database, FolderOpen, Globe, KeyRound, Layers, MessageCircle, Monitor, Send, type LucideIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Dialog } from '@/components/ui/dialog'

type Node = { key: string; icon: LucideIcon }

const layers: { key: string; tone: string; nodes: Node[] }[] = [
  { key: 'client', tone: 'border-grape-400/50 bg-grape-400/10', nodes: [{ key: 'spa', icon: Monitor }] },
  {
    key: 'api',
    tone: 'border-brand-400/60 bg-brand-500/10',
    nodes: [
      { key: 'auth', icon: KeyRound },
      { key: 'services', icon: Layers },
      { key: 'workers', icon: Cog },
    ],
  },
  {
    key: 'data',
    tone: 'border-sun-400/60 bg-sun-400/10',
    nodes: [
      { key: 'sql', icon: Database },
      { key: 'files', icon: FolderOpen },
    ],
  },
  {
    key: 'external',
    tone: 'border-coral-400/50 bg-coral-400/10',
    nodes: [
      { key: 'llm', icon: Bot },
      { key: 'social', icon: Send },
      { key: 'whatsapp', icon: MessageCircle },
      { key: 'competitors', icon: Globe },
    ],
  },
]

export function ArchitectureDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t } = useTranslation()
  return (
    <Dialog open={open} onClose={onClose} size="lg" title={t('landing.architecture.title')} description={t('landing.architecture.subtitle')}>
      <ol className="space-y-2" aria-label={t('landing.architecture.title')}>
        {layers.map((layer, index) => (
          <li key={layer.key}>
            {index > 0 && (
              <p className="flex items-center justify-center gap-2 py-1 text-xs font-semibold text-muted">
                <ArrowDown className="size-4" aria-hidden />
                {t(`landing.architecture.links.${layer.key}`)}
              </p>
            )}
            <section aria-labelledby={`layer-${layer.key}`} className={`rounded-2xl border p-4 ${layer.tone}`}>
              <h3 id={`layer-${layer.key}`} className="text-sm font-bold">{t(`landing.architecture.layers.${layer.key}`)}</h3>
              <ul className="mt-3 grid gap-2 sm:grid-cols-[repeat(auto-fit,minmax(10rem,1fr))]">
                {layer.nodes.map(({ key, icon: Icon }) => (
                  <li key={key} className="flex items-start gap-3 rounded-xl bg-surface p-3 shadow-sm">
                    <Icon className="mt-0.5 size-5 shrink-0 text-brand-600 dark:text-brand-400" aria-hidden />
                    <span>
                      <span className="block text-sm font-semibold">{t(`landing.architecture.nodes.${key}.title`)}</span>
                      <span className="block text-xs leading-relaxed text-muted">{t(`landing.architecture.nodes.${key}.body`)}</span>
                    </span>
                  </li>
                ))}
              </ul>
            </section>
          </li>
        ))}
      </ol>
    </Dialog>
  )
}
