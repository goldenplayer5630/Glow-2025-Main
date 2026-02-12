#!/usr/bin/env python3
"""
Waveshare Modbus TCP Relay 16ch test (pymodbus).

- Tests channels 1..16 sequentially: ON -> delay -> OFF
- Optional: blink all relays together

Usage examples:
  python relay_test_16ch.py --host 192.168.1.200
  python relay_test_16ch.py --host 192.168.1.200 --unit 1
  python relay_test_16ch.py --host 192.168.1.200 --port 502 --delay 0.25 --loops 2
  python relay_test_16ch.py --host 192.168.1.200 --blink-all --blink-count 3

Notes:
- Channel 1 is usually coil address 0, channel 16 is coil address 15.
- Requires Modbus TCP enabled on the relay (port 502 typical).
"""

import argparse
import sys
import time

from pymodbus.client import ModbusTcpClient


def die(msg: str, code: int = 1) -> None:
    print(f"[ERROR] {msg}", file=sys.stderr)
    sys.exit(code)


def write_coil_checked(client: ModbusTcpClient, address: int, value: bool, unit: int) -> None:
    """Write a single coil and raise on Modbus exception / IO error."""
    rr = client.write_coil(address=address, value=value, slave=unit)
    # pymodbus returns a response object; isError() covers Modbus exceptions.
    if rr is None:
        die(f"No response writing coil {address}={'ON' if value else 'OFF'} (unit {unit}).")
    if rr.isError():
        die(f"Modbus error writing coil {address}={'ON' if value else 'OFF'} (unit {unit}): {rr!r}")


def read_coils_checked(client: ModbusTcpClient, start: int, count: int, unit: int) -> list[bool]:
    rr = client.read_coils(address=start, count=count, slave=unit)
    if rr is None:
        die(f"No response reading coils {start}..{start+count-1} (unit {unit}).")
    if rr.isError():
        die(f"Modbus error reading coils {start}..{start+count-1} (unit {unit}): {rr!r}")
    # rr.bits may include extra padding bits; slice to count.
    return list(rr.bits[:count])


def test_sequential(client: ModbusTcpClient, unit: int, delay: float, loops: int, verify: bool) -> None:
    for loop in range(1, loops + 1):
        print(f"\n=== Sequential test loop {loop}/{loops} ===")
        for ch in range(1, 17):
            coil = ch - 1  # channel 1 -> coil 0
            print(f"[CH {ch:02d}] ON  (coil {coil})")
            write_coil_checked(client, coil, True, unit)
            time.sleep(delay)

            if verify:
                bits = read_coils_checked(client, 0, 16, unit)
                state = bits[coil]
                print(f"         verify: {'ON' if state else 'OFF'}")
                if not state:
                    die(f"Verification failed: CH {ch} expected ON but read OFF.")

            print(f"[CH {ch:02d}] OFF (coil {coil})")
            write_coil_checked(client, coil, False, unit)
            time.sleep(delay)

            if verify:
                bits = read_coils_checked(client, 0, 16, unit)
                state = bits[coil]
                print(f"         verify: {'ON' if state else 'OFF'}")
                if state:
                    die(f"Verification failed: CH {ch} expected OFF but read ON.")


def blink_all(client: ModbusTcpClient, unit: int, delay: float, count: int, verify: bool) -> None:
    print(f"\n=== Blink all channels ({count}x) ===")
    for i in range(1, count + 1):
        print(f"[ALL] ON  ({i}/{count})")
        rr = client.write_coils(address=0, values=[True] * 16, slave=unit)
        if rr is None or rr.isError():
            die(f"Modbus error writing all ON: {rr!r}")
        time.sleep(delay)

        if verify:
            bits = read_coils_checked(client, 0, 16, unit)
            if not all(bits):
                die(f"Verification failed: expected all ON, got: {bits}")

        print(f"[ALL] OFF ({i}/{count})")
        rr = client.write_coils(address=0, values=[False] * 16, slave=unit)
        if rr is None or rr.isError():
            die(f"Modbus error writing all OFF: {rr!r}")
        time.sleep(delay)

        if verify:
            bits = read_coils_checked(client, 0, 16, unit)
            if any(bits):
                die(f"Verification failed: expected all OFF, got: {bits}")


def main() -> None:
    ap = argparse.ArgumentParser(description="Test 16ch Modbus TCP relay (pymodbus).")
    ap.add_argument("--host", required=True, help="Relay IP or hostname (e.g. 192.168.1.200)")
    ap.add_argument("--port", type=int, default=502, help="Modbus TCP port (default: 502)")
    ap.add_argument("--unit", type=int, default=1, help="Modbus unit/slave id (default: 1)")
    ap.add_argument("--delay", type=float, default=0.25, help="Delay between actions in seconds (default: 0.25)")
    ap.add_argument("--loops", type=int, default=1, help="How many sequential loops (default: 1)")
    ap.add_argument("--verify", action="store_true", help="Read back coils to verify state after writes")
    ap.add_argument("--blink-all", action="store_true", help="Also blink all channels together")
    ap.add_argument("--blink-count", type=int, default=3, help="How many all-on/all-off blinks (default: 3)")
    args = ap.parse_args()

    client = ModbusTcpClient(host=args.host, port=args.port, timeout=2.0, retries=1)

    print(f"Connecting to {args.host}:{args.port} (unit {args.unit}) ...")
    if not client.connect():
        die("Could not connect. Check IP/port, network, and that Modbus TCP is enabled (port 502).")

    try:
        # Optional initial read to confirm device responds.
        bits = read_coils_checked(client, 0, 16, args.unit)
        print(f"Connected. Current coil states (CH1..CH16): {['1' if b else '0' for b in bits]}")

        test_sequential(client, args.unit, args.delay, args.loops, args.verify)

        if args.blink_all:
            blink_all(client, args.unit, args.delay, args.blink_count, args.verify)

        print("\nDone ✅")
    finally:
        client.close()


if __name__ == "__main__":
    main()
