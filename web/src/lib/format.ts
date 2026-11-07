// Arabic text with Western digits, as shops in the region usually write prices.
const locale = (language: string) => (language === 'ar' ? 'ar-u-nu-latn' : 'en-GB')

const symbols: Record<string, string> = { USD: '$', EUR: '€', GBP: '£', ILS: '₪', TRY: '₺' }

// Built by hand: Intl writes "US$" in Arabic. The isolate marks keep "$45" or "45 JOD" in one piece inside Arabic text.
export function formatMoney(value: number, currency: string, language: string) {
  const amount = new Intl.NumberFormat(locale(language), { maximumFractionDigits: value % 1 === 0 ? 0 : 2 }).format(value)
  const symbol = symbols[currency]
  return `\u2066${symbol ? `${symbol}${amount}` : `${amount} ${currency}`}\u2069`
}

export function formatNumber(value: number | null | undefined, language: string, digits = 0) {
  if (value == null || Number.isNaN(value)) return '—'
  return new Intl.NumberFormat(locale(language), { maximumFractionDigits: digits }).format(value)
}

export function formatPercent(value: number | null | undefined, language: string, digits = 1) {
  if (value == null || Number.isNaN(value)) return '—'
  return new Intl.NumberFormat(locale(language), { style: 'percent', maximumFractionDigits: digits }).format(value)
}

export function formatDate(value: string | null | undefined, language: string, options?: Intl.DateTimeFormatOptions) {
  if (!value) return '—'
  return new Intl.DateTimeFormat(locale(language), options ?? { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(value))
}

export function formatDateTime(value: string | null | undefined, language: string, timeZone?: string) {
  if (!value) return '—'
  return new Intl.DateTimeFormat(locale(language), {
    day: 'numeric', month: 'short', hour: 'numeric', minute: '2-digit', timeZone,
  }).format(new Date(value))
}

export function formatRelative(value: string | null | undefined, language: string, now = Date.now()) {
  if (!value) return '—'
  const seconds = Math.round((new Date(value).getTime() - now) / 1000)
  const units: [Intl.RelativeTimeFormatUnit, number][] = [['year', 31536000], ['month', 2592000], ['week', 604800], ['day', 86400], ['hour', 3600], ['minute', 60]]
  const format = new Intl.RelativeTimeFormat(locale(language), { numeric: 'auto' })
  for (const [unit, size] of units) if (Math.abs(seconds) >= size) return format.format(Math.round(seconds / size), unit)
  return format.format(0, 'minute')
}

export function formatBytes(bytes: number | null | undefined, language: string) {
  if (bytes == null) return '—'
  const [value, unit] = bytes >= 1024 * 1024 ? [bytes / (1024 * 1024), 'megabyte'] : [bytes / 1024, 'kilobyte']
  return new Intl.NumberFormat(locale(language), { style: 'unit', unit, unitDisplay: 'short', maximumFractionDigits: 1 }).format(value)
}
