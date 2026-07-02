-- ============================================================================
-- fn_account_statement — #1 of 4 (warm-up, but a real legacy pattern).
--
-- Returns the statement lines for an account within a VALUE-DATE window, each
-- with a RUNNING BALANCE. Written the way it was before window functions existed:
-- the running balance is a CORRELATED SUBQUERY that re-sums every prior txn for
-- each row — O(n^2). The opening balance (everything value-dated before the
-- window) is folded into that same correlated sum, so line 1 already carries it.
--
-- Legacy traps the C# port must preserve:
--   1. VALUE DATE drives inclusion and ordering, NOT posted_at. A txn posted
--      inside the window but value-dated after it does NOT appear (and vice versa).
--   2. Tie-break on (value_date, txn_id) so same-day txns have a stable order and
--      a deterministic running balance.
--   3. The running balance includes pre-window history (opening balance), it does
--      not restart at zero on p_from.
--
-- C# port target: pull the window's rows ordered once, carry opening balance as a
-- seed, accumulate in a single pass — O(n). The differential test asserts the C#
-- output equals this function's output row-for-row.
-- ============================================================================

CREATE OR REPLACE FUNCTION dbo.fn_account_statement(
    p_account_id bigint,
    p_from       date,
    p_to         date
)
RETURNS TABLE (
    txn_id          bigint,
    posted_at       timestamptz,
    value_date      date,
    amount          numeric,
    txn_type        text,
    description     text,
    running_balance numeric
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        t.txn_id,
        t.posted_at,
        t.value_date,
        t.amount,
        t.txn_type::text,
        t.description::text,
        (
            SELECT COALESCE(SUM(p.amount), 0)
            FROM dbo.account_txn p
            WHERE p.account_id = t.account_id
              AND (
                    p.value_date < t.value_date
                 OR (p.value_date = t.value_date AND p.txn_id <= t.txn_id)
              )
        ) AS running_balance
    FROM dbo.account_txn t
    WHERE t.account_id = p_account_id
      AND t.value_date >= p_from
      AND t.value_date <= p_to
    ORDER BY t.value_date, t.txn_id;
$$;
