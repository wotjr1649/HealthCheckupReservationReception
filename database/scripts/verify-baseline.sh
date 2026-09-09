#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),path=require("path"),c=require("crypto");
const DIR="../docs/baseline";
// 봉인 목록. 문서가 FINAL 이 되어 입주할 때 한 줄이 늘어난다 (ROOT AGENTS.md §2.1).
const exp={
 "00_Project_Policy.md":"4e8f70fe07ca0f459aa9c41a5dadb88c0df5c33539cdf34eb2596fe2a8100722",
 "01_Process_Definition.md":"39d34063b495f28a2d870bf6322c4f557a43ec1fd80418a08aef6c65cdf0b29a",
 "02_Function_Definition.xlsx":"cd616207717d4cbb8c2ed3284061d728af4933af00dac75acf835650865798c0",
 "03_Wireframe_Definition.md":"fea36bbdd45b15c6b9ee1447c4fbf8f1bf1fbdbf3febbee2155b256c2db841ab",
 "04_DB_Design.md":"004864e313df74c25f62da46bb5a78573633c3d86ee6db121a421ef51b4a902e",
 "05_DB_Rule_SP_Contract.md":"646d2ccbea0d90dd528aadceed3d8866b93f908765278f1435ff01c4f898d444",
 "06_DB_Transaction_Security_Seed.md":"b37f3ac3388e0a2a80387b81e3590951610fcc3629d9d790b4985b4aa1474791"};
const N=Object.keys(exp).length;
let ok=0;
for(const [n,e] of Object.entries(exp)){
  const p=path.join(DIR,n);
  if(!fs.existsSync(p)){ console.log("MISS "+n.padEnd(38)+"파일이 없다"); continue; }
  const h=c.createHash("sha256").update(fs.readFileSync(p)).digest("hex");
  const m=h===e; if(m)ok++;
  console.log((m?"OK   ":"DIFF ")+n.padEnd(38)+h);
}
// [!] 목록에 없는 파일이 봉인 폴더에 들어오면 아무도 안 지킨다.
//     "여기 있는 건 다 봉인됐다" 가 이 폴더의 유일한 뜻이므로 그 상태를 허용하지 않는다.
//     output/ 은 생성물이라 대상이 아니다 (ROOT CLAUDE.md §3).
const stray=fs.readdirSync(DIR)
  .filter(f=>fs.statSync(path.join(DIR,f)).isFile())
  .filter(f=>!(f in exp));
if(stray.length){
  console.log("STRAY 봉인 목록에 없는 파일 "+stray.length+"건 — 입주시켰으면 해시를 추가하라");
  stray.forEach(f=>console.log("      "+f));
}
console.log("=== "+ok+"/"+N+(stray.length?" · 미등록 "+stray.length:"")+" ===");
if(ok!==N||stray.length) process.exit(1);
'
