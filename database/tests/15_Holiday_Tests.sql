SET NOCOUNT ON;
PRINT '--- 15_Holiday_Tests 시작 ---';
GO
----------------------------------------------------------------------------
-- 기준정보 SP 4개의 시험 (05 §12.4~§12.8 · 06 §18).
--
-- [!] RS0 의 결과코드 는 여기서 보지 않는다. T-SQL 은 SELECT 로 나온 Result Set 을
--     INSERT ... EXEC 로 받을 수 없고(다중 RS, 06 §33), 이 계열의 다른 Write 시험도
--     같은 이유로 **DB 상태**를 단언한다. RS0 코드 판정은 tests/contract 계열이 한다.
--     여기서 보는 것은 "요청이 데이터를 바꿨는가 / 안 바꿨는가" 다.
--
-- 이 파일은 Seed 를 건드리므로 **자기가 심은 것을 자기가 지운다.** 끝에서 41행 복귀를 확인한다.
----------------------------------------------------------------------------
DECLARE @Fail INT = 0;

DECLARE @H0 INT = (SELECT COUNT(*) FROM [dbo].[휴무일]);
IF (@H0 = 41)
    PRINT 'PASS HLD-000 사전조건 - 휴무일 Seed 41행';
ELSE BEGIN PRINT 'FAIL HLD-000 사전조건 불일치 (실측 ' + CONVERT(VARCHAR(5), @H0) + '행)'; SET @Fail += 1; END

DECLARE @D  DATE = '2027-03-15';          -- Seed 에 없는 날짜 (06 §14.4 에 3월 행이 없다)
DECLARE @Law DATE = '2026-01-01';         -- 법정공휴일 (신정)
DECLARE @Rv BINARY(8), @Rv2 BINARY(8), @LawRv BINARY(8);
DECLARE @Stale BINARY(8) = 0x0000000000000001;
DECLARE @Log0 INT = (SELECT COUNT(*) FROM [dbo].[변경이력]);

----------------------------------------------------------------------------
-- 등록
----------------------------------------------------------------------------
-- HLD-001 자체휴무일 등록. 휴무구분 은 Parameter 가 아니며 SP 가 고정한다 (00 HOL-05).
EXEC [dbo].[USP_HC_자체휴무일_등록] @D, N'설비 점검', 1, N'HLD 시험';
IF EXISTS (SELECT 1 FROM [dbo].[휴무일]
            WHERE [휴무일자] = @D AND [휴무구분] = N'자체휴무일' AND [휴무일명] = N'설비 점검')
    PRINT 'PASS HLD-001 자체휴무일 1행 등록 · 휴무구분 자동 고정';
ELSE BEGIN PRINT 'FAIL HLD-001 등록되지 않았거나 휴무구분이 다르다'; SET @Fail += 1; END

-- HLD-002 신규 행은 생성일시 = 최종수정일시 다 (SP 가 @저장시각 하나로 넣는다, 05 §12.6).
-- [X] 여기 있던 단언은 [행버전] IS NOT NULL AND [최종수정일시] >= [생성일시] 였다.
--     앞은 ROWVERSION 이라 불가능하고 뒤는 CK_휴무일_EDIT_DATE 가 이미 강제한다 —
--     스키마가 보증하는 것만 보는 항진명제였다. 실패할 수 없는 단언은 시험이 아니다(06 §44.8).
DECLARE @Cre DATETIME2(0) = (SELECT [생성일시] FROM [dbo].[휴무일] WHERE [휴무일자] = @D);
IF ((SELECT [최종수정일시] FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = @Cre)
    PRINT 'PASS HLD-002 신규 행의 생성일시 = 최종수정일시';
ELSE BEGIN PRINT 'FAIL HLD-002 등록이 두 시각을 다른 값으로 넣었다'; SET @Fail += 1; END

-- HLD-003 같은 날짜 재등록은 아무것도 바꾸지 않는다 (801)
SELECT @Rv = [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @D;
EXEC [dbo].[USP_HC_자체휴무일_등록] @D, N'중복시도', 1, NULL;
IF ((SELECT COUNT(*) FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = 1
    AND (SELECT [휴무일명] FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = N'설비 점검'
    AND (SELECT [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = @Rv)
    PRINT 'PASS HLD-003 날짜 중복 등록이 기존 행을 건드리지 않았다';
ELSE BEGIN PRINT 'FAIL HLD-003 중복 등록이 데이터를 바꿨다'; SET @Fail += 1; END

----------------------------------------------------------------------------
-- 법정·대체 공휴일 보호 (00 HOL-05 · 결과코드 802)
----------------------------------------------------------------------------
SELECT @LawRv = [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @Law;

-- HLD-004 법정공휴일 수정 시도. 토큰이 **맞아도** 막혀야 한다.
EXEC [dbo].[USP_HC_자체휴무일_수정] @Law, @LawRv, N'바꿔보기', 1, NULL;
IF ((SELECT [휴무일명] FROM [dbo].[휴무일] WHERE [휴무일자] = @Law) = N'신정'
    AND (SELECT [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @Law) = @LawRv)
    PRINT 'PASS HLD-004 법정공휴일 수정 시도가 행을 바꾸지 않았다';
ELSE BEGIN PRINT 'FAIL HLD-004 법정공휴일이 수정됐다'; SET @Fail += 1; END

-- HLD-005 법정공휴일 삭제 시도
EXEC [dbo].[USP_HC_자체휴무일_삭제] @Law, @LawRv;
IF EXISTS (SELECT 1 FROM [dbo].[휴무일] WHERE [휴무일자] = @Law)
    PRINT 'PASS HLD-005 법정공휴일 삭제 시도가 행을 지우지 않았다';
ELSE BEGIN PRINT 'FAIL HLD-005 법정공휴일이 삭제됐다'; SET @Fail += 1; END

----------------------------------------------------------------------------
-- 수정 · 동시성
----------------------------------------------------------------------------
-- HLD-006 자체휴무일 수정 성공. 행버전 은 반드시 바뀐다.
SELECT @Rv = [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @D;
EXEC [dbo].[USP_HC_자체휴무일_수정] @D, @Rv, N'설비 점검(연장)', 1, N'HLD 시험';
SELECT @Rv2 = [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @D;
IF ((SELECT [휴무일명] FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = N'설비 점검(연장)'
    AND @Rv2 <> @Rv)
    PRINT 'PASS HLD-006 자체휴무일 수정 반영 + 행버전 변경';
ELSE BEGIN PRINT 'FAIL HLD-006 수정이 반영되지 않았거나 행버전이 그대로다'; SET @Fail += 1; END

-- HLD-006b 수정은 생성일시를 건드리지 않는다. SET 목록에 [생성일시] 가 끼면 여기서 걸린다.
IF ((SELECT [생성일시] FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = @Cre
    AND (SELECT [최종수정일시] FROM [dbo].[휴무일] WHERE [휴무일자] = @D) >= @Cre)
    PRINT 'PASS HLD-006b 수정이 생성일시를 바꾸지 않았다';
ELSE BEGIN PRINT 'FAIL HLD-006b 수정이 생성일시를 건드렸다'; SET @Fail += 1; END

-- HLD-006c 대소문자만 바꾼 이름이 저장되는가 (07a No-op 의 정렬 함정).
-- Korean_Wansung_CI_AS 는 N''설비 점검(연장)'' 과 소문자 라틴 혼용을 같다고 본다.
DECLARE @CaseName NVARCHAR(100) = N'HVAC 점검', @Rv3 BINARY(8);
SELECT @Rv3 = [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @D;
EXEC [dbo].[USP_HC_자체휴무일_수정] @D, @Rv3, @CaseName, 1, N'HLD 시험';
SELECT @Rv3 = [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @D;
EXEC [dbo].[USP_HC_자체휴무일_수정] @D, @Rv3, N'hvac 점검', 1, N'HLD 시험';
IF ((SELECT [휴무일명] FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = N'hvac 점검'
    AND CONVERT(VARBINARY(200), (SELECT [휴무일명] FROM [dbo].[휴무일] WHERE [휴무일자] = @D))
        = CONVERT(VARBINARY(200), N'hvac 점검'))
    PRINT 'PASS HLD-006c 대소문자만 바꾼 수정이 저장됐다 (No-op 으로 삼키지 않았다)';
ELSE BEGIN PRINT 'FAIL HLD-006c 대소문자 변경이 No-op 으로 사라졌다'; SET @Fail += 1; END

-- 뒤 시험이 이름에 기대므로 되돌린다.
SELECT @Rv3 = [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @D;
EXEC [dbo].[USP_HC_자체휴무일_수정] @D, @Rv3, N'설비 점검(연장)', 1, N'HLD 시험';
SELECT @Rv2 = [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @D;

-- HLD-007 stale 행버전 은 601 이고 아무것도 바꾸지 않는다
EXEC [dbo].[USP_HC_자체휴무일_수정] @D, @Rv, N'stale시도', 0, NULL;
IF ((SELECT [휴무일명] FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = N'설비 점검(연장)'
    AND (SELECT [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = @Rv2)
    PRINT 'PASS HLD-007 stale 행버전이 데이터를 바꾸지 않았다';
ELSE BEGIN PRINT 'FAIL HLD-007 stale 요청이 반영됐다'; SET @Fail += 1; END

-- HLD-008 No-op — 같은 값 재전송은 행을 갱신하지 않는다 (05 §12.7)
EXEC [dbo].[USP_HC_자체휴무일_수정] @D, @Rv2, N'설비 점검(연장)', 1, N'HLD 시험';
IF ((SELECT [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = @Rv2)
    PRINT 'PASS HLD-008 No-op 이 행버전을 바꾸지 않았다';
ELSE BEGIN PRINT 'FAIL HLD-008 No-op 이 행을 갱신했다'; SET @Fail += 1; END

-- HLD-009 사용여부 0 은 삭제가 아니라 일시 무효화이며 그 날짜를 계속 점유한다 (03 §24.5)
EXEC [dbo].[USP_HC_자체휴무일_수정] @D, @Rv2, N'설비 점검(연장)', 0, N'HLD 시험';
IF EXISTS (SELECT 1 FROM [dbo].[휴무일] WHERE [휴무일자] = @D AND [사용여부] = 0)
    PRINT 'PASS HLD-009 사용여부 0 이 행을 남긴 채 비활성만 시켰다';
ELSE BEGIN PRINT 'FAIL HLD-009 비활성화가 반영되지 않았다'; SET @Fail += 1; END

-- HLD-010 비활성 행도 날짜를 점유하므로 같은 날짜 등록은 여전히 막힌다 (HOL-04)
EXEC [dbo].[USP_HC_자체휴무일_등록] @D, N'비활성위에등록', 1, NULL;
IF ((SELECT COUNT(*) FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = 1
    AND (SELECT [사용여부] FROM [dbo].[휴무일] WHERE [휴무일자] = @D) = 0)
    PRINT 'PASS HLD-010 비활성 행 위에 덮어 등록되지 않았다';
ELSE BEGIN PRINT 'FAIL HLD-010 비활성 날짜에 중복 등록됐다'; SET @Fail += 1; END

----------------------------------------------------------------------------
-- 삭제
----------------------------------------------------------------------------
-- HLD-011 stale 행버전 삭제는 지우지 않는다
SELECT @Rv2 = [행버전] FROM [dbo].[휴무일] WHERE [휴무일자] = @D;
EXEC [dbo].[USP_HC_자체휴무일_삭제] @D, @Stale;
IF EXISTS (SELECT 1 FROM [dbo].[휴무일] WHERE [휴무일자] = @D)
    PRINT 'PASS HLD-011 stale 행버전 삭제가 행을 지우지 않았다';
ELSE BEGIN PRINT 'FAIL HLD-011 stale 삭제가 행을 지웠다'; SET @Fail += 1; END

-- HLD-012 물리 삭제
EXEC [dbo].[USP_HC_자체휴무일_삭제] @D, @Rv2;
IF NOT EXISTS (SELECT 1 FROM [dbo].[휴무일] WHERE [휴무일자] = @D)
    PRINT 'PASS HLD-012 자체휴무일 물리 삭제';
ELSE BEGIN PRINT 'FAIL HLD-012 삭제되지 않았다'; SET @Fail += 1; END

-- HLD-013 없는 휴무일 삭제는 800 이고 다른 행을 건드리지 않는다
DECLARE @Before INT = (SELECT COUNT(*) FROM [dbo].[휴무일]);
EXEC [dbo].[USP_HC_자체휴무일_삭제] @D, @Rv2;
IF ((SELECT COUNT(*) FROM [dbo].[휴무일]) = @Before)
    PRINT 'PASS HLD-013 없는 휴무일 삭제가 다른 행을 건드리지 않았다';
ELSE BEGIN PRINT 'FAIL HLD-013 행수가 변했다'; SET @Fail += 1; END

----------------------------------------------------------------------------
-- 감사 · 복귀
----------------------------------------------------------------------------
-- HLD-014 휴무일 변경은 변경이력을 남기지 않는다 (04 §8.6.3 · 03 §24.7).
--         기록하지 않는 것이 계약이므로 그것을 시험한다 — 나중에 조용히 늘어나면 여기서 걸린다.
IF ((SELECT COUNT(*) FROM [dbo].[변경이력]) = @Log0)
    PRINT 'PASS HLD-014 휴무일 CRUD 가 변경이력을 남기지 않았다';
ELSE BEGIN PRINT 'FAIL HLD-014 휴무일 CRUD 가 감사행을 남겼다'; SET @Fail += 1; END

-- HLD-015 시험이 심은 것을 전부 걷었다
DECLARE @H1 INT = (SELECT COUNT(*) FROM [dbo].[휴무일]);
IF (@H1 = 41)
    PRINT 'PASS HLD-015 Seed 41행 복귀 - 시험이 남긴 행 0건';
ELSE BEGIN PRINT 'FAIL HLD-015 Seed 행수 ' + CONVERT(VARCHAR(5), @H1) + ' (기대 41)'; SET @Fail += 1; END

-- @Fail 은 이 배치에서만 산다. GO 뒤로 넘기면 Msg 137 이다 (database/AGENTS.md §11).
IF @Fail > 0 THROW 51015, N'휴무일 시험에 실패가 있습니다.', 1;
PRINT '=== 15_Holiday_Tests 완료 ===';
GO
