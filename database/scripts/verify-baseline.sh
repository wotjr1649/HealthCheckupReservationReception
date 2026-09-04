#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),c=require("crypto");
const exp={
 "00_Project_Policy.md":"0d10397d607823bb85f357c91ebd00d686b65b15e3d5ff13938d13c823c22a92",
 "01_Process_Definition.md":"633bf096ae729674e70bf0a78209b68c9d068632fe68ba7f69e814d1f639325b",
 "02_Function_Definition.xlsx":"7763aaae5e7a48c16b3751777be0787fdbf62a0f22d1d91fd5995eda7a3897eb",
 "03_Wireframe_Definition.md":"f2660e62d436a4970a685779da1a4b6ef5338faaaf92f57798e4b7da1f0d145e",
 "04_DB_Design.md":"89041998a9c1645d35170fb23416d7c8f72997c1199553a2b02a59d768ca7a0c",
 "05_DB_Rule_SP_Contract.md":"70dd32295ec667734c99d76bce3ae8a91eaac8b7f184ead5d483c54a34300a78"};
let ok=0;
for(const [n,e] of Object.entries(exp)){
  const h=c.createHash("sha256").update(fs.readFileSync("../docs/baseline/"+n)).digest("hex");
  const m=h===e; if(m)ok++;
  console.log((m?"OK   ":"DIFF ")+n.padEnd(30)+h);
}
console.log("=== "+ok+"/6 ===");
if(ok!==6) process.exit(1);
'
