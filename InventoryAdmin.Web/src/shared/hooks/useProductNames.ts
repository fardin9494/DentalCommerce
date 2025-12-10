import { useEffect, useMemo, useState } from 'react'
import { getProductDetail } from '@/shared/api/catalog'

type NameMaps = {
  products: Map<string, string>
  variants: Map<string, string>
}

/**
 * Hook برای دریافت نام محصول و واریانت از API کاتالوگ
 * از useEffect + Promise.all استفاده می‌کند تا تعداد هوک‌ها ثابت بماند
 * و خطای «Rendered more hooks than during the previous render» رخ ندهد.
 */
export function useProductNames(productIds: (string | null | undefined)[]) {
  const [maps, setMaps] = useState<NameMaps>(() => ({
    products: new Map(),
    variants: new Map(),
  }))
  const [isLoading, setIsLoading] = useState(false)
  const [hasError, setHasError] = useState(false)

  // فیلتر کردن productId های تکراری و معتبر و تولید کلید پایدار
  const { uniqueIds, key } = useMemo(() => {
    const ids = Array.from(new Set(productIds.filter((id): id is string => !!id)))
    ids.sort() // برای پایداری کلید
    return { uniqueIds: ids, key: ids.join('|') }
  }, [productIds])

  useEffect(() => {
    let cancelled = false

    // اگر هیچ شناسه‌ای نیست، state را خالی می‌کنیم و برمی‌گردیم
    if (uniqueIds.length === 0) {
      setMaps({ products: new Map(), variants: new Map() })
      setIsLoading(false)
      setHasError(false)
      return
    }

    setIsLoading(true)
    setHasError(false)

    Promise.all(uniqueIds.map((id) => getProductDetail(id)))
      .then((results) => {
        if (cancelled) return

        const products = new Map<string, string>()
        const variants = new Map<string, string>()

        results.forEach((p) => {
          products.set(p.id, p.name)
          p.variants?.forEach((v) => variants.set(v.id, v.value))
        })

        setMaps({ products, variants })
      })
      .catch(() => {
        if (cancelled) return
        setHasError(true)
      })
      .finally(() => {
        if (cancelled) return
        setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [key])

  // تابع helper برای گرفتن نام محصول
  const getProductName = (productId: string | null | undefined): string => {
    if (!productId) return 'نامشخص'
    return maps.products.get(productId) || `${productId.substring(0, 8)}...`
  }

  // تابع helper برای گرفتن نام واریانت
  const getVariantName = (variantId: string | null | undefined): string | null => {
    if (!variantId) return null
    return maps.variants.get(variantId) || null
  }

  return {
    getProductName,
    getVariantName,
    isLoading,
    hasError,
  }
}

