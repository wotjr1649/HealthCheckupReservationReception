SET QUOTED_IDENTIFIER ON;   -- 01_Schema.sql 과 같은 설정으로 객체를 만든다 (CLAUDE.md §6)
GO
SET NOCOUNT ON;
GO
-- Inline TVF 는 단일 SELECT 여야 하고 CASE 결과를 다음 단계에서 재사용해야 하므로 중첩 derived table 을 쓴다.
-- SYSDATETIME() 을 함수 안에서 호출하지 않는다 — 호출 SP 가 캡처한 @서버시각 만 쓴다.
-- SET DATEFIRST 에 의존하지 않는다 — DATEDIFF(DAY,0,d)%7 은 0=월 … 6=일 로 세션 설정과 무관하다.
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_일정확인]
(
    @서버시각      DATETIME2(7),
    @예약일 DATE,
    @시간대코드        CHAR(2),
    @마감구분      VARCHAR(10)
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          [현재업무가능]    = CONVERT(BIT, CASE WHEN d.[오늘업무일] = 1 AND d.[운영시간내여부] = 1 THEN 1 ELSE 0 END)
        , [업무가능코드]      = CONVERT(INT, CASE WHEN d.[오늘업무일] = 0 THEN 308
                                            WHEN d.[운영시간내여부] = 0 THEN 309 ELSE 0 END)
        , [업무가능메시지]   = CONVERT(NVARCHAR(300),
                            CASE WHEN d.[오늘업무일] = 0    THEN N'오늘은 업무일이 아닙니다.'
                                 WHEN d.[운영시간내여부] = 0 THEN N'현재는 업무 운영시간이 아닙니다.'
                                 ELSE N'' END)
        , [업무일여부] = CONVERT(BIT, d.[요청업무일])
        , [휴무일명]   = CONVERT(NVARCHAR(100), d.[요청휴무일명])
        , [운영여부]        = CONVERT(BIT, CASE WHEN d.[요청업무일] = 1 AND d.[시간대운영] = 1 THEN 1 ELSE 0 END)
        , [마감시각]    = CONVERT(TIME(0), CASE WHEN d.[시간대운영] = 0 THEN NULL ELSE d.[적용마감시각] END)
        , [마감경과여부]  = CONVERT(BIT, CASE WHEN d.[적용마감시각] IS NOT NULL
                                             AND d.[현재시각] >= CONVERT(TIME(7), d.[적용마감시각]) THEN 1 ELSE 0 END)
        , [일정가능]        = CONVERT(BIT, CASE WHEN d.[사유코드] = 0 THEN 1 ELSE 0 END)
        , [사유코드]    = CONVERT(INT, d.[사유코드])
        , [사유메시지] = CONVERT(NVARCHAR(300),
                            CASE d.[사유코드]
                                WHEN 300 THEN N'과거 날짜는 예약할 수 없습니다.'
                                WHEN 301 THEN N'일요일은 업무일이 아닙니다.'
                                WHEN 302 THEN N'선택한 날짜는 휴무일입니다.'
                                WHEN 303 THEN N'선택한 시간대는 운영하지 않습니다.'
                                WHEN 304 THEN N'해당 시간대의 마감시간이 지났습니다.'
                                ELSE N'' END)
    FROM
    (
        SELECT c.*
             , [사유코드] = CASE WHEN @예약일 < c.[오늘날짜]                                   THEN 300
                                 WHEN c.[요청요일] = 6                                                  THEN 301
                                 WHEN c.[요청휴무일명] IS NOT NULL                                      THEN 302
                                 WHEN c.[시간대운영] = 0                                                THEN 303
                                 WHEN c.[적용마감시각] IS NOT NULL
                                  AND c.[현재시각] >= CONVERT(TIME(7), c.[적용마감시각])                       THEN 304
                                 ELSE 0 END
        FROM
        (
            SELECT b.*
                 , [오늘업무일]   = CASE WHEN b.[오늘요일] <> 6 AND b.[오늘휴무일명] IS NULL THEN 1 ELSE 0 END
                 , [운영시간내여부]= CASE WHEN b.[현재시각] >= CONVERT(TIME(7), '09:00:00')
                                      AND b.[현재시각] <  CONVERT(TIME(7), '18:00:00') THEN 1 ELSE 0 END
                 , [요청업무일]     = CASE WHEN b.[요청요일] <> 6 AND b.[요청휴무일명] IS NULL THEN 1 ELSE 0 END
                 , [시간대운영]   = CASE WHEN b.[요청요일] = 5 AND @시간대코드 = 'PM' THEN 0 ELSE 1 END
                 , [적용마감시각]     = CASE WHEN @예약일 = b.[오늘날짜] THEN b.[기본마감시각] ELSE NULL END
            FROM
            (
                SELECT
                      [오늘날짜]        = CONVERT(DATE, @서버시각)
                    , [현재시각]      = CONVERT(TIME(7), @서버시각)
                    , [오늘요일]     = DATEDIFF(DAY, 0, CONVERT(DATE, @서버시각)) % 7
                    , [요청요일]       = DATEDIFF(DAY, 0, @예약일) % 7
                    , [오늘휴무일명] = (SELECT TOP (1) h.[휴무일명] FROM [dbo].[휴무일] h
                                       WHERE h.[휴무일자] = CONVERT(DATE, @서버시각) AND h.[사용여부] = 1)
                    , [요청휴무일명]   = (SELECT TOP (1) h.[휴무일명] FROM [dbo].[휴무일] h
                                       WHERE h.[휴무일자] = @예약일 AND h.[사용여부] = 1)
                    -- 토요일 오후는 00 §3장이 마감을 "해당 없음" 으로 확정했다.
                    -- 기본마감시각 는 요일을 보지 않으므로 마감시각 을 시간대운영=0 에서 NULL 로 덮는다.
                    , [기본마감시각]    = CASE WHEN @마감구분 = 'NORMAL'    AND @시간대코드 = 'AM' THEN CONVERT(TIME(0), '10:00:00')
                                          WHEN @마감구분 = 'NORMAL'    AND @시간대코드 = 'PM' THEN CONVERT(TIME(0), '15:00:00')
                                          WHEN @마감구분 = 'RECEPTION' AND @시간대코드 = 'AM' THEN CONVERT(TIME(0), '11:00:00')
                                          WHEN @마감구분 = 'RECEPTION' AND @시간대코드 = 'PM' THEN CONVERT(TIME(0), '16:00:00')
                                          ELSE NULL END
            ) b
        ) c
    ) d
);
GO
-- 완료이력 범위를 CompletionDate < @예약일 외로 넓히지 않는다.
-- Work 상태(RSV/RCP/CNR/CNC)를 완료이력으로 쓰지 않는다.
-- Patient 가 없으면 0행을 반환한다 (내부 derived table 이 0행이 되어 자연히 그렇게 된다).
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_검진대상확인]
(
    @수검자ID       BIGINT,
    @예약일 DATE
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          [검진대상여부]        = CONVERT(BIT, CASE WHEN x.[사유코드] = 0 THEN 1 ELSE 0 END)
        , [나이]             = CONVERT(INT, x.[나이])
        , [최근완료일자] = CONVERT(DATE, x.[최근완료일자])
        , [사유코드]      = CONVERT(INT, x.[사유코드])
        , [사유메시지]   = CONVERT(NVARCHAR(300),
                              CASE x.[사유코드]
                                  WHEN 400 THEN N'예약일 기준 만 20세 미만으로 검진 대상이 아닙니다.'
                                  WHEN 401 THEN N'일반건강검진 2년 주기가 도래하지 않았습니다.'
                                  ELSE N'' END)
    FROM
    (
        SELECT a.[나이], a.[최근완료일자]
             , [사유코드] = CASE WHEN a.[나이] < 20 THEN 400
                                 WHEN a.[최근완료일자] IS NOT NULL
                                  AND (YEAR(@예약일) - YEAR(a.[최근완료일자])) < 2 THEN 401
                                 ELSE 0 END
        FROM
        (
            SELECT
                  [나이] = DATEDIFF(YEAR, p.[생년월일일자], @예약일)
                        - CASE WHEN (MONTH(@예약일) * 100 + DAY(@예약일))
                                  < (MONTH(p.[생년월일일자])  * 100 + DAY(p.[생년월일일자])) THEN 1 ELSE 0 END
                , [최근완료일자] = (SELECT TOP (1) h.[완료일자]
                                       FROM [dbo].[완료이력] h
                                      WHERE h.[수검자ID] = @수검자ID
                                        AND h.[완료일자] < @예약일
                                      ORDER BY h.[완료일자] DESC)
            FROM (SELECT [생년월일일자] = CONVERT(DATE, i.[생년월일], 112)
                    FROM [dbo].[수검자] i WHERE i.[수검자ID] = @수검자ID) p
        ) a
    ) x
);
GO
-- TGT 비대상에게는 행을 반환하지 않는다 (t.검진대상여부 = 1 조건).
-- 조건부 술어는 00 §7.2.2 를 그대로 옮긴 것이다. 여기서 바꾸지 않는다.
-- 정렬(검사항목코드 ASC)은 Inline TVF 에서 ORDER BY 를 쓸 수 없으므로 호출자가 붙인다.
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_국가검사구성]
(
    @수검자ID       BIGINT,
    @예약일 DATE
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          [검사항목코드] = CONVERT(VARCHAR(10),  m.[검사항목코드])
        , [검사항목명] = CONVERT(NVARCHAR(100), m.[검사항목명])
        , [국가검사구분] = CONVERT(VARCHAR(12), CASE WHEN m.[국가검사규칙코드] = 'NEX-01' THEN 'BASIC' ELSE 'CONDITIONAL' END)
        , [국가검사규칙코드] = CONVERT(VARCHAR(10),  m.[국가검사규칙코드])
    FROM [dbo].[검사코드] m
    CROSS JOIN
    (
        SELECT g.[검진대상여부], g.[나이], i.[성별], i.[B형간염제외여부]
        FROM [dbo].[수검자] i
        CROSS APPLY [dbo].[UFN_HC_검진대상확인](i.[수검자ID], @예약일) g
        WHERE i.[수검자ID] = @수검자ID
    ) t
    WHERE m.[국가검사규칙코드] IS NOT NULL
      AND t.[검진대상여부] = 1
      AND
      (
            m.[국가검사규칙코드] = 'NEX-01'
        OR (m.[국가검사규칙코드] = 'NEX-02' AND
            (  (t.[성별] = 'M' AND t.[나이] >= 24 AND (t.[나이] - 24) % 4 = 0)
            OR (t.[성별] = 'F' AND t.[나이] >= 40 AND (t.[나이] - 40) % 4 = 0) ))
        OR (m.[국가검사규칙코드] = 'NEX-03' AND t.[나이] = 40
            AND t.[B형간염제외여부] = 0)
        OR (m.[국가검사규칙코드] = 'NEX-04' AND t.[나이] = 56)
        OR (m.[국가검사규칙코드] = 'NEX-05' AND t.[성별] = 'F' AND t.[나이] IN (54, 60, 66))
        OR (m.[국가검사규칙코드] = 'NEX-06' AND t.[나이] IN (56, 66))
      )
);
GO
-- 요청 값은 VALUES 행 생성자로만 받는다 (TVP·동적 SQL 은 허용 T-SQL 목록 밖이다).
-- 선택하지 않은 무효 항목은 사유만 표시하고 저장을 막지 않는다 — 요청선택여부 와 유효선택여부 를 분리한다.
-- 정렬(추가검사코드 ASC)은 호출자가 붙인다.
CREATE OR ALTER FUNCTION [dbo].[UFN_HC_추가검사확인]
(
    @수검자ID        BIGINT,
    @예약일  DATE,
    @업무ID           BIGINT,
    @저장검사사용여부    BIT,
    @추가검사01선택여부 BIT, @추가검사02선택여부 BIT, @추가검사03선택여부 BIT,
    @추가검사04선택여부 BIT, @추가검사05선택여부 BIT, @추가검사06선택여부 BIT,
    @추가검사07선택여부 BIT
)
RETURNS TABLE
AS
RETURN
(
    SELECT
          [추가검사코드]    = CONVERT(VARCHAR(10),  b.[추가검사코드])
        , [검사항목코드]      = CONVERT(VARCHAR(10),  b.[검사항목코드])
        , [검사항목명]      = CONVERT(NVARCHAR(100), b.[검사항목명])
        , [요청선택여부]     = CONVERT(BIT, b.[요청선택여부])
        , [유효선택여부]      = CONVERT(BIT, CASE WHEN b.[요청선택여부] = 1 AND b.[사유코드] = 0 THEN 1 ELSE 0 END)
        , [선택가능]     = CONVERT(BIT, CASE WHEN b.[사유코드] = 0 THEN 1 ELSE 0 END)
        , [사유코드]    = CONVERT(INT, b.[사유코드])
        , [사유메시지] = CONVERT(NVARCHAR(300),
                            CASE b.[사유코드]
                                WHEN 400 THEN N'예약일 기준 만 20세 미만으로 검진 대상이 아닙니다.'
                                WHEN 401 THEN N'일반건강검진 2년 주기가 도래하지 않았습니다.'
                                WHEN 410 THEN N'현재 사용할 수 없는 추가검사입니다.'
                                WHEN 411 THEN N'성별 조건을 충족하지 않는 추가검사입니다.'
                                WHEN 412 THEN N'일반건강검진에 포함된 검사입니다.'
                                ELSE N'' END)
    FROM
    (
        SELECT m.[추가검사코드], m.[검사항목코드], m.[검사항목명], r.[요청선택여부]
             , [사유코드] =
                 CASE
                     WHEN @저장검사사용여부 = 0 AND t.[검진대상여부] = 0 THEN t.[사유코드]      -- 400 / 401
                     WHEN m.[추가검사사용여부] = 0                                THEN 410
                     WHEN m.[추가검사성별코드] <> 'A'
                      AND m.[추가검사성별코드] <> t.[성별]                  THEN 411
                     WHEN EXISTS
                          (
                              SELECT 1 FROM
                              (
                                  SELECT n.[검사항목코드] FROM [dbo].[UFN_HC_국가검사구성](@수검자ID, @예약일) n
                                   WHERE @저장검사사용여부 = 0
                                  UNION ALL
                                  SELECT s.[검사항목코드] FROM [dbo].[검사코드] s
                                   WHERE @저장검사사용여부 = 1
                                     AND EXISTS (SELECT 1 FROM [dbo].[예약접수] w
                                                  WHERE w.[업무ID] = @업무ID
                                                    AND N',' + ISNULL(w.[국가검사항목], N'') + N','
                                                        LIKE N'%,' + s.[검사항목코드] + N',%')
                              ) nx WHERE nx.[검사항목코드] = m.[검사항목코드]
                          )                                                       THEN 412
                     ELSE 0
                 END
        FROM [dbo].[검사코드] m
        CROSS JOIN
        (
            SELECT i.[성별]
                 , [검진대상여부]   = ISNULL(g.[검진대상여부], CONVERT(BIT,0))
                 , [사유코드] = ISNULL(g.[사유코드], 400)
            FROM [dbo].[수검자] i
            OUTER APPLY [dbo].[UFN_HC_검진대상확인](i.[수검자ID], @예약일) g
            WHERE i.[수검자ID] = @수검자ID
        ) t
        JOIN
        (
            VALUES ('OPT01', @추가검사01선택여부), ('OPT02', @추가검사02선택여부), ('OPT03', @추가검사03선택여부),
                   ('OPT04', @추가검사04선택여부), ('OPT05', @추가검사05선택여부), ('OPT06', @추가검사06선택여부),
                   ('OPT07', @추가검사07선택여부)
        ) r ([추가검사코드], [요청선택여부]) ON r.[추가검사코드] = m.[추가검사코드]
        WHERE m.[추가검사코드] IS NOT NULL
    ) b
);
GO
