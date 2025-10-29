# 📋 راهنمای Migration - تبدیل TaskItem به Issue Model (مثل Jira)

## ✅ تغییرات انجام شده

### 1️⃣ **مدل‌های Domain (تکمیل شده)**

#### `TaskItem.cs` - تبدیل به Issue Model
- ✅ اضافه شد: `IssueType` enum (Epic, Story, Task, Subtask, Bug)
- ✅ اضافه شد: `IssueKey` (مثل PROJ-123)
- ✅ اضافه شد: `StoryPoints` (برای Story/Task)
- ✅ اضافه شد: Time Tracking (`OriginalEstimateHours`, `TimeSpentHours`, `RemainingTimeHours`)
- ✅ تغییر نام: `ParentId` → `ParentTaskId`
- ✅ تغییر نام: `SubTasks` → `ChildIssues`
- ✅ اضافه شد: Audit fields (`CreatedByUserId`, `CreatedAt`, `UpdatedAt`)
- ✅ تبدیل: `CategoryId` به nullable

#### `Project.cs`
- ✅ اضافه شد: `IssueKeyPrefix` (مثل "PROJ")
- ✅ اضافه شد: `LastIssueNumber` (برای auto-increment)
- ✅ اضافه شد: `CreatedAt`, `UpdatedAt`
- ✅ اضافه شد: متد `GenerateNextIssueKey()`

#### Helper Classes (جدید)
- ✅ `IssueTypeExtensions.cs` - Extension methods برای IssueType
- ✅ `IssueHierarchyValidator.cs` - Validation قوانین سلسله مراتب
- ✅ `IssueKeyGenerator.cs` - Generator برای IssueKey

### 2️⃣ **Database Context (تکمیل شده)**

#### `MVPTestDatabaseContext.cs`
- ✅ آپدیت: Parent-Child relationship
- ✅ اضافه شد: Index برای `IssueKey` (Unique)
- ✅ اضافه شد: Index برای `ProjectId` + `IssueType`
- ✅ اضافه شد: Index برای `IssueKeyPrefix` در Project
- ✅ آپدیت: `CategoryId` به nullable با `OnDelete(SetNull)`
- ✅ اضافه شد: Default value برای `IssueType`

### 3️⃣ **Controllers (تکمیل شده)**

#### `TasksController.cs`
- ✅ تمام استفاده از `ParentId` به `ParentTaskId` تغییر کرد
- ✅ تمام استفاده از `SubTasks` به `ChildIssues` تغییر کرد
- ✅ منطق ایجاد Subtask اصلاح شد (با `IssueType.Subtask`)

### 4️⃣ **Views (تکمیل شده)**

✅ فایل‌های زیر اصلاح شدند:
- `Index.cshtml`
- `Weekly.cshtml`
- `_TaskBranch.cshtml`
- `_TaskTree.cshtml`

---

## 🚀 مراحل اجرای Migration

### مرحله 1: ایجاد Migration (اگر لازم است)

اگر می‌خواهید Migration رو با EF Core CLI بسازید:

```bash
cd Tabloyar.Persistence
dotnet ef migrations add AddJiraLikeIssueModel --startup-project ../Endpoint.Site --context MVPTestDatabaseContext
```

**نکته:** من یک Migration دستی در `Migrations/MVPTestDatabase/20251028000000_AddJiraLikeIssueModel.cs` ایجاد کردم که می‌تونید مستقیماً استفاده کنید.

### مرحله 2: اجرای Migration

```bash
cd Tabloyar.Persistence
dotnet ef database update --startup-project ../Endpoint.Site --context MVPTestDatabaseContext
```

یا در `Package Manager Console`:

```powershell
Update-Database -Context MVPTestDatabaseContext
```

### مرحله 3: بررسی Database

پس از اجرای Migration، بررسی کنید:

#### جدول `TaskItems`:
```sql
-- بررسی ستون‌های جدید
SELECT TOP 5 
    Id, Title, IssueType, IssueKey, ParentTaskId, 
    StoryPoints, CategoryId, CreatedAt
FROM TaskItems;

-- بررسی توزیع IssueType
SELECT IssueType, COUNT(*) as Count
FROM TaskItems
GROUP BY IssueType;
```

#### جدول `Projects`:
```sql
-- بررسی IssueKeyPrefix و LastIssueNumber
SELECT Id, Name, IssueKeyPrefix, LastIssueNumber, CreatedAt
FROM Projects;
```

---

## 📊 Data Migration (اتوماتیک)

Migration شامل **Data Migration** اتوماتیک است:

### 1. تشخیص IssueType:
```sql
UPDATE TaskItems 
SET IssueType = CASE 
    WHEN ParentTaskId IS NOT NULL THEN 4  -- Subtask
    ELSE 3  -- Task
END
```

### 2. Generate کردن IssueKey:
- برای هر Project، تسک‌ها شماره‌گذاری می‌شوند
- IssueKey به صورت `{IssueKeyPrefix}-{Number}` ساخته می‌شود
- مثال: `PROJ-1`, `PROJ-2`, `PROJ-3`

### 3. به‌روزرسانی LastIssueNumber:
- برای هر Project، تعداد کل تسک‌ها محاسبه و در `LastIssueNumber` ذخیره می‌شود

---

## 🔍 بررسی و تست

### 1. Compile Check
```bash
dotnet build Endpoint.Site
```

**وضعیت:** ✅ بدون خطا (تکمیل شده)

### 2. Migration Check
```bash
# بررسی Migration‌های Pending
dotnet ef migrations list --startup-project Endpoint.Site --context MVPTestDatabaseContext
```

### 3. Functional Test

پس از اجرای Migration:

1. ✅ وارد صفحه Tasks شوید (`/TaskPlanner/Tasks/Index`)
2. ✅ بررسی کنید تسک‌های قدیمی نمایش داده می‌شوند
3. ✅ یک Task جدید ایجاد کنید
4. ✅ یک Subtask برای Task ایجاد شده اضافه کنید
5. ✅ بررسی کنید IssueKey اتوماتیک generate شده است

---

## 🐛 Troubleshooting

### خطا: "Cannot insert NULL into CategoryId"
**راه حل:** Migration به‌طور خودکار `CategoryId` رو nullable می‌کنه.

### خطا: "Duplicate IssueKey"
**راه حل:** 
```sql
-- پاک کردن IssueKey‌های duplicate
UPDATE TaskItems SET IssueKey = NULL WHERE IssueKey IN (
    SELECT IssueKey FROM TaskItems 
    GROUP BY IssueKey HAVING COUNT(*) > 1
);

-- اجرای مجدد data migration script
```

### خطا: "ParentId does not exist"
**راه حل:** همه فایل‌های View و Controller اصلاح شدند. Clear کردن cache:
```bash
dotnet clean
dotnet build
```

---

## 📈 مراحل بعدی (Future Enhancements)

### مرحله 2: UI برای Issue Types
- [ ] افزودن فیلتر IssueType در Index
- [ ] نمایش آیکون‌ها و Badge‌ها برای هر IssueType
- [ ] فرم ایجاد Issue با انتخاب Type

### مرحله 3: Epic View
- [ ] صفحه جداگانه برای Epics
- [ ] نمایش Story‌های زیر Epic
- [ ] Progress Bar برای Epic

### مرحله 4: Sprint Board
- [ ] Kanban Board با Drag & Drop
- [ ] Group By Epic
- [ ] Swimlanes

### مرحله 5: Time Tracking
- [ ] Log Work functionality
- [ ] نمایش گزارش‌های Time
- [ ] Burndown Chart

---

## 📝 خلاصه تغییرات Database

| جدول | تغییرات |
|------|---------|
| **TaskItems** | + IssueType, IssueKey, StoryPoints, Time Tracking<br>+ CreatedByUserId, CreatedAt, UpdatedAt<br>~ CategoryId (nullable)<br>~ ParentId → ParentTaskId |
| **Projects** | + IssueKeyPrefix, LastIssueNumber<br>+ CreatedAt, UpdatedAt |
| **Indexes** | + IX_TaskItems_IssueKey (Unique)<br>+ IX_TaskItems_ProjectId_IssueType<br>+ IX_Projects_IssueKeyPrefix |

---

## ✅ Checklist

- [x] اصلاح Entity Models
- [x] اصلاح DbContext Configuration
- [x] اصلاح Controllers
- [x] اصلاح Views
- [x] ایجاد Extension Methods
- [x] ایجاد Validators
- [x] ایجاد Migration File
- [ ] **اجرای Migration**
- [ ] تست Functional
- [ ] آموزش به کاربران

---

**✨ تبریک! مدل Issue شما حالا مثل Jira است! ✨**

