/*  copy-completion.sql ― 접수완료(RCP) 업무를 완료이력으로 복사한다

    배포물이 아니다. deploy/ 에 들어가지 않고 Deploy.sql 이 참조하지 않는다.
    시나리오 시험에서 "과거에 검진을 받은 수검자" 상태를 만들 때 직접 돌린다.

    plans/10 §4.4 가 SP 를 신설하지 않기로 한 이유:
      05 §1.3 의 외부 호출 SP 15개 계약과 SCH-014 기대치를 움직이지 않기 위해서다.
      접수완료 SP 가 함께 넣는 안은 기각했다 ― TGT-02 가 "현재 예약·접수 업무의 상태는
      완료이력으로 사용하지 않는다" 고 못박아 접수를 검진완료로 간주하게 되기 때문이다.
      이 스크립트를 돌리는 것은 "외부에서 완료 사실이 들어왔다" 를 사람이 선언하는 행위다.

    두 번 돌려도 결과가 같다. 이미 있는 (수검자ID, 완료일자) 는 건너뛴다.

    실행:  ./scripts/copy-completion.sh
*/
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO
DECLARE @Before INT = (SELECT COUNT(*) FROM [dbo].[완료이력]);
DECLARE @Src    INT = (SELECT COUNT(*) FROM [dbo].[예약접수] WHERE [상태코드] = 'RCP');

INSERT INTO [dbo].[완료이력] ([수검자ID], [완료일자], [국가검사항목], [추가검사항목])
SELECT w.[수검자ID], w.[예약일], w.[국가검사항목], w.[추가검사항목]
  FROM [dbo].[예약접수] w
 WHERE w.[상태코드] = 'RCP'
   AND NOT EXISTS (SELECT 1 FROM [dbo].[완료이력] h
                    WHERE h.[수검자ID] = w.[수검자ID] AND h.[완료일자] = w.[예약일]);

DECLARE @After INT = (SELECT COUNT(*) FROM [dbo].[완료이력]);
PRINT N'INFO 접수완료 업무 ' + CONVERT(VARCHAR(10), @Src)
    + N' / 완료이력 ' + CONVERT(VARCHAR(10), @Before)
    + N' -> ' + CONVERT(VARCHAR(10), @After);

-- 접수완료 업무가 전부 완료이력에 있는지 확인한다. 두 번째 실행에서는 새로 넣는 것이 0건이어야 한다.
DECLARE @Missing INT = (SELECT COUNT(*) FROM [dbo].[예약접수] w
                         WHERE w.[상태코드] = 'RCP'
                           AND NOT EXISTS (SELECT 1 FROM [dbo].[완료이력] h
                                            WHERE h.[수검자ID] = w.[수검자ID] AND h.[완료일자] = w.[예약일]));
IF (@Missing = 0) PRINT N'PASS CPY-001 접수완료 업무 전건이 완료이력에 있다';
ELSE BEGIN PRINT N'FAIL CPY-001 누락 ' + CONVERT(VARCHAR(10), @Missing) + N'건'; THROW 51020, N'복사 실패', 1; END
GO
