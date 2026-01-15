#!/usr/bin/env python3

import xml.etree.ElementTree as ET
import os
import shutil
import subprocess
import glob
import sys

def run_tests_and_collect_coverage():
    # Remove TestResults folder if it exists (to clear previous results)
    test_results_path = "TestResults"
    if os.path.exists(test_results_path):
        print("Removing existing TestResults folder...")
        shutil.rmtree(test_results_path)

    # Run dotnet test with coverage collection
    print("Running tests and collecting coverage...")
    result = subprocess.run(["dotnet", "test", "--collect:XPlat Code Coverage"],
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)

    if result.returncode != 0:
        print(f"Error running tests: {result.stderr}")
        sys.exit(1)

    # Find the XML coverage file in the TestResults folder
    xml_files = glob.glob("TestResults/**/coverage.cobertura.xml", recursive=True)

    if not xml_files:
        print("No coverage XML file found in TestResults folder.")
        sys.exit(1)

    coverage_file = xml_files[0]
    print(f"Found coverage file: {coverage_file}")

    return coverage_file

def parse_coverage_xml(file_path):
    # Parse the XML file
    tree = ET.parse(file_path)
    root = tree.getroot()
    
    # Find all class elements
    classes = root.findall('.//class')
    
    # Extract class information
    results = []
    
    for cls in classes:
        class_name = cls.get('name')
        line_rate = float(cls.get('line-rate'))
        branch_rate = float(cls.get('branch-rate'))
        
        # Only include classes with non-zero line and branch rates
        if line_rate > 0 and branch_rate > 0:
            results.append((class_name, line_rate, branch_rate))
    
    # Calculate averages only for classes with non-zero line and branch rates
    if results:
        avg_line_rate = sum([item[1] for item in results]) / len(results)
        avg_branch_rate = sum([item[2] for item in results]) / len(results)
    else:
        avg_line_rate = 0
        avg_branch_rate = 0
    
    return results, avg_line_rate, avg_branch_rate

def main():
    # Run tests and collect coverage
    coverage_file = run_tests_and_collect_coverage()
    
    # Parse the coverage XML
    results, avg_line_rate, avg_branch_rate = parse_coverage_xml(coverage_file)
    
    # Write the results to a markdown file
    with open("TestsCoverage.md", "w") as f:
        f.write("# Test Coverage Report\n\n")
        f.write(f"Average Line Rate (over non-zero classes): {avg_line_rate:.4f}\n")
        f.write(f"Average Branch Rate (over non-zero classes): {avg_branch_rate:.4f}\n\n")
        f.write("| ClassName | Line Rate | Branch Rate |\n")
        f.write("|-----------|-----------|-------------|\n")
        
        for class_name, line_rate, branch_rate in results:
            f.write(f"| {class_name} | {line_rate:.4f} | {branch_rate:.4f} |\n")
    
    print(f"Found {len(results)} classes with non-zero line and branch rates.")
    print(f"Average line rate (over non-zero classes): {avg_line_rate:.4f}")
    print(f"Average branch rate (over non-zero classes): {avg_branch_rate:.4f}")
    print("Report written to TestsCoverage.md")

if __name__ == "__main__":
    main()