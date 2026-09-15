#!/usr/bin/env node
/*
 * 프로그램 아이콘 생성기 — `src/.../Resources/App.ico`
 *
 * `[!]` **바이너리를 손으로 만지지 않는다.** 아이콘을 고치려면 이 파일의 도형을 고쳐 다시
 *       돌린다 (ROOT AGENTS.md §3 이 산출물에 세운 것과 같은 규칙). 손으로 고친 .ico 는
 *       무엇이 왜 그렇게 생겼는지 아무도 되짚을 수 없다.
 *
 * 모양 — 파란 둥근 사각형에 흰 체크. 16px 에서도 읽히려면 형태가 하나여야 한다.
 *        버튼 아이콘(`Resources/Icons/*.svg`)의 「접수 = 확인」과 같은 결이다.
 *
 * `[X]` **PNG 압축 항목을 쓰지 않는다.** .NET 의 `System.Drawing.Icon` 은 PNG 로 압축된
 *       항목을 제대로 읽지 못하는 경우가 있어, 폼이 아이콘을 잃는다. 전부 무압축 32bpp
 *       DIB 로 넣는다 — 크기 넷이 33KB 남짓이라 그 값을 치를 이유가 없다.
 *
 * `[X]` **256 은 넣지 않는다.** 무압축이라 그 하나가 270KB 다. 제목표시줄(16)·작업표시줄(32)
 *       ·큰 아이콘(48·64)이 실제로 쓰이는 자리이고, 그보다 크면 Windows 가 64 를 늘린다.
 *
 *   node tools/build-app-icon.js
 */
'use strict';

const fs = require('fs');
const path = require('path');

const OUT = path.resolve(__dirname, '..', 'src', 'HealthCheckupReservationReception.WinForms', 'Resources', 'App.ico');
const SIZES = [16, 32, 48, 64];
const SUPERSAMPLE = 4;

/* 파란 바탕과 흰 체크. 버튼 아이콘의 파랑(#2F6FB5)과 같은 값이다. */
const BG = { r: 0x2F, g: 0x6F, b: 0xB5 };
const FG = { r: 0xFF, g: 0xFF, b: 0xFF };

/* 0~1 로 정규화한 도형. 크기가 바뀌어도 같은 비율로 그려진다. */
const PAD = 0.06;
const RADIUS = 0.20;
const CHECK = [[0.27, 0.52], [0.43, 0.69], [0.75, 0.31]];
const CHECK_HALF = 0.058;

function insideRoundedRect(x, y) {
  const lo = PAD;
  const hi = 1 - PAD;
  if (x < lo || x > hi || y < lo || y > hi) {
    return false;
  }

  // 모서리 넷만 원으로 깎는다.
  const cx = x < lo + RADIUS ? lo + RADIUS : (x > hi - RADIUS ? hi - RADIUS : x);
  const cy = y < lo + RADIUS ? lo + RADIUS : (y > hi - RADIUS ? hi - RADIUS : y);
  const dx = x - cx;
  const dy = y - cy;
  return dx * dx + dy * dy <= RADIUS * RADIUS;
}

function distanceToSegment(px, py, ax, ay, bx, by) {
  const vx = bx - ax;
  const vy = by - ay;
  const wx = px - ax;
  const wy = py - ay;
  const len = vx * vx + vy * vy;
  let t = len === 0 ? 0 : (wx * vx + wy * vy) / len;
  t = t < 0 ? 0 : (t > 1 ? 1 : t);
  const dx = px - (ax + t * vx);
  const dy = py - (ay + t * vy);
  return Math.sqrt(dx * dx + dy * dy);
}

function onCheck(x, y) {
  for (let i = 0; i + 1 < CHECK.length; i++) {
    const a = CHECK[i];
    const b = CHECK[i + 1];
    if (distanceToSegment(x, y, a[0], a[1], b[0], b[1]) <= CHECK_HALF) {
      return true;
    }
  }

  return false;
}

/** 한 크기의 BGRA 화소를 위에서 아래 순서로 만든다. */
function render(size) {
  const pixels = Buffer.alloc(size * size * 4);
  const step = 1 / (size * SUPERSAMPLE);
  const total = SUPERSAMPLE * SUPERSAMPLE;

  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      let bg = 0;
      let fg = 0;
      for (let sy = 0; sy < SUPERSAMPLE; sy++) {
        for (let sx = 0; sx < SUPERSAMPLE; sx++) {
          const px = (x * SUPERSAMPLE + sx + 0.5) * step;
          const py = (y * SUPERSAMPLE + sy + 0.5) * step;
          if (!insideRoundedRect(px, py)) {
            continue;
          }

          bg++;
          if (onCheck(px, py)) {
            fg++;
          }
        }
      }

      const alpha = bg / total;
      const mix = bg === 0 ? 0 : fg / bg;
      const at = (y * size + x) * 4;
      pixels[at] = Math.round(BG.b + (FG.b - BG.b) * mix);
      pixels[at + 1] = Math.round(BG.g + (FG.g - BG.g) * mix);
      pixels[at + 2] = Math.round(BG.r + (FG.r - BG.r) * mix);
      pixels[at + 3] = Math.round(alpha * 255);
    }
  }

  return pixels;
}

/** ICO 안의 한 항목 — BITMAPINFOHEADER + XOR(32bpp, 아래에서 위) + AND 마스크. */
function dib(size, pixels) {
  const header = Buffer.alloc(40);
  header.writeUInt32LE(40, 0);            // biSize
  header.writeInt32LE(size, 4);           // biWidth
  header.writeInt32LE(size * 2, 8);       // biHeight — XOR 와 AND 를 합쳐 적는다
  header.writeUInt16LE(1, 12);            // biPlanes
  header.writeUInt16LE(32, 14);           // biBitCount
  header.writeUInt32LE(0, 16);            // biCompression = BI_RGB

  const xor = Buffer.alloc(size * size * 4);
  for (let y = 0; y < size; y++) {
    const from = (size - 1 - y) * size * 4;   // 아래에서 위로 뒤집는다
    pixels.copy(xor, y * size * 4, from, from + size * 4);
  }

  // 알파가 투명을 맡으므로 마스크는 전부 0 이다. 행은 4바이트로 맞춘다.
  const maskRow = Math.ceil(size / 32) * 4;
  const mask = Buffer.alloc(maskRow * size);

  header.writeUInt32LE(xor.length + mask.length, 20);   // biSizeImage
  return Buffer.concat([header, xor, mask]);
}

function main() {
  const images = SIZES.map(function (size) { return dib(size, render(size)); });

  const dir = Buffer.alloc(6);
  dir.writeUInt16LE(0, 0);                // reserved
  dir.writeUInt16LE(1, 2);                // type = icon
  dir.writeUInt16LE(SIZES.length, 4);

  const entries = Buffer.alloc(16 * SIZES.length);
  let offset = dir.length + entries.length;
  for (let i = 0; i < SIZES.length; i++) {
    const at = i * 16;
    entries[at] = SIZES[i] === 256 ? 0 : SIZES[i];      // 0 은 256 을 뜻한다
    entries[at + 1] = SIZES[i] === 256 ? 0 : SIZES[i];
    entries[at + 2] = 0;                                 // 색 수 — 32bpp 는 0
    entries[at + 3] = 0;                                 // reserved
    entries.writeUInt16LE(1, at + 4);                    // planes
    entries.writeUInt16LE(32, at + 6);                   // bitCount
    entries.writeUInt32LE(images[i].length, at + 8);
    entries.writeUInt32LE(offset, at + 12);
    offset += images[i].length;
  }

  fs.mkdirSync(path.dirname(OUT), { recursive: true });
  const ico = Buffer.concat([dir, entries].concat(images));
  fs.writeFileSync(OUT, ico);

  console.log('생성 ' + OUT.replace(/\\/g, '/'));
  console.log('크기 ' + SIZES.join('·') + ' · ' + Math.round(ico.length / 1024) + 'KB');
}

main();
