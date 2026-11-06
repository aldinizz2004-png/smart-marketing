import type { Role } from './session'

// Mirrors the API policies: CanEdit, CanReview, TenantAdmin and TenantOwner.
const permissions = {
  edit: ['Owner', 'Admin', 'Editor'],
  review: ['Owner', 'Admin', 'Reviewer'],
  admin: ['Owner', 'Admin'],
  owner: ['Owner'],
} as const satisfies Record<string, readonly Role[]>

export type Permission = keyof typeof permissions

export function can(role: Role, permission: Permission): boolean {
  return (permissions[permission] as readonly Role[]).includes(role)
}
