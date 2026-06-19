-- ============================================================================
-- fn_apply_payment_waterfall — #4 of 4 (the beast: real 10-year-old bank proc).
--
-- Allocates an incoming loan payment across overdue installments. For each
-- overdue installment, OLDEST FIRST, it accrues penalty interest to the payment
-- date, assesses a one-off late fee, then pours the remaining payment down a
-- per-installment WATERFALL: FEE -> PENALTY -> INTEREST -> PRINCIPAL. It resolves
-- one installment fully before moving to the next. Anything left after all overdue
-- installments lands in an UNAPPLIED (suspense) line.
--
-- This is a pure calculation: it READS the loan tables and RETURNS the allocation
-- breakdown. It does not persist (no UPDATE / no INSERT into loan_payment_alloc) —
-- persistence is the caller's job and out of scope for the logic port. Written in
-- procedural plpgsql (FOR-loop cursor, per-row CASE) to mirror the legacy shape.
--
-- Legacy traps the C# port must preserve:
--   1. OLDEST-FIRST, FULL per-installment waterfall — resolve installment N's
--      fee/penalty/interest/principal before touching N+1. Not "all fees across
--      all installments, then all penalties".
--   2. PENALTY ACCRUAL = unpaid (penalty_accrued - penalty_paid) PLUS new accrual
--      on the overdue base (unpaid principal + unpaid interest) at penalty_rate,
--      ACT/365, from COALESCE(last_accrued_on, due_date) to the payment date.
--      Rounded to 2dp PER INSTALLMENT (not at the end).
--   3. LATE FEE assessed once: if no fee is on the installment yet (fee_due = 0),
--      add loan.late_fee into the fee bucket.
--   4. Each bucket is capped by what's outstanding (LEAST) and only emitted when
--      > 0. Overdue test is due_date <= payment_date; PAID installments skipped.
--   5. Half-away-from-zero rounding on the penalty (see fn #2 note re: Math.Round).
-- ============================================================================

CREATE OR REPLACE FUNCTION dbo.fn_apply_payment_waterfall(
    p_loan_id        bigint,
    p_payment_amount numeric,
    p_payment_date   date
)
RETURNS TABLE (
    seq_no         int,
    installment_id bigint,
    bucket         text,
    amount         numeric
)
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_remaining    numeric(19,4) := p_payment_amount;
    v_penalty_rate numeric(9,6);
    v_late_fee     numeric(19,4);
    r              RECORD;
    v_prin_out     numeric(19,4);
    v_int_out      numeric(19,4);
    v_fee_out      numeric(19,4);
    v_pen_out      numeric(19,4);
    v_overdue_base numeric(19,4);
    v_accrue_from  date;
    v_days         int;
    v_new_penalty  numeric(19,4);
    v_pay          numeric(19,4);
BEGIN
    SELECT penalty_rate, late_fee
      INTO v_penalty_rate, v_late_fee
      FROM dbo.loan
     WHERE loan_id = p_loan_id;

    FOR r IN
        SELECT *
          FROM dbo.loan_installment
         WHERE loan_id = p_loan_id
           AND due_date <= p_payment_date
           AND status <> 'PAID'
         ORDER BY seq_no
    LOOP
        EXIT WHEN v_remaining <= 0;

        v_prin_out := r.principal_due - r.principal_paid;
        v_int_out  := r.interest_due  - r.interest_paid;

        -- penalty: carried unpaid + freshly accrued on the overdue base
        v_overdue_base := v_prin_out + v_int_out;
        v_accrue_from  := COALESCE(r.last_accrued_on, r.due_date);
        v_days         := GREATEST(0, p_payment_date - v_accrue_from);
        v_new_penalty  := ROUND(v_overdue_base * v_penalty_rate * v_days / 365.0, 2);
        v_pen_out      := (r.penalty_accrued - r.penalty_paid) + v_new_penalty;

        -- late fee: assessed once (none booked yet)
        v_fee_out := (r.fee_due - r.fee_paid)
                   + CASE WHEN r.fee_due = 0 THEN v_late_fee ELSE 0 END;

        -- waterfall: FEE -> PENALTY -> INTEREST -> PRINCIPAL
        v_pay := LEAST(v_remaining, v_fee_out);
        IF v_pay > 0 THEN
            seq_no := r.seq_no; installment_id := r.installment_id; bucket := 'FEE'; amount := v_pay;
            RETURN NEXT;
            v_remaining := v_remaining - v_pay;
        END IF;

        v_pay := LEAST(v_remaining, v_pen_out);
        IF v_pay > 0 THEN
            seq_no := r.seq_no; installment_id := r.installment_id; bucket := 'PENALTY'; amount := v_pay;
            RETURN NEXT;
            v_remaining := v_remaining - v_pay;
        END IF;

        v_pay := LEAST(v_remaining, v_int_out);
        IF v_pay > 0 THEN
            seq_no := r.seq_no; installment_id := r.installment_id; bucket := 'INTEREST'; amount := v_pay;
            RETURN NEXT;
            v_remaining := v_remaining - v_pay;
        END IF;

        v_pay := LEAST(v_remaining, v_prin_out);
        IF v_pay > 0 THEN
            seq_no := r.seq_no; installment_id := r.installment_id; bucket := 'PRINCIPAL'; amount := v_pay;
            RETURN NEXT;
            v_remaining := v_remaining - v_pay;
        END IF;
    END LOOP;

    IF v_remaining > 0 THEN
        seq_no := NULL; installment_id := NULL; bucket := 'UNAPPLIED'; amount := v_remaining;
        RETURN NEXT;
    END IF;

    RETURN;
END;
$$;
