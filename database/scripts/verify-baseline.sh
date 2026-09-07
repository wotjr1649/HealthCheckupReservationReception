#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),c=require("crypto");
const exp={
 "00_Project_Policy.md":"b33aa64df24f9d42481fd1bea5a8e05da65f2cfe30892b66266dea67e388864e",
 "01_Process_Definition.md":"079891e98f69d40082d822b53ad50ba81f16d21ee74ce3e45fd5b499e80811a0",
 "02_Function_Definition.xlsx":"e4e6601d07cf86c23a1f30d5bd17e18f993949b30cfd769de239d1a50c83e41a",
 "03_Wireframe_Definition.md":"508f12d323269a8b716551bc6202d42feaa675eaab275a4f3d1a8d785dbdc045",
 "04_DB_Design.md":"cb1a40045e296f88abf14c6a9dad47a3c0c50c04ffe2d0e53fbd85ef76ea0157",
 "05_DB_Rule_SP_Contract.md":"1861025a0463cb89625873399e0847297d004d0505a57d5b940479b35a3a1dd3"};
let ok=0;
for(const [n,e] of Object.entries(exp)){
  const h=c.createHash("sha256").update(fs.readFileSync("../docs/baseline/"+n)).digest("hex");
  const m=h===e; if(m)ok++;
  console.log((m?"OK   ":"DIFF ")+n.padEnd(30)+h);
}
console.log("=== "+ok+"/6 ===");
if(ok!==6) process.exit(1);
'
