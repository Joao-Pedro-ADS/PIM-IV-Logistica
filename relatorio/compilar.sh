#!/usr/bin/env bash
# Compila o relatório: pdflatex -> bibtex -> pdflatex x2
cd "$(dirname "$0")"
pdflatex -interaction=nonstopmode 0_main.tex > /dev/null 2>&1
bibtex 0_main > /dev/null 2>&1
pdflatex -interaction=nonstopmode 0_main.tex > /dev/null 2>&1
pdflatex -interaction=nonstopmode 0_main.tex > /dev/null 2>&1
grep -a -E "^!" 0_main.log | sort | uniq -c
grep -a -E "LaTeX Warning: (Reference|Citation)" 0_main.log | head
pdfinfo 0_main.pdf | grep Pages
