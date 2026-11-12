export function initials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  const [first, second] = parts
  if (first && second) return (first[0]! + second[0]!).toUpperCase()
  return (first ?? '?').slice(0, 2).toUpperCase()
}
