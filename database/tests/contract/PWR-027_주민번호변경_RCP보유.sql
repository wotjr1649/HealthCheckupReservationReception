SET NOCOUNT ON;
-- T014 는 tests/00b 가 만든 RCP Work 를 가진다. RSV 뿐 아니라 RCP 도 205 로 막는다 (05 §17.6).
DECLARE @Pid BIGINT, @Led DATETIME, @Cn NVARCHAR(100), @Nm NVARCHAR(100), @Ssn VARCHAR(13),
        @Mo VARCHAR(13), @Ph VARCHAR(13), @Em VARCHAR(200), @Zip VARCHAR(10),
        @Ad NVARCHAR(200), @Ad2 NVARCHAR(200), @Me NVARCHAR(MAX), @Hep BIT;
SELECT @Pid=[수검자ID], @Led=[최종수정일시], @Cn=[차트번호], @Nm=[성명],
       @Mo=[휴대전화], @Ph=[전화번호], @Em=[이메일], @Zip=[우편번호],
       @Ad=[주소], @Ad2=[상세주소], @Me=[비고], @Hep=[B형간염제외여부]
  FROM [dbo].[수검자] WHERE [차트번호] = N'T014';
EXEC [dbo].[USP_HC_수검자정보_수정] @Pid, @Led, @Cn, @Nm, '7010011999997', @Mo, @Ph, @Em, @Zip, @Ad, @Ad2, @Me, @Hep, N'TEST';
GO
