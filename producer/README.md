# Producer

Simulates 100+ factory floor machines (e.g., Ferrari engine dyno, packaging lines) streaming sensor data to Kafka.

## Features

- **Asyncio**: Concurrent simulation of 120 machines (configurable via `NUM_MACHINES`)
- **Realistic data**: Temperature, RPM, Status_Code with evolving values
- **Kafka**: Publishes to `sensor-data` topic

## Run

```bash
# Install deps
pip install -r requirements.txt

# Start (requires Kafka at localhost:9092)
python producer.py
```

## Env vars

- `KAFKA_BOOTSTRAP_SERVERS` – Kafka brokers (default: localhost:9092)
- `SENSOR_TOPIC` – Topic name (default: sensor-data)
- `NUM_MACHINES` – Number of simulated machines (default: 120)
- `INTERVAL_SECONDS` – Seconds between readings (default: 0.5)
