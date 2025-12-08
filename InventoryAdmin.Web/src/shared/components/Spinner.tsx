interface SpinnerProps {
  className?: string
}

export function Spinner({ className = 'h-5 w-5' }: SpinnerProps) {
  return (
    <div className={`animate-spin rounded-full border-2 border-current border-t-transparent ${className}`} />
  )
}
