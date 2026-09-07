SET NOCOUNT ON;
-- 차트번호 변경은 활성 Work 가 있어도 차단하지 않는다 (00 EP-08). 205 는 주민번호에만 건다.
DECLARE @Pid BIGINT, @Led DATETIME, @Cn NVARCHAR(100), @Nm NVARCHAR(100), @Ssn VARCHAR(13),
        @Mo VARCHAR(13), @Ph VARCHAR(13), @Em VARCHAR(200), @Zip VARCHAR(10),
        @Ad NVARCHAR(200), @Ad2 NVARCHAR(200), @Me NVARCHAR(MAX), @Hep BIT;
SELECT @Pid=[수검자ID], @Led=[최종수정일시], @Nm=[성명], @Ssn=[주민번호],
       @Mo=[휴대전화], @Ph=[전화번호], @Em=[이메일], @Zip=[우편번호],
       @Ad=[주소], @Ad2=[상세주소], @Me=[비고], @Hep=[B형간염제외여부]
  FROM [dbo].[수검자] WHERE [차트번호] = N'F001';
EXEC [dbo].[USP_HC_수검자정보_수정] @Pid, @Led, N'F001X', @Nm, @Ssn, @Mo, @Ph, @Em, @Zip, @Ad, @Ad2, @Me, @Hep, N'TEST';
GO
