# TerraTopia SMP Season 5

Plugin TShock untuk server Terraria Master Mode. Berisi sistem ekonomi (ShiroCoin), shop resmi, rank dengan bonus hadiah, prefix chat berdasarkan rank, player shop antar pemain, rumah pribadi dengan proteksi chest, teleport ke rumah dan lokasi mati, serta pembatasan boss.

> Dibuat untuk **TShock 6.2.1** dengan **.NET 9** dan **Terraria 1.4.5**.
> Versi plugin: **0.21.0**

## Status Fitur

| Fitur | Status |
|---|---|
| Economy (ShiroCoin) | ✅ Berjalan |
| Shop resmi (`/shop`) | ✅ Berjalan |
| Rank & bonus mob | ✅ Berjalan |
| Prefix chat rank | ✅ Berjalan |
| Batas hadiah boss harian | ✅ Berjalan |
| Hadiah mob khusus (`SpecialRewards`) | ✅ Berjalan |
| Anti farm patung (mob dari patung tidak dapat koin) | ✅ Berjalan |
| Player shop (`/lps`) | ✅ Berjalan |
| Rumah pribadi (`/house`) | ✅ Berjalan |
| Teleport ke rumah (`/tph`) | ✅ Berjalan |
| Proteksi chest rumah (hanya pemilik dan admin) | ✅ Berjalan |
| Penjaga spawn (mob hostile dihapus di region tertentu) | ✅ Berjalan |
| Pembatasan boss (boss tidak bisa muncul) | ✅ Berjalan |
| Teleport lokasi mati (`/tpd`) | ✅ Berjalan |
| Bantuan command (`/tthelp`) | ✅ Berjalan |
| Per-player loot chest | 🔌 Plugin pihak ketiga (lihat bagian Plugin Pendamping) |
| Crate dari memancing diganti ShiroCoin | 📋 Rencana |
| Anti-break chest dengan laporan dan peringatan | 📋 Rencana |

## Fitur

- **Economy**: saldo per akun, hadiah dari kill mob dan boss, transfer antar pemain.
- **Hadiah mob khusus**: mob tertentu (berdasarkan ID) punya hadiah sendiri, misalnya Mimic.
- **Anti farm patung**: mob yang muncul dari patung tidak memberi koin (`StatueMobReward` default 0).
- **Batas boss harian**: setiap rank punya batas jumlah hadiah boss per hari. Boss tidak mendapat bonus rank.
- **Pembatasan boss**: boss yang terdaftar tidak bisa muncul di server. Kalau ada yang mencoba memunculkannya, akan muncul peringatan dan boss dihapus.
- **Shop resmi**: beli item dengan ShiroCoin, hanya bisa dipakai di dalam region `Shop`.
- **Rank**: naik level dengan `/rankup`. Biaya dan bonus hadiah mob bisa diatur di config.
- **Prefix chat**: nama rank tampil di depan nama pemain saat chat.
- **Player shop (LPS)**: pemain menjual item lewat `/lpss`. Item disimpan di server sampai terjual atau dibatalkan. Kalau inventory penuh, item masuk klaim.
- **Rumah pribadi**: pemain membuat satu rumah dengan `/house`. Maksimal 200 tile ke samping dan 50 tile ke atas. Region rumah otomatis menjadi milik pemain.
- **Proteksi chest rumah**: chest di dalam rumah pemain lain tidak bisa dibuka. Pemilik rumah dan admin tetap bisa membukanya.
- **Kembali ke rumah**: `/tph` memindahkan pemain ke rumahnya, termasuk saat tidak bisa memakai `/sethome` bawaan.
- **Penjaga spawn**: mob hostile yang muncul di region tertentu (misalnya hub NPC dan shop) akan dihapus otomatis.
- **Teleport lokasi mati**: `/tpd` memindahkan pemain ke lokasi terakhir saat mati, dengan biaya 5 ShiroCoin.

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
| `/shop` atau `/sb` | Lihat daftar item. Harus di region `Shop`. |
| `/shop <nomor> <jumlah>` | Beli item. Harus di region `Shop`. |

### Rank

| Command | Fungsi |
|---|---|
| `/rank` | Lihat rank, level, bonus mob, dan biaya rankup berikutnya. |
| `/rankup` | Naik satu level dengan membayar ShiroCoin. |

### Player shop (LPS)

| Command | Fungsi |
|---|---|
| `/lps` | Lihat listing aktif. |
| `/lpss <harga>` | Jual item yang sedang dipegang. Seluruh stack ikut dijual. |
| `/lpsb <nomor>` | Beli listing. |
| `/lpsc <nomor>` | Batalkan listing milik sendiri dan ambil item kembali. |
| `/lpsk` | Ambil item yang masuk klaim karena inventory penuh. |

Batas: 3 listing aktif per pemain.

### Rumah

| Command | Fungsi |
|---|---|
| `/house set 1` | Tandai sudut kiri atas rumah dari posisi kamu berdiri. |
| `/house set 2` | Tandai sudut kanan bawah rumah dari posisi kamu berdiri. |
| `/house define` | Buat rumah dari dua sudut yang sudah ditandai. Rumah otomatis jadi milikmu. |
| `/house info` | Lihat region di tempat kamu berdiri dan pemiliknya. |
| `/house delete` | Hapus rumahmu untuk membuat yang baru. |
| `/tph` | Kembali ke rumahmu. |
| `/sethome` | Atur titik respawn di dalam region milikmu. |

Batas: 1 rumah per pemain, maksimal 200 x 50 tile.

### Lokasi mati

| Command | Fungsi |
|---|---|
| `/tpd` | Teleport ke lokasi terakhir saat mati. Biaya 5 ShiroCoin. |

### Lainnya

| Command | Fungsi |
|---|---|
| `/tthelp` | Tampilkan daftar command TerraTopia. |

## Cara Kerja

- **Kill mob biasa**: dapat `MobReward`, dikali bonus rank.
- **Kill boss**: dapat `BossReward`, dibatasi `DailyBossLimit` sesuai rank per hari. Hari dihitung dari jam server.
- **Mob khusus**: kalau ID mob ada di `SpecialRewards`, hadiahnya memakai angka dari config. Mob khusus biasa mendapat bonus rank, boss tidak.
- **Mob dari patung**: tidak memberi koin, kecuali `StatueMobReward` diubah.
- **Pembatasan boss**: boss di daftar `BlockedNpcIds` tidak bisa muncul. Pemanggilan yang dibatalkan memunculkan pesan peringatan untuk semua pemain, dengan jeda 30 detik per jenis boss.
- **Player shop**: saat `/lpss`, item diambil dari tangan dan disimpan di database. Saat dibeli, koin pindah ke penjual dan item diberikan ke pembeli. Kalau inventory penuh, item masuk klaim.
- **Rumah**: `/house define` membuat region dan menyimpan titik tengah rumah untuk `/tph`.
- **Proteksi chest**: saat chest dibuka, jika berada di dalam region `house_*` milik pemain lain, aksinya dibatalkan.
- **Penjaga spawn**: dicek sekitar setiap detik. Mob hostile di dalam region yang terdaftar langsung dihapus.
- **Lokasi mati**: setiap kali pemain berubah dari hidup ke mati, posisi tile-nya disimpan di database.

## Konfigurasi

File config dibuat otomatis di `tshock/TerraTopia/` saat server pertama kali jalan. Bisa diubah lewat panel, lalu restart server.

| File | Isi |
|---|---|
| `economy.json` | Nama mata uang, `MobReward`, `BossReward`, `StatueMobReward`, `ShowMobReward`, dan `SpecialRewards` (ID mob → hadiah). |
| `rank.json` | Tier rank, `BaseCost`, `GrowthPercent`, `PercentPerLevel`, dan `DailyBossLimit` per tier. |
| `shop.json` | Daftar item shop resmi: ID, tipe item, nama, harga, dan maksimal pembelian. |
| `chat.json` | Format prefix chat, warna, dan on/off. |
| `spawnguard.json` | `RegionNames` (region yang bebas mob) dan on/off. |
| `bossblock.json` | `BlockedNpcIds` (boss yang tidak bisa muncul), `Message`, dan on/off. |

Contoh `economy.json`:
```json
{
  "CurrencyName": "ShiroCoin",
  "MobReward": 10,
  "BossReward": 50,
  "StatueMobReward": 0,
  "ShowMobReward": false,
  "SpecialRewards": {
    "4": 15,
    "50": 10,
    "85": 75
  }
}
```

Untuk mematikan satu fitur, cari flag `Enabled` di config-nya (misalnya `rank.json`, `chat.json`, `spawnguard.json`, atau `bossblock.json`), ubah ke `false`, lalu restart.

## Plugin Pendamping

Beberapa fitur di server ini berasal dari plugin pihak ketiga yang dipasang terpisah. Plugin tersebut tidak disertakan di repo ini.

- **Bagger**: menyediakan command `/gb` untuk mengambil bag boss.
- **Auto Team**
- **FindPermission**
- **Per-player loot chest**: dari koleksi plugin TShock UnrealMultiple (PerPlayerLoot). Setiap pemain mendapat isi chest loot sendiri.
- **Lock Boss**: tidak lagi diperlukan untuk pembatasan boss, karena sudah ditangani `BossBlock`. Hanya dipasang kalau masih dibutuhkan untuk fitur lain.

## Instalasi

1. Pastikan server memakai **TShock 6.2.1** dengan **.NET 9**.
2. Unduh `TerraTopia.dll` dari halaman release, atau build sendiri (lihat bagian berikut).
3. Taruh `TerraTopia.dll` di folder `ServerPlugins`.
4. Restart server.
5. Cek log atau konsol. Harus ada tulisan `[TerraTopia] Semua modul aktif.`

## Build Sendiri

DLL dari server tidak disertakan di repo ini. Kamu perlu menyalinnya dari folder server kamu sendiri.

1. Buat folder `libs/` di root repo.
2. Salin dari folder server: `TShockAPI.dll`, `TerrariaServer.dll`, dan `OTAPI.dll` (atau `OTAPI.zip` yang berisi `OTAPI.dll`).
3. Build:
   ```bash
   dotnet build TerraTopia.csproj -c Release -o out
   ```
4. Hasilnya ada di `out/TerraTopia.dll`.

Repo ini juga punya workflow GitHub Actions (`main.yml`) yang build otomatis, asal file di `libs/` sudah tersedia.

## Struktur Folder

```
BossBlock/   Pembatasan boss (tidak bisa muncul)
Chat/        Prefix chat rank
Economy/     Saldo, kill reward, config
Help/        Command bantuan (tidak lagi dipanggil, bisa dihapus)
Home/        Penyimpanan rumah, /sethome, /tph
House/       Rumah pribadi (/house) dan proteksi chest
Lps/         Player shop
Rank/        Rank, rankup, batas boss harian
Shop/        Shop resmi
Spawn/       Penjaga spawn
Teleport/    /tpd dan pencatat lokasi mati
Plugin.cs    Titik masuk plugin dan registrasi command
```

## Database

Plugin menambahkan tabel ke `tshock.sqlite`:
- `tt_balances`: saldo per akun.
- `tt_ranks`: level rank per akun.
- `tt_boss_daily`: jumlah hadiah boss yang sudah dipakai per akun per hari.
- `tt_lps_listings`: listing player shop.
- `tt_lps_claims`: item klaim yang belum diambil.
- `tt_homes`: titik rumah per akun.
- `tt_last_death`: lokasi terakhir saat mati per akun.

Backup `tshock.sqlite` secara berkala, terutama sebelum update.

## Catatan

- **ID mob, boss, dan item** di config (`SpecialRewards`, `BlockedNpcIds`, `shop.json`) perlu dicek ulang di wiki Terraria sebelum dipakai, karena bisa berbeda antar versi.
- Prefix pada listing player shop ditampilkan sebagai angka, belum sebagai nama.
- Prefix chat menggantikan format chat bawaan TShock untuk pemain yang sudah login.
- Plugin ini belum diuji di banyak server. Laporkan bug lewat tab **Issues**.

## Lisensi

Belum ditentukan. Tambahkan file `LICENSE` sebelum repo dibuat publik (misalnya MIT).

## Kredit

- [TShock](https://github.com/Pryaxis/TShock) dan TerrariaServerAPI.
- Terraria oleh Re-Logic. Plugin ini tidak berafiliasi dengan Re-Logic.
- Per-player loot chest dari [UnrealMultiple/TShockPlugin](https://github.com/UnrealMultiple/TShockPlugin).
