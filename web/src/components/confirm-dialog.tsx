import { useTranslation } from 'react-i18next'
import { Button } from './ui/button'
import { Dialog } from './ui/dialog'
import { Spinner } from './ui/spinner'

type ConfirmDialogProps = {
  open: boolean
  title: string
  description?: string
  confirmLabel: string
  danger?: boolean
  busy?: boolean
  onConfirm: () => void
  onClose: () => void
}

export function ConfirmDialog({ open, title, description, confirmLabel, danger, busy, onConfirm, onClose }: ConfirmDialogProps) {
  const { t } = useTranslation()
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={title}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>{t('common.cancel')}</Button>
          <Button variant={danger ? 'danger' : 'primary'} onClick={onConfirm} disabled={busy}>
            {busy && <Spinner />}
            {confirmLabel}
          </Button>
        </>
      }
    >
      {description && <p className="text-muted">{description}</p>}
    </Dialog>
  )
}
