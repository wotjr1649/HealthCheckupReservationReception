SET NOCOUNT ON;
DECLARE @Fail INT = 0;

-- 이 파일은 DB 상태 불변조건만 판정한다. RS0 결과코드 와 RS 형상은
-- tests/contract/PWR-* + tools/verify-contract.js 가 판정한다 (스펙 §33.1a).
-- INSERT … EXEC 를 쓰지 않는다 — RS 가 2개인 SP 는 Msg 213 이고 내부 ROLLBACK 은 Msg 3915 다.
--
-- [R12] 업무시간 가드가 없다. 05 §10.1·§10.2 에서 공통 업무 가능조건이 빠졌으므로
--       수검자 Write 는 308/309 를 내지 않는다 — PWR-001~031 은 **언제나 실행**한다.
--       여기 있던 창 밖 분기(PWR-OFF)는 "창 밖 호출이 DB 를 바꾸지 않는다" 를 단언했는데
--       R12 로 전제가 뒤집혔다. 바꾸는 것이 정상이므로 그 단언을 지운다.
--       예약·접수(RWR·CWR)는 여전히 가드를 갖는다 — 06 §33.2a.

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
-- R7: 동시성 토큰이 [행버전] 이다. [최종수정일시] 는 감사 정보라 토큰으로 쓰지 않는다 (04 §1.2).
DECLARE @Led BINARY(8), @Before BINARY(8);
DECLARE @Cn NVARCHAR(100), @Nm NVARCHAR(100), @Ssn VARCHAR(13);

----------------------------------------------------------------------------
-- INSERT_수검자
----------------------------------------------------------------------------
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력]);

EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, @NmA, @SsnA,
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

-- PWR-012  자동 발급 차트번호 = '코드' + 6자리 (스펙 §12.6)
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

-- PWR-002  동일 주민번호 + 동일 이름 재등록은 행을 늘리지 않는다 (결과코드=2)
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, @NmA, @SsnA,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[수검자] WHERE [주민번호] = @SsnA) = 1)
    PRINT 'PASS PWR-002 동일 주민번호 재등록이 행을 만들지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-002 중복 행 생성'; SET @Fail += 1; END

-- PWR-003  동일 주민번호 + 다른 이름은 202 이고 기존 행을 바꾸지 않는다
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, N'다른이름', @SsnA,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[수검자] WHERE [주민번호] = @SsnA) = 1
    AND (SELECT [성명] FROM [dbo].[수검자] WHERE [주민번호] = @SsnA) = @NmA)
    PRINT 'PASS PWR-003 202 는 기존 행을 바꾸지도 늘리지도 않는다';
ELSE BEGIN PRINT 'FAIL PWR-003 202 경로가 데이터를 바꿨다'; SET @Fail += 1; END

-- PWR-013  14자리 표시값은 @주민번호 VARCHAR(13) 경계에서 절단된다(실측 확인).
--   [X] plans/04 는 101 을 기대했으나 SP 는 절단된 13자리만 보므로 길이 검증에 도달하지 않는다.
--       05 §2.2 가 C# 에 14자리 전달을 금지한 이유가 이것이다. 관측 가능한 계약은
--       "14자리 주민번호를 가진 행이 생기지 않는다" 이고 그것을 단언한다.
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, @NmA, '95010110000199',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
IF (NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE LEN([주민번호]) <> 13)
    AND (SELECT COUNT(*) FROM [dbo].[수검자] WHERE [주민번호] = @SsnA) = 1)
    PRINT 'PASS PWR-013 14자리 입력이 절단되어 새 행을 만들지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-013 14자리 입력이 행을 만들었다'; SET @Fail += 1; END

-- PWR-004/005/007/008/014  실패 경로는 행을 남기지 않는다
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력]);
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, N'짧은번호',  '950101100001',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';                 -- 101
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, N'잘못된날짜', '9502311000012',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';                 -- 101
EXEC [dbo].[USP_HC_수검자_등록] 0, NULL, N'차트없음',   '9601011000015',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';                 -- 102
EXEC [dbo].[USP_HC_수검자_등록] 0, N'T001', N'중복차트', '9601011000015',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';                 -- 201
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, N'비숫자',     '950101A000019',
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
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, @NmA, @SsnB,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 0, N'TEST';
IF NOT EXISTS (SELECT 1 FROM [dbo].[수검자] WHERE [주민번호] = @SsnB)
    PRINT 'PASS PWR-009 유사후보 미확인은 저장하지 않는다';
ELSE BEGIN PRINT 'FAIL PWR-009 미확인 상태로 저장됐다'; SET @Fail += 1; END

-- PWR-010  같은 인자에 확인값 1 이면 별도 등록된다
EXEC [dbo].[USP_HC_수검자_등록] 1, NULL, @NmA, @SsnB,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, N'TEST';
SET @Bid = (SELECT [수검자ID] FROM [dbo].[수검자] WHERE [주민번호] = @SsnB);
IF (@Bid IS NOT NULL
    AND (SELECT COUNT(*) FROM [dbo].[수검자] WHERE [성명] = @NmA AND [생년월일] = '19950101') = 2)
    PRINT 'PASS PWR-010 후보 확인 후 별도 등록';
ELSE BEGIN PRINT 'FAIL PWR-010 확인 후에도 등록되지 않았다'; SET @Fail += 1; END

----------------------------------------------------------------------------
-- UPDATE_수검자정보
----------------------------------------------------------------------------
SELECT @Led = [행버전], @Cn = [차트번호], @Ssn = [주민번호]
  FROM [dbo].[수검자] WHERE [수검자ID] = @Bid;

-- PWR-020  No-op 은 행을 갱신하지 않는다 (05 §10.2)
SET @Before = @Led;
EXEC [dbo].[USP_HC_수검자정보_수정] @Bid, @Led, @Cn, @NmA, @Ssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [행버전] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) = @Before)
    PRINT 'PASS PWR-020 No-op 은 RowVersion 을 바꾸지 않는다';
ELSE BEGIN PRINT 'FAIL PWR-020 No-op 이 행을 갱신했다'; SET @Fail += 1; END

-- PWR-031  No-op 은 감사 기록도 남기지 않는다
SET @H0 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Bid);
IF (@H0 = 4)
    PRINT 'PASS PWR-031 No-op 이 변경이력을 남기지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-031 No-op 이 감사행을 남겼다'; SET @Fail += 1; END

-- PWR-022  실제 변경은 행버전 을 반드시 바꾼다 (스펙 §26)
-- [!] R7 이전에는 최종수정일시 의 **증가**를 봤다. 그 단조성은 +4ms 보정이 만든 것이었고
--     보정을 걷은 지금 DATETIME 3.33ms 틱 안의 연속 갱신은 같은 값이 될 수 있다.
--     ROWVERSION 은 DB 가 UPDATE 마다 반드시 바꾸므로 <> 로 본다.
EXEC [dbo].[USP_HC_수검자정보_수정] @Bid, @Led, @Cn, N'이름변경됨', @Ssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [행버전] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) <> @Before
    AND (SELECT [성명] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) = N'이름변경됨')
    PRINT 'PASS PWR-022 실제 변경 시 RowVersion 변경 + 값 반영';
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

-- PWR-021  stale 행버전 은 601 이고 아무것도 바꾸지 않는다 (600 은 R7 에서 폐지, 05 §4.4)
SELECT @Led = [행버전] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid;
SET @Before = @Led;
EXEC [dbo].[USP_HC_수검자정보_수정] @Bid, 0x0000000000000001, @Cn, N'stale시도', @Ssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [행버전] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) = @Before
    AND (SELECT [성명] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) = N'이름변경됨')
    PRINT 'PASS PWR-021 stale RowVersion 이 데이터를 바꾸지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-021 stale 요청이 반영됐다'; SET @Fail += 1; END

-- PWR-025  다른 수검자가 쓰는 주민번호는 204 이고 저장되지 않는다
DECLARE @T001Ssn VARCHAR(13) = (SELECT [주민번호] FROM [dbo].[수검자] WHERE [차트번호] = N'T001');
EXEC [dbo].[USP_HC_수검자정보_수정] @Bid, @Led, @Cn, N'이름변경됨', @T001Ssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) = @Ssn)
    PRINT 'PASS PWR-025 타 수검자 주민번호가 저장되지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-025 중복 주민번호가 저장됐다'; SET @Fail += 1; END

-- PWR-026  미존재 수검자ID 는 200 이고 행 수를 바꾸지 않는다
DECLARE @건수 INT = (SELECT COUNT(*) FROM [dbo].[수검자]);
EXEC [dbo].[USP_HC_수검자정보_수정] -1, 0x0000000000000001, N'X001', N'없는사람', '9701011000013',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT COUNT(*) FROM [dbo].[수검자]) = @건수)
    PRINT 'PASS PWR-026 미존재 PatientId 가 행을 만들지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-026 행 수가 바뀌었다'; SET @Fail += 1; END

-- PWR-023 / PWR-024  RSV 를 가진 F001 (00 EP-08)
DECLARE @Fid BIGINT, @Fled BINARY(8), @Fnm NVARCHAR(100), @Fssn VARCHAR(13);
SELECT @Fid = [수검자ID], @Fled = [행버전], @Fnm = [성명], @Fssn = [주민번호]
  FROM [dbo].[수검자] WHERE [차트번호] = N'F001';

EXEC [dbo].[USP_HC_수검자정보_수정] @Fid, @Fled, N'F001', @Fnm, '8001011999998',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Fid) = @Fssn)
    PRINT 'PASS PWR-023 RSV 보유 시 주민번호 변경이 저장되지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-023 RSV 보유인데 주민번호가 바뀌었다'; SET @Fail += 1; END

EXEC [dbo].[USP_HC_수검자정보_수정] @Fid, @Fled, N'F001X', @Fnm, @Fssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [차트번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Fid) = N'F001X')
    PRINT 'PASS PWR-024 RSV 가 있어도 차트번호 변경은 차단하지 않는다';
ELSE BEGIN PRINT 'FAIL PWR-024 차트번호 변경이 차단됐다'; SET @Fail += 1; END

-- PWR-027 / PWR-028  RCP 를 가진 T014 (05 §17.6 · 스펙 §33.4)
DECLARE @Tid BIGINT, @Tled BINARY(8), @Tnm NVARCHAR(100), @Tssn VARCHAR(13);
SELECT @Tid = [수검자ID], @Tled = [행버전], @Tnm = [성명], @Tssn = [주민번호]
  FROM [dbo].[수검자] WHERE [차트번호] = N'T014';

EXEC [dbo].[USP_HC_수검자정보_수정] @Tid, @Tled, N'T014', @Tnm, '7010011999997',
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [주민번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Tid) = @Tssn)
    PRINT 'PASS PWR-027 RCP 보유 시 주민번호 변경이 저장되지 않았다';
ELSE BEGIN PRINT 'FAIL PWR-027 RCP 보유인데 주민번호가 바뀌었다'; SET @Fail += 1; END

EXEC [dbo].[USP_HC_수검자정보_수정] @Tid, @Tled, N'T014X', @Tnm, @Tssn,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [차트번호] FROM [dbo].[수검자] WHERE [수검자ID] = @Tid) = N'T014X')
    PRINT 'PASS PWR-028 RCP 가 있어도 차트번호 변경은 차단하지 않는다';
ELSE BEGIN PRINT 'FAIL PWR-028 차트번호 변경이 차단됐다'; SET @Fail += 1; END

-- PWR-029  연속 갱신에서 행버전 이 매번 바뀌는가 (스펙 §26)
-- [X] R7 이전 이 자리는 최종수정일시 의 +4ms 단조증가 가드를 발동시키는 시험이었다.
--     그 가드가 사라졌으므로 시험도 기계장치를 바꾼다. **의도는 그대로다** —
--     지연 없이 5회 연속 수정해 같은 DATETIME 틱 충돌을 강제하고, 그래도 토큰이
--     매번 달라지는지 본다. 다른 점은 그것을 보장하는 주체다:
--       R7 이전  저장 SP 가 +4ms 를 더해 만들었다 (저장 시각이 사실과 달라지는 대가)
--       R7 이후  ROWVERSION 을 DB 가 UPDATE 마다 반드시 바꾼다 (대가 없음)
--     같은 틱에 떨어진 횟수(@MTight)를 함께 내보내 시험이 실제로 충돌을 만들었는지 보인다.
DECLARE @M0 BINARY(8) = (SELECT [행버전] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid);
DECLARE @Mprev BINARY(8) = @M0, @Mnow BINARY(8), @Mled BINARY(8);
DECLARE @MT0 DATETIME, @MT1 DATETIME;
DECLARE @Mi INT = 0, @MFail INT = 0, @MTight INT = 0;
DECLARE @MName NVARCHAR(100);
WHILE @Mi < 5
BEGIN
    SET @Mi += 1;
    SELECT @Mled = [행버전], @MT0 = [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid;
    -- [X] EXEC 인자는 상수나 변수만 받는다. 식을 쓰면 Msg 102 로 배치가 컴파일 실패한다 (실측).
    SET @MName = N'단조증가' + CONVERT(NVARCHAR(2), @Mi);
    EXEC [dbo].[USP_HC_수검자정보_수정] @Bid, @Mled, @Cn, @MName, @Ssn,
         NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0, N'TEST';
    SELECT @Mnow = [행버전], @MT1 = [최종수정일시] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid;
    IF @Mnow = @Mprev SET @MFail += 1;
    -- 최종수정일시 가 같은 눈금에 떨어진 회차. R7 이전이면 여기서 +4ms 가드가 발동했다.
    IF @MT1 = @MT0 SET @MTight += 1;
    SET @Mprev = @Mnow;
END
IF @MFail = 0 AND @Mnow <> @M0
    PRINT 'PASS PWR-029 연속 5회 수정에서 RowVersion 이 매번 변경 (최종수정일시 동일 틱 '
        + CONVERT(VARCHAR(3), @MTight) + '회)';
ELSE BEGIN PRINT 'FAIL PWR-029 RowVersion 이 정체했다 (정체 '
        + CONVERT(VARCHAR(3), @MFail) + '회)'; SET @Fail += 1; END

-- PWR-032  대소문자만 바꾼 수정이 저장되고 감사에도 남는가 (스펙 §28.2 · §43-17)
-- [X] 정렬이 Korean_Wansung_CI_AS 라 No-op 판정의 INTERSECT 가 'a@B.com' 과 'a@b.com' 을
--     같다고 봤다. 결과코드=1 '변경된 내용이 없습니다' 가 돌아오고 사용자 편집이 조용히 사라졌다.
--     감사 필터도 같은 CI 비교였으므로 **둘을 함께** 바이트 비교로 바꿨다 —
--     하나만 고치면 저장은 되는데 변경이력에 한 줄도 안 남는다. 그래서 이 시험이 둘 다 본다.
DECLARE @CiName NVARCHAR(100) = (SELECT [성명] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid);
DECLARE @CiL BINARY(8), @CiH0 INT, @CiH1 INT, @CiMail VARCHAR(200), @CiEdit BINARY(8);

-- 준비: 대문자가 섞인 이메일을 넣는다
SET @CiL = (SELECT [행버전] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid);
EXEC [dbo].[USP_HC_수검자정보_수정] @Bid, @CiL, @Cn, @CiName, @Ssn,
     NULL, NULL, 'case@TEST.com', NULL, NULL, NULL, NULL, 0, N'TEST';

-- 대소문자만 바꾼다
SET @CiH0 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Bid AND [컬럼명] = N'이메일');
SET @CiL = (SELECT [행버전] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid);
EXEC [dbo].[USP_HC_수검자정보_수정] @Bid, @CiL, @Cn, @CiName, @Ssn,
     NULL, NULL, 'case@test.com', NULL, NULL, NULL, NULL, 0, N'TEST';
SET @CiH1 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Bid AND [컬럼명] = N'이메일');
SET @CiMail = (SELECT [이메일] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid);

-- 저장값 비교도 CI 라 = 로는 구분되지 않는다. 여기서도 바이트로 본다.
IF (CONVERT(VARBINARY(400), @CiMail) = CONVERT(VARBINARY(400), 'case@test.com')
    AND @CiH1 = @CiH0 + 1)
    PRINT 'PASS PWR-032 대소문자만 바꾼 수정이 저장되고 감사 1행이 남았다';
ELSE
BEGIN
    PRINT 'FAIL PWR-032 저장값=' + ISNULL(@CiMail, '(NULL)')
        + ' 감사증가=' + CONVERT(VARCHAR(5), @CiH1 - @CiH0);
    SET @Fail += 1;
END

-- 진짜 무변경은 여전히 No-op 이어야 한다. 바이트 비교로 바꾸면서 이것이 깨지면 안 된다.
SET @CiEdit = (SELECT [행버전] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid);
SET @CiH0 = (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Bid);
EXEC [dbo].[USP_HC_수검자정보_수정] @Bid, @CiEdit, @Cn, @CiName, @Ssn,
     NULL, NULL, 'case@test.com', NULL, NULL, NULL, NULL, 0, N'TEST';
IF ((SELECT [행버전] FROM [dbo].[수검자] WHERE [수검자ID] = @Bid) = @CiEdit
    AND (SELECT COUNT(*) FROM [dbo].[변경이력] WHERE [대상키] = @Bid) = @CiH0)
    PRINT 'PASS PWR-032b 같은 값 재전송은 여전히 No-op (행·감사 불변)';
ELSE BEGIN PRINT 'FAIL PWR-032b 무변경 재전송이 행을 갱신했다'; SET @Fail += 1; END

-- 뒤 파일이 쓰는 Fixture 차트번호를 되돌린다. 이 파일이 만든 수검자 2명은 그대로 둔다 —
-- tests/00 이 전건을 지우고 다시 만들기 때문이다.
UPDATE [dbo].[수검자] SET [차트번호] = N'F001' WHERE [수검자ID] = @Fid;
UPDATE [dbo].[수검자] SET [차트번호] = N'T014' WHERE [수검자ID] = @Tid;

-- @Fail 은 이 배치에서만 산다. GO 뒤로 넘기면 Msg 137 이다 (CLAUDE.md §11).
IF @Fail > 0 THROW 51000, N'테스트 파일에 실패가 있습니다.', 1;
PRINT '=== 05_Patient_Write_Tests 완료 ===';
GO
