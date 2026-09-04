#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
node -e '
const fs=require("fs"),c=require("crypto");
const exp={
 "00_Project_Policy.md":"5adba8d4001e8f7aa27091614df33922a7d3d7cf27965f9ccc60874316beaefc",
 "01_Process_Definition.md":"1b0d1c23cb8dda15c6c0ba46a86a48a2586608b42079ed96a837386ae35f69e6",
 "02_Function_Definition.xlsx":"ac7b362ea79b062a889cd296bada304db4f66cb850a2ab04ca999c7530a1654a",
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
