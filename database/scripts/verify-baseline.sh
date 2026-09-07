#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),c=require("crypto");
const exp={
 "00_Project_Policy.md":"b748098b7d1090028e8c9532b37629a24893025ab842bbd88eea408278bbb3a6",
 "01_Process_Definition.md":"079891e98f69d40082d822b53ad50ba81f16d21ee74ce3e45fd5b499e80811a0",
 "02_Function_Definition.xlsx":"6ef02f8edcf232fd8fa01865b2a5ba84ab9b78d0200c47174dd85fdfc30b8401",
 "03_Wireframe_Definition.md":"66b81a5f04eee7a4858c6361b9875e5d46aa548d11f0f98c3843b9521a1f2d9c",
 "04_DB_Design.md":"257680d582a419ab959fecc37feaf46a660780a21f4120a92c7ae3f82c94dd44",
 "05_DB_Rule_SP_Contract.md":"4f749a7c991b6ef45fa2a48f5e703694cac9bbf85aee8e8f7731b638750ad08e"};
let ok=0;
for(const [n,e] of Object.entries(exp)){
  const h=c.createHash("sha256").update(fs.readFileSync("../docs/baseline/"+n)).digest("hex");
  const m=h===e; if(m)ok++;
  console.log((m?"OK   ":"DIFF ")+n.padEnd(30)+h);
}
console.log("=== "+ok+"/6 ===");
if(ok!==6) process.exit(1);
'
