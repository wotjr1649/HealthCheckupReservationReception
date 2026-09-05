/*  inspect.sql ― DB 구조·데이터를 한 화면에서 보는 조회 전용 스크립트

    배포물이 아니다. deploy/ 에 들어가지 않고 Deploy.sql 이 참조하지 않는다.
    읽기만 한다 ― INSERT/UPDATE/DELETE/DDL 이 한 줄도 없다.

    sqlcmd  ./scripts/inspect.sh
    SSMS    파일을 열고 F5. 아래 두 변수만 바꾼다.

    표시 폭을 좁히려고 NVARCHAR(100) 컬럼에 CAST 를 건다. 저장값을 자르는 것이 아니라
    화면만 자른다 ― 이 파일의 목적이 "한눈에" 이기 때문이다.

    GO 를 쓰지 않는다 ― 전체가 한 배치라 아래 변수가 끝까지 살아 있다(CLAUDE.md §11).
*/
SET NOCOUNT ON;

DECLARE @WorkId  BIGINT        = NULL;   -- [3] 볼 업무. NULL 이면 검사구성이 가장 많은 업무
DECLARE @ChartNo NVARCHAR(100) = NULL;   -- [4] 볼 수검자. NULL 이면 업무가 가장 많은 수검자

IF @WorkId IS NULL
    SELECT TOP (1) @WorkId = [업무ID]
      FROM [dbo].[예약접수] ORDER BY LEN([국가검사항목]) DESC, [업무ID];

IF @ChartNo IS NULL
    SELECT TOP (1) @ChartNo = p.[차트번호]
      FROM [dbo].[예약접수] w JOIN [dbo].[수검자] p ON p.[수검자ID] = w.[수검자ID]
     GROUP BY p.[차트번호] ORDER BY COUNT(*) DESC, p.[차트번호];

PRINT N'==== [1] 6개 테이블 ====';
SELECT TableNm = CAST(t.[name] AS NVARCHAR(10))
     , Rws     = SUM(CASE WHEN i.[index_id] IN (0,1) THEN p.[rows] ELSE 0 END)
     , UsedKB  = SUM(a.[used_pages]) * 8
  FROM sys.tables t
  JOIN sys.indexes    i ON i.[object_id] = t.[object_id]
  JOIN sys.partitions p ON p.[object_id] = i.[object_id] AND p.[index_id] = i.[index_id]
  JOIN sys.allocation_units a ON a.[container_id] = p.[partition_id]
 GROUP BY t.[name]
 ORDER BY t.[name];

PRINT N'==== [2] 검사코드 Master 19행 · 역할 ====';
SELECT Code    = [검사항목코드]
     , Nm      = CAST([검사항목명] AS NVARCHAR(14))
     , Role    = CAST(CASE WHEN [국가검사규칙코드] IS NOT NULL AND [추가검사코드] IS NOT NULL THEN 'NEX+AEX'
                           WHEN [국가검사규칙코드] IS NOT NULL                                      THEN 'NEX'
                           ELSE                                                                     'AEX' END AS CHAR(7))
     , NexRule = ISNULL([국가검사규칙코드], '-')
     , AexCode = ISNULL([추가검사코드], '-')
     , Sex     = ISNULL([추가검사성별코드], '-')
     , AexOn   = [추가검사사용여부]
  FROM [dbo].[검사코드]
 ORDER BY [검사항목코드];

PRINT N'==== [3] 업무 1건 상세 ====';
SELECT WorkId  = w.[업무ID]
     , ChartNo = CAST(p.[차트번호] AS NVARCHAR(8)), Nm = CAST(p.[성명] AS NVARCHAR(10))
     , Sex     = p.[성별], Birth = p.[생년월일]
     , ResDate = w.[예약일], Slot = w.[시간대코드], Status = w.[상태코드]
     , Edited  = w.[최종수정일시]
  FROM [dbo].[예약접수] w
  JOIN [dbo].[수검자]   p ON p.[수검자ID] = w.[수검자ID]
 WHERE w.[업무ID] = @WorkId;

SELECT Src    = V.Src
     , Code   = m.[검사항목코드]
     , Nm     = CAST(m.[검사항목명] AS NVARCHAR(14))
     , RuleCd = CASE WHEN V.Src = 'AEX' THEN m.[추가검사코드] ELSE m.[국가검사규칙코드] END
  FROM [dbo].[예약접수] w
 CROSS APPLY (VALUES ('NEX', w.[국가검사항목]), ('AEX', w.[추가검사항목])) V(Src, Lst)
  JOIN [dbo].[검사코드] m
    ON N',' + ISNULL(V.Lst, N'') + N',' LIKE N'%,' + m.[검사항목코드] + N',%'
 WHERE w.[업무ID] = @WorkId
 ORDER BY V.Src, m.[검사항목코드];

PRINT N'==== [4] 수검자 1명의 업무 타임라인 ====';
SELECT ChartNo = CAST(p.[차트번호] AS NVARCHAR(8)), Nm = CAST(p.[성명] AS NVARCHAR(10))
     , WorkId  = w.[업무ID]
     , ResDate = w.[예약일], Slot = w.[시간대코드], Status = w.[상태코드]
     , NEX     = CAST(w.[국가검사항목] AS NVARCHAR(40))
     , AEX     = CAST(w.[추가검사항목] AS NVARCHAR(20))
  FROM [dbo].[수검자]   p
  JOIN [dbo].[예약접수] w ON w.[수검자ID] = p.[수검자ID]
 WHERE p.[차트번호] = @ChartNo
 ORDER BY w.[예약일], w.[업무ID];

SELECT ChartNo = CAST(p.[차트번호] AS NVARCHAR(8)), LastCheckup = h.[완료일자]
     , NEX = CAST(h.[국가검사항목] AS NVARCHAR(40)), AEX = CAST(h.[추가검사항목] AS NVARCHAR(20))
  FROM [dbo].[수검자] p
  JOIN [dbo].[완료이력] h ON h.[수검자ID] = p.[수검자ID]
 WHERE p.[차트번호] = @ChartNo
 ORDER BY h.[완료일자] DESC;

PRINT N'==== [5] 슬롯별 정원 현황 (RSV+RCP 만 산정 · 정원 20) ====';
SELECT ResDate   = w.[예약일]
     , Slot      = w.[시간대코드]
     , Used      = COUNT(*)
     , SeatsLeft = CASE WHEN 20 - COUNT(*) < 0 THEN 0 ELSE 20 - COUNT(*) END
     , Holiday   = CAST(ISNULL(h.[휴무일명], N'') AS NVARCHAR(12))
  FROM [dbo].[예약접수] w
  LEFT JOIN [dbo].[휴무일] h ON h.[휴무일자] = w.[예약일] AND h.[사용여부] = 1
 WHERE w.[상태코드] IN ('RSV','RCP')
 GROUP BY w.[예약일], w.[시간대코드], h.[휴무일명]
 ORDER BY w.[예약일], w.[시간대코드];
