# RateCon API Documentation

## Mundarija

- [Auth — Autentifikatsiya](#auth--autentifikatsiya)
- [Users — Foydalanuvchilar](#users--foydalanuvchilar)
- [Company — Kompaniya](#company--kompaniya)
- [SuperAdmin — Super admin](#superadmin--super-admin)
- [Groups — Haydovchi guruhlari](#groups--haydovchi-guruhlari)
- [Loads — Yuklar](#loads--yuklar)
- [ELD — Haydovchi lokatsiya](#eld--haydovchi-lokatsiya)
- [Motive Dispatch — Yuklar integratsiyasi](#motive-dispatch--yuklar-integratsiyasi)
- [Telegram — Xabar yuborish](#telegram--xabar-yuborish)
- [Trucks & Map — Mashina va xarita](#trucks--map--mashina-va-xarita)
- [ETA — Yetib borish vaqti](#eta--yetib-borish-vaqti)
- [Files & POD — Fayllar](#files--pod--fayllar)
- [Invoice — Hisob-faktura](#invoice--hisob-faktura)
- [Extract — PDF dan yuk olish](#extract--pdf-dan-yuk-olish)
- [Settings — Sozlamalar](#settings--sozlamalar)
- [Analytics & KPI — Tahlil](#analytics--kpi--tahlil)
- [Pay Tiers — To'lov stavkalari](#pay-tiers--tolov-stavkalari)
- [Earnings — Daromad](#earnings--daromad)
- [Alerts — Ogohlantirishlar](#alerts--ogohlantirishlar)
- [Audit — Jurnal](#audit--jurnal)
- [Motive API — Tashqi API](#motive-api--tashqi-api)

---

## Auth — Autentifikatsiya

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `POST` | `/api/register` | Yangi foydalanuvchi ro'yxatdan o'tkazish. `{name, email, password}` yuboriladi, `{token, name, role}` qaytadi |
| `POST` | `/api/login` | Tizimga kirish. `{email, password}` yuboriladi, `{token, name, role}` qaytadi |
| `POST` | `/api/change-password` | Parolni o'zgartirish. `{current_password, new_password}` kerak |
| `GET` | `/api/me` | Joriy foydalanuvchi ma'lumotlari. Token orqali kim ekanini aniqlash |

---

## Users — Foydalanuvchilar

### Company admin boshqaradi:

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/company/users` | Kompaniya foydalanuvchilari ro'yxati |
| `POST` | `/api/company/users` | Yangi dispetcher/foydalanuvchi yaratish. `{name, email, password}` |
| `PATCH` | `/api/company/users/{id}` | Foydalanuvchi ma'lumotlarini tahrirlash (ism, rol) |
| `POST` | `/api/company/users/{id}/reset-password` | Foydalanuvchi parolini tiklash |
| `DELETE` | `/api/company/users/{id}` | Foydalanuvchini o'chirish |

### Admin panel (eski):

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/admin/dispatchers` | Barcha dispetcherlar ro'yxati |
| `PATCH` | `/api/admin/dispatchers/{id}` | Dispetcher ma'lumotlarini tahrirlash |
| `DELETE` | `/api/admin/dispatchers/{id}` | Dispetcherni o'chirish |
| `POST` | `/api/admin/dispatchers/{id}/reset-password` | Dispetcher parolini tiklash |

---

## Company — Kompaniya

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/company` | Kompaniya ma'lumotlari (nomi, manzil, telefon, MC/DOT, bank rekvizitlari) |
| `POST` | `/api/company` | Kompaniya ma'lumotlarini saqlash/yangilash |

---

## SuperAdmin — Super admin

Butun tizimni boshqarish (barcha kompaniyalar ustidan nazorat):

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/superadmin/stats` | Umumiy statistika (kompaniyalar soni, foydalanuvchilar, yuklar) |
| `GET` | `/api/superadmin/companies` | Barcha kompaniyalar ro'yxati |
| `POST` | `/api/superadmin/companies` | Yangi kompaniya yaratish |
| `PATCH` | `/api/superadmin/companies/{id}` | Kompaniya nomini o'zgartirish |
| `DELETE` | `/api/superadmin/companies/{id}` | Kompaniyani o'chirish (barcha ma'lumotlar bilan) |
| `POST` | `/api/superadmin/companies/{id}/assign-admin` | Kompaniyaga admin tayinlash |
| `GET` | `/api/superadmin/companies/{id}/users` | Kompaniya foydalanuvchilari |
| `POST` | `/api/superadmin/companies/{id}/users` | Kompaniyaga foydalanuvchi qo'shish |
| `PATCH` | `/api/superadmin/users/{id}` | Foydalanuvchini tahrirlash |
| `POST` | `/api/superadmin/users/{id}/reset-password` | Parol tiklash |
| `DELETE` | `/api/superadmin/users/{id}` | Foydalanuvchini o'chirish |

---

## Groups — Haydovchi guruhlari

Haydovchilarni guruhlash (truck + driver = group):

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/groups` | Barcha guruhlar ro'yxati (haydovchi ismi, ELD driver ID) |
| `PATCH` | `/api/groups/{id}` | Guruh nomini o'zgartirish |
| `DELETE` | `/api/groups/{id}` | Guruhni o'chirish |
| `PATCH` | `/api/groups/{id}/driver` | Guruhga ELD haydovchi tayinlash. `{eld_driver_id, eld_driver_name}` |

---

## Loads — Yuklar

Asosiy yuk boshqarish (dispatch):

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `POST` | `/api/loads` | Yangi yuk yaratish. Avtomatik deadhead miles hisoblanadi |
| `GET` | `/api/loads` | Kompaniyaning barcha yuklari ro'yxati |
| `PUT` | `/api/loads/{id}` | Yuk ma'lumotlarini tahrirlash (manzil, sana, guruh, narx) |
| `PATCH` | `/api/loads/{id}/status` | Yuk statusini o'zgartirish: `upcoming` → `dispatched` → `delivered` |
| `POST` | `/api/loads/{id}/next-stop` | Keyingi stopga o'tish (current_stop_index +1) |
| `DELETE` | `/api/loads/{id}` | Yukni o'chirish |
| `POST` | `/api/loads/{id}/eta` | ETA hisoblash (Google Maps orqali haydovchi → keyingi stop) |
| `POST` | `/api/loads/{id}/send-status` | Telegram guruhga status xabar yuborish (joylashuv, masofa, tezlik) |
| `PATCH` | `/api/loads/{id}/auto-send` | Avtomatik status yuborish intervali. `{hours: 2}` = har 2 soatda |

### Load body maydonlari:

```json
{
  "group_id": 1,
  "load_number": "LD-1234",
  "origin_state": "TX",
  "destination_state": "CA",
  "total_rate_usd": "3500",
  "miles": "1200",
  "pickup_address": "123 Main St, Dallas, TX",
  "pickup_date": "2026-04-17",
  "delivery_address": "456 Oak Ave, Los Angeles, CA",
  "delivery_date": "2026-04-19",
  "stops_json": "[{\"type\":\"PU\",\"address\":\"...\"}]",
  "broker_name": "CH Robinson",
  "charge": "100"
}
```

---

## ELD — Haydovchi lokatsiya

ELD (Electronic Logging Device) provayderlar bilan ishlash:

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/eld/config` | Saqlangan ELD sozlamalar (API kalit masklangan) |
| `POST` | `/api/eld/config` | ELD sozlama qo'shish. `{provider, api_key, company?, provider_token?}` |
| `DELETE` | `/api/eld/config?config_id=X` | ELD sozlamani o'chirish |
| `GET` | `/api/eld/drivers` | Barcha ELD provayderlardan haydovchilar ro'yxati |

### Qo'llab-quvvatlanadigan provayderlar:

| Provider | `provider` qiymati | Kerak maydonlar |
|----------|-------------------|-----------------|
| Motive | `motive` | `api_key` |
| Samsara | `samsara` | `api_key` |
| ZippyELD | `zippyeld` | `api_key`, `provider_token`, `company` (USDOT) |
| EVO ELD | `evoeld` | `api_key`, `provider_token`, `company` (USDOT) |

---

## Motive Dispatch — Yuklar integratsiyasi

Motive bilan yuklar import/push/sync:

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/motive/dispatches` | Motive'dan dispatchlar ro'yxatini olish. `?status=active&page=1` filter |
| `POST` | `/api/motive/dispatches/{id}/import` | Motive'dan bitta dispatchni RateCon'ga import qilish. Dublikat tekshiradi |
| `POST` | `/api/loads/{id}/motive/push` | RateCon yukini Motive'ga yuborish. `{vehicle_id}` kerak. Haydovchi ilovada ko'radi |
| `POST` | `/api/loads/{id}/motive/sync-status` | Status sinxronlash. `{direction: "push"}` yoki `{direction: "pull"}` |

### Push jarayoni:
1. Yukga haydovchi tayinlangan bo'lishi kerak (`eld_driver_id`)
2. `vehicle_id` body'da yuboriladi
3. Motive'da dispatch yaratiladi → `motive_dispatch_id` saqlanadi
4. Haydovchi Motive ilovasida yukni ko'radi

### Import jarayoni:
1. `GET /api/motive/dispatches` — ro'yxatdan kerakli dispatch tanlash
2. `POST /api/motive/dispatches/{id}/import` — RateCon'ga yuk sifatida import
3. Dublikat bo'lsa 409 qaytadi

### Sync jarayoni:
- **push** — RateCon status → Motive status (upcoming→planned, dispatched→active, delivered→completed)
- **pull** — Motive status → RateCon status + stop progress yangilanadi

---

## Telegram — Xabar yuborish

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `POST` | `/api/send` | Telegram guruhga ratecon xabar yuborish (yuk ma'lumotlari, haydovchi, narx) |
| `GET` | `/api/bot-info` | Telegram bot ma'lumotlari (nomi, username) |

---

## Trucks & Map — Mashina va xarita

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/trucks` | Barcha ELD provayderlardan mashinalar ro'yxati (lokatsiya, haydovchi, tezlik) |
| `GET` | `/api/map/locations` | Xarita uchun barcha mashina koordinatalari (real-time tracking) |

---

## ETA — Yetib borish vaqti

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/eta` | Haydovchining keyingi stopgacha ETA (Google Maps Distance Matrix) |
| `POST` | `/api/loads/{id}/eta` | Yuk uchun ETA hisoblash va saqlash |

---

## Files & POD — Fayllar

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `POST` | `/api/loads/{id}/file` | Yuk hujjatini yuklash (ratecon PDF) |
| `GET` | `/api/loads/{id}/file` | Yuk hujjatini yuklab olish |
| `POST` | `/api/loads/{id}/pod` | POD (Proof of Delivery) yuklash |
| `GET` | `/api/loads/{id}/pods` | POD ro'yxati |
| `GET` | `/api/loads/{id}/pod/{pod_id}` | Bitta POD faylini yuklab olish |
| `DELETE` | `/api/loads/{id}/pod/{pod_id}` | POD o'chirish |

---

## Invoice — Hisob-faktura

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/loads/{id}/invoice` | Yuk uchun invoice PDF generatsiya qilish (ReportLab). Kompaniya rekvizitlari, yuk ma'lumotlari, narx |

---

## Extract — PDF dan yuk olish

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `POST` | `/extract` | Ratecon PDF faylni yuklash → AI (Gemini) orqali yuk ma'lumotlarini avtomatik ajratib olish (load number, manzillar, sanalar, narx) |

---

## Settings — Sozlamalar

### Google Maps:

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/settings/google-maps` | Google Maps API kalit sozlanganligi |
| `POST` | `/api/settings/google-maps` | Google Maps API kalitni saqlash |
| `DELETE` | `/api/settings/google-maps` | Google Maps API kalitni o'chirish |

### Status template:

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/settings/status-template` | Status xabar shabloni |
| `POST` | `/api/settings/status-template` | Shablon saqlash (o'zgaruvchilar: `{load_id}`, `{location}`, `{miles_left}`, `{speed}` va h.k.) |
| `DELETE` | `/api/settings/status-template` | Shablonni o'chirish (default ga qaytish) |

### Ratecon template:

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/settings/ratecon-template` | Ratecon xabar shabloni |
| `POST` | `/api/settings/ratecon-template` | Shablon saqlash |
| `DELETE` | `/api/settings/ratecon-template` | Shablonni o'chirish |

---

## Analytics & KPI — Tahlil

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/analytics` | Umumiy analytics (yuklar soni, daromad, o'rtacha narx, kunlik/haftalik statistika) |
| `GET` | `/api/kpi/weekly` | Haftalik KPI hisoboti (dispetcherlar bo'yicha) |
| `POST` | `/api/kpi/comment` | KPI ga izoh yozish |
| `POST` | `/api/kpi/entry` | Qo'lda KPI kiritish (masalan offline yuklar) |
| `DELETE` | `/api/kpi/entry/{id}` | KPI yozuvini o'chirish |
| `GET` | `/api/kpi/my` | Joriy foydalanuvchining KPI |
| `GET` | `/api/kpi/all-entries` | Barcha KPI yozuvlari |

---

## Pay Tiers — To'lov stavkalari

Haydovchilarga miles bo'yicha to'lov hisoblash:

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/pay-tiers` | To'lov stavkalari ro'yxati (miles oralig'i → narx per mile) |
| `POST` | `/api/pay-tiers` | Yangi stavka qo'shish. `{min_miles, max_miles, cost}` |
| `DELETE` | `/api/pay-tiers/{id}` | Stavkani o'chirish |

---

## Earnings — Daromad

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/earnings` | Daromad hisoboti (guruh/haydovchi bo'yicha, vaqt oralig'i bilan) |

---

## Alerts — Ogohlantirishlar

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/alerts` | Faol ogohlantirishlar (ETA o'tib ketgan yuklar, yetib borish vaqti yaqin) |

---

## Audit — Jurnal

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/audit/logs` | Audit loglari (kim qachon nima qildi). `?limit=20` |

---

## Health — Tizim holati

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/api/health` | Tizim salomatligi tekshirish (DB, bot, ELD ulanish) |

---

## Static — Frontend

| Method | Endpoint | Vazifasi |
|--------|----------|----------|
| `GET` | `/` | Bosh sahifa (HTML) |
| `GET` | `/app` | Asosiy ilova sahifasi |
| `GET` | `/setup` | Sozlamalar sahifasi |
| `GET` | `/login` | Kirish sahifasi |
| `GET` | `/admin` | Admin panel |
| `GET` | `/image.png` | Logo rasm |

---

## Motive API — Tashqi API

RateCon Motive serveriga quyidagi so'rovlarni yuboradi:

| Method | Motive Endpoint | Vazifasi | Ishlatiladi |
|--------|----------------|----------|-------------|
| `GET` | `/v2/users` | Haydovchilar ro'yxati | `GET /api/eld/drivers` |
| `GET` | `/v2/vehicles/current_locations` | Mashina lokatsiyalari + haydovchi | `GET /api/trucks`, `GET /api/map/locations`, ETA, status xabar |
| `POST` | `/v1/dispatch_locations` | Manzil yaratish | `POST /api/loads/{id}/motive/push` |
| `POST` | `/v2/dispatches` | Yangi dispatch (yuk) yaratish | `POST /api/loads/{id}/motive/push` |
| `GET` | `/v2/dispatches` | Dispatchlar ro'yxati | `GET /api/motive/dispatches` |
| `GET` | `/v2/dispatches/{id}` | Bitta dispatch olish | Import va sync |
| `PUT` | `/v2/dispatches` | Dispatch yangilash (to'liq ob'yekt) | `POST /api/loads/{id}/motive/sync-status` |
