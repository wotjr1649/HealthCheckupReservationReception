SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- [R13] 업무 운영시간을 이 배치 동안만 넓힌다. 값이 04 §8.7 [운영기준] 에 있어서 가능해졌다 --
--       R12 까지는 SP 안 리터럴이라 창 밖 회차가 성공 경로를 **한 번도** 재지 못했다 (NOT RUN).
-- [!] 넓힌 것은 창뿐이다. 성공 경로가 판정하는 것(정원·중복·상태전이·감사)은 그대로다.
--     창 자체의 계약(308/309 경계)은 이 파일이 아니라 tests/03 의 RUL-T01~T04 와
--     tests/contract/OFF-* 가 **진짜 값으로** 잰다. 그래서 여기서 넓혀도 그 계약은 안 비어 있다.
-- [X] 되돌리지 못한 채 끝나면 다음 회차가 다른 제품을 시험한 PASS 를 낸다 (06 §43-14).
--     파일 끝에서 되돌리고, 되돌아왔는지는 scripts/verify-operating-baseline.sh OPR-G4 가
--     회차마다 따로 판정한다 -- 여기서 THROW 로 빠져나가도 그 게이트는 red 가 된다.
DECLARE @OprFrom TIME(0), @OprTo TIME(0);
SELECT @OprFrom = [운영시작시각], @OprTo = [운영종료시각] FROM [dbo].[운영기준] WHERE [기준ID] = 1;
UPDATE [dbo].[운영기준] SET [운영시작시각] = '00:00:00', [운영종료시각] = '23:59:59' WHERE [기준ID] = 1;

-- [R13] 접수 PM 마감도 늦춘다. 이것이 없으면 CWR-006/007/050 은 15:50 이후 SKIP 이었다.
--       마감 **경계** 자체는 RUL-T07·T08·T11·T12 가 TVF 에 시각을 주입해 진짜 값으로 잰다.
--       여기서 재는 것은 마감이 아니라 '마감 전이면 RCP 로 전이하고 다른 컬럼은 안 건드린다' 다.
DECLARE @OprPm TIME(0) = (SELECT [접수PM마감] FROM [dbo].[운영기준] WHERE [기준ID] = 1);
UPDATE [dbo].[운영기준] SET [접수PM마감] = '23:59:59' WHERE [기준ID] = 1;

-- 이 파일은 DB 상태 불변조건만 판정한다. RS0 결과코드 와 RS 형상은
-- tests/contract/CWR-* + tools/verify-contract.js 가 판정한다 (스펙 §33.1a).
--
-- 업무일 가드 (스펙 §33.2a)
-- [!] 업무일 밖이라고 건너뛰지 않는다. 밖에서 Write SP 가 308 을 내고 **아무것도 바꾸지
--     않는다** 는 것은 계약이지 시험 사정이 아니다 (05 §5 우선순위 9번).
--     SKIP 은 PASS 가 아니므로(CLAUDE.md §10) 밖에서는 그 계약을 실제로 판정한다.
--     아래 호출들은 **안이면 성공했을 인자**다. 그래서 "실패라 안 바뀌었다" 가 아니라
--     "업무일이 아니라 성공 경로가 차단됐다" 를 본다. RS0 결과코드 판정은 tests/contract/OFF-* 가 한다.
-- [R13] 시각 조건이 빠졌다 -- 위에서 창을 넓혔으므로 언제나 참이다. 남은 것은 요일과 휴무일이고
--       이 둘은 00 이 **날짜로** 정한 것이라 넓힐 대상이 아니다. 일요일·휴무일에는 여전히
--       NOT RUN 이며, 그때도 아래 분기가 "아무것도 바뀌지 않았다" 를 실제로 판정한다.
DECLARE @BizOk BIT = CASE WHEN DATEPART(WEEKDAY, SYSDATETIME()) BETWEEN 2 AND 7
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

    DECLARE @OW BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                           JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                          WHERE p.[차트번호] = N'T014' AND w.[상태코드] = 'RCP');
    DECLARE @ORv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @OW);
    DECLARE @OW2 BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                            JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                           WHERE p.[차트번호] = N'F003' AND w.[상태코드] = 'RSV');
    DECLARE @ORv2 BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @OW2);

    -- 접수완료는 창 안이면 503(미래 예약일)이지만 그보다 공통 업무가능이 먼저다 (05 §12.1).
    EXEC [dbo].[USP_HC_접수_완료] @OW2, @ORv2, N'TEST';
    EXEC [dbo].[USP_HC_접수추가검사_변경] @OW, @ORv, 1,1,0,0,0,0,0, N'TEST';
    EXEC [dbo].[USP_HC_접수_취소] @OW, @ORv, N'TEST';

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
        PRINT 'PASS CWR-OFF 업무일 밖 Write SP 호출이 DB 를 바꾸지 않았다  ' + @Off1;
    ELSE BEGIN PRINT 'FAIL CWR-OFF 창 밖 호출이 데이터를 바꿨다  ' + @Off0 + ' -> ' + @Off1;
               SET @Fail += 1; END

    PRINT 'NOT RUN CWR-001~052 업무일(월~토, 비휴무일) 밖 - 요일·휴무일은 00 이 날짜로 정한 것이라 넓히지 않는다';
    -- [R13] 넓힌 창을 되돌린다. OPR-G4 가 회차 끝에서 이 줄이 실제로 돌았는지 판정한다.
    UPDATE [dbo].[운영기준] SET [운영시작시각] = @OprFrom, [운영종료시각] = @OprTo,
                           [접수PM마감]   = @OprPm  WHERE [기준ID] = 1;

    IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
    PRINT '=== 07_Reception_Write_Tests 완료 (창 밖 분기) ===';
    RETURN;
END

-- [!] 접수완료는 업무일·운영시간 안에서도 **접수마감 전**이어야 성공한다 (05 §2.4).
--     production SP 에 시각 주입 뒷문은 여전히 없다 -- 넣지 않는다. R13 이 바꾼 것은 그것이 아니라
--     마감시각이 **어디에 사는가** 다: SP 안 리터럴이던 것이 04 §8.7 [운영기준] 으로 나왔고,
--     그래서 시험이 시계를 기다리는 대신 값을 옮겨 두 경로를 다 밟을 수 있다.
--     SKIP 은 PASS 가 아니다 (CLAUDE.md §10). 마감 **경계** 자체는 RUL-T07·T08·T11·T12 가
--     TVF 에 시각을 주입해 진짜 값으로 증명한다 -- 이 파일은 경계가 아니라 전이를 잰다.
DECLARE @Now  TIME(0) = CONVERT(TIME(0), SYSDATETIME());
DECLARE @시간대코드 CHAR(2) = CASE WHEN @Now < '11:00:00' THEN 'AM' ELSE 'PM' END;
-- [X] 하나의 3상태 변수로 006 과 009 를 갈랐던 것이 "배타적" 의 원인이었다.
--     배타성은 Rule 이 아니라 **둘 다 같은 시간대 을 쓴 선택**의 결과다. 마감은 시간대 마다 다르다.
--     그 뒤 시간대를 갈라 11:10~15:50 에는 둘 다 성립하게 했지만, 그 밖에서는 여전히 한쪽이 SKIP 이었다.
-- [R13] 이제 시계가 아니라 [운영기준] 이 마감을 정하므로 둘 다 **언제나** 성립한다. PM 마감은 파일
--       진입 시 늦췄고(성공 경로), AM 마감은 CWR-009 바로 앞에서만 앞당긴다(마감경과 경로).
--       @CutPm·@CutAm 이 그래서 사라졌다 -- SKIP 을 없앤 것이지 판정을 없앤 것이 아니다.

DECLARE @P10 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T010');
DECLARE @P09 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T009');
DECLARE @Pf  BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'F020');

IF @P10 IS NULL OR @P09 IS NULL OR @Pf IS NULL
BEGIN
    PRINT N'FAIL 사전조건 T010/T009/F020 중 없는 Fixture 가 있다 (tests/00 을 먼저 실행했는가)';
    SET @Fail += 1;
END

-- 단독 재실행 가능하도록 이 파일이 만든 Work 만 지운다.
DELETE FROM [dbo].[예약접수] WHERE [수검자ID] IN (@P10, @P09);

-- 기본검사(NEX-01) 문자열을 검사코드에서 유도한다.
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

DECLARE @W BIGINT, @Rv BINARY(8), @H0 INT, @H1 INT, @건수 INT;

----------------------------------------------------------------------------
-- UPDATE_접수완료
----------------------------------------------------------------------------
-- CWR-001  미존재 업무ID 는 아무 행도 만들지 않는다
SET @건수 = (SELECT COUNT(*) FROM [dbo].[예약접수]);
EXEC [dbo].[USP_HC_접수_완료] -1, 0x0000000000000001, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[예약접수]) = @건수)
    PRINT 'PASS CWR-001 미존재 Work 호출이 행을 만들지 않았다';
ELSE BEGIN PRINT 'FAIL CWR-001 행이 생겼다'; SET @Fail += 1; END

-- CWR-002  미래 예약일은 접수되지 않는다 (503). F003 은 2026-11-16 이다.
DECLARE @Wf BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                       JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                      WHERE p.[차트번호] = N'F003' AND w.[상태코드] = 'RSV');
DECLARE @Rf BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wf);
EXEC [dbo].[USP_HC_접수_완료] @Wf, @Rf, N'TEST';
IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @Wf) = 'RSV')
    PRINT 'PASS CWR-002 미래 예약일이 RCP 로 전이되지 않았다';
ELSE BEGIN PRINT 'FAIL CWR-002 미래 예약이 접수됐다'; SET @Fail += 1; END

-- CWR-008  과거 예약일도 접수되지 않는다 (503). 정상 SP 로는 만들 수 없어 직접 심는다.
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@P09, DATEADD(DAY, -7, CONVERT(DATE, SYSDATETIME())), 'AM', 'RSV', @Basic, NULL);
DECLARE @Wp BIGINT = SCOPE_IDENTITY();
DECLARE @Rp BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wp);
EXEC [dbo].[USP_HC_접수_완료] @Wp, @Rp, N'TEST';
IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @Wp) = 'RSV')
    PRINT 'PASS CWR-008 과거 예약일이 RCP 로 전이되지 않았다';
ELSE BEGIN PRINT 'FAIL CWR-008 과거 예약이 접수됐다'; SET @Fail += 1; END
DELETE FROM [dbo].[예약접수] WHERE [업무ID] = @Wp;

-- CWR-004 / CWR-010  CNR Work 는 접수되지 않는다 (502)
DECLARE @Wcnr BIGINT = (SELECT TOP (1) [업무ID] FROM [dbo].[예약접수] WHERE [수검자ID] = @Pf);
DECLARE @Rcnr BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wcnr);
EXEC [dbo].[USP_HC_접수_완료] @Wcnr, @Rcnr, N'TEST';
IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @Wcnr) = 'CNR')
    PRINT 'PASS CWR-004/010 CNR Work 가 접수되지 않았다';
ELSE BEGIN PRINT 'FAIL CWR-004/010 CNR 이 RCP 로 전이됐다'; SET @Fail += 1; END

-- CWR-005  CORRUPT-2 (저장 NEX 0행) 는 701 이다. 무결성 검사가 503 보다 앞이라
--          예약일이 미래여도 701 이 나온다 (05 §12.1 검증순서).
DECLARE @Wc2 BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                       WHERE p.[차트번호] = N'T013' AND w.[상태코드] = 'RSV');
IF @Wc2 IS NULL
BEGIN PRINT N'FAIL CWR-005 사전조건 CORRUPT-2 Work 가 없다'; SET @Fail += 1; END
ELSE
BEGIN
    DECLARE @Rc2 BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wc2);
    EXEC [dbo].[USP_HC_접수_완료] @Wc2, @Rc2, N'TEST';
    IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @Wc2) = 'RSV')
        PRINT 'PASS CWR-005 NEX 0행 손상 Work 가 접수되지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-005 손상 Work 가 접수됐다'; SET @Fail += 1; END
END

-- CWR-011  CORRUPT-4. AEX 전용 자리에 NEX 전용 코드가 저장된 역할 불일치 → 701.
--          소비 시나리오 바로 앞에서 만들고 직후에 지운다 (plans/02 손상 Fixture 소유표).
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@P09, CONVERT(DATE, SYSDATETIME()), @시간대코드, 'RSV', @Basic, N'EX001');
DECLARE @Wc4 BIGINT = SCOPE_IDENTITY();
DECLARE @Rc4 BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wc4);
EXEC [dbo].[USP_HC_접수_완료] @Wc4, @Rc4, N'TEST';
IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @Wc4) = 'RSV')
    PRINT 'PASS CWR-011 Master 역할 불일치 Work 가 접수되지 않았다';
ELSE BEGIN PRINT 'FAIL CWR-011 역할 불일치 Work 가 접수됐다'; SET @Fail += 1; END
DELETE FROM [dbo].[예약접수] WHERE [업무ID] = @Wc4;

-- 오늘 Work 를 만든다. 성공 경로는 PM Work(@P10), 마감경과 경로는 AM Work(@P09) 다.
-- CWR-003 은 마감과 무관하다 — 601 이 304 보다 앞에서 판정된다.
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@P10, CONVERT(DATE, SYSDATETIME()), 'PM', 'RSV', @Basic, NULL);
SET @W = SCOPE_IDENTITY();
SET @Rv = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
-- [X] CWR-006 의 '시간대 불변' 단언이 @시간대코드(현재시각으로 정해지는 값)와 비교했다.
--     이 Work 는 위에서 'PM' 리터럴로 만드는데, 09:00~11:00 에 돌리면 @시간대코드 가 'AM' 이라
--     접수가 정상 성공했는데도 FAIL 이 났다. 창 밖에서는 블록이 통째로 SKIP 되고 11:10~15:50
--     에서는 둘 다 'PM' 이라 우연히 맞아, 두 회차를 다 돌려도 드러나지 않았다(실측 2026-09-08 10:00).
--     불변 단언은 **저장된 값**과 비교해야 한다. 생성 시점 값을 여기서 잡는다.
DECLARE @생성시간대 CHAR(2) = (SELECT [시간대코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W);

-- CWR-003  stale 행버전 은 전이시키지 않는다 (601 이 마감보다 앞이다)
EXEC [dbo].[USP_HC_접수_완료] @W, 0x0000000000000001, N'TEST';
IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = 'RSV')
    PRINT 'PASS CWR-003 stale RowVersion 이 접수를 막았다';
ELSE BEGIN PRINT 'FAIL CWR-003 stale 요청이 접수됐다'; SET @Fail += 1; END

-- CWR-009  AM Work · 접수마감 경과 → 304. 상태는 그대로다.
--   @P09 는 CWR-011 이 쓰고 지운 뒤라 이 지점에서 유효업무가 없다.
-- [R13] AM 마감을 이 블록 동안만 앞당긴다. 범위를 블록으로 좁힌 이유가 있다 -- 파일 전체에 걸면
--       위쪽 CWR-011(@시간대코드 가 오전이면 'AM')이 701 대신 304 로 막혀, 지금 재고 있는
--       '역할 불일치' 를 아침 회차에서 잃는다.
DECLARE @OprAm TIME(0) = (SELECT [접수AM마감] FROM [dbo].[운영기준] WHERE [기준ID] = 1);
UPDATE [dbo].[운영기준] SET [접수AM마감] = '00:00:01' WHERE [기준ID] = 1;
BEGIN
    INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
    VALUES (@P09, CONVERT(DATE, SYSDATETIME()), 'AM', 'RSV', @Basic, NULL);
    DECLARE @Wam  BIGINT     = SCOPE_IDENTITY();
    DECLARE @Rvam BINARY(8)  = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wam);
    EXEC [dbo].[USP_HC_접수_완료] @Wam, @Rvam, N'TEST';
    IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @Wam) = 'RSV')
        PRINT 'PASS CWR-009 접수마감(AM) 경과 후 접수가 막혔다';
    ELSE BEGIN PRINT 'FAIL CWR-009 마감 후에 접수됐다'; SET @Fail += 1; END
    DELETE FROM [dbo].[예약접수] WHERE [업무ID] = @Wam;
END
UPDATE [dbo].[운영기준] SET [접수AM마감] = @OprAm WHERE [기준ID] = 1;

BEGIN
    -- CWR-006 / CWR-050  접수 성공 → RCP 전이. 예약일·시간대·검사구성은 바뀌지 않는다.
    SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @W);
    EXEC [dbo].[USP_HC_접수_완료] @W, @Rv, N'TEST';
    SET @H1 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @W);
    IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = 'RCP'
        AND (SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = CONVERT(DATE, SYSDATETIME())
        AND (SELECT [시간대코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @생성시간대
        AND (SELECT [국가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Basic
        AND (SELECT [추가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W) IS NULL)
        PRINT 'PASS CWR-006 접수 성공 - RCP 전이 · 예약일·시간대·검사구성 불변';
    ELSE BEGIN PRINT 'FAIL CWR-006 접수완료가 반영되지 않았거나 다른 컬럼을 건드렸다'; SET @Fail += 1; END

    IF (@H1 = @H0 + 1
        AND EXISTS (SELECT 1 FROM [dbo].[변경이력]
                     WHERE [대상키] = @W AND [컬럼명] = N'상태코드'
                       AND [변경전] = N'RSV' AND [변경후] = N'RCP'))
        PRINT 'PASS CWR-050 UPDATE_접수완료 감사 1행 (상태코드 RSV to RCP)';
    ELSE BEGIN PRINT 'FAIL CWR-050 접수완료 감사 기록 불일치'; SET @Fail += 1; END

    -- CWR-007  이미 RCP 인 Work 재접수 → 502, 아무것도 바뀌지 않는다
    DECLARE @Rv2 BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
    EXEC [dbo].[USP_HC_접수_완료] @W, @Rv2, N'TEST';
    IF ((SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Rv2)
        PRINT 'PASS CWR-007 이미 RCP 인 Work 재접수가 아무것도 바꾸지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-007 재접수가 행을 갱신했다'; SET @Fail += 1; END
END

----------------------------------------------------------------------------
-- UPDATE_접수추가검사  (마감과 무관하다 — 05 §12.2 검증순서에 마감이 없다)
----------------------------------------------------------------------------
DECLARE @W14 BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                       WHERE p.[차트번호] = N'T014' AND w.[상태코드] = 'RCP');
IF @W14 IS NULL
BEGIN
    PRINT N'FAIL 사전조건 T014 의 RCP Work 가 없다 (tests/00b 를 먼저 실행했는가)';
    SET @Fail += 1;
END
ELSE
BEGIN
    DECLARE @D14 DATE = (SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W14);
    DECLARE @N14 NVARCHAR(100) = (SELECT [국가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W14);
    SET @Rv = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W14);

    -- CWR-020  동일 AEX 집합은 No-op. 행버전 이 바뀌지 않는다 (스펙 §28).
    EXEC [dbo].[USP_HC_접수추가검사_변경] @W14, @Rv, 1,0,0,0,0,0,0, N'TEST';
    IF ((SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W14) = @Rv)
        PRINT 'PASS CWR-020 동일 AEX 집합 No-op 이 RowVersion 을 바꾸지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-020 No-op 이 행을 갱신했다'; SET @Fail += 1; END

    -- CWR-021 / CWR-051  AEX 실제 변경. 예약일·시간대·NEX 는 재평가하지도 바꾸지도 않는다.
    SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @W14);
    EXEC [dbo].[USP_HC_접수추가검사_변경] @W14, @Rv, 1,1,0,0,0,0,0, N'TEST';
    SET @H1 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @W14);
    IF ((SELECT [추가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W14) = N'EX014,EX015'
        AND (SELECT [국가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W14) = @N14
        AND (SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W14) = @D14)
        PRINT 'PASS CWR-021 AEX 실제 변경 · 예약일·NEX 불변';
    ELSE BEGIN PRINT 'FAIL CWR-021 AEX 변경이 다른 컬럼을 건드렸다'; SET @Fail += 1; END

    IF (@H1 = @H0 + 1
        AND EXISTS (SELECT 1 FROM [dbo].[변경이력]
                     WHERE [대상키] = @W14 AND [컬럼명] = N'추가검사항목'
                       AND [변경전] = N'EX014' AND [변경후] = N'EX014,EX015'))
        PRINT 'PASS CWR-051 UPDATE_접수추가검사 감사 1행 (추가검사항목)';
    ELSE BEGIN PRINT 'FAIL CWR-051 접수추가검사 감사 기록 불일치'; SET @Fail += 1; END

    -- CWR-022  AEX 전체 해제는 빈 문자열이 아니라 NULL 이다 (04 §8.2.2)
    SET @Rv = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W14);
    EXEC [dbo].[USP_HC_접수추가검사_변경] @W14, @Rv, 0,0,0,0,0,0,0, N'TEST';
    IF ((SELECT [추가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W14) IS NULL)
        PRINT 'PASS CWR-022 AEX 전체 해제가 NULL 로 저장됐다';
    ELSE BEGIN PRINT 'FAIL CWR-022 AEX 전체 해제가 NULL 이 아니다'; SET @Fail += 1; END

    -- CWR-023  남성이 여성 전용 OPT03 요청 → 411, 저장되지 않는다
    SET @Rv = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W14);
    EXEC [dbo].[USP_HC_접수추가검사_변경] @W14, @Rv, 0,0,1,0,0,0,0, N'TEST';
    IF ((SELECT [추가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W14) IS NULL)
        PRINT 'PASS CWR-023 성별 불충족 AEX 가 저장되지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-023 성별 불충족 AEX 가 저장됐다'; SET @Fail += 1; END

    -- CWR-026  stale 행버전 은 아무것도 바꾸지 않는다
    SET @Rv = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W14);
    EXEC [dbo].[USP_HC_접수추가검사_변경] @W14, 0x0000000000000001, 1,1,0,0,0,0,0, N'TEST';
    IF ((SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W14) = @Rv)
        PRINT 'PASS CWR-026 stale RowVersion 이 AEX 를 바꾸지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-026 stale 요청이 반영됐다'; SET @Fail += 1; END
END

-- CWR-024  저장 NEX 에 이미 있는 항목을 AEX 로 요청 → 412. T011(여 만 54세)의 저장 NEX 에 EX012 가 있다.
--          412 는 EX012 로만 발생한다 — Seed 19행 중 NEX·AEX 역할을 동시에 갖는 행이 그것뿐이다 (스펙 §17.2a).
DECLARE @W11 BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                       WHERE p.[차트번호] = N'T011' AND w.[상태코드] = 'RCP');
IF @W11 IS NULL
BEGIN PRINT N'FAIL CWR-024 사전조건 T011 의 RCP Work 가 없다'; SET @Fail += 1; END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[예약접수]
                    WHERE [업무ID] = @W11
                      AND N',' + [국가검사항목] + N',' LIKE N'%,EX012,%')
    BEGIN PRINT N'FAIL CWR-024 사전조건 T011 의 저장 NEX 에 EX012 가 없다'; SET @Fail += 1; END
    ELSE
    BEGIN
        DECLARE @Rv11 BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W11);
        EXEC [dbo].[USP_HC_접수추가검사_변경] @W11, @Rv11, 0,0,0,1,0,0,0, N'TEST';
        IF ((SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W11) = @Rv11
            AND (SELECT [추가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W11) IS NULL)
            PRINT 'PASS CWR-024 저장 NEX 와 중복되는 AEX 가 저장되지 않았다';
        ELSE BEGIN PRINT 'FAIL CWR-024 중복 AEX 가 저장됐다'; SET @Fail += 1; END
    END
END

-- CWR-025  RSV 상태에서 접수추가검사 호출 → 502
DECLARE @Wrsv BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                         JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                        WHERE p.[차트번호] = N'F004' AND w.[상태코드] = 'RSV');
DECLARE @Rrsv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wrsv);
EXEC [dbo].[USP_HC_접수추가검사_변경] @Wrsv, @Rrsv, 1,0,0,0,0,0,0, N'TEST';
IF ((SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wrsv) = @Rrsv
    AND (SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @Wrsv) = 'RSV')
    PRINT 'PASS CWR-025 RSV 상태에서 접수추가검사가 막혔다';
ELSE BEGIN PRINT 'FAIL CWR-025 RSV 상태 Work 의 AEX 가 바뀌었다'; SET @Fail += 1; END

----------------------------------------------------------------------------
-- UPDATE_접수취소
----------------------------------------------------------------------------
-- CWR-040  RSV 상태에서 접수취소 호출 → 502
EXEC [dbo].[USP_HC_접수_취소] @Wrsv, @Rrsv, N'TEST';
IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @Wrsv) = 'RSV')
    PRINT 'PASS CWR-040 RSV 상태에서 접수취소가 막혔다';
ELSE BEGIN PRINT 'FAIL CWR-040 RSV 가 CNC 로 전이됐다'; SET @Fail += 1; END

IF @W14 IS NOT NULL
BEGIN
    DECLARE @Dc NVARCHAR(200) = (SELECT ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
                                   FROM [dbo].[예약접수] WHERE [업무ID] = @W14);
    SET @Rv = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W14);

    -- CWR-041  stale 행버전 은 취소하지 않는다
    EXEC [dbo].[USP_HC_접수_취소] @W14, 0x0000000000000001, N'TEST';
    IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W14) = 'RCP')
        PRINT 'PASS CWR-041 stale RowVersion 이 접수취소를 막았다';
    ELSE BEGIN PRINT 'FAIL CWR-041 stale 요청이 취소했다'; SET @Fail += 1; END

    -- CWR-042 / CWR-052  취소 성공 + 검사구성 보존 + 감사 1행
    SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @W14);
    EXEC [dbo].[USP_HC_접수_취소] @W14, @Rv, N'TEST';
    SET @H1 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @W14);
    IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W14) = 'CNC'
        AND (SELECT ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
               FROM [dbo].[예약접수] WHERE [업무ID] = @W14) = @Dc)
        PRINT 'PASS CWR-042 접수취소 성공 + 검사구성 보존';
    ELSE BEGIN PRINT 'FAIL CWR-042 취소 실패 또는 검사구성 소실'; SET @Fail += 1; END

    IF (@H1 = @H0 + 1
        AND EXISTS (SELECT 1 FROM [dbo].[변경이력]
                     WHERE [대상키] = @W14 AND [컬럼명] = N'상태코드'
                       AND [변경전] = N'RCP' AND [변경후] = N'CNC'))
        PRINT 'PASS CWR-052 UPDATE_접수취소 감사 1행 (상태코드 RCP to CNC)';
    ELSE BEGIN PRINT 'FAIL CWR-052 접수취소 감사 기록 불일치'; SET @Fail += 1; END

    -- CWR-043  CNC 재취소 → 502
    SET @Rv = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W14);
    EXEC [dbo].[USP_HC_접수_취소] @W14, @Rv, N'TEST';
    IF ((SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W14) = @Rv)
        PRINT 'PASS CWR-043 CNC 재취소가 아무것도 바꾸지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-043 CNC 가 다시 취소됐다'; SET @Fail += 1; END

    -- CWR-044  CNC 에서 RSV 로 복원하는 경로가 없다 (05 §12.3). 예약변경도 502 다.
    EXEC [dbo].[USP_HC_예약_변경] @W14, @Rv, '2026-11-18', 'AM', 0,0,0,0,0,0,0, N'TEST';
    IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W14) = 'CNC')
        PRINT 'PASS CWR-044 CNC 에서 RSV 로 복원되지 않았다';
    ELSE BEGIN PRINT 'FAIL CWR-044 CNC 가 되살아났다'; SET @Fail += 1; END
END

----------------------------------------------------------------------------
-- 이 파일이 바꾼 Fixture 상태를 되돌린다. 뒤따르는 게이트(PWR-027·PWR-028)가 T014 의
-- RCP 보유를 전제하고, RUL-A09 가 T011 의 저장 NEX 를 전제한다.
-- 변경이력은 되돌리지 않는다 - 감사 기록은 대상 행보다 오래 산다 (04 §8.6.3).
----------------------------------------------------------------------------
DELETE FROM [dbo].[예약접수] WHERE [수검자ID] IN (@P10, @P09);
UPDATE [dbo].[예약접수] SET [상태코드] = 'RCP', [추가검사항목] = N'EX014'
 WHERE [업무ID] = @W14;
UPDATE [dbo].[예약접수] SET [추가검사항목] = NULL
 WHERE [업무ID] = @W11;

-- [R13] 넓힌 창을 되돌린다. OPR-G4 가 회차 끝에서 이 줄이 실제로 돌았는지 판정한다.
UPDATE [dbo].[운영기준] SET [운영시작시각] = @OprFrom, [운영종료시각] = @OprTo,
                           [접수PM마감]   = @OprPm  WHERE [기준ID] = 1;

-- @Fail 은 이 배치에서만 산다. GO 뒤로 넘기면 Msg 137 이다 (CLAUDE.md §11).
IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 07_Reception_Write_Tests 완료 ===';
GO
