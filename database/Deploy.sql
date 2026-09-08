:setvar DeployDir "deploy"
SET NOCOUNT ON;
PRINT '=== Deploy 시작 ' + CONVERT(VARCHAR(40), SYSDATETIMEOFFSET(), 126) + ' ===';
GO
:r $(DeployDir)\00_Preflight.sql
:r $(DeployDir)\01_Schema.sql
:r $(DeployDir)\02_Seed.sql
:r $(DeployDir)\03_Functions.sql
:r $(DeployDir)\04_Procedures_Select.sql
:r $(DeployDir)\05_Procedures_Patient_Write.sql
:r $(DeployDir)\06_Procedures_Reservation_Write.sql
:r $(DeployDir)\07_Procedures_Reception_Write.sql
:r $(DeployDir)\07a_Procedures_Holiday.sql
:r $(DeployDir)\08_Verify.sql
GO
PRINT '=== Deploy 완료 ===';
GO
