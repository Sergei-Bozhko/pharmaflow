-- ============================================================================
-- Seed for the legacy dbo schema. Deterministic ids so differential tests can
-- reference rows directly. Re-runnable: truncates first.
-- ============================================================================

TRUNCATE
    dbo.loan_payment_alloc, dbo.loan_payment, dbo.loan_installment, dbo.loan,
    dbo.account_txn, dbo.account, dbo.interest_rate_tier, dbo.fee_schedule, dbo.customer
RESTART IDENTITY CASCADE;

-- ---------------------------------------------------------------------------
-- Customers
-- ---------------------------------------------------------------------------
INSERT INTO dbo.customer (customer_id, full_name, segment, created_at) OVERRIDING SYSTEM VALUE VALUES
    (1, 'Robert Vance',       'RETAIL',  '2023-11-02T09:00:00Z'),
    (2, 'Acme Logistics Ltd', 'SME',     '2022-04-18T09:00:00Z'),
    (3, 'Eleanor Whitfield',  'PRIVATE', '2021-01-09T09:00:00Z');

-- ---------------------------------------------------------------------------
-- Accounts
-- ---------------------------------------------------------------------------
INSERT INTO dbo.account (account_id, customer_id, account_no, account_type, currency, status, opened_on) OVERRIDING SYSTEM VALUE VALUES
    (101, 1, 'CHK-0000101', 'CHECKING', 'USD', 'OPEN', '2024-01-15'),
    (102, 2, 'SAV-0000102', 'SAVINGS',  'USD', 'OPEN', '2023-06-01'),
    (103, 3, 'SAV-0000103', 'SAVINGS',  'USD', 'OPEN', '2024-03-20');

-- ---------------------------------------------------------------------------
-- Transactions on account 101 (checking) — drives fn_account_statement.
-- Note the posting/value gaps:
--   * txn 5 is BOOKED 2025-01-31 but VALUE-DATED 2025-02-03 (weekend cut-off).
--   * txn 7 is BOOKED 2025-02-28 but VALUE-DATED 2025-03-03 — so a value-dated
--     statement through 2025-02-28 must EXCLUDE it even though it posted in Feb.
-- txn 1 (Dec 2024) is the opening position before any 2025 statement window.
-- ---------------------------------------------------------------------------
INSERT INTO dbo.account_txn (txn_id, account_id, posted_at, value_date, amount, txn_type, description) OVERRIDING SYSTEM VALUE VALUES
    (1, 101, '2024-12-20T10:15:00Z', '2024-12-20',  1000.0000, 'DEPOSIT',    'Opening deposit'),
    (2, 101, '2025-01-05T08:30:00Z', '2025-01-05',  -200.0000, 'WITHDRAWAL', 'ATM cash'),
    (3, 101, '2025-01-15T14:00:00Z', '2025-01-15',   500.0000, 'DEPOSIT',    'Payroll'),
    (4, 101, '2025-01-20T11:05:00Z', '2025-01-20',   -49.9900, 'WITHDRAWAL', 'Card purchase'),
    (5, 101, '2025-01-31T23:50:00Z', '2025-02-03',   -12.0000, 'FEE',        'Monthly maintenance fee'),
    (6, 101, '2025-02-10T09:45:00Z', '2025-02-10',  -120.0000, 'WITHDRAWAL', 'Utility bill'),
    (7, 101, '2025-02-28T19:20:00Z', '2025-03-03',  2000.0000, 'DEPOSIT',    'Quarter-end bonus'),
    (8, 101, '2025-03-10T12:00:00Z', '2025-03-10',   -50.0000, 'WITHDRAWAL', 'Card purchase');

-- Some movement on the savings account 102 — used by fee/interest functions.
INSERT INTO dbo.account_txn (account_id, posted_at, value_date, amount, txn_type, description) VALUES
    (102, '2025-01-02T09:00:00Z', '2025-01-02', 20000.0000, 'DEPOSIT',  'Initial funding'),
    (102, '2025-01-20T09:00:00Z', '2025-01-20', -5000.0000, 'TRANSFER', 'Transfer out'),
    (102, '2025-02-15T09:00:00Z', '2025-02-15',  3000.0000, 'DEPOSIT',  'Top-up');

-- ---------------------------------------------------------------------------
-- Fee schedule (fn_assess_monthly_fees)
-- ---------------------------------------------------------------------------
INSERT INTO dbo.fee_schedule (account_type, monthly_fee, min_avg_balance, free_txn_count, per_txn_fee) VALUES
    ('CHECKING', 12.0000, 1500.0000, 5, 0.5000),
    ('SAVINGS',   0.0000, 0.0000,    9, 0.0000);

-- ---------------------------------------------------------------------------
-- Tiered, effective-dated interest (fn_accrue_interest)
-- SAVINGS rates step up at 10k; a rate change lands 2025-02-01 mid-period.
-- ---------------------------------------------------------------------------
INSERT INTO dbo.interest_rate_tier (account_type, effective_from, balance_floor, annual_rate) VALUES
    ('SAVINGS', '2024-01-01',      0.0000, 0.005000),   -- 0.50% base
    ('SAVINGS', '2024-01-01',  10000.0000, 0.015000),   -- 1.50% above 10k
    ('SAVINGS', '2025-02-01',      0.0000, 0.007500),   -- 0.75% base from Feb
    ('SAVINGS', '2025-02-01',  10000.0000, 0.020000);   -- 2.00% above 10k from Feb

-- ---------------------------------------------------------------------------
-- A loan with overdue installments (fn_apply_payment_waterfall)
-- 12,000 @ 12% p.a., 6 monthly installments from 2024-10. As of early 2025 the
-- first three are overdue (one partially paid), the rest still future-dated.
-- ---------------------------------------------------------------------------
INSERT INTO dbo.loan (loan_id, customer_id, principal, annual_rate, day_count, penalty_rate, late_fee, opened_on, status) OVERRIDING SYSTEM VALUE VALUES
    (9001, 2, 12000.0000, 0.120000, 'ACT/365', 0.180000, 25.0000, '2024-09-01', 'ACTIVE');

INSERT INTO dbo.loan_installment
    (installment_id, loan_id, seq_no, due_date, principal_due, interest_due, fee_due,
     principal_paid, interest_paid, fee_paid, penalty_accrued, penalty_paid, last_accrued_on, status)
OVERRIDING SYSTEM VALUE VALUES
    (1, 9001, 1, '2024-10-01', 2000.0000, 120.0000, 0.0000,  2000.0000, 120.0000, 0.0000, 0.0000, 0.0000, '2024-10-01', 'PAID'),
    (2, 9001, 2, '2024-11-01', 2000.0000, 100.0000, 0.0000,   500.0000,   0.0000, 0.0000, 0.0000, 0.0000, NULL,         'PARTIAL'),
    (3, 9001, 3, '2024-12-01', 2000.0000,  80.0000, 0.0000,     0.0000,   0.0000, 0.0000, 0.0000, 0.0000, NULL,         'DUE'),
    (4, 9001, 4, '2025-01-01', 2000.0000,  60.0000, 0.0000,     0.0000,   0.0000, 0.0000, 0.0000, 0.0000, NULL,         'DUE'),
    (5, 9001, 5, '2025-02-01', 2000.0000,  40.0000, 0.0000,     0.0000,   0.0000, 0.0000, 0.0000, 0.0000, NULL,         'DUE'),
    (6, 9001, 6, '2025-03-01', 2000.0000,  20.0000, 0.0000,     0.0000,   0.0000, 0.0000, 0.0000, 0.0000, NULL,         'DUE');

SELECT setval(pg_get_serial_sequence('dbo.customer',          'customer_id'),    100, true);
SELECT setval(pg_get_serial_sequence('dbo.account',           'account_id'),     200, true);
SELECT setval(pg_get_serial_sequence('dbo.account_txn',       'txn_id'),        1000, true);
SELECT setval(pg_get_serial_sequence('dbo.loan',              'loan_id'),      10000, true);
SELECT setval(pg_get_serial_sequence('dbo.loan_installment',  'installment_id'),1000, true);
