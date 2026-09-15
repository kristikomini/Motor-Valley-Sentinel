"""
Kafka Producer for Motor-Valley Monitor.
Simulates 100+ machines concurrently using asyncio.
"""

import asyncio
import json
import logging
import os
from aiokafka import AIOKafkaProducer

from sensor import Sensor

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
)
logger = logging.getLogger(__name__)

KAFKA_BOOTSTRAP = os.getenv("KAFKA_BOOTSTRAP_SERVERS", "localhost:9093")
SENSOR_TOPIC = os.getenv("SENSOR_TOPIC", "sensor-data")
NUM_MACHINES = int(os.getenv("NUM_MACHINES", "120"))
INTERVAL_SECONDS = float(os.getenv("INTERVAL_SECONDS", "0.5"))


async def run_sensor(producer: AIOKafkaProducer, machine_id: str) -> None:
    """Run a single sensor, publishing readings to Kafka."""
    sensor = Sensor(machine_id)
    while True:
        try:
            reading = sensor.read()
            await producer.send_and_wait(
                SENSOR_TOPIC,
                value=reading.to_json().encode("utf-8"),
                key=machine_id.encode("utf-8"),
            )
            logger.debug(
                "Published %s: T=%.1f°C RPM=%d",
                machine_id,
                reading.temperature,
                reading.rpm,
            )
        except Exception as e:
            logger.error("Sensor %s error: %s", machine_id, e)
        await asyncio.sleep(INTERVAL_SECONDS)


async def main() -> None:
    producer = AIOKafkaProducer(bootstrap_servers=KAFKA_BOOTSTRAP)
    await producer.start()

    try:
        logger.info("Starting %d machine sensors on topic %s", NUM_MACHINES, SENSOR_TOPIC)
        tasks = [
            asyncio.create_task(run_sensor(producer, f"MACHINE-{i:04d}"))
            for i in range(1, NUM_MACHINES + 1)
        ]
        await asyncio.gather(*tasks)
    finally:
        await producer.stop()


if __name__ == "__main__":
    asyncio.run(main())
