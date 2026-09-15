import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts'

export interface TopFailingMachine {
  id: number
  machineId: string
  temperature: number
  consecutiveCount: number
  message: string
  timestamp: string
  alertCount: number
}

interface MachineHealthChartProps {
  data: TopFailingMachine[]
}

export function MachineHealthChart({ data }: MachineHealthChartProps) {
  if (data.length === 0) {
    return (
      <div
        style={{
          color: 'var(--muted)',
          padding: '2rem',
          textAlign: 'center',
          background: 'var(--surface)',
          border: '1px solid var(--border)',
          borderRadius: 12,
        }}
      >
        No failing machines data yet.
      </div>
    )
  }

  const chartData = data.map((d) => ({
    name: d.machineId,
    alerts: d.alertCount,
    temperature: d.temperature,
  }))

  return (
    <div
      style={{
        background: 'var(--surface)',
        border: '1px solid var(--border)',
        borderRadius: 12,
        padding: '1.5rem',
        height: 280,
      }}
    >
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={chartData} layout="vertical" margin={{ left: 20, right: 20 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
          <XAxis type="number" stroke="var(--muted)" fontSize={12} />
          <YAxis
            type="category"
            dataKey="name"
            width={90}
            stroke="var(--muted)"
            fontSize={11}
            tick={{ fontFamily: 'JetBrains Mono' }}
          />
          <Tooltip
            contentStyle={{ background: 'var(--surface)', border: '1px solid var(--border)' }}
            formatter={(value: number) => [value, 'Alerts']}
            labelFormatter={(label, payload) => {
              const p = payload?.[0]?.payload as { temperature?: number } | undefined
              return p ? `${label} (${p.temperature?.toFixed(1) ?? '?'}°C)` : label
            }}
          />
          <Bar dataKey="alerts" fill="var(--alert)" radius={[0, 4, 4, 0]} name="alerts" />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
