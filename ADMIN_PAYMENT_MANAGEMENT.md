# 📚 مستندات سیستم مدیریت پرداخت‌ها (Admin Payment Management)

## 📋 فهرست مطالب
- [معماری کلی سیستم](#معماری-کلی-سیستم)
- [APIهای موجود](#apiهای-موجود)
- [فلوچارت سناریوهای مختلف](#فلوچارت-سناریوهای-مختلف)
- [دیاگرام Sequence](#دیاگرام-sequence)
- [ساختار دیتابیس](#ساختار-دیتابیس)
- [مثال‌های عملی](#مثالهای-عملی)
- [Dashboard پیشنهادی](#dashboard-پیشنهادی)
- [نکات امنیتی](#نکات-امنیتی)

---

## 🏗️ معماری کلی سیستم

```
┌─────────────────────────────────────────────────────────────────┐
│                         ADMIN DASHBOARD                          │
│                      (Frontend Interface)                        │
└────────────────┬────────────────────────────────────────────────┘
                 │
                 │ HTTP Requests
                 ↓
┌─────────────────────────────────────────────────────────────────┐
│              ApiPaymentsController (Admin Area)                  │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐                │
│  │  Payment   │  │  Wallet    │  │   Report   │                │
│  │Management  │  │Management  │  │  & Query   │                │
│  └────────────┘  └────────────┘  └────────────┘                │
└────────────┬────────────────────────────────────────────────────┘
             │
             │ Service Layer Calls
             ↓
┌─────────────────────────────────────────────────────────────────┐
│                    Application Services                          │
│  ┌──────────────────┐  ┌──────────────────┐                    │
│  │ Payment Services │  │  Wallet Services │                    │
│  │ - Earnest        │  │  - Balance Check │                    │
│  │ - Settlement     │  │  - Transactions  │                    │
│  │ - Gateway Check  │  │                  │                    │
│  └──────────────────┘  └──────────────────┘                    │
└────────────┬────────────────────────────────────────────────────┘
             │
             │ Data Access
             ↓
┌─────────────────────────────────────────────────────────────────┐
│                      Database Context                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │WalletTransac-│  │   Wallets    │  │   Payments   │         │
│  │    tions     │  │              │  │  (Various)   │         │
│  └──────────────┘  └──────────────┘  └──────────────┘         │
└─────────────────────────────────────────────────────────────────┘
             │
             │ External API
             ↓
┌─────────────────────────────────────────────────────────────────┐
│                    ZarinPal Gateway                              │
│                  (Payment Verification)                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📡 APIهای موجود

### 🔧 APIهای عملیاتی (Operation APIs)

| API Endpoint | Method | توضیحات | پارامترها |
|---|---|---|---|
| `ManualTransaction` | POST | ثبت تراکنش دستی توسط ادمین | userId, amount, status, etc. |
| `UpdateTransactionStatus` | POST | تغییر وضعیت تراکنش | transactionId, newStatus |
| `RefundTransaction` | POST | استرداد پرداخت به کاربر | transactionId, amount |
| `ReconcileWallet` | POST | بررسی و اصلاح موجودی کیف پول | userId, autoFix |
| `revalidate` | POST | بررسی مجدد یک پرداخت با درگاه | kind, paymentId, authority, amount |
| `BulkRevalidate` | POST | بررسی دسته‌ای تراکنش‌های pending | hoursThreshold |

### 📊 APIهای اطلاعاتی (Query APIs)

| API Endpoint | Method | توضیحات | پارامترها |
|---|---|---|---|
| `Transactions` | GET | لیست تراکنش‌ها با فیلترهای اختیاری | userId, status, fromDate, toDate |
| `Wallet` | GET | جزئیات کیف پول یک کاربر | userId |
| `Transaction` | GET | جزئیات یک تراکنش خاص | id |
| `SuspiciousTransactions` | GET | تراکنش‌های pending طولانی مدت | hoursThreshold (default: 24) |
| `FactorPaymentHistory` | GET | تاریخچه کامل پرداخت‌های فاکتور | factorId |
| `Stats` | GET | گزارش آماری مالی | - |

---

## 🔄 فلوچارت سناریوهای مختلف

### سناریو 1: مشتری می‌گه پرداخت کردم ولی ثبت نشده

```
START (شکایت کاربر)
    │
    ↓
┌──────────────────────────────────────┐
│ ادمین: بررسی تراکنش‌های کاربر       │
│ GET /Transactions?userId=xxx         │
└─────────────┬────────────────────────┘
              │
              ↓
        ┌─────────────┐
        │آیا تراکنش   │
        │وجود دارد؟   │
        └──┬──────┬───┘
           │NO    │YES
           │      │
           │      ↓
           │  ┌──────────────────────┐
           │  │ وضعیت چیست؟         │
           │  └───┬──────────────┬───┘
           │      │Pending       │Confirm
           │      │              │
           │      ↓              ↓
           │  ┌─────────────┐   END
           │  │ بررسی درگاه │   (پرداخت قبلاً
           │  │POST         │    ثبت شده)
           │  │/revalidate  │
           │  └──┬──────────┘
           │     │
           │     ↓
           │  ┌───────────────┐
           │  │درگاه تایید   │
           │  │کرد؟          │
           │  └──┬────────┬───┘
           │     │YES     │NO
           │     │        │
           │     ↓        ↓
           │  ┌──────┐  ┌──────────────┐
           │  │Update│  │ادمین تصمیم  │
           │  │Status│  │می‌گیرد:     │
           │  │      │  │Manual OR     │
           │  │      │  │Reject        │
           │  └──────┘  └──────────────┘
           │
           ↓
      ┌────────────────────┐
      │ تراکنش وجود ندارد │
      │ POST               │
      │ /ManualTransaction │
      └────────────────────┘
              │
              ↓
            END
```

### سناریو 2: موجودی کیف پول اشتباه است

```
START (شکایت عدم تطابق)
    │
    ↓
┌──────────────────────────────────────┐
│ ادمین: بررسی کیف پول                │
│ GET /Wallet?userId=xxx               │
└─────────────┬────────────────────────┘
              │
              ↓
┌──────────────────────────────────────┐
│ بررسی تطابق موجودی                  │
│ POST /ReconcileWallet                │
│ {userId, autoFix: false}             │
└─────────────┬────────────────────────┘
              │
              ↓
        ┌─────────────┐
        │اختلاف وجود  │
        │دارد؟        │
        └──┬──────┬───┘
           │NO    │YES
           │      │
           ↓      ↓
         END   ┌────────────────────────┐
               │ بررسی علت اختلاف      │
               │ (تراکنش‌های pending؟) │
               └─────────┬──────────────┘
                         │
                         ↓
                   ┌─────────────┐
                   │ادمین تایید  │
                   │می‌کند؟      │
                   └──┬──────┬───┘
                      │YES   │NO
                      │      │
                      ↓      ↓
            ┌──────────────┐ END
            │POST          │ (نیاز به
            │/ReconcileWa- │  بررسی
            │llet          │  بیشتر)
            │autoFix:true  │
            └──────────────┘
                      │
                      ↓
                    END
              (تراکنش adjustment
               ایجاد شد)
```

### سناریو 3: بررسی روزانه تراکنش‌های معلق

```
START (وظیفه روزانه ادمین)
    │
    ↓
┌──────────────────────────────────────┐
│ لیست تراکنش‌های مشکوک               │
│ GET /SuspiciousTransactions          │
│ ?hoursThreshold=24                   │
└─────────────┬────────────────────────┘
              │
              ↓
        ┌─────────────┐
        │تراکنش مشکوک │
        │وجود دارد؟   │
        └──┬──────┬───┘
           │NO    │YES
           │      │
           ↓      ↓
         END   ┌────────────────────────┐
               │ بررسی یکجای همه        │
               │ POST /BulkRevalidate   │
               │ ?hoursThreshold=24     │
               └─────────┬──────────────┘
                         │
                         ↓
               ┌─────────────────────┐
               │ نتایج دریافت شد:   │
               │ - Success: X        │
               │ - Failed: Y         │
               └─────────┬───────────┘
                         │
                         ↓
               ┌─────────────────────┐
               │ بررسی موارد Failed  │
               │ - Manual Review     │
               │ - Contact User      │
               └─────────────────────┘
                         │
                         ↓
                       END
```

### سناریو 4: استرداد وجه

```
START (درخواست استرداد)
    │
    ↓
┌──────────────────────────────────────┐
│ پیدا کردن تراکنش                    │
│ GET /Transactions                    │
│ GET /Transaction?id=xxx              │
└─────────────┬────────────────────────┘
              │
              ↓
        ┌─────────────┐
        │وضعیت تراکنش │
        │Confirm است؟ │
        └──┬──────┬───┘
           │NO    │YES
           │      │
           ↓      ↓
         ERROR ┌────────────────────────┐
         (فقط  │ ثبت استرداد            │
          تراکن│ POST /RefundTransaction│
          ش‌های │ {transactionId, amount}│
          تایید└─────────┬──────────────┘
          شده)           │
                         ↓
               ┌─────────────────────┐
               │ سیستم:              │
               │ 1. تراکنش معکوس    │
               │    ایجاد می‌کند    │
               │ 2. موجودی آپدیت    │
               │    می‌شود           │
               └─────────┬───────────┘
                         │
                         ↓
                       END
                  (RefId برگشت
                   داده می‌شود)
```

---

## 🔄 دیاگرام Sequence

### مثال: Revalidate یک پرداخت

```
Admin      Controller    Service       Database    ZarinPal
  │             │            │             │            │
  │──Request───>│            │             │            │
  │ POST        │            │             │            │
  │ /revalidate │            │             │            │
  │             │            │             │            │
  │             │──Get Payment Info──────>│            │
  │             │            │             │            │
  │             │<───Payment Data──────────│            │
  │             │            │             │            │
  │             │──Verify──>│             │            │
  │             │  Gateway  │             │            │
  │             │            │             │            │
  │             │            │──Verify────────────────>│
  │             │            │  Request    │            │
  │             │            │             │            │
  │             │            │<───Status───────────────│
  │             │            │  100/101    │            │
  │             │            │             │            │
  │             │            │──Update────>│            │
  │             │            │  Transaction│            │
  │             │            │  & Wallet   │            │
  │             │            │             │            │
  │             │            │<──Success───│            │
  │             │            │             │            │
  │             │<─Result────│             │            │
  │             │            │             │            │
  │<──Response──│            │             │            │
  │  {RefId,    │            │             │            │
  │   Message}  │            │             │            │
```

---

## 🗂️ ساختار دیتابیس

```
┌─────────────────────────┐
│      Wallets            │
├─────────────────────────┤
│ Id (Guid)               │
│ UserId (string) FK      │
│ CashBalance (decimal)   │◄────┐
│ CheckBalance (decimal)  │     │
│ InsertTime              │     │
└─────────────────────────┘     │
                                │ 1:N
┌─────────────────────────┐     │
│  WalletTransactions     │     │
├─────────────────────────┤     │
│ Id (Guid) PK            │     │
│ WalletId (Guid) FK      ├─────┘
│ Amount (decimal)        │
│ Status (string)         │◄── "pending", "confirm", "reject"
│ TransactionType (str)   │◄── "واریز", "برداشت"
│ PaymentType (string)    │◄── "نقدی", "چکی", "تنظیم موجودی"
│ RefId (long)            │
│ PaymentId (Guid?)       │
│ InsertTime (DateTime)   │
└─────────────────────────┘
           │
           │ N:1
           ↓
┌─────────────────────────┐
│   EarnestCashInfo       │
├─────────────────────────┤
│ Id (Guid) PK            │
│ MainFactorId (long) FK  │
│ TotalAmount (decimal)   │
│ Authority (string)      │
│ RefId (long)            │
│ IsApproved (bool)       │
│ status (string)         │
└─────────────────────────┘

┌─────────────────────────┐
│ SettlementCashPayment   │
├─────────────────────────┤
│ Id (Guid) PK            │
│ UserId (string) FK      │
│ TotalAmount (decimal)   │
│ Authority (string)      │
│ RefId (long)            │
│ IsApproved (bool)       │
│ status (string)         │
└─────────────────────────┘
           │
           │ N:M (via CashSettlement)
           ↓
┌─────────────────────────┐
│    CashSettlement       │
├─────────────────────────┤
│ Id (long) PK            │
│ PaymentId (Guid) FK     │
│ factorId (long) FK      │
└─────────────────────────┘
```

### جداول کلیدی:

#### Wallets
- **Id**: شناسه منحصر به فرد کیف پول
- **UserId**: شناسه کاربر
- **CashBalance**: موجودی نقدی
- **CheckBalance**: موجودی چکی

#### WalletTransactions
- **Id**: شناسه تراکنش
- **WalletId**: ارجاع به کیف پول
- **Amount**: مبلغ تراکنش
- **Status**: وضعیت (pending/confirm/reject)
- **TransactionType**: نوع (واریز/برداشت)
- **PaymentType**: روش پرداخت (نقدی/چکی/تنظیم موجودی)
- **RefId**: شماره پیگیری
- **PaymentId**: ارجاع به پرداخت (اختیاری)

---

## 📝 مثال‌های عملی

### 1️⃣ ثبت تراکنش دستی

**Request:**
```http
POST /admin/api/ApiPayments/ManualTransaction
Content-Type: application/json
Authorization: Bearer {admin_token}

{
  "userId": "user-guid-1234",
  "amount": 50000,
  "status": "confirm",
  "refId": 123456789,
  "paymentId": "payment-guid-5678",
  "paymentType": "نقدی",
  "transactionType": "واریز"
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "تراکنش دستی با موفقیت ثبت شد"
}
```

**استفاده‌ها:**
- وقتی کاربر از روش‌های دیگر (حواله بانکی، واریز نقدی) پرداخت کرده
- تراکنش در سیستم ثبت نشده ولی پرداخت انجام شده
- اصلاح اشتباهات مالی

---

### 2️⃣ تغییر وضعیت تراکنش

**Request:**
```http
POST /admin/api/ApiPayments/UpdateTransactionStatus
Content-Type: application/json
Authorization: Bearer {admin_token}

{
  "transactionId": "transaction-guid-1234",
  "newStatus": "confirm"
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "وضعیت تراکنش با موفقیت به‌روزرسانی شد"
}
```

**نکته مهم:**
- ✅ تغییر از pending به confirm → موجودی کیف پول آپدیت می‌شود
- ✅ تغییر از confirm به reject → موجودی برگشت داده می‌شود
- ⚠️ چک می‌کند موجودی کافی باشد

---

### 3️⃣ استرداد پرداخت

**Request:**
```http
POST /admin/api/ApiPayments/RefundTransaction
Content-Type: application/json
Authorization: Bearer {admin_token}

{
  "transactionId": "transaction-guid-1234",
  "amount": 50000
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "استرداد با موفقیت انجام شد. RefId: 987654321"
}
```

**فرآیند:**
1. تراکنش اصلی باید `confirm` باشد
2. یک تراکنش معکوس ایجاد می‌شود
3. موجودی کیف پول به‌روزرسانی می‌شود
4. RefId جدید برگشت داده می‌شود

---

### 4️⃣ بررسی موجودی کیف پول (فقط چک)

**Request:**
```http
POST /admin/api/ApiPayments/ReconcileWallet
Content-Type: application/json
Authorization: Bearer {admin_token}

{
  "userId": "user-guid-1234",
  "autoFix": false
}
```

**Response - اگر اختلاف وجود داشته باشد:**
```json
{
  "isSuccess": false,
  "message": "عدم تطابق در موجودی کیف پول",
  "data": {
    "currentBalance": 100000,
    "calculatedBalance": 95000,
    "difference": 5000
  }
}
```

**Response - اگر اختلاف نباشد:**
```json
{
  "isSuccess": true,
  "message": "موجودی کیف پول صحیح است",
  "data": {
    "currentBalance": 100000,
    "calculatedBalance": 100000
  }
}
```

---

### 5️⃣ اصلاح خودکار موجودی

**Request:**
```http
POST /admin/api/ApiPayments/ReconcileWallet
Content-Type: application/json
Authorization: Bearer {admin_token}

{
  "userId": "user-guid-1234",
  "autoFix": true
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "موجودی کیف پول اصلاح شد",
  "data": {
    "oldBalance": 100000,
    "newBalance": 95000,
    "difference": 5000,
    "adjustmentRefId": 987654321
  }
}
```

**فرآیند:**
1. محاسبه موجودی واقعی از روی تراکنش‌های confirm شده
2. مقایسه با موجودی فعلی
3. ایجاد تراکنش تنظیم (adjustment) برای رفع اختلاف
4. به‌روزرسانی موجودی

---

### 6️⃣ لیست تراکنش‌های مشکوک

**Request:**
```http
GET /admin/api/ApiPayments/SuspiciousTransactions?hoursThreshold=48
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "5 تراکنش مشکوک یافت شد",
  "data": {
    "count": 5,
    "transactions": [
      {
        "id": "guid-1",
        "amount": 100000,
        "status": "pending",
        "paymentType": "نقدی",
        "transactionType": "واریز",
        "refId": 123456,
        "paymentId": "payment-guid-1",
        "insertTime": "2025-10-13T10:30:00",
        "hoursSincePending": 56.5,
        "userId": "user-guid-1",
        "user": {
          "id": "user-guid-1",
          "fullName": "علی احمدی",
          "phoneNumber": "09123456789"
        }
      }
    ]
  }
}
```

**استفاده:**
- بررسی روزانه تراکنش‌های معلق
- شناسایی مشکلات پرداخت
- پیگیری با کاربران

---

### 7️⃣ بررسی دسته‌ای تراکنش‌های معلق

**Request:**
```http
POST /admin/api/ApiPayments/BulkRevalidate?hoursThreshold=24
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "بررسی 5 تراکنش: 3 موفق، 2 ناموفق",
  "data": {
    "total": 5,
    "success": 3,
    "failed": 2,
    "details": [
      {
        "transactionId": "guid-1",
        "success": true,
        "refId": 123456,
        "message": "تراکنش تایید شد"
      },
      {
        "transactionId": "guid-2",
        "success": true,
        "refId": 789012,
        "message": "تراکنش قبلا مورد تایید قرار گرفته است"
      },
      {
        "transactionId": "guid-3",
        "success": false,
        "message": "Authority یافت نشد"
      }
    ]
  }
}
```

**فرآیند:**
1. یافتن تمام تراکنش‌های pending قدیمی‌تر از threshold
2. یافتن Authority از جداول مختلف پرداخت
3. فراخوانی API زرین‌پال برای هر تراکنش
4. گزارش نتایج

---

### 8️⃣ تاریخچه کامل پرداخت‌های فاکتور

**Request:**
```http
GET /admin/api/ApiPayments/FactorPaymentHistory?factorId=12345
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "تاریخچه پرداخت‌های فاکتور دریافت شد",
  "data": {
    "factorId": 12345,
    "earnestPayments": [
      {
        "id": "earnest-guid-1",
        "totalAmount": 50000,
        "authority": "A00000000000000000000000000123456",
        "isApproved": true,
        "insertTime": "2025-10-10T14:30:00"
      }
    ],
    "settlementPayments": [
      {
        "id": "settlement-guid-1",
        "totalAmount": 200000,
        "authority": "A00000000000000000000000000789012",
        "isApproved": true,
        "insertTime": "2025-10-13T10:15:00"
      }
    ],
    "transactions": [
      {
        "id": "transaction-guid-1",
        "amount": 50000,
        "status": "confirm",
        "paymentType": "نقدی",
        "transactionType": "واریز",
        "refId": 123456789,
        "paymentId": "earnest-guid-1",
        "insertTime": "2025-10-10T14:35:00",
        "userId": "user-guid-1"
      }
    ],
    "summary": {
      "totalEarnest": 50000,
      "totalSettlement": 200000,
      "confirmedTransactions": 250000,
      "pendingTransactions": 0
    }
  }
}
```

**استفاده:**
- مشاهده کامل تاریخچه پرداخت یک فاکتور
- ردیابی مشکلات پرداخت
- حسابرسی مالی

---

### 9️⃣ بررسی مجدد یک پرداخت با درگاه

**Request:**
```http
POST /admin/api/ApiPayments/revalidate
Content-Type: application/json
Authorization: Bearer {admin_token}

{
  "kind": 0,
  "paymentId": "payment-guid-1234",
  "authority": "A00000000000000000000000000123456",
  "amount": 100000
}
```

**پارامتر kind:**
- `0` = Earnest (بیعانه)
- `1` = WalletTopup (شارژ کیف پول)
- `2` = SettlementCash (تسویه نقدی)

**Response - موفق:**
```json
{
  "refId": 123456789,
  "message": "تراکنش تایید شد",
  "internal": {
    "isSuccess": true,
    "message": "پرداخت با موفقیت تایید شد"
  }
}
```

**Response - ناموفق:**
```json
{
  "refId": 0,
  "message": "پرداخت ناموفق بود",
  "internal": null
}
```

---

### 🔟 گزارش آماری مالی

**Request:**
```http
GET /admin/api/ApiPayments/Stats
Authorization: Bearer {admin_token}
```

**Response:**
```json
{
  "isSuccess": true,
  "total": 5000000,
  "confirmed": 4500000,
  "pending": 300000,
  "rejected": 200000
}
```

---

## 📈 Dashboard پیشنهادی برای ادمین

```
┌────────────────────────────────────────────────────────┐
│              ADMIN PAYMENT DASHBOARD                   │
├────────────────────────────────────────────────────────┤
│                                                        │
│  📊 آمار کلی (Stats API)                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐│
│  │  Total   │ │Confirmed │ │ Pending  │ │ Rejected ││
│  │ 5,000,000│ │4,500,000 │ │  300,000 │ │  200,000 ││
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘│
│                                                        │
│  ⚠️ تراکنش‌های مشکوک (SuspiciousTransactions)        │
│  ┌────────────────────────────────────────────────┐  │
│  │ 5 تراکنش بیش از 24 ساعت pending             │  │
│  │ [بررسی یکجا]  [مشاهده جزئیات]              │  │
│  └────────────────────────────────────────────────┘  │
│                                                        │
│  🔍 جستجو و فیلتر (Transactions API)                 │
│  ┌────────────────────────────────────────────────┐  │
│  │ UserId: [...] Status: [All▼] Date: [...]     │  │
│  │ [جستجو]                                       │  │
│  └────────────────────────────────────────────────┘  │
│                                                        │
│  📋 لیست تراکنش‌ها                                   │
│  ┌────┬──────┬────────┬────────┬─────────┬────────┐ │
│  │ID  │User  │Amount  │Status  │DateTime │Actions ││
│  ├────┼──────┼────────┼────────┼─────────┼────────┤ │
│  │... │Ali   │100,000 │Pending │10:30 AM │[View]  ││
│  │    │      │        │        │         │[Check] ││
│  │... │Reza  │50,000  │Confirm │09:15 AM │[View]  ││
│  │    │      │        │        │         │[Refund]││
│  └────┴──────┴────────┴────────┴─────────┴────────┘ │
│                                                        │
│  🛠️ ابزارها                                          │
│  [ثبت تراکنش دستی] [بررسی موجودی] [گزارش‌گیری]    │
│                                                        │
└────────────────────────────────────────────────────────┘
```

### ویژگی‌های Dashboard:

#### 1. کارت‌های آماری (Stats Cards)
- **Total**: مجموع کل تراکنش‌ها
- **Confirmed**: تراکنش‌های تایید شده
- **Pending**: تراکنش‌های در انتظار
- **Rejected**: تراکنش‌های رد شده

#### 2. هشدار تراکنش‌های مشکوک
- نمایش تعداد تراکنش‌های pending طولانی مدت
- دکمه بررسی یکجا (Bulk Revalidate)
- لینک به جزئیات

#### 3. جستجو و فیلتر
- فیلتر بر اساس UserId
- فیلتر بر اساس Status (pending/confirm/reject)
- فیلتر بر اساس تاریخ (از - تا)

#### 4. جدول تراکنش‌ها
نمایش:
- شناسه تراکنش
- نام کاربر
- مبلغ
- وضعیت
- تاریخ و زمان
- دکمه‌های عملیات:
  - **View**: مشاهده جزئیات
  - **Check**: بررسی مجدد با درگاه
  - **Refund**: استرداد وجه
  - **Edit Status**: تغییر وضعیت

#### 5. ابزارها
- **ثبت تراکنش دستی**: باز کردن فرم ManualTransaction
- **بررسی موجودی**: باز کردن صفحه ReconcileWallet
- **گزارش‌گیری**: دانلود Excel یا PDF

---

## 🔐 نکات امنیتی

### Authorization
```csharp
[Authorize(Roles = "ADMIN")]
[Route("admin/api/[controller]")]
```

✅ **تمامی APIها فقط برای ادمین‌ها:**
- فقط کاربران با نقش `ADMIN` دسترسی دارند
- احراز هویت با JWT Token
- همه درخواست‌ها باید شامل `Authorization: Bearer {token}` باشند

### Logging & Audit Trail

**توصیه‌ها:**
1. **لاگ تمام عملیات مالی:**
   ```csharp
   // در هر متد
   _logger.LogInformation($"Admin {adminUserId} performed {actionName} on transaction {transactionId}");
   ```

2. **جدول Audit:**
   ```sql
   CREATE TABLE AdminAuditLog (
       Id BIGINT PRIMARY KEY,
       AdminUserId NVARCHAR(450),
       Action NVARCHAR(100),
       TargetEntity NVARCHAR(100),
       TargetId NVARCHAR(450),
       OldValue NVARCHAR(MAX),
       NewValue NVARCHAR(MAX),
       Timestamp DATETIME,
       IpAddress NVARCHAR(50)
   )
   ```

3. **تاریخچه تغییرات:**
   - ذخیره وضعیت قبل و بعد تغییرات
   - ثبت IP Address و User Agent
   - ثبت زمان دقیق عملیات

### Validation

✅ **اعتبارسنجی داده‌ها:**
- استفاده از `[Required]`, `[Range]`, `[StringLength]`
- بررسی `ModelState.IsValid`
- جلوگیری از SQL Injection با استفاده از EF Core
- Sanitize ورودی‌های کاربر

### Transaction Safety

✅ **امنیت تراکنش‌ها:**
```csharp
using (var transaction = await _context.BeginTransactionAsync())
{
    try
    {
        // عملیات‌های چندگانه
        _context.SaveChanges();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## 📝 کدهای وضعیت (Status Codes)

### WalletTransactionStatus
```csharp
public class WalletTransactionStatus
{
    public const string pending = "pending";   // در انتظار
    public const string confirm = "confirm";   // تایید شده
    public const string reject = "reject";     // رد شده
}
```

### WalletTransactionType
```csharp
public class WalletTransactionType
{
    public const string deposit = "واریز";     // افزایش موجودی
    public const string withdrawal = "برداشت"; // کاهش موجودی
}
```

### PaymentType
```csharp
public class PaymentType
{
    public const string cash = "نقدی";
    public const string check = "چکی";
    public const string adjustment = "تنظیم موجودی"; // برای ReconcileWallet
}
```

### PaymentKind (Enum)
```csharp
public enum PaymentKind
{
    Earnest = 0,        // بیعانه
    WalletTopup = 1,    // شارژ کیف پول
    SettlementCash = 2  // تسویه نقدی
}
```

---

## 🚀 نکات پیاده‌سازی

### 1. Error Handling

```csharp
try
{
    // عملیات
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Database error");
    return StatusCode(500, new ResultDto 
    { 
        IsSuccess = false, 
        Message = "خطا در ذخیره‌سازی اطلاعات" 
    });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return StatusCode(500, new ResultDto 
    { 
        IsSuccess = false, 
        Message = "خطای غیرمنتظره رخ داده است" 
    });
}
```

### 2. Performance Optimization

**استفاده از AsNoTracking برای Query‌های Read-Only:**
```csharp
var transactions = await _context.WalletTransactions
    .AsNoTracking()
    .Where(...)
    .ToListAsync();
```

**Pagination برای لیست‌های بزرگ:**
```csharp
var transactions = await _context.WalletTransactions
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### 3. Background Jobs

**برای بررسی خودکار روزانه:**
```csharp
// استفاده از Hangfire یا Quartz.NET
RecurringJob.AddOrUpdate(
    "check-suspicious-transactions",
    () => CheckSuspiciousTransactions(),
    Cron.Daily(9)); // هر روز ساعت 9 صبح
```

---

## 📞 پشتیبانی و تماس

برای سوالات و مشکلات:
- **Email**: support@tabloyar.com
- **تیم توسعه**: dev@tabloyar.com

---

## 📄 نسخه و تاریخچه تغییرات

**نسخه 1.0.0** - 15 اکتبر 2025
- پیاده‌سازی اولیه سیستم مدیریت پرداخت‌ها
- 8 API عملیاتی و اطلاعاتی
- پشتیبانی از زرین‌پال
- سیستم Reconciliation کیف پول

---

**تهیه شده توسط تیم توسعه Tabloyar**

