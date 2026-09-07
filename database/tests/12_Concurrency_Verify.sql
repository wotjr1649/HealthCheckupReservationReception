SET NOCOUNT ON;
PRINT '--- 12_Concurrency_Verify 시작 ---';
GO
-- 동시성 판정 - DB **최종 상태** 몫 (스펙 §38.2). 로그 수치(applock 잠금결과=1 · Msg 1205 · 50002 ·
-- 2627 · 50001 · 601 · 502)는 concurrency-test.sh 가 본다. 둘 다 있어야 G11 이 성립한다.
-- 세션 A/B 는 업무실패로 끝날 수 있고 그것이 정상이므로 exit code 로 판정하지 않는다.
DECLARE @Scen INT = CONVERT(INT, N'$(Scenario)');
DECLARE @Fail INT = 0;
DECLARE @P1 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'CONC1');
DECLARE @P2 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'CONC2');
DECLARE @오늘날짜 DATE = CONVERT(DATE, SYSDATETIME());
DECLARE @N INT;

IF @Scen = 1
BEGIN
    SET @N = (SELECT COUNT(*) FROM [dbo].[수검자] WHERE [주민번호] = '9505051000014');
    IF @N = 1 PRINT 'PASS CON-001 동일 주민번호 동시등록 2건 → 저장 1건';
    ELSE BEGIN PRINT 'FAIL CON-001 저장 ' + CONVERT(VARCHAR(5), @N) + '건'; SET @Fail += 1; END
END

IF @Scen = 2
BEGIN
    SET @N = (SELECT COUNT(*) FROM [dbo].[예약접수]
               WHERE [예약일] = '2026-11-16' AND [시간대코드] = 'AM' AND [상태코드] IN ('RSV','RCP'));
    IF @N = 20 PRINT 'PASS CON-002 19/20 Slot 동시예약 2건 → 최종 20';
    ELSE BEGIN PRINT 'FAIL CON-002 최종 인원 ' + CONVERT(VARCHAR(5), @N); SET @Fail += 1; END
END

IF @Scen = 3
BEGIN
    SET @N = (SELECT COUNT(*) FROM [dbo].[예약접수]
               WHERE [수검자ID] = @P1 AND [상태코드] IN ('RSV','RCP'));
    IF @N = 1 PRINT 'PASS CON-003 동일 Patient 다른 Slot 동시예약 → 유효업무 1건';
    ELSE BEGIN PRINT 'FAIL CON-003 유효업무 ' + CONVERT(VARCHAR(5), @N) + '건'; SET @Fail += 1; END
END

IF @Scen = 4
BEGIN
    -- 스펙 §38.6. "둘 다 성공 금지" 가 아니라 **저장 검사구성이 최종 수검자 상태와 맞는가** 다.
    --   ① A→B  주민번호 새 값 · 검사구성 새 기준        정상
    --   ② B→A  A 가 205(EP-08) · 검사구성 옛 기준       정상
    --   ③ 경합  주민번호 새 값 · 검사구성 **옛** 기준    위반
    DECLARE @국가검사 NVARCHAR(100), @D DATE;
    SELECT @국가검사 = w.[국가검사항목], @D = w.[예약일] FROM [dbo].[예약접수] w
     WHERE w.[수검자ID] = @P1 AND w.[상태코드] IN ('RSV','RCP');
    IF @국가검사 IS NULL BEGIN PRINT 'FAIL CON-004 CONC1 의 유효업무가 없다'; SET @Fail += 1; END
    ELSE
    BEGIN
        -- STRING_SPLIT 은 허용목록 밖이다 (06 §9.2). 쉼표 개수와 LIKE 로 양방향을 센다.
        DECLARE @Saved INT = CASE WHEN LEN(@국가검사) = 0 THEN 0
                                  ELSE LEN(@국가검사) - LEN(REPLACE(@국가검사, N',', N'')) + 1 END;
        DECLARE @Tvf INT = (SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@P1, @D));
        DECLARE @Both INT = (SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@P1, @D) f
                              WHERE N',' + @국가검사 + N',' LIKE N'%,' + f.[검사항목코드] + N',%');
        DECLARE @Ssn2 VARCHAR(13) = (SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @P1);
        PRINT 'INFO CON-004 주민번호=' + @Ssn2 + ' 저장NEX=' + CONVERT(VARCHAR(5), @Saved)
            + ' 최종기준NEX=' + CONVERT(VARCHAR(5), @Tvf) + ' 교집합=' + CONVERT(VARCHAR(5), @Both);
        IF @Saved = @Both AND @Tvf = @Both
            PRINT 'PASS CON-004 저장 국가검사항목 = 최종 수검자 상태 기준 NEX (양방향 일치)';
        ELSE BEGIN PRINT 'FAIL CON-004 저장 검사구성이 최종 수검자 상태와 어긋난다'; SET @Fail += 1; END
    END
END

IF @Scen = 5
BEGIN
    -- 예약취소의 목표상태는 CNR 이다. CNC 는 접수취소(RCP→CNC)의 것이다 (실측 확인).
    DECLARE @St CHAR(3) = (SELECT TOP (1) w.[상태코드] FROM [dbo].[예약접수] w
                            WHERE w.[수검자ID] = @P1 AND w.[예약일] = @오늘날짜 AND w.[시간대코드] = 'PM'
                            ORDER BY w.[업무ID]);
    SET @N = (SELECT COUNT(*) FROM [dbo].[예약접수] w
               WHERE w.[수검자ID] = @P1 AND w.[예약일] = @오늘날짜 AND w.[시간대코드] = 'PM');
    PRINT 'INFO CON-005 최종 상태코드=' + ISNULL(@St, '(없음)') + ' 행수=' + CONVERT(VARCHAR(5), @N);
    IF @N = 1 AND @St IN ('RCP', 'CNR')
        PRINT 'PASS CON-005 접수완료 vs 예약취소 → 한쪽만 전이 (RCP 또는 CNR)';
    ELSE BEGIN PRINT 'FAIL CON-005 상태 또는 행수가 기대와 다르다'; SET @Fail += 1; END
END

IF @Scen = 6
BEGIN
    -- 601 은 로그 판정이다. 여기서는 두 요청 중 **한쪽 집합만** 저장됐는지 본다.
    --   A = EX014,EX018 (OPT01+OPT06) · B = EX014,EX015 (OPT01+OPT02)
    DECLARE @추가검사 NVARCHAR(50) = (SELECT TOP (1) w.[추가검사항목] FROM [dbo].[예약접수] w
                                  WHERE w.[수검자ID] = @P1 AND w.[상태코드] = 'RCP' ORDER BY w.[업무ID]);
    PRINT 'INFO CON-006 최종 추가검사항목=' + ISNULL(@추가검사, '(NULL)');
    IF @추가검사 IN (N'EX014,EX018', N'EX014,EX015')
        PRINT 'PASS CON-006 같은 Work 동시 AEX 변경 → 한쪽 집합만 저장';
    ELSE BEGIN PRINT 'FAIL CON-006 두 요청이 섞였거나 아무것도 저장되지 않았다'; SET @Fail += 1; END
END

IF @Scen = 7
BEGIN
    -- Msg 1205 · 50002 0건은 로그 판정이다. 여기서는 교차이동이 상태를 깨뜨리지 않았는지 본다.
    DECLARE @N1 INT = (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [수검자ID] = @P1 AND [상태코드] IN ('RSV','RCP'));
    DECLARE @N2 INT = (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [수검자ID] = @P2 AND [상태코드] IN ('RSV','RCP'));
    DECLARE @SA INT = (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [예약일] = '2026-11-18' AND [시간대코드] = 'AM' AND [상태코드] IN ('RSV','RCP'));
    DECLARE @SP INT = (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [예약일] = '2026-11-18' AND [시간대코드] = 'PM' AND [상태코드] IN ('RSV','RCP'));
    PRINT 'INFO CON-007 CONC1=' + CONVERT(VARCHAR(5), @N1) + ' CONC2=' + CONVERT(VARCHAR(5), @N2)
        + ' 11-18 AM=' + CONVERT(VARCHAR(5), @SA) + ' PM=' + CONVERT(VARCHAR(5), @SP);
    IF @N1 = 1 AND @N2 = 1 AND @SA <= 20 AND @SP <= 20
        PRINT 'PASS CON-007 교차이동 후에도 각 수검자 유효업무 1건 · 정원 이내';
    ELSE BEGIN PRINT 'FAIL CON-007 유효업무 또는 정원이 깨졌다'; SET @Fail += 1; END
END

IF @Scen = 8
BEGIN
    SET @N = (SELECT COUNT(*) FROM [dbo].[예약접수]
               WHERE [예약일] = @오늘날짜 AND [시간대코드] = 'PM' AND [상태코드] IN ('RSV','RCP'));
    IF @N = 20 PRINT 'PASS CON-008 접수완료 vs WalkIn 신규 → 최종 20 (21 이면 §24.2 결함 재발)';
    ELSE BEGIN PRINT 'FAIL CON-008 최종 인원 ' + CONVERT(VARCHAR(5), @N); SET @Fail += 1; END
END

-- 집계 THROW 는 @Fail 을 선언한 배치 안에 있어야 한다. GO 는 변수 경계다 (CLAUDE.md §11).
IF @Fail > 0 THROW 51000, N'동시성 시나리오 판정에 실패가 있습니다.', 1;
PRINT '=== 12_Concurrency_Verify 완료 ===';
GO
