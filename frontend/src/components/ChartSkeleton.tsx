export function ChartSkeleton() {
  return (
    <div
      style={{
        background: 'var(--surface)',
        border: '1px solid var(--border)',
        borderRadius: 12,
        padding: '1.5rem',
        height: 280,
        display: 'flex',
        flexDirection: 'column',
        gap: 12,
      }}
    >
      {[60, 80, 100, 120, 140].map((w, i) => (
        <div
          key={i}
          style={{
            width: `${w}%`,
            height: 24,
            background: 'var(--border)',
            borderRadius: 4,
            opacity: 0.6,
          }}
        />
      ))}
    </div>
  )
}
