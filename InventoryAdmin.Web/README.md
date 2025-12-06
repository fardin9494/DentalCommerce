# InventoryAdmin.Web

پنل ادمین برای مدیریت بخش Inventory (انبارداری). ساخته‌شده با React + TypeScript + Vite، TailwindCSS، React Query، react-hook-form و zod.

## ساختار پروژه

این پروژه با همان ساختار و تکنولوژی‌های `Admin.Web` (پنل ادمین Catalog) ساخته شده است:

- **React 18** + **TypeScript**
- **Vite** برای build و dev server
- **React Router** برای routing
- **React Query** برای state management و caching
- **TailwindCSS** برای styling
- **Zod** برای validation
- **SweetAlert2** (via CDN) برای dialogs

## اجرای پروژه

1) نصب پکیج‌ها

```bash
cd InventoryAdmin.Web
npm install
```

2) ساخت فایل `.env` (اختیاری)

```bash
# .env
VITE_API_BASE_URL=http://localhost:7223/api/inventory
```

اگر فایل `.env` وجود نداشته باشد، از مقدار پیش‌فرض `http://localhost:7223/api/inventory` استفاده می‌شود.

3) اجرا

```bash
npm run dev
```

سپس به آدرس نمایش‌داده‌شده (معمولاً `http://localhost:5173`) مراجعه کنید.

## ساختار فایل‌ها

```
InventoryAdmin.Web/
├── src/
│   ├── app/                    # تنظیمات اصلی اپلیکیشن
│   │   ├── router.tsx         # مسیرها و صفحات
│   │   ├── providers.tsx       # Providerها (React Query, Auth, Toast, Confirm)
│   │   ├── auth.tsx            # مدیریت احراز هویت
│   │   ├── env.ts              # تنظیمات محیط و API_BASE
│   │   ├── LoginPage.tsx       # صفحه ورود
│   │   └── RouteError.tsx      # صفحه خطا
│   ├── lib/api/                # API client
│   │   ├── client.ts           # wrapper برای fetch با خطای ساختارمند
│   │   └── types.ts            # انواع پایه API
│   ├── shared/                 # کامپوننت‌ها و ابزارهای مشترک
│   │   ├── components/
│   │   │   ├── Layout.tsx      # Layout اصلی با navigation
│   │   │   ├── PageHeader.tsx  # هدر صفحات
│   │   │   ├── Spinner.tsx     # Loading spinner
│   │   │   ├── toast/          # Toast notifications
│   │   │   └── confirm/        # Confirm dialogs
│   │   └── utils/
│   │       └── swal.ts         # SweetAlert2 wrappers
│   └── features/               # Features (feature-based structure)
│       ├── receipts/           # رسیدها (ورود به انبار)
│       │   ├── types.ts        # Zod schemas و TypeScript types
│       │   ├── api.ts          # توابع API (CRUD)
│       │   ├── queries.ts      # React Query hooks
│       │   └── routes/         # صفحات
│       ├── issues/              # خروجی‌ها
│       ├── transfers/           # انتقالات بین انبارها
│       ├── adjustments/         # اصلاحات موجودی
│       ├── warehouses/          # انبارها
│       └── stock-items/         # موجودی‌ها
├── index.html
├── package.json
├── vite.config.ts
├── tsconfig.json
└── tailwind.config.js
```

## Features پیاده‌سازی شده

### ✅ Receipts (رسیدها)
- ایجاد رسید جدید
- مشاهده جزئیات رسید
- افزودن/حذف خطوط
- دریافت رسید (Receive)
- تایید رسید (Approve)
- لغو رسید

### ✅ Issues (خروجی‌ها)
- ایجاد خروجی جدید
- مشاهده جزئیات خروجی
- افزودن/حذف خطوط
- تخصیص FEFO
- ثبت خروجی (Post)
- لغو خروجی

### ⏳ Transfers (انتقالات)
- در حال توسعه

### ⏳ Adjustments (اصلاحات)
- در حال توسعه

### ⏳ Warehouses (انبارها)
- در حال توسعه

### ⏳ Stock Items (موجودی‌ها)
- در حال توسعه

## احراز هویت

پنل ادمین از Bearer token برای احراز هویت استفاده می‌کند. پسورد در localStorage مرورگر ذخیره می‌شود و به صورت header `Authorization: Bearer {token}` به API ارسال می‌شود.

## API Endpoints

پنل ادمین با Inventory API در آدرس `http://localhost:7223/api/inventory` ارتباط برقرار می‌کند.

### Receipts
- `GET /receipts/{id}` - دریافت جزئیات رسید
- `POST /receipts` - ایجاد رسید جدید
- `POST /receipts/{id}/lines` - افزودن خط
- `DELETE /receipts/{id}/lines/{lineId}` - حذف خط
- `PUT /receipts/{id}` - به‌روزرسانی هدر
- `PUT /receipts/{id}/lines/{lineId}` - به‌روزرسانی خط
- `POST /receipts/{id}/receive` - دریافت رسید
- `POST /receipts/{id}/approve` - تایید رسید
- `POST /receipts/{id}/cancel` - لغو رسید

### Issues
- `GET /issues/{id}` - دریافت جزئیات خروجی
- `POST /issues` - ایجاد خروجی جدید
- `POST /issues/{id}/lines` - افزودن خط
- `DELETE /issues/{id}/lines/{lineId}` - حذف خط
- `PUT /issues/{id}` - به‌روزرسانی هدر
- `PUT /issues/{id}/lines/{lineId}` - به‌روزرسانی خط
- `POST /issues/{id}/lines/{lineId}/allocate-fefo` - تخصیص FEFO
- `POST /issues/{id}/post` - ثبت خروجی
- `POST /issues/{id}/cancel` - لغو خروجی

## توسعه بیشتر

برای اضافه کردن feature جدید:

1. در `src/features/` یک پوشه جدید ایجاد کنید
2. `types.ts` - Zod schemas و TypeScript types
3. `api.ts` - توابع API
4. `queries.ts` - React Query hooks
5. `routes/` - صفحات React
6. در `src/app/router.tsx` route جدید اضافه کنید
7. در `src/shared/components/Layout.tsx` لینک navigation اضافه کنید

## Build برای Production

```bash
npm run build
```

فایل‌های build در پوشه `dist/` قرار می‌گیرند.

## Notes

- این پروژه برای development و testing طراحی شده است
- برای production، باید authentication و security را تقویت کنید
- CORS باید در Inventory API فعال باشد
- API باید Bearer token authentication را پشتیبانی کند


