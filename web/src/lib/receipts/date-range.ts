// Local-calendar month bounds, formatted as the DateOnly strings the API expects.
// Built from the parts rather than toISOString(), which would shift across UTC midnight.
export function currentMonthRange(today: Date = new Date()): { from: string; to: string } {
  const year = today.getFullYear()
  const month = today.getMonth()
  const lastDay = new Date(year, month + 1, 0).getDate()
  return { from: formatDate(year, month, 1), to: formatDate(year, month, lastDay) }
}

function formatDate(year: number, month: number, day: number): string {
  return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}
