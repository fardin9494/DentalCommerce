import { isRouteErrorResponse, useRouteError } from 'react-router-dom'

export function RouteError() {
  const err = useRouteError() as any
  let title = 'Unexpected Error'
  let message = 'Something went wrong.'

  if (isRouteErrorResponse(err)) {
    title = `${err.status} ${err.statusText}`
    try { message = err.data?.message || err.data || message } catch {}
  } else if (err instanceof Error) {
    title = err.name || title
    message = err.message || message
  }

  return (
    <div className="container-std py-10">
      <div className="max-w-lg mx-auto card p-6 space-y-2">
        <h1 className="text-lg font-semibold">{title}</h1>
        <p className="text-sm text-gray-600">{message}</p>
      </div>
    </div>
  )
}


