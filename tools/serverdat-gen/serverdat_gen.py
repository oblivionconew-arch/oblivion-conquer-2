#!/usr/bin/env python3
"""
Conquer Online 2.0 Server.dat Generator & Legacy DatCryptor (Python 3, zero external deps)
Supports:
  - legacy-dat : TQ DatCryptor 0x2537 128-byte PRNG+rotate cipher (classic CO 2.0 server.dat / .dat)
  - xml        : Flash Login XML schema (CO 2.0 v5517-v6022+ dlgloginflash.cpp)
"""
import argparse
import xml.sax.saxutils as saxutils

TQ_LEGACY_KEY = bytes([
    0xAD, 0x6B, 0x4F, 0xFB, 0xDD, 0xB8, 0x0E, 0x09, 0x13, 0x33, 0x8F, 0xF5, 0x43, 0x09, 0x15, 0x88,
    0x5D, 0x80, 0xA3, 0x45, 0x2D, 0x42, 0x08, 0x56, 0x80, 0xF8, 0x19, 0xC5, 0x88, 0x1B, 0x3E, 0xEF,
    0x81, 0x07, 0x30, 0x36, 0x95, 0x52, 0x00, 0xF7, 0xFD, 0x5B, 0x5C, 0xBC, 0x6A, 0x26, 0x0E, 0xB2,
    0xA3, 0x67, 0xC5, 0x5D, 0x6F, 0xDC, 0x18, 0x8A, 0xB5, 0xE0, 0xC8, 0x85, 0xE2, 0x3E, 0x45, 0x8D,
    0x8B, 0x43, 0x74, 0x85, 0x54, 0x17, 0xB0, 0xEC, 0x10, 0x4D, 0x0F, 0x0F, 0x29, 0xB8, 0xE6, 0x7D,
    0x42, 0x80, 0x8F, 0xBC, 0x1C, 0x76, 0x69, 0x3A, 0xB6, 0xA5, 0x21, 0x86, 0xB9, 0x29, 0x30, 0xC0,
    0x12, 0x45, 0xA5, 0x4F, 0xE1, 0xAF, 0x25, 0xD1, 0x92, 0x2E, 0x30, 0x58, 0x49, 0x67, 0xA5, 0xD3,
    0x84, 0xF4, 0x89, 0xCA, 0xFC, 0xB7, 0x04, 0x4F, 0xCC, 0x6E, 0xAC, 0x31, 0xD4, 0x87, 0x07, 0x72,
])


def encrypt_legacy(data: bytes) -> bytes:
    out = bytearray(len(data))
    for i, b in enumerate(data):
        r = i % 8
        rot = ((b >> (8 - r)) | (b << r)) & 0xFF
        out[i] = rot ^ TQ_LEGACY_KEY[(i & 0xFF) % 128]
    return bytes(out)


def decrypt_legacy(data: bytes) -> bytes:
    out = bytearray(len(data))
    for i, c in enumerate(data):
        b = c ^ TQ_LEGACY_KEY[(i & 0xFF) % 128]
        r = i % 8
        out[i] = ((b << (8 - r)) | (b >> r)) & 0xFF
    return bytes(out)


def build_flash_xml(group: str, name: str, ip: str, port: int, icon: str, hint: str) -> str:
    return (
        '<?xml version="1.0" encoding="utf-8"?>\n'
        "<ServerList>\n"
        f'  <Group name="{saxutils.escape(group)}" id="1">\n'
        "    <Server>\n"
        "      <field name=\"id\">101</field>\n"
        f"      <field name=\"ServerName\">{saxutils.escape(name)}</field>\n"
        f"      <field name=\"ServerIP\">{saxutils.escape(ip)}</field>\n"
        f"      <field name=\"ServerPort\">{port}</field>\n"
        f"      <field name=\"FlashName\">{saxutils.escape(name)}</field>\n"
        f"      <field name=\"FlashIcon\">{saxutils.escape(icon)}</field>\n"
        f"      <field name=\"FlashHint\">{saxutils.escape(hint)}</field>\n"
        "      <field name=\"Child\">0</field>\n"
        "    </Server>\n"
        "  </Group>\n"
        "</ServerList>\n"
    )


def build_legacy_ini(name: str, ip: str, port: int, icon: str, hint: str) -> str:
    return (
        "[Header]\r\n"
        "GroupAmount=1\r\n"
        "Group1=Group1\r\n\r\n"
        "[Group1]\r\n"
        "ServerAmount=1\r\n"
        f"Server1={name}\r\n"
        f"Ip1={ip}\r\n"
        f"Port1={port}\r\n"
        f"ServerName1={name}\r\n"
        f"HintWord1={hint}\r\n"
        "Pic1=Group3/server8.ani\r\n"
        f"FlashName1={name}\r\n"
        f"FlashIcon1={icon}\r\n"
    )


def main():
    p = argparse.ArgumentParser(description="Conquer 2.0 Server.dat generator")
    p.add_argument("--mode", choices=["legacy-dat", "xml"], default="legacy-dat")
    p.add_argument("--group", default="Conquer 2.0")
    p.add_argument("--name", default="Oblivion")
    p.add_argument("--ip", default="127.0.0.1")
    p.add_argument("--port", type=int, default=9958)
    p.add_argument("--icon", default="Group3/server8.swf")
    p.add_argument("--hint", default="")
    p.add_argument("--out", default="server.dat")
    p.add_argument("--decrypt", help="Decrypt a legacy .dat file to --out")
    args = p.parse_args()

    if args.decrypt:
        with open(args.decrypt, "rb") as f:
            dec = decrypt_legacy(f.read())
        with open(args.out, "wb") as f:
            f.write(dec)
        print(f"Decrypted {args.decrypt} -> {args.out} ({len(dec)} bytes)")
        return

    if args.mode == "xml":
        payload = build_flash_xml(args.group, args.name, args.ip, args.port, args.icon, args.hint).encode("utf-8")
    else:
        payload = encrypt_legacy(build_legacy_ini(args.name, args.ip, args.port, args.icon, args.hint).encode("ascii"))

    with open(args.out, "wb") as f:
        f.write(payload)
    print(f"Generated {args.mode} server.dat -> {args.out} ({len(payload)} bytes)")


if __name__ == "__main__":
    main()
