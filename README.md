# KozaBluetooth

Windows 10/11 desktop app for Bluetooth Classic (HC-05/HC-06 over COM or RFCOMM) and BLE sensors.

Repository: https://github.com/eekilinc/KozaBluetooh

## Features
- Scan Classic serial ports and BLE devices
- List paired Classic Bluetooth device names (best-effort)
- Classic connect supports both COM and direct RFCOMM (SPP)
- Connect/disconnect and reconnect
- Send free text commands
- Quick command buttons: START, STOP, STATUS, RESET
- Real-time data flow panel (incoming, outgoing, error)
- TX/RX/error log panel
- Turkish UI labels and status texts
- Runtime language switch (Turkish / English)
- Export live flow to CSV and logs to TXT
- Connection settings panel (COM, baud, BLE service/characteristic)
- Built-in simulator for testing without hardware
- Quick link button to Windows Bluetooth settings
- Git branch/hash shown in status bar
- GitHub Actions workflow publishes downloadable Windows EXE artifact

## Build and run
```bash
dotnet restore
dotnet build
dotnet run --project src/BluetoothDeskApp/BluetoothDeskApp.csproj
```

## HC-05 / HC-06 notes
1. Pair the module in Windows Bluetooth settings first.
2. Find assigned COM port in Device Manager -> Ports (COM & LPT).
3. Select the same COM port and baud rate in the app.

If no COM port is available, app will try direct RFCOMM connect for paired Classic devices.

## BLE notes
- Use scan to find device.
- Connect, then load services and characteristics.
- Select the read/write characteristic and apply selection.

## Tests
```bash
dotnet test
```

## Git setup
```bash
git init
git add .
git commit -m "Initial BluetoothDeskApp scaffold"
git branch -M main
git remote add origin https://github.com/<username>/<repo>.git
git push -u origin main
```
