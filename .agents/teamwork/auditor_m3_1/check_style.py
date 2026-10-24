import os
import re

emoji_pattern = re.compile(r'[\U00010000-\U0010ffff\u2600-\u27ff\u2300-\u23ff\u2b50]')
em_dash_pattern = re.compile(r'[\u2014\u2013]|--')
oxford_comma_pattern = re.compile(r',\s+(?:and|or)\b', re.IGNORECASE)

directories_to_check = ['Assets', 'Assets/Resources/Catalogs']

def check_file(filepath):
    results = {'emojis': [], 'em_dashes': [], 'oxford_commas': []}
    with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
        for idx, line in enumerate(f, 1):
            # check emojis
            emojis = emoji_pattern.findall(line)
            if emojis:
                results['emojis'].append((idx, emojis, line.strip()))
            
            # check em-dash
            if '—' in line or '–' in line:
                results['em_dashes'].append((idx, line.strip()))
            
            # check oxford comma in text/json strings
            # Only check within strings (or json values)
            if filepath.endswith('.json') or filepath.endswith('.cs'):
                # find string literals
                strings = re.findall(r'"([^"\\]*(?:\\.[^"\\]*)*)"', line)
                for s in strings:
                    # check if s contains comma followed by and/or
                    ox = oxford_comma_pattern.findall(s)
                    if ox:
                        results['oxford_commas'].append((idx, ox, s))
    return results

print("=== NARRATIVE STYLE AUDIT ===")
total_issues = 0
for root, dirs, files in os.walk('Assets'):
    for file in files:
        if file.endswith(('.json', '.cs')):
            fp = os.path.join(root, file)
            res = check_file(fp)
            if res['emojis']:
                print(f"[EMOJI] {fp}: {res['emojis']}")
                total_issues += len(res['emojis'])
            if res['em_dashes']:
                print(f"[EM-DASH] {fp}: {res['em_dashes']}")
                total_issues += len(res['em_dashes'])
            if res['oxford_commas']:
                print(f"[OXFORD COMMA] {fp}: {res['oxford_commas']}")
                total_issues += len(res['oxford_commas'])

print(f"Total violations found: {total_issues}")
