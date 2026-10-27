#!/usr/bin/env python3
"""
test_analysis_pipeline.py
-------------------------
End-to-end integration test verifying:
1. Synthetic dataset generation
2. Schema validation against technical specification
3. Statistical analysis and report generation
"""

import os
import sys
import tempfile
import unittest

# Add current dir to path
sys.path.insert(0, os.path.dirname(__file__))

from generate_sample_data import generate_dataset
from schema_validator import validate_csv
from analyze_telemetry import load_telemetry, analyze, generate_markdown_report


class TestAnalysisPipeline(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.csv_path = os.path.join(self.temp_dir.name, "test_telemetry.csv")
        self.report_path = os.path.join(self.temp_dir.name, "test_report.md")

    def tearDown(self):
        self.temp_dir.cleanup()

    def test_pipeline_end_to_end(self):
        # 1. Generate data
        generate_dataset(num_participants=12, output_path=self.csv_path)
        self.assertTrue(os.path.exists(self.csv_path))

        # 2. Validate schema
        is_valid, errors, warnings = validate_csv(self.csv_path)
        self.assertTrue(is_valid, f"Schema validation failed: {errors}")
        self.assertEqual(len(errors), 0)

        # 3. Analyze data
        data = load_telemetry(self.csv_path)
        self.assertEqual(len(data), 36)  # 12 participants * 3 modes

        results = analyze(data)
        self.assertEqual(results["total_participants"], 12)
        self.assertEqual(results["total_trials"], 36)
        self.assertIn("Static", results["modes"])
        self.assertIn("DynamicArrow", results["modes"])
        self.assertIn("AI_HUD", results["modes"])

        # Check Chi-Square outputs
        self.assertIn("chi2", results["chi2_safe_path"])
        self.assertIn("p_value", results["chi2_safe_path"])

        # 4. Generate report
        generate_markdown_report(results, self.report_path)
        self.assertTrue(os.path.exists(self.report_path))
        with open(self.report_path, "r", encoding="utf-8") as f:
            content = f.read()
            self.assertIn("Smart-Evac: Empirical Telemetry Analysis Report", content)


if __name__ == "__main__":
    unittest.main()
