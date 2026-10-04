import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { logout } from '@/lib/api/auth'
import { useOfflineUploadQueue } from '@/lib/offline/use-offline-upload-queue'
import { useOnlineStatus } from '@/lib/offline/use-online-status'
import { useReviewQueue } from '@/lib/receipts/review-queue'

const navItems = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/receipts', label: 'Receipts' },
  { to: '/review', label: 'Review' },
  { to: '/merchants', label: 'Merchants' },
  { to: '/upload', label: 'Upload' },
  { to: '/categories', label: 'Categories' },
  { to: '/budgets', label: 'Budgets' },
]

export function AppLayout() {
  const navigate = useNavigate()
  // pageSize 1: only the total is needed for the badge.
  const reviewCount = useReviewQueue(1, 1).data?.totalCount ?? 0
  const isOnline = useOnlineStatus()
  const { pending, isFlushing } = useOfflineUploadQueue()

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex min-h-svh flex-col">
      <header className="border-b">
        <div className="mx-auto flex max-w-5xl items-center gap-8 px-4 py-3">
          <span className="font-semibold">ReceiptIQ</span>
          <nav className="flex flex-1 gap-4 text-sm">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  isActive
                    ? 'font-medium text-foreground'
                    : 'text-muted-foreground hover:text-foreground'
                }
              >
                {item.label}
                {item.to === '/review' && reviewCount > 0 && (
                  <span className="ml-1.5 rounded-full bg-amber-100 px-1.5 py-0.5 text-xs font-medium text-amber-800">
                    {reviewCount}
                  </span>
                )}
              </NavLink>
            ))}
          </nav>
          <Button variant="ghost" size="sm" onClick={handleLogout}>
            Log out
          </Button>
        </div>
      </header>
      {(!isOnline || pending.length > 0) && (
        <div className="border-b bg-amber-50 px-4 py-2 text-center text-sm text-amber-900">
          {!isOnline ? (
            <span>
              You're offline — uploads are saved on this device and will send automatically once you're back online.
            </span>
          ) : (
            <span>{isFlushing ? 'Sending' : 'Waiting to send'} {pending.length} queued upload{pending.length === 1 ? '' : 's'}…</span>
          )}
        </div>
      )}
      <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}
