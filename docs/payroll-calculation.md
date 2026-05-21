# Payroll Calculation Reference

> How payroll is calculated for each employee in the system.

---

## Table of Contents

1. [Input Data](#1-input-data)
2. [Core Formulas](#2-core-formulas)
3. [Deductions](#3-deductions)
4. [Net Pay](#4-net-pay)
5. [Persisted Records](#5-persisted-records)
6. [Not Yet Implemented](#6-not-yet-implemented)
7. [Practical Example](#7-practical-example)
8. [Source Files](#8-source-files)

---

## 1. Input Data

### Employee (`Models/Employee.cs:29`)
| Field | Source | Example |
|-------|--------|---------|
| `BasicSalary` | Employee record | ₱48,000 (monthly) |

### Attendance (`Models/Attendance.cs:14-85`) — one record per day
| Field | Description | Used in Payroll? |
|-------|-------------|-----------------|
| `TimeIn` | Clock-in time | Raw data |
| `TimeOut` | Clock-out time | Raw data |
| `TotalHours` | Hours worked that day | ✅ Yes — sums into Basic Pay |
| `OvertimeHours` | Overtime hours | ❌ No — payroll uses Overtimes table instead |
| `NightShiftHours` | Hours within 10PM–6AM | ✅ Yes — Night Shift Differential |
| `LateMinutes` | Minutes late | ❌ Informational only |
| `UndertimeMinutes` | Minutes early out | ❌ Informational only |
| `DayType` | `Regular` / `RestDay` / `Holiday` / `RestDayHoliday` | ✅ Yes — OT multiplier |

### Overtime (`Models/Overtime.cs:6-44`) — approved requests only
| Field | Description |
|-------|-------------|
| `Hours` | Overtime hours (this is what payroll uses) |
| `Status` | Must be `Approved` to count |

### Shift (`Models/Shift.cs:6-41`)
| Field | Description |
|-------|-------------|
| `StartTime` | Shift start time |
| `EndTime` | Shift end time |
| `GracePeriodMinutes` | Grace period before late marking |
| `IsNightShift` | Whether this is a night shift |
| `IsOvernight` | Whether shift goes past midnight |

---

## 2. Core Formulas

All code in `Services/PayrollServices.cs`.

### Hourly Rate (line 33-34)

```
HourlyRate = MonthlySalary / 22 / 8
```

22 working days per month × 8 hours per day = **176 hours per month**.

### Basic Pay (line 36-37)

```
BasicPay = HourlyRate × TotalHoursWorked
```

Where `TotalHoursWorked` = sum of all `Attendance.TotalHours` in the period.

### Overtime Pay (lines 39-49)

```
OvertimePay = HourlyRate × TotalApprovedOT × DayTypeMultiplier
```

- `TotalApprovedOT` = sum of `Overtime.Hours` where `Status == Approved`
- Uses **approved Overtime requests** — NOT `Attendance.OvertimeHours`

**Day Type Multiplier:**

| `DayType` | Multiplier |
|-----------|-----------|
| `Regular` | **1.25** (125%) |
| `RestDay` | **1.30** (130%) |
| `Holiday` | **1.30** (130%) |
| `RestDayHoliday` | **1.50** (150%) |
| (fallback) | **1.25** |

The day type used is the **most common** `DayType` across all attendance records in the period (line 155-158).

### Night Shift Differential (line 51-52)

```
NightDiffPay = HourlyRate × NightShiftHours × 0.10
```

- 10% (not 10x — it's an additional 10% of hourly rate per night hour)

### Gross Pay (line 165)

```
GrossPay = BasicPay + OvertimePay + NightDiffPay
```

---

## 3. Deductions

All deductions use `contributionBase = GrossPay` (line 169).

### SSS — Social Security System

**Code:** `Services/GovernmentServices.cs:16-23`

```
SSS = SSSContributions table lookup by GrossPay → EmployeeShare
```

50 brackets from 5,000 to 30,000 (`Data/Seed/SeedGovernmentData.cs:12-65`):

| Gross Pay Range | Employee Share |
|----------------|---------------|
| 5,000.00 – 5,249.99 | ₱225.00 |
| 5,250.00 – 5,749.99 | ₱236.25 |
| 5,750.00 – 6,249.99 | ₱247.50 |
| 6,250.00 – 6,749.99 | ₱258.75 |
| ... | ... |
| 29,750.00 – 30,000.00 | ₱787.50 |

If GrossPay is below ₱5,000 or no bracket matches → **₱0**.

### PhilHealth — Philippine Health Insurance

**Code:** `Services/GovernmentServices.cs:25-36`

```
baseSalary  = Clamp(GrossPay, 10,000, 80,000)
PhilHealth  = baseSalary × 4% ÷ 2
```

- `Clamp(x, min, max)` — if below min, use min; if above max, use max
- The `÷ 2` is the **employee share** (employer pays the other half)

| Gross Pay | PhilHealth (Employee) |
|-----------|----------------------|
| ≤ ₱10,000 | ₱10,000 × 4% ÷ 2 = **₱200.00** |
| ₱48,000 | ₱48,000 × 4% ÷ 2 = **₱960.00** |
| ≥ ₱80,000 | ₱80,000 × 4% ÷ 2 = **₱1,600.00** |
| ≤ 0 | **₱0** |

### Pag-IBIG — Home Development Mutual Fund

**Code:** `Services/GovernmentServices.cs:38-44`

```
if GrossPay ≤ 1,500:
    PagIBIG = GrossPay × 1%
else:
    PagIBIG = Min(GrossPay × 2%, 100)    ← capped at ₱100
```

| Gross Pay | Pag-IBIG |
|-----------|----------|
| ₱1,000 | ₱1,000 × 1% = **₱10.00** |
| ₱5,000 | Min(5,000 × 2%, 100) = **₱100.00** |
| any amount > ₱5,000 | **₱100.00** (capped) |

### Tax — Withholding Tax (TRAIN Law)

**Code:** `Services/TaxServices.cs:5-22`

```
TaxableIncome = GrossPay - (SSS + PhilHealth + PagIBIG)
```

| Taxable Income | Tax Due |
|---------------|---------|
| ≤ ₱20,833 | **₱0** |
| ₱20,833.01 – ₱33,333 | (Income − 20,833) × **15%** |
| ₱33,333.01 – ₱66,667 | ₱1,875 + (Income − 33,333) × **20%** |
| ₱66,667.01 – ₱166,667 | ₱8,541.80 + (Income − 66,667) × **25%** |
| > ₱166,667 | ₱33,541.80 + (Income − 166,667) × **30%** |

If `TaxableIncome < 0`, tax = **₱0**.

### Deduction Capping Safety Net

**Code:** `Services/PayrollServices.cs:58-122`

If `SSS + PhilHealth + PagIBIG + Tax > GrossPay`:

1. All four are **proportionally scaled** by factor = GrossPay ÷ total
2. If rounding still causes excess, cascading reduction in order: **Tax → PagIBIG → PhilHealth → SSS**
3. If GrossPay ≤ 0, all deductions are set to 0

This guarantees **NetPay is never negative**.

### Total Deductions (line 184)

```
TotalDeductions = SSS + PhilHealth + PagIBIG + Tax
```

---

## 4. Net Pay (line 185)

```
NetPay = GrossPay - TotalDeductions
```

Always non-negative (enforced by deduction capping + model validation).

---

## 5. Persisted Records

### Payroll (`Models/Payroll.cs:6-74`)

| Field | Description |
|-------|-------------|
| `GrossPay` | BasicPay + OvertimePay + NightDiffPay |
| `TotalDeductions` | SSS + PhilHealth + PagIBIG + Tax |
| `NetPay` | GrossPay - TotalDeductions |
| `Status` | `Draft` → `Processed` → `Released` |

### Earnings Created (`Services/PayrollServices.cs:209-218`)

| `EarningType` | Always Created? |
|--------------|----------------|
| `BasicPay` | ✅ Always (even if 0) |
| `Overtime` | ✅ Always (even if 0) |
| `NightShiftDifferential` | Only if amount > 0 |
| `Bonus` | ❌ Enum exists, not generated |
| `Allowance` | ❌ Enum exists, not generated |

### Deductions Created (`Services/PayrollServices.cs:223-228`)

| `DeductionType` | Always Created? |
|----------------|----------------|
| `SSS` | ✅ Always |
| `PhilHealth` | ✅ Always |
| `PagIBIG` | ✅ Always |
| `Tax` | ✅ Always |
| `Loan` | ❌ Enum exists, not generated |
| `Other` | ❌ Enum exists, not generated |

---

## 6. Not Yet Implemented

| Feature | Status |
|---------|--------|
| Late minutes reducing pay | ❌ Informational only |
| Undertime minutes reducing pay | ❌ Informational only |
| Attendance `OvertimeHours` in calc | ❌ Uses `Overtimes` table instead |
| Bonuses / Allowances | ❌ Enum exists, not generated |
| Loan deductions | ❌ Enum exists, not generated |
| 13th month pay | ❌ Not implemented |

---

## 7. Practical Example

### Employee: Maria Santos (EMP-2025-001)

**Inputs:**
- Monthly Salary: **₱48,000**
- Period: 1 month (e.g., April 2026)
- Attendance: **176 hours** (perfect attendance, 22 days × 8 hrs)
- No overtime, no night shift

**Step-by-step:**

| Step | Calculation | Amount |
|------|------------|--------|
| Hourly Rate | 48,000 ÷ 22 ÷ 8 | **₱272.7273/hr** |
| Basic Pay | 272.7273 × 176 | **₱48,000.00** |
| Overtime Pay | 0 | **₱0.00** |
| Night Diff Pay | 0 | **₱0.00** |
| **Gross Pay** | 48,000 + 0 + 0 | **₱48,000.00** |
| SSS | Bracket 19,750–20,249.99 | **₱562.50** |
| PhilHealth | Clamp(48,000, 10k, 80k) × 4% ÷ 2 | **₱960.00** |
| Pag-IBIG | Min(48,000 × 2%, 100) | **₱100.00** |
| Gov Total | 562.50 + 960 + 100 | **₱1,622.50** |
| Taxable Income | 48,000 - 1,622.50 | **₱46,377.50** |
| Tax | 1,875 + (46,377.50 - 33,333) × 20% | **₱4,483.90** |
| Cap Check | 1,622.50 + 4,483.90 = 6,106.40 ≤ 48,000 | ✅ No cap needed |
| **Total Deductions** | 562.50 + 960 + 100 + 4,483.90 | **₱6,106.40** |
| **Net Pay** | 48,000 - 6,106.40 | **₱41,893.60** |

---

## 8. Source Files

| File | What It Contains |
|------|-----------------|
| `Services/PayrollServices.cs` | Core calculation engine (all formulas) |
| `Services/GovernmentServices.cs` | SSS bracket lookup, PhilHealth, Pag-IBIG |
| `Services/TaxServices.cs` | Progressive tax brackets |
| `Services/AttendanceService.cs` | Night shift hours, late/undertime calculation |
| `Models/Payroll.cs` | Payroll entity + validation |
| `Models/Earning.cs` | Earning line items + enum |
| `Models/Deduction.cs` | Deduction line items + enum |
| `Models/Attendance.cs` | Attendance record fields |
| `Models/Overtime.cs` | Overtime request fields |
| `Models/Shift.cs` | Shift definition fields |
| `Models/SSSContribution.cs` | SSS bracket entity |
| `Controllers/PayrollController.cs` | HTTP endpoints, validation, flow |
| `Data/Seed/SeedGovernmentData.cs` | SSS bracket seed data |
