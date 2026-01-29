#!/usr/bin/env python3
"""
Refactors all dialog components to have their own folder structure.
Creates separate .html, .scss, and .ts files for each dialog component.
"""

import os
import re
from pathlib import Path

# Base directory for dialogs
DIALOGS_DIR = Path("/Users/maximilianmoriggl/Documents/GitHub/Rechnungsfreigabe/frontend/src/app/features/master-data/dialogs")

def extract_template_from_ts(content: str) -> str:
    """Extract template string from component decorator."""
    # Match template: `...` pattern
    match = re.search(r"template:\s*`([^`]*)`", content, re.DOTALL)
    if match:
        return match.group(1).strip()
    
    # If no template found, return empty string
    return ""

def extract_styles_from_ts(content: str) -> str:
    """Extract styles array from component decorator."""
    # Match styles: [`...`] pattern
    match = re.search(r"styles:\s*\[\s*`([^`]*)`\s*\]", content, re.DOTALL)
    if match:
        css_content = match.group(1).strip()
        # Remove leading CSS selector dots and clean up
        return css_content
    
    # If no styles found, return empty string
    return ""

def create_component_ts(original_content: str, has_template_file: bool = False, has_style_file: bool = False) -> str:
    """
    Create new component TS file with templateUrl and styleUrls instead of inline.
    """
    # Remove template and styles from the original content
    modified = re.sub(r",?\s*template:\s*`[^`]*`", "", original_content, flags=re.DOTALL)
    modified = re.sub(r",?\s*styles:\s*\[\s*`[^`]*`\s*\]", "", modified, flags=re.DOTALL)
    
    # Add templateUrl and styleUrls to the component decorator
    if has_template_file or has_style_file:
        # Find the component decorator
        decorator_match = re.search(r"@Component\(\{(.*?)\}\)", modified, re.DOTALL)
        if decorator_match:
            decorator_content = decorator_match.group(1)
            
            # Extract selector
            selector_match = re.search(r"selector:\s*['\"]([^'\"]*)['\"]", decorator_content)
            selector = selector_match.group(1) if selector_match else "app-dialog"
            
            # Build new decorator content
            new_decorator = f"@Component({{\n  selector: '{selector}',"
            
            if has_template_file:
                new_decorator += f"\n  templateUrl: './{selector}.component.html',"
            
            if has_style_file:
                new_decorator += f"\n  styleUrls: ['./{selector}.component.scss'],"
            
            # Add imports and other properties
            # Remove old selector line
            remaining = re.sub(r"\s*selector:\s*['\"][^'\"]*['\"],?", "", decorator_content)
            # Remove templateUrl/styleUrls if they exist
            remaining = re.sub(r"\s*templateUrl:\s*['\"][^'\"]*['\"],?", "", remaining)
            remaining = re.sub(r"\s*styleUrls:\s*\[\s*[^\]]*\],?", "", remaining)
            remaining = remaining.strip()
            if remaining.startswith(","):
                remaining = remaining[1:]
            
            new_decorator += f"\n  {remaining}\n}}"
            
            # Replace in modified content
            modified = modified.replace(decorator_match.group(0), new_decorator)
    
    return modified

def refactor_dialog_component(ts_file: Path):
    """Refactor a single dialog component."""
    component_name = ts_file.stem  # e.g., "approval-workflow-dialog.component"
    component_basename = component_name.replace(".component", "")  # e.g., "approval-workflow-dialog"
    
    print(f"\nProcessing: {ts_file.name}")
    
    # Check if already has separate files
    html_file = DIALOGS_DIR / f"{component_name}.html"
    scss_file = DIALOGS_DIR / f"{component_name}.scss"
    
    if html_file.exists() or scss_file.exists():
        print(f"  ✓ Already has separate files, skipping...")
        return
    
    # Read the TS file
    with open(ts_file, 'r', encoding='utf-8') as f:
        ts_content = f.read()
    
    # Extract template and styles
    template = extract_template_from_ts(ts_content)
    styles = extract_styles_from_ts(ts_content)
    
    if not template and not styles:
        print(f"  ✓ No inline template/styles found, already using files...")
        return
    
    # Create new folder
    component_folder = DIALOGS_DIR / component_basename
    component_folder.mkdir(exist_ok=True)
    
    # Create new TS file
    new_ts_content = create_component_ts(ts_content, bool(template), bool(styles))
    new_ts_file = component_folder / f"{component_basename}.component.ts"
    
    with open(new_ts_file, 'w', encoding='utf-8') as f:
        f.write(new_ts_content)
    print(f"  ✓ Created {new_ts_file.relative_to(DIALOGS_DIR.parent)}")
    
    # Create HTML file
    if template:
        new_html_file = component_folder / f"{component_basename}.component.html"
        with open(new_html_file, 'w', encoding='utf-8') as f:
            f.write(template)
        print(f"  ✓ Created {new_html_file.relative_to(DIALOGS_DIR.parent)}")
    
    # Create SCSS file
    if styles:
        new_scss_file = component_folder / f"{component_basename}.component.scss"
        with open(new_scss_file, 'w', encoding='utf-8') as f:
            f.write(styles)
        print(f"  ✓ Created {new_scss_file.relative_to(DIALOGS_DIR.parent)}")
    
    # Delete original TS file
    os.remove(ts_file)
    print(f"  ✓ Deleted original {ts_file.name}")

def main():
    print("Starting dialog component refactoring...")
    print(f"Dialogs directory: {DIALOGS_DIR}")
    
    # Find all .ts files that are dialog components
    ts_files = list(DIALOGS_DIR.glob("*.component.ts"))
    
    if not ts_files:
        print("No dialog components found!")
        return
    
    print(f"\nFound {len(ts_files)} dialog components")
    
    for ts_file in sorted(ts_files):
        try:
            refactor_dialog_component(ts_file)
        except Exception as e:
            print(f"  ✗ Error: {e}")
    
    print("\n✓ Refactoring complete!")

if __name__ == "__main__":
    main()
