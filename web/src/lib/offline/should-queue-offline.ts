import { ApiError } from '../api/client'

// Decides whether an upload failure means "the network is the problem, queue it for
// later" versus "the server rejected this specific request, show the error." A browser's
// fetch() throws a plain TypeError for network failures (no connection, DNS, CORS-blocked
// preflight) — it never reaches the server, so there's no ApiError and no status code.
// An ApiError means a response did come back, so retrying the same request would just
// fail the same way again; that's a real error, not an offline condition.
export function shouldQueueOffline(error: unknown): boolean {
  if (error instanceof ApiError) {
    return false
  }
  return error instanceof TypeError || !navigator.onLine
}
