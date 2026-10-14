#!/usr/bin/env python3
"""Extract the slice's bright-star catalog from the HYG database.

Pitch slice §12 (Sky): "Real star catalog (brightest ~2,000 stars) positioned
for Cusco, 16 Nov 1532, 22:00". This script keeps the N brightest stars with
the fields the runtime needs; positions for 1532 are computed at load time by
Pachacuti.Core.Astronomy (proper motion + precession from J2000).

Source: HYG database v4.1, https://github.com/astronexus/HYG-Database
License of the extracted data: CC BY-SA 4.0 (see the LICENSE file written next
to the output).

Usage: python3 tools/build_star_catalog.py <hygdata_v41.csv> [count]
Output columns (J2000, epoch 2000.0):
  ra_hours, dec_deg, pm_ra_mas_yr (includes cos dec), pm_dec_mas_yr, mag, bv, name
"""
import csv
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(ROOT, "Assets", "Pachacuti", "Resources", "Pachacuti", "Sky")

LICENSE = """Star data in bright_stars.csv is extracted from the HYG Database v4.1
by David Nash / astronexus (https://github.com/astronexus/HYG-Database),
licensed under the Creative Commons Attribution-ShareAlike 4.0 International
License (http://creativecommons.org/licenses/by-sa/4.0/).

The extraction (brightest stars, selected columns) is produced by
tools/build_star_catalog.py and is distributed under the same license.
"""


def fnum(s, default=0.0):
    try:
        return float(s)
    except (TypeError, ValueError):
        return default


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 1
    count = int(sys.argv[2]) if len(sys.argv) > 2 else 2000
    rows = []
    with open(sys.argv[1], newline="") as f:
        for r in csv.DictReader(f):
            if r["proper"] == "Sol":
                continue
            mag = fnum(r["mag"], 99.0)
            rows.append((mag, r))
    rows.sort(key=lambda x: x[0])
    rows = rows[:count]
    os.makedirs(OUT_DIR, exist_ok=True)
    out_path = os.path.join(OUT_DIR, "bright_stars.csv")
    with open(out_path, "w", newline="\n") as f:
        f.write("# ra_h,dec_deg,pmra_mas_yr,pmdec_mas_yr,mag,bv,name  (HYG v4.1, CC BY-SA 4.0)\n")
        for mag, r in rows:
            name = r["proper"] or ((r["bayer"] + " " + r["con"]).strip() if r["bayer"] else "")
            name = name.replace(",", " ")
            f.write("%.6f,%.5f,%.2f,%.2f,%.2f,%.3f,%s\n" % (
                fnum(r["ra"]), fnum(r["dec"]), fnum(r["pmra"]), fnum(r["pmdec"]), mag, fnum(r["ci"], 0.6), name))
    with open(os.path.join(OUT_DIR, "bright_stars_LICENSE.txt"), "w", newline="\n") as f:
        f.write(LICENSE)
    print("wrote %d stars (faintest mag %.2f) to %s" % (len(rows), rows[-1][0], os.path.relpath(out_path, ROOT)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
