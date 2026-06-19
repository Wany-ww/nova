import os
import re
import json
from datetime import datetime

class StaticAnalyzer:
    def __init__(self, root_dir):
        self.root_dir = root_dir
        self.issues = []
        self.files_metrics = []
        self.stats = {
            "total_files": 0,
            "cs_files": 0,
            "lua_files": 0,
            "lines_of_code": 0,
            "code_lines": 0,
            "comment_lines": 0,
            "blank_lines": 0,
            "total_cc": 0,
            "avg_cc": 0.0,
            "critical_issues": 0,
            "warning_issues": 0,
            "info_issues": 0
        }
        self.scores = {
            "analysis_score": 100.0,
            "complexity_score": 100.0,
            "scale_score": 100.0,
            "documentation_score": 100.0,
            "overall_score": 100.0,
            "overall_grade": "A+"
        }

    def run(self):
        for root, dirs, files in os.walk(self.root_dir):
            # Skip bin and obj directories
            if "bin" in root.split(os.sep) or "obj" in root.split(os.sep):
                continue
            
            for file in files:
                filepath = os.path.join(root, file)
                rel_path = os.path.relpath(filepath, self.root_dir).replace("\\", "/")
                
                if file.endswith(".cs"):
                    self.stats["total_files"] += 1
                    self.stats["cs_files"] += 1
                    self.analyze_file(filepath, rel_path, "C#")
                elif file.endswith(".lua"):
                    self.stats["total_files"] += 1
                    self.stats["lua_files"] += 1
                    self.analyze_file(filepath, rel_path, "Lua")

        self.stats["critical_issues"] = len([i for i in self.issues if i["severity"] == "CRITICAL"])
        self.stats["warning_issues"] = len([i for i in self.issues if i["severity"] == "WARNING"])
        self.stats["info_issues"] = len([i for i in self.issues if i["severity"] == "INFO"])
        
        if self.stats["total_files"] > 0:
            self.stats["avg_cc"] = round(self.stats["total_cc"] / self.stats["total_files"], 1)
        
        self.calculate_scores()

    def calculate_scores(self):
        # 1. 정밀 분석 점수 (Analysis Score)
        crit_count = self.stats["critical_issues"]
        warn_count = self.stats["warning_issues"]
        info_count = self.stats["info_issues"]
        
        crit_deductions = min(70.0, crit_count * 15.0)
        warn_deductions = min(50.0, warn_count * 5.0)
        info_deductions = min(10.0, info_count * 0.1) # 0.1 per info issue, max 10 points (fair deduction for code smells)
        
        self.scores["analysis_score"] = round(max(0.0, 100.0 - (crit_deductions + warn_deductions + info_deductions)), 1)
        
        # 2. 복잡도 관리 점수 (Complexity Score)
        # Average of file scores based on CC rating
        file_scores = []
        for m in self.files_metrics:
            if m["rating"] == "Very High":
                file_scores.append(60.0)
            elif m["rating"] == "High":
                file_scores.append(80.0)
            elif m["rating"] == "Moderate":
                file_scores.append(95.0)
            else: # Low
                file_scores.append(100.0)
        
        if file_scores:
            self.scores["complexity_score"] = round(sum(file_scores) / len(file_scores), 1)
        else:
            self.scores["complexity_score"] = 100.0

        
        # 3. 가독성 및 규모 점수 (Scale Score)
        # Average of file sizing scores, minus nesting penalties
        sizing_scores = []
        for m in self.files_metrics:
            if m["loc"] > 1500:
                sizing_scores.append(80.0)
            elif m["loc"] > 1000:
                sizing_scores.append(90.0)
            elif m["loc"] > 800:
                sizing_scores.append(97.0)
            else:
                sizing_scores.append(100.0)
                
        base_scale = sum(sizing_scores) / len(sizing_scores) if sizing_scores else 100.0
        nesting_issues = len([i for i in self.issues if i["title"] == "Deep Control Flow Nesting"])
        nesting_penalty = min(5.0, nesting_issues * 0.1) # -0.1 per nesting issue, max 5 pts (nesting is already evaluated in CC)
        
        self.scores["scale_score"] = round(max(0.0, base_scale - nesting_penalty), 1)


        
        # 4. 주석 및 문서화 점수 (Documentation Score)
        code_lines = self.stats["code_lines"]
        comment_lines = self.stats["comment_lines"]
        total_lines = code_lines + comment_lines
        
        if total_lines > 0:
            comment_ratio = comment_lines / total_lines
        else:
            comment_ratio = 0.0
            
        if comment_ratio >= 0.10: # 10%+ density is excellent for dynamic codebases
            doc_score = 95.0
        elif comment_ratio >= 0.05:
            doc_score = 85.0
        elif comment_ratio >= 0.02:
            doc_score = 75.0
        else:
            doc_score = 50.0
            
        self.scores["documentation_score"] = doc_score
        
        # 5. 종합 코드 품질 점수 (Overall Score)
        w_analysis = 0.35
        w_complexity = 0.30
        w_scale = 0.20
        w_doc = 0.15
        
        overall = (
            (self.scores["analysis_score"] * w_analysis) +
            (self.scores["complexity_score"] * w_complexity) +
            (self.scores["scale_score"] * w_scale) +
            (self.scores["documentation_score"] * w_doc)
        )
        self.scores["overall_score"] = round(overall, 1)
        
        # 6. 종합 등급 (Overall Grade)
        score = self.scores["overall_score"]
        if score >= 95:
            grade = "A+"
        elif score >= 90:
            grade = "A"
        elif score >= 80:
            grade = "B"
        elif score >= 70:
            grade = "C"
        elif score >= 60:
            grade = "D"
        else:
            grade = "F"
        self.scores["overall_grade"] = grade


    def calculate_metrics(self, content, file_type):
        lines = content.splitlines()
        total_lines = len(lines)
        
        # Calculate Code Lines (excluding blank lines and comment-only lines)
        code_lines = 0
        comment_lines = 0
        blank_lines = 0
        for line in lines:
            trimmed = line.strip()
            if not trimmed:
                blank_lines += 1
                continue
            if file_type == "C#":
                if trimmed.startswith("//") or trimmed.startswith("/*") or trimmed.startswith("*"):
                    comment_lines += 1
                    continue
            elif file_type == "Lua":
                if trimmed.startswith("--"):
                    comment_lines += 1
                    continue
            code_lines += 1

        # Calculate Cyclomatic Complexity (CC)
        keywords = [
            r'\bif\b',
            r'\belseif\b', # Lua
            r'\belse\s+if\b', # C#
            r'\bwhile\b',
            r'\bfor\b',
            r'\bforeach\b',
            r'\bcatch\b',
            r'\bcase\b',
            r'&&',
            r'\|\|',
            r'\band\b', # Lua
            r'\bor\b', # Lua
            r'\b\?\b', # ternary
            r'\b\?\?\b' # null coalescing
        ]
        
        cc = 1
        for kw in keywords:
            cc += len(re.findall(kw, content))
            
        return total_lines, code_lines, comment_lines, blank_lines, cc

    def add_issue(self, filepath, line_num, category, title, description, severity, code_snippet=None):
        self.issues.append({
            "file": filepath,
            "line": line_num,
            "category": category,
            "title": title,
            "description": description,
            "severity": severity,
            "code_snippet": code_snippet
        })

    def analyze_file(self, filepath, rel_path, file_type):
        with open(filepath, "r", encoding="utf-8") as f:
            content = f.read()

        total_lines, code_lines, comment_lines, blank_lines, cc = self.calculate_metrics(content, file_type)
        
        self.stats["lines_of_code"] += total_lines
        self.stats["code_lines"] += code_lines
        self.stats["comment_lines"] += comment_lines
        self.stats["blank_lines"] += blank_lines
        self.stats["total_cc"] += cc

        # Rating based on CC (using industry-standard thresholds)
        if cc <= 50:
            rating = "Low"
            rating_class = "rating-low"
        elif cc <= 200:
            rating = "Moderate"
            rating_class = "rating-moderate"
        elif cc <= 1000:
            rating = "High"
            rating_class = "rating-high"
        else:
            rating = "Very High"
            rating_class = "rating-very-high"

        # Rating based on LOC (using standard thresholds matching our score system)
        if total_lines <= 800:
            loc_rating = "Low"
            loc_rating_class = "rating-low"
        elif total_lines <= 1000:
            loc_rating = "Moderate"
            loc_rating_class = "rating-moderate"
        elif total_lines <= 1500:
            loc_rating = "High"
            loc_rating_class = "rating-high"
        else:
            loc_rating = "Very High"
            loc_rating_class = "rating-very-high"

        self.files_metrics.append({
            "file": rel_path,
            "type": file_type,
            "loc": total_lines,
            "code_lines": code_lines,
            "cc": cc,
            "rating": rating,
            "rating_class": rating_class,
            "loc_rating": loc_rating,
            "loc_rating_class": loc_rating_class
        })

        lines = content.splitlines()

        if file_type == "C#":
            self.analyze_cs_rules(lines, content, rel_path)
        elif file_type == "Lua":
            self.analyze_lua_rules(lines, content, rel_path)

    def analyze_cs_rules(self, lines, content, rel_path):
        # 1. Event Subscription Leak Analysis
        event_subs = []
        for idx, line in enumerate(lines):
            match = re.search(r"([\w.]+)\s*\+=\s*(\w+);", line)
            if match:
                event_name, handler_name = match.groups()
                event_subs.append((idx + 1, line.strip(), event_name, handler_name))

        for line_num, line_text, event_name, handler_name in event_subs:
            unsub_pattern = rf"{re.escape(event_name)}\s*-=\s*{re.escape(handler_name)}"
            if not re.search(unsub_pattern, content):
                severity = "WARNING" if "MainWindow" in rel_path else "CRITICAL"
                self.add_issue(
                    rel_path,
                    line_num,
                    "Memory Leak",
                    "Dangling Event Subscription",
                    f"Event registration '{event_name} += {handler_name}' has no matching unsubscription (-=) in this file. This can prevent garbage collection of the subscribing instance.",
                    severity,
                    line_text
                )

        # 2. Memory Leaks: Undisposed IDisposable (Mat, MatWrapper, JsonDocument)
        for idx, line in enumerate(lines):
            if "OpenCvLuaApi.cs" in rel_path or "MatWrapper.cs" in rel_path:
                continue

            alloc_match = re.search(r"\b(new\s+(Mat|MatWrapper|JsonDocument))\b", line)
            if alloc_match:
                is_using = "using" in line or (idx > 0 and "using" in lines[idx - 1])
                
                var_name_match = re.search(r"(?:var|Mat|MatWrapper|JsonDocument)\s+(\w+)\s*=", line)
                is_disposed = False
                if var_name_match:
                    var_name = var_name_match.group(1)
                    end_scan = min(idx + 35, len(lines))
                    for scan_idx in range(idx + 1, end_scan):
                        scan_line = lines[scan_idx]
                        if f"{var_name}.Dispose()" in scan_line or f"{var_name}?.Dispose()" in scan_line or f"{var_name}.release()" in scan_line:
                            is_disposed = True
                            break
                
                if not is_using and not is_disposed:
                    self.add_issue(
                        rel_path,
                        idx + 1,
                        "Memory Leak",
                        "Undisposed IDisposable Object",
                        "Allocating a class implementing IDisposable (like Mat or MatWrapper) without a 'using' statement or calling '.Dispose()' leaks unmanaged native memory.",
                        "CRITICAL",
                        line.strip()
                    )

        # 3. Concurrency: Unsynchronized Shared Static Variable Writes
        static_fields = []
        for idx, line in enumerate(lines):
            field_match = re.search(r"\bstatic\s+([\w<>?]+)\s+(\w+)\s*[=;]", line)
            if field_match and "readonly" not in line and "(" not in line:
                field_type, field_name = field_match.groups()
                static_fields.append((field_name, line.strip()))

        for field_name, decl_line in static_fields:
            if "lock" in field_name.lower() or "lock" in decl_line.lower():
                continue
            
            write_pattern = rf"\b{field_name}\s*=[^=]"
            for idx, line in enumerate(lines):
                if re.search(write_pattern, line) and "static" not in line:
                    has_lock = False
                    start_scan = max(0, idx - 15)
                    for scan_idx in range(start_scan, idx):
                        if "lock (" in lines[scan_idx] or "lock(" in lines[scan_idx]:
                            has_lock = True
                            break
                    
                    if not has_lock:
                        self.add_issue(
                            rel_path,
                            idx + 1,
                            "Concurrency",
                            "Unsynchronized Write to Shared Static Field",
                            f"Shared static field '{field_name}' is modified without locking. Under concurrent executions (e.g. multiple threads running Lua scripts), this can lead to race conditions.",
                            "WARNING",
                            line.strip()
                        )

        # 4. Concurrency: UI Thread Safety in Callbacks
        ui_controls = ["Background", "BorderThickness", "Children", "Parent", "Grid.SetRow", "Grid.SetColumn", "Width", "Height"]
        for idx, line in enumerate(lines):
            has_ui_mod = any(control in line for control in ui_controls)
            if has_ui_mod and "=" in line:
                has_dispatcher = False
                start_scan = max(0, idx - 25)
                for scan_idx in range(start_scan, idx):
                    scan_line = lines[scan_idx]
                    if "Dispatcher.Invoke" in scan_line or "Dispatcher.BeginInvoke" in scan_line or "Dispatcher.InvokeAsync" in scan_line:
                        has_dispatcher = True
                        break
                
                is_inside_callback = False
                for scan_idx in range(max(0, idx - 50), idx):
                    scan_line = lines[scan_idx]
                    if "OnWebMessageReceived" in scan_line or "RunFlow" in scan_line or "Task.Run" in scan_line or "Thread" in scan_line:
                        is_inside_callback = True
                        break

                if is_inside_callback and not has_dispatcher:
                    self.add_issue(
                        rel_path,
                        idx + 1,
                        "Concurrency",
                        "UI Control Modified from Non-UI Thread Callback",
                        "UI controls are updated from a potential worker thread context without Dispatcher synchronization. This will trigger a InvalidOperationException at runtime.",
                        "CRITICAL",
                        line.strip()
                    )

        # 5. Exception Handling: Swallowed Exceptions
        for idx, line in enumerate(lines):
            if "catch" in line:
                end_scan = min(idx + 5, len(lines))
                catch_block = "".join(lines[idx:end_scan])
                
                is_swallowed = False
                clean_block = re.sub(r"\s+", "", catch_block)
                if "{}" in clean_block or "{\n}" in catch_block:
                    is_swallowed = True
                elif "Console.Write" in catch_block or "Debug.Write" in catch_block:
                    if "throw" not in catch_block and "logCallback" not in catch_block and "MessageBox" not in catch_block:
                        is_swallowed = True
                
                if is_swallowed:
                    self.add_issue(
                        rel_path,
                        idx + 1,
                        "Exception Handling",
                        "Swallowed Exception",
                        "Exceptions are caught but swallowed or only printed to debug output, without rethrowing or informing the log callback. This hides runtime failures and makes troubleshooting difficult.",
                        "WARNING",
                        line.strip()
                    )

        # 6. Code Smell: Magic Numbers Check (excluding UI styling layout values)
        layout_ignores = ["thickness", "cornerradius", "color.from", "width", "height", "fontsize", "margin", "padding", "opacity", "canvas.set", "grid.set", "scrollviewer"]
        for idx, line in enumerate(lines):
            if "const" in line or "readonly" in line or "static" in line or "private" in line or "public" in line:
                continue
            line_lower = line.lower()
            if any(ig in line_lower for ig in layout_ignores):
                continue
            for num_match in re.finditer(r"\b\d+(\.\d+)?[fF]?\b", line):
                num_str = num_match.group(0).lower().rstrip('f')
                try:
                    num_val = float(num_str)
                    if num_val not in [0.0, 1.0, 2.0, -1.0, 10.0, 100.0, 180.0, 360.0, -999.0, 3.0, 5.0]:
                        if re.search(r"(=|==|!=|>|<|>=|<=|\+|-|\*|/)\s*" + re.escape(num_match.group(0)), line) or \
                           re.search(re.escape(num_match.group(0)) + r"\s*(\)|,|;|\+|-|\*|/|==|!=|>|<|>=|<=)", line):
                            if "Thread.Sleep" not in line and "Task.Delay" not in line and "GC.GetTotalMemory" not in line:
                                self.add_issue(
                                    rel_path,
                                    idx + 1,
                                    "Code Smell",
                                    "Magic Number",
                                    f"Magic number literal '{num_match.group(0)}' used in calculation or comparison. Consider extracting to a named constant or config variable.",
                                    "INFO",
                                    line.strip()
                                )
                                break
                except:
                    pass

        # 7. Code Smell: Deep Control Flow Nesting Check (level >= 6 corresponding to 3+ nested blocks inside method)
        for idx, line in enumerate(lines):
            trimmed = line.strip()
            if not trimmed:
                continue
            leading_space_count = len(line) - len(line.lstrip(' '))
            leading_tab_count = len(line) - len(line.lstrip('\t'))
            nesting_level = (leading_space_count // 4) + leading_tab_count
            
            if nesting_level >= 6:
                control_match = re.search(r"\b(if|for|foreach|while|switch)\b", trimmed)
                if control_match:
                    self.add_issue(
                        rel_path,
                        idx + 1,
                        "Code Smell",
                        "Deep Control Flow Nesting",
                        f"Control structure '{control_match.group(1)}' is nested {nesting_level} levels deep. Consider refactoring into smaller helper methods to improve readability.",
                        "INFO",
                        trimmed
                    )


    def analyze_lua_rules(self, lines, content, rel_path):
        allocated_vars = []
        for idx, line in enumerate(lines):
            match = re.search(r"(\w+)\s*=\s*cv\.(imread|cvtColor|threshold|Canny|resize|Mat)\(", line)
            if match:
                var_name = match.group(1)
                allocated_vars.append((idx + 1, line.strip(), var_name))

        for line_num, line_text, var_name in allocated_vars:
            release_pattern = rf"\b{var_name}:release\s*\("
            return_pattern = rf"\breturn\b.*?\b{var_name}\b"
            if not re.search(release_pattern, content) and not re.search(return_pattern, content):
                self.add_issue(
                    rel_path,
                    line_num,
                    "Memory Leak (Lua)",
                    "Potential Undisposed OpenCV Mat in Lua",
                    f"Lua variable '{var_name}' holds an OpenCV MatWrapper but ':release()' is never called on it. Lua GC is lazy, which can cause high native unmanaged memory overhead.",
                    "WARNING",
                    line_text
                )

        # 2. Lua Code Smell: Implicit Global Variable in Function Check
        in_function = False
        func_params = set()
        local_vars = set()
        for idx, line in enumerate(lines):
            trimmed = line.strip()
            
            # Start of function
            func_match = re.search(r"\bfunction\s+\w+\s*\(([^)]*)\)", trimmed)
            if func_match:
                in_function = True
                params = [p.split(":")[0].strip() for p in func_match.group(1).split(",") if p.strip()]
                func_params = set(params)
                local_vars = set()
                continue
            
            if in_function:
                # End of function
                if trimmed == "end":
                    in_function = False
                    continue
                
                # Check local variable declarations
                local_match = re.search(r"\blocal\s+([\w\s,]+)\b", trimmed)
                if local_match:
                    vars_declared = [v.strip() for v in local_match.group(1).split(",") if v.strip()]
                    for v in vars_declared:
                        local_vars.add(v)
                
                # Check assignments: varname = ...
                assign_match = re.search(r"\b([a-zA-Z_]\w*)\s*=[^=]", trimmed)
                if assign_match:
                    var_name = assign_match.group(1)
                    # Exclude typical globals or built-ins
                    lua_globals = {"cv", "log", "console", "time", "math", "tostring", "tonumber", "type", "print", "pairs", "ipairs", "table"}
                    if var_name not in func_params and var_name not in local_vars and var_name not in lua_globals:
                        # Check if it was already declared local in another line
                        if not re.search(rf"\blocal\s+{var_name}\b", content):
                            self.add_issue(
                                rel_path,
                                idx + 1,
                                "Code Smell",
                                "Implicit Global Variable in Function",
                                f"Lua variable '{var_name}' is assigned inside a function without the 'local' keyword. In Lua, this implicitly creates or overwrites a global variable.",
                                "WARNING",
                                trimmed
                            )

        # 3. Lua Code Smell: Use of Standard Print Check
        for idx, line in enumerate(lines):
            trimmed = line.strip()
            if trimmed.startswith("--"):
                continue
            if re.search(r"\bprint\s*\(", line):
                self.add_issue(
                    rel_path,
                    idx + 1,
                    "Code Smell",
                    "Use of Standard Print in Node",
                    "Using standard 'print()' in Lua nodes is a code smell. Consider using the structured logging API (e.g. 'log.info()', 'log.warn()', 'log.error()') to specify clean message severity.",
                    "INFO",
                    trimmed
                )

    def generate_html_report(self, output_path):
        issues_json = json.dumps(self.issues, indent=2)
        metrics_json = json.dumps(self.files_metrics, indent=2)
        stats_json = json.dumps(self.stats, indent=2)
        scores_json = json.dumps(self.scores, indent=2)

        # HTML and CSS for a gorgeous modern dashboard
        html_content = f"""<!DOCTYPE html>
<html lang="ko">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>NOVA Static Analysis Dashboard</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&family=Outfit:wght@400;500;600;700;800&family=Fira+Code:wght@400;500&display=swap" rel="stylesheet">
    <style>
        :root {{
            --bg-color: #07080e;
            --card-bg: rgba(18, 20, 38, 0.6);
            --card-border: rgba(255, 255, 255, 0.08);
            --text-color: #f3f4f6;
            --text-muted: #9ca3af;
            --primary: #6366f1;
            --primary-glow: rgba(99, 102, 241, 0.35);
            --critical: #ef4444;
            --critical-glow: rgba(239, 68, 68, 0.3);
            --warning: #fbbf24;
            --warning-glow: rgba(251, 191, 36, 0.3);
            --info: #3b82f6;
            --info-glow: rgba(59, 130, 246, 0.3);
            --success: #10b981;
            --success-glow: rgba(16, 185, 129, 0.3);
            
            --grade-a: #10b981;
            --grade-b: #3b82f6;
            --grade-c: #fbbf24;
            --grade-d: #ed64a6;
            --grade-f: #ef4444;
        }}

        * {{
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }}

        body {{
            font-family: 'Inter', sans-serif;
            background-color: var(--bg-color);
            color: var(--text-color);
            line-height: 1.6;
            padding: 2rem;
            background-image: 
                radial-gradient(at 0% 0%, rgba(99, 102, 241, 0.12) 0px, transparent 50%),
                radial-gradient(at 100% 0%, rgba(239, 68, 68, 0.08) 0px, transparent 50%),
                radial-gradient(at 50% 100%, rgba(16, 185, 129, 0.05) 0px, transparent 50%);
            background-attachment: fixed;
        }}

        .glass-panel {{
            background: var(--card-bg);
            border: 1px solid var(--card-border);
            border-radius: 16px;
            backdrop-filter: blur(12px);
            -webkit-backdrop-filter: blur(12px);
            box-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.37);
            transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
        }}

        header {{
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 2rem;
            padding-bottom: 1.5rem;
            border-bottom: 1px solid rgba(255, 255, 255, 0.08);
        }}

        header h1 {{
            font-family: 'Outfit', sans-serif;
            font-size: 2.2rem;
            font-weight: 700;
            background: linear-gradient(135deg, #a5b4fc 0%, #6366f1 50%, #ec4899 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            letter-spacing: -0.02em;
        }}

        .timestamp {{
            font-size: 0.9rem;
            color: var(--text-muted);
            margin-top: 0.25rem;
        }}

        /* Tabs Nav */
        .tab-nav {{
            display: flex;
            gap: 0.75rem;
            margin-bottom: 2rem;
            padding: 0.35rem;
            background: rgba(255, 255, 255, 0.03);
            border-radius: 12px;
            width: fit-content;
            border: 1px solid rgba(255, 255, 255, 0.05);
        }}

        .tab-btn {{
            background: transparent;
            border: none;
            color: var(--text-muted);
            padding: 0.6rem 1.2rem;
            font-size: 0.95rem;
            font-weight: 600;
            cursor: pointer;
            border-radius: 8px;
            transition: all 0.2s ease;
        }}

        .tab-btn:hover {{
            color: var(--text-color);
        }}

        .tab-btn.active {{
            color: white;
            background: var(--primary);
            box-shadow: 0 4px 14px 0 var(--primary-glow);
        }}

        /* Top Metrics Row for general stats */
        .metrics-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
            gap: 1.25rem;
            margin-bottom: 2rem;
        }}

        .metric-card {{
            padding: 1.25rem;
            text-align: center;
        }}

        .metric-card:hover {{
            transform: translateY(-2px);
            border-color: rgba(255, 255, 255, 0.15);
        }}

        .metric-val {{
            font-family: 'Outfit', sans-serif;
            font-size: 1.8rem;
            font-weight: 700;
            margin-top: 0.25rem;
        }}

        .metric-val.critical {{ color: var(--critical); }}
        .metric-val.warning {{ color: var(--warning); }}
        .metric-val.info {{ color: var(--info); }}
        .metric-val.success {{ color: var(--success); }}

        .metric-label {{
            font-size: 0.8rem;
            color: var(--text-muted);
            text-transform: uppercase;
            letter-spacing: 0.05em;
            font-weight: 500;
        }}

        /* ---------------- TAB CONTENT: REPORT CARD ---------------- */
        .report-layout {{
            display: grid;
            grid-template-columns: 320px 1fr;
            gap: 2rem;
            margin-bottom: 2rem;
        }}

        @media (max-width: 968px) {{
            .report-layout {{
                grid-template-columns: 1fr;
            }}
        }}

        /* Grade Summary Card */
        .grade-card {{
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            padding: 2.5rem 2rem;
            text-align: center;
            position: relative;
            overflow: hidden;
        }}

        .grade-card::before {{
            content: '';
            position: absolute;
            top: -50%;
            left: -50%;
            width: 200%;
            height: 200%;
            background: radial-gradient(circle, var(--grade-glow, rgba(99, 102, 241, 0.15)) 0%, transparent 60%);
            pointer-events: none;
        }}

        .grade-title {{
            font-size: 1.1rem;
            font-weight: 600;
            color: var(--text-muted);
            text-transform: uppercase;
            letter-spacing: 0.05em;
            margin-bottom: 1.5rem;
        }}

        /* Large Ring */
        .overall-ring-container {{
            position: relative;
            width: 180px;
            height: 180px;
            margin-bottom: 1.5rem;
        }}

        .overall-progress {{
            transform: rotate(-90deg);
        }}

        .overall-progress circle {{
            fill: none;
            stroke-width: 12;
        }}

        .overall-progress .bg-circle {{
            stroke: rgba(255, 255, 255, 0.04);
        }}

        .overall-progress .fg-circle {{
            stroke-linecap: round;
            transition: stroke-dashoffset 1.5s cubic-bezier(0.4, 0, 0.2, 1);
        }}

        .grade-display {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            display: flex;
            flex-direction: column;
            align-items: center;
        }}

        .grade-letter {{
            font-family: 'Outfit', sans-serif;
            font-size: 4rem;
            font-weight: 800;
            line-height: 1;
            margin-bottom: 0.2rem;
        }}

        .score-value {{
            font-family: 'Outfit', sans-serif;
            font-size: 1.1rem;
            font-weight: 600;
            color: var(--text-muted);
        }}

        .grade-desc {{
            font-size: 0.95rem;
            color: var(--text-color);
            font-weight: 500;
            margin-top: 0.5rem;
            padding: 0.3rem 0.8rem;
            border-radius: 20px;
            background: rgba(255, 255, 255, 0.05);
        }}

        /* Section Score Grid */
        .section-scores-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 1.5rem;
        }}

        .section-card {{
            padding: 1.5rem;
            display: flex;
            flex-direction: column;
            justify-content: space-between;
        }}

        .section-card:hover {{
            border-color: rgba(255, 255, 255, 0.15);
            transform: translateY(-2px);
        }}

        .section-header {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 1.25rem;
        }}

        .section-title {{
            font-size: 1.05rem;
            font-weight: 600;
            color: var(--text-color);
        }}

        .section-weight {{
            font-size: 0.75rem;
            background: rgba(255, 255, 255, 0.06);
            color: var(--text-muted);
            padding: 0.15rem 0.45rem;
            border-radius: 4px;
            font-weight: 500;
        }}

        .section-body {{
            display: flex;
            gap: 1.25rem;
            align-items: center;
            margin-bottom: 1.25rem;
        }}

        .section-circle-container {{
            position: relative;
            width: 80px;
            height: 80px;
            flex-shrink: 0;
        }}

        .section-progress {{
            transform: rotate(-90deg);
        }}

        .section-progress circle {{
            fill: none;
            stroke-width: 8;
        }}

        .section-progress .bg-circle {{
            stroke: rgba(255, 255, 255, 0.04);
        }}

        .section-progress .fg-circle {{
            stroke-linecap: round;
            transition: stroke-dashoffset 1.2s cubic-bezier(0.4, 0, 0.2, 1);
        }}

        .section-score-txt {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            font-family: 'Outfit', sans-serif;
            font-size: 1.25rem;
            font-weight: 700;
        }}

        .section-details {{
            flex-grow: 1;
            display: flex;
            flex-direction: column;
            gap: 0.35rem;
            font-size: 0.85rem;
            color: var(--text-muted);
        }}

        .detail-row {{
            display: flex;
            justify-content: space-between;
        }}

        .detail-val {{
            color: var(--text-color);
            font-weight: 500;
        }}

        .section-footer {{
            font-size: 0.8rem;
            color: var(--text-muted);
            border-top: 1px solid rgba(255, 255, 255, 0.05);
            padding-top: 0.75rem;
            margin-top: 0.25rem;
        }}

        /* Methodology panel */
        .methodology-panel {{
            padding: 2rem;
            margin-top: 2.5rem;
        }}

        .methodology-title {{
            font-size: 1.2rem;
            font-weight: 600;
            margin-bottom: 1.5rem;
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }}

        .methodology-title::before {{
            content: '';
            display: inline-block;
            width: 4px;
            height: 18px;
            background: var(--primary);
            border-radius: 2px;
        }}

        .methodology-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 1.5rem;
        }}

        .methodology-item h4 {{
            font-size: 0.95rem;
            font-weight: 600;
            margin-bottom: 0.5rem;
            color: var(--text-color);
        }}

        .methodology-item p {{
            font-size: 0.85rem;
            color: var(--text-muted);
            line-height: 1.5;
        }}

        /* ---------------- TAB CONTENT: ISSUES ---------------- */
        .content-layout {{
            display: grid;
            grid-template-columns: 280px 1fr;
            gap: 2rem;
        }}

        @media (max-width: 1024px) {{
            .content-layout {{
                grid-template-columns: 1fr;
            }}
        }}

        /* Filters */
        .filter-panel {{
            padding: 1.5rem;
            height: fit-content;
        }}

        .filter-title {{
            font-size: 1rem;
            font-weight: 600;
            margin-bottom: 1.25rem;
            border-bottom: 1px solid rgba(255, 255, 255, 0.08);
            padding-bottom: 0.5rem;
        }}

        .filter-group {{
            margin-bottom: 1.5rem;
        }}

        .filter-group label {{
            display: block;
            font-size: 0.85rem;
            color: var(--text-muted);
            margin-bottom: 0.6rem;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }}

        .filter-btn {{
            display: block;
            width: 100%;
            text-align: left;
            background: transparent;
            border: none;
            color: var(--text-color);
            padding: 0.6rem 0.8rem;
            border-radius: 8px;
            cursor: pointer;
            font-size: 0.9rem;
            margin-bottom: 0.4rem;
            transition: all 0.2s ease;
        }}

        .filter-btn:hover {{
            background: rgba(255, 255, 255, 0.04);
        }}

        .filter-btn.active {{
            background: var(--primary);
            color: white;
            font-weight: 500;
            box-shadow: 0 4px 10px rgba(99, 102, 241, 0.25);
        }}

        /* Issue list */
        .issues-container {{
            display: flex;
            flex-direction: column;
            gap: 1rem;
        }}

        .issue-card {{
            padding: 1.5rem;
            position: relative;
            overflow: hidden;
        }}

        .severity-strip {{
            position: absolute;
            top: 0;
            left: 0;
            bottom: 0;
            width: 6px;
        }}

        .severity-strip.CRITICAL {{ background-color: var(--critical); }}
        .severity-strip.WARNING {{ background-color: var(--warning); }}
        .severity-strip.INFO {{ background-color: var(--info); }}

        .issue-header {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 0.8rem;
        }}

        .issue-title {{
            font-size: 1.1rem;
            font-weight: 600;
        }}

        .issue-badge {{
            font-size: 0.75rem;
            font-weight: 700;
            padding: 0.2rem 0.6rem;
            border-radius: 20px;
            text-transform: uppercase;
            letter-spacing: 0.02em;
        }}

        .issue-badge.CRITICAL {{ background: rgba(239, 68, 68, 0.15); color: #f87171; border: 1px solid rgba(239, 68, 68, 0.3); }}
        .issue-badge.WARNING {{ background: rgba(251, 191, 36, 0.15); color: #fbbf24; border: 1px solid rgba(251, 191, 36, 0.3); }}
        .issue-badge.INFO {{ background: rgba(59, 130, 246, 0.15); color: #60a5fa; border: 1px solid rgba(59, 130, 246, 0.3); }}

        .issue-meta {{
            display: flex;
            flex-wrap: wrap;
            gap: 1.5rem;
            font-size: 0.85rem;
            color: var(--text-muted);
            margin-bottom: 0.8rem;
        }}

        .issue-meta strong {{
            color: #d1d5db;
        }}

        .issue-desc {{
            font-size: 0.95rem;
            color: #d1d5db;
            margin-bottom: 1rem;
            line-height: 1.5;
        }}

        .code-snippet {{
            font-family: 'Fira Code', monospace;
            background: #040508;
            border: 1px solid rgba(255, 255, 255, 0.04);
            border-radius: 8px;
            padding: 0.8rem 1rem;
            font-size: 0.85rem;
            overflow-x: auto;
            color: #e5e7eb;
        }}

        .no-issues {{
            padding: 3rem;
            text-align: center;
            color: var(--text-muted);
        }}

        .no-issues h3 {{
            font-size: 1.2rem;
            margin-bottom: 0.5rem;
            color: var(--text-color);
        }}

        /* ---------------- TAB CONTENT: METRICS ---------------- */
        .table-container {{
            padding: 1.5rem;
            overflow-x: auto;
        }}

        table {{
            width: 100%;
            border-collapse: collapse;
            text-align: left;
        }}

        th {{
            border-bottom: 2px solid rgba(255, 255, 255, 0.08);
            padding: 0.9rem 1rem;
            font-size: 0.9rem;
            font-weight: 600;
            color: var(--text-muted);
        }}

        td {{
            border-bottom: 1px solid rgba(255, 255, 255, 0.04);
            padding: 0.9rem 1rem;
            font-size: 0.9rem;
        }}

        tr:hover td {{
            background: rgba(255, 255, 255, 0.01);
        }}

        .rating-badge {{
            font-size: 0.75rem;
            font-weight: 600;
            padding: 0.25rem 0.5rem;
            border-radius: 6px;
            letter-spacing: 0.02em;
        }}

        .rating-low {{ background: rgba(16, 185, 129, 0.15); color: #34d399; }}
        .rating-moderate {{ background: rgba(251, 191, 36, 0.15); color: #fbbf24; }}
        .rating-high {{ background: rgba(239, 68, 68, 0.15); color: #f87171; }}
        .rating-very-high {{ background: rgba(239, 68, 68, 0.25); color: #f87171; border: 1px solid rgba(239, 68, 68, 0.4); }}
    </style>
</head>
<body>

    <header>
        <div>
            <h1>NOVA Static Analysis Dashboard</h1>
            <div class="timestamp">분석 일시: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}</div>
        </div>
        <div>
            <span style="font-weight: 600; background: rgba(255,255,255,0.06); padding: 0.5rem 1rem; border-radius: 8px; border: 1px solid rgba(255,255,255,0.05);">프로젝트: NOVA (WPF + Lua)</span>
        </div>
    </header>

    <div class="metrics-grid">
        <div class="metric-card glass-panel">
            <div class="metric-label">총 분석 파일 수</div>
            <div class="metric-val" id="val-total-files">0</div>
        </div>
        <div class="metric-card glass-panel">
            <div class="metric-label">총 코드 라인 (LOC)</div>
            <div class="metric-val" id="val-loc">0</div>
        </div>
        <div class="metric-card glass-panel">
            <div class="metric-label">실제 코드 라인 (SLOC)</div>
            <div class="metric-val" id="val-sloc">0</div>
        </div>
        <div class="metric-card glass-panel">
            <div class="metric-label">평균 복잡도 (CC)</div>
            <div class="metric-val" id="val-avg-cc">0</div>
        </div>
        <div class="metric-card glass-panel" style="border-color: rgba(239, 68, 68, 0.3);">
            <div class="metric-label" style="color: #f87171;">Critical 이슈</div>
            <div class="metric-val critical" id="val-critical">0</div>
        </div>
        <div class="metric-card glass-panel" style="border-color: rgba(255, 191, 36, 0.3);">
            <div class="metric-label" style="color: #fbbf24;">Warning 이슈</div>
            <div class="metric-val warning" id="val-warning">0</div>
        </div>
    </div>

    <!-- Navigation Tab -->
    <div class="tab-nav">
        <button class="tab-btn active" id="tab-report-btn" onclick="switchTab('report')">코드 품질 성적표 (Report Card)</button>
        <button class="tab-btn" id="tab-issues-btn" onclick="switchTab('issues')">정적 분석 결과 (이슈)</button>
        <button class="tab-btn" id="tab-metrics-btn" onclick="switchTab('metrics')">파일 메트릭스 (LOC / CC)</button>
    </div>

    <!-- Tab 1: Report Card -->
    <div id="tab-report">
        <div class="report-layout">
            <!-- Overall Grade -->
            <div class="grade-card glass-panel" id="overall-grade-card">
                <div class="grade-title">종합 코드 품질 등급</div>
                <div class="overall-ring-container">
                    <svg width="180" height="180" viewBox="0 0 180 180" class="overall-progress">
                        <circle cx="90" cy="90" r="75" class="bg-circle" />
                        <circle cx="90" cy="90" r="75" class="fg-circle" id="circle-overall" stroke-dasharray="471.24" stroke-dashoffset="471.24" />
                    </svg>
                    <div class="grade-display">
                        <span class="grade-letter" id="val-grade">F</span>
                        <span class="score-value" id="val-overall-score">0 / 100</span>
                    </div>
                </div>
                <div class="grade-desc" id="val-grade-desc">평가 대기 중</div>
            </div>

            <!-- Section Scores -->
            <div class="section-scores-grid">
                <!-- Analysis Card -->
                <div class="section-card glass-panel">
                    <div class="section-header">
                        <span class="section-title">정밀 분석 점수 (Static Analysis)</span>
                        <span class="section-weight">비중 35%</span>
                    </div>
                    <div class="section-body">
                        <div class="section-circle-container">
                            <svg width="80" height="80" viewBox="0 0 80 80" class="section-progress">
                                <circle cx="40" cy="40" r="34" class="bg-circle" />
                                <circle cx="40" cy="40" r="34" class="fg-circle" id="circle-analysis" stroke-dasharray="213.63" stroke-dashoffset="213.63" />
                            </svg>
                            <span class="section-score-txt" id="score-analysis">0</span>
                        </div>
                        <div class="section-details">
                            <div class="detail-row">
                                <span>Critical 이슈:</span>
                                <span class="detail-val" id="val-detail-critical">0건</span>
                            </div>
                            <div class="detail-row">
                                <span>Warning 이슈:</span>
                                <span class="detail-val" id="val-detail-warning">0건</span>
                            </div>
                            <div class="detail-row">
                                <span>Info 이슈:</span>
                                <span class="detail-val" id="val-detail-info">0건</span>
                            </div>
                        </div>
                    </div>
                    <div class="section-footer">
                        이슈 감점 적용 (Critical -15, Warning -5, Info -0.1)
                    </div>
                </div>

                <!-- Complexity Card -->
                <div class="section-card glass-panel">
                    <div class="section-header">
                        <span class="section-title">복잡도 관리 점수 (Complexity)</span>
                        <span class="section-weight">비중 30%</span>
                    </div>
                    <div class="section-body">
                        <div class="section-circle-container">
                            <svg width="80" height="80" viewBox="0 0 80 80" class="section-progress">
                                <circle cx="40" cy="40" r="34" class="bg-circle" />
                                <circle cx="40" cy="40" r="34" class="fg-circle" id="circle-complexity" stroke-dasharray="213.63" stroke-dashoffset="213.63" />
                            </svg>
                            <span class="section-score-txt" id="score-complexity">0</span>
                        </div>
                        <div class="section-details">
                            <div class="detail-row">
                                <span>평균 복잡도 (CC):</span>
                                <span class="detail-val" id="val-detail-avg-cc">0</span>
                            </div>
                            <div class="detail-row">
                                <span>Very High 복잡도 파일:</span>
                                <span class="detail-val" id="val-detail-cc-vh">0개</span>
                            </div>
                            <div class="detail-row">
                                <span>High 복잡도 파일:</span>
                                <span class="detail-val" id="val-detail-cc-h">0개</span>
                            </div>
                        </div>
                    </div>
                    <div class="section-footer">
                        순환 복잡도 등급별 기준 점수 평균치 (Low: 100, High: 70 등)
                    </div>
                </div>

                <!-- Scale Card -->
                <div class="section-card glass-panel">
                    <div class="section-header">
                        <span class="section-title">가독성 및 규모 점수 (Scale)</span>
                        <span class="section-weight">비중 20%</span>
                    </div>
                    <div class="section-body">
                        <div class="section-circle-container">
                            <svg width="80" height="80" viewBox="0 0 80 80" class="section-progress">
                                <circle cx="40" cy="40" r="34" class="bg-circle" />
                                <circle cx="40" cy="40" r="34" class="fg-circle" id="circle-scale" stroke-dasharray="213.63" stroke-dashoffset="213.63" />
                            </svg>
                            <span class="section-score-txt" id="score-scale">0</span>
                        </div>
                        <div class="section-details">
                            <div class="detail-row">
                                <span>Very High 규모 파일 (&gt;1500 LOC):</span>
                                <span class="detail-val" id="val-detail-loc-vh">0개</span>
                            </div>
                            <div class="detail-row">
                                <span>High 규모 파일 (1000~1500 LOC):</span>
                                <span class="detail-val" id="val-detail-loc-h">0개</span>
                            </div>
                            <div class="detail-row">
                                <span>Deep Nesting 감지건:</span>
                                <span class="detail-val" id="val-detail-nesting">0건</span>
                            </div>
                        </div>
                    </div>
                    <div class="section-footer">
                        파일 크기 등급별 기준 점수 평균치 (Low: 100, High: 90 등) 및 중첩 감점
                    </div>
                </div>

                <!-- Documentation Card -->
                <div class="section-card glass-panel">
                    <div class="section-header">
                        <span class="section-title">주석 및 문서화 점수 (Documentation)</span>
                        <span class="section-weight">비중 15%</span>
                    </div>
                    <div class="section-body">
                        <div class="section-circle-container">
                            <svg width="80" height="80" viewBox="0 0 80 80" class="section-progress">
                                <circle cx="40" cy="40" r="34" class="bg-circle" />
                                <circle cx="40" cy="40" r="34" class="fg-circle" id="circle-doc" stroke-dasharray="213.63" stroke-dashoffset="213.63" />
                            </svg>
                            <span class="section-score-txt" id="score-doc">0</span>
                        </div>
                        <div class="section-details">
                            <div class="detail-row">
                                <span>실제 코드 라인:</span>
                                <span class="detail-val" id="val-detail-code-lines">0라인</span>
                            </div>
                            <div class="detail-row">
                                <span>주석 라인:</span>
                                <span class="detail-val" id="val-detail-comment-lines">0라인</span>
                            </div>
                            <div class="detail-row">
                                <span>주석 밀도 (%):</span>
                                <span class="detail-val" id="val-detail-doc-ratio">0%</span>
                            </div>
                        </div>
                    </div>
                    <div class="section-footer">
                        코드 대비 주석 비율 평가 (10% 이상: 95점, 2% 미만: 50점)
                    </div>
                </div>
            </div>
        </div>

        <!-- Methodology -->
        <div class="methodology-panel glass-panel">
            <div class="methodology-title">코드 품질 평가 기준 및 채점 방식</div>
            <div class="methodology-grid">
                <div class="methodology-item">
                    <h4>1. 정밀 분석 점수 (35%)</h4>
                    <p>정적 분석 규칙에 의해 감지된 문제들의 위험도별 감점. Critical 감점 -15점 (최대 -70), Warning 감점 -5점 (최대 -50), Info 감점 -0.1점 (최대 -10)을 적용하여 기본 100점에서 차감합니다.</p>
                </div>
                <div class="methodology-item">
                    <h4>2. 복잡도 관리 점수 (30%)</h4>
                    <p>각 파일의 순환 복잡도(Cyclomatic Complexity) 등급에 따른 평균치. Low(CC &le; 50)는 100점, Moderate(CC &le; 200)는 95점, High(CC &le; 1000)는 80점, Very High(CC &gt; 1000)는 60점을 부여합니다.</p>
                </div>
                <div class="methodology-item">
                    <h4>3. 가독성 및 규모 점수 (20%)</h4>
                    <p>파일 크기(LOC) 등급에 따른 평균치. Low(LOC &le; 800)는 100점, Moderate(LOC &le; 1000)는 97점, High(LOC &le; 1500)는 90점, Very High(LOC &gt; 1500)는 80점을 부여하며, 깊은 제어문 중첩(6단계 이상) 감지 시 건당 -0.1점 감점(최대 -5)을 적용합니다.</p>
                </div>
                <div class="methodology-item">
                    <h4>4. 주석 및 문서화 점수 (15%)</h4>
                    <p>프로젝트 전체의 주석 라인 비율에 따른 평가. 비율 10% 이상 시 95점, 5% 이상 85점, 2% 이상 75점, 2% 미만 시 50점을 적용하여 문서화 수준을 관리합니다.</p>
                </div>
            </div>
        </div>
    </div>

    <!-- Tab 2: Issues -->
    <div class="content-layout" id="tab-issues" style="display: none;">
        <!-- Sidebar filters -->
        <div class="filter-panel glass-panel">
            <div class="filter-title">필터</div>
            
            <div class="filter-group">
                <label>위험도</label>
                <button class="filter-btn active" onclick="filterSeverity('ALL')" id="btn-sev-all">전체</button>
                <button class="filter-btn" onclick="filterSeverity('CRITICAL')" id="btn-sev-crit">Critical</button>
                <button class="filter-btn" onclick="filterSeverity('WARNING')" id="btn-sev-warn">Warning</button>
                <button class="filter-btn" onclick="filterSeverity('INFO')" id="btn-sev-info">Info</button>
            </div>

            <div class="filter-group">
                <label>카테고리</label>
                <button class="filter-btn active" onclick="filterCategory('ALL')" id="btn-cat-all">전체</button>
                <button class="filter-btn" onclick="filterCategory('Memory Leak')" id="btn-cat-mem">Memory Leak</button>
                <button class="filter-btn" onclick="filterCategory('Concurrency')" id="btn-cat-conc">Concurrency / Race Condition</button>
                <button class="filter-btn" onclick="filterCategory('Exception Handling')" id="btn-cat-ex">Exception Handling</button>
                <button class="filter-btn" onclick="filterCategory('Code Smell')" id="btn-cat-smell">Code Smell</button>
            </div>
        </div>

        <!-- Issue listings -->
        <div>
            <div class="issues-container" id="issues-list">
                <!-- Javascript will populate this -->
            </div>
        </div>
    </div>

    <!-- Tab 3: File Metrics -->
    <div id="tab-metrics" style="display: none;">
        <div class="table-container glass-panel">
            <table>
                <thead>
                    <tr>
                        <th>파일 경로</th>
                        <th>유형</th>
                        <th>총 라인수 (LOC)</th>
                        <th>규모 등급</th>
                        <th>순환 복잡도 (CC)</th>
                        <th>복잡도 등급</th>
                    </tr>
                </thead>
                <tbody id="metrics-table-body">
                    <!-- Javascript will populate this -->
                </tbody>
            </table>
        </div>
    </div>

    <script>
        const issues = {issues_json};
        const metrics = {metrics_json};
        const stats = {stats_json};
        const scores = {scores_json};

        let currentSeverity = 'ALL';
        let currentCategory = 'ALL';

        // Set metrics top bar
        document.getElementById('val-total-files').innerText = stats.total_files;
        document.getElementById('val-loc').innerText = stats.lines_of_code.toLocaleString();
        document.getElementById('val-sloc').innerText = stats.code_lines.toLocaleString();
        document.getElementById('val-avg-cc').innerText = stats.avg_cc;
        document.getElementById('val-critical').innerText = stats.critical_issues;
        document.getElementById('val-warning').innerText = stats.warning_issues;

        // Set Report Card overall grade & score
        document.getElementById('val-grade').innerText = scores.overall_grade;
        document.getElementById('val-overall-score').innerText = scores.overall_score + ' / 100';
        
        // Dynamic grade color configuration
        let gradeColor = 'var(--grade-f)';
        let gradeGlow = 'rgba(239, 68, 68, 0.2)';
        let gradeDescText = '개선이 매우 시급한 상태 (F)';
        
        if (scores.overall_grade === 'A+') {{
            gradeColor = 'var(--grade-a)';
            gradeGlow = 'rgba(16, 185, 129, 0.35)';
            gradeDescText = '최우수 코드 품질 상태 (A+)';
        }} else if (scores.overall_grade === 'A') {{
            gradeColor = 'var(--grade-a)';
            gradeGlow = 'rgba(16, 185, 129, 0.25)';
            gradeDescText = '매우 우수한 코드 품질 상태 (A)';
        }} else if (scores.overall_grade === 'B') {{
            gradeColor = 'var(--grade-b)';
            gradeGlow = 'rgba(59, 130, 246, 0.25)';
            gradeDescText = '양호한 코드 품질 상태 (B)';
        }} else if (scores.overall_grade === 'C') {{
            gradeColor = 'var(--grade-c)';
            gradeGlow = 'rgba(251, 191, 36, 0.25)';
            gradeDescText = '보통 수준의 코드 품질 상태 (C)';
        }} else if (scores.overall_grade === 'D') {{
            gradeColor = 'var(--grade-d)';
            gradeGlow = 'rgba(237, 100, 166, 0.25)';
            gradeDescText = '주의 및 개선이 필요한 상태 (D)';
        }}
        
        const gradeCard = document.getElementById('overall-grade-card');
        gradeCard.style.setProperty('--grade-glow', gradeGlow);
        document.getElementById('val-grade').style.color = gradeColor;
        document.getElementById('val-grade').style.textShadow = `0 0 20px ${{gradeColor}}`;
        document.getElementById('val-grade-desc').innerText = gradeDescText;

        // Populate detail panels in Report Card
        document.getElementById('score-analysis').innerText = Math.round(scores.analysis_score);
        document.getElementById('val-detail-critical').innerText = stats.critical_issues + '건';
        document.getElementById('val-detail-warning').innerText = stats.warning_issues + '건';
        document.getElementById('val-detail-info').innerText = stats.info_issues + '건';

        document.getElementById('score-complexity').innerText = Math.round(scores.complexity_score);
        document.getElementById('val-detail-avg-cc').innerText = stats.avg_cc;
        
        // Calculate complexity details
        let ccVh = 0, ccH = 0, ccM = 0, ccL = 0;
        metrics.forEach(m => {{
            if (m.rating === 'Very High') ccVh++;
            else if (m.rating === 'High') ccH++;
            else if (m.rating === 'Moderate') ccM++;
            else ccL++;
        }});
        document.getElementById('val-detail-cc-vh').innerText = ccVh + '개 파일';
        document.getElementById('val-detail-cc-h').innerText = ccH + '개 파일';

        document.getElementById('score-scale').innerText = Math.round(scores.scale_score);
        let locVh = 0, locH = 0, locM = 0, locL = 0;
        metrics.forEach(m => {{
            if (m.loc_rating === 'Very High') locVh++;
            else if (m.loc_rating === 'High') locH++;
            else if (m.loc_rating === 'Moderate') locM++;
            else locL++;
        }});
        const nestingIssuesCount = issues.filter(i => i.title === 'Deep Control Flow Nesting').length;
        document.getElementById('val-detail-loc-vh').innerText = locVh + '개 파일';
        document.getElementById('val-detail-loc-h').innerText = locH + '개 파일';
        document.getElementById('val-detail-nesting').innerText = nestingIssuesCount + '건';

        document.getElementById('score-doc').innerText = Math.round(scores.documentation_score);
        document.getElementById('val-detail-code-lines').innerText = stats.code_lines.toLocaleString() + ' L';
        document.getElementById('val-detail-comment-lines').innerText = stats.comment_lines.toLocaleString() + ' L';
        
        const totalLinesForRatio = stats.code_lines + stats.comment_lines;
        const commentRatioPct = totalLinesForRatio > 0 ? ((stats.comment_lines / totalLinesForRatio) * 100).toFixed(1) : '0.0';
        document.getElementById('val-detail-doc-ratio').innerText = commentRatioPct + '%';

        function animateCircularProgress() {{
            // Overall score circle
            const overallCircle = document.getElementById('circle-overall');
            if (overallCircle) {{
                const r = overallCircle.r.baseVal.value;
                const circumference = 2 * Math.PI * r;
                overallCircle.style.strokeDasharray = circumference;
                overallCircle.style.strokeDashoffset = circumference;
                overallCircle.getBoundingClientRect(); // trigger reflow
                const offset = circumference - (scores.overall_score / 100) * circumference;
                overallCircle.style.strokeDashoffset = offset;
                overallCircle.style.stroke = gradeColor;
                overallCircle.style.filter = `drop-shadow(0 0 8px ${{gradeColor}})`;
            }}

            // Section circles
            const progressElements = [
                {{ id: 'circle-analysis', score: scores.analysis_score }},
                {{ id: 'circle-complexity', score: scores.complexity_score }},
                {{ id: 'circle-scale', score: scores.scale_score }},
                {{ id: 'circle-doc', score: scores.documentation_score }}
            ];

            progressElements.forEach(item => {{
                const circle = document.getElementById(item.id);
                if (circle) {{
                    const r = circle.r.baseVal.value;
                    const circumference = 2 * Math.PI * r;
                    circle.style.strokeDasharray = circumference;
                    circle.style.strokeDashoffset = circumference;
                    circle.getBoundingClientRect(); // trigger reflow
                    const offset = circumference - (item.score / 100) * circumference;
                    circle.style.strokeDashoffset = offset;

                    let color = 'var(--grade-f)';
                    if (item.score >= 90) color = 'var(--grade-a)';
                    else if (item.score >= 80) color = 'var(--grade-b)';
                    else if (item.score >= 70) color = 'var(--grade-c)';
                    else if (item.score >= 60) color = 'var(--grade-d)';
                    circle.style.stroke = color;
                }}
            }});
        }}

        function switchTab(tabName) {{
            if (tabName === 'report') {{
                document.getElementById('tab-report').style.display = 'block';
                document.getElementById('tab-issues').style.display = 'none';
                document.getElementById('tab-metrics').style.display = 'none';
                
                document.getElementById('tab-report-btn').classList.add('active');
                document.getElementById('tab-issues-btn').classList.remove('active');
                document.getElementById('tab-metrics-btn').classList.remove('active');
                animateCircularProgress();
            }} else if (tabName === 'issues') {{
                document.getElementById('tab-report').style.display = 'none';
                document.getElementById('tab-issues').style.display = 'grid';
                document.getElementById('tab-metrics').style.display = 'none';
                
                document.getElementById('tab-report-btn').classList.remove('active');
                document.getElementById('tab-issues-btn').classList.add('active');
                document.getElementById('tab-metrics-btn').classList.remove('active');
            }} else {{
                document.getElementById('tab-report').style.display = 'none';
                document.getElementById('tab-issues').style.display = 'none';
                document.getElementById('tab-metrics').style.display = 'block';
                
                document.getElementById('tab-report-btn').classList.remove('active');
                document.getElementById('tab-issues-btn').classList.remove('active');
                document.getElementById('tab-metrics-btn').classList.add('active');
                renderMetrics();
            }}
        }}

        function renderIssues() {{
            const listContainer = document.getElementById('issues-list');
            listContainer.innerHTML = '';

            const filtered = issues.filter(issue => {{
                const matchSev = currentSeverity === 'ALL' || issue.severity === currentSeverity;
                const matchCat = currentCategory === 'ALL' || issue.category === currentCategory || 
                                 (currentCategory === 'Memory Leak' && issue.category.includes('Memory Leak'));
                return matchSev && matchCat;
            }});

            if (filtered.length === 0) {{
                listContainer.innerHTML = `
                    <div class="no-issues glass-panel">
                        <h3>감지된 이슈가 없습니다!</h3>
                        <p style="margin-top: 0.5rem; color: var(--text-muted);">선택한 조건에 해당하는 정적 분석 결과가 깨끗합니다.</p>
                    </div>
                `;
                return;
            }}

            filtered.forEach(issue => {{
                const card = document.createElement('div');
                card.className = 'issue-card glass-panel';
                
                let snippetHtml = '';
                if (issue.code_snippet) {{
                    snippetHtml = `<pre class="code-snippet"><code>${{escapeHtml(issue.code_snippet)}}</code></pre>`;
                }}

                card.innerHTML = `
                    <div class="severity-strip ${{issue.severity}}"></div>
                    <div class="issue-header">
                        <div class="issue-title">${{issue.title}}</div>
                        <span class="issue-badge ${{issue.severity}}">${{issue.severity}}</span>
                    </div>
                    <div class="issue-meta">
                        <span><strong>파일:</strong> ${{issue.file}}</span>
                        <span><strong>라인:</strong> ${{issue.line}}</span>
                        <span><strong>카테고리:</strong> ${{issue.category}}</span>
                    </div>
                    <div class="issue-desc">${{issue.description}}</div>
                    ${{snippetHtml}}
                `;
                listContainer.appendChild(card);
            }});
        }}

        function renderMetrics() {{
            const tbody = document.getElementById('metrics-table-body');
            tbody.innerHTML = '';

            // Sort by complexity descending
            const sortedMetrics = [...metrics].sort((a, b) => b.cc - a.cc);

            sortedMetrics.forEach(file => {{
                const tr = document.createElement('tr');
                tr.innerHTML = `
                    <td><strong>${{file.file}}</strong></td>
                    <td>${{file.type}}</td>
                    <td>${{file.loc.toLocaleString()}}</td>
                    <td><span class="rating-badge ${{file.loc_rating_class}}">${{file.loc_rating}}</span></td>
                    <td><strong>${{file.cc}}</strong></td>
                    <td><span class="rating-badge ${{file.rating_class}}">${{file.rating}}</span></td>
                `;
                tbody.appendChild(tr);
            }});
        }}

        function escapeHtml(text) {{
            return text
                .replace(/&/g, "&amp;")
                .replace(/</g, "&lt;")
                .replace(/>/g, "&gt;")
                .replace(/"/g, "&quot;")
                .replace(/'/g, "&#039;");
        }}

        function filterSeverity(sev) {{
            currentSeverity = sev;
            
            // Update buttons
            document.querySelectorAll('#tab-issues .filter-panel .filter-group:nth-child(2) .filter-btn').forEach(btn => btn.classList.remove('active'));
            if (sev === 'ALL') document.getElementById('btn-sev-all').classList.add('active');
            else if (sev === 'CRITICAL') document.getElementById('btn-sev-crit').classList.add('active');
            else if (sev === 'WARNING') document.getElementById('btn-sev-warn').classList.add('active');
            else if (sev === 'INFO') document.getElementById('btn-sev-info').classList.add('active');

            renderIssues();
        }}

        function filterCategory(cat) {{
            currentCategory = cat;

            // Update buttons
            document.querySelectorAll('#tab-issues .filter-panel .filter-group:nth-child(3) .filter-btn').forEach(btn => btn.classList.remove('active'));
            if (cat === 'ALL') document.getElementById('btn-cat-all').classList.add('active');
            else if (cat === 'Memory Leak') document.getElementById('btn-cat-mem').classList.add('active');
            else if (cat === 'Concurrency') document.getElementById('btn-cat-conc').classList.add('active');
            else if (cat === 'Exception Handling') document.getElementById('btn-cat-ex').classList.add('active');
            else if (cat === 'Code Smell') document.getElementById('btn-cat-smell').classList.add('active');

            renderIssues();
        }}

        // Initial render
        renderIssues();
        animateCircularProgress();
    </script>
</body>
</html>
"""

        with open(output_path, "w", encoding="utf-8") as f:
            f.write(html_content)


if __name__ == "__main__":
    analyzer = StaticAnalyzer("./Backend/FlowEngine")
    analyzer.run()
    # Write report to workspace
    analyzer.generate_html_report("./static_analysis_dashboard.html")
    print(f"Analysis completed. Found {len(analyzer.issues)} issues.")
