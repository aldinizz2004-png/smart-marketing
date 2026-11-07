// Values sent to the API stay in English; labels come from the translation files.
export const businessCategories = [
  'Cafe', 'Restaurant', 'Bakery', 'Sweets', 'Clothing', 'Beauty', 'Electronics', 'Pharmacy',
  'Supermarket', 'Flowers', 'Gym', 'Furniture', 'Services',
] as const

export const countries = ['PS', 'JO', 'EG', 'SA', 'AE', 'KW', 'QA', 'BH', 'OM', 'LB', 'SY', 'IQ', 'MA', 'TN', 'DZ', 'LY', 'TR', 'US', 'GB', 'DE'] as const

export const timeZones = [
  'Asia/Hebron', 'Asia/Gaza', 'Asia/Jerusalem', 'Asia/Amman', 'Africa/Cairo', 'Asia/Riyadh', 'Asia/Dubai', 'Asia/Kuwait',
  'Asia/Qatar', 'Asia/Bahrain', 'Asia/Muscat', 'Asia/Beirut', 'Asia/Damascus', 'Asia/Baghdad', 'Africa/Casablanca',
  'Africa/Tunis', 'Africa/Algiers', 'Africa/Tripoli', 'Europe/Istanbul', 'Europe/London', 'Europe/Berlin', 'America/New_York', 'UTC',
] as const

export const currencies = ['ILS', 'JOD', 'USD', 'EUR', 'EGP', 'SAR', 'AED', 'KWD', 'QAR', 'BHD', 'OMR', 'LBP', 'IQD', 'MAD', 'TND', 'TRY', 'GBP'] as const

export const dialects = ['Palestinian Arabic', 'Jordanian Arabic', 'Levantine Arabic', 'Egyptian Arabic', 'Gulf Arabic', 'Iraqi Arabic', 'Maghrebi Arabic', 'Modern Standard Arabic', 'English'] as const

export const tones = ['friendly', 'professional', 'playful', 'warm', 'luxurious', 'bold'] as const

export function browserTimeZone(): string {
  try {
    const zone = Intl.DateTimeFormat().resolvedOptions().timeZone
    return (timeZones as readonly string[]).includes(zone) ? zone : 'Asia/Hebron'
  } catch {
    return 'Asia/Hebron'
  }
}
