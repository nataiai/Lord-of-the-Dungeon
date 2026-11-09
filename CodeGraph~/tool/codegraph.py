#!/usr/bin/env python3
"""CodeGraph launcher: python CodeGraph~/tool/codegraph.py build | diff | notes | check"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from codegraph.cli import main  # noqa: E402

if __name__ == "__main__":
    main()
