SET NOCOUNT ON;
-- T013 의 Work 는 CORRUPT-2 다. 국가검사항목이 빈 문자열이라 저장 NEX 가 없다 (tests/00).
DECLARE @Wc BIGINT = (SELECT TOP (1) w.[업무ID] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
                       WHERE p.[차트번호] = N'T013' AND w.[상태코드] = 'RSV' ORDER BY w.[업무ID]);
EXEC [dbo].[USP_HC_예약접수상세_조회] @Wc;
GO
