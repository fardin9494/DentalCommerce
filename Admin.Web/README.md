# Admin.Web

پنل ادمین برای تست و مدیریت دامین Product و بخش‌های مرتبط. ساخته‌شده با React + TypeScript + Vite، TailwindCSS، React Query، react-hook-form و zod.

## اجرای پروژه

1) نصب پکیج‌ها

```
cd Admin.Web
npm i
```

2) ساخت فایل env

```
cp .env.example .env
# یا بصورت دستی بسازید
```

`VITE_API_BASE_URL` را به آدرس API بک‌اند خود تنظیم کنید (مثلاً `http://localhost:5000/api/catalog`).

3) اجرا

```
npm run dev
```

سپس به آدرس نمایش‌داده‌شده (معمولاً `http://localhost:5173`) مراجعه کنید.

## ساختار

- `src/app`
  - `router.tsx`: مسیرها و صفحات
  - `providers.tsx`: Providerها (React Query)
  - `env.ts`: تنظیمات محیط و `API_BASE`
- `src/lib/api`
  - `client.ts`: wrapper برای fetch با خطای ساختارمند
  - `types.ts`: انواع پایه API
- `src/shared/components`: کامپوننت‌های مشترک (Layout، Spinner، PageHeader)
- `src/features/products`
  - `types.ts`: zod schema و انواع Product/Brand/Category
  - `api.ts`: توابع API (CRUD محصول، لیست برند/دسته، آپلود تصویر)
  - `queries.ts`: هوک‌های React Query
  - `components/`: فرم، جدول لیست، بخش تصاویر
  - `routes/`: صفحات لیست و جزئیات
 - `src/features/brands/routes/BrandsPage.tsx`: صفحه مدیریت برندها
 - `src/features/categories/routes/CategoriesPage.tsx`: صفحه مدیریت دسته‌ها

## انطباق با API شما

- مسیرها پیش‌فرض به‌صورت زیر فرض شده‌اند. در صورت تفاوت، در `src/features/products/api.ts` تغییر دهید:
  - GET `/products?search=&page=&pageSize=&brandId=&categoryId=&isActive=`
  - GET `/products/:id`
  - POST `/products`
  - PUT `/products/:id`
  - DELETE `/products/:id`
  - POST `/products/:id/images` (multipart/form-data)
  - GET `/brands` و GET `/categories`

- قرارداد صفحه‌بندی: `Paginated<T> = { items, page, pageSize, totalItems, totalPages }`

## نکات

- برای استفاده از shadcn/ui می‌توانید بعد از نصب پروژه دستور زیر را اجرا کنید و کامپوننت‌های موردنیاز را اضافه کنید:
  - `npx shadcn-ui@latest init`
  - `npx shadcn-ui@latest add button input label dialog textarea select toast table`
- در حال حاضر از کامپوننت‌های ساده Tailwind استفاده شده تا سریعاً قابل‌اجرا باشد.

## TODO پیشنهادی

- افزودن صفحه/CRUD برند و دسته‌بندی
- فیلترهای پیشرفته (brand/category) و sort
- Toast اعلان موفق/خطا
- محافظت مسیریابی (Auth) در صورت نیاز
