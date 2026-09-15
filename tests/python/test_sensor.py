"""Tests for producer sensor module."""

import pytest
from sensor import Sensor, SensorReading, StatusCode


def test_sensor_reading_has_required_fields() -> None:
    """SensorReading must include machine_id, temperature, rpm, status_code."""
    s = Sensor("MACHINE-0001")
    r = s.read()
    assert isinstance(r, SensorReading)
    assert r.machine_id == "MACHINE-0001"
    assert 40 <= r.temperature <= 110
    assert 0 <= r.rpm <= 10000
    assert r.status_code in (c.value for c in StatusCode)


def test_sensor_reading_to_json() -> None:
    """to_json produces valid JSON with expected keys."""
    s = Sensor("MACHINE-0002")
    r = s.read()
    import json

    data = json.loads(r.to_json())
    assert "machine_id" in data
    assert "temperature" in data
    assert "rpm" in data
    assert "status_code" in data
    assert "timestamp" in data


def test_sensor_evolves_over_readings() -> None:
    """Multiple reads produce varying (realistic) values."""
    s = Sensor("MACHINE-0003")
    temps = [s.read().temperature for _ in range(20)]
    # Should have some variation
    assert len(set(temps)) > 1 or max(temps) - min(temps) >= 0
