const fs = require('fs');
function R(P, o, x, tag) {
  let s = fs.readFileSync(P, 'utf8');
  const NL = s.includes('\r\n') ? '\r\n' : '\n';
  const O = o.split('\n').join(NL), X = x.split('\n').join(NL);
  if (s.split(O).length - 1 !== 1) throw new Error(tag + ' 앵커');
  fs.writeFileSync(P, s.split(O).join(X)); console.log('  ' + tag);
}
R('../docs/baseline/05_DB_Rule_SP_Contract.md',
  '검사항목=NEX 전체 + Selected=1인 AEX', '검사구성=NEX 전체 + Selected=1인 AEX', '05:1657');
R('../docs/phase4/plans/01-preflight-schema.md',
`CREATE NONCLUSTERED INDEX [IX_수검자_NAME_BIRTHDAY]
    ON [dbo].[수검자] ([성명], [생년월일])
    INCLUDE ([수검자ID], [차트번호], [Gender], [CelNumber]);
GO
CREATE NONCLUSTERED INDEX [IX_수검자_BIRTHDAY]
    ON [dbo].[수검자] ([생년월일])
    INCLUDE ([수검자ID], [차트번호], [Name], [Gender], [CelNumber]);
GO
CREATE NONCLUSTERED INDEX [IX_수검자_CEL_NUMBER_S]
    ON [dbo].[수검자] ([CelNumberS])
    INCLUDE ([수검자ID], [차트번호], [Name], [생년월일], [Gender], [CelNumber])
    WHERE [CelNumberS] IS NOT NULL;
GO`,
`CREATE NONCLUSTERED INDEX [IX_수검자_NAME_BIRTHDAY]
    ON [dbo].[수검자] ([성명], [생년월일])
    INCLUDE ([수검자ID], [차트번호], [성별], [휴대전화]);
GO
CREATE NONCLUSTERED INDEX [IX_수검자_BIRTHDAY]
    ON [dbo].[수검자] ([생년월일])
    INCLUDE ([수검자ID], [차트번호], [성명], [성별], [휴대전화]);
GO`, 'plans/01 인덱스 3 -> 2');
