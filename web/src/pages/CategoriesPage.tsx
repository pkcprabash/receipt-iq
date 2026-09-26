import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { createCategory, deleteCategory, listCategories, renameCategory } from '@/lib/api/categories'
import { ApiError } from '@/lib/api/client'
import type { Category } from '@/lib/api/types'

function errorMessage(error: unknown): string | null {
  if (!error) {
    return null
  }
  return error instanceof ApiError ? error.message : 'Something went wrong.'
}

function CategoryRow({ category }: { category: Category }) {
  const queryClient = useQueryClient()
  const [isEditing, setIsEditing] = useState(false)
  const [name, setName] = useState(category.name)

  // Deleting reassigns items to Uncategorized, so receipts refresh as well as the category list.
  function refresh() {
    void queryClient.invalidateQueries({ queryKey: ['categories'] })
    void queryClient.invalidateQueries({ queryKey: ['receipt'] })
    void queryClient.invalidateQueries({ queryKey: ['receipts'] })
  }

  const renameMutation = useMutation({
    mutationFn: () => renameCategory(category.id, { name }),
    onSuccess: () => {
      setIsEditing(false)
      refresh()
    },
  })
  const deleteMutation = useMutation({ mutationFn: () => deleteCategory(category.id), onSuccess: refresh })

  function startEditing() {
    setName(category.name)
    renameMutation.reset()
    setIsEditing(true)
  }

  function handleDelete() {
    if (window.confirm(`Delete “${category.name}”? Its line items will become Uncategorized.`)) {
      deleteMutation.mutate()
    }
  }

  const error = errorMessage(renameMutation.error) ?? errorMessage(deleteMutation.error)

  return (
    <li className="px-4 py-3 text-sm">
      <div className="flex items-center justify-between gap-4">
        {isEditing ? (
          <Input
            aria-label="Category name"
            value={name}
            maxLength={50}
            autoFocus
            onChange={(e) => setName(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && renameMutation.mutate()}
          />
        ) : (
          <span>{category.name}</span>
        )}

        {category.isSystem ? (
          <span className="rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">Built-in</span>
        ) : isEditing ? (
          <div className="flex gap-2">
            <Button size="sm" onClick={() => renameMutation.mutate()} disabled={renameMutation.isPending}>
              {renameMutation.isPending ? 'Saving…' : 'Save'}
            </Button>
            <Button size="sm" variant="ghost" onClick={() => setIsEditing(false)}>
              Cancel
            </Button>
          </div>
        ) : (
          <div className="flex gap-2">
            <Button size="sm" variant="ghost" onClick={startEditing}>
              Rename
            </Button>
            <Button size="sm" variant="ghost" onClick={handleDelete} disabled={deleteMutation.isPending}>
              {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
            </Button>
          </div>
        )}
      </div>
      {error && <p className="mt-1 text-destructive">{error}</p>}
    </li>
  )
}

export function CategoriesPage() {
  const queryClient = useQueryClient()
  const [newName, setNewName] = useState('')

  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: listCategories })

  const createMutation = useMutation({
    mutationFn: () => createCategory({ name: newName }),
    onSuccess: () => {
      setNewName('')
      void queryClient.invalidateQueries({ queryKey: ['categories'] })
    },
  })

  function handleCreate(event: FormEvent) {
    event.preventDefault()
    if (newName.trim()) {
      createMutation.mutate()
    }
  }

  const custom = categoriesQuery.data?.filter((c) => !c.isSystem) ?? []
  const builtIn = categoriesQuery.data?.filter((c) => c.isSystem) ?? []

  return (
    <div className="max-w-xl">
      <h1 className="text-2xl font-semibold">Categories</h1>
      <p className="mt-2 text-muted-foreground">
        Add your own categories to use on line items. Built-in categories can’t be changed.
      </p>

      <form onSubmit={handleCreate} className="mt-6 flex gap-2">
        <Input
          aria-label="New category name"
          placeholder="New category, e.g. Coffee"
          value={newName}
          maxLength={50}
          onChange={(e) => setNewName(e.target.value)}
        />
        <Button type="submit" disabled={!newName.trim() || createMutation.isPending}>
          {createMutation.isPending ? 'Adding…' : 'Add'}
        </Button>
      </form>
      {createMutation.isError && <p className="mt-2 text-sm text-destructive">{errorMessage(createMutation.error)}</p>}

      {categoriesQuery.isPending && <p className="mt-6 text-sm text-muted-foreground">Loading…</p>}
      {categoriesQuery.isError && (
        <p className="mt-6 text-sm text-destructive">Couldn’t load categories. Try again in a moment.</p>
      )}

      {categoriesQuery.data && (
        <>
          <h2 className="mt-8 mb-2 font-medium">Your categories</h2>
          {custom.length === 0 ? (
            <p className="text-sm text-muted-foreground">You haven’t added any yet.</p>
          ) : (
            <ul className="divide-y rounded-lg border">
              {custom.map((category) => (
                <CategoryRow key={category.id} category={category} />
              ))}
            </ul>
          )}

          <h2 className="mt-8 mb-2 font-medium">Built-in</h2>
          <ul className="divide-y rounded-lg border">
            {builtIn.map((category) => (
              <CategoryRow key={category.id} category={category} />
            ))}
          </ul>
        </>
      )}
    </div>
  )
}
