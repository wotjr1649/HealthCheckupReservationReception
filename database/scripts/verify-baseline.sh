#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),c=require("crypto");
const exp={
 "00_Project_Policy.md":"b33aa64df24f9d42481fd1bea5a8e05da65f2cfe30892b66266dea67e388864e",
 "01_Process_Definition.md":"079891e98f69d40082d822b53ad50ba81f16d21ee74ce3e45fd5b499e80811a0",
 "02_Function_Definition.xlsx":"e4e6601d07cf86c23a1f30d5bd17e18f993949b30cfd769de239d1a50c83e41a",
 "03_Wireframe_Definition.md":"d3999d7936f8f9b759a98c02db0bc4a9e4658c39c5b5823b859c97b05cbe5d31",
 "04_DB_Design.md":"253e2cc43ae4a3afc04eecee490479681410d3936be71c98d928f30af6471653",
 "05_DB_Rule_SP_Contract.md":"540e356a649dbeed4f13de453a3d77a07707e14577aba5a4ecce160d4e7308a3"};
let ok=0;
for(const [n,e] of Object.entries(exp)){
  const h=c.createHash("sha256").update(fs.readFileSync("../docs/baseline/"+n)).digest("hex");
  const m=h===e; if(m)ok++;
  console.log((m?"OK   ":"DIFF ")+n.padEnd(30)+h);
}
console.log("=== "+ok+"/6 ===");
if(ok!==6) process.exit(1);
'
