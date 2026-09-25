import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ApiError } from '@/lib/api/client'
import { updateLineItem, updateLineItemCategory } from '@/lib/api/receipts'
import type { Category, ReceiptLineItem } from '@/lib/api/types'
import { parseAmount } from '@/lib/receipts/parse-amount'

const SELECT_CLASS =
  'h-8 w-full rounded-lg border border-input bg-transparent px-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30'

interface LineItemRowProps {
  receiptId: string
  lineItem: ReceiptLineItem
  categories: Category[]
}

export function LineItemRow({ receiptId, lineItem, categories }: LineItemRowProps) {
  const queryClient = useQueryClient()
  const [isEditing, setIsEditing] = useState(false)
  const [description, setDescription] = useState(lineItem.description)
  const [amount, setAmount] = useState(lineItem.amount.toFixed(2))
  const [validationError, setValidationError] = useState<string | null>(null)

  // Both edits change what the detail and list views show (amounts feed the dominant category).
  function refreshReceipt() {
    void queryClient.invalidateQueries({ queryKey: ['receipt', receiptId] })
    void queryClient.invalidateQueries({ queryKey: ['receipts'] })
  }

  const saveMutation = useMutation({
    mutationFn: (values: { description: string; amount: number }) => updateLineItem(receiptId, lineItem.id, values),
    onSuccess: () => {
      setIsEditing(false)
      refreshReceipt()
    },
  })

  const categoryMutation = useMutation({
    mutationFn: (categoryId: string) => updateLineItemCategory(receiptId, lineItem.id, { categoryId }),
    onSuccess: refreshReceipt,
  })

  function startEditing() {
    setDescription(lineItem.description)
    setAmount(lineItem.amount.toFixed(2))
    setValidationError(null)
    saveMutation.reset()
    setIsEditing(true)
  }

  function handleSave() {
    const parsedAmount = parseAmount(amount)
    if (!description.trim()) {
      setValidationError('Description is required.')
    } else if (parsedAmount === null) {
      setValidationError('Enter a valid amount, e.g. 4.99.')
    } else {
      setValidationError(null)
      saveMutation.mutate({ description: description.trim(), amount: parsedAmount })
    }
  }

  const error =
    validationError ??
    [saveMutation.error, categoryMutation.error]
      .map((e) => (e instanceof ApiError ? e.message : e ? 'Something went wrong.' : null))
      .find(Boolean)

  return (
    <>
      <tr className="border-b last:border-0 align-top">
        <td className="px-4 py-2">
          {isEditing ? (
            <Input
              aria-label="Description"
              value={description}
              maxLength={200}
              onChange={(e) => setDescription(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleSave()}
              autoFocus
            />
          ) : (
            lineItem.description
          )}
        </td>
        <td className="px-4 py-2 text-right tabular-nums">
          {isEditing ? (
            <Input
              aria-label="Amount"
              inputMode="decimal"
              className="text-right"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleSave()}
            />
          ) : (
            `$${lineItem.amount.toFixed(2)}`
          )}
        </td>
        <td className="px-4 py-2">
          <select
            aria-label={`Category for ${lineItem.description}`}
            className={SELECT_CLASS}
            value={lineItem.categoryId ?? ''}
            disabled={categoryMutation.isPending}
            onChange={(e) => e.target.value && categoryMutation.mutate(e.target.value)}
          >
            {!lineItem.categoryId && <option value="">Uncategorized</option>}
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </td>
        <td className="px-4 py-2 text-right whitespace-nowrap">
          {isEditing ? (
            <div className="flex justify-end gap-2">
              <Button size="sm" onClick={handleSave} disabled={saveMutation.isPending}>
                {saveMutation.isPending ? 'Saving…' : 'Save'}
              </Button>
              <Button size="sm" variant="ghost" onClick={() => setIsEditing(false)} disabled={saveMutation.isPending}>
                Cancel
              </Button>
            </div>
          ) : (
            <Button size="sm" variant="ghost" onClick={startEditing}>
              Edit
            </Button>
          )}
        </td>
      </tr>
      {error && (
        <tr>
          <td colSpan={4} className="px-4 pb-2 text-sm text-destructive">
            {error}
          </td>
        </tr>
      )}
    </>
  )
}
