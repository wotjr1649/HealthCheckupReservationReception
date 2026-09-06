// xlsx(zip) 안의 워크시트 XML 을 문자열 치환으로 고치고 다시 묶는다. 의존성 0.
// 바꾸지 않는 엔트리는 압축된 바이트를 그대로 옮기므로 손실이 없다.
const fs = require('fs'), zlib = require('zlib');
const SIGL = Buffer.from([0x50, 0x4b, 0x03, 0x04]);
const SIGC = Buffer.from([0x50, 0x4b, 0x01, 0x02]);
const SIGE = Buffer.from([0x50, 0x4b, 0x05, 0x06]);

function read(file) {
  const b = fs.readFileSync(file);
  // 중앙 디렉터리를 권위로 삼는다. 로컬 헤더만 훑으면 데이터 안의 시그니처에 속을 수 있다.
  let eocd = b.length - 22;
  while (eocd >= 0 && b.compare(SIGE, 0, 4, eocd, eocd + 4) !== 0) eocd--;
  if (eocd < 0) throw new Error('EOCD 없음');
  const count = b.readUInt16LE(eocd + 10);
  let p = b.readUInt32LE(eocd + 16);
  const items = [];
  for (let k = 0; k < count; k++) {
    if (b.compare(SIGC, 0, 4, p, p + 4) !== 0) throw new Error('중앙 디렉터리 손상 @' + p);
    const flags = b.readUInt16LE(p + 8), method = b.readUInt16LE(p + 10);
    const mtime = b.readUInt16LE(p + 12), mdate = b.readUInt16LE(p + 14);
    const crc = b.readUInt32LE(p + 16), csize = b.readUInt32LE(p + 20), usize = b.readUInt32LE(p + 24);
    const nlen = b.readUInt16LE(p + 28), elen = b.readUInt16LE(p + 30), clen = b.readUInt16LE(p + 32);
    const extAttr = b.readUInt32LE(p + 38), lho = b.readUInt32LE(p + 42);
    const name = b.slice(p + 46, p + 46 + nlen).toString();
    const lnlen = b.readUInt16LE(lho + 26), lelen = b.readUInt16LE(lho + 28);
    const data = b.slice(lho + 30 + lnlen + lelen, lho + 30 + lnlen + lelen + csize);
    items.push({ name, flags, method, mtime, mdate, crc, csize, usize, extAttr, data });
    p += 46 + nlen + elen + clen;
  }
  return items;
}

function write(file, items) {
  const chunks = [], central = [];
  let off = 0;
  for (const it of items) {
    const nb = Buffer.from(it.name);
    const lh = Buffer.alloc(30);
    SIGL.copy(lh, 0);
    lh.writeUInt16LE(20, 4); lh.writeUInt16LE(it.flags, 6); lh.writeUInt16LE(it.method, 8);
    lh.writeUInt16LE(it.mtime, 10); lh.writeUInt16LE(it.mdate, 12);
    lh.writeUInt32LE(it.crc, 14); lh.writeUInt32LE(it.csize, 18); lh.writeUInt32LE(it.usize, 22);
    lh.writeUInt16LE(nb.length, 26); lh.writeUInt16LE(0, 28);
    chunks.push(lh, nb, it.data);
    const ch = Buffer.alloc(46);
    SIGC.copy(ch, 0);
    ch.writeUInt16LE(20, 4); ch.writeUInt16LE(20, 6); ch.writeUInt16LE(it.flags, 8); ch.writeUInt16LE(it.method, 10);
    ch.writeUInt16LE(it.mtime, 12); ch.writeUInt16LE(it.mdate, 14);
    ch.writeUInt32LE(it.crc, 16); ch.writeUInt32LE(it.csize, 20); ch.writeUInt32LE(it.usize, 24);
    ch.writeUInt16LE(nb.length, 28); ch.writeUInt32LE(it.extAttr, 38); ch.writeUInt32LE(off, 42);
    central.push(ch, nb);
    off += lh.length + nb.length + it.data.length;
  }
  const cdStart = off;
  let cdLen = 0; for (const c of central) cdLen += c.length;
  const eo = Buffer.alloc(22);
  SIGE.copy(eo, 0);
  eo.writeUInt16LE(items.length, 8); eo.writeUInt16LE(items.length, 10);
  eo.writeUInt32LE(cdLen, 12); eo.writeUInt32LE(cdStart, 16);
  fs.writeFileSync(file, Buffer.concat([...chunks, ...central, eo]));
}

function text(it) {
  return (it.method === 8 ? zlib.inflateRawSync(it.data) : it.data).toString('utf8');
}
function setText(it, s) {
  const raw = Buffer.from(s, 'utf8');
  it.usize = raw.length;
  it.crc = zlib.crc32(raw);
  it.method = 8;
  it.data = zlib.deflateRawSync(raw, { level: 9 });
  it.csize = it.data.length;
}
module.exports = { read, write, text, setText };
