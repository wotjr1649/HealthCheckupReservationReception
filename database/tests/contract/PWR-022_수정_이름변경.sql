SET NOCOUNT ON;
-- PWR-000 이 성명을 N'테스트사육' 으로 되돌려 두므로 이 호출은 항상 실제 변경이다.
DECLARE @Pid BIGINT, @Led DATETIME, @Cn NVARCHAR(100), @Nm NVARCHAR(100), @Ssn VARCHAR(13),
        @Mo VARCHAR(13), @Ph VARCHAR(13), @Em VARCHAR(200), @Zip VARCHAR(10),
        @Ad NVARCHAR(200), @Ad2 NVARCHAR(200), @Me NVARCHAR(MAX), @Hep BIT;
SELECT @Pid=[수검자ID], @Led=[최종수정일시], @Cn=[차트번호], @Ssn=[주민번호],
       @Mo=[휴대전화], @Ph=[전화번호], @Em=[이메일], @Zip=[우편번호],
       @Ad=[주소], @Ad2=[상세주소], @Me=[비고], @Hep=[B형간염제외여부]
  FROM [dbo].[수검자] WHERE [차트번호] = N'T015';
EXEC [dbo].[USP_HC_수검자정보_수정] @Pid, @Led, @Cn, N'이름변경됨', @Ssn, @Mo, @Ph, @Em, @Zip, @Ad, @Ad2, @Me, @Hep, N'TEST';
GO
