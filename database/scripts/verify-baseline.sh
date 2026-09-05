#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),c=require("crypto");
const exp={
 "00_Project_Policy.md":"af2b17635c9b6f3264b91a1cdd671c2ff19aa4299044d282c676f9d8025143ac",
 "01_Process_Definition.md":"633bf096ae729674e70bf0a78209b68c9d068632fe68ba7f69e814d1f639325b",
 "02_Function_Definition.xlsx":"7763aaae5e7a48c16b3751777be0787fdbf62a0f22d1d91fd5995eda7a3897eb",
 "03_Wireframe_Definition.md":"f2660e62d436a4970a685779da1a4b6ef5338faaaf92f57798e4b7da1f0d145e",
 "04_DB_Design.md":"0148ad24733586d8c2efd5d72f8cac0aabb15c392eb4839ed597b57120bd8682",
 "05_DB_Rule_SP_Contract.md":"63457f8dde74dae403af944f063c892b70ee7d1b32b8163e8e4dffba3b96e7b6"};
let ok=0;
for(const [n,e] of Object.entries(exp)){
  const h=c.createHash("sha256").update(fs.readFileSync("../docs/baseline/"+n)).digest("hex");
  const m=h===e; if(m)ok++;
  console.log((m?"OK   ":"DIFF ")+n.padEnd(30)+h);
}
console.log("=== "+ok+"/6 ===");
if(ok!==6) process.exit(1);
'
