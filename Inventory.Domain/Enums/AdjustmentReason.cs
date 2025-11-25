namespace Inventory.Domain.Enums;

public enum AdjustmentReason
{
    InitialBalance = 1, // موجودی اول دوره
    Damage = 2,         // خرابی/آسیب
    Expired = 3,        // تاریخ مصرف گذشته
    Found = 4,          // یافت‌شده/اضافه
    Shrinkage = 5,      // کسری/افت
    Correction = 6,     // اصلاح دستی
    Other = 99          // سایر
}