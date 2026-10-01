import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { createBudget, deleteBudget, listBudgets, updateBudget } from '@/lib/api/budgets'
import { listCategories } from '@/lib/api/categories'
import { ApiError } from '@/lib/api/client'
import type { Budget } from '@/lib/api/types'

const SELECT_CLASS =
  'h-8 w-full rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30'

const METER_FILL_CLASS: Record<Budget['status'], string> = {
  Ok: 'bg-[var(--viz-series-1)]',
  Warning: 'bg-amber-500',
  Exceeded: 'bg-destructive',
}

const STATUS_TEXT_CLASS: Record<Budget['status'], string> = {
  Ok: 'text-muted-foreground',
  Warning: 'text-amber-700',
  Exceeded: 'text-destructive',
}

function errorMessage(error: unknown): string | null {
  if (!error) {
    return null
  }
  return error instanceof ApiError ? error.message : 'Something went wrong.'
}

function BudgetMeter({ budget }: { budget: Budget }) {
  // The track always reads 0–100%; a budget blown past that still shows a full bar,
  // with the exact overage carried in the text underneath instead.
  const fillPercent = Math.min(budget.percentUsed, 100)

  return (
    <div>
      <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
        <div className={`h-full rounded-full ${METER_FILL_CLASS[budget.status]}`} style={{ width: `${fillPercent}%` }} />
      </div>
      <p className={`mt-1 text-xs ${STATUS_TEXT_CLASS[budget.status]}`}>
        ${budget.currentSpend.toFixed(2)} of ${budget.monthlyLimit.toFixed(2)} ({budget.percentUsed.toFixed(0)}%)
        {budget.status === 'Warning' && ' · approaching limit'}
        {budget.status === 'Exceeded' && ' · over budget'}
      </p>
    </div>
  )
}

function BudgetRow({ budget }: { budget: Budget }) {
  const queryClient = useQueryClient()
  const [isEditing, setIsEditing] = useState(false)
  const [limit, setLimit] = useState(budget.monthlyLimit.toFixed(2))

  function refresh() {
    void queryClient.invalidateQueries({ queryKey: ['budgets'] })
  }

  const updateMutation = useMutation({
    mutationFn: () => updateBudget(budget.id, { categoryId: budget.categoryId, monthlyLimit: Number(limit) }),
    onSuccess: () => {
      setIsEditing(false)
      refresh()
    },
  })
  const deleteMutation = useMutation({ mutationFn: () => deleteBudget(budget.id), onSuccess: refresh })

  function startEditing() {
    setLimit(budget.monthlyLimit.toFixed(2))
    updateMutation.reset()
    setIsEditing(true)
  }

  function handleDelete() {
    if (window.confirm(`Delete the budget for "${budget.categoryName}"?`)) {
      deleteMutation.mutate()
    }
  }

  const parsedLimit = Number(limit)
  const limitIsValid = limit.trim() !== '' && Number.isFinite(parsedLimit) && parsedLimit > 0
  const error = errorMessage(updateMutation.error) ?? errorMessage(deleteMutation.error)

  return (
    <li className="px-4 py-3 text-sm">
      <div className="flex items-center justify-between gap-4">
        <span className="font-medium">{budget.categoryName}</span>
        {isEditing ? (
          <div className="flex items-center gap-2">
            <Input
              aria-label={`Monthly limit for ${budget.categoryName}`}
              type="number"
              min="0.01"
              step="0.01"
              className="w-28"
              value={limit}
              onChange={(e) => setLimit(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && limitIsValid && updateMutation.mutate()}
            />
            <Button size="sm" onClick={() => updateMutation.mutate()} disabled={!limitIsValid || updateMutation.isPending}>
              {updateMutation.isPending ? 'Saving…' : 'Save'}
            </Button>
            <Button size="sm" variant="ghost" onClick={() => setIsEditing(false)}>
              Cancel
            </Button>
          </div>
        ) : (
          <div className="flex gap-2">
            <Button size="sm" variant="ghost" onClick={startEditing}>
              Edit
            </Button>
            <Button size="sm" variant="ghost" onClick={handleDelete} disabled={deleteMutation.isPending}>
              {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
            </Button>
          </div>
        )}
      </div>
      <div className="mt-2">
        <BudgetMeter budget={budget} />
      </div>
      {error && <p className="mt-1 text-destructive">{error}</p>}
    </li>
  )
}

export function BudgetsPage() {
  const queryClient = useQueryClient()
  const [categoryId, setCategoryId] = useState('')
  const [limit, setLimit] = useState('')

  const budgetsQuery = useQuery({ queryKey: ['budgets'], queryFn: listBudgets })
  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: listCategories })

  const budgetedCategoryIds = new Set(budgetsQuery.data?.map((b) => b.categoryId).filter((id): id is string => id !== null))
  const hasOverallBudget = budgetsQuery.data?.some((b) => b.categoryId === null) ?? false
  const availableCategories = categoriesQuery.data?.filter((c) => !budgetedCategoryIds.has(c.id)) ?? []

  const createMutation = useMutation({
    mutationFn: () => createBudget({ categoryId: categoryId || null, monthlyLimit: Number(limit) }),
    onSuccess: () => {
      setCategoryId('')
      setLimit('')
      void queryClient.invalidateQueries({ queryKey: ['budgets'] })
    },
  })

  function handleCreate(event: FormEvent) {
    event.preventDefault()
    const parsedLimit = Number(limit)
    if (Number.isFinite(parsedLimit) && parsedLimit > 0) {
      createMutation.mutate()
    }
  }

  const budgets = budgetsQuery.data ?? []
  const alertCount = budgets.filter((b) => b.status !== 'Ok').length

  return (
    <div className="max-w-xl">
      <h1 className="text-2xl font-semibold">Budgets</h1>
      <p className="mt-2 text-muted-foreground">
        Set a monthly limit per category, or one overall — tracked against this calendar month's confirmed, dated
        spend.
      </p>

      <form onSubmit={handleCreate} className="mt-6 flex gap-2">
        <select
          aria-label="Budget category"
          className={SELECT_CLASS}
          value={categoryId}
          disabled={hasOverallBudget && availableCategories.length === 0}
          onChange={(e) => setCategoryId(e.target.value)}
        >
          {!hasOverallBudget && <option value="">Overall</option>}
          {availableCategories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
        <Input
          aria-label="Monthly limit"
          type="number"
          min="0.01"
          step="0.01"
          placeholder="Monthly limit"
          value={limit}
          onChange={(e) => setLimit(e.target.value)}
        />
        <Button type="submit" disabled={!limit || Number(limit) <= 0 || createMutation.isPending}>
          {createMutation.isPending ? 'Adding…' : 'Add'}
        </Button>
      </form>
      {createMutation.isError && <p className="mt-2 text-sm text-destructive">{errorMessage(createMutation.error)}</p>}

      <div className="mt-8">
        {budgetsQuery.isPending && <p className="text-sm text-muted-foreground">Loading…</p>}
        {budgetsQuery.isError && <p className="text-sm text-destructive">Couldn’t load budgets.</p>}

        {budgetsQuery.data && budgets.length === 0 && (
          <p className="text-sm text-muted-foreground">No budgets yet — add one above.</p>
        )}

        {budgets.length > 0 && (
          <>
            {alertCount > 0 && (
              <p className="mb-3 text-sm text-amber-700">
                {alertCount} budget{alertCount === 1 ? '' : 's'} need{alertCount === 1 ? 's' : ''} attention this
                month.
              </p>
            )}
            <ul className="divide-y rounded-lg border">
              {budgets.map((budget) => (
                <BudgetRow key={budget.id} budget={budget} />
              ))}
            </ul>
          </>
        )}
      </div>
    </div>
  )
}
