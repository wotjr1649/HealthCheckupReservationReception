SET QUOTED_IDENTIFIER ON;   -- 01_Schema.sql 과 같은 설정으로 객체를 만든다 (CLAUDE.md §6)
GO
SET NOCOUNT ON;
GO
-- Inline TVF 는 단일 SELECT 여야 하고 CASE 결과를 다음 단계에서 재사용해야 하므로 중첩 derived table 을 쓴다.
-- SYSDATETIME() 을 함수 안에서 호출하지 않는다 — 호출 SP 가 캡처한 @ServerTime 만 쓴다.
-- SET DATEFIRST 에 의존하지 않는다 — DATEDIFF(DAY,0,d)%7 은 0=월 … 6=일 로 세션 설정과 무관하다.
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_일정확인]
(
    @ServerTime      DATETIME2(7),
    @ReservationDate DATE,
    @TimeSlot        CHAR(2),
    @CutoffType      VARCHAR(10)
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          CanWorkNow    = CONVERT(BIT, CASE WHEN d.TodayBiz = 1 AND d.WithinHours = 1 THEN 1 ELSE 0 END)
        , WorkCode      = CONVERT(INT, CASE WHEN d.TodayBiz = 0 THEN 308
                                            WHEN d.WithinHours = 0 THEN 309 ELSE 0 END)
        , WorkMessage   = CONVERT(NVARCHAR(300),
                            CASE WHEN d.TodayBiz = 0    THEN N'오늘은 업무일이 아닙니다.'
                                 WHEN d.WithinHours = 0 THEN N'현재는 업무 운영시간이 아닙니다.'
                                 ELSE N'' END)
        , IsBusinessDay = CONVERT(BIT, d.ReqBiz)
        , HolidayName   = CONVERT(NVARCHAR(100), d.ReqHoliday)
        , IsOpen        = CONVERT(BIT, CASE WHEN d.ReqBiz = 1 AND d.SlotOpen = 1 THEN 1 ELSE 0 END)
        , CutoffTime    = CONVERT(TIME(0), CASE WHEN d.SlotOpen = 0 THEN NULL ELSE d.Cutoff END)
        , CutoffPassed  = CONVERT(BIT, CASE WHEN d.Cutoff IS NOT NULL
                                             AND d.NowTime >= CONVERT(TIME(7), d.Cutoff) THEN 1 ELSE 0 END)
        , CanUse        = CONVERT(BIT, CASE WHEN d.ReasonCode = 0 THEN 1 ELSE 0 END)
        , ReasonCode    = CONVERT(INT, d.ReasonCode)
        , ReasonMessage = CONVERT(NVARCHAR(300),
                            CASE d.ReasonCode
                                WHEN 300 THEN N'과거 날짜는 예약할 수 없습니다.'
                                WHEN 301 THEN N'일요일은 업무일이 아닙니다.'
                                WHEN 302 THEN N'선택한 날짜는 휴무일입니다.'
                                WHEN 303 THEN N'선택한 시간대는 운영하지 않습니다.'
                                WHEN 304 THEN N'해당 시간대의 마감시간이 지났습니다.'
                                ELSE N'' END)
    FROM
    (
        SELECT c.*
             , ReasonCode = CASE WHEN @ReservationDate < c.Today                                   THEN 300
                                 WHEN c.ReqDow = 6                                                  THEN 301
                                 WHEN c.ReqHoliday IS NOT NULL                                      THEN 302
                                 WHEN c.SlotOpen = 0                                                THEN 303
                                 WHEN c.Cutoff IS NOT NULL
                                  AND c.NowTime >= CONVERT(TIME(7), c.Cutoff)                       THEN 304
                                 ELSE 0 END
        FROM
        (
            SELECT b.*
                 , TodayBiz   = CASE WHEN b.TodayDow <> 6 AND b.TodayHoliday IS NULL THEN 1 ELSE 0 END
                 , WithinHours= CASE WHEN b.NowTime >= CONVERT(TIME(7), '09:00:00')
                                      AND b.NowTime <  CONVERT(TIME(7), '18:00:00') THEN 1 ELSE 0 END
                 , ReqBiz     = CASE WHEN b.ReqDow <> 6 AND b.ReqHoliday IS NULL THEN 1 ELSE 0 END
                 , SlotOpen   = CASE WHEN b.ReqDow = 5 AND @TimeSlot = 'PM' THEN 0 ELSE 1 END
                 , Cutoff     = CASE WHEN @ReservationDate = b.Today THEN b.RawCutoff ELSE NULL END
            FROM
            (
                SELECT
                      Today        = CONVERT(DATE, @ServerTime)
                    , NowTime      = CONVERT(TIME(7), @ServerTime)
                    , TodayDow     = DATEDIFF(DAY, 0, CONVERT(DATE, @ServerTime)) % 7
                    , ReqDow       = DATEDIFF(DAY, 0, @ReservationDate) % 7
                    , TodayHoliday = (SELECT TOP (1) h.[휴무일명] FROM [dbo].[휴무일] h
                                       WHERE h.[휴무일자] = CONVERT(DATE, @ServerTime) AND h.[사용여부] = 1)
                    , ReqHoliday   = (SELECT TOP (1) h.[휴무일명] FROM [dbo].[휴무일] h
                                       WHERE h.[휴무일자] = @ReservationDate AND h.[사용여부] = 1)
                    -- 토요일 오후는 00 §3장이 마감을 "해당 없음" 으로 확정했다.
                    -- RawCutoff 는 요일을 보지 않으므로 CutoffTime 을 SlotOpen=0 에서 NULL 로 덮는다.
                    , RawCutoff    = CASE WHEN @CutoffType = 'NORMAL'    AND @TimeSlot = 'AM' THEN CONVERT(TIME(0), '10:00:00')
                                          WHEN @CutoffType = 'NORMAL'    AND @TimeSlot = 'PM' THEN CONVERT(TIME(0), '15:00:00')
                                          WHEN @CutoffType = 'RECEPTION' AND @TimeSlot = 'AM' THEN CONVERT(TIME(0), '11:00:00')
                                          WHEN @CutoffType = 'RECEPTION' AND @TimeSlot = 'PM' THEN CONVERT(TIME(0), '16:00:00')
                                          ELSE NULL END
            ) b
        ) c
    ) d
);
GO
-- 완료이력 범위를 CompletionDate < @ReservationDate 외로 넓히지 않는다.
-- Work 상태(RSV/RCP/CNR/CNC)를 완료이력으로 쓰지 않는다.
-- Patient 가 없으면 0행을 반환한다 (내부 derived table 이 0행이 되어 자연히 그렇게 된다).
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_검진대상확인]
(
    @PatientId       BIGINT,
    @ReservationDate DATE
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          Eligible        = CONVERT(BIT, CASE WHEN x.ReasonCode = 0 THEN 1 ELSE 0 END)
        , Age             = CONVERT(INT, x.Age)
        , LastCheckupDate = CONVERT(DATE, x.LastCheckupDate)
        , ReasonCode      = CONVERT(INT, x.ReasonCode)
        , ReasonMessage   = CONVERT(NVARCHAR(300),
                              CASE x.ReasonCode
                                  WHEN 400 THEN N'예약일 기준 만 20세 미만으로 검진 대상이 아닙니다.'
                                  WHEN 401 THEN N'일반건강검진 2년 주기가 도래하지 않았습니다.'
                                  ELSE N'' END)
    FROM
    (
        SELECT a.Age, a.LastCheckupDate
             , ReasonCode = CASE WHEN a.Age < 20 THEN 400
                                 WHEN a.LastCheckupDate IS NOT NULL
                                  AND (YEAR(@ReservationDate) - YEAR(a.LastCheckupDate)) < 2 THEN 401
                                 ELSE 0 END
        FROM
        (
            SELECT
                  Age = DATEDIFF(YEAR, p.[Birthday_D], @ReservationDate)
                        - CASE WHEN (MONTH(@ReservationDate) * 100 + DAY(@ReservationDate))
                                  < (MONTH(p.[Birthday_D])  * 100 + DAY(p.[Birthday_D])) THEN 1 ELSE 0 END
                , LastCheckupDate = (SELECT TOP (1) h.[완료일자]
                                       FROM [dbo].[완료이력] h
                                      WHERE h.[수검자ID] = @PatientId
                                        AND h.[완료일자] < @ReservationDate
                                      ORDER BY h.[완료일자] DESC)
            FROM (SELECT [Birthday_D] = CONVERT(DATE, i.[Birthday], 112)
                    FROM [dbo].[수검자] i WHERE i.[PatientId] = @PatientId) p
        ) a
    ) x
);
GO
-- TGT 비대상에게는 행을 반환하지 않는다 (t.Eligible = 1 조건).
-- 조건부 술어는 00 §7.2.2 를 그대로 옮긴 것이다. 여기서 바꾸지 않는다.
-- 정렬(ExamCode ASC)은 Inline TVF 에서 ORDER BY 를 쓸 수 없으므로 호출자가 붙인다.
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_국가검사구성]
(
    @PatientId       BIGINT,
    @ReservationDate DATE
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          ExamCode = CONVERT(VARCHAR(10),  m.[ExamItemCode])
        , ExamName = CONVERT(NVARCHAR(100), m.[ExamItemName])
        , ExamType = CONVERT(VARCHAR(12), CASE WHEN m.[NexRuleCode] = 'NEX-01' THEN 'BASIC' ELSE 'CONDITIONAL' END)
        , RuleCode = CONVERT(VARCHAR(10),  m.[NexRuleCode])
    FROM [dbo].[검사코드] m
    CROSS JOIN
    (
        SELECT g.Eligible, g.Age, i.[Gender], i.[HepatitisBExcluded]
        FROM [dbo].[수검자] i
        CROSS APPLY [dbo].[UFN_HC_검진대상확인](i.[PatientId], @ReservationDate) g
        WHERE i.[PatientId] = @PatientId
    ) t
    WHERE m.[NexRuleCode] IS NOT NULL
      AND t.Eligible = 1
      AND
      (
            m.[NexRuleCode] = 'NEX-01'
        OR (m.[NexRuleCode] = 'NEX-02' AND
            (  (t.[Gender] = 'M' AND t.Age >= 24 AND (t.Age - 24) % 4 = 0)
            OR (t.[Gender] = 'F' AND t.Age >= 40 AND (t.Age - 40) % 4 = 0) ))
        OR (m.[NexRuleCode] = 'NEX-03' AND t.Age = 40
            AND t.[HepatitisBExcluded] = 0)
        OR (m.[NexRuleCode] = 'NEX-04' AND t.Age = 56)
        OR (m.[NexRuleCode] = 'NEX-05' AND t.[Gender] = 'F' AND t.Age IN (54, 60, 66))
        OR (m.[NexRuleCode] = 'NEX-06' AND t.Age IN (56, 66))
      )
);
GO
-- 요청 값은 VALUES 행 생성자로만 받는다 (TVP·동적 SQL 은 허용 T-SQL 목록 밖이다).
-- 선택하지 않은 무효 항목은 사유만 표시하고 저장을 막지 않는다 — Requested 와 Selected 를 분리한다.
-- 정렬(OptionCode ASC)은 호출자가 붙인다.
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_추가검사확인]
(
    @PatientId        BIGINT,
    @ReservationDate  DATE,
    @WorkId           BIGINT,
    @UseSavedExams    BIT,
    @AexOpt01Selected BIT, @AexOpt02Selected BIT, @AexOpt03Selected BIT,
    @AexOpt04Selected BIT, @AexOpt05Selected BIT, @AexOpt06Selected BIT,
    @AexOpt07Selected BIT
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          OptionCode    = CONVERT(VARCHAR(10),  b.[AdditionalExamCode])
        , ExamCode      = CONVERT(VARCHAR(10),  b.[ExamItemCode])
        , ExamName      = CONVERT(NVARCHAR(100), b.[ExamItemName])
        , Requested     = CONVERT(BIT, b.Requested)
        , Selected      = CONVERT(BIT, CASE WHEN b.Requested = 1 AND b.ReasonCode = 0 THEN 1 ELSE 0 END)
        , CanSelect     = CONVERT(BIT, CASE WHEN b.ReasonCode = 0 THEN 1 ELSE 0 END)
        , ReasonCode    = CONVERT(INT, b.ReasonCode)
        , ReasonMessage = CONVERT(NVARCHAR(300),
                            CASE b.ReasonCode
                                WHEN 400 THEN N'예약일 기준 만 20세 미만으로 검진 대상이 아닙니다.'
                                WHEN 401 THEN N'일반건강검진 2년 주기가 도래하지 않았습니다.'
                                WHEN 410 THEN N'현재 사용할 수 없는 추가검사입니다.'
                                WHEN 411 THEN N'성별 조건을 충족하지 않는 추가검사입니다.'
                                WHEN 412 THEN N'일반건강검진에 포함된 검사입니다.'
                                ELSE N'' END)
    FROM
    (
        SELECT m.[AdditionalExamCode], m.[ExamItemCode], m.[ExamItemName], r.Requested
             , ReasonCode =
                 CASE
                     WHEN @UseSavedExams = 0 AND t.Eligible = 0 THEN t.ReasonCode      -- 400 / 401
                     WHEN m.[AdditionalActive] = 0                                THEN 410
                     WHEN m.[AdditionalGenderCode] <> 'A'
                      AND m.[AdditionalGenderCode] <> t.[Gender]                  THEN 411
                     WHEN EXISTS
                          (
                              SELECT 1 FROM
                              (
                                  SELECT n.ExamCode FROM [dbo].[UFN_HC_국가검사구성](@PatientId, @ReservationDate) n
                                   WHERE @UseSavedExams = 0
                                  UNION ALL
                                  SELECT d.[ExamItemCode] FROM [dbo].[검사항목] d
                                   WHERE @UseSavedExams = 1 AND d.[WorkId] = @WorkId AND d.[ExamSourceCode] = 'NEX'
                              ) nx WHERE nx.ExamCode = m.[ExamItemCode]
                          )                                                       THEN 412
                     ELSE 0
                 END
        FROM [dbo].[검사코드] m
        CROSS JOIN
        (
            SELECT i.[Gender]
                 , Eligible   = ISNULL(g.Eligible, CONVERT(BIT,0))
                 , ReasonCode = ISNULL(g.ReasonCode, 400)
            FROM [dbo].[수검자] i
            OUTER APPLY [dbo].[UFN_HC_검진대상확인](i.[PatientId], @ReservationDate) g
            WHERE i.[PatientId] = @PatientId
        ) t
        JOIN
        (
            VALUES ('OPT01', @AexOpt01Selected), ('OPT02', @AexOpt02Selected), ('OPT03', @AexOpt03Selected),
                   ('OPT04', @AexOpt04Selected), ('OPT05', @AexOpt05Selected), ('OPT06', @AexOpt06Selected),
                   ('OPT07', @AexOpt07Selected)
        ) r (OptionCode, Requested) ON r.OptionCode = m.[AdditionalExamCode]
        WHERE m.[AdditionalExamCode] IS NOT NULL
    ) b
);
GO
