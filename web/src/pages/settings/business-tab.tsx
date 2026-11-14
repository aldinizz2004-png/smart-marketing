import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, Trash2 } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ErrorState } from '@/components/error-state'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useAddBrandTerm, useBrandProfile, useBrandTerms, useDeleteBrandTerm, useSaveBrandProfile } from '@/features/brand/api'
import { BrandProfileForm } from '@/features/brand/brand-profile-form'
import { BusinessForm } from '@/features/business/business-form'
import { useUpdateBusiness } from '@/features/workspace/api'
import { useWorkspace } from '@/features/workspace/workspace-context'

const termSchema = z.object({
  term: z.string().trim().min(1, 'validation.required').max(100, 'validation.tooLong'),
  rule: z.enum(['Preferred', 'Prohibited']),
  note: z.string().trim().max(300, 'validation.tooLong'),
})

type TermValues = z.infer<typeof termSchema>

export function BusinessTab() {
  const { t } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { business, businessId } = useWorkspace()
  const update = useUpdateBusiness(businessId)
  const profile = useBrandProfile(businessId)
  const saveProfile = useSaveBrandProfile(businessId)
  const canAdmin = can(session.tenant.role, 'admin')
  const canEdit = can(session.tenant.role, 'edit')

  return (
    <div className="space-y-6">
      <Card className="space-y-4 p-6">
        <div>
          <h2 className="text-xl font-bold">{t('settings.business.title')}</h2>
          {!canAdmin && <p className="mt-1 text-sm text-muted">{t('settings.business.adminOnly')}</p>}
        </div>
        <BusinessForm
          key={businessId}
          business={business}
          submitLabel={t('common.save')}
          readOnly={!canAdmin}
          onSubmit={async (request) => {
            try {
              await update.mutateAsync(request)
              toast.success(t('common.saved'))
            } catch (error) {
              toast.error(error)
              throw error
            }
          }}
        />
      </Card>

      <Card className="space-y-4 p-6">
        <div>
          <h2 className="text-xl font-bold">{t('settings.brand.title')}</h2>
          <p className="mt-1 text-sm text-muted">{t('settings.brand.subtitle')}</p>
        </div>
        {profile.isPending ? (
          <SectionSpinner />
        ) : profile.isError ? (
          <ErrorState error={profile.error} onRetry={() => void profile.refetch()} />
        ) : (
          <BrandProfileForm
            key={`${businessId}-${profile.data?.updatedAt ?? 'new'}`}
            profile={profile.data}
            submitLabel={t('common.save')}
            readOnly={!canEdit}
            onSubmit={async (request) => {
              try {
                await saveProfile.mutateAsync(request)
                toast.success(t('common.saved'))
              } catch (error) {
                toast.error(error)
                throw error
              }
            }}
          />
        )}
      </Card>

      <BrandTerms businessId={businessId} canEdit={canEdit} />
    </div>
  )
}

function BrandTerms({ businessId, canEdit }: { businessId: string; canEdit: boolean }) {
  const { t } = useTranslation()
  const toast = useToast()
  const terms = useBrandTerms(businessId)
  const add = useAddBrandTerm(businessId)
  const remove = useDeleteBrandTerm(businessId)
  const { register, handleSubmit, reset, formState } = useForm<TermValues>({
    resolver: zodResolver(termSchema),
    defaultValues: { term: '', rule: 'Preferred', note: '' },
  })

  const submit = handleSubmit((v) =>
    add.mutateAsync({ term: v.term, rule: v.rule, note: v.note === '' ? null : v.note }).then(
      () => {
        toast.success(t('settings.terms.added'))
        reset()
      },
      (error) => toast.error(error),
    ),
  )

  const items = terms.data ?? []

  return (
    <Card className="space-y-4 p-6">
      <div>
        <h2 className="text-xl font-bold">{t('settings.terms.title')}</h2>
        <p className="mt-1 text-sm text-muted">{t('settings.terms.subtitle')}</p>
      </div>

      {terms.isPending ? (
        <SectionSpinner />
      ) : terms.isError ? (
        <ErrorState error={terms.error} onRetry={() => void terms.refetch()} />
      ) : items.length === 0 ? (
        <p className="text-sm text-muted">{t('settings.terms.none')}</p>
      ) : (
        <ul className="divide-y divide-line rounded-2xl border border-line">
          {items.map((term) => (
            <li key={term.id} className="flex flex-wrap items-center gap-3 p-3">
              <Badge tone={term.rule === 'Prohibited' ? 'coral' : 'green'}>{t(`settings.terms.rules.${term.rule ?? 'Preferred'}`)}</Badge>
              <div className="min-w-0 flex-1">
                <p dir="auto" className="font-semibold [overflow-wrap:anywhere]">{term.term}</p>
                {term.note && <p dir="auto" className="text-sm text-muted [overflow-wrap:anywhere]">{term.note}</p>}
              </div>
              {canEdit && (
                <Button
                  variant="ghost"
                  size="sm"
                  aria-label={t('settings.terms.deleteNamed', { term: term.term })}
                  disabled={remove.isPending}
                  onClick={() => remove.mutate(term.id!, { onError: (e) => toast.error(e) })}
                >
                  <Trash2 aria-hidden />
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}

      {canEdit && (
        <form onSubmit={submit} className="grid gap-4 sm:grid-cols-[1fr_10rem] " noValidate>
          <Field label={t('settings.terms.term')} error={formState.errors.term?.message}>
            <Input {...register('term')} />
          </Field>
          <Field label={t('settings.terms.rule')}>
            <Select {...register('rule')}>
              <option value="Preferred">{t('settings.terms.rules.Preferred')}</option>
              <option value="Prohibited">{t('settings.terms.rules.Prohibited')}</option>
            </Select>
          </Field>
          <div className="sm:col-span-2">
            <Field label={t('settings.terms.note')} optional error={formState.errors.note?.message}>
              <Input {...register('note')} />
            </Field>
          </div>
          <div className="sm:col-span-2">
            <Button type="submit" variant="secondary" disabled={add.isPending}>
              {add.isPending ? <Spinner /> : <Plus aria-hidden />}
              {t('settings.terms.add')}
            </Button>
          </div>
        </form>
      )}
    </Card>
  )
}
