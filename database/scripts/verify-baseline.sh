#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),c=require("crypto");
const exp={
 "00_Project_Policy.md":"0d10397d607823bb85f357c91ebd00d686b65b15e3d5ff13938d13c823c22a92",
 "01_Process_Definition.md":"633bf096ae729674e70bf0a78209b68c9d068632fe68ba7f69e814d1f639325b",
 "02_Function_Definition.xlsx":"7763aaae5e7a48c16b3751777be0787fdbf62a0f22d1d91fd5995eda7a3897eb",
 "03_Wireframe_Definition.md":"831b61f27e38d60e856e9309906af2df89c5632a732b8289fdb216b257c80024",
 "04_DB_Design.md":"8176d8a82ae360718f811d7f26481b6cb56778585aeebd3b38b42ec89950038f",
 "05_DB_Rule_SP_Contract.md":"b865c76fa5d041fa816e0d2380a7ca16306e80502652a9369201fdc956f71c24"};
let ok=0;
for(const [n,e] of Object.entries(exp)){
  const h=c.createHash("sha256").update(fs.readFileSync("../docs/baseline/"+n)).digest("hex");
  const m=h===e; if(m)ok++;
  console.log((m?"OK   ":"DIFF ")+n.padEnd(30)+h);
}
console.log("=== "+ok+"/6 ===");
if(ok!==6) process.exit(1);
'
