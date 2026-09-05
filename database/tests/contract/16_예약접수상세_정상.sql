SET NOCOUNT ON;
-- T020 의 RSV Work 는 NEX 8행 + AEX 1행(EX012)을 갖는다 (tests/00 Step 3).
DECLARE @Wn BIGINT = (SELECT TOP (1) w.[WorkId] FROM [dbo].[예약접수] w
                        JOIN [dbo].[수검자] p ON p.[PatientId] = w.[PatientId]
                       WHERE p.[ChartNo] = N'T020' AND w.[StatusCode] = 'RSV' ORDER BY w.[WorkId]);
EXEC [dbo].[USP_HC_SELECT_예약접수상세] @Wn;
GO
