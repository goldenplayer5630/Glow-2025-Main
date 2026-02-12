from pymodbus.client import ModbusTcpClient

RELAY_IP = "192.168.1.200"  # <-- your relay IP
PORT = 502
UNIT_ID = 2

client = ModbusTcpClient(RELAY_IP, port=PORT)
client.connect()

print("Relay ON (channel 1)")
client.write_coil(0, True, UNIT_ID)

input("Press ENTER to turn relay OFF...")

print("Relay OFF (channel 1)")
client.write_coil(0, False, UNIT_ID)

client.close()
print("Done.")
