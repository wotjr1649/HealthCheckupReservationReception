SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- 이 파일은 DB 상태 불변조건만 판정한다. RS0 Code 와 RS 형상은
-- tests/contract/RWR-* + tools/verify-contract.js 가 판정한다 (스펙 §33.1a).
--
-- 업무시간 가드 (스펙 §33.2a). Write SP 는 308/309 를 업무 Rule 보다 먼저 판정한다.
-- [!] 업무시간 밖이라고 건너뛰지 않는다. 창 밖에서 Write SP 가 308/309 를 내고 **아무것도
--     바꾸지 않는다** 는 것은 계약이지 시험 사정이 아니다 (05 §5 우선순위 9번).
--     SKIP 은 PASS 가 아니므로(CLAUDE.md §10) 창 밖에서는 그 계약을 실제로 판정한다.
--     아래 호출들은 **창 안이면 성공했을 인자**다. 그래서 "실패라 안 바뀌었다" 가 아니라
--     "업무시간 밖이라 성공 경로가 차단됐다" 를 본다. RS0 Code 판정은 tests/contract/OFF-* 가 한다.
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
    DECLARE @OW BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                           JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                          WHERE p.[차트번호] = N'F003' AND w.[상태코드] = 'RSV');
    DECLARE @ORv BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @OW);
    DECLARE @OW2 BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                            JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                           WHERE p.[차트번호] = N'F004' AND w.[상태코드] = 'RSV');
    DECLARE @ORv2 BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @OW2);

    EXEC [dbo].[USP_HC_INSERT_예약] @OPt, 'NORMAL', '2026-11-17', 'AM', 1,0,0,0,0,0,0, N'TEST';
    EXEC [dbo].[USP_HC_UPDATE_예약변경] @OW, @ORv, '2026-11-16', 'PM', 0,0,0,0,0,0,0, N'TEST';
    EXEC [dbo].[USP_HC_UPDATE_예약취소] @OW2, @ORv2, N'TEST';

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
        PRINT 'PASS RWR-OFF 업무시간 밖 Write SP 호출이 DB 를 바꾸지 않았다  ' + @Off1;
    ELSE BEGIN PRINT 'FAIL RWR-OFF 창 밖 호출이 데이터를 바꿨다  ' + @Off0 + ' -> ' + @Off1;
               SET @Fail += 1; END

    PRINT 'NOT RUN RWR-001~052 업무시간(월~토 09:00~18:00, 비휴무일) 밖 - 성공 경로는 창 안에서만 성립한다';
    IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
    PRINT '=== 06_Reservation_Write_Tests 완료 (창 밖 분기) ===';
    RETURN;
END

-- 요일 (실측): 2026-11-16 월 · 17 화 · 18 수 · 19 목 · 20 금 · 21 토 · 22 일 · 24 화 · 25 수
--             2026-12-25 는 휴무일 Seed 다.

DECLARE @Pt  BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
DECLARE @P18 BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T017');
DECLARE @P2  BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T012');
DECLARE @Pc  BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T018');
DECLARE @Pn  BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T016');
DECLARE @Pf  BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'F020');

IF @Pt IS NULL OR @P18 IS NULL OR @P2 IS NULL OR @Pc IS NULL OR @Pn IS NULL OR @Pf IS NULL
BEGIN
    PRINT N'FAIL 사전조건 T015/T017/T012/T018/T016/F020 중 없는 Fixture 가 있다 (tests/00 을 먼저 실행했는가)';
    SET @Fail += 1;
END

-- 단독 재실행 가능하도록 이 파일이 만든 Work 만 지우고 F0 계열 상태를 Fixture 값으로 되돌린다.
DELETE FROM [dbo].[예약접수] WHERE [수검자ID] IN (@Pt, @P18, @Pc, @Pn);
UPDATE [dbo].[예약접수] SET [상태코드] = 'RSV'
 WHERE [예약일] = '2026-11-16' AND [시간대코드] = 'AM' AND [상태코드] = 'CNR'
   AND [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자]
                       WHERE [차트번호] LIKE 'F0%' AND [차트번호] <> N'F020');
UPDATE [dbo].[예약접수] SET [상태코드] = 'CNR' WHERE [수검자ID] = @Pf;

-- 기본검사(NEX-01) 문자열을 검사코드에서 유도한다. 손으로 적으면 Seed 와 어긋난다.
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

DECLARE @Before INT, @H0 INT, @H1 INT;
DECLARE @W BIGINT, @Rv BINARY(8), @Cfg NVARCHAR(200), @Nex NVARCHAR(100), @Aex NVARCHAR(50);

----------------------------------------------------------------------------
-- INSERT_예약
----------------------------------------------------------------------------
-- RWR-001~007  실패 경로는 Work 를 만들지 않는다
SET @Before = (SELECT COUNT(*) FROM [dbo].[예약접수]);
EXEC [dbo].[USP_HC_INSERT_예약] @Pt,  'NORMAL', '2020-01-06', 'AM', 0,0,0,0,0,0,0, N'TEST';  -- 300 과거일
EXEC [dbo].[USP_HC_INSERT_예약] @Pt,  'NORMAL', '2026-11-22', 'AM', 0,0,0,0,0,0,0, N'TEST';  -- 301 일요일
EXEC [dbo].[USP_HC_INSERT_예약] @Pt,  'NORMAL', '2026-12-25', 'AM', 0,0,0,0,0,0,0, N'TEST';  -- 302 휴무일
EXEC [dbo].[USP_HC_INSERT_예약] @Pt,  'NORMAL', '2026-11-21', 'PM', 0,0,0,0,0,0,0, N'TEST';  -- 303 토요일 PM
EXEC [dbo].[USP_HC_INSERT_예약] @Pt,  'WALKIN', '2026-11-16', 'AM', 0,0,0,0,0,0,0, N'TEST';  -- 102 WalkIn 날짜
EXEC [dbo].[USP_HC_INSERT_예약] @P18, 'NORMAL', '2026-11-17', 'AM', 0,0,0,0,0,0,0, N'TEST';  -- 400 만 18세
EXEC [dbo].[USP_HC_INSERT_예약] @Pt,  'NORMAL', '2026-11-17', 'AM', 0,0,1,0,0,0,0, N'TEST';  -- 411 남성 OPT03
IF ((SELECT COUNT(*) FROM [dbo].[예약접수]) = @Before)
    PRINT 'PASS RWR-001~007 실패 경로가 Work 를 만들지 않았다';
ELSE BEGIN PRINT 'FAIL RWR-001~007 실패 경로가 Work 를 남겼다'; SET @Fail += 1; END

-- RWR-008  신규예약이 RSV Work 로 저장된다
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상테이블] = N'예약접수');
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-17', 'AM', 1,0,0,0,0,0,0, N'TEST';
SET @W = (SELECT TOP (1) [업무ID] FROM [dbo].[예약접수]
           WHERE [수검자ID] = @Pt AND [상태코드] = 'RSV' ORDER BY [업무ID] DESC);
IF (@W IS NOT NULL
    AND (SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = '2026-11-17'
    AND (SELECT [시간대코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = 'AM')
    PRINT 'PASS RWR-008 신규예약이 RSV Work 로 저장됐다';
ELSE BEGIN PRINT 'FAIL RWR-008 신규예약이 저장되지 않았다'; SET @Fail += 1; END

-- RWR-009  검사구성 = NEX 8~11 + Selected AEX. 빈 문자열은 0개다 (스펙 §21.2a).
SELECT @Nex = [국가검사항목], @Aex = [추가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W;
IF ((CASE WHEN LEN(ISNULL(@Nex, N'')) = 0 THEN 0
          ELSE LEN(@Nex) - LEN(REPLACE(@Nex, N',', N'')) + 1 END) BETWEEN 8 AND 11
    AND @Aex = N'EX014')
    PRINT 'PASS RWR-009 검사구성 NEX 8~11종 + 요청 AEX 1종';
ELSE BEGIN PRINT 'FAIL RWR-009 검사구성 조립 불일치'; SET @Fail += 1; END

-- RWR-050  INSERT_예약 감사. 값이 들어간 컬럼마다 1행, 변경전은 전건 NULL.
SET @H1 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @W AND [대상테이블] = N'예약접수');
IF (@H1 = 6
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력] WHERE [대상키] = @W AND [변경전] IS NOT NULL)
    AND EXISTS (SELECT 1 FROM [dbo].[변경이력]
                 WHERE [대상키] = @W AND [컬럼명] = N'상태코드' AND [변경후] = N'RSV'))
    PRINT 'PASS RWR-050 INSERT_예약 감사 6행 · 변경전 전건 NULL';
ELSE BEGIN PRINT 'FAIL RWR-050 INSERT_예약 감사 기록 불일치'; SET @Fail += 1; END

-- RWR-010 / RWR-032  타 유효업무 1건 → 306. 저장되지 않는다 (00 RP-06)
SET @Before = (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [수검자ID] = @Pt);
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-18', 'AM', 0,0,0,0,0,0,0, N'TEST';
EXEC [dbo].[USP_HC_INSERT_예약] @Pt, 'NORMAL', '2026-11-19', 'AM', 0,0,0,0,0,0,0, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [수검자ID] = @Pt) = @Before)
    PRINT 'PASS RWR-010/032 유효업무 1건 보유 시 재예약이 저장되지 않았다';
ELSE BEGIN PRINT 'FAIL RWR-010/032 RP-06 위반'; SET @Fail += 1; END

-- RWR-011 / RWR-033  타 유효업무 2건은 306 이 아니라 701 이다 (CORRUPT-1)
SET @Before = (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [수검자ID] = @P2);
EXEC [dbo].[USP_HC_INSERT_예약] @P2, 'NORMAL', '2026-11-19', 'AM', 0,0,0,0,0,0,0, N'TEST';
EXEC [dbo].[USP_HC_INSERT_예약] @P2, 'NORMAL', '2026-11-20', 'AM', 0,0,0,0,0,0,0, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [수검자ID] = @P2) = @Before)
    PRINT 'PASS RWR-011/033 유효업무 2건 손상 상태에서 저장되지 않았다';
ELSE BEGIN PRINT 'FAIL RWR-011/033 손상 상태에서 저장됐다'; SET @Fail += 1; END

-- RWR-026  CNR Work 는 변경할 수 없다 (502). F020 이 Fixture 상 CNR 이다.
DECLARE @Wcnr BIGINT = (SELECT TOP (1) [업무ID] FROM [dbo].[예약접수] WHERE [수검자ID] = @Pf);
DECLARE @RvCnr BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wcnr);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @Wcnr, @RvCnr, '2026-11-18', 'PM', 0,0,0,0,0,0,0, N'TEST';
IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @Wcnr) = 'CNR'
    AND (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wcnr) = @RvCnr)
    PRINT 'PASS RWR-026 CNR Work 는 변경되지 않았다';
ELSE BEGIN PRINT 'FAIL RWR-026 CNR Work 가 변경됐다'; SET @Fail += 1; END

-- RWR-012  F020 을 RSV 로 뒤집어 20/20 을 만든 뒤 정원 초과 저장이 막히는지 본다.
--   대상 수검자는 유효업무가 없어야 한다 — 있으면 305 보다 306 이 먼저 걸린다(05 §11.1 검증순서).
--   T018(만 44세 여, 완료이력 없음, Work 없음)을 쓴다.
UPDATE [dbo].[예약접수] SET [상태코드] = 'RSV' WHERE [수검자ID] = @Pf;
DECLARE @Slot INT = (SELECT COUNT(*) FROM [dbo].[예약접수]
                      WHERE [예약일] = '2026-11-16' AND [시간대코드] = 'AM'
                        AND [상태코드] IN ('RSV', 'RCP'));
IF @Slot = 20 PRINT 'PASS RWR-012 사전조건 20/20 성립';
ELSE BEGIN PRINT 'FAIL RWR-012 사전조건 Slot=' + CONVERT(VARCHAR(5), @Slot); SET @Fail += 1; END

EXEC [dbo].[USP_HC_INSERT_예약] @Pc, 'NORMAL', '2026-11-16', 'AM', 0,0,0,0,0,0,0, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[예약접수]
      WHERE [예약일] = '2026-11-16' AND [시간대코드] = 'AM'
        AND [상태코드] IN ('RSV', 'RCP')) = 20)
    PRINT 'PASS RWR-012 정원 20 초과 저장이 차단됐다 (RP-03)';
ELSE BEGIN PRINT N'FAIL RWR-012 정원 21건 RP-03 위반'; SET @Fail += 1; END

-- RWR-030  20/20 Slot 에 있는 Work 의 AEX 변경은 305 로 막히지 않는다 (스펙 §30.1).
--   자기 자신을 정원에 두 번 세면 이 호출이 실패한다.
DECLARE @W30 BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                       WHERE p.[차트번호] = N'F003' AND w.[상태코드] = 'RSV');
DECLARE @Rv30 BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W30);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W30, @Rv30, '2026-11-16', 'AM', 1,0,0,0,0,0,0, N'TEST';
IF ((SELECT [추가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W30) = N'EX014')
    PRINT 'PASS RWR-030 20/20 Slot 유지 변경이 정원으로 막히지 않았다';
ELSE BEGIN PRINT 'FAIL RWR-030 자기 Work 를 정원에 이중계상했다'; SET @Fail += 1; END

UPDATE [dbo].[예약접수] SET [상태코드] = 'CNR' WHERE [수검자ID] = @Pf;   -- F020 복원

----------------------------------------------------------------------------
-- UPDATE_예약변경
----------------------------------------------------------------------------
SET @Rv  = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
SET @Cfg = (SELECT ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
              FROM [dbo].[예약접수] WHERE [업무ID] = @W);

-- RWR-020  No-op 은 RowVersion 을 바꾸지 않는다 (스펙 §28)
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, @Rv, '2026-11-17', 'AM', 1,0,0,0,0,0,0, N'TEST';
IF ((SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Rv)
    PRINT 'PASS RWR-020 No-op 은 RowVersion 을 바꾸지 않는다';
ELSE BEGIN PRINT 'FAIL RWR-020 No-op 이 행을 갱신했다'; SET @Fail += 1; END

-- RWR-025 / RWR-027  stale RowVersion 과 미존재 WorkId 는 아무것도 바꾸지 않는다
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, 0x0000000000000001, '2026-11-18', 'PM', 1,0,0,0,0,0,0, N'TEST';
EXEC [dbo].[USP_HC_UPDATE_예약변경] -1, @Rv, '2026-11-18', 'PM', 1,0,0,0,0,0,0, N'TEST';
IF ((SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Rv
    AND (SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = '2026-11-17')
    PRINT 'PASS RWR-025/027 stale RowVersion·미존재 WorkId 가 데이터를 바꾸지 않았다';
ELSE BEGIN PRINT 'FAIL RWR-025/027 실패 경로가 데이터를 바꿨다'; SET @Fail += 1; END

-- RWR-021 / RWR-028  시간대만 변경 — 검사구성 불변이고, 자기 Work 를 306 으로 오인하지 않는다
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, @Rv, '2026-11-17', 'PM', 1,0,0,0,0,0,0, N'TEST';
IF ((SELECT [시간대코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = 'PM'
    AND (SELECT ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
           FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Cfg)
    PRINT 'PASS RWR-021/028 시간대만 변경 시 검사구성 불변 · 자기 Work 오탐 없음';
ELSE BEGIN PRINT 'FAIL RWR-021/028 시간대 변경이 검사구성을 건드렸거나 306 으로 막혔다'; SET @Fail += 1; END

-- RWR-022  AEX 만 변경 — 추가검사항목만 바뀌고 국가검사항목은 그대로다
SET @Rv  = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
SET @Nex = (SELECT [국가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, @Rv, '2026-11-17', 'PM', 1,1,0,0,0,0,0, N'TEST';
IF ((SELECT [추가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = N'EX014,EX015'
    AND (SELECT [국가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Nex)
    PRINT 'PASS RWR-022 AEX 만 변경 시 국가검사항목 불변';
ELSE BEGIN PRINT 'FAIL RWR-022 AEX 변경이 국가검사항목을 건드렸다'; SET @Fail += 1; END

-- RWR-024  시간대 + AEX 동시 변경
SET @Rv = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, @Rv, '2026-11-17', 'AM', 1,0,0,0,0,0,0, N'TEST';
IF ((SELECT [시간대코드] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = 'AM'
    AND (SELECT [추가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = N'EX014'
    AND (SELECT [국가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = @Nex)
    PRINT 'PASS RWR-024 시간대 + AEX 동시 변경이 둘 다 반영됐다';
ELSE BEGIN PRINT 'FAIL RWR-024 시간대 + AEX 동시 변경 불일치'; SET @Fail += 1; END

-- RWR-023 / RWR-051  예약일 변경 — 검사구성을 새 예약일 기준으로 재조립하고 감사행을 남긴다
SET @Rv = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W);
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @W);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W, @Rv, '2026-11-19', 'AM', 1,0,0,0,0,0,0, N'TEST';
SET @H1 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @W);
IF ((SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W) = '2026-11-19'
    AND (SELECT COUNT(*) FROM [dbo].[UFN_HC_국가검사구성](@Pt, '2026-11-19')) =
        (SELECT LEN([국가검사항목]) - LEN(REPLACE([국가검사항목], N',', N'')) + 1
           FROM [dbo].[예약접수] WHERE [업무ID] = @W))
    PRINT 'PASS RWR-023 예약일 변경이 검사구성을 새 기준일로 재조립했다';
ELSE BEGIN PRINT 'FAIL RWR-023 예약일 변경 후 검사구성이 새 기준과 다르다'; SET @Fail += 1; END

-- 예약일만 바뀌고 검사구성 결과가 같으면 감사행은 예약일 1건이다. 값이 같은 컬럼은 남지 않는다.
IF (@H1 = @H0 + 1
    AND EXISTS (SELECT 1 FROM [dbo].[변경이력]
                 WHERE [대상키] = @W AND [컬럼명] = N'예약일'
                   AND [변경전] = N'2026-11-17' AND [변경후] = N'2026-11-19')
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]
                     WHERE [대상키] = @W
                       AND ISNULL([변경전], N'~NULL~') = ISNULL([변경후], N'~NULL~')))
    PRINT 'PASS RWR-051 UPDATE_예약변경 감사 · 바뀐 컬럼만 · 무변경 컬럼 0행';
ELSE BEGIN PRINT 'FAIL RWR-051 UPDATE_예약변경 감사 기록 불일치'; SET @Fail += 1; END

-- RWR-029  예약일 변경 후 TGT 비대상이면 기존 Work 를 전혀 바꾸지 않는다.
--   [!] 400 UnderAge 는 예약변경으로 도달할 수 없다 — 미래로 옮길수록 나이가 늘기 때문이다.
--       T017 도 2026-11-18 에 만 19세가 되고 그 뒤로는 계속 늘어난다. 401 NotDue 로 시험한다.
--       T016 은 2025-05-01 완료이력이 있어 2026 의 어떤 예약일에도 2년 주기가 도래하지 않는다.
--       TGT 비대상은 정상 SP 로 Work 를 만들 수 없으므로 직접 INSERT 로 심는다.
INSERT INTO [dbo].[예약접수] ([수검자ID], [예약일], [시간대코드], [상태코드], [국가검사항목], [추가검사항목])
VALUES (@Pn, '2026-11-24', 'AM', 'RSV', @Basic, NULL);
DECLARE @W29 BIGINT = SCOPE_IDENTITY();
DECLARE @Rv29 BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W29);
EXEC [dbo].[USP_HC_UPDATE_예약변경] @W29, @Rv29, '2026-11-25', 'AM', 0,0,0,0,0,0,0, N'TEST';
IF ((SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W29) = '2026-11-24'
    AND (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W29) = @Rv29)
    PRINT 'PASS RWR-029 TGT 비대상 예약일로의 변경이 기존 Work 를 보존했다';
ELSE BEGIN PRINT 'FAIL RWR-029 TGT 비대상인데 Work 가 변경됐다'; SET @Fail += 1; END
DELETE FROM [dbo].[예약접수] WHERE [업무ID] = @W29;

-- RWR-031  예약일 이동으로 만나이가 경계를 넘으면 AEX 중복을 **변경 후** 구성으로 판정한다.
--   T020(1972-11-20생 여)은 2026-11-17 에 만 53세라 EX012 가 없고, 2026-11-20 에 만 54세가 되어
--   NEX-05 로 EX012 가 생긴다. 그러면 이미 선택된 OPT04(EX012)가 412 다.
--   변경 전 구성으로 판정하면 이 시나리오가 조용히 통과한다 — 그것이 이 시험의 표적이다.
DECLARE @W31 BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                       WHERE p.[차트번호] = N'T020' AND w.[상태코드] = 'RSV');
IF @W31 IS NULL
BEGIN PRINT N'FAIL RWR-031 사전조건 T020 의 RSV Work 가 없다'; SET @Fail += 1; END
ELSE
BEGIN
    DECLARE @Rv31 BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W31);
    DECLARE @D31  DATE      = (SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W31);
    DECLARE @P31  BIGINT    = (SELECT [수검자ID] FROM [dbo].[예약접수] WHERE [업무ID] = @W31);
    IF EXISTS (SELECT 1 FROM [dbo].[UFN_HC_국가검사구성](@P31, @D31) WHERE ExamCode = 'EX012')
    BEGIN PRINT N'FAIL RWR-031 사전조건 변경 전에 이미 EX012 가 있다'; SET @Fail += 1; END
    ELSE
    BEGIN
        EXEC [dbo].[USP_HC_UPDATE_예약변경] @W31, @Rv31, '2026-11-20', 'AM', 0,0,0,1,0,0,0, N'TEST';
        IF ((SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W31) = @D31
            AND (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W31) = @Rv31)
            PRINT 'PASS RWR-031 변경 후 나이 기준으로 AEX 중복을 판정해 저장을 막았다';
        ELSE BEGIN PRINT 'FAIL RWR-031 변경 전 구성으로 판정해 중복 AEX 가 저장됐다'; SET @Fail += 1; END
    END
END

-- RWR-034  CORRUPT-5. 저장 NEX 가 상한 11 을 넘으면 변경을 거부한다 (스펙 §21.2a-(1)).
--   소비 테스트 바로 앞에서 만들고 직후에 되돌린다 — Fixture 파일에 두면 뒤 테스트가 전부 오염된다.
DECLARE @W34 BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                       WHERE p.[차트번호] = N'F002' AND w.[상태코드] = 'RSV');
IF @W34 IS NULL
BEGIN PRINT N'FAIL RWR-034 사전조건 CORRUPT-5 대상 Work(F002)가 없다'; SET @Fail += 1; END
ELSE
BEGIN
    DECLARE @Save5 NVARCHAR(100) = (SELECT [국가검사항목] FROM [dbo].[예약접수] WHERE [업무ID] = @W34);
    DECLARE @All5 NVARCHAR(100) = N'', @C5 VARCHAR(10);
    DECLARE @N5 TABLE (C VARCHAR(10) PRIMARY KEY);
    INSERT INTO @N5 (C) SELECT [검사항목코드] FROM [dbo].[검사코드] WHERE [국가검사규칙코드] IS NOT NULL;
    WHILE EXISTS (SELECT 1 FROM @N5)
    BEGIN
        SELECT TOP (1) @C5 = C FROM @N5 ORDER BY C;
        SET @All5 = @All5 + @C5 + N',';
        DELETE FROM @N5 WHERE C = @C5;
    END
    UPDATE [dbo].[예약접수] SET [국가검사항목] = LEFT(@All5, LEN(@All5) - 1) WHERE [업무ID] = @W34;

    IF ((SELECT LEN([국가검사항목]) - LEN(REPLACE([국가검사항목], N',', N'')) + 1
           FROM [dbo].[예약접수] WHERE [업무ID] = @W34) <= 11)
    BEGIN PRINT 'FAIL CORRUPT-5 NEX 가 12종에 도달하지 못했다'; SET @Fail += 1; END

    DECLARE @Rv34 BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W34);
    EXEC [dbo].[USP_HC_UPDATE_예약변경] @W34, @Rv34, '2026-11-19', 'AM', 0,0,0,0,0,0,0, N'TEST';
    IF ((SELECT [예약일] FROM [dbo].[예약접수] WHERE [업무ID] = @W34) = '2026-11-16'
        AND (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @W34) = @Rv34)
        PRINT 'PASS RWR-034 NEX 상한 초과 손상 Work 는 변경되지 않았다';
    ELSE BEGIN PRINT 'FAIL RWR-034 손상 Work 가 변경됐다'; SET @Fail += 1; END

    UPDATE [dbo].[예약접수] SET [국가검사항목] = @Save5 WHERE [업무ID] = @W34;   -- CORRUPT-5 복원
END

----------------------------------------------------------------------------
-- UPDATE_예약취소
----------------------------------------------------------------------------
-- 취소 대상은 ORDER BY 업무ID 로 F001 을 고정한다. F020 은 RWR-012 전용이라 건드리지 않는다.
DECLARE @Wc BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                       JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                      WHERE p.[차트번호] LIKE 'F0%' AND p.[차트번호] <> N'F020'
                        AND w.[상태코드] = 'RSV' ORDER BY w.[업무ID]);
DECLARE @Dc  NVARCHAR(200) = (SELECT ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
                                FROM [dbo].[예약접수] WHERE [업무ID] = @Wc);
DECLARE @RvC BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wc);
DECLARE @Cap0 INT = (SELECT COUNT(*) FROM [dbo].[예약접수]
                      WHERE [예약일] = '2026-11-16' AND [시간대코드] = 'AM'
                        AND [상태코드] IN ('RSV', 'RCP'));

-- RWR-040 / RWR-041  미존재 WorkId 와 stale RowVersion 은 아무것도 바꾸지 않는다
EXEC [dbo].[USP_HC_UPDATE_예약취소] -1, 0x0000000000000001, N'TEST';
EXEC [dbo].[USP_HC_UPDATE_예약취소] @Wc, 0x0000000000000001, N'TEST';
IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @Wc) = 'RSV')
    PRINT 'PASS RWR-040/041 미존재 WorkId·stale RowVersion 이 취소하지 않았다';
ELSE BEGIN PRINT 'FAIL RWR-040/041 실패 경로가 취소했다'; SET @Fail += 1; END

-- RWR-043 / RWR-052  취소 성공 + 검사구성 보존 + 감사 1행
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Wc);
EXEC [dbo].[USP_HC_UPDATE_예약취소] @Wc, @RvC, N'TEST';
SET @H1 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Wc);
IF ((SELECT [상태코드] FROM [dbo].[예약접수] WHERE [업무ID] = @Wc) = 'CNR'
    AND (SELECT ISNULL([국가검사항목], N'') + N'|' + ISNULL([추가검사항목], N'')
           FROM [dbo].[예약접수] WHERE [업무ID] = @Wc) = @Dc)
    PRINT 'PASS RWR-043 취소 성공 + 검사구성 보존';
ELSE BEGIN PRINT 'FAIL RWR-043 취소 실패 또는 검사구성 소실'; SET @Fail += 1; END

IF (@H1 = @H0 + 1
    AND EXISTS (SELECT 1 FROM [dbo].[변경이력]
                 WHERE [대상키] = @Wc AND [컬럼명] = N'상태코드'
                   AND [변경전] = N'RSV' AND [변경후] = N'CNR'))
    PRINT 'PASS RWR-052 UPDATE_예약취소 감사 1행 (상태코드 RSV to CNR)';
ELSE BEGIN PRINT 'FAIL RWR-052 UPDATE_예약취소 감사 기록 불일치'; SET @Fail += 1; END

-- RWR-042  이미 CNR 인 Work 는 다시 취소되지 않는다 (502)
DECLARE @RvC2 BINARY(8) = (SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wc);
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Wc);
EXEC [dbo].[USP_HC_UPDATE_예약취소] @Wc, @RvC2, N'TEST';
IF ((SELECT [행버전] FROM [dbo].[예약접수] WHERE [업무ID] = @Wc) = @RvC2
    AND (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Wc) = @H0)
    PRINT 'PASS RWR-042 CNR Work 재취소가 데이터도 감사도 남기지 않았다';
ELSE BEGIN PRINT 'FAIL RWR-042 CNR Work 가 다시 취소됐다'; SET @Fail += 1; END

-- RWR-044  취소는 그 Slot 의 정원을 1 줄인다 (00 RP-03 의 CurrentCount 정의)
IF ((SELECT COUNT(*) FROM [dbo].[예약접수]
      WHERE [예약일] = '2026-11-16' AND [시간대코드] = 'AM'
        AND [상태코드] IN ('RSV', 'RCP')) = @Cap0 - 1)
    PRINT 'PASS RWR-044 취소 후 Slot 정원이 1 줄었다';
ELSE BEGIN PRINT 'FAIL RWR-044 취소가 정원에 반영되지 않았다'; SET @Fail += 1; END


-- 이 파일이 바꾼 Fixture 상태를 되돌린다. 뒤따르는 게이트(계약 시나리오 09·13·PWR-023)가
-- T015 의 유효업무 0건 · 2026-11 Work 24건 · F001 의 RSV 를 전제하기 때문이다.
-- 변경이력은 되돌리지 않는다 - 감사 기록은 대상 행보다 오래 산다 (04 §8.6.3).
DELETE FROM [dbo].[예약접수]
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자]
                       WHERE [차트번호] IN (N'T015', N'T016', N'T017', N'T018'));
UPDATE [dbo].[예약접수] SET [상태코드] = 'RSV'
 WHERE [예약일] = '2026-11-16' AND [시간대코드] = 'AM' AND [상태코드] = 'CNR'
   AND [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자]
                       WHERE [차트번호] LIKE 'F0%' AND [차트번호] <> N'F020');
UPDATE [dbo].[예약접수] SET [상태코드] = 'CNR'
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'F020');
UPDATE [dbo].[예약접수] SET [추가검사항목] = NULL
 WHERE [추가검사항목] IS NOT NULL
   AND [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] LIKE 'F0%');
UPDATE [dbo].[예약접수] SET [예약일] = '2026-11-17', [시간대코드] = 'AM', [추가검사항목] = N'EX012'
 WHERE [수검자ID] IN (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T020');

-- @Fail 은 이 배치에서만 산다. GO 뒤로 넘기면 Msg 137 이다 (CLAUDE.md §11).
IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 06_Reservation_Write_Tests 완료 ===';
GO
