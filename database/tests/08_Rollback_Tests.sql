SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- 부분저장 0건 (스펙 §21.3·§37, G10). 업무실패 시 Master/Detail 이 **전혀** 바뀌지 않았음을
-- SP 반환값이 아니라 **행수와 값**으로 증명한다. RS 는 받지 않는다 (INSERT … EXEC 금지, §33.1a).
--
-- 업무시간 가드 (스펙 §33.2a). 이 파일의 실패 유도는 전부 접수마감과 무관하다 —
-- 접수완료를 쓰지 않기 때문이다.
-- [!] 업무시간 밖이라고 건너뛰지 않는다. 창 밖에서 Write SP 가 308/309 를 내고 **아무것도
--     바꾸지 않는다** 는 것은 계약이지 시험 사정이 아니다 (05 §5 우선순위 9번).
--     SKIP 은 PASS 가 아니므로(CLAUDE.md §10) 창 밖에서는 그 계약을 실제로 판정한다.
--     아래 호출들은 **창 안이면 성공했을 인자**다. 그래서 "실패라 안 바뀌었다" 가 아니라
--     "업무시간 밖이라 성공 경로가 차단됐다" 를 본다. RS0 결과코드 판정은 tests/contract/OFF-* 가 한다.
DECLARE @BizOk BIT = CASE WHEN DATEPART(WEEKDAY, SYSDATETIME()) BETWEEN 2 AND 7
                           AND CONVERT(TIME(0), SYSDATETIME()) >= '09:00:00'
                           AND CONVERT(TIME(0), SYSDATETIME()) <  '18:00:00'
                           AND NOT EXISTS (SELECT 1 FROM [dbo].[휴무일]
                                            WHERE [휴무일자] = CONVERT(DATE, SYSDATETIME())
                                              AND [사용여부] = 1)
                          THEN 1 ELSE 0 END;

IF @BizOk = 0
BEGIN
    DECLARE @Off0 VARCHAR(300) =
          CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[수검자]))     + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[예약접수]))   + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[변경이력]))   + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [상태코드] = 'RSV')) + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [상태코드] = 'RCP')) + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [상태코드] = 'CNR')) + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [상태코드] = 'CNC')) + '|'
        + CONVERT(VARCHAR(20), (SELECT ISNULL(SUM(CONVERT(BIGINT,
                     DATEDIFF(SECOND, '2020-01-01', [최종수정일시]))), 0) FROM [dbo].[예약접수]));

    DECLARE @OPt BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
    DECLARE @OP9 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T009');

    EXEC [dbo].[USP_HC_예약_등록] @OP9, 'NORMAL', '2026-11-17', 'AM', 0,0,0,0,0,0,0, N'TEST';
    EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, N'창밖롤백', '9601011000018',
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';

    DECLARE @Off1 VARCHAR(300) =
          CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[수검자]))     + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[예약접수]))   + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[변경이력]))   + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [상태코드] = 'RSV')) + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [상태코드] = 'RCP')) + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [상태코드] = 'CNR')) + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [상태코드] = 'CNC')) + '|'
        + CONVERT(VARCHAR(20), (SELECT ISNULL(SUM(CONVERT(BIGINT,
                     DATEDIFF(SECOND, '2020-01-01', [최종수정일시]))), 0) FROM [dbo].[예약접수]));

    IF @Off0 = @Off1 AND @@TRANCOUNT = 0
        PRINT 'PASS RBK-OFF 업무시간 밖 Write SP 호출이 DB 를 바꾸지 않았다  ' + @Off1;
    ELSE BEGIN PRINT 'FAIL RBK-OFF 창 밖 호출이 데이터를 바꿨다  ' + @Off0 + ' -> ' + @Off1;
               SET @Fail += 1; END

    PRINT 'NOT RUN RBK-001~008 업무시간(월~토 09:00~18:00, 비휴무일) 밖 - 성공 경로는 창 안에서만 성립한다';
    IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
    PRINT '=== 08_Rollback_Tests 완료 (창 밖 분기) ===';
    RETURN;
END

DECLARE @P09 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T009');
DECLARE @P16 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T016');
DECLARE @Pf  BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'F020');

IF @P09 IS NULL OR @P16 IS NULL OR @Pf IS NULL
BEGIN
    PRINT N'FAIL 사전조건 T009/T016/F020 중 없는 Fixture 가 있다 (tests/00 을 먼저 실행했는가)';
    SET @Fail += 1;
END

-- 단독 재실행 가능하도록 이 파일이 만든 Work 만 지운다.
DELETE FROM [dbo].[예약접수] WHERE [수검자ID] IN (@P09, @P16);
UPDATE [dbo].[예약접수] SET [상태코드] = 'CNR' WHERE [수검자ID] = @Pf;

DECLARE @Basic NVARCHAR(100) = N'', @BC VARCHAR(10);
DECLARE @BCodes TABLE ([코드] VARCHAR(10) PRIMARY KEY);
INSERT INTO @BCodes ([코드]) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] = 'NEX-01';
WHILE EXISTS (SELECT 1 FROM @BCodes)
BEGIN
    SELECT TOP (1) @BC = [코드] FROM @BCodes ORDER BY [코드];
    SET @Basic = @Basic + @BC + N',';
    DELETE FROM @BCodes WHERE [코드] = @BC;
END
SET @Basic = LEFT(@Basic, LEN(@Basic) - 1);

DECLARE @W BIGINT, @Rv BINARY(8), @D DATE, @S CHAR(2), @Cfg NVARCHAR(200);
DECLARE @Cnt0 INT, @H0 INT;

----------------------------------------------------------------------------
-- RBK-001  INSERT_예약 이 AEX 성별 위반(411)으로 실패 -> Work 신규 0건
--   검사구성이 예약접수 행의 컬럼이므로 "Detail 신규 0건" 은 곧 "행 신규 0건" 이다 (04 §8.2.2).
----------------------------------------------------------------------------
SET @Cnt0 = (SELECT COUNT(*) FROM [dbo].[예약접수]);
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력]);
EXEC [dbo].[USP_HC_예약_등록] @P09, 'NORMAL', '2026-11-17', 'AM', 0,0,1,0,0,0,0, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[예약접수]) = @Cnt0
    AND (SELECT COUNT(*) FROM [dbo].[변경이력]) = @H0)
    PRINT 'PASS RBK-001 AEX 성별 위반 실패가 Work 도 감사행도 남기지 않았다';
ELSE BEGIN PRINT 'FAIL RBK-001 부분저장 발생'; SET @Fail += 1; END

----------------------------------------------------------------------------
-- RBK-002  UPDATE_예약변경 이 TGT 비대상으로 실패 -> Work 전량 불변
--   [!] 400 UnderAge 는 예약변경으로 도달할 수 없다 - 미래로 옮길수록 나이가 늘기 때문이다.
--       T016 은 2025-05-01 완료이력이 있어 2026 의 어떤 예약일에도 2년 주기가 도래하지 않는다(401).
--       TGT 비대상 Work 는 정상 SP 로 만들 수 없으므로 직접 심는다.
----------------------------------------------------------------------------
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@P16, '2026-11-24', 'AM', 'RSV', @Basic, NULL);
SET @W = SCOPE_IDENTITY();
SELECT @Rv = [행버전], @D = [예약일], @S = [시간대코드]
     , @Cfg = ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
  FROM [dbo].[예약접수] WHERE [업무ID] = @W;

EXEC [dbo].[USP_HC_예약_변경] @W, @Rv, '2026-11-25', 'PM', 0,0,0,0,0,0,0, N'TEST';

IF ((SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @D
    AND (SELECT [시간대코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @S
    AND (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Rv
    AND (SELECT ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
           FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Cfg)
    PRINT 'PASS RBK-002 TGT 비대상 실패가 예약일·시간대·행버전·검사구성을 전부 보존했다';
ELSE BEGIN PRINT 'FAIL RBK-002 TGT 실패인데 Work 가 바뀌었다'; SET @Fail += 1; END
DELETE FROM [dbo].[예약접수] WHERE [업무ID] = @W;

----------------------------------------------------------------------------
-- RBK-003  UPDATE_예약변경 이 정원 마감(305)으로 실패 -> Work 전량 불변
--   F020 을 RSV 로 뒤집어 2026-11-16 AM 을 20/20 으로 만들고 T020 의 Work 를 그리로 옮긴다.
--   현재 Work 를 COUNT 에서 뺀 뒤 +1 하므로 21 이 되어 305 다 (스펙 §30.1).
--   정원 판정이 TGT·AEX 보다 앞이라 이 Work 의 EX012 는 관여하지 않는다 (05 §11.2 검증순서).
----------------------------------------------------------------------------
UPDATE [dbo].[예약접수] SET [상태코드] = 'RSV' WHERE [수검자ID] = @Pf;
DECLARE @시간대인원 INT = (SELECT COUNT(*) FROM [dbo].[예약접수]
                      WHERE [예약일] = '2026-11-16' AND [시간대코드] = 'AM'
                        AND [상태코드] IN ('RSV', 'RCP'));
IF @시간대인원 = 20 PRINT 'PASS RBK-003 사전조건 20/20 성립';
ELSE BEGIN PRINT 'FAIL RBK-003 사전조건 Slot=' + CONVERT(VARCHAR(5), @시간대인원); SET @Fail += 1; END

SET @W = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
           JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
          WHERE p.[차트번호] = N'T020' AND w.[상태코드] = 'RSV');
SELECT @Rv = [행버전], @D = [예약일], @S = [시간대코드]
     , @Cfg = ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
  FROM [dbo].[예약접수] WHERE [업무ID] = @W;

EXEC [dbo].[USP_HC_예약_변경] @W, @Rv, '2026-11-16', 'AM', 0,0,0,1,0,0,0, N'TEST';

IF ((SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @D
    AND (SELECT [시간대코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @S
    AND (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Rv
    AND (SELECT ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
           FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Cfg
    AND (SELECT COUNT(*) FROM [dbo].[예약접수]
          WHERE [예약일] = '2026-11-16' AND [시간대코드] = 'AM'
            AND [상태코드] IN ('RSV', 'RCP')) = 20)
    PRINT 'PASS RBK-003 정원 마감 실패가 Work 를 보존하고 정원도 21 이 되지 않았다';
ELSE BEGIN PRINT 'FAIL RBK-003 정원 실패인데 Work 또는 정원이 바뀌었다'; SET @Fail += 1; END

UPDATE [dbo].[예약접수] SET [상태코드] = 'CNR' WHERE [수검자ID] = @Pf;   -- F020 복원

----------------------------------------------------------------------------
-- RBK-004  UPDATE_접수추가검사 가 성별 위반(411)으로 실패 -> 추가검사항목·행버전 불변
----------------------------------------------------------------------------
SET @W = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
           JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
          WHERE p.[차트번호] = N'T014' AND w.[상태코드] = 'RCP');
IF @W IS NULL
BEGIN PRINT N'FAIL RBK-004 사전조건 T014 의 RCP Work 가 없다 (tests/00b 를 먼저 실행했는가)'; SET @Fail += 1; END
ELSE
BEGIN
    SELECT @Rv = [행버전]
         , @Cfg = ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
      FROM [dbo].[예약접수] WHERE [업무ID] = @W;

    -- OPT03(EX016)은 여성 전용이고 T014 는 남성이다.
    EXEC [dbo].[USP_HC_접수추가검사_변경] @W, @Rv, 1,0,1,0,0,0,0, N'TEST';

    IF ((SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Rv
        AND (SELECT ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
               FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Cfg)
        PRINT 'PASS RBK-004 AEX 성별 위반 실패가 검사구성과 RowVersion 을 보존했다';
    ELSE BEGIN PRINT 'FAIL RBK-004 AEX 실패인데 Work 가 바뀌었다'; SET @Fail += 1; END
END

----------------------------------------------------------------------------
-- RBK-005  UPDATE_수검자정보 가 205(활성 업무 보유)로 실패 -> 수검자 행 전체 불변
----------------------------------------------------------------------------
-- R7: 동시성 토큰은 [행버전] 이다 (04 §1.2).
DECLARE @Pid BIGINT, @Led BINARY(8), @Snap NVARCHAR(500);
SELECT @Pid = [수검자ID], @Led = [행버전]
     , @Snap = [차트번호] + N'|' + [성명] + N'|' + [주민번호] + N'|' + [생년월일] + N'|' + [성별]
              + N'|' + ISNULL([휴대전화], N'-') + N'|' + CONVERT(NVARCHAR(1), [B형간염제외여부])
  FROM [dbo].[수검자] WHERE [차트번호] = N'F001';

-- F001 은 2026-11-16 AM 의 RSV Work 를 가진다. 주민번호 변경은 205 로 막힌다 (00 EP-08).
EXEC [dbo].[USP_HC_수검자정보_수정] @Pid, @Led, N'F001', N'바뀐이름', '8001011999998',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, N'TEST';

IF ((SELECT [행버전] FROM [dbo].[수검자] WHERE [수검자ID] = @Pid) = @Led
    AND (SELECT [차트번호] + N'|' + [성명] + N'|' + [주민번호] + N'|' + [생년월일] + N'|' + [성별]
              + N'|' + ISNULL([휴대전화], N'-') + N'|' + CONVERT(NVARCHAR(1), [B형간염제외여부])
           FROM [dbo].[수검자] WHERE [수검자ID] = @Pid) = @Snap)
    PRINT 'PASS RBK-005 205 실패가 수검자 행과 RowVersion 을 전부 보존했다';
ELSE BEGIN PRINT 'FAIL RBK-005 205 인데 수검자 행이 바뀌었다'; SET @Fail += 1; END

----------------------------------------------------------------------------
-- RBK-006  INSERT_수검자 가 차트번호 중복(201)으로 실패 -> 신규 Patient 0건
--   Sequence 소비는 허용한다 (04 §3.6 결번 허용). 여기서는 수동 차트번호라 소비도 없다.
----------------------------------------------------------------------------
SET @Cnt0 = (SELECT COUNT(*) FROM [dbo].[수검자]);
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력]);
EXEC [dbo].[USP_HC_수검자_등록] 0, N'T001', N'중복차트롤백', '9401011000017',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[수검자]) = @Cnt0
    AND (SELECT COUNT(*) FROM [dbo].[변경이력]) = @H0
    AND NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [성명] = N'중복차트롤백'))
    PRINT 'PASS RBK-006 차트번호 중복 실패가 수검자도 감사행도 남기지 않았다';
ELSE BEGIN PRINT 'FAIL RBK-006 201 인데 행이 생겼다'; SET @Fail += 1; END

----------------------------------------------------------------------------
-- RBK-007  위 실패 6건을 지난 직후 열린 Transaction 이 남지 않았다
--   XACT_ABORT ON 아래에서 ROLLBACK 이 누락되면 여기서 1 이상이 나온다 (스펙 §21.1).
----------------------------------------------------------------------------
IF (@@TRANCOUNT = 0)
    PRINT 'PASS RBK-007 업무실패 6건 이후 @@TRANCOUNT = 0';
ELSE BEGIN PRINT 'FAIL RBK-007 Transaction 잔여 ' + CONVERT(VARCHAR(5), @@TRANCOUNT); SET @Fail += 1; END

----------------------------------------------------------------------------
-- RBK-008  실패 응답의 Result Set 개수는 T-SQL 로 셀 수 없다.
--   INSERT … EXEC 는 RS 가 2개 이상인 SP 에서 Msg 213 이고(스펙 §33.1a),
--   sys.dm_exec_describe_first_result_set_for_object 는 sp_getapplock 을 부르는
--   Write SP 에서 Msg 11520 이다(스펙 §36 실측). 여기서 PASS 를 찍을 근거가 없다.
--   판정은 tools/verify-docs.js 의 V18 이 한다 - expected-contracts.json 의 모든 실패
--   시나리오가 RS 1개만 선언했는지(202·203 제외) 정적으로 대조한다.
----------------------------------------------------------------------------
PRINT 'NOT RUN RBK-008 실패 응답 RS 개수 - T-SQL 로 셀 수 없다. verify-docs V18 이 판정한다';

-- 이 파일이 바꾼 Fixture 상태를 되돌린다. 변경이력은 되돌리지 않는다 (04 §8.6.3).
DELETE FROM [dbo].[예약접수] WHERE [수검자ID] IN (@P09, @P16);
UPDATE [dbo].[예약접수] SET [상태코드] = 'CNR' WHERE [수검자ID] = @Pf;

-- @Fail 은 이 배치에서만 산다. GO 뒤로 넘기면 Msg 137 이다 (CLAUDE.md §11).
IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 08_Rollback_Tests 완료 ===';
GO
