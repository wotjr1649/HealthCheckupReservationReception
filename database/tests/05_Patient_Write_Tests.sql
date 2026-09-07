SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- 이 파일은 DB 상태 불변조건만 판정한다. RS0 Code 와 RS 형상은
-- tests/contract/PWR-* + tools/verify-contract.js 가 판정한다 (스펙 §33.1a).
-- INSERT … EXEC 를 쓰지 않는다 — RS 가 2개인 SP 는 Msg 213 이고 내부 ROLLBACK 은 Msg 3915 다.
--
-- 업무시간 가드 (스펙 §33.2a). Write SP 는 308/309 를 업무 Rule 보다 먼저 판정하므로
-- 업무시간 밖에서는 성공 경로가 하나도 성립하지 않는다. SKIP 은 PASS 가 아니다 (CLAUDE.md §10).
-- [!] 업무시간 밖이라고 건너뛰지 않는다. 창 밖에서 Write SP 가 308/309 를 내고 **아무것도
--     바꾸지 않는다** 는 것은 계약이지 시험 사정이 아니다 (05 §5 우선순위 9번).
--     SKIP 은 PASS 가 아니므로(CLAUDE.md §10) 창 밖에서는 그 계약을 실제로 판정한다.
--     RS0 Code 자체는 이 파일에서 받을 수 없다(INSERT … EXEC 금지, §33.1a) —
--     308/309 판정은 tests/contract/OFF-* 가 한다.
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
        + CONVERT(VARCHAR(20), (SELECT ISNULL(SUM(CONVERT(BIGINT,
                     DATEDIFF(SECOND, '2020-01-01', [최종수정일시]))), 0) FROM [dbo].[수검자]));

    DECLARE @Op BIGINT = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [차트번호] = N'T015');
    DECLARE @Ol DATETIME = (SELECT [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Op);
    DECLARE @Os VARCHAR(13) = (SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Op);

    -- 두 Write SP 모두 공통 업무가능을 업무 Rule 보다 먼저 판정한다.
    EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'창밖등록', '9501011000019',
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
    EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Op, @Ol, N'T015', N'창밖수정', @Os,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, N'TEST';

    DECLARE @Off1 VARCHAR(300) =
          CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[수검자]))     + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[예약접수]))   + '|'
        + CONVERT(VARCHAR(12), (SELECT COUNT(*) FROM [dbo].[변경이력]))   + '|'
        + CONVERT(VARCHAR(20), (SELECT ISNULL(SUM(CONVERT(BIGINT,
                     DATEDIFF(SECOND, '2020-01-01', [최종수정일시]))), 0) FROM [dbo].[수검자]));

    IF @Off0 = @Off1 AND @@TRANCOUNT = 0
        PRINT 'PASS PWR-OFF 업무시간 밖 Write SP 호출이 DB 를 바꾸지 않았다  ' + @Off1;
    ELSE BEGIN PRINT 'FAIL PWR-OFF 창 밖 호출이 데이터를 바꿨다  ' + @Off0 + ' -> ' + @Off1;
               SET @Fail += 1; END

    PRINT 'NOT RUN PWR-001~031 업무시간(월~토 09:00~18:00, 비휴무일) 밖 - 성공 경로는 창 안에서만 성립한다';
    IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
    PRINT '=== 05_Patient_Write_Tests 완료 (창 밖 분기) ===';
    RETURN;
END

-- 주민번호는 체크디지트가 무효인 값이다 (스펙 §16.1). 950101 생, 7번째 자리 1(남)/2(여).
--   '950101100001' 의 유효 검증번호는 8 → 9 를 쓴다
--   '950101200001' 의 유효 검증번호는 1 → 8 을 쓴다
-- 두 값은 같은 생년월일(19950101)을 산출하므로 203 유사후보 판정에 그대로 쓴다.
DECLARE @SsnA VARCHAR(13) = '9501011000019';
DECLARE @SsnB VARCHAR(13) = '9501012000018';
DECLARE @NmA  NVARCHAR(100) = N'산출확인';

-- 단독 재실행 가능하도록 이 파일이 만든 것만 먼저 지운다 (tests/00 은 전건을 지운다).
DELETE FROM [dbo].[수검자] WHERE [주민번호] IN (@SsnA, @SsnB);

DECLARE @H0 INT, @H1 INT, @Pid BIGINT, @Bid BIGINT;
DECLARE @Led DATETIME, @Before DATETIME;
DECLARE @Cn NVARCHAR(100), @Nm NVARCHAR(100), @Ssn VARCHAR(13);

----------------------------------------------------------------------------
-- INSERT_수검자
----------------------------------------------------------------------------
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력]);

EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, @NmA, @SsnA,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
SET @Pid = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [주민번호] = @SsnA);

IF @Pid IS NOT NULL
    PRINT 'PASS PWR-001 신규등록이 수검자 1행을 만들었다';
ELSE BEGIN PRINT 'FAIL PWR-001 신규등록 행이 없다'; SET @Fail += 1; END

-- PWR-011  생년월일·성별은 SP 가 아니라 계산열이 만든다 (04 §8.1.2)
IF EXISTS (SELECT 1 FROM [dbo].[수검자]
            WHERE [주민번호] = @SsnA AND [생년월일] = '19950101' AND [성별] = 'M')
    PRINT 'PASS PWR-011 Birthday/Gender 가 주민번호 산출값과 일치';
ELSE BEGIN PRINT 'FAIL PWR-011 파생값 불일치'; SET @Fail += 1; END

-- PWR-012  자동 발급 ChartNo = 'C' + 6자리 (스펙 §12.6)
IF EXISTS (SELECT 1 FROM [dbo].[수검자]
            WHERE [주민번호] = @SsnA
              AND [차트번호] LIKE 'C[0-9][0-9][0-9][0-9][0-9][0-9]'
              AND LEN([차트번호]) = 7)
    PRINT 'PASS PWR-012 자동 ChartNo 형식 C + 6자리';
ELSE BEGIN PRINT 'FAIL PWR-012 자동 ChartNo 형식 위반'; SET @Fail += 1; END

-- PWR-030  성공한 INSERT 는 값이 들어간 컬럼마다 1행을 남긴다 (00 CP-06 · 04 §8.6.2).
--   필수 4개만 값이 있으므로 4행이다. 나머지 7개는 NULL 이라 기록되지 않는다.
SET @H1 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Pid AND [대상테이블] = N'수검자');
IF (@H1 = 4
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력] WHERE [대상키] = @Pid AND [변경전] IS NOT NULL)
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력] WHERE [대상키] = @Pid AND [변경후] IS NULL)
    AND EXISTS (SELECT 1 FROM [dbo].[변경이력]
                 WHERE [대상키] = @Pid AND [컬럼명] = N'성명' AND [변경후] = @NmA))
    PRINT 'PASS PWR-030 INSERT 감사 4행 · 변경전 전건 NULL · 값 없는 컬럼 미기록';
ELSE BEGIN PRINT 'FAIL PWR-030 INSERT 감사 기록 불일치'; SET @Fail += 1; END

-- PWR-002  동일 주민번호 + 동일 이름 재등록은 행을 늘리지 않는다 (Code=2)
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, @NmA, @SsnA,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[수검자] WHERE [주민번호] = @SsnA) = 1)
    PRINT 'PASS PWR-002 동일 주민번호 재등록이 행을 만들지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-002 중복 행 생성'; SET @Fail += 1; END

-- PWR-003  동일 주민번호 + 다른 이름은 202 이고 기존 행을 바꾸지 않는다
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'다른이름', @SsnA,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[수검자] WHERE [주민번호] = @SsnA) = 1
    AND (SELECT [성명] FROM [dbo].[수검자] WHERE [주민번호] = @SsnA) = @NmA)
    PRINT 'PASS PWR-003 202 는 기존 행을 바꾸지도 늘리지도 않는다';
ELSE BEGIN PRINT 'FAIL PWR-003 202 경로가 데이터를 바꿨다'; SET @Fail += 1; END

-- PWR-013  14자리 표시값은 @SocialNumber VARCHAR(13) 경계에서 절단된다(실측 확인).
--   [X] plans/04 는 101 을 기대했으나 SP 는 절단된 13자리만 보므로 길이 검증에 도달하지 않는다.
--       05 §2.2 가 C# 에 14자리 전달을 금지한 이유가 이것이다. 관측 가능한 계약은
--       "14자리 주민번호를 가진 행이 생기지 않는다" 이고 그것을 단언한다.
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, @NmA, '95010110000199',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
IF (NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE LEN([주민번호]) <> 13)
    AND (SELECT COUNT(*) FROM [dbo].[수검자] WHERE [주민번호] = @SsnA) = 1)
    PRINT 'PASS PWR-013 14자리 입력이 절단되어 새 행을 만들지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-013 14자리 입력이 행을 만들었다'; SET @Fail += 1; END

-- PWR-004/005/007/008/014  실패 경로는 행을 남기지 않는다
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력]);
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'짧은번호',  '950101100001',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';                 -- 101
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'잘못된날짜', '9502311000012',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';                 -- 101
EXEC [dbo].[USP_HC_INSERT_수검자] 0, NULL, N'차트없음',   '9601011000015',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';                 -- 102
EXEC [dbo].[USP_HC_INSERT_수검자] 0, N'T001', N'중복차트', '9601011000015',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';                 -- 201
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, N'비숫자',     '950101A000019',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';                 -- 101
IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자]
                WHERE [성명] IN (N'짧은번호', N'잘못된날짜', N'차트없음', N'중복차트', N'비숫자'))
    PRINT 'PASS PWR-004/005/007/008/014 실패 경로가 행을 남기지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-004/005/007/008/014 실패 경로가 행을 남겼다'; SET @Fail += 1; END

-- PWR-030 (2) 업무실패는 감사 기록도 남기지 않는다 (04 §14 L6)
IF ((SELECT COUNT(*) FROM [dbo].[변경이력]) = @H0)
    PRINT 'PASS PWR-030 업무실패 5건이 변경이력을 남기지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-030 실패 경로가 감사행을 남겼다'; SET @Fail += 1; END

-- FIX-PWR-TRAN  업무실패 뒤에 열린 Transaction 이 남지 않는다 (plans/04 T23 Step 5)
IF (@@TRANCOUNT = 0)
    PRINT 'PASS FIX-PWR-TRAN 업무실패 후 @@TRANCOUNT = 0';
ELSE BEGIN PRINT 'FAIL FIX-PWR-TRAN Transaction 잔여'; SET @Fail += 1; END

-- PWR-009  이름 + 산출 생년월일 후보가 있는데 미확인이면 203 이고 저장하지 않는다
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, @NmA, @SsnB,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [주민번호] = @SsnB)
    PRINT 'PASS PWR-009 유사후보 미확인은 저장하지 않는다';
ELSE BEGIN PRINT 'FAIL PWR-009 미확인 상태로 저장됐다'; SET @Fail += 1; END

-- PWR-010  같은 인자에 확인값 1 이면 별도 등록된다
EXEC [dbo].[USP_HC_INSERT_수검자] 1, NULL, @NmA, @SsnB,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, N'TEST';
SET @Bid = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [주민번호] = @SsnB);
IF (@Bid IS NOT NULL
    AND (SELECT COUNT(*) FROM [dbo].[수검자] WHERE [성명] = @NmA AND [생년월일] = '19950101') = 2)
    PRINT 'PASS PWR-010 후보 확인 후 별도 등록';
ELSE BEGIN PRINT 'FAIL PWR-010 확인 후에도 등록되지 않았다'; SET @Fail += 1; END

----------------------------------------------------------------------------
-- UPDATE_수검자정보
----------------------------------------------------------------------------
SELECT @Led = [최종수정일시], @Cn = [차트번호], @Ssn = [주민번호]
  FROM [dbo].[수검자] WHERE [수검자ID] = @Bid;

-- PWR-020  No-op 은 행을 갱신하지 않는다 (05 §10.2)
SET @Before = @Led;
EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Bid, @Led, @Cn, @NmA, @Ssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) = @Before)
    PRINT 'PASS PWR-020 No-op 은 LastEditDate 를 바꾸지 않는다';
ELSE BEGIN PRINT 'FAIL PWR-020 No-op 이 행을 갱신했다'; SET @Fail += 1; END

-- PWR-031  No-op 은 감사 기록도 남기지 않는다
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Bid);
IF (@H0 = 4)
    PRINT 'PASS PWR-031 No-op 이 변경이력을 남기지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-031 No-op 이 감사행을 남겼다'; SET @Fail += 1; END

-- PWR-022  실제 변경은 LastEditDate 를 반드시 증가시킨다 (스펙 §26, DATETIME 3.33ms 틱)
EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Bid, @Led, @Cn, N'이름변경됨', @Ssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) > @Before
    AND (SELECT [성명] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) = N'이름변경됨')
    PRINT 'PASS PWR-022 실제 변경 시 LastEditDate 단조증가 + 값 반영';
ELSE BEGIN PRINT 'FAIL PWR-022 변경이 반영되지 않았다'; SET @Fail += 1; END

-- PWR-031 (2) 바뀐 컬럼만 1행이 늘어난다. 값이 같은 컬럼은 기록되지 않는다.
IF ((SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Bid) = 5
    AND EXISTS (SELECT 1 FROM [dbo].[변경이력]
                 WHERE [대상키] = @Bid AND [컬럼명] = N'성명'
                   AND [변경전] = @NmA AND [변경후] = N'이름변경됨')
    AND NOT EXISTS (SELECT 1 FROM [dbo].[변경이력]
                     WHERE [대상키] = @Bid
                       AND ISNULL([변경전], N'~NULL~') = ISNULL([변경후], N'~NULL~')))
    PRINT 'PASS PWR-031 UPDATE 감사 1행 · 변경전/후 정확 · 무변경 컬럼 0행';
ELSE BEGIN PRINT 'FAIL PWR-031 UPDATE 감사 기록 불일치'; SET @Fail += 1; END

-- PWR-021  stale LastEditDate 는 600 이고 아무것도 바꾸지 않는다
SELECT @Led = [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid;
SET @Before = @Led;
EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Bid, '2000-01-01', @Cn, N'stale시도', @Ssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) = @Before
    AND (SELECT [성명] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) = N'이름변경됨')
    PRINT 'PASS PWR-021 stale LastEditDate 가 데이터를 바꾸지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-021 stale 요청이 반영됐다'; SET @Fail += 1; END

-- PWR-025  다른 수검자가 쓰는 주민번호는 204 이고 저장되지 않는다
DECLARE @T001Ssn VARCHAR(13) = (SELECT [주민번호] FROM [dbo].[수검자] WHERE [차트번호] = N'T001');
EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Bid, @Led, @Cn, N'이름변경됨', @T001Ssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) = @Ssn)
    PRINT 'PASS PWR-025 타 수검자 주민번호가 저장되지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-025 중복 주민번호가 저장됐다'; SET @Fail += 1; END

-- PWR-026  미존재 PatientId 는 200 이고 행 수를 바꾸지 않는다
DECLARE @Cnt INT = (SELECT COUNT(*) FROM [dbo].[수검자]);
EXEC [dbo].[USP_HC_UPDATE_수검자정보] -1, '2000-01-01', N'X001', N'없는사람', '9701011000013',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[수검자]) = @Cnt)
    PRINT 'PASS PWR-026 미존재 PatientId 가 행을 만들지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-026 행 수가 바뀌었다'; SET @Fail += 1; END

-- PWR-023 / PWR-024  RSV 를 가진 F001 (00 EP-08)
DECLARE @Fid BIGINT, @Fled DATETIME, @Fnm NVARCHAR(100), @Fssn VARCHAR(13);
SELECT @Fid = [수검자ID], @Fled = [최종수정일시], @Fnm = [성명], @Fssn = [주민번호]
  FROM [dbo].[수검자] WHERE [차트번호] = N'F001';

EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Fid, @Fled, N'F001', @Fnm, '8001011999998',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Fid) = @Fssn)
    PRINT 'PASS PWR-023 RSV 보유 시 주민번호 변경이 저장되지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-023 RSV 보유인데 주민번호가 바뀌었다'; SET @Fail += 1; END

EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Fid, @Fled, N'F001X', @Fnm, @Fssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [차트번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Fid) = N'F001X')
    PRINT 'PASS PWR-024 RSV 가 있어도 차트번호 변경은 차단하지 않는다';
ELSE BEGIN PRINT 'FAIL PWR-024 차트번호 변경이 차단됐다'; SET @Fail += 1; END

-- PWR-027 / PWR-028  RCP 를 가진 T014 (05 §17.6 · 스펙 §33.4)
DECLARE @Tid BIGINT, @Tled DATETIME, @Tnm NVARCHAR(100), @Tssn VARCHAR(13);
SELECT @Tid = [수검자ID], @Tled = [최종수정일시], @Tnm = [성명], @Tssn = [주민번호]
  FROM [dbo].[수검자] WHERE [차트번호] = N'T014';

EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Tid, @Tled, N'T014', @Tnm, '7010011999997',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Tid) = @Tssn)
    PRINT 'PASS PWR-027 RCP 보유 시 주민번호 변경이 저장되지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-027 RCP 보유인데 주민번호가 바뀌었다'; SET @Fail += 1; END

EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Tid, @Tled, N'T014X', @Tnm, @Tssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [차트번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Tid) = N'T014X')
    PRINT 'PASS PWR-028 RCP 가 있어도 차트번호 변경은 차단하지 않는다';
ELSE BEGIN PRINT 'FAIL PWR-028 차트번호 변경이 차단됐다'; SET @Fail += 1; END

-- PWR-029  LastEditDate 단조증가 가드를 실제로 발동시킨다 (05:545 · 스펙 §26)
-- [X] PWR-022 는 갱신을 **한 번**만 하고 > 비교만 한다. DATETIME 은 약 3.33ms 틱이라
--     연속 갱신이 같은 틱에 떨어질 때만 IF @NewEdit <= @OldEdit 가드가 발동한다.
--     즉 그 가드를 통째로 지워도 PWR-022 는 PASS 한다 — 이름이 가리키는 기계장치를 시험하지 않았다.
--     지연 없이 5회 연속 수정해 같은 틱 충돌을 강제하고, 매 회차가 직전보다 큰지 본다.
DECLARE @M0 DATETIME = (SELECT [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid);
DECLARE @Mprev DATETIME = @M0, @Mnow DATETIME, @Mled DATETIME;
DECLARE @Mi INT = 0, @MFail INT = 0, @MTight INT = 0;
DECLARE @MName NVARCHAR(100);
WHILE @Mi < 5
BEGIN
    SET @Mi += 1;
    SET @Mled = (SELECT [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid);
    -- [X] EXEC 인자는 상수나 변수만 받는다. 식을 쓰면 Msg 102 로 배치가 컴파일 실패한다 (실측).
    SET @MName = N'단조증가' + CONVERT(NVARCHAR(2), @Mi);
    EXEC [dbo].[USP_HC_UPDATE_수검자정보] @Bid, @Mled, @Cn, @MName, @Ssn,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
    SET @Mnow = (SELECT [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid);
    IF @Mnow <= @Mprev SET @MFail += 1;
    IF DATEDIFF(MILLISECOND, @Mprev, @Mnow) <= 4 SET @MTight += 1;
    SET @Mprev = @Mnow;
END
IF @MFail = 0 AND @Mnow > @M0
    PRINT 'PASS PWR-029 연속 5회 수정에서 LastEditDate 가 매번 단조증가 (4ms 이내 근접 '
        + CONVERT(VARCHAR(3), @MTight) + '회)';
ELSE BEGIN PRINT 'FAIL PWR-029 LastEditDate 가 역행하거나 정체했다 (역행 '
        + CONVERT(VARCHAR(3), @MFail) + '회)'; SET @Fail += 1; END

-- 뒤 파일이 쓰는 Fixture 차트번호를 되돌린다. 이 파일이 만든 수검자 2명은 그대로 둔다 —
-- tests/00 이 전건을 지우고 다시 만들기 때문이다.
UPDATE [dbo].[수검자] SET [차트번호] = N'F001' WHERE [수검자ID] = @Fid;
UPDATE [dbo].[수검자] SET [차트번호] = N'T014' WHERE [수검자ID] = @Tid;

-- @Fail 은 이 배치에서만 산다. GO 뒤로 넘기면 Msg 137 이다 (CLAUDE.md §11).
IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 05_Patient_Write_Tests 완료 ===';
GO
