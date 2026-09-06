#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),c=require("crypto");
const exp={
 "00_Project_Policy.md":"b33aa64df24f9d42481fd1bea5a8e05da65f2cfe30892b66266dea67e388864e",
 "01_Process_Definition.md":"079891e98f69d40082d822b53ad50ba81f16d21ee74ce3e45fd5b499e80811a0",
 "02_Function_Definition.xlsx":"e4e6601d07cf86c23a1f30d5bd17e18f993949b30cfd769de239d1a50c83e41a",
 "03_Wireframe_Definition.md":"84c283d6f4c761b45362f91ce4f208a24389c0c38815dd5f59b71c45c476a5b8",
 "04_DB_Design.md":"359a14a0800de11fd322ed98cb081277602eb65b3fe88846418ee3ddd0e856b9",
 "05_DB_Rule_SP_Contract.md":"0cd0776011d7cb35d688139d0269c9e1287a876dd606e61692448892842a33ed"};
let ok=0;
for(const [n,e] of Object.entries(exp)){
  const h=c.createHash("sha256").update(fs.readFileSync("../docs/baseline/"+n)).digest("hex");
  const m=h===e; if(m)ok++;
  console.log((m?"OK   ":"DIFF ")+n.padEnd(30)+h);
}
console.log("=== "+ok+"/6 ===");
if(ok!==6) process.exit(1);
'
