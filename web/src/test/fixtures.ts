// Small API answers shared by the page tests.
export const activeSubscription = {
  id: 's1', plan: 'Pro', maxPostsPerDay: 10, duration: 'OneMonth', price: 45, currency: 'USD',
  startsAt: '2026-11-01T00:00:00Z', endsAt: '2026-12-01T00:00:00Z', status: { id: 302, code: 'Active', text: 'فعّال' }, grantsAccessNow: true,
}

export const tenantWithAccess = {
  id: 't1', name: 'Sara Cafe', myRole: 'Owner', status: { id: 101, code: 'Active', text: 'نشط' }, currentSubscription: activeSubscription,
}

export const tenantWithoutAccess = { ...tenantWithAccess, currentSubscription: null }

export const business = {
  id: 'b1', name: 'Qahwet Al-Hara', category: 'Cafe', timeZoneId: 'Asia/Hebron', defaultCurrency: 'ILS',
  status: { id: 402, code: 'Active', text: 'نشط' },
}

export function paged<T>(items: T[]) {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1, hasNext: false, hasPrevious: false }
}

export const plans = [
  { plan: 'Basic', maxPostsPerDay: 3, currency: 'USD', prices: { OneMonth: 20, ThreeMonths: 54, OneYear: 192 } },
  { plan: 'Pro', maxPostsPerDay: 10, currency: 'USD', prices: { OneMonth: 45, ThreeMonths: 120, OneYear: 432 } },
  { plan: 'Business', maxPostsPerDay: 25, currency: 'USD', prices: { OneMonth: 90, ThreeMonths: 240, OneYear: 864 } },
]

export const platforms = [
  { code: 'instagram', displayName: 'إنستغرام', supportsPublishing: true, supportsMessaging: true, supportsCompetitorTracking: true },
  { code: 'facebook', displayName: 'فيسبوك', supportsPublishing: true, supportsMessaging: true, supportsCompetitorTracking: true },
  { code: 'website', displayName: 'موقع إلكتروني', supportsPublishing: false, supportsMessaging: false, supportsCompetitorTracking: true },
]

// Routes every signed-in page needs: tenant, businesses, health and platform names.
export function workspaceRoutes() {
  return [
    { path: '/api/tenant', body: tenantWithAccess },
    { path: '/api/businesses', body: paged([business]) },
    { path: '/health' },
    { path: '/api/platforms', body: platforms },
  ]
}
