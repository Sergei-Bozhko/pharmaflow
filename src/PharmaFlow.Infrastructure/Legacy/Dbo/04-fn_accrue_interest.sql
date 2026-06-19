-- ============================================================================
-- fn_accrue_interest — #3 of 4 (time-and-money: the one naive ports get wrong).
--
-- Daily interest accrual on an account over [p_from, p_to] inclusive:
--   * For each day, take the VALUE-dated balance.
--   * Pick the rate from interest_rate_tier: first the latest rate BATCH whose
--     effective_from <= that day (rates change mid-period), then within that
--     batch the highest balance_floor tier the balance qualifies for.
--   * Daily interest = balance * annual_rate / 365  (ACT/365, fixed 365).
--   * Sum every day; round the TOTAL once at the end.
--
-- Legacy traps the C# port must preserve:
--   1. EFFECTIVE-DATED rates: a rate change inside the window must split the
--      period — days before the break use the old batch, days on/after use the
--      new one. Picking "the current rate" for the whole window is the classic bug.
--   2. TIER BY BALANCE within the active batch: highest qualifying balance_floor
--      wins (ORDER BY balance_floor DESC LIMIT 1), re-evaluated as the balance moves.
--   3. ACT/365 with a FIXED 365 denominator — not 365.25, not actual-days-in-year.
--   4. Round ONCE on the summed total, NOT per day. Per-day rounding drifts.
--   5. Balances below the lowest floor (e.g. negative/overdrawn) accrue nothing
--      (no tier matches -> rate NULL -> 0), never negative interest.
-- ============================================================================

CREATE OR REPLACE FUNCTION dbo.fn_accrue_interest(
    p_account_id bigint,
    p_from       date,
    p_to         date
)
RETURNS TABLE (
    accrual_days   int,
    total_interest numeric
)
LANGUAGE sql
STABLE
AS $$
    WITH acct AS (
        SELECT account_type FROM dbo.account WHERE account_id = p_account_id
    ),
    days AS (
        SELECT generate_series(p_from, p_to, INTERVAL '1 day')::date AS d
    ),
    daily AS (
        SELECT d,
               ( SELECT COALESCE(SUM(t.amount), 0)
                 FROM dbo.account_txn t
                 WHERE t.account_id = p_account_id
                   AND t.value_date <= d ) AS bal
        FROM days
    ),
    rated AS (
        SELECT dd.d, dd.bal,
            ( SELECT r.annual_rate
              FROM dbo.interest_rate_tier r
              JOIN acct ON r.account_type = acct.account_type
              WHERE r.effective_from = (
                        SELECT MAX(r2.effective_from)
                        FROM dbo.interest_rate_tier r2
                        JOIN acct a2 ON r2.account_type = a2.account_type
                        WHERE r2.effective_from <= dd.d )
                AND r.balance_floor <= dd.bal
              ORDER BY r.balance_floor DESC
              LIMIT 1 ) AS rate
        FROM daily dd
    )
    SELECT COUNT(*)::int,
           ROUND(COALESCE(SUM(bal * COALESCE(rate, 0) / 365.0), 0), 2)
    FROM rated;
$$;
