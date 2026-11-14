import { zodResolver } from '@hookform/resolvers/zod'
import { UserPlus } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ErrorState } from '@/components/error-state'
import { StatusBadge } from '@/components/status-badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Dialog } from '@/components/ui/dialog'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { assignableRoles, useAddMember, useMembers, useUpdateMember, type Member, type MembershipRole } from '@/features/settings/api'
import { formatDate } from '@/lib/format'

const schema = z.object({
  email: z.string().trim().min(1, 'validation.required').email('validation.email'),
  role: z.enum(['Admin', 'Editor', 'Reviewer', 'Viewer']),
})

type Values = z.infer<typeof schema>

export function TeamTab() {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const members = useMembers()
  const update = useUpdateMember()
  const [adding, setAdding] = useState(false)
  const canAdmin = can(session.tenant.role, 'admin')
  const isOwner = can(session.tenant.role, 'owner')

  const changeRole = (member: Member, role: MembershipRole) =>
    update.mutate({ userId: member.userId!, role }, { onSuccess: () => toast.success(t('settings.team.roleChanged')), onError: (e) => toast.error(e) })

  const toggleActive = (member: Member) => {
    const active = member.status?.code === 'Active'
    update.mutate(
      { userId: member.userId!, isActive: !active },
      { onSuccess: () => toast.success(active ? t('settings.team.deactivated') : t('settings.team.activated')), onError: (e) => toast.error(e) },
    )
  }

  return (
    <Card className="space-y-4 p-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-xl font-bold">{t('settings.team.title')}</h2>
          <p className="mt-1 text-sm text-muted">{canAdmin ? t('settings.team.subtitle') : t('settings.team.adminOnly')}</p>
        </div>
        {canAdmin && (
          <Button onClick={() => setAdding(true)}>
            <UserPlus aria-hidden />
            {t('settings.team.add')}
          </Button>
        )}
      </div>

      {members.isPending ? (
        <SectionSpinner />
      ) : members.isError ? (
        <ErrorState error={members.error} onRetry={() => void members.refetch()} />
      ) : (
        <ul className="divide-y divide-line rounded-2xl border border-line">
          {(members.data.items ?? []).map((member) => {
            const self = member.email === session.user.email
            const owner = member.role === 'Owner'
            const locked = owner || self || (!isOwner && member.role === 'Admin')
            const name = member.fullName || member.email
            return (
              <li key={member.userId} className="flex flex-wrap items-center gap-3 p-4">
                <div className="min-w-0 flex-1">
                  <p dir="auto" className="truncate font-semibold">{name}{self && <span className="ms-2 text-sm font-normal text-muted">({t('settings.team.you')})</span>}</p>
                  <p dir="ltr" className="truncate text-start text-sm text-muted">{member.email}</p>
                  {member.joinedAt && <p className="text-xs text-muted">{t('settings.team.joined', { date: formatDate(member.joinedAt, i18n.language) })}</p>}
                </div>
                <StatusBadge status={member.status} />
                {canAdmin && !locked ? (
                  <>
                    <Select
                      aria-label={t('settings.team.roleOf', { name })}
                      className="w-36"
                      value={member.role}
                      disabled={update.isPending}
                      onChange={(event) => changeRole(member, event.target.value as MembershipRole)}
                    >
                      {assignableRoles.map((r) => (
                        <option key={r} value={r}>{t(`roles.${r}`)}</option>
                      ))}
                    </Select>
                    <Button variant="ghost" size="sm" disabled={update.isPending} onClick={() => toggleActive(member)}>
                      {member.status?.code === 'Active' ? t('settings.team.deactivate') : t('settings.team.activate')}
                    </Button>
                  </>
                ) : (
                  <span className="text-sm font-semibold">{t(`roles.${member.role ?? 'Viewer'}`)}</span>
                )}
              </li>
            )
          })}
        </ul>
      )}

      <AddMemberDialog open={adding} onClose={() => setAdding(false)} />
    </Card>
  )
}

function AddMemberDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t } = useTranslation()
  const toast = useToast()
  const add = useAddMember()
  const { register, handleSubmit, reset, formState } = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { email: '', role: 'Editor' } })

  const close = () => {
    reset()
    onClose()
  }

  const submit = handleSubmit((v) =>
    add.mutateAsync(v).then(
      () => {
        toast.success(t('settings.team.added'))
        close()
      },
      (error) => toast.error(error),
    ),
  )

  return (
    <Dialog
      open={open}
      onClose={close}
      title={t('settings.team.add')}
      description={t('settings.team.addHint')}
      footer={
        <>
          <Button type="submit" form="add-member-form" disabled={add.isPending}>
            {add.isPending && <Spinner />}
            {t('settings.team.add')}
          </Button>
          <Button variant="ghost" onClick={close}>{t('common.cancel')}</Button>
        </>
      }
    >
      <form id="add-member-form" onSubmit={submit} className="space-y-4" noValidate>
        <Field label={t('settings.team.email')} error={formState.errors.email?.message}>
          <Input type="email" dir="ltr" autoComplete="off" {...register('email')} />
        </Field>
        <Field label={t('settings.team.role')} hint={t('settings.team.roleHint')}>
          <Select {...register('role')}>
            {assignableRoles.map((r) => (
              <option key={r} value={r}>{t(`roles.${r}`)}</option>
            ))}
          </Select>
        </Field>
      </form>
    </Dialog>
  )
}
