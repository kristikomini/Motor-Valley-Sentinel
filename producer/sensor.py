"""
Sensor simulation for Motor-Valley Monitor.
Generates realistic machine data: Machine_ID, Temperature, RPM, Status_Code.
"""

import asyncio
import json
import random
from dataclasses import dataclass, asdict
from datetime import datetime
from enum import IntEnum


class StatusCode(IntEnum):
    """Machine status codes."""

    NORMAL = 0
    WARNING = 1
    CRITICAL = 2
    OFFLINE = 3
    MAINTENANCE = 4


@dataclass
class SensorReading:
    """A single sensor reading from a machine."""

    machine_id: str
    temperature: float
    rpm: int
    status_code: int
    timestamp: str

    def to_json(self) -> str:
        return json.dumps(asdict(self))


class Sensor:
    """Simulates a single machine sensor with realistic, evolving data."""

    def __init__(self, machine_id: str) -> None:
        self.machine_id = machine_id
        self._temperature = random.uniform(60.0, 85.0)
        self._rpm = random.randint(2000, 8000)
        self._status = StatusCode.NORMAL

    def _evolve(self) -> None:
        """Apply small random changes for realism."""
        self._temperature += random.uniform(-2.0, 2.0)
        self._temperature = max(40.0, min(110.0, self._temperature))

        self._rpm += random.randint(-100, 100)
        self._rpm = max(0, min(10000, self._rpm))

        if self._temperature > 95:
            self._status = StatusCode.CRITICAL
        elif self._temperature > 85:
            self._status = StatusCode.WARNING
        elif random.random() < 0.001:
            self._status = StatusCode.MAINTENANCE
        else:
            self._status = StatusCode.NORMAL

    def read(self) -> SensorReading:
        """Produce one reading."""
        self._evolve()
        return SensorReading(
            machine_id=self.machine_id,
            temperature=round(self._temperature, 2),
            rpm=self._rpm,
            status_code=int(self._status),
            timestamp=datetime.utcnow().isoformat() + "Z",
        )
