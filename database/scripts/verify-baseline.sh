#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),path=require("path"),c=require("crypto");
const DIR="../docs/baseline";
// 봉인 목록. 문서가 FINAL 이 되어 입주할 때 한 줄이 늘어난다 (ROOT AGENTS.md §2.1).
const exp={
 "00_Project_Policy.md":"b748098b7d1090028e8c9532b37629a24893025ab842bbd88eea408278bbb3a6",
 "01_Process_Definition.md":"079891e98f69d40082d822b53ad50ba81f16d21ee74ce3e45fd5b499e80811a0",
 "02_Function_Definition.xlsx":"6ef02f8edcf232fd8fa01865b2a5ba84ab9b78d0200c47174dd85fdfc30b8401",
 "03_Wireframe_Definition.md":"66b81a5f04eee7a4858c6361b9875e5d46aa548d11f0f98c3843b9521a1f2d9c",
 "04_DB_Design.md":"e3191c8fec66bb9b9018ae665de2d715554ffce19dbe443a0aa2d62e6ded62fa",
 "05_DB_Rule_SP_Contract.md":"c1da5eb502a7a2ee9c288d26ef4ac9642c28728a8319a5bb2ef34bd85692bbc8",
 "06_DB_Transaction_Security_Seed.md":"537c18c9f69f63e30f559fe6235be87d315af00f83b5900a93ee96dc411ba002"};
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
