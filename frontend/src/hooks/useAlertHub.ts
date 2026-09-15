import { useEffect, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import type { CriticalAlert } from '../App'

const HUB_URL = '/hubs/alerts'

export function useAlertHub() {
  const [alerts, setAlerts] = useState<CriticalAlert[]>([])
  const [connected, setConnected] = useState(false)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build()

    connection.on('ReceiveAlert', (alert: CriticalAlert) => {
      setAlerts((prev) => [alert, ...prev].slice(0, 50))
    })

    connection
      .start()
      .then(() => setConnected(true))
      .catch(console.error)

    return () => {
      connection.stop()
    }
  }, [])

  return { alerts, connected }
}
