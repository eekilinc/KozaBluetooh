# KozaBluetooth

KozaBluetooth, Windows 10/11 uzerinde Bluetooth Classic (HC-05/HC-06) ve BLE cihazlariyla haberlesmek icin gelistirilmis masaustu terminal uygulamasidir.

Repository: https://github.com/eekilinc/KozaBluetooh

## Ozellikler
- Classic cihaz tarama (COM port ve eslesmis cihaz listesi)
- BLE cihaz tarama, servis/characteristic secimi
- Classic baglanti: COM veya dogrudan RFCOMM (SPP)
- Baglan / Baglantiyi Kes / Yeniden Baglan
- Komut gonderme: ASCII veya HEX, satir sonu secenekleri (NONE, LF, CR, CRLF)
- BLE yazma modu secimi: Auto / WriteWithoutResponse / WriteWithResponse
- Hazir komut butonlari: START, STOP, STATUS, RESET
- Profil kaydetme/uygulama/silme
- Makro kaydetme ve tek tikla coklu komut calistirma
- Zamanli (periyodik) komut gonderimi
- Canli veri akis ekrani (gelen/giden event bazli)
- Canli akis filtreleme (tip + anahtar kelime)
- Log ve hata kayit ekrani
- Canli akis ve log icin temizleme (Clear) butonlari
- Yeniden baglanmada retry/backoff politikasi
- Opsiyonel saglik telemetrisi (TX/RX/HATA/RECONNECT)
- CSV/TXT disa aktarma
- Turkce/Ing. dil secimi
- Simulasyon cihazi ile donanim olmadan test
- Durum cubugunda git branch/hash bilgisi

## Derleme ve Calistirma
```bash
dotnet restore
dotnet build
dotnet run --project src/BluetoothDeskApp/BluetoothDeskApp.csproj
```

## Test
```bash
dotnet test
```

## HC-05 / HC-06 Notlari
1. Once Windows Bluetooth ayarlarindan cihazi eslestirin.
2. Aygit Yoneticisi > Ports (COM & LPT) altindan COM portu kontrol edin.
3. Uygulamada ayni COM ve dogru baud degerini secin (genelde 9600).
4. COM yoksa uygulama eslesmis cihaz icin dogrudan RFCOMM baglantisini dener.

## BLE Notlari
1. BLE tara ile cihazi bulun.
2. Baglandiktan sonra servisleri ve characteristic'leri yukleyin.
3. Write/Notify characteristic secip uygulayin.

## GitHub Actions
- `Build Windows EXE` workflow'u, `main` branch'e push oldugunda sadece `KozaBluetooth.exe` artifact uretir.
- `Release on Tag` workflow'u, tag atildiginda (`v1.0.0` gibi) GitHub Release olusturur ve dogrudan calisan `KozaBluetooth.exe` ile `KozaBluetooth-portable.zip` dosyalarini ekler.

## Release Alma (Tag ile)
Asagidaki komutlar release tetikler:

```bash
git tag v1.0.0
git push origin v1.0.0
```

Ardindan GitHub Releases sayfasinda otomatik olusan surume dogrudan calisan EXE dosyasi eklenecektir.
