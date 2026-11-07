import { describe, expect, it } from 'vitest'
import { formatMoney, formatNumber, formatPercent } from './format'

const plain = (s: string) => s.replace(/[⁦⁩]/g, '')

describe('format', () => {
  it('writes prices with the symbol first and Western digits, in Arabic too', () => {
    expect(plain(formatMoney(45, 'USD', 'ar'))).toBe('$45')
    expect(plain(formatMoney(14.5, 'ILS', 'ar'))).toBe('₪14.5')
    expect(plain(formatMoney(3, 'JOD', 'en'))).toBe('3 JOD')
  })

  it('shows a dash for missing numbers', () => {
    expect(formatNumber(null, 'ar')).toBe('—')
    expect(formatPercent(undefined, 'en')).toBe('—')
  })

  it('uses Western digits for Arabic numbers', () => {
    expect(formatNumber(1234, 'ar')).toMatch(/1.234/)
  })
})
