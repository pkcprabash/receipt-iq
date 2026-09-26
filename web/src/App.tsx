import { Route, Routes } from 'react-router-dom'
import { RedirectIfAuthenticated } from './components/auth/RedirectIfAuthenticated'
import { RequireAuth } from './components/auth/RequireAuth'
import { AppLayout } from './components/layout/AppLayout'
import { DashboardPage } from './pages/DashboardPage'
import { LoginPage } from './pages/LoginPage'
import { CategoriesPage } from './pages/CategoriesPage'
import { ReceiptDetailPage } from './pages/ReceiptDetailPage'
import { ReceiptsPage } from './pages/ReceiptsPage'
import { RegisterPage } from './pages/RegisterPage'
import { ReviewQueuePage } from './pages/ReviewQueuePage'
import { UploadPage } from './pages/UploadPage'

function App() {
  return (
    <Routes>
      <Route
        path="/login"
        element={
          <RedirectIfAuthenticated>
            <LoginPage />
          </RedirectIfAuthenticated>
        }
      />
      <Route
        path="/register"
        element={
          <RedirectIfAuthenticated>
            <RegisterPage />
          </RedirectIfAuthenticated>
        }
      />
      <Route element={<RequireAuth />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/receipts" element={<ReceiptsPage />} />
          <Route path="/receipts/:id" element={<ReceiptDetailPage />} />
          <Route path="/review" element={<ReviewQueuePage />} />
          <Route path="/upload" element={<UploadPage />} />
          <Route path="/categories" element={<CategoriesPage />} />
        </Route>
      </Route>
    </Routes>
  )
}

export default App
