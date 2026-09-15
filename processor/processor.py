"""
Processor microservice for Motor-Valley Monitor.
Consumes sensor stream, calculates moving average, emits CRITICAL_ALERT when temperature > 90°C for 3+ consecutive readings.
"""

import asyncio
import json
import logging
import os
from collections import defaultdict
from dataclasses import dataclass, asdict
from datetime import datetime

from aiokafka import AIOKafkaConsumer, AIOKafkaProducer
from fastapi import FastAPI

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
)
logger = logging.getLogger(__name__)

KAFKA_BOOTSTRAP = os.getenv("KAFKA_BOOTSTRAP_SERVERS", "localhost:9093")
SENSOR_TOPIC = os.getenv("SENSOR_TOPIC", "sensor-data")
ALERTS_TOPIC = os.getenv("ALERTS_TOPIC", "critical-alerts")
WINDOW_SIZE = 5
CRITICAL_THRESHOLD = 90.0
CONSECUTIVE_COUNT = 3

app = FastAPI(title="Motor-Valley Processor")


@dataclass
class CriticalAlert:
    machine_id: str
    temperature: float
    consecutive_count: int
    message: str
    timestamp: str

    def to_json(self) -> str:
        return json.dumps(asdict(self))


class TemperatureProcessor:
    """Tracks moving average and consecutive high temps per machine."""

    def __init__(self) -> None:
        self._temps: dict[str, list[float]] = defaultdict(list)
        self._consecutive_high: dict[str, int] = defaultdict(int)

    def process(self, machine_id: str, temperature: float) -> CriticalAlert | None:
        self._temps[machine_id].append(temperature)
        if len(self._temps[machine_id]) > WINDOW_SIZE:
            self._temps[machine_id].pop(0)

        if temperature > CRITICAL_THRESHOLD:
            self._consecutive_high[machine_id] += 1
            if self._consecutive_high[machine_id] >= CONSECUTIVE_COUNT:
                alert = CriticalAlert(
                    machine_id=machine_id,
                    temperature=temperature,
                    consecutive_count=self._consecutive_high[machine_id],
                    message="CRITICAL_ALERT",
                    timestamp=datetime.utcnow().isoformat() + "Z",
                )
                self._consecutive_high[machine_id] = 0
                return alert
        else:
            self._consecutive_high[machine_id] = 0

        return None

    def moving_average(self, machine_id: str) -> float | None:
        vals = self._temps.get(machine_id)
        if not vals:
            return None
        return sum(vals) / len(vals)


processor = TemperatureProcessor()


async def consume_sensors(producer: AIOKafkaProducer) -> None:
    consumer = AIOKafkaConsumer(
        SENSOR_TOPIC,
        bootstrap_servers=KAFKA_BOOTSTRAP,
        group_id="processor-group",
        auto_offset_reset="earliest",
    )
    await consumer.start()
    try:
        async for msg in consumer:
            try:
                data = json.loads(msg.value.decode("utf-8"))
                mid = data["machine_id"]
                temp = data["temperature"]
                alert = processor.process(mid, temp)
                if alert:
                    await producer.send_and_wait(
                        ALERTS_TOPIC,
                        value=alert.to_json().encode("utf-8"),
                        key=mid.encode("utf-8"),
                    )
                    logger.info("CRITICAL_ALERT: %s T=%.1f°C", mid, temp)
            except Exception as e:
                logger.error("Process error: %s", e)
    finally:
        await consumer.stop()


@app.on_event("startup")
async def startup() -> None:
    producer = AIOKafkaProducer(bootstrap_servers=KAFKA_BOOTSTRAP)
    await producer.start()
    app.state.kafka_producer = producer
    asyncio.create_task(consume_sensors(producer))


@app.on_event("shutdown")
async def shutdown() -> None:
    if hasattr(app.state, "kafka_producer"):
        await app.state.kafka_producer.stop()


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


@app.get("/moving-average/{machine_id}")
def get_moving_average(machine_id: str) -> dict:
    avg = processor.moving_average(machine_id)
    return {"machine_id": machine_id, "moving_average": avg}
