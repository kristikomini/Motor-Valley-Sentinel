# Processor

Microservice that consumes the sensor Kafka stream, computes a moving average of temperature, and pushes `CRITICAL_ALERT` to a second Kafka topic when temperature exceeds 90°C for 3+ consecutive readings.

## Logic

- Consumes from `sensor-data` topic
- Tracks moving average (window size 5) per machine
- Emits to `critical-alerts` topic when T > 90°C for 3 consecutive readings

## Run

```bash
# Local
pip install -r requirements.txt
uvicorn processor:app --reload --port 8000

# Docker
docker build -t motor-valley-processor .
docker run -p 8000:8000 -e KAFKA_BOOTSTRAP_SERVERS=host.docker.internal:9092 motor-valley-processor
```

## Endpoints

- `GET /health` – Health check
- `GET /moving-average/{machine_id}` – Current moving average for a machine
