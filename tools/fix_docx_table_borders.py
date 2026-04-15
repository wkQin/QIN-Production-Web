from pathlib import Path
import shutil

from docx import Document
from docx.oxml import OxmlElement
from docx.oxml.ns import qn


DOCX_PATH = Path("docs/ARBEITSANWEISUNG-WARENEINGANG.docx")
BACKUP_PATH = Path("docs/ARBEITSANWEISUNG-WARENEINGANG.backup-before-borders.docx")


def set_table_borders(table, outer_size="12", inner_size="8", color="444444"):
    tbl = table._tbl
    tbl_pr = tbl.tblPr

    tbl_borders = tbl_pr.first_child_found_in("w:tblBorders")
    if tbl_borders is None:
        tbl_borders = OxmlElement("w:tblBorders")
        tbl_pr.append(tbl_borders)

    border_specs = {
        "top": outer_size,
        "left": outer_size,
        "bottom": outer_size,
        "right": outer_size,
        "insideH": inner_size,
        "insideV": inner_size,
    }

    for edge, size in border_specs.items():
        element = tbl_borders.find(qn(f"w:{edge}"))
        if element is None:
            element = OxmlElement(f"w:{edge}")
            tbl_borders.append(element)
        element.set(qn("w:val"), "single")
        element.set(qn("w:sz"), size)
        element.set(qn("w:color"), color)
        element.set(qn("w:space"), "0")


def set_cell_borders(cell, outer_size="12", inner_size="8", color="444444"):
    tc = cell._tc
    tc_pr = tc.get_or_add_tcPr()

    tc_borders = tc_pr.first_child_found_in("w:tcBorders")
    if tc_borders is None:
        tc_borders = OxmlElement("w:tcBorders")
        tc_pr.append(tc_borders)

    for edge, size in {
        "top": inner_size,
        "left": inner_size,
        "bottom": inner_size,
        "right": inner_size,
    }.items():
        element = tc_borders.find(qn(f"w:{edge}"))
        if element is None:
            element = OxmlElement(f"w:{edge}")
            tc_borders.append(element)
        element.set(qn("w:val"), "single")
        element.set(qn("w:sz"), size)
        element.set(qn("w:color"), color)
        element.set(qn("w:space"), "0")


def main():
    if not DOCX_PATH.exists():
        raise FileNotFoundError(f"Datei nicht gefunden: {DOCX_PATH}")

    shutil.copy2(DOCX_PATH, BACKUP_PATH)

    doc = Document(DOCX_PATH)

    for table in doc.tables:
        set_table_borders(table)
        for row in table.rows:
            for cell in row.cells:
                set_cell_borders(cell)

    doc.save(DOCX_PATH)
    print(f"Backup erstellt: {BACKUP_PATH}")
    print(f"Tabellenrahmen aktualisiert: {DOCX_PATH}")


if __name__ == "__main__":
    main()
