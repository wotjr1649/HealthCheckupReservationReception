SET NOCOUNT ON;
PRINT '--- 09_Concurrency_Setup 시작 ---';
GO
-- 동시성 시나리오별 사전상태 (스펙 §38 · plans/08 T34).
--   concurrency-test.sh 가 시나리오 번호를 -v Scenario 로 넘긴다.
--   호출 순서는 rebuild -> 00_Test_Harness -> 이 파일이다 (scripts/test.sh).
--
-- 전용 수검자 CONC1·CONC2 를 쓴다. F001~F019 는 이미 유효업무가 있어 306 이 먼저 나오고,
-- T001~T020 은 Rule 시험이 읽으므로 오염시키면 안 된다.
-- 주민번호는 체크디지트가 **유효한** 값이어야 한다 (스펙 §16.1 SSN-006).
-- 스키마에는 체크디지트 CHECK 이 없어 직접 INSERT 는 무효값도 통과하지만, CON-004 의
-- USP_HC_UPDATE_수검자정보 가 새 주민번호를 검증하므로 무효값이면 시나리오가 성립하지 않는다.
-- 계획서 Step 2 의 '9505051000019' 도 무효였다 - 유효값은 9505051000014 다 (실측).
--   CONC1  8003011000014  1980-03-01 남 -> 2026 기준 만 46세 · NEX 8행
--   CONC2  8003012000017  1980-03-01 여 -> 만 46세
-- CON-004 가 CONC1 을 7003011000016(1970-03-01 남, 만 56세)로 바꾼다. NEX 가 8 -> 11 로 움직인다.
-- 생년월일·성별은 계산열이라 INSERT 목록에 넣지 않는다 (Msg 271).
DECLARE @Scen INT = CONVERT(INT, N'$(Scenario)');
PRINT 'INFO 시나리오 ' + CONVERT(VARCHAR(3), @Scen);

DELETE w FROM [dbo].[예약접수] w
  JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
 WHERE p.[차트번호] IN (N'CONC1', N'CONC2');
DELETE FROM [dbo].[수검자] WHERE [차트번호] IN (N'CONC1', N'CONC2');

INSERT INTO [dbo].[수검자] ([차트번호], [성명], [주민번호])
VALUES (N'CONC1', N'동시성일', '8003011000014')
     , (N'CONC2', N'동시성이', '8003012000017');
GO
-- 기본검사(NEX-01) 문자열을 검사코드에서 유도한다. 손으로 적으면 Seed 와 어긋난다.
-- GO 는 변수 경계이므로 사용하는 배치 안에서 다시 만든다 (CLAUDE.md §11).
DECLARE @Scen INT = CONVERT(INT, N'$(Scenario)');
DECLARE @Basic NVARCHAR(100) = N'', @BC VARCHAR(10);
DECLARE @BCodes TABLE (C VARCHAR(10) PRIMARY KEY);
INSERT INTO @BCodes (C) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01';
WHILE EXISTS (SELECT 1 FROM @BCodes)
BEGIN
    SELECT TOP (1) @BC = C FROM @BCodes ORDER BY C;
    SET @Basic = @Basic + @BC + N',';
    DELETE FROM @BCodes WHERE C = @BC;
END
SET @Basic = LEFT(@Basic, LEN(@Basic) - 1);

DECLARE @P1 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'CONC1');
DECLARE @P2 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'CONC2');
DECLARE @Today DATE = CONVERT(DATE, SYSDATETIME());

----------------------------------------------------------------------------
-- CON-001  동일 SocialNumber 동시등록 : 그 주민번호가 없어야 한다. 위 DELETE 와 rebuild 가 보장한다.
-- CON-002  19/20 Slot 동시예약        : 00_Test_Harness 가 2026-11-16 AM 에 F001~F019 19건을 심는다.
-- CON-003  동일 Patient 다른 Slot     : CONC1 에게 유효업무가 없어야 한다. 위 DELETE 가 보장한다.
-- CON-004  주민번호 변경 vs 신규예약   : 같음. A 가 CONC1 을 만 56세로 바꾸고 B 가 예약한다.
----------------------------------------------------------------------------

IF @Scen = 5
BEGIN
    -- CON-005 접수완료 vs 예약취소. 접수완료가 성공하려면 예약일이 오늘이어야 하고(503)
    --   접수마감 전이어야 한다(304). PM Slot 은 마감이 16:00 이라 11:00~15:50 창을 쓸 수 있다 (스펙 §38.7).
    INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
    VALUES (@P1, @Today, 'PM', 'RSV', @Basic, NULL);
    PRINT 'INFO CON-005 사전상태 · CONC1 의 오늘 PM RSV Work 1건';
END

IF @Scen = 6
BEGIN
    -- CON-006 같은 Work 의 AEX 동시변경. RCP 상태가 필요한데 정상 SP 로는 마감 안에서만 만들 수 있으므로
    --   여기서 직접 심는다. 접수추가검사 계약에는 예약일 제약이 없어 미래로 둔다 (05 §12.2).
    --   [!] A 는 반드시 실제 변경이어야 한다. 현재 집합과 같은 값을 주면 A 가 No-op 으로 끝나
    --       RowVersion 이 그대로 남고 B 도 성공해 버려 601 이 영영 관측되지 않는다.
    INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
    VALUES (@P1, '2026-11-18', 'AM', 'RCP', @Basic, N'EX014');
    PRINT 'INFO CON-006 사전상태 · CONC1 의 RCP Work 1건 (AEX EX014)';
END

IF @Scen = 7
BEGIN
    -- CON-007 예약 교차이동. A 는 CONC1 을 AM -> PM, B 는 CONC2 를 PM -> AM 으로 옮긴다.
    --   자원명 오름차순 획득이 없으면 A 가 AM 을 쥔 채 PM 을, B 가 PM 을 쥔 채 AM 을 기다려 교착이다.
    --   2026-11-18 을 쓴다. CORRUPT-1 의 T012 가 같은 날 PM 에 1건 있으나 정원 20 에 영향이 없다.
    INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
    VALUES (@P1, '2026-11-18', 'AM', 'RSV', @Basic, NULL)
         , (@P2, '2026-11-18', 'PM', 'RSV', @Basic, NULL);
    PRINT 'INFO CON-007 사전상태 · CONC1 2026-11-18 AM · CONC2 같은 날 PM';
END

IF @Scen = 8
BEGIN
    -- CON-008 접수완료 vs WalkIn 신규 (스펙 §24.2·§38.5).
    --   오늘 PM Slot 을 RSV 20건으로 채운다. B 가 그중 하나를 접수완료(RSV -> RCP)하는 사이
    --   A 의 정원 COUNT 가 그 행을 놓치면 21건이 저장된다. IX_예약접수_SLOT 키 안에서 뒤(RSV)에서
    --   앞(RCP)으로 옮겨가는 유일한 전이라 SLOT 잠금 없이는 실제로 누락된다.
    --
    --   [X] F001~F019 에 오늘 PM Work 를 **새로 넣지 않는다.** 그들은 2026-11-16 AM 에 이미
    --       유효업무가 있어 한 수검자에 유효업무 2건이 되고, 그것은 RP-06 위반 상태다.
    --       기존 Work 를 오늘 PM 으로 **옮긴다.** 시나리오마다 rebuild + 00 이 다시 도므로 안전하다.
    UPDATE w SET w.[예약일] = @Today, w.[시간대코드] = 'PM'
      FROM [dbo].[예약접수] w
      JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
     WHERE p.[차트번호] LIKE 'F0%' AND p.[차트번호] <> N'F020' AND w.[상태코드] = 'RSV';

    -- CONC2 로 20 번째를 채운다. CONC1 은 A 의 WalkIn 대상이라 비워 둔다.
    INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
    VALUES (@P2, @Today, 'PM', 'RSV', @Basic, NULL);

    DECLARE @N INT = (SELECT COUNT(*) FROM [dbo].[예약접수]
                       WHERE [예약일] = @Today AND [시간대코드] = 'PM'
                         AND [상태코드] IN ('RSV', 'RCP'));
    IF @N <> 20 THROW 51030, N'CON-008 사전상태가 20/20 이 아닙니다.', 1;
    PRINT 'INFO CON-008 사전상태 · 오늘 PM 20/20';
END

PRINT '=== 09_Concurrency_Setup 완료 ===';
GO
