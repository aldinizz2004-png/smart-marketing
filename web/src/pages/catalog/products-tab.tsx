import { Package, Pencil, Plus, Search, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { Pagination } from '@/components/pagination'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { SectionSpinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useDeleteProduct, useProducts, useSaveProduct, type Product } from '@/features/catalog/api'
import { ProductFormDialog } from '@/features/catalog/product-form-dialog'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { formatMoney } from '@/lib/format'

export function ProductsTab() {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId } = useWorkspace()
  const [params, setParams] = useSearchParams()
  const search = params.get('q') ?? ''
  const page = Number(params.get('page') ?? 1)
  const [draft, setDraft] = useState(search)
  const products = useProducts(businessId, { search, page, pageSize: 20 })
  const save = useSaveProduct(businessId)
  const remove = useDeleteProduct(businessId)
  const [editing, setEditing] = useState<Product | 'new' | null>(null)
  const [deleting, setDeleting] = useState<Product | null>(null)
  const canEdit = can(session.tenant.role, 'edit')

  const setParam = (key: string, value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next)
  }

  const toggleActive = (product: Product) =>
    save.mutate(
      {
        id: product.id,
        body: { name: product.name ?? '', sku: product.sku, description: product.description, price: product.price ?? 0, currency: product.currency, isActive: !product.isActive },
      },
      { onSuccess: () => toast.success(product.isActive ? t('catalog.hidden') : t('catalog.shown')), onError: (e) => toast.error(e) },
    )

  const items = products.data?.items ?? []

  return (
    <>
      <div className="mb-5 flex flex-wrap items-center gap-3">
        <form
          role="search"
          className="relative min-w-56 flex-1 sm:max-w-sm"
          onSubmit={(e) => {
            e.preventDefault()
            setParam('q', draft.trim())
          }}
        >
          <Search className="pointer-events-none absolute inset-y-0 start-3 my-auto size-4 text-muted" aria-hidden />
          <Input type="search" value={draft} onChange={(e) => setDraft(e.target.value)} placeholder={t('catalog.searchProducts')} aria-label={t('catalog.searchProducts')} className="ps-9" />
        </form>
        {canEdit && (
          <Button onClick={() => setEditing('new')}>
            <Plus aria-hidden />
            {t('catalog.newProduct')}
          </Button>
        )}
      </div>
      {products.isPending ? (
        <SectionSpinner />
      ) : products.isError ? (
        <ErrorState error={products.error} onRetry={() => void products.refetch()} />
      ) : items.length === 0 ? (
        <EmptyState icon={Package} title={search ? t('catalog.noProductsFound') : t('catalog.noProducts')} hint={search ? undefined : t('catalog.noProductsHint')} />
      ) : (
        <>
          <ul className={cn('grid gap-3 sm:grid-cols-2 xl:grid-cols-3', products.isFetching && 'opacity-70')}>
            {items.map((product) => (
              <li key={product.id} className={cn('flex flex-col rounded-2xl border border-line bg-surface p-5 shadow-sm', !product.isActive && 'opacity-70')}>
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <h3 className="font-bold [overflow-wrap:anywhere]">{product.name}</h3>
                    {product.sku && <p className="text-xs text-muted" dir="ltr">{product.sku}</p>}
                  </div>
                  <span className="shrink-0 text-lg font-extrabold text-brand-700 dark:text-brand-300">
                    {formatMoney(product.price ?? 0, product.currency ?? 'ILS', i18n.language)}
                  </span>
                </div>
                {product.description && <p className="mt-2 line-clamp-2 text-sm text-muted">{product.description}</p>}
                <div className="mt-auto flex flex-wrap items-center gap-2 pt-4">
                  <Badge tone={product.isActive ? 'green' : 'gray'}>{product.isActive ? t('catalog.active') : t('catalog.inactive')}</Badge>
                  {canEdit && (
                    <div className="ms-auto flex gap-1">
                      <Button size="sm" variant="ghost" onClick={() => toggleActive(product)} disabled={save.isPending}>
                        {product.isActive ? t('catalog.hide') : t('catalog.show')}
                      </Button>
                      <Button size="sm" variant="ghost" onClick={() => setEditing(product)} aria-label={`${t('common.edit')} ${product.name}`}>
                        <Pencil aria-hidden />
                      </Button>
                      <Button size="sm" variant="ghost" onClick={() => setDeleting(product)} aria-label={`${t('common.remove')} ${product.name}`} className="text-coral-500">
                        <Trash2 aria-hidden />
                      </Button>
                    </div>
                  )}
                </div>
              </li>
            ))}
          </ul>
          <Pagination page={page} totalPages={products.data.totalPages ?? 1} onChange={(p) => setParam('page', String(p))} />
        </>
      )}
      <ProductFormDialog open={editing !== null} onClose={() => setEditing(null)} product={editing === 'new' || editing === null ? undefined : editing} />
      <ConfirmDialog
        open={deleting !== null}
        title={t('catalog.deleteProduct', { name: deleting?.name })}
        description={t('catalog.deleteProductHint')}
        confirmLabel={t('common.remove')}
        danger
        busy={remove.isPending}
        onClose={() => setDeleting(null)}
        onConfirm={() =>
          remove.mutate(deleting?.id ?? '', {
            onSuccess: () => {
              toast.success(t('catalog.deleted'))
              setDeleting(null)
            },
            onError: (e) => {
              toast.error(e)
              setDeleting(null)
            },
          })
        }
      />
    </>
  )
}
