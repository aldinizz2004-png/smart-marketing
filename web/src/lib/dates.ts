// <input type="date"> and "datetime-local" work in the browser's local time; the API wants ISO with an offset.
export function toDateInput(value?: string | null) {
  if (!value) return ''
  const date = new Date(value)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

export function fromDateInput(value: string, endOfDay = false) {
  if (!value) return null
  const [y, m, d] = value.split('-').map(Number)
  const date = endOfDay ? new Date(y!, m! - 1, d!, 23, 59, 59) : new Date(y!, m! - 1, d!)
  return date.toISOString()
}

export function toDateTimeInput(value?: string | null) {
  if (!value) return ''
  const date = new Date(value)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${toDateInput(value)}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

export function fromDateTimeInput(value: string) {
  return value ? new Date(value).toISOString() : null
}
