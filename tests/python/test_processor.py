"""Tests for processor temperature logic."""

import pytest
from processor import TemperatureProcessor, CRITICAL_THRESHOLD, CONSECUTIVE_COUNT


def test_no_alert_below_threshold() -> None:
    """Temperatures below 90°C should not produce alerts."""
    p = TemperatureProcessor()
    for _ in range(5):
        alert = p.process("M-01", 85.0)
        assert alert is None


def test_alert_after_consecutive_high() -> None:
    """Alert when T > 90 for 3+ consecutive readings."""
    p = TemperatureProcessor()
    alerts = []
    for _ in range(5):
        a = p.process("M-02", 92.0)
        if a:
            alerts.append(a)
    assert len(alerts) >= 1
    assert alerts[0].machine_id == "M-02"
    assert alerts[0].temperature == 92.0
    assert alerts[0].message == "CRITICAL_ALERT"


def test_reset_after_low_reading() -> None:
    """Consecutive count resets when temperature drops."""
    p = TemperatureProcessor()
    p.process("M-03", 91.0)
    p.process("M-03", 91.0)
    p.process("M-03", 80.0)  # reset
    alert = p.process("M-03", 91.0)  # only 1 high
    assert alert is None


def test_moving_average() -> None:
    """Moving average is computed over window."""
    p = TemperatureProcessor()
    for t in [70, 72, 74, 76, 78]:
        p.process("M-04", float(t))
    avg = p.moving_average("M-04")
    assert avg is not None
    assert 74 <= avg <= 76
