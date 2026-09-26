import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { logout } from '@/lib/api/auth'
import { useReviewQueue } from '@/lib/receipts/review-queue'

const navItems = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/receipts', label: 'Receipts' },
  { to: '/review', label: 'Review' },
  { to: '/upload', label: 'Upload' },
  { to: '/categories', label: 'Categories' },
]

export function AppLayout() {
  const navigate = useNavigate()
  // pageSize 1: only the total is needed for the badge.
  const reviewCount = useReviewQueue(1, 1).data?.totalCount ?? 0

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
      <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}
