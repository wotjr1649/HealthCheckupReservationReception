SET NOCOUNT ON;
DECLARE @Pid BIGINT, @Led BINARY(8), @Cn NVARCHAR(100), @Nm NVARCHAR(100), @Ssn VARCHAR(13),
        @Mo VARCHAR(13), @Ph VARCHAR(13), @Em VARCHAR(200), @Zip VARCHAR(10),
        @Ad NVARCHAR(200), @Ad2 NVARCHAR(200), @Me NVARCHAR(MAX), @Hep BIT;
DECLARE @T001Ssn VARCHAR(13) = (SELECT [주민번호] FROM [dbo].[수검자] WHERE [차트번호] = N'T001');
SELECT @Pid=[수검자ID], @Led=[행버전], @Cn=[차트번호], @Nm=[성명],
       @Mo=[휴대전화], @Ph=[전화번호], @Em=[이메일], @Zip=[우편번호],
       @Ad=[주소], @Ad2=[상세주소], @Me=[비고], @Hep=[B형간염제외여부]
  FROM [dbo].[수검자] WHERE [차트번호] = N'T015';
EXEC [dbo].[USP_HC_수검자정보_수정] @Pid, @Led, @Cn, @Nm, @T001Ssn, @Mo, @Ph, @Em, @Zip, @Ad, @Ad2, @Me, @Hep, N'TEST';
GO
