#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),c=require("crypto");
const exp={
 "00_Project_Policy.md":"b748098b7d1090028e8c9532b37629a24893025ab842bbd88eea408278bbb3a6",
 "01_Process_Definition.md":"079891e98f69d40082d822b53ad50ba81f16d21ee74ce3e45fd5b499e80811a0",
 "02_Function_Definition.xlsx":"e4e6601d07cf86c23a1f30d5bd17e18f993949b30cfd769de239d1a50c83e41a",
 "03_Wireframe_Definition.md":"66b81a5f04eee7a4858c6361b9875e5d46aa548d11f0f98c3843b9521a1f2d9c",
 "04_DB_Design.md":"67dc80334770f516e4c78936024bf2e6fba3e6bd8d776738b13c3d04e45ec30b",
 "05_DB_Rule_SP_Contract.md":"b9c16d7c21c7b63269e39a2a6742a0d166aa9d36e9be7992dc5236da1ed8b39e"};
let ok=0;
for(const [n,e] of Object.entries(exp)){
  const h=c.createHash("sha256").update(fs.readFileSync("../docs/baseline/"+n)).digest("hex");
  const m=h===e; if(m)ok++;
  console.log((m?"OK   ":"DIFF ")+n.padEnd(30)+h);
}
console.log("=== "+ok+"/6 ===");
if(ok!==6) process.exit(1);
'
