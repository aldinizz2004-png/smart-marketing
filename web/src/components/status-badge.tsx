import type { components } from '@/api/schema'
import { Badge, type BadgeTone } from './ui/badge'

type StatusInfo = components['schemas']['StatusInfo']

const tones: Record<string, BadgeTone> = {
  Active: 'green', Approved: 'green', Published: 'green', Succeeded: 'green', Connected: 'green', Replied: 'green',
  Processed: 'green', Completed: 'green', Applied: 'green', Accepted: 'green',
  Pending: 'sun', InReview: 'sun', Queued: 'sun', Running: 'sun', Planning: 'sun', Retry: 'sun', WaitingForOwner: 'sun',
  New: 'sun', Scheduled: 'grape', Ready: 'grape', Analyzed: 'grape', Open: 'grape', Invited: 'grape',
  Draft: 'gray', Paused: 'gray', Ignored: 'gray', Closed: 'gray', Ended: 'gray', Disabled: 'gray',
  Failed: 'coral', Rejected: 'coral', Canceled: 'coral', Expired: 'coral', Disconnected: 'coral', Suspended: 'coral',
  ChangesRequested: 'sun',
}

// The API translates status texts itself (Accept-Language); only the color is chosen here.
export function StatusBadge({ status }: { status?: StatusInfo | null }) {
  if (!status) return null
  return <Badge tone={tones[status.code ?? ''] ?? 'gray'}>{status.text ?? status.code}</Badge>
}
