SET NOCOUNT ON;
DECLARE @Fail INT = 0;
DECLARE @Biz DATE = '2026-11-16';   -- 월요일, 휴무일 아님

-- RUL-T01 08:59:59.9999999 → 309
IF ((SELECT WorkCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 08:59:59.9999999'), @Biz, 'AM', 'NONE')) = 309)
    PRINT 'PASS RUL-T01 08:59:59 업무불가 309';
ELSE BEGIN PRINT 'FAIL RUL-T01'; SET @Fail += 1; END

-- RUL-T02 09:00:00 → CanWorkNow=1
IF ((SELECT CanWorkNow FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 09:00:00.0000000'), @Biz, 'AM', 'NONE')) = 1)
    PRINT 'PASS RUL-T02 09:00:00 업무가능';
ELSE BEGIN PRINT 'FAIL RUL-T02'; SET @Fail += 1; END

-- RUL-T03 17:59:59.9999999 → CanWorkNow=1
IF ((SELECT CanWorkNow FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 17:59:59.9999999'), @Biz, 'AM', 'NONE')) = 1)
    PRINT 'PASS RUL-T03 17:59:59 업무가능';
ELSE BEGIN PRINT 'FAIL RUL-T03'; SET @Fail += 1; END

-- RUL-T04 18:00:00 → 309
IF ((SELECT WorkCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 18:00:00.0000000'), @Biz, 'AM', 'NONE')) = 309)
    PRINT 'PASS RUL-T04 18:00:00 업무불가 309';
ELSE BEGIN PRINT 'FAIL RUL-T04'; SET @Fail += 1; END

-- RUL-T05/T06 NORMAL AM 마감 10:00
IF ((SELECT CutoffPassed FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 09:59:59.9999999'), @Biz, 'AM', 'NORMAL')) = 0)
    PRINT 'PASS RUL-T05 09:59:59 NORMAL AM 마감 전';
ELSE BEGIN PRINT 'FAIL RUL-T05'; SET @Fail += 1; END
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:00:00.0000000'), @Biz, 'AM', 'NORMAL')) = 304)
    PRINT 'PASS RUL-T06 10:00:00 NORMAL AM 304';
ELSE BEGIN PRINT 'FAIL RUL-T06'; SET @Fail += 1; END

-- RUL-T07/T08 RECEPTION AM 마감 11:00
IF ((SELECT CutoffPassed FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:59:59.9999999'), @Biz, 'AM', 'RECEPTION')) = 0)
    PRINT 'PASS RUL-T07 10:59:59 RECEPTION AM 마감 전';
ELSE BEGIN PRINT 'FAIL RUL-T07'; SET @Fail += 1; END
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 11:00:00.0000000'), @Biz, 'AM', 'RECEPTION')) = 304)
    PRINT 'PASS RUL-T08 11:00:00 RECEPTION AM 304';
ELSE BEGIN PRINT 'FAIL RUL-T08'; SET @Fail += 1; END

-- RUL-T09 14:59:59.9999999 + NORMAL/PM → CutoffPassed=0
IF ((SELECT CutoffPassed FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 14:59:59.9999999'), @Biz, 'PM', 'NORMAL')) = 0)
    PRINT 'PASS RUL-T09 14:59:59 NORMAL PM 마감 전';
ELSE BEGIN PRINT 'FAIL RUL-T09'; SET @Fail += 1; END

-- RUL-T10 15:00:00 + NORMAL/PM → ReasonCode=304
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 15:00:00.0000000'), @Biz, 'PM', 'NORMAL')) = 304)
    PRINT 'PASS RUL-T10 15:00:00 NORMAL PM 304';
ELSE BEGIN PRINT 'FAIL RUL-T10'; SET @Fail += 1; END

-- RUL-T11 15:59:59.9999999 + RECEPTION/PM → CutoffPassed=0
IF ((SELECT CutoffPassed FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 15:59:59.9999999'), @Biz, 'PM', 'RECEPTION')) = 0)
    PRINT 'PASS RUL-T11 15:59:59 RECEPTION PM 마감 전';
ELSE BEGIN PRINT 'FAIL RUL-T11'; SET @Fail += 1; END

-- RUL-T12 16:00:00 + RECEPTION/PM → ReasonCode=304
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 16:00:00.0000000'), @Biz, 'PM', 'RECEPTION')) = 304)
    PRINT 'PASS RUL-T12 16:00:00 RECEPTION PM 304';
ELSE BEGIN PRINT 'FAIL RUL-T12'; SET @Fail += 1; END

-- ── 일정 경계 RUL-D01~D09 ────────────────────────────────────────────────────
-- 기준시각은 업무시간 안의 고정값 2026-11-16 10:30:00 을 쓴다.
-- 실행 시각에 의존하면 309 가 먼저 걸려 일정 판정에 도달하지 못한다.
-- 요일 실측: 2020-01-05 일 · 2020-01-06 월 · 2026-11-17 화 · 2026-11-21 토 · 2026-11-22 일
--            2026-12-25 금 · 2026-12-26 토   (DATEDIFF(DAY,0,d)%7 → 0=월 … 6=일)

IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:30:00'), '2020-01-06', 'AM', 'NONE')) = 300)
    PRINT 'PASS RUL-D01 과거 평일 300';
ELSE BEGIN PRINT 'FAIL RUL-D01'; SET @Fail += 1; END

IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:30:00'), '2020-01-05', 'AM', 'NONE')) = 300)
    PRINT 'PASS RUL-D02 과거 일요일 300 (과거가 일요일보다 우선)';
ELSE BEGIN PRINT 'FAIL RUL-D02'; SET @Fail += 1; END

IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:30:00'), '2026-11-22', 'AM', 'NONE')) = 301)
    PRINT 'PASS RUL-D03 미래 일요일 301';
ELSE BEGIN PRINT 'FAIL RUL-D03'; SET @Fail += 1; END

IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:30:00'), '2026-12-25', 'AM', 'NONE')) = 302)
    PRINT 'PASS RUL-D04 활성 평일 휴무일 302';
ELSE BEGIN PRINT 'FAIL RUL-D04'; SET @Fail += 1; END

IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:30:00'), '2026-12-26', 'AM', 'NONE')) = 302)
    PRINT 'PASS RUL-D05 활성 토요일 휴무일 302 (휴무일이 토요일 규칙보다 우선)';
ELSE BEGIN PRINT 'FAIL RUL-D05'; SET @Fail += 1; END

IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:30:00'), '2026-11-21', 'AM', 'NONE')) = 0)
    PRINT 'PASS RUL-D06 미래 토요일 오전 운영';
ELSE BEGIN PRINT 'FAIL RUL-D06'; SET @Fail += 1; END

IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:30:00'), '2026-11-21', 'PM', 'NONE')) = 303)
    PRINT 'PASS RUL-D07 미래 토요일 오후 미운영 303';
ELSE BEGIN PRINT 'FAIL RUL-D07'; SET @Fail += 1; END

-- 토요일 오후는 마감시각이 존재하지 않는다. RawCutoff 가 요일을 보지 않으므로
-- CutoffTime 이 NULL 인지 함께 본다 (05 §6.1.3 "마감 적용 시각이 있을 때만 반환").
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:30:00'), '2026-11-17', 'AM', 'NONE')) = 0)
    PRINT 'PASS RUL-D08 정상 평일 0';
ELSE BEGIN PRINT 'FAIL RUL-D08'; SET @Fail += 1; END

-- RUL-D09 DATEFIRST 비종속. 같은 인자를 DATEFIRST 1 과 7 에서 각각 돌린다.
SET DATEFIRST 1;
DECLARE @Sun1 INT = (SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:00:00'), CONVERT(DATE,'2026-11-22'), 'AM', 'NONE'));
SET DATEFIRST 7;
DECLARE @Sun7 INT = (SELECT ReasonCode FROM [dbo].[UFN_HC_일정확인](
        CONVERT(DATETIME2(7), '2026-11-16 10:00:00'), CONVERT(DATE,'2026-11-22'), 'AM', 'NONE'));
IF @Sun1 = 301 AND @Sun7 = 301
    PRINT 'PASS RUL-D09 DATEFIRST 비종속 확인';
ELSE BEGIN PRINT 'FAIL RUL-D09 DATEFIRST 종속성 발견'; SET @Fail += 1; END

-- ── TGT 경계 RUL-G01~G08 (스펙 §35.3) ────────────────────────────────────────
-- 기준 예약일은 §15.5 의 Rule Test 기준일 2026-10-01 이다.
DECLARE @Ref DATE = '2026-10-01';
DECLARE @P BIGINT;

SELECT @P = [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = 'T001';   -- 만 19세
IF ((SELECT ReasonCode FROM [dbo].[UFN_HC_검진대상확인](@P, @Ref)) = 400)
    PRINT 'PASS RUL-G01 만 19세 400 UnderAge';
ELSE BEGIN PRINT 'FAIL RUL-G01'; SET @Fail += 1; END

SELECT @P = [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = 'T002';   -- 만 20세
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@P, @Ref)) = 1)
    PRINT 'PASS RUL-G02 만 20세 대상';
ELSE BEGIN PRINT 'FAIL RUL-G02'; SET @Fail += 1; END

-- RUL-G03 ~ RUL-G07  완료이력 판정 (스펙 §35.3)
--   T005(완료이력 없음) / T016(2025-05-01, 1년차) / T004(2024-05-01, 2년차) 를 쓴다.
DECLARE @Pg BIGINT;

SELECT @Pg = [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = 'T005';
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2026-10-01')) = 1
    AND (SELECT LastCheckupDate FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2026-10-01')) IS NULL)
    PRINT 'PASS RUL-G03 완료이력 없음 → Eligible=1, LastCheckupDate NULL';
ELSE BEGIN PRINT 'FAIL RUL-G03'; SET @Fail += 1; END

SELECT @Pg = [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = 'T016';
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2026-10-01')) = 0
    AND (SELECT ReasonCode FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2026-10-01')) = 401)
    PRINT 'PASS RUL-G04 1년차 완료이력 → 401 NotDue';
ELSE BEGIN PRINT 'FAIL RUL-G04'; SET @Fail += 1; END

SELECT @Pg = [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = 'T004';
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2026-10-01')) = 1)
    PRINT 'PASS RUL-G05 2년차 완료이력 → 대상';
ELSE BEGIN PRINT 'FAIL RUL-G05'; SET @Fail += 1; END

-- RUL-G06 완료일 = 예약일 당일 → 완료이력으로 쓰지 않는다 (T016 의 완료일을 예약일로 준다)
SELECT @Pg = [PatientId] FROM [dbo].[수검자] WHERE [ChartNo] = 'T016';
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2025-05-01')) = 1)
    PRINT 'PASS RUL-G06 완료일 당일은 완료이력으로 사용하지 않는다';
ELSE BEGIN PRINT 'FAIL RUL-G06'; SET @Fail += 1; END

-- RUL-G07 완료일 > 예약일 → 완료이력으로 쓰지 않는다
IF ((SELECT Eligible FROM [dbo].[UFN_HC_검진대상확인](@Pg, '2025-04-30')) = 1)
    PRINT 'PASS RUL-G07 예약일 이후의 완료일은 완료이력으로 사용하지 않는다';
ELSE BEGIN PRINT 'FAIL RUL-G07'; SET @Fail += 1; END

-- RUL-G08 존재하지 않는 PatientId → 0행
IF ((SELECT COUNT(*) FROM [dbo].[UFN_HC_검진대상확인](-1, @Ref)) = 0)
    PRINT 'PASS RUL-G08 미존재 Patient 0행';
ELSE BEGIN PRINT 'FAIL RUL-G08'; SET @Fail += 1; END

IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
GO
