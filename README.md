# TerraTopia

Plugin TShock untuk server Terraria SMP. Isinya sistem ekonomi (ShiroCoin), shop resmi, rank dengan bonus hadiah, prefix chat berdasarkan rank, batas hadiah boss harian, dan player shop antar pemain.

> Dibuat untuk TShock 6.2.1 dengan .NET 9.

## Fitur

- **Economy**: saldo per akun, hadiah dari kill mob dan boss, transfer antar pemain.
- **Shop resmi**: beli item dengan ShiroCoin, hanya bisa dipakai di dalam region tertentu.
- **Rank**: naik level dengan membayar ShiroCoin. Setiap level menambah bonus hadiah mob.
- **Prefix chat**: nama rank tampil di depan nama pemain di chat.
- **Batas boss harian**: setiap rank punya batas jumlah hadiah boss per hari.
- **Hadiah mob khusus**: mob tertentu (berdasarkan ID) bisa memberi hadiah lebih besar.
- **Player shop (LPS)**: pemain bisa menjual item lewat listing. Item disimpan di server sampai terjual atau dibatalkan.

## Command

### Ekonomi

| Command | Fungsi |
|---|---|
| `/bal` | Lihat saldo kamu. |
| `/pay <nama> <jumlah>` | Kirim ShiroCoin ke pemain lain. |
| `/baltop` | Lihat 10 saldo tertinggi. |
| `/givebal <nama> <jumlah>` | Tambah saldo pemain. *(admin)* |
| `/takebal <nama> <jumlah>` | Kurangi saldo pemain. *(admin)* |
| `/setbal <nama> <jumlah>` | Atur saldo pemain. *(admin)* |

### Shop resmi

| Command | Fungsi |
|---|---|
| `/sb` atau `/shop` | Lihat daftar item shop. Harus di region `Shop`. |
| `/sb <id> <jumlah>` | Beli item dari shop. Harus di region `Shop`. |

### Rank

| Command | Fungsi |
|---|---|
| `/rank` | Lihat rank, level, bonus hadiah mob, dan biaya rankup berikutnya. |
| `/rankup` | Naik satu level dengan membayar ShiroCoin. |

### Player shop (LPS)

| Command | Fungsi |
|---|---|
| `/lps` | Lihat daftar listing aktif. |
| `/lpss <harga>` | Jual item yang sedang dipegang. Seluruh stack ikut dijual. |
| `/lpsb <nomor>` | Beli listing. |
| `/lpsc <nomor>` | Batalkan listing milik sendiri dan ambil item kembali. |
| `/lpsk` | Ambil item yang masuk klaim karena inventory penuh. |

Batas: 3 listing aktif per pemain.

## Cara kerja singkat

- **Kill mob**: dapat `MobReward` ShiroCoin, dikali bonus rank.
- **Kill boss**: dapat `BossReward`, dibatasi `DailyBossLimit` sesuai rank per hari (dihitung dari jam server). Boss tidak mendapat bonus rank.
- **Mob khusus**: kalau ID mob ada di `SpecialRewards`, hadiahnya pakai angka dari config. Mob biasa dapat bonus rank, boss tidak.
- **Player shop**: saat `/lpss`, item diambil dari tangan dan disimpan di database. Saat dibeli, koin pindah ke penjual dan item diberikan ke pembeli. Kalau inventory penuh, item masuk klaim dan diambil lewat `/lpsk`.

## Konfigurasi

File config dibuat otomatis di `tshock/TerraTopia/` saat server pertama kali jalan. Bisa diubah lewat panel, lalu restart server.

| File | Isi |
|---|---|
| `economy.json` | Nama mata uang, hadiah mob, hadiah boss, dan `SpecialRewards` (ID mob → hadiah). |
| `rank.json` | Tier rank, biaya dasar rankup, `GrowthPercent` (kenaikan biaya per level), `PercentPerLevel` (bonus hadiah per level), dan `DailyBossLimit` per tier. |
| `shop.json` | Daftar item shop resmi: ID, tipe item, nama, harga, dan maksimal pembelian. |
| `chat.json` | Format prefix chat, warna, dan on/off. |

Contoh isi `economy.json`:

```json
{
  "CurrencyName": "ShiroCoin",
  "MobReward": 10,
  "BossReward": 50,
  "SpecialRewards": {
    "4": 15,
    "50": 10,
    "85": 75
  }
}
```

Untuk mematikan satu fitur, cari flag `Enabled` di config-nya (ada di `rank.json` dan `chat.json`), ubah ke `false`, lalu restart.

## Instalasi

1. Pastikan server memakai **TShock 6.2.1** dengan **.NET 9**.
2. Unduh `TerraTopia.dll` dari halaman release, atau build sendiri (lihat bagian berikut).
3. Taruh `TerraTopia.dll` di folder `ServerPlugins`.
4. Restart server.
5. Cek log. Harus ada tulisan `[TerraTopia] Economy, Shop, Rank, Chat, dan Player Shop aktif.`

## Build sendiri

Plugin ini butuh DLL dari server kamu sendiri sebagai referensi saat build. DLL itu tidak disertakan di repo ini.

1. Buat folder `libs/` di root repo.
2. Salin dari folder server kamu: `TShockAPI.dll`, `TerrariaServer.dll`, dan `OTAPI.dll` (atau `OTAPI.zip` yang berisi `OTAPI.dll`).
3. Jalankan:
   ```bash
   dotnet build TerraTopia.csproj -c Release -o out
   ```
4. Hasilnya ada di `out/TerraTopia.dll`.

Di repo ini sudah ada workflow GitHub Actions (`main.yml`) yang melakukan build otomatis, asal file di `libs/` sudah tersedia.

## Database

Plugin menambahkan tabel ke database `tshock.sqlite`:

- `tt_balances`: saldo per akun.
- `tt_ranks`: level rank per akun.
- `tt_boss_daily`: jumlah hadiah boss yang sudah dipakai per akun per hari.
- `tt_lps_listings`: listing player shop.
- `tt_lps_claims`: item klaim yang belum diambil.

Backup `tshock.sqlite` secara berkala, terutama sebelum update.

## Catatan

- **ID mob** di `SpecialRewards` dan **ID item** di `shop.json` perlu dicek ulang di wiki Terraria sebelum dipakai, karena bisa berbeda antar versi.
- Prefix pada listing player shop saat ini ditampilkan sebagai angka, bukan nama.
- Prefix chat menggantikan format chat bawaan TShock untuk pemain yang sudah login.
- Plugin ini belum diuji di banyak server. Laporkan bug lewat Issues.

## Lisensi

Tentukan lisensi sebelum publik (misalnya MIT). Tambahkan file `LICENSE` ke repo.

## Kredit

- [TShock](https://github.com/Pryaxis/TShock) dan TerrariaServerAPI.
- Terraria oleh Re-Logic. Plugin ini tidak berafiliasi dengan Re-Logic.
