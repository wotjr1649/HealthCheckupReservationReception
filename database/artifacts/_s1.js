const fs = require('fs');
function R(P, o, x, tag) {
  let s = fs.readFileSync(P, 'utf8');
  const NL = s.includes('\r\n') ? '\r\n' : '\n';
  const O = o.split('\n').join(NL), X = x.split('\n').join(NL);
  if (s.split(O).length - 1 !== 1) throw new Error(tag + ' 앵커');
  fs.writeFileSync(P, s.split(O).join(X)); console.log('  ' + tag);
}
const B = '../docs/baseline/04_DB_Design.md';
R(B, `| \`DF_수검자_HEPATITIS_B_EXCLUDED\` | \`HepatitisBExcluded\` | \`0\` |
| \`DF_수검자_CREATION_DATE\` | \`CreationDate\` | \`GETDATE()\` |
| \`DF_수검자_LAST_EDIT_DATE\` | \`LastEditDate\` | \`GETDATE()\` |`,
`| \`DF_수검자_HEPATITIS_B_EXCLUDED\` | \`B형간염제외여부\` | \`0\` |
| \`DF_수검자_CREATION_DATE\` | \`생성일시\` | \`GETDATE()\` |
| \`DF_수검자_LAST_EDIT_DATE\` | \`최종수정일시\` | \`GETDATE()\` |`, '§8.1.4 Default');
R(B, `| \`UQ_수검자_CHART_NO\` | \`ChartNo\` | - | 차트번호 정확조회·고유성 |
| \`UQ_수검자_SOCIAL_NUMBER\` | \`SocialNumber\` | - | 주민번호 테스트값 정확조회·고유성 |
| \`IX_수검자_NAME_BIRTHDAY\` | \`Name, Birthday\` | \`PatientId, ChartNo, Gender, CelNumber\` | 이름 조회·이름+생년월일 중복후보 |
| \`IX_수검자_BIRTHDAY\` | \`Birthday\` | \`PatientId, ChartNo, Name, Gender, CelNumber\` | 생년월일 단독조회·중복후보 |
| \`IX_수검자_CEL_NUMBER_S\` | \`CelNumberS\` | \`PatientId, ChartNo, Name, Birthday, Gender, CelNumber\` / \`WHERE CelNumberS IS NOT NULL\` | 휴대전화 정확조회 |

\`HepatitisBExcluded\`는 항상 \`PatientId\`로 단일행을 집은 뒤 읽으므로 Index를 두지 않는다.`,
`| \`UQ_수검자_CHART_NO\` | \`차트번호\` | - | 차트번호 정확조회·고유성 |
| \`UQ_수검자_SOCIAL_NUMBER\` | \`주민번호\` | - | 주민번호 테스트값 정확조회·고유성 |
| \`IX_수검자_NAME_BIRTHDAY\` | \`성명, 생년월일\` | \`수검자ID, 차트번호, 성별, 휴대전화\` | 이름 조회·이름+생년월일 중복후보 |
| \`IX_수검자_BIRTHDAY\` | \`생년월일\` | \`수검자ID, 차트번호, 성명, 성별, 휴대전화\` | 생년월일 단독조회·중복후보 |

\`B형간염제외여부\`는 항상 \`수검자ID\`로 단일행을 집은 뒤 읽으므로 Index를 두지 않는다.

휴대전화 검색 전용 Index 를 두지 않는다. \`CelNumberS\` 를 제거하면서 그 Key 컬럼이 사라졌고,
\`@MobilePhone\` 검색은 \`REPLACE([휴대전화], '-', '')\` 비교라 seek 이 성립하지 않는다(§8.1.2).`, '§8.1.5 Index');
// 검사기 타입 정규화 - ROWVERSION 은 timestamp 의 별칭이다
R('tools/verify-schema-doc.js',
  `  const norm = t => t.toLowerCase().replace(/\s*identity\(1,1\)/, '').replace(/\(.*\)/, '').trim();`,
  `  // ROWVERSION 은 timestamp 의 별칭이다. sys.types 는 timestamp 로 돌려준다.\n  const norm = t => t.toLowerCase().replace(/\s*identity\(1,1\)/, '').replace(/\(.*\)/, '').trim()\n                     .replace(/^rowversion$/, 'timestamp');`, '검사기 타입 정규화');
