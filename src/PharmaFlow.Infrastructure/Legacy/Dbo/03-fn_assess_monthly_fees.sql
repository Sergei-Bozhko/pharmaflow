-- ============================================================================
-- fn_assess_monthly_fees — #2 of 4 (tiered, conditional business logic).
--
-- Computes the monthly fee for an account over a calendar month:
--   * AVERAGE DAILY BALANCE (ADB): the value-dated balance at the end of each
--     day of the month, averaged. Originally a WHILE-loop over days; here a
--     generate_series day list with a correlated balance subquery.
--   * WAIVER: if ADB >= fee_schedule.min_avg_balance the monthly fee is waived.
--   * EXCESS-TRANSACTION FEE: billable txns (customer-initiated debits:
--     WITHDRAWAL / TRANSFER, counted by POSTING date) beyond free_txn_count are
--     charged per_txn_fee each.
--   * total_fee = (waived ? 0 : monthly_fee) + excess_txn_fee.
--
-- Legacy traps the C# port must preserve:
--   1. ADB uses VALUE-dated balances per day; the excess-txn count uses POSTING
--      date. Two different date semantics in one function — easy to conflate.
--   2. Rounding is HALF-AWAY-FROM-ZERO (T-SQL ROUND), to 2dp on the money parts.
--      C#'s Math.Round defaults to BANKER'S (to-even) — a naive port silently
--      diverges on .xx5 cases. Use MidpointRounding.AwayFromZero.
--   3. The free-transaction allowance is a floor at zero (GREATEST(0, ...)) — a
--      light month must never produce a negative fee.
-- ============================================================================

CREATE OR REPLACE FUNCTION dbo.fn_assess_monthly_fees(
    p_account_id   bigint,
    p_period_start date          -- first day of the billing month
)
RETURNS TABLE (
    avg_daily_balance numeric,
    fee_waived        boolean,
    monthly_fee       numeric,
    billable_txns     int,
    free_txns         int,
    excess_txn_fee    numeric,
    total_fee         numeric
)
LANGUAGE sql
STABLE
AS $$
    WITH bounds AS (
        SELECT p_period_start AS d0,
               (p_period_start + INTERVAL '1 month' - INTERVAL '1 day')::date AS d1
    ),
    acct AS (
        SELECT a.account_type FROM dbo.account a WHERE a.account_id = p_account_id
    ),
    sched AS (
        SELECT f.* FROM dbo.fee_schedule f JOIN acct ON f.account_type = acct.account_type
    ),
    days AS (
        SELECT generate_series(b.d0, b.d1, INTERVAL '1 day')::date AS d FROM bounds b
    ),
    daily_balance AS (
        SELECT d,
               ( SELECT COALESCE(SUM(t.amount), 0)
                 FROM dbo.account_txn t
                 WHERE t.account_id = p_account_id
                   AND t.value_date <= d ) AS bal
        FROM days
    ),
    adb AS ( SELECT AVG(bal) AS avg_bal FROM daily_balance ),
    txns AS (
        SELECT COUNT(*)::int AS cnt
        FROM dbo.account_txn t, bounds b
        WHERE t.account_id = p_account_id
          AND t.posted_at::date BETWEEN b.d0 AND b.d1
          AND t.txn_type IN ('WITHDRAWAL', 'TRANSFER')
    )
    SELECT
        ROUND(adb.avg_bal, 4),
        (adb.avg_bal >= sched.min_avg_balance),
        CASE WHEN adb.avg_bal >= sched.min_avg_balance THEN 0 ELSE sched.monthly_fee END,
        txns.cnt,
        sched.free_txn_count,
        ROUND(GREATEST(0, txns.cnt - sched.free_txn_count) * sched.per_txn_fee, 2),
        ROUND( (CASE WHEN adb.avg_bal >= sched.min_avg_balance THEN 0 ELSE sched.monthly_fee END)
               + GREATEST(0, txns.cnt - sched.free_txn_count) * sched.per_txn_fee, 2)
    FROM adb, sched, txns;
$$;
