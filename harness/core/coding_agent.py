
import os
import queue
import re
import signal
import subprocess
import threading
import time
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Literal


Status = Literal["success", "usage_limit", "timeout", "failed"]


@dataclass
class AgentResult:
    status: Status
    returncode: int
    log_path: Path


class CodingAgent:
    def __init__(self, project_dir: Path):
        self.project_dir = project_dir
        self.log_dir = project_dir / "harness" / "reports" / "logs"
        self.log_dir.mkdir(parents=True, exist_ok=True)

    def execute(
        self,
        task: str,
        timeout: int = 900,
        on_progress=None,
        report_interval: int = 300,
    ) -> AgentResult:
        prompt = f"""
너는 Unity C# 전문 개발 에이전트다.

프로젝트: car3-c
현재 TODO: {task}

작업 규칙:
1. 기존 프로젝트 구조와 코드를 분석한다.
2. 이미 구현된 기능은 중복 개발하지 않는다.
3. 필요한 코드만 수정한다.
4. 가능한 범위에서 테스트한다.
5. 작업 결과를 한국어로 요약한다.
6. Git 커밋은 하지 않는다.
7. Unity 프로젝트 외부 파일은 수정하지 않는다.
"""

        log_path = self.log_dir / (
            f"codex_{datetime.now():%Y%m%d_%H%M%S}.log"
        )

        command = [
            "codex",
            "exec",
            "--sandbox",
            "workspace-write",
            "-C",
            str(self.project_dir),
            prompt,
        ]

        print(f"Codex 작업 시작: {task}", flush=True)

        process = subprocess.Popen(
            command,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            encoding="utf-8",
            errors="replace",
            bufsize=1,
            start_new_session=True,
        )

        output_queue = queue.Queue()

        def read_output():
            try:
                for line in process.stdout:
                    output_queue.put(line)
            finally:
                output_queue.put(None)

        reader = threading.Thread(
            target=read_output,
            daemon=True,
        )
        reader.start()

        usage_limit = False
        timed_out = False
        reader_finished = False
        deadline = time.monotonic() + timeout
        next_report = time.monotonic() + report_interval

        try:
            with log_path.open("w", encoding="utf-8") as log:
                while True:

                    if (
                        on_progress is not None
                        and time.monotonic() >= next_report
                    ):
                        elapsed = timeout - max(
                            0,
                            deadline - time.monotonic()
                        )

                        try:
                            on_progress(task, int(elapsed))
                        except Exception as e:
                            print(f"진행 보고 실패: {e}")

                        next_report = time.monotonic() + report_interval

                    if time.monotonic() >= deadline:
                        timed_out = True
                        break

                    try:
                        line = output_queue.get(timeout=0.5)
                    except queue.Empty:
                        if reader_finished and process.poll() is not None:
                            break
                        continue

                    if line is None:
                        reader_finished = True
                        if process.poll() is not None:
                            break
                        continue

                    print(line, end="", flush=True)
                    log.write(line)
                    log.flush()

                    if re.search(
                        r"usage limit|rate limit|usage_limit_reached|"
                        r"try again at|you.ve hit your usage limit",
                        line,
                        re.IGNORECASE,
                    ):
                        usage_limit = True

                if timed_out:
                    print("Codex 실행 시간 초과", flush=True)

        finally:
            if process.poll() is None:
                os.killpg(process.pid, signal.SIGTERM)
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, signal.SIGKILL)
                    process.wait()

            if process.stdout:
                process.stdout.close()

        returncode = process.returncode

        if timed_out:
            status = "timeout"
        elif usage_limit:
            status = "usage_limit"
        elif returncode == 0:
            status = "success"
        else:
            status = "failed"

        return AgentResult(
            status=status,
            returncode=returncode,
            log_path=log_path,
        )
